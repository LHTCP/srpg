# SRPG 기술 설계 문서 (TDD)

버전 0.5 — 13단계(맵 편의성 개선) 이후 코드 기준.

---

## 1. 아키텍처 개요

```
SrpLobbyController (MonoBehaviour)  — 로비 씬: 맵/프리셋 선택, 메이커 진입
  └── SrpGameSettings (static)       — 씬 간 설정 전달 (SelectedPreset, CustomMap, 씬 상수 5개)

SrpGameController (MonoBehaviour) ← partial class 3파일
  │  SrpGameController.cs           — 핵심 필드·Awake·카메라·입력·스킬타게팅·전투·게임 흐름
  │  SrpGameController.Rendering.cs — 그리드·유닛 뷰·타일 색상
  │  SrpGameController.Hud.cs       — HUD 생성·로그·스킬 슬롯·Tooltip·UpdateHud
  │
  ├── SrpBattleState           — 시뮬레이션 상태(그리드, 유닛, 턴, SkillLookup)
  │     ├── SrpUnitRuntime     — 유닛 인스턴스 (skillRuntimes 포함)
  │     └── SrpPathfinder      — Dijkstra 이동 탐색
  ├── SrpCombatResolver        — 전투 해석(AP·HP·PG·처단)
  ├── SrpSkills                — 데이터 기반 스킬 효과 해석 (패시브·액티브)
  ├── SrpSkillData             — 스킬/유닛 데이터 모델 + enum + DB 래퍼
  ├── SrpDefaultMaps           — 내장 맵 3종
  ├── SrpDefaultSkills         — 기본 스킬 4종 시드
  ├── SrpDefaultUnits          — 기본 유닛 3종 시드
  ├── SrpMapIO                 — 맵 JSON 저장/로드
  ├── SrpDataIO                — 스킬/유닛 DB JSON 저장/로드
  ├── SrpDevTools              — 개발자 도구 (F3 패널)
  └── SrpFontWarmup            — TMP 한글 글리프 사전 로드

SrpSkillMakerController (MonoBehaviour) — 스킬 에디터 씬
SrpUnitMakerController  (MonoBehaviour) — 유닛 에디터 씬
SrpMapMakerController   (MonoBehaviour) — 맵 에디터 씬
```

- **시뮬레이션 분리**: `SrpBattleState`는 Unity 의존성이 없어 테스트·되감기에 유리.
- **partial class 분리**: `SrpGameController`는 한 MonoBehaviour지만 관심사별로 3개 파일로 나뉜다.
- **뷰**: `SrpGameController.Rendering.cs`가 Cube(타일) + Cylinder(유닛) Primitive 생성·갱신 담당.
- **입력**: `SrpTileView.OnMouseDown` → `SrpGameController.OnTileClicked(x, y)`. 타일만 콜라이더 보유.
- **영속화**: `JsonUtility` 기반. 맵(`SrpMapFileV1`), 스킬 DB(`SrpSkillDatabase`), 유닛 DB(`SrpUnitDatabase`).

---

## 2. 직사각형 그리드

- 좌표: `x ∈ [0, width)`, `y ∈ [0, height)`.
- 타일 평탄 배열: `walkable[y * width + x]`.
- 유닛 점유 조회: `SrpBattleState.GetOccupant(x, y)` — footprint 전체 칸 탐색.

---

## 3. 맵·유닛 JSON 스키마 v1

```json
{
  "version": 1,
  "name": "string",
  "width": 10,
  "height": 8,
  "walkable": [ true, false, "..." ],
  "playerOrder": [ 0, 1 ],
  "allowedSkillIds": [],
  "templates": [
    {
      "id": "knight",
      "displayName": "기사",
      "moveRange": 5,
      "attackRange": 1,
      "attackPower": 12,
      "maxHp": 40,
      "maxAp": 15,
      "maxPosture": 80,
      "skillIds": ["heart_spike"],
      "maxSkills": 4,
      "frozenHeart": 0,
      "tags": 0,
      "footprintWidth": 1,
      "footprintHeight": 1
    }
  ],
  "placements": [
    {
      "templateId": "knight",
      "owner": 0,
      "x": 1,
      "y": 2,
      "footprint": [],
      "disabledSkillIds": []
    }
  ]
}
```

- `tags`: 비트마스크 — `Boss = 1`, `Large = 2`.
- `footprint`: 상대 좌표 `{dx, dy}` 목록. 비어 있으면 `footprintWidth × footprintHeight`로 자동 생성.
- `disabledSkillIds`: 배치 단위로 특정 스킬 비활성화.
- `allowedSkillIds`: 맵 전역 스킬 화이트리스트 (빈 배열이면 제한 없음).

---

## 4. 이동 탐색 (SrpPathfinder)

- **알고리즘**: 단순 Dijkstra (최솟값을 List 선형 탐색으로 추출 — 소규모 맵에서 충분).
- **비용 모델**: 기본 1/칸. 적 유닛이 4방향 인접한 칸 진입 시 +1 (ZOC).
- **API**:
  - `GetReachableAnchors(state, unit)` — 도달 가능 위치 목록(기존 호환).
  - `GetReachableWithCosts(state, unit, maxCost)` — `Dictionary<Vector2Int, int>` 위치→비용 맵. 잔여 이동력을 `maxCost`로 제한 가능.

---

## 5. 전투 해석 순서 (SrpCombatResolver)

1. 공격자·방어자 거리 검증 (`ChebyshevAnchor ≤ attackRange`).
2. 방어자 **그로기** 여부 확인.
   - 그로기: AP 무시, HP 직접 차감(처단). PG 0·그로기 해제.
   - 일반: `min(damage, AP)` → AP 차감. 나머지 → HP 차감. HP 피해의 50% → PG 증가.
3. PG ≥ maxPosture 이면 그로기 상태 진입.
4. HP ≤ 0 이면 사망 플래그 설정.

`AttackOutcome` 구조체로 결과 반환: `damageToAp`, `damageToHp`, `wasExecution`, `defenderDied`, `postureGained`, `becameGroggy`.

---

## 6. 스킬 시스템 (SrpSkillData + SrpSkills)

### 데이터 모델

```
SrpSkillData
  ├── id, displayName, description
  ├── skillType: Active | Passive
  ├── trigger: OnActivate | OnTurnStart | OnAttackHit | OnTakeDamage
  ├── targetType: None | Self | SingleEnemy | SingleAlly | AreaEnemy | AreaAlly
  ├── range, areaSize
  ├── endsActivation: bool
  ├── cooldown: int
  └── effects: SrpSkillEffect[]
        ├── type: Damage | Heal | BuffStat | DebuffStat | FrozenHeart | Cleave
        ├── stat: string (hp, ap, attackPower, moveRange, attackRange, self)
        ├── value: int
        └── duration: int
```

### 런타임

- `SrpSkillRuntime`: 유닛별 스킬 인스턴스. `skillId` + `cooldownRemaining`.
- `SrpUnitRuntime.skillRuntimes`: 유닛이 보유한 스킬 런타임 목록.

### 실행 흐름

| 트리거 | 호출 시점 | 메서드 |
|--------|-----------|--------|
| `OnTurnStart` (패시브) | 유닛 첫 활성화 시 | `SrpSkills.TryApplyPassiveTurnStart` |
| `OnAttackHit` (패시브) | 공격 적중 후 | `SrpSkills.OnAttackResolved` |
| `OnTakeDamage` (패시브) | 피격 시 | `SrpSkills.OnTakeDamage` |
| `OnActivate` (액티브) | 스킬 버튼 → 타게팅 → 확정 | `SrpSkills.ResolveActiveSkill` |

### 액티브 스킬 타게팅

1. HUD 스킬 버튼 클릭 → `BeginSkillTargeting(data, runtime)`.
2. `targetType == Self`면 즉시 발동.
3. 그 외: `Phase = SelectingSkillTarget`, 대상 타일 보라색 하이라이트.
4. 대상 타일 클릭 → `SrpSkills.ResolveActiveSkill` 실행 → 쿨다운 설정.
5. `endsActivation`이면 유닛 행동 종료, 아니면 `UnitActive`로 복귀.

### 쿨다운

- 턴 종료 시 `SrpSkills.TickCooldownsForPlayer` — 해당 플레이어 유닛의 모든 스킬 `cooldownRemaining--`.
- `cooldownRemaining > 0`이면 스킬 사용 불가.

---

## 7. 턴·상태 관리 (SrpGameController — 3파일 partial class)

### Phase enum

```csharp
enum Phase { Idle, UnitActive, SelectingSkillTarget }
```

- `Idle`: 유닛 미선택. "플레이어 턴 종료" 버튼 활성.
- `UnitActive`: 유닛 활성화 중. 이동/공격/스킬 사용 가능. "유닛 완료" 버튼 활성.
- `SelectingSkillTarget`: 액티브 스킬 대상 선택 중. 타일 클릭으로 확정 또는 취소.

### 핵심 필드

| 파일 | 필드 | 타입 | 설명 |
|------|------|------|------|
| `.cs` | `_selectedId` | `int?` | 현재 활성 유닛 ID |
| `.cs` | `_remainingMove` | `int` | 잔여 이동력 |
| `.cs` | `_hasAttackedThisTurn` | `bool` | 이번 활성화에서 공격 여부 |
| `.cs` | `_moveCostMap` | `Dictionary<Vector2Int,int>` | 도달 가능 위치→비용 |
| `.cs` | `_attackIds` | `List<int>` | 공격 가능 적 ID 목록 |
| `.cs` | `_actedUnitsThisTurn` | `HashSet<int>` | 이번 플레이어 턴 완료 유닛 세트 |
| `.cs` | `_pendingSkillData` | `SrpSkillData` | 타게팅 중인 스킬 데이터 |
| `.cs` | `_pendingSkillRuntime` | `SrpSkillRuntime` | 타게팅 중인 스킬 런타임 |
| `.cs` | `_skillTargetTiles` | `List<Vector2Int>` | 스킬 대상 타일 목록 |
| `.Rendering.cs` | `_tiles` | `GameObject[,]` | 타일 GameObject 배열 |
| `.Rendering.cs` | `_unitObjs` | `Dictionary<int,GameObject>` | 유닛 ID → GameObject |
| `.Hud.cs` | `_log` | `List<string>` | 로그 내역(최대 80줄) |

### 턴 종료 흐름

```
OnEndTurnSoft()
  └─ (Idle 상태) 체크
  └─ AdvancePlayerTurn()
       └─ _actedUnitsThisTurn.Clear()
       └─ _state.AdvanceToNextLivingPlayer()
       └─ SrpSkills.TickCooldownsForPlayer()
       └─ ResetPassivesForCurrentPlayer()
       └─ CheckWin()
       └─ UpdateHud()
```

> 이중 클릭 방지는 `UpdateHud()` 안의 `_btnEndTurn.interactable = (_phase == Phase.Idle)` 조건으로 처리.

---

## 8. 되감기 (Undo)

- `PushUndo()`: `_state.Clone()`을 스택에 저장.
- `OnUndo()`: 스택에서 팝 → 상태 복원 → 뷰 갱신.
- `_actedUnitsThisTurn`, 스킬 타게팅 상태도 함께 초기화.

---

## 9. HUD (코드 생성 uGUI — TMP)

- `Canvas` + `CanvasScaler` (referenceResolution 1920×1080, matchWidthOrHeight 0.5).
- 좌패널(`LeftPanel`): 고정 폭(`leftPanelWidth`, 기본 370), 앵커 좌측 전체 높이.
  - 유닛 정보, 이동력/공격 상태, 스킬 버튼 목록, 액션 버튼(유닛 완료·턴 종료·되감기·로비 복귀).
- 우패널(`RightPanel`): 고정 폭(`rightPanelWidth`, 기본 370), 앵커 우측 전체 높이.
  - ScrollRect 기반 로그(최대 80줄). `RectMask2D` 클리핑. 자동 스크롤(2프레임 지연 코루틴).
- **텍스트**: 전부 `TextMeshProUGUI` (SDF 렌더링). 폰트: Pretendard-Regular SDF (Dynamic).
- **스킬 Tooltip**: `EventTrigger`(PointerEnter/Exit) + 재사용 팝업 GameObject.
- `SrpFontWarmup.Warmup()`: 한글 자주 사용 글리프를 Awake에서 사전 로드.

---

## 10. 개발자 도구 vs 프로덕션

- **조건**: `Application.isEditor || Debug.isDebugBuild` → SrpDevTools UI 표시.
- **F3 패널 위치**: 화면 오른쪽 상단(260×168px).
- **기능**: JSON 저장(파일명 입력), 불러와 즉시 적용, 씬 재시작.
- **릴리스 빌드**: UI 비표시, 저장 기능 잠금.

---

## 11. 데이터 영속화 (IO)

### 저장 경로

| 대상 | 파일 | 경로 |
|------|------|------|
| 맵 | `SrpMapIO` | `persistentDataPath/SrpMaps/{name}.json` |
| 스킬 DB | `SrpDataIO` | `persistentDataPath/SrpData/skills.json` |
| 유닛 DB | `SrpDataIO` | `persistentDataPath/SrpData/units.json` |

### 직렬화

- `JsonUtility` 사용. `[Serializable]` 필수.
- 스키마 버전 필드(`version`)로 하위 호환.
- `LoadSkillsOrDefault()` / `LoadUnitsOrDefault()`: 파일 없으면 `SrpDefaultSkills` / `SrpDefaultUnits`에서 시드 생성.
- `SrpMapIO.ListMaps()`: `SrpMaps/*.json` 파일명 배열 반환 (드롭다운 목록용).

### 타입 관계

```
SrpSkillDatabase ─── SrpSkillData[] ─── SrpSkillEffect[]
SrpUnitDatabase ──── SrpUnitTemplateData[] (skillIds: string[])
SrpMapFileV1 ─────── SrpUnitTemplateData[] (templates)
                  └── SrpPlacementData[] (placements, disabledSkillIds)
SrpBattleState.FromMap(SrpMapFileV1) → 런타임 변환 (SkillLookup 포함)
```

---

## 12. 메이커 시스템 (에디터 씬 3종)

### 공통 패턴

- 각 메이커는 독립 씬 + 독립 MonoBehaviour.
- UI: 전부 코드 생성 uGUI (TMP). 프리팹 없음.
- 데이터: `SrpDataIO` / `SrpMapIO`로 JSON 저장/로드.
- `TMP_InputField`: `caretWidth = 2`, `enabled = false → true` 재활성화로 caret 초기화.

### 스킬 메이커 (`SrpgSkillMaker` 씬)

- `SrpSkillMakerController`: 스킬 CRUD. 효과 배열 편집 (타입·스탯·값·지속).
- 저장 시 `SrpDataIO.SaveSkills()`.

### 유닛 메이커 (`SrpgUnitMaker` 씬)

- `SrpUnitMakerController`: 유닛 템플릿 CRUD. 스탯 편집, Large 풋프린트(가로×세로), 스킬 할당.
- 스킬 목록은 `SrpDataIO.LoadSkillsOrDefault()`에서 로드.

### 맵 메이커 (`SrpgMapMaker` 씬)

- `SrpMapMakerController`: 시각 그리드 편집 + 유닛 배치.
- `EditMode { Terrain, PlaceUnit, RemoveUnit }` 전환.
- 카메라 확대/축소/패닝 자체 구현.
- 유닛별 스킬 비활성화(`disabledSkillIds`) 토글.
- 불러오기: `TMP_Dropdown` + `SrpMapIO.ListMaps()` 목록.
- 저장 완료 후 드롭다운 목록 자동 갱신.

---

## 13. 씬 구조

### 씬 이름 규약

| 씬 | 상수(`SrpGameSettings`) | 컨트롤러 |
|----|------------------------|----------|
| 로비 | `LobbyScene = "SrpgLobby"` | `SrpLobbyController` |
| 전투 | `BattleScene = "SrpgBattle"` | `SrpGameController` |
| 스킬 메이커 | `SkillMakerScene = "SrpgSkillMaker"` | `SrpSkillMakerController` |
| 유닛 메이커 | `UnitMakerScene = "SrpgUnitMaker"` | `SrpUnitMakerController` |
| 맵 메이커 | `MapMakerScene = "SrpgMapMaker"` | `SrpMapMakerController` |

### 씬 전환 흐름

```
SrpgLobby
  ├─ 내장 프리셋 선택 → SrpGameSettings.StartBattle(preset) → SrpgBattle
  ├─ JSON 맵 로드    → SrpGameSettings.StartBattleWithMap(map) → SrpgBattle
  ├─ 스킬 메이커     → SceneManager.LoadScene("SrpgSkillMaker")
  ├─ 유닛 메이커     → SceneManager.LoadScene("SrpgUnitMaker")
  └─ 맵 메이커       → SceneManager.LoadScene("SrpgMapMaker")

SrpgBattle
  └─ ◀ 로비로 돌아가기 → SrpGameSettings.ReturnToLobby()
```

### SrpGameSettings 설계

- **정적 클래스**: `DontDestroyOnLoad` 없이 C# static 필드만으로 씬 사이 값 유지.
- `CustomMap`: `SrpMapFileV1` 참조. `Awake()`에서 소비(읽은 뒤 null로 초기화).
- `SelectedPreset`: `SrpMapPreset` enum. 로비 선택값이 없으면 기본값 `Skirmish`.

---

## 14. 폴더 구조

```
Assets/Scripts/SRPG/
├── SrpBattleState.cs              — 시뮬레이션 상태(그리드·유닛·점유·ZOC·SkillLookup)
├── SrpCombatResolver.cs           — 전투 해석(AP·HP·PG·처단)
├── SrpDataIO.cs                   — 스킬/유닛 DB JSON IO
├── SrpDefaultMaps.cs              — 내장 맵 3종 코드 생성
├── SrpDefaultSkills.cs            — 기본 스킬 4종 시드
├── SrpDefaultUnits.cs             — 기본 유닛 3종 시드
├── SrpDevTools.cs                 — 개발자 F3 패널
├── SrpFontWarmup.cs               — TMP 한글 글리프 사전 로드
├── SrpGameController.cs           — 핵심 필드·Awake·입력·스킬타게팅·게임 흐름 (partial)
├── SrpGameController.Rendering.cs — 그리드·유닛 뷰·타일 색상 (partial)
├── SrpGameController.Hud.cs       — HUD·로그·스킬 UI·Tooltip (partial)
├── SrpGameSettings.cs             — 씬 간 설정 전달(로비↔전투↔메이커)
├── SrpLobbyController.cs          — 로비 씬 MonoBehaviour
├── SrpMapFile.cs                  — JSON 스키마 v1 (풋프린트·스킬제한 포함)
├── SrpMapIO.cs                    — 맵 저장/로드 + ListMaps
├── SrpMapMakerController.cs       — 맵 메이커 씬 (그리드 편집·유닛 배치·카메라)
├── SrpMapPreset.cs                — 프리셋 enum
├── SrpPathfinder.cs               — 이동 탐색(다익스트라 + ZOC)
├── SrpSkillData.cs                — 스킬 정의 모델 + enum + 런타임 + DB 래퍼
├── SrpSkillMakerController.cs     — 스킬 메이커 씬 (CRUD·효과 편집)
├── SrpSkills.cs                   — 데이터 기반 스킬 효과 해석
├── SrpTileView.cs                 — 타일 클릭 위임
├── SrpUnitMakerController.cs      — 유닛 메이커 씬 (스탯·스킬·풋프린트)
├── SrpUnitRuntime.cs              — 유닛 인스턴스 (스킬 런타임 포함)
└── SrpUnitTags.cs                 — Boss/Large 비트마스크

Assets/Scripts/Chess/              — 레거시 체스(참고용)
docs/srpg/                         — 기획·기술·이력 문서
```

맵 저장 경로(개발): `Application.persistentDataPath/SrpMaps/*.json`
스킬/유닛 DB 경로: `Application.persistentDataPath/SrpData/`

---

## 15. 향후 작업 (Backlog 참조)

- AI 스텁 (무작위 합법 수 → 레벨 검증용).
- 2인 원격 세션 (액션 커맨드 스트림 기반 동기화).
- 전투 중 맵 스킬 제한 적용 (`disabledSkillIds` 검증).

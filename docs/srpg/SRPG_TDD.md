# SRPG 기술 설계 문서 (TDD)

버전 0.3 — 현재 구현 코드 기준.

---

## 1. 아키텍처 개요

```
SrpLobbyController (MonoBehaviour)  — 로비 씬: 맵 선택 → 전투 씬 전환
  └── SrpGameSettings (static)       — 씬 간 설정 전달 (SelectedPreset, CustomMap)

SrpGameController (MonoBehaviour) ← partial class 3파일로 분리
  │  SrpGameController.cs           — 핵심 필드·Awake·입력·전투·게임 흐름
  │  SrpGameController.Rendering.cs — 그리드·유닛 뷰·타일 색상
  │  SrpGameController.Hud.cs       — HUD 생성·LogLine·UpdateHud
  │
  ├── SrpBattleState         — 시뮬레이션 상태(그리드, 유닛, 턴)
  │     ├── SrpUnitRuntime   — 유닛 인스턴스
  │     └── SrpPathfinder    — BFS 이동 탐색
  ├── SrpCombatResolver      — 전투 해석(AP·HP·PG·처단)
  ├── SrpSkills              — 스킬 효과 처리
  ├── SrpDefaultMaps         — 내장 맵 3종
  ├── SrpMapIO               — JSON 저장/로드
  └── SrpDevTools            — 개발자 도구 (F3 패널)
```

- **시뮬레이션 분리**: `SrpBattleState`는 Unity 의존성이 없어 테스트·되감기에 유리.
- **partial class 분리**: `SrpGameController`는 한 MonoBehaviour지만 관심사별로 3개 파일로 나뉜다. Unity 인스펙터·씬 참조는 영향 없음.
- **뷰**: `SrpGameController.Rendering.cs`가 그리드(Cube Primitive) + 유닛(Cylinder Primitive) 생성·갱신 담당.
- **입력**: `SrpTileView.OnMouseDown` → `SrpGameController.OnTileClicked(x, y)`. 타일만 콜라이더 보유, 유닛 메시에는 콜라이더 없음.
- **영속화**: `SrpMapFileV1` JSON (`JsonUtility`). 스키마 버전 필드로 하위 호환 유지.

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
  "walkable": [ true, false, ... ],
  "playerOrder": [ 0, 1 ],
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
      "tags": 0
    }
  ],
  "placements": [
    {
      "templateId": "knight",
      "owner": 0,
      "x": 1,
      "y": 2,
      "footprint": []
    }
  ]
}
```

- `tags`: 비트마스크 — `Boss = 1 (SrpUnitTags.Boss)`, `Large = 2 (SrpUnitTags.Large)`.
- `footprint`: 상대 좌표 `{dx, dy}` 목록. 비어 있으면 1×1 유닛.

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

---

## 6. 턴·상태 관리 (SrpGameController — 3파일 partial class)

### Phase enum

```csharp
enum Phase { Idle, UnitActive }
```

- `Idle`: 유닛 미선택. "플레이어 턴 종료" 버튼 활성.
- `UnitActive`: 유닛 활성화 중. "유닛 완료" 버튼 활성. "플레이어 턴 종료" 비활성.

### 핵심 필드

| 파일 | 필드 | 타입 | 설명 |
|------|------|------|------|
| `.cs` | `_selectedId` | `int?` | 현재 활성 유닛 ID |
| `.cs` | `_remainingMove` | `int` | 잔여 이동력 |
| `.cs` | `_hasAttackedThisTurn` | `bool` | 이번 활성화에서 공격 여부 |
| `.cs` | `_moveCostMap` | `Dictionary<Vector2Int,int>` | 도달 가능 위치→비용 |
| `.cs` | `_attackIds` | `List<int>` | 공격 가능 적 ID 목록 |
| `.cs` | `_actedUnitsThisTurn` | `HashSet<int>` | 이번 플레이어 턴 완료 유닛 세트 |
| `.Rendering.cs` | `_tiles` | `GameObject[,]` | 타일 GameObject 배열 |
| `.Rendering.cs` | `_unitObjs` | `Dictionary<int,GameObject>` | 유닛 ID → GameObject |
| `.Hud.cs` | `_log` | `List<string>` | 로그 내역(최대 80줄) |

### 턴 종료 흐름

```
OnEndTurnSoft()
  └─ (Idle 상태) 체크 — UnitActive 중엔 버튼 비활성
  └─ AdvancePlayerTurn()
       └─ _actedUnitsThisTurn.Clear()
       └─ _state.AdvanceToNextLivingPlayer()
       └─ ResetPassivesForCurrentPlayer()
       └─ CheckWin()
       └─ UpdateHud()
```

> 이중 클릭 방지는 `UpdateHud()` 안의 `_btnEndTurn.interactable = (_phase == Phase.Idle)` 조건으로 처리. 별도 쿨다운 코루틴 없음.

---

## 7. 되감기 (Undo)

- `PushUndo()`: `_state.Clone()`을 스택에 저장.
- `OnUndo()`: 스택에서 팝 → 상태 복원 → 뷰 갱신.
- `_actedUnitsThisTurn`도 초기화 (되감기 후 유닛 재선택 허용).

---

## 8. HUD (코드 생성 uGUI)

- `Canvas` + `CanvasScaler` (referenceResolution 1920×1080, matchWidthOrHeight 0.5).
- 좌패널(`LeftPanel`): 고정 폭(기본 420), 앵커 좌측 전체 높이.
- 우패널(`RightPanel`): 고정 폭(기본 420), 앵커 우측 전체 높이.
- 버튼·텍스트: 전부 코드에서 `AddComponent<Text/Button/Image>` 생성 (`typeof(RectTransform)` 명시).

---

## 9. 개발자 도구 vs 프로덕션

- **조건**: `Application.isEditor || Debug.isDebugBuild` → SrpDevTools UI 표시.
- **F3 패널 위치**: 화면 오른쪽 상단(260×168px).
- **기능**: JSON 저장(파일명 입력), 불러와 즉시 적용, 씬 재시작.
- **릴리스 빌드**: UI 비표시, 저장 기능 잠금.

---

## 10. 폴더 구조

```
Assets/Scripts/SRPG/
├── SrpBattleState.cs              — 시뮬레이션 상태
├── SrpCombatResolver.cs           — 전투 해석
├── SrpDefaultMaps.cs              — 내장 맵 3종
├── SrpDevTools.cs                 — 개발자 F3 패널
├── SrpGameController.cs           — 핵심 필드·Awake·입력·게임 흐름
├── SrpGameController.Rendering.cs — 그리드·유닛 뷰 (partial)
├── SrpGameController.Hud.cs       — HUD 생성·로그·갱신 (partial)
├── SrpMapFile.cs                  — JSON 스키마 v1
├── SrpMapIO.cs                    — 맵 저장/로드
├── SrpMapPreset.cs                — 프리셋 enum
├── SrpPathfinder.cs               — 이동 탐색
├── SrpSkills.cs                   — 스킬 효과 스텁
├── SrpTileView.cs                 — 타일 클릭 위임
├── SrpUnitRuntime.cs              — 유닛 인스턴스
└── SrpUnitTags.cs                 — Boss/Large 비트마스크

├── SrpGameSettings.cs             — 씬 간 설정 전달(로비↔전투)
└── SrpLobbyController.cs          — 로비 씬 MonoBehaviour

Assets/Scripts/Chess/              — 레거시 체스(참고용)
docs/srpg/                         — 기획·기술·이력 문서
```

맵 저장 경로(개발): `Application.persistentDataPath/SrpMaps/*.json`

---

## 11. 로비 씬 구조

### 씬 이름 규약

| 씬 | 상수(`SrpGameSettings`) |
|----|------------------------|
| 로비 | `LobbyScene = "SrpgLobby"` |
| 전투 | `BattleScene = "SrpgBattle"` |

### 씬 전환 흐름

```
SrpgLobby
  └─ SrpLobbyController.OnStartBattle()
       ├─ 내장 프리셋 선택 → SrpGameSettings.StartBattle(preset)
       └─ JSON 로드 맵   → SrpGameSettings.StartBattleWithMap(map)
                                  ↓
                           SrpgBattle 씬 로드
                                  ↓
                           SrpGameController.Awake()
                             SrpGameSettings.CustomMap != null
                               → 해당 맵 사용
                             else
                               → SrpGameSettings.SelectedPreset 내장 맵 사용
```

### SrpGameSettings 설계

- **정적 클래스**: `DontDestroyOnLoad` 없이 C# static 필드만으로 씬 사이 값 유지.
- `CustomMap`: `SrpMapFileV1` 참조. `Awake()`에서 소비(읽은 뒤 null로 초기화).
- `SelectedPreset`: `SrpMapPreset` enum. 로비 선택값이 없으면 기본값 `Skirmish`.

### Unity 설정 체크리스트

- [ ] `SrpgLobby` 씬 생성 → 빈 GameObject에 `SrpLobbyController` 추가.
- [ ] 기존 전투 씬 이름을 `SrpgBattle`로 변경.
- [ ] **Build Settings** → 두 씬 모두 등록 (Lobby index 0, Battle index 1 권장).

---

## 12. 향후 작업 (Backlog 참조)

- ScrollRect 기반 로그 패널.
- AI 스텁 (무작위 합법 수 → 레벨 검증용).
- TMP(TextMeshPro) 전환 — 한글 가독성 향상.

# SRPG 프로토타입 구현 진행 이력

> 최초 채팅 시작부터 현재까지 작업된 내용을 시간 순으로 기록한다.  
> 코드 수정 세부 사항은 각 파일의 주석과 커밋 메시지를 참고한다.

---

## 0단계 — 프로젝트 방향 결정

### 출발점
- 기존 프로젝트는 Unity 6 기반 **체스 프로토타입**으로 시작됨.
- 체스 규칙 자체는 관심 없고, **SRPG(전술 시뮬레이션 RPG)** 를 만드는 것이 목적.

### 결정된 기본 방향
- 체스 코드(`Assets/Scripts/Chess/`)는 참고용으로 남기되 주 플로우에서 제외.
- `Assets/Scripts/SRPG/`에 새 엔진을 처음부터 작성.
- 프로토타입 목표: **맵 세팅 → 핫시트 플레이 → 유닛 이동/공격** 사이클 검증.

### 1차 기획 결정 사항
| 항목 | 결정 |
|------|------|
| 맵 구조 | 평지 직사각형 타일(높낮이 없음) |
| 플레이어 | 최대 6 슬롯(프로토타입: 2인 핫시트) |
| 이동 | 4방향 BFS, 이동력(moveRange) 내 |
| 공격 | 체비쇼프 거리(8방향) |
| ZOC | 적 4방향 인접 칸 이동 비용 +1 |
| 턴 구조 | 한 플레이어가 자기 유닛 전부를 운용, 버튼으로 명시적으로 턴 종료 |

---

## 1단계 — 레거시 체스 버그 수정 (P0)

**대상 파일**: `Assets/Scripts/Chess/ChessGameManager.cs`

### 수정 내용
- **흑 턴에서 잡기 불가 버그**: 적 유닛 위에 `OnMouseDown` 콜라이더가 타일보다 앞에 맞아, 포획 목적지 클릭이 `OnPieceClicked(적)` → 턴 오류로 처리되던 문제.
- **수정**: 아군 선택 상태에서 적 말을 클릭하면 해당 타일의 합법 이동 여부를 먼저 검사한 뒤 포획 이동으로 위임.

---

## 2단계 — SRPG 코어 엔진 초기 구현

### 생성된 파일 목록

| 파일 | 역할 |
|------|------|
| `SrpMapFile.cs` | JSON 직렬화용 맵·유닛 스키마 v1(`SrpMapFileV1`, `SrpUnitTemplateData`, `SrpPlacementData`) |
| `SrpUnitTags.cs` | 비트마스크 태그(`Boss`, `Large`) |
| `SrpUnitRuntime.cs` | 전장 유닛 인스턴스(모든 런타임 스탯, footprint, groggy, FH 등) |
| `SrpBattleState.cs` | 그리드 상태, 점유 조회, ZOC, 이동 가능 검사, 승리 조건 |
| `SrpPathfinder.cs` | 가중 다익스트라 이동 탐색(`GetReachableAnchors`, `GetReachableWithCosts`) |
| `SrpCombatResolver.cs` | AP/HP 피해, 자세(PG), 그로기, 처단 해석 |
| `SrpSkills.cs` | 스킬 ID별 효과(heart_spike, fh_bless_ally 등 프로토타입 스텁) |
| `SrpMapFile.cs` | JSON 스키마 |
| `SrpMapIO.cs` | 맵 JSON 저장/로드(`Application.persistentDataPath/SrpMaps/`) |
| `SrpMapPreset.cs` | `SrpMapPreset` enum(`Skirmish`, `TinyDuel`, `Corridor`) |
| `SrpDefaultMaps.cs` | 코드 생성 내장 맵 3종 |
| `SrpTileView.cs` | 타일 클릭 → `SrpGameController.OnTileClicked` 위임 |
| `SrpDevTools.cs` | F3 패널(에디터·DevBuild): 맵 저장/불러오기, 씬 재시작 |
| `SrpGameController.cs` | 전투 뷰·입력·HUD·턴 흐름 전담 MonoBehaviour |

### 내장 맵 3종

| 프리셋 | 크기 | 특징 |
|--------|------|------|
| `TinyDuel` | 6×4 | 기사 1vs1, 장애물 없음 — 입력·이동 최소 검증 |
| `Corridor` | 8×10 | 중앙 2칸 장애물 띠(통로 y=5만 개방) — ZOC·우회 검증 |
| `Skirmish` | 10×8 | 기사·궁수·보스 브루트(2칸 풋프린트) — 대형 유닛·스킬 포함 |

---

## 3단계 — HUD 레이아웃 반복 개선

### 변천 요약

1. **초기 상하 배치**: TopPanel(화면 위 ~48%) + LogPanel(아래 ~50%) → 보드 가림·UI 과대.
2. **CanvasScaler 설정 오류**: `referenceResolution` 미설정 → 해상도별 크기 불안정.
3. **버튼 라벨 NRE**: `GameObject`에 `LayoutElement` 추가 후 `RectTransform` 추가 순서 문제 → `NullReferenceException` (245행).
4. **최종 레이아웃**: 좌/우 사이드 패널 2개로 전환.

### 현재 HUD 구조 (최종)

```
┌──[LEFT 420px]──┬──────────────────────┬──[RIGHT 420px]──┐
│ 플레이어·맵 정보 │                      │ [로그 숨기기]    │
│ 상태(이동력·공격)│   보드(화면 중앙)     │ ...로그 내역...  │
│ 선택 유닛 스탯  │                      │                 │
│ ─────────────  │                      │                 │
│ [유닛 완료]     │                      │                 │
│ [플레이어 턴 종료│                      │                 │
│ [되감기]        │                      │                 │
└────────────────┴──────────────────────┴─────────────────┘
```

- **CanvasScaler**: `referenceResolution = 1920×1080`, `matchWidthOrHeight = 0.5`.
- **폰트**: TurnInfo 28px / Status 22px / UnitInfo 20px / Log 20px / 버튼 24px.
- **버튼 높이**: 60px (액션), 52px (로그 토글).
- **패널 폭**: 인스펙터 `Left/Right Panel Width` 필드(기본 420).

---

## 4단계 — 턴 구조 재설계

### 구 구조 (이동 강제 순서)

```
Idle → 유닛 클릭 → SelectingMove → 이동 확정 → SelectingAttack → 공격/스킵 → AdvancePlayerTurn
```

문제: 이동과 공격 순서가 강제됨. 한 유닛 공격 시 플레이어 전체 턴 종료.

### 현재 구조 (자유 액션)

```
Idle → 유닛 클릭 → UnitActive
  ├─ 이동 칸(녹) 클릭 → 이동(잔여 이동력 차감), UnitActive 유지
  ├─ 적 칸(적) 클릭 → 공격(1회), FinishActivation → Idle
  ├─ [유닛 완료] 버튼 → FinishActivation → Idle
  └─ 다른 유닛 클릭 → 현재 유닛 완료 요구 메시지
                    (활성화 중 타 유닛 전환 불가)

Idle → [플레이어 턴 종료] → AdvancePlayerTurn → 다음 플레이어
```

### 핵심 규칙

- **이동**: 잔여 이동력(`_remainingMove`) 내에서 여러 번 이동 가능. 이동할 때마다 실제 BFS 비용 차감.
- **공격**: 유닛당 1회. 공격하면 해당 유닛 활성화 즉시 종료.
- **유닛 활성화 중 타 유닛 전환 불가**: 현재 유닛을 완료(`유닛 완료` 버튼 또는 공격)해야 다음 유닛 선택 가능.
- **이미 행동한 유닛 재선택 불가**: `_actedUnitsThisTurn` 세트로 추적. 플레이어 턴 종료 시 초기화.
- **`플레이어 턴 종료` 버튼**: Idle 상태에서만 활성. `_endTurnLock`(1프레임 쿨다운)으로 이중 클릭 방지.

---

## 5단계 — 전투 로직 수정

### 이동 후 뷰 갱신 누락 수정
- **증상**: 이동 확정 시 유닛이 시각적으로 이동하지 않고, 공격/피격 시에야 렌더링.
- **원인**: `su.anchorX/Y` 갱신 후 `RefreshUnitViews()` 미호출.
- **수정**: 이동 확정 직후 `RefreshUnitViews()` 추가.

### 공격 사거리 8방향(체비쇼프) 전환
- **기존**: 맨해튼 거리(`|dx|+|dy|`) → 대각선 미포함.
- **수정**: `SrpBattleState.ChebyshevAnchor` 추가(`max(|dx|,|dy|)`), `SrpCombatResolver.CanAttack`에서 체비쇼프 사용.
- 이동 탐색(BFS)은 4방향 맨해튼 유지.

### `SrpMapFileV1` null 체크 버그 수정
- **원인**: `SrpMapFileV1`은 `[Serializable]` 일반 클래스라 Unity 인스펙터에서 null이 아닌 빈 내장 객체로 직렬화됨. `initialMap == null` 체크가 항상 false → 빈 맵 로드 → 유닛 0 → 즉시 무승부 판정.
- **수정**: `initialMap.placements == null || placements.Length == 0` 조건 추가.

---

## 6단계 — 카메라 자동 프레이밍

- `FrameBoardCamera()`: `Camera.main` 직교 탑다운, `orthographicSize`를 보드 크기·화면 비율에 맞게 계산.
- 인스펙터 `Frame Camera On Start` 토글(기본 true).
- `ApplyMap()` 호출 시에도 자동 재적용.

---

## 7단계 — 개발자 도구

- `SrpDevTools`: F3 키로 화면 오른쪽 상단 패널 토글. 에디터·Development Build 전용(`Application.isEditor || Debug.isDebugBuild`).
- 기능: 맵 JSON 저장, 파일명으로 불러와 즉시 적용, 씬 재시작.
- 저장 경로: `Application.persistentDataPath/SrpMaps/`.

---

## 8단계 — SrpGameController 파일 분리 (리팩토링)

### 배경
`SrpGameController.cs`가 727줄에 달하며 렌더링·HUD·게임 흐름을 한 파일에서 전부 담당, 수정·탐색이 불편해졌음.

### 변경 내용
`partial class` 방식으로 관심사별 3파일로 분리. `MonoBehaviour`는 여전히 하나이므로 Unity 씬·인스펙터 참조 변경 없음.

| 파일 | 담당 | 줄 수 |
|------|------|-------|
| `SrpGameController.cs` | 핵심 필드·`Awake`·카메라·입력·전투·게임 흐름 | ~260 |
| `SrpGameController.Rendering.cs` | 그리드 생성·타일 색상·유닛 뷰 갱신 | ~120 |
| `SrpGameController.Hud.cs` | HUD 생성·`LogLine`·`UpdateHud` | ~230 |

### 함께 정리된 사항
- TDD의 `_endTurnLock` 기술 제거 — 실제 코드에 없는 필드였음. 현재 이중 클릭 방지는 `_btnEndTurn.interactable = (_phase == Phase.Idle)` 조건으로 충분히 처리됨.
- `UpdateHud()`에 `_btnUndo.interactable = (_undo.Count > 0)` 조건 추가(기존 누락).

---

---

## 9단계 — 로비 씬 분리

### 배경
단일 씬에서 인스펙터 `Start Preset`으로 맵을 고르던 방식을 씬 분리 구조로 전환.

### 추가된 파일

| 파일 | 역할 |
|------|------|
| `SrpGameSettings.cs` | 씬 간 설정 전달 정적 클래스(`SelectedPreset`, `CustomMap`). 씬 로드 후에도 static 필드로 값 유지. |
| `SrpLobbyController.cs` | 로비 씬 MonoBehaviour. 코드 생성 UI — 프리셋 선택 버튼 3종, JSON 로드 입력 필드, 전투 시작 버튼. |

### 수정된 파일

| 파일 | 변경 내용 |
|------|-----------|
| `SrpGameController.cs` | `Awake()` 앞쪽에서 `SrpGameSettings.CustomMap` / `SelectedPreset` 우선 읽기 추가. |
| `SrpGameController.Hud.cs` | 좌패널 하단에 "◀ 로비로 돌아가기" 버튼 추가. |

### 씬 이름 규약

| 씬 | 상수(`SrpGameSettings`) |
|----|------------------------|
| 로비 | `LobbyScene = "SrpgLobby"` |
| 전투 | `BattleScene = "SrpgBattle"` |

### Unity 설정 체크리스트

- [ ] `SrpgLobby` 씬 생성 → 빈 GameObject에 `SrpLobbyController` 추가.
- [ ] 기존 전투 씬 이름을 `SrpgBattle`로 변경(또는 복사).
- [ ] **Build Settings** → 두 씬 모두 추가(Lobby가 index 0, Battle이 index 1 권장).
- [ ] 빌드 후 진입점은 `SrpgLobby` 씬.

---

## 10단계 — 로그 패널 ScrollRect 전환

### 배경
오른쪽 로그 패널이 최근 22줄만 보이고 이전 로그를 볼 방법이 없었음.
80줄 전체를 마우스 휠로 스크롤할 수 있도록 `ScrollRect` 기반으로 재구성.

### 수정된 파일

| 파일 | 변경 내용 |
|------|-----------|
| `SrpGameController.Hud.cs` | `BuildRightPanel` — `_logBody`를 ScrollRect/Viewport/Content/Scrollbar 계층으로 재구성. `LogLine()` — 전체 로그 렌더링, `ApplyLogScroll()` 코루틴으로 최하단 자동 스크롤 |

### 주요 설계 결정

| 항목 | 선택 | 이유 |
|------|------|------|
| Viewport 클리핑 | `RectMask2D` | `Mask`+투명 Image는 코드 생성 시 스텐실 머티리얼 교체 타이밍 문제로 텍스트가 완전히 숨겨질 수 있음 |
| Content 높이 관리 | `LogLine`에서 `sizeDelta.y` 직접 설정 | `ContentSizeFitter`는 레이아웃 패스 bottom-up 단계에서 width=0 기준으로 preferredHeight를 잘못 읽는 순환 의존 문제 발생 |
| 스크롤 적용 시점 | `ApplyLogScroll()` 코루틴 (2프레임 지연) | Awake 직후 `Canvas.ForceUpdateCanvases()` 동기 호출 시 VLG 레이아웃 미확정 상태에서 normalizedPosition이 설정되어 Content가 뷰포트 밖으로 밀림 |
| 스크롤바 Visibility | `Permanent` | `AutoHideAndExpandViewport`는 Viewport offsetMax를 동적으로 재설정해 수동 여백(-14px)과 충돌 |

---

## 11단계 — TMP(TextMeshPro) 전환

### 배경
레거시 `UnityEngine.UI.Text`는 비트맵 폰트 기반이라 한글 가독성이 낮고 고해상도에서 흐릿하게 표시됨.
TMP(`TextMeshProUGUI`)로 전환해 SDF 렌더링 기반의 선명한 한글 출력을 확보.

### 수정된 파일

| 파일 | 변경 내용 |
|------|-----------|
| `SrpGameController.Hud.cs` | `Text` → `TextMeshProUGUI` (필드 5개, `MakeLabel`, `MakeButton`, `BuildRightPanel` 내 로그 텍스트). `SafeFont()` 제거. 정렬 enum → `TextAlignmentOptions`. `overflowMode = Overflow` 설정. |
| `SrpLobbyController.cs` | `Text` → `TextMeshProUGUI`, `InputField` → `TMP_InputField`. 모든 버튼·라벨 헬퍼 전환. `MakeInputField` 내부에 "Text Area" + `RectMask2D` viewport 구조 추가. `SafeFont()` 제거. 특수기호(`⚔`) 제거(폰트 미지원). |

### 주요 설계 결정

| 항목 | 선택 | 이유 |
|------|------|------|
| 폰트 에셋 | Pretendard Variable Dynamic SDF | 한글 지원, Variable 폰트로 단일 파일에 다중 굵기 포함 |
| 아틀라스 방식 | Dynamic | 한글 11,172자를 Static으로 구우면 아틀라스가 거대해지고 빌드 시간이 길어짐 |
| 기본 폰트 등록 | Project Settings → TextMeshPro → Default Font Asset | 코드 생성 UI 전체에 일괄 적용, 코드 변경 불필요 |
| `TMP_InputField` viewport | "Text Area" + `RectMask2D` | TMP_InputField는 textViewport 연결이 없으면 입력 커서·클리핑이 동작하지 않음 |

---

## 12단계 — 메이커 시스템 + 스킬 사용

### 배경
유닛별 스킬 사용, 스킬/유닛/맵을 생성·관리하는 메이커(에디터) 시스템이 필요.

### 추가된 파일

| 파일 | 역할 |
|------|------|
| `SrpSkillData.cs` | 스킬 정의 모델(`SrpSkillData`, `SrpSkillRuntime`, enum들, DB 래퍼) |
| `SrpDataIO.cs` | 스킬/유닛 DB JSON 저장/로드(`persistentDataPath/SrpData/`) |
| `SrpDefaultSkills.cs` | 기본 스킬 4종 시드(심장관통, 빙결축복, 강타, 치유의빛) |
| `SrpDefaultUnits.cs` | 기본 유닛 3종 시드(기사, 궁수, 브루트보스) |
| `SrpSkillMakerController.cs` | 스킬 메이커 씬 — CRUD, 효과 편집, JSON 저장 |
| `SrpUnitMakerController.cs` | 유닛 메이커 씬 — 스탯 편집, Large 풋프린트, 스킬 할당 |
| `SrpMapMakerController.cs` | 맵 메이커 씬 — 시각 그리드 편집, 유닛 배치, 카메라 조작 |
| `SrpFontWarmup.cs` | TMP Dynamic SDF 한글 글리프 사전 로드 |

### 수정된 파일

| 파일 | 변경 |
|------|------|
| `SrpGameSettings.cs` | 메이커 씬 상수 3개 추가 |
| `SrpUnitRuntime.cs` | `skillRuntimes`, `hasUsedSkillThisActivation` 필드 + Clone 확장 |
| `SrpBattleState.cs` | `SkillLookup` 딕셔너리, 스킬 DB 로드, 풋프린트 자동 생성 |
| `SrpSkills.cs` | 하드코딩 → 데이터 기반 동적 효과 시스템 전면 리팩터 |
| `SrpGameController.cs` | `Phase.SelectingSkillTarget`, 스킬 타게팅/취소, 쿨다운 Tick, HUD 630px |
| `SrpGameController.Hud.cs` | 스킬 사용 버튼, 스킬 목록 팝업, Tooltip(마우스 호버), 패널 너비 확대 |
| `SrpLobbyController.cs` | 메이커 진입 버튼 3개(스킬/유닛/맵) |
| `SrpMapFile.cs` | `footprintWidth/Height`, `disabledSkillIds`, `allowedSkillIds` 필드 |

### 주요 설계 결정

| 항목 | 선택 | 이유 |
|------|------|------|
| 데이터 저장 | JSON (`persistentDataPath/SrpData/`) | 기존 맵 IO와 일관, 런타임 편집 가능 |
| 스킬 효과 값 | 고정 피해량 | % 기반은 스킬 메이커에서 직관성 떨어짐 |
| 스킬 대상 스탯 | 드롭다운 고정 목록 | 수기 입력 오류 방지, 확장 시 배열만 수정 |
| 맵 스킬 제한 | 유닛별 `disabledSkillIds` | 전역 화이트리스트보다 세밀한 제어 |
| 풋프린트 설정 | 가로×세로 직사각형 | L자 등 복잡한 형태 불필요 |
| HUD Tooltip | EventTrigger + 재사용 팝업 | 실전 UI 패턴, Ellipsis로 패널 넘침 방지 |

### 씬 구조

| 씬 | 상수(`SrpGameSettings`) |
|----|------------------------|
| 로비 | `LobbyScene = "SrpgLobby"` |
| 전투 | `BattleScene = "SrpgBattle"` |
| 스킬 메이커 | `SkillMakerScene = "SrpgSkillMaker"` |
| 유닛 메이커 | `UnitMakerScene = "SrpgUnitMaker"` |
| 맵 메이커 | `MapMakerScene = "SrpgMapMaker"` |

---

## 현재 미해결/진행 중 사항 (Backlog)

| 우선도 | 항목 | 비고 |
|--------|------|------|
| 낮음 | AI 스텁(무작위 합법 수) | 레벨 밸런스 테스트용 |
| 낮음 | 2인 원격 세션 | 핫시트 검증 후 |

---

## 코드베이스 현황 요약

```
Assets/Scripts/SRPG/
├── SrpBattleState.cs              — 시뮬레이션 상태(그리드·유닛·점유·ZOC·스킬DB)
├── SrpCombatResolver.cs           — 전투 해석(AP·HP·PG·처단)
├── SrpDataIO.cs                   — 스킬/유닛 DB JSON IO
├── SrpDefaultMaps.cs              — 내장 맵 3종 코드 생성
├── SrpDefaultSkills.cs            — 기본 스킬 4종 시드
├── SrpDefaultUnits.cs             — 기본 유닛 3종 시드
├── SrpDevTools.cs                 — 개발자 F3 패널
├── SrpFontWarmup.cs               — TMP 한글 글리프 사전 로드
├── SrpGameController.cs           — 핵심 필드·Awake·입력·게임 흐름 (partial)
├── SrpGameController.Rendering.cs — 그리드·유닛 뷰·타일 색상 (partial)
├── SrpGameController.Hud.cs       — HUD·로그·스킬 UI·Tooltip (partial)
├── SrpGameSettings.cs             — 씬 간 설정 전달(로비↔전투↔메이커)
├── SrpLobbyController.cs          — 로비 씬 MonoBehaviour
├── SrpMapFile.cs                  — JSON 스키마 v1 (풋프린트·스킬제한 포함)
├── SrpMapIO.cs                    — 맵 저장/로드
├── SrpMapMakerController.cs       — 맵 메이커 씬 (그리드 편집·유닛 배치·카메라)
├── SrpMapPreset.cs                — 프리셋 enum
├── SrpPathfinder.cs               — 이동 탐색(다익스트라 + ZOC)
├── SrpSkillData.cs                — 스킬 정의 모델 + enum + 런타임
├── SrpSkillMakerController.cs     — 스킬 메이커 씬 (CRUD·효과 편집)
├── SrpSkills.cs                   — 데이터 기반 스킬 효과 해석
├── SrpTileView.cs                 — 타일 클릭 위임
├── SrpUnitMakerController.cs      — 유닛 메이커 씬 (스탯·스킬·풋프린트)
├── SrpUnitRuntime.cs              — 유닛 인스턴스 (스킬 런타임 포함)
└── SrpUnitTags.cs                 — Boss/Large 비트마스크
```

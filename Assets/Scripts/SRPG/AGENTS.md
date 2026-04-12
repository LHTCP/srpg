# SRPG 스크립트 — 에이전트 진입점

이 폴더의 스크립트는 **6개 도메인**으로 나뉜다. 작업 시 해당 도메인의 Cursor Rule(`.cursor/rules/srpg-*.mdc`)을 참조한다.

---

## 도메인 → 파일 맵

### 1. 전투 시스템 (`srpg-battle`)
> 게임 흐름, 입력, Phase 전환, 전투 해석, 이동 탐색

| 파일 | 한 줄 설명 |
|------|-----------|
| `SrpGameController.cs` | partial 핵심 — Awake, OnTileClicked, 턴/Undo |
| `SrpBattleState.cs` | Unity 비의존 시뮬레이션 상태 |
| `SrpCombatResolver.cs` | AP→HP→PG→그로기→처단 |
| `SrpPathfinder.cs` | Dijkstra + ZOC 가중치 |

### 2. HUD/UI (`srpg-hud`)
> 코드 생성 uGUI, 로그 패널, 버튼, TMP

| 파일 | 한 줄 설명 |
|------|-----------|
| `SrpGameController.Hud.cs` | partial — 좌·우 패널, 버튼, 스킬 슬롯, 로그 |
| `SrpFontWarmup.cs` | TMP 한글 글리프 사전 로드 |

### 3. 렌더링/뷰 (`srpg-rendering`)
> 3D 타일, 유닛 Cylinder, 카메라 프레이밍

| 파일 | 한 줄 설명 |
|------|-----------|
| `SrpGameController.Rendering.cs` | partial — 그리드·유닛 생성·색상·카메라 |
| `SrpTileView.cs` | 타일 클릭 → OnTileClicked 위임 |

### 4. 메이커 (`srpg-makers`)
> 스킬/유닛/맵 에디터 씬

| 파일 | 한 줄 설명 |
|------|-----------|
| `SrpSkillMakerController.cs` | 스킬 에디터 |
| `SrpUnitMakerController.cs` | 유닛 템플릿 에디터 |
| `SrpMapMakerController.cs` | 맵 에디터 (지형+배치) |

### 5. 데이터/IO (`srpg-data`)
> JSON 스키마, 저장/로드, 기본값, 스킬 효과

| 파일 | 한 줄 설명 |
|------|-----------|
| `SrpSkillData.cs` | 스킬/유닛 데이터 클래스 + enum |
| `SrpSkills.cs` | 스킬 효과 실행 |
| `SrpUnitRuntime.cs` | 유닛 인스턴스 |
| `SrpUnitTags.cs` | Boss/Large 비트마스크 |
| `SrpMapFile.cs` | SrpMapFileV1 스키마 |
| `SrpMapIO.cs` | 맵 JSON 저장/로드 |
| `SrpDataIO.cs` | 스킬·유닛 DB 저장/로드 |
| `SrpDefaultMaps.cs` | 내장 맵 3종 |
| `SrpDefaultUnits.cs` | 기본 유닛 템플릿 |
| `SrpDefaultSkills.cs` | 기본 스킬 정의 |
| `SrpMapPreset.cs` | 프리셋 enum |
| `SrpGameSettings.cs` | static 씬 간 설정 전달 |

### 6. 로비 (`srpg-lobby`)
> 로비 씬 UI, 맵 선택, 씬 전환

| 파일 | 한 줄 설명 |
|------|-----------|
| `SrpLobbyController.cs` | 로비 씬 MonoBehaviour |

---

## 작업 가이드

1. **단일 도메인 작업**: 해당 도메인 파일만 읽고 수정한다.
2. **도메인 경계 호출**: 다른 도메인의 public API만 사용. 내부 구조 의존 금지.
3. **partial class 주의**: `SrpGameController`는 3파일(`.cs`, `.Rendering.cs`, `.Hud.cs`)로 나뉨. 수정 파일이 속한 도메인 규칙을 따른다.
4. **새 파일 추가**: 가장 가까운 도메인에 배치하고, 이 문서와 해당 `.mdc` 규칙을 갱신한다.
5. **복수 도메인·대규모 변경**: 저장소 루트의 `.cursor/rules/srpg-dispatch.mdc`(always-apply)에 정한 **Task 위임 트리거**와 병렬 예외를 따른다.

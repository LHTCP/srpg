# SRPG 스크립트 — 에이전트 진입점 (v1)

이 폴더는 v1 프로토타입 기준으로 유지한다.

## 도메인 → 파일 맵

### 1) 전투 코어 (`srpg-battle`)

| 파일 | 설명 |
| ---- | ---- |
| `SrpGameController.cs` | 전투 입력/흐름 오케스트레이션 |
| `SrpBattleState.cs` | 시뮬레이션 상태 |
| `SrpCombatResolver.cs` | HP/PG/처단 계산 |
| `SrpPathfinder.cs` | 이동 탐색 |
| `SrpTurnOrder.cs` | 속도 기반 라운드 턴 (신규 예정) |
| `SrpReaction.cs` | RP 반응행동 처리 (신규 예정) |
| `SrpOverwatch.cs` | 경계태세 처리 (신규 예정) |
| `SrpLineOfSight.cs` | 사선 판정 (신규 예정) |

### 2) HUD (`srpg-hud`)

| 파일 | 설명 |
| ---- | ---- |
| `SrpGameController.Hud.cs` | 전투 HUD |
| `SrpFontWarmup.cs` | TMP 글리프 워밍업 |

### 3) 렌더링/뷰 (`srpg-rendering`)

| 파일 | 설명 |
| ---- | ---- |
| `SrpGameController.Rendering.cs` | 그리드/유닛 렌더링 |
| `SrpTileView.cs` | 타일 클릭 위임 |

### 4) 메이커 (`srpg-makers`)

| 파일 | 설명 |
| ---- | ---- |
| `SrpSkillMakerController.cs` | 스킬 메이커 |
| `SrpUnitMakerController.cs` | 유닛 메이커 |
| `SrpMapMakerController.cs` | 맵 메이커 |

### 5) 데이터/IO (`srpg-data`)

| 파일 | 설명 |
| ---- | ---- |
| `SrpSkillData.cs` | 스킬/유닛 데이터 모델 |
| `SrpSkills.cs` | 스킬 효과 실행 |
| `SrpUnitRuntime.cs` | 유닛 런타임 모델 |
| `SrpMapFile.cs` | 맵 스키마 |
| `SrpMapIO.cs` | 맵 저장/로드 |
| `SrpDataIO.cs` | 스킬/유닛 DB 저장/로드 |
| `SrpDefaultMaps.cs` | 기본 맵 데이터 |
| `SrpDefaultUnits.cs` | 기본 유닛 데이터 |
| `SrpDefaultSkills.cs` | 기본 스킬 데이터 |
| `SrpMapPreset.cs` | 프리셋 enum |
| `SrpGameSettings.cs` | 씬 간 설정 전달 |

### 6) 로비 (`srpg-lobby`)

| 파일 | 설명 |
| ---- | ---- |
| `SrpLobbyController.cs` | 로비 UI/전환 |

## 작업 가이드

1. 작업 전에 `docs/srpg/SRPG_프로토타입_마스터플랜.md`를 먼저 확인한다.
2. 단일 도메인 수정 원칙을 우선한다.
3. `SrpGameController`는 partial class 3파일을 함께 확인한다.
4. 신규 파일 추가 시 본 문서와 관련 문서(TDD/마스터플랜)를 함께 갱신한다.

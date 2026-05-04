# SRPG 레거시 코드 분류 (v1 전환 기준)

## 목적

v0 코드베이스에서 무엇을 버리고, 무엇을 고쳐 쓰고, 무엇을 유지할지 명확히 정의한다.

## A. Discard (폐기)

| 파일/영역 | 판단 | 근거 |
|------|------|------|
| `Assets/Scripts/Chess/` 전체 | 폐기 | v1 전환과 직접 무관한 체스 룰 전용 코드 |
| `docs/chess/` 전체 | 폐기 | 현재 프로젝트 범위 밖 문서 |
| `SrpUnitRuntime.frozenHeart` | 폐기 | 신규 기획 핵심 자원이 아님 |
| `SrpEffectType.FrozenHeart` | 폐기 | 전장 개입형 마법 설계와 충돌 |
| `SrpEffectType.Cleave` | 폐기/대체 | 무기 분류 기반 전투 공식으로 통합 필요 |
| AP(방어구) 중심 전투 해석 | 폐기 | 신규 AP는 행동자원, 의미 충돌 |

## B. Rework (대규모 수정)

| 파일 | 현재 역할 | v1 변경 방향 |
|------|------|------|
| `SrpBattleState.cs` | 플레이어 턴/점유/생존 관리 | 속도 라운드 큐 상태, 교전 고정 상태, 반응 이벤트 추적 |
| `SrpUnitRuntime.cs` | HP/AP/PG/FH 중심 런타임 | HP/PG + AP2/RP1 + 태세/방향/무기클래스/속도 |
| `SrpCombatResolver.cs` | AP 흡수 후 HP/PG 계산 | 총기/근접/마법 분기, 조건부 살상력, 방향 보정 |
| `SrpGameController.cs` | 선택 유닛 기반 입력 흐름 | 라운드 턴 오케스트레이션 + 반응행동 분기 |
| `SrpGameController.Hud.cs` | 이동/공격/스킬 HUD | 태세, AP/RP, 경계태세, 반응 선택 HUD |
| `SrpSkillData.cs` | 단순 효과 열거형 중심 | 전장 개입형 효과 타입, 반응 트리거, 스키마 v2 |
| `SrpSkills.cs` | 단일 대상 효과 중심 | 타일/위치/LOS 개입 중심으로 재구성 |
| `SrpMapFile.cs` | v1 맵 스키마(기존) | 속도/무기/태세/방향/엄폐 정보 포함 v2 |
| `SrpDefaultUnits.cs` | 기사/궁수/브루트 중심 샘플 | 사격수/마도사/탱커/근접투사 기준 샘플로 교체 |
| `SrpDefaultSkills.cs` | v0 효과셋 시드 | 전장 개입형 마법 및 반응형 기술 시드 |

## C. Keep (유지 + 확장)

| 파일 | 유지 포인트 | 확장 포인트 |
|------|------|------|
| `SrpPathfinder.cs` | 기본 이동 비용 탐색 구조 | 교전 진입/이탈 비용 규칙 반영 |
| `SrpMapIO.cs` | 저장/불러오기 파이프라인 | 스키마 v2 로드/변환 |
| `SrpDataIO.cs` | 스킬/유닛 DB IO | 신규 필드 직렬화 |
| `SrpFontWarmup.cs` | TMP 한글 워밍업 | 유지 |
| `SrpDevTools.cs` | 개발용 빠른 반복 툴 | 신규 전투 파라미터 토글 추가 가능 |
| `SrpGameSettings.cs` | 씬간 설정 전달 | 프리셋/검증 시나리오 키 확장 |
| `SrpLobbyController.cs` | 진입 흐름/맵 선택 | v1 검증 시나리오 선택 UI 추가 |
| `SrpTileView.cs` | 타일 클릭 위임 | 유지 |

## D. New (신규 도입)

| 파일(예정) | 목적 |
|------|------|
| `SrpTurnOrder.cs` | 속도 기반 라운드 순서 생성/진행 |
| `SrpStance.cs` | 공격/수비 태세 규칙 |
| `SrpFacing.cs` | 방향 및 정면/측면/후방 판정 |
| `SrpLineOfSight.cs` | LOS/엄폐 판정 |
| `SrpReaction.cs` | RP 소비 반응행동 처리 |
| `SrpOverwatch.cs` | 경계태세 상태 및 발동 |

## E. 우선순위

1. Discard 반영(체스/용어 충돌 제거)
2. 턴/자원/전투 코어 Rework
3. 태세/방향/반응 신규 모듈 도입
4. 메이커/기본 데이터 동기화

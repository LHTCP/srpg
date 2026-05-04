# SRPG AI 스텁 시뮬레이션 가이드

## 목적

- M1~M2 전투 규칙을 대량 자동 전투로 빠르게 검증한다.
- 결과를 JSON으로 저장해 회귀 여부를 수치로 판단한다.
- PlayMode 표본 재검증으로 EditMode 결과의 신뢰도를 확인한다.

## 구성 요소

- 정책 인터페이스: `Assets/Tests/Simulation/SrpAiPolicy.cs`
- 기본 정책(랜덤/휴리스틱): `Assets/Tests/Simulation/SrpAiPolicies.Basic.cs`
- 배틀 루프 러너: `Assets/Tests/Simulation/SrpBattleSimRunner.cs`
- 지표/임계치: `Assets/Tests/Simulation/SrpSimMetrics.cs`, `Assets/Tests/Simulation/SrpSimThresholds.cs`
- JSON 출력: `Assets/Tests/Simulation/SrpSimReportWriter.cs`
- EditMode 단일 엔트리: `Assets/Tests/EditMode/Editor/SrpM1AiSimAllEntry.cs`
- PlayMode 표본 검증: `Assets/Tests/PlayMode/Editor/SrpM1AiPlaySampleTests.cs`
- 메뉴 실행: `Assets/Tests/Editor/SrpAiSimMenu.cs`

## 실행 방법 (초보자용)

1. Unity에서 `Window > General > Test Runner`를 연다.
2. 메뉴 `SRPG > Run AI Simulation QA (Hybrid)`를 실행한다.
3. Console에서 아래 순서 로그를 확인한다.
   - `[SRPG][AI-Sim] 하이브리드 QA 시작 (EditMode -> PlayMode)`
   - `[SRPG][AI-Sim] EditMode 완료, PlayMode 실행 시작`
   - `[SRPG][AI-Sim] 하이브리드 QA 완료`
4. JSON 결과 파일 위치를 확인한다.
   - 기본 경로: `TestResults/SrpSim/`

## JSON 핵심 필드

- `runMeta`: 실행 메타(시드, 판수, 정책, 맵)
- `outcome`: 승률/무승부율/평균 라운드
- `combat`: 총기 HP 비중, 근접 PG 비중, 처단률
- `control`: 평균 ZOC 페널티, 태세별 공격 횟수
- `threshold`: 임계치 판정 결과(pass/warnings)
- `sampleSeeds`: PlayMode 재검증 시드

## 실패 시 대응

- 임계치 실패 시 `threshold.warnings`를 먼저 확인한다.
- 무승부율 과다이면 `maxRounds` 또는 정책 조합을 점검한다.
- 총기/근접 분기 실패면 `SrpCombatResolver`의 무기별 분배식과 태세 보정을 확인한다.
- ZOC 수치 이상이면 `SrpPathfinder.GetReachableWithCosts`의 진입 비용 계산을 확인한다.

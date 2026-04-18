# SRPG M1 QA 루틴 (Unity Test Runner 기준)

## 목적

M1 범위(속도 턴 큐, AP/RP, HP/PG, 무기 분기)가 회귀 없이 유지되는지
Unity Test Runner 실행 순서 기준으로 점검한다.

## 사전 준비

- Unity 에디터: 프로젝트 루트 `c:/workdir/srpg` 오픈
- 씬/코드 변경사항 저장 완료 (`Ctrl+S`)
- Console에서 기존 오류 정리 후 시작
- Test Runner 창 열기: `Window > General > Test Runner`
- 로비 프리셋: `M1 QA 통합 검증` 선택 상태 확인

## 한 번에 실행 (초보자 권장)

- 메뉴 실행: `SRPG > Run M1 Automated QA (Edit+Play)`
- 실행 대상
  - EditMode: `SrpM1All` 카테고리
  - PlayMode: `SrpM1All` 카테고리
- 완료 로그
  - `[SRPG][TestRunner] M1 자동화 QA 완료`

## AI 시뮬레이션 한 번에 실행 (대량 자동 검증)

- 메뉴 실행: `SRPG > Run AI Simulation QA (Hybrid)`
- 실행 대상
  - EditMode: `SrpAiSim` 카테고리 (`SrpM1AiSimAllEntry`)
  - PlayMode: `SrpAiSim` 카테고리 (`SrpM1AiPlaySampleTests`)
- 결과물
  - JSON 리포트: `TestResults/SrpSim/srpg_ai_sim_*.json`
  - 정책 매트릭스 요약: `TestResults/SrpSim/srpg_ai_sim_matrix_*.json`
  - 임계치 실패 시 테스트 실패 + 경고 메시지 출력
- 우선 확인 지표
  - `combat.firearmHpShare` (총기 HP 압박 비중)
  - `combat.meleePgShare` (근접 PG 압박 비중)
  - `control.zocPenaltyAverage` (ZOC 진입 비용 평균)

## 실행 순서 체크리스트

### 1) EditMode - M1 핵심 스모크

- [ ] Test Runner를 `EditMode` 탭으로 전환
- [ ] `SrpM1CoreTests`만 선택 실행
- [ ] 아래 2개 테스트 통과 확인
  - [ ] `TurnOrder_UsesSpeedDescending`
  - [ ] `CombatSplit_FirearmAndMeleeProduceDifferentPressure`
- [ ] `SrpM1RuleSpecTests` 실행 및 아래 규칙 테스트 통과 확인
  - [ ] `ZocPenalty_IncreasesMoveCost_WhenEnemyAdjacent`
  - [ ] `Stance_Aggressive_IncreasesPgPressure`
  - [ ] `Stance_Defensive_ReducesIncomingDamage`
  - [ ] `Execution_Triggers_WhenDefenderPgZeroOrGroggy`

### 2) EditMode - 전체 회귀

- [ ] `EditMode` 전체 테스트 실행 (`Run All`)
- [ ] 실패 테스트 0건 확인
- [ ] 실패 시 우선 원인 분류
  - [ ] 턴 큐/속도 정렬
  - [ ] 전투 해석(HP/PG/무기 분기)
  - [ ] 데이터 스키마/초기화

### 3) PlayMode - 전투 루프 스모크(수동)

- [ ] `SrpgLobby`에서 `M1 QA 통합 검증` 선택 후 `전투 시작`
- [ ] 4유닛 교전에서 속도 순서대로 턴 순환 확인
- [ ] 동일 전장/동일 조건에서 총기와 근접 결과 차이 확인
  - [ ] 총기: HP 압박 우세
  - [ ] 근접: PG 압박 우세
- [ ] HUD에서 라운드/현재 유닛/대기 큐/AP/RP/PG 표시 확인
- [ ] HUD 상태 문구가 페이즈별로 명확히 바뀌는지 확인 (행동 단계/스킬 대상 선택/되감기 안내)
- [ ] 공격 가능 시 상태 패널에 `공격 후 턴 종료` 안내 표시 확인
- [ ] 무효 클릭 시 피드백 로그가 출력되는지 확인
- [ ] AP 0 상태에서 스킬 버튼이 비활성화되는지 확인
- [ ] AP 0 상태에서 스킬 타깃 클릭 시 즉시 `행동 단계`로 복귀하고 입력 잠김이 없는지 확인
- [ ] `위험영역 보기` 토글 ON/OFF 시 위험 타일(공격/ZOC) 표시가 즉시 반영되는지 확인
- [ ] 이동 가능 칸 hover 시 상태 패널에 위험도(안전/위험/ZOC/진입불가) 안내가 표시되는지 확인
- [ ] 유닛 hover 시 해당 유닛 공격범위/ZOC 프리뷰가 시각적으로 표시되는지 확인
- [ ] 강제 턴 종료를 눌렀을 때 스킬 대상 선택 중이어도 즉시 탈출되는지 확인

### 3-1) AI 자동 검증 (권장)

- [ ] `SRPG > Run AI Simulation QA (Hybrid)` 실행
- [ ] EditMode `SrpM1AiSimAllEntry` 통과 확인
- [ ] EditMode `Run_M1_Ai_Policy_Matrix_Comparison` 통과 확인
- [ ] PlayMode `SrpM1AiPlaySampleTests` 통과 확인
- [ ] `TestResults/SrpSim/` JSON 생성 확인
- [ ] `srpg_ai_sim_matrix_*.json`에서 4조합 결과 확인
- [ ] `Random_vs_Random`은 경향 관찰용(warn-only) 케이스로 확인 (게이트 실패 제외)
- [ ] 임계치 경고(`threshold.warnings`) 0건 또는 허용 범위 확인

### 4) 결과 기록

- [ ] `docs/srpg/SRPG_CHANGELOG.md`에 QA 결과 기록
- [ ] `docs/srpg/SRPG_GDD_TEST_TRACEABILITY.md` 커버 상태 갱신
- [ ] 실패 항목은 `docs/srpg/SRPG_BACKLOG.md`에 후속 작업으로 등록
- [ ] Notion 허브/개발자 페이지에 상태 동기화

## 실패 시 확인 순서 (자주 나는 오류 3개)

1. 테스트가 안 보임
- `Assets/Tests/EditMode/Editor`, `Assets/Tests/PlayMode` 경로 확인
- Test Runner 필터/검색어 초기화 후 다시 조회

2. PlayMode 테스트가 시작되지 않음
- Console 에러 먼저 0건으로 정리
- `SRPG > Run M1 Automated QA (Edit+Play)` 메뉴를 다시 실행

3. HUD assert 실패
- `SrpGameController`가 실제로 생성되었는지 확인
- `SrpGameController.Hud.cs`에서 `TestHudReady`가 true인지 확인
- 1~2프레임 대기 후 텍스트 assert하도록 테스트 유지

## 완료 기준 (M1 QA Pass)

- EditMode 핵심 + 전체 테스트 통과
- AI 하이브리드 시뮬레이션 통과 + JSON 리포트 생성
- 수동 스모크에서 아래 2개 재현
  - 속도 기반 턴 큐 순환
  - 총기/근접 결과 차이
- 기록 문서(CHANGELOG/BACKLOG/Notion) 동기화 완료

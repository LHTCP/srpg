# SRPG v1 프로토타입 실행 가이드

## 개요

현재 프로토타입 목표는 v1 전투 루프 검증이다.

- 속도 기반 라운드 턴
- AP 2 + RP 1
- HP/PG 이중 자원
- 태세/방향/교전/반응행동

상세 규칙은 `SRPG_GDD.md`, 기술 구현 기준은 `SRPG_TDD.md`를 따른다.

## 진행 상태 (2026-04-18)

- M0: 완료 (체스 코드/문서 제거, 문서 세트 v1 정합 완료)
- M1: 확장 진행 중 (코어 + 안정화 + UX 1차)
  - 속도 기반 라운드 턴 큐 적용
  - AP 2 / RP 1 자원 모델 적용
  - 총기/근접/마법 분기 + HP/PG 이원화 전투 해석 적용
  - EditMode 테스트 `Assets/Tests/EditMode/Editor/SrpM1CoreTests.cs` 추가
  - 기본 내장 프리셋을 `M1 QA 통합 검증` 단일 체계로 전면 재구성
  - 전투 UX 1차 확장: 위험영역 토글, 타일/유닛 hover 프리뷰, 경량 intent 텔레그래프
- M2~M4: 미착수 (태세/방향 심화, 교전 고정, 반응/경계/LOS, 마법 전장 개입 확장 예정)

## 씬 구성

- `SrpgLobby`
- `SrpgBattle`
- `SrpgSkillMaker`
- `SrpgUnitMaker`
- `SrpgMapMaker`

## 기본 조작 (v1 기준)

1. 행동 유닛 선택
2. AP를 사용해 이동/공격/스킬/경계태세 수행
3. 필요 시 태세 전환
4. 적 턴에서 RP로 반응행동 수행
5. 라운드 큐에 따라 다음 유닛으로 진행

## UI에서 확인해야 할 핵심 정보

- 현재 라운드/행동 순서
- 선택 유닛 AP/RP
- 현재 태세(공격/수비)
- 방향(정면 기준)
- 교전 상태
- 경계태세 활성 여부

## 전투 화면 UI 개선 기준 (M1)

- 대기 큐 프리뷰를 확대해 다음 행동 유닛 흐름을 더 길게 확인 가능
- 상태 패널에서 행동 단계/스킬 대상 선택/되감기 후 안내를 명확히 분리
- 유닛 패널에 AP/RP/PG와 함께 태세/방향/그로기 정보를 상시 노출
- 공격 가능 상태에는 `공격 후 턴 종료` 안내를 고정 노출
- 무효 클릭 시 짧은 피드백 로그를 출력해 입력 실수를 빠르게 교정

## 전투 UX 확장(현재 반영분)

- `위험영역 보기/숨기기` 버튼으로 적 공격 범위 및 ZOC를 상시 오버레이 확인 가능
- 이동 가능 타일 hover 시 상태 패널에 `안전/위험/ZOC/진입불가` 정보 표시
- 유닛 hover 시 해당 유닛의 공격범위 및 ZOC 미리보기 표시
- 적 의도(intent)는 경량 휴리스틱 기반 `예상` 경로/타깃 표시(확정 행동 아님)
- `강제 턴 종료`는 스킬 타깃 선택 중에도 즉시 탈출 가능
- `되감기`는 행동 확정 스냅샷이 있을 때만 가능(선택/hover 단계는 스냅샷 미생성)

## 프로토타입 플레이 체크리스트

- 속도 순서가 의도대로 순환하는가
- AP/RP 소모가 직관적인가
- 총기/근접/마법 역할 차이가 체감되는가
- 탱커의 교전 고정이 작동하는가
- 근접 투사의 PG 연쇄 붕괴가 가능한가

## 관련 문서

- `SRPG_프로토타입_마스터플랜.md`
- `SRPG_GDD.md`
- `SRPG_TDD.md`
- `SRPG_BACKLOG.md`
- `SRPG_CHANGELOG.md`
- `SRPG_M1_QA_TEST_RUNNER_CHECKLIST.md`
- `SRPG_AI_SIMULATION_GUIDE.md`
- `SRPG_GDD_TEST_TRACEABILITY.md`
- `SRPG_레거시_코드_분류.md`
- `SRPG_다음미팅_논의사항.md`

## M1 자동화 실행(초보자용)

- 메뉴: `SRPG > Run M1 Automated QA (Edit+Play)`
- 단일 카테고리: `SrpM1All`
- 핵심 테스트
  - EditMode: `SrpM1AllTestsEntry`, `SrpM1CoreTests`, `SrpM1RuleSpecTests`
  - PlayMode: `SrpM1PlayModeTests`

## AI 스텁 시뮬레이션 실행(하이브리드)

- 메뉴: `SRPG > Run AI Simulation QA (Hybrid)`
- 단일 카테고리: `SrpAiSim`
- 핵심 테스트
  - EditMode: `SrpM1AiSimAllEntry` (대량 500판 + 임계치 + JSON 저장)
  - EditMode: `SrpM1AiSimAllEntry.Run_M1_Ai_Policy_Matrix_Comparison` (정책 4조합 비교)
  - PlayMode: `SrpM1AiPlaySampleTests` (표본 시드 재검증)
- 결과 경로: `TestResults/SrpSim/srpg_ai_sim_*.json`
  - 매트릭스 요약: `TestResults/SrpSim/srpg_ai_sim_matrix_*.json`

# SRPG GDD-테스트 추적 매핑표 (M1)

## 목적

- `SRPG_GDD.md`의 의도 문장을 테스트 케이스와 1:1로 연결해, 자동화 검증 범위를 명시한다.
- 회귀 발생 시 어떤 규칙이 깨졌는지 빠르게 역추적한다.

## 범례

- 검증 방식
  - `단위`: 고정 입력/고정 출력 assert
  - `통합`: 런타임/HUD/턴 흐름 통합 확인
  - `통계`: 대량 시뮬레이션 지표 및 임계치 판정
- 상태
  - `완전`: 기획 의도 핵심 조건이 자동화 assert로 직접 검증됨
  - `부분`: 간접 지표 또는 일부 시나리오만 검증됨
  - `미커버`: 현재 자동화 없음

## GDD 항목별 매핑

| GDD 항목 | 의도 문장 요약 | 커버 테스트 | 검증 방식 | 상태 | 다음 액션 |
|---|---|---|---|---|---|
| 턴 구조(속도 기반) | 속도 내림차순 라운드 순환 | `SrpM1CoreTests.TurnOrder_UsesSpeedDescending`, `SrpM1CoreTests.TurnOrder_UsesOwnerAndIdAsTieBreaker_WhenSpeedSame`, `SrpM1PlayModeTests.M1IntegratedPreset_InitializesRoundAndHud` | 단위/통합 | 완전 | 라운드 중 동적 속도 변경 규칙 도입 시 재검증 |
| 자원(AP/RP) 가시성 | AP/RP가 HUD에 표시되고 행동 루프에서 소비 | `SrpM1PlayModeTests.M1IntegratedPreset_InitializesRoundAndHud` | 통합 | 부분 | RP 소비 트리거(반응행동) 자동화 테스트 추가 |
| 총기/근접 역할 분담 | 총기 HP 압박, 근접 PG 압박 | `SrpM1CoreTests.CombatSplit_FirearmAndMeleeProduceDifferentPressure`, `SrpM1AiSimAllEntry.Run_M1_Hybrid_Ai_Simulation_And_Validate_Thresholds`, `Run_M1_Ai_Policy_Matrix_Comparison` | 단위/통계 | 완전 | `Random_vs_Random`은 warn-only로 분리, 게이트는 휴리스틱 포함 조합 기준 유지 |
| 태세 효용(공격/수비) | 공격 태세 PG 압박 강화, 수비 태세 피해 감소 | `SrpM1RuleSpecTests.Stance_Aggressive_IncreasesPgPressure`, `SrpM1RuleSpecTests.Stance_Defensive_ReducesIncomingDamage` | 단위 | 완전 | 태세 전환 비용/타이밍 규칙 테스트 추가 |
| 전투 정보 가시성(UI) | 위험 영역/행동 상태를 플레이 중 즉시 해석 가능 | `SrpM1PlayModeTests.DangerAreaAndHoverPreview_UpdatesStatusText` | 통합 | 부분 | 타일 렌더 결과(색상/윤곽선)까지 검증하는 시각 테스트 또는 스냅샷 테스트 추가 |
| 처단 조건 | PG 0/그로기에서 처단 위험 증가 | `SrpM1RuleSpecTests.Execution_Triggers_WhenDefenderPgZeroOrGroggy` | 단위 | 완전 | 처단 후 상태 전이(사망/생존 경계) 케이스 확장 |
| ZOC 이동 압박 | 적 인접 칸 진입 시 이동 부담 증가 | `SrpM1RuleSpecTests.ZocPenalty_IncreasesMoveCost_WhenEnemyAdjacent`, `SrpM1AiSimAllEntry`의 `zocPenaltyAverage` | 단위/통계 | 완전 | 강제 이탈/기회공격 규칙이 도입되면 별도 항목으로 분리 |
| 마법 전장 개입 | 재배치/제어 중심 역할 | (현재 없음) | - | 미커버 | M2에서 스킬 효과 단위/통합 테스트 추가 |
| 교전 고정(탱커) | 근접 고정 성능 | (현재 없음) | - | 미커버 | 교전 유지/이탈 실패 조건 테스트 추가 |
| 반응행동/RP 사용 | 적 턴 반응행동 소비 | (현재 없음) | - | 미커버 | 반응행동 트리거/소비 테스트 추가 |

## 운용 규칙

1. 새 규칙 구현 시 먼저 이 문서에 목표 상태(`완전/부분/미커버`)를 갱신한다.
2. 테스트가 추가되면 해당 행의 `커버 테스트`와 `상태`를 즉시 업데이트한다.
3. 릴리즈 전 `SrpM1All` + `SrpAiSim` 실행 결과와 함께 이 문서를 점검한다.

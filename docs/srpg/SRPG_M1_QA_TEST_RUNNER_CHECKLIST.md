# SRPG v2 QA 체크리스트 (문서 정렬 기준)

## 목적

문서 1차 개편 이후 기준서(v2)와 테스트/운영 문서가 충돌하지 않는지 점검한다.

## 사전 확인

- 기준 문서:
  - `SRPG_전투규칙_기준서_v2.md`
  - `new/SRPG_NEW_DIALOG_POLICY_LOCK.md`
- 동기화 대상:
  - `SRPG_GDD.md`
  - `SRPG_TDD.md`
  - `SRPG_GDD_TEST_TRACEABILITY.md`
  - `SRPG_BACKLOG.md`
  - `SRPG_CHANGELOG.md`

## 문서 QA 체크

- [ ] AP 2 / RP 1 정책이 문서마다 동일하게 적혀 있는가
- [ ] 패링이 주인공 전용 + 강공/스킬 조건으로 일치하는가
- [ ] 전 유닛 RP2 비채택 정책이 일치하는가
- [ ] 스킬 자원이 쿨다운/충전 중심으로 통일되어 있는가
- [ ] 안정도 오버클럭 설명이 동일한가
- [ ] 미정 항목(TBD)이 확정처럼 쓰이지 않았는가

## 자동화 QA 체크 (기존 테스트 유지)

- [ ] `SrpM1CoreTests` 통과
- [ ] `SrpM1RuleSpecTests` 통과
- [ ] `SrpM1PlayModeTests` 통과
- [ ] `SrpM1AiSimAllEntry` 및 매트릭스 비교 테스트 통과

## 결과 기록

- [ ] `SRPG_CHANGELOG.md`에 문서 개편 이력 반영
- [ ] `SRPG_GDD_TEST_TRACEABILITY.md` 상태 반영
- [ ] 신규 미커버 항목을 `SRPG_BACKLOG.md`에 등록

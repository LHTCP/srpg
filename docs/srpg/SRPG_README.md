# SRPG v2 문서 우선 실행 가이드

## 개요

현재 기준은 `신규 대화 원본(06~23)` 기반 v2 규칙이다.

핵심 규칙 축:

- 속도 기반 라운드 턴
- AP 2 / RP 1
- GRD(PG 감쇠) + RP 반응행동 + 엄폐/위치/특성 방어
- 총기 HP 직접 위협 + 실제 HP 피해량 50%의 PG 파급(v0.2 기준, 조정 가능)
- 공격/수비 2태세
- 주인공 전용 패링(강공/스킬 카운터)
- 공용 전투 태그(`표식`, `균형 붕괴`, `사살 지시`)
- 쿨다운/충전 기반 스킬 자원

## 우선 참조 문서

1. `README.md`
2. `SRPG_전투규칙_기준서_v2.md`
3. `new/SRPG_NEW_DIALOG_POLICY_LOCK.md`
4. `SRPG_GDD.md`
5. `SRPG_TDD.md`
6. `SRPG_BACKLOG.md`
7. `SRPG_PHASE2_CODE_BACKLOG.md`

## 현재 작업 상태

- 1차: 신규 대화/기획서 기반 문서 전면 개편 완료
- 2차: 전투 코어 1차 스프린트 완료, 11~23 대화 정책 반영 문서 정렬 완료
  - 완료: AP/RP 라운드 리셋, 교전 상태/클론, 기존 DEF/GRD + Guard 반응 브릿지, 교전 이탈 비용 브릿지, 교전 이탈 기회공격 1차 구현, 쿨다운/충전/오버클럭 기본 모델, 패링 조건/텔레그래프 1차 구현, Dodge/Parry/명시형 ReactionShot 브릿지, 수비 지속 완충/탱커 다중 대응 브릿지, 메이커 메타데이터 UI 확장, 스킬/맵 데이터 정합성 보정, 유닛 시각 방향성 개선, 교전/둘러싸임 검증 프리셋 보강, RP/HUD 노출 정책 정리, 기획 대조 P1 보정, HUD/로그 가독성 동기화, 오버워치 사선/횟수/해제 상세 규칙, 테스트 프리셋 v2 + HUD 레이아웃 개편, 전투 직접 조작 UI 보강, 오버클럭 성능 증폭, 재장전 AP 행동 1차 구현, 엄폐 AP 행동 1차 구현, 상호작용 AP 행동 1차 구현, 개발용 전술 HUD 개선, 총기 1발 고화력 + 방향성 엄폐 설계, 방향성 엄폐 1차 구현, 11~23 대화 정책 잠금/문서 정렬
  - 검증: Unity EditMode `64 passed / 0 failed`, PlayMode `5 passed / 0 failed`
- 3차: 전투 플레이 가능성 P1 확장 완료
  - 완료: 총기 HP-PG 파급 최종 HP 기준 보정, 공용 전투 태그 런타임 계약, 패링 성공 보상, `완벽한 수비` 1차 구현, 태그 대표 스킬/프리셋 노출
  - 검증: Unity EditMode `71 passed / 0 failed`, PlayMode `5 passed / 0 failed`
- 4차: 다음 P1 초기 4인/방향성 엄폐/오버워치/마법 브릿지 완료
  - 완료: 초기 4인 고유 패시브 데이터, `M1QaIntegrated` 4인 역할 검증, `blocksLineOfSight` 사선 차단, 오버워치 후보 우선순위, 마법 전장 개입 스킬 `전장 장막`
  - 검증: Unity EditMode `75 passed / 0 failed`, PlayMode `5 passed / 0 failed`
  - 다음 후보: 맵 메이커 엄폐 segment 편집 UI, 초기 4인 전직 연계/최종 수치, 특수 지형 복합 상호작용
- 5차: 첫 전투 프로토타입 프리셋 분리 완료
  - 완료: `M1QaIntegrated`는 기능 QA 맵으로 유지, `M1OpeningPrototype`을 첫 전투 판단용 내장 프리셋으로 추가
  - 완료: 북쪽 사격 루트/남쪽 돌입 루트, 사선 차단 엄폐, 상호작용 포인트 1개, 비대칭 적 역할 구성
  - 완료: 기본 전투 진입값, `SrpgBattle` 씬 직접 실행 기본값, 로비 첫 선택을 `M1OpeningPrototype`으로 교체, `M1QaIntegrated`는 로비 후순위 QA 선택지와 코드/테스트용 deprecated 프리셋으로 유지
  - 검증: Unity EditMode `76 passed / 0 failed`, PlayMode `6 passed / 0 failed`
  - 다음 후보: 첫 전투 실제 플레이/AI 시뮬레이션 후 적 수, 배치 간격, 태그/패링/총기 파급 수치 보정

## 작업 처리 기준

- 작업 시작 전 공통 계약은 `docs/project/work-contract.md`를 따른다.
- 현재 실행 플랜은 `SRPG_BACKLOG.md`의 다음 스프린트와 `SRPG_PHASE2_CODE_BACKLOG.md`의 파일 단위 항목을 우선한다.
- `SRPG_프로토타입_마스터플랜.md`는 문서 우선 개편과 코드 2차 착수 준비 기준으로 유지한다.
- 구현 중 임시 고정한 브릿지 수치와 다음 의사결정 후보는 `SRPG_IMPLEMENTATION_DECISIONS.md`에 둔다.
- 과거 원문, 변환본, v1 미팅 메모는 현재 기준서와 백로그를 대체하지 않는다.

## 씬 구성

- `SrpgLobby`
- `SrpgBattle`
- `SrpgSkillMaker`
- `SrpgUnitMaker`
- `SrpgMapMaker`

## v2 플레이 확인 포인트

- `M1OpeningPrototype`: 기본 로비 첫 선택 맵. 한 판의 전술 판단, 역할 체감, 적 비대칭 구성, 루트 선택을 확인한다.
- `M1QaIntegrated`: deprecated 기능 QA 맵. 로비의 후순위 QA 선택지로 유지하고 최신 전투 규칙, HUD, 스킬 자원, 사선 차단, 상호작용 연결 회귀 확인에도 사용한다.
- 공격 태세가 고위험 진입 보조로 체감되는가
- 수비 태세가 만능이 아닌 안정 생존으로 체감되는가
- 패링 텔레그래프를 보고 의사결정 가능한가
- 패링 성공이 공격 무효 외에도 PG 피해/`균형 붕괴` 기회로 체감되는가
- 탱커가 RP2 없이도 둘러싸임 상황을 버티는가
- `노출`이 디버프가 아니라 엄폐/사선/포지션 상태로 이해되는가
- 쿨다운/충전 스킬 UI가 과밀하지 않은가
- `M1QaIntegrated`에서 주인공/탱커/사격수/마도사 고유 패시브와 대표 스킬이 구분되는가
- `M1OpeningPrototype`에서 총기 사선 압박, 엄폐, 사선 차단, 오버워치 위험, 근접 돌입, PG 붕괴/처단 흐름이 한 전투 안에서 드러나는가
- 사선 차단 방향성 엄폐가 오버워치/총기 기본 공격을 납득 가능하게 막는가

## 자동화/QA 문서

- `SRPG_M1_QA_TEST_RUNNER_CHECKLIST.md`
- `SRPG_GDD_TEST_TRACEABILITY.md`
- `SRPG_CHANGELOG.md`

## 관련 문서

- `SRPG_BACKLOG.md`
- `SRPG_PHASE2_CODE_BACKLOG.md`
- `SRPG_IMPLEMENTATION_DECISIONS.md`
- `README.md`
- `SRPG_AI_SIMULATION_GUIDE.md`
- `SRPG_레거시_코드_분류.md`

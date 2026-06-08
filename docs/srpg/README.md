# SRPG 문서 맵

이 폴더는 SRPG v2 프로토타입의 기획, 기술 설계, 실행 백로그, QA 기록을 관리한다. 현재 개발 기준은 신규 대화 원본 06~23을 반영한 v2 규칙이다.

## 먼저 읽을 문서

| 순서 | 문서 | 역할 |
| --- | --- | --- |
| 1 | [SRPG_README.md](SRPG_README.md) | 현재 상태와 실행/검증 진입점 |
| 2 | [SRPG_전투규칙_기준서_v2.md](SRPG_전투규칙_기준서_v2.md) | 단일 전투 규칙 기준서 |
| 3 | [new/SRPG_NEW_DIALOG_POLICY_LOCK.md](new/SRPG_NEW_DIALOG_POLICY_LOCK.md) | 확정 규칙(`RQ-*`)과 미정 항목(`TBD-*`) 잠금표 |
| 4 | [SRPG_BACKLOG.md](SRPG_BACKLOG.md) | 지금 진행할 스프린트와 후속 과제 |
| 5 | [SRPG_PHASE2_CODE_BACKLOG.md](SRPG_PHASE2_CODE_BACKLOG.md) | 파일 단위 코드 착수 계획 |

## 문서 계층

| 계층 | 문서 | 사용 기준 |
| --- | --- | --- |
| 기준 | `SRPG_전투규칙_기준서_v2.md`, `new/SRPG_NEW_DIALOG_POLICY_LOCK.md` | 규칙 충돌 시 최우선으로 따른다. |
| 설계 | `SRPG_GDD.md`, `SRPG_TDD.md` | 기준 규칙을 게임/기술 계약으로 풀어 쓴다. |
| 실행 | `SRPG_BACKLOG.md`, `SRPG_PHASE2_CODE_BACKLOG.md` | 다음 구현 순서와 파일별 작업 단위를 관리한다. |
| 검증 | `SRPG_GDD_TEST_TRACEABILITY.md`, `SRPG_M1_QA_TEST_RUNNER_CHECKLIST.md`, `SRPG_AI_SIMULATION_GUIDE.md` | 테스트 커버, QA, 시뮬레이션 확인에 사용한다. |
| 의사결정 | `SRPG_IMPLEMENTATION_DECISIONS.md` | 구현 브릿지와 다음 목표 의사결정 후보를 관리한다. |
| 기록 | `SRPG_CHANGELOG.md`, `SRPG_V0_ARCHIVE.md`, `SRPG_레거시_코드_분류.md` | 완료 이력과 과거 기준을 확인한다. |
| 원문 | `new/*.txt`, `new/*.md`, PDF/DOCX 변환본 | 기준서가 놓친 맥락 확인에만 사용한다. |

## 현재 실행 플랜

현재 다음 스프린트는 `SRPG_BACKLOG.md`의 "후속 스프린트 (밸런스/검증, P2)"와 `SRPG_IMPLEMENTATION_DECISIONS.md`의 2026-06-03 의사결정 후보를 따른다. 이미 구현한 항목의 브릿지 수치와 다음 의사결정은 `SRPG_IMPLEMENTATION_DECISIONS.md`를 확인한다.

1. 초기 4인 고유 패시브/대표 스킬 최종 이름, 전직 연계, 밸런스 수치 확정
2. 맵 메이커 방향성 엄폐 segment 편집 UI
3. 특수 지형 상호작용의 복합 효과와 승리 조건 연동
4. 공용 전투 태그/패링/총기 파급 브릿지 수치 밸런스 검증
5. `M1OpeningPrototype` 후속 실플레이 표본으로 북쪽 사격 루트/남쪽 돌입 루트 선택 가치와 화면 가독성 검증
6. 총기 발포 방향/조준 문법 재정의: 기본 공격/오버워치/발포 연출이 모두 8방향 고정처럼 보이는 문제를 P2에서 검토
7. 행동 순서 패널 분리: 상단 HUD와 별도 initiative/turn order tracker의 정보 경계를 P2에서 확정
8. 타일 overlay 시각 문법 개편: 이동/공격/ZOC/오버워치/패링/상호작용 표시를 중심 원/외곽 danger/ring 계열로 재검토
9. 메이커 화면 UX: 효과유형 드롭다운 지연 재현과 필드 의미 툴팁은 P3로 유지

## 내장 전투 프리셋 역할

- `M1OpeningPrototype`: 기본 로비 첫 선택 맵. 새 시스템을 늘리지 않고 현재 전투 규칙 조합만으로 한 판의 전술 문제를 판단한다.
- `M1EngagementLab`: 교전/둘러싸임 QA 맵. 탱커 다중 교전, 교전 이탈 비용, 기회공격을 고정 조건에서 확인한다.
- `M1QaIntegrated`: deprecated 기능 QA 맵. 로비의 후순위 QA 선택지로 유지하고 코드/자동 테스트 회귀 확인에도 사용한다.

## 작업 처리 규칙

- 작업 전에는 [../project/work-contract.md](../project/work-contract.md)를 확인한다.
- 새 규칙 구현은 `RQ-*` 또는 `TBD-*`와 연결한다.
- 문서가 충돌하면 기준서와 잠금표를 우선하고, 백로그/추적표를 함께 갱신한다.
- 과거 문서나 원문 변환본은 현재 기준을 대체하지 않는다.
- 문서만 변경한 경우에는 Unity 테스트 대신 링크와 진입점 정합성을 확인한다.

## 주의 문서

- [SRPG_다음미팅_논의사항.md](SRPG_다음미팅_논의사항.md)는 v1 시절 의사결정 메모다. 현재 구현 우선순위는 `SRPG_BACKLOG.md`와 `SRPG_PHASE2_CODE_BACKLOG.md`를 따른다.
- [SRPG_V0_ARCHIVE.md](SRPG_V0_ARCHIVE.md)는 v0 기준 문서의 종료 안내다.

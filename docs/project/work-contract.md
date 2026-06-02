# 작업 처리 계약

이 문서는 사람과 에이전트가 같은 기준으로 작업을 시작하고 마무리하기 위한 공통 계약이다. Git 운영 세부 규칙은 [workflow.md](workflow.md), 문서 위치 규칙은 [documentation-standards.md](documentation-standards.md)를 따른다.

## 1. 작업 시작 전

1. 요청 범위를 한 문장으로 정리한다.
2. 적용 도메인을 확인한다.
   - 공통/운영: `docs/project/`
   - SRPG 기획/전투/데이터/HUD/QA: `docs/srpg/`
   - SRPG 코드: `Assets/Scripts/SRPG/`
3. 상위 기준 문서를 먼저 확인한다.
   - 공통 작업: `AGENTS.md`, `docs/README.md`, 이 문서
   - SRPG 작업: `docs/srpg/README.md`, `docs/srpg/SRPG_전투규칙_기준서_v2.md`, `docs/srpg/SRPG_BACKLOG.md`
4. 미정 항목(`TBD-*`)이나 정책 충돌이 있으면 구현 전에 작업 범위와 가정을 짧게 남긴다.

## 2. 문서 우선순위

문서가 충돌하면 아래 순서로 판단한다.

1. 루트 `AGENTS.md`와 `.cursor/rules/*.mdc`
2. 도메인 진입점: `docs/README.md`, `docs/srpg/README.md`, `Assets/Scripts/SRPG/AGENTS.md`
3. 상위 기준서: `SRPG_전투규칙_기준서_v2.md`, `SRPG_NEW_DIALOG_POLICY_LOCK.md`
4. 설계 문서: `SRPG_GDD.md`, `SRPG_TDD.md`
5. 실행 문서: `SRPG_BACKLOG.md`, `SRPG_PHASE2_CODE_BACKLOG.md`, QA/Traceability/Changelog
6. 원문, 변환본, 과거 회의 메모, 아카이브 문서

과거 문서의 내용이 현재 기준과 다르면 과거 문서를 근거로 코드를 바꾸지 않는다. 필요한 경우 현재 기준 문서나 백로그에 먼저 반영한다.

## 3. 구현 계약

- 변경은 요청 범위에 필요한 최소 단위로 제한한다.
- SRPG 코드 작업은 `Assets/Scripts/SRPG/AGENTS.md`의 도메인 맵을 따른다.
- `SrpGameController` 작업은 `.cs`, `.Hud.cs`, `.Rendering.cs` 영향을 함께 확인한다.
- `SrpBattleState`는 Unity 타입 의존 없이 유지한다.
- 공개 API, 저장 스키마, 인스펙터 필드, 씬/프리팹 레퍼런스가 바뀌면 관련 문서와 확인 항목을 같이 남긴다.
- 새 규칙을 구현할 때는 요구사항 ID(`RQ-*`, `TBD-*`)를 백로그나 추적표와 연결한다.

## 4. 문서 갱신 계약

다음 변경은 문서 갱신 대상이다.

- 전투 규칙, 수치 정책, 요구사항 ID 상태 변경
- 데이터 스키마, 저장/로드 호환성, 기본 프리셋 변경
- HUD/로그/오버레이처럼 플레이어가 보는 용어 변경
- 테스트 수, 커버 상태, QA 절차 변경
- 다음 착수 후보나 우선순위 변경

갱신 위치는 다음을 기본값으로 한다.

| 변경 유형 | 우선 갱신 문서 |
| --- | --- |
| 전투 규칙 확정 | `SRPG_전투규칙_기준서_v2.md` |
| 구현 순서/남은 일 | `SRPG_BACKLOG.md` |
| 파일 단위 작업 | `SRPG_PHASE2_CODE_BACKLOG.md` |
| 기술 계약 | `SRPG_TDD.md` |
| 테스트 커버 | `SRPG_GDD_TEST_TRACEABILITY.md` |
| 완료 이력 | `SRPG_CHANGELOG.md` |
| 문서 구조/진입점 | `docs/README.md`, `docs/srpg/README.md`, `AGENTS.md` |

## 5. 완료 기준

작업 완료 보고에는 가능한 범위에서 다음을 포함한다.

- 변경한 핵심 파일
- 검증한 명령 또는 검증하지 못한 이유
- 씬, 프리팹, 인스펙터 확인이 필요한 경우의 체크리스트
- 후속으로 남긴 `TBD-*`나 백로그 항목

문서만 바꾼 작업은 Unity 테스트가 항상 필요하지 않다. 대신 링크, 문서 우선순위, 현재/과거 문서 경계가 맞는지 확인한다.

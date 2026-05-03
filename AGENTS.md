# AGENTS — 프로젝트 진입점

## 한 줄 요약

Unity 6 기반 전술/SRPG 전환 중인 프로젝트. **주 플로우는 SRPG** (`Assets/Scripts/SRPG/`). 레거시 체스는 `Assets/Scripts/Chess/`에 참고용으로 남아 있을 수 있다. **문서는 전부 [`docs/`](docs/README.md) 아래** (범주별 폴더).

## 환경

- **Unity 에디터**: 6000.3.13f1 (`ProjectSettings/ProjectVersion.txt` 기준)
- **실행**: Unity에서 프로젝트를 연 뒤 씬을 열고 Play. 체스 씬 설정은 [docs/chess/Unity_설정가이드.md](docs/chess/Unity_설정가이드.md), SRPG는 [docs/srpg/SRPG_README.md](docs/srpg/SRPG_README.md).
- **공통 개발 기준**: [docs/project/setup.md](docs/project/setup.md), [docs/project/workflow.md](docs/project/workflow.md), [docs/project/worktrees.md](docs/project/worktrees.md)
- **로컬 취향 설정(선택)**: `project.local.json`이 있으면 공통 규칙과 충돌하지 않는 범위에서 참고

## 하위 에이전트

SRPG 코드는 6개 도메인(전투·HUD·렌더링·메이커·데이터·로비)으로 분리돼 있다. 디스패치 규칙(`.cursor/rules/srpg-dispatch.mdc`, always-apply)에 따라 **메인 에이전트가** 도메인을 나누고, 조건에 맞으면 **Task** 하위 에이전트로 위임한다 (엔진이 자동 분기하지는 않는다). 도메인별 상세 규칙은 `.cursor/rules/srpg-{도메인}.mdc`, 파일 맵은 [`Assets/Scripts/SRPG/AGENTS.md`](Assets/Scripts/SRPG/AGENTS.md) 참조.

## 에이전트 작업 시

1. 요청 범위에 맞게 **최소 변경**으로 수정한다 (전역 규칙: `.cursor/rules/project-core.mdc`).
2. 구현 전에 가정·모호함·트레이드오프가 있으면 먼저 드러낸다.
3. 작업 완료 기준을 검증 가능한 형태로 잡고, 가능한 범위에서 확인한다.
4. 인스펙터 할당·씬 레퍼런스가 바뀔 수 있으면, 사용자에게 확인할 항목을 짧은 체크리스트로 남긴다.
5. **새·수정 문서**는 `Assets/`가 아니라 `docs/<범주>/`에 둔다 (`.cursor/rules/documentation.mdc`).

## 문서·규칙 링크

| 목적 | 위치 |
|------|------|
| 문서 목록·맵 | [docs/README.md](docs/README.md) |
| 개발 환경·재현성 기준 | [docs/project/setup.md](docs/project/setup.md) |
| Git·브랜치·PR | [docs/project/workflow.md](docs/project/workflow.md) |
| 멀티 워크트리 운영 | [docs/project/worktrees.md](docs/project/worktrees.md) |
| 로컬 전용 취향 설정 | [docs/project/local-preferences.md](docs/project/local-preferences.md) |
| 문서 작성 규칙 | [docs/project/documentation-standards.md](docs/project/documentation-standards.md) |
| Cursor AI 규칙 | `.cursor/rules/*.mdc` |
| 체스 가이드 | [docs/chess/체스게임_완전가이드.md](docs/chess/체스게임_완전가이드.md) |
| SRPG 실행 방법 | [docs/srpg/SRPG_README.md](docs/srpg/SRPG_README.md) |
| SRPG 게임 디자인 | [docs/srpg/SRPG_GDD.md](docs/srpg/SRPG_GDD.md) |
| SRPG 기술 설계 | [docs/srpg/SRPG_TDD.md](docs/srpg/SRPG_TDD.md) |
| SRPG 구현 이력 | [docs/srpg/SRPG_진행이력.md](docs/srpg/SRPG_진행이력.md) |
| SRPG 백로그 | [docs/srpg/SRPG_BACKLOG.md](docs/srpg/SRPG_BACKLOG.md) |

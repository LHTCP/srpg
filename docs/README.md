# 문서 인덱스

프로젝트의 **마크다운 문서는 모두 `docs/` 아래 범주 폴더**에 둔다. 상세 규칙은 [project/documentation-standards.md](project/documentation-standards.md)를 본다.

## 빠른 링크

| 문서 | 설명 |
|------|------|
| [AGENTS.md](../AGENTS.md) | 사람·에이전트 공통 진입점 |
| [project/workflow.md](project/workflow.md) | Git·브랜치·PR·첫 커밋 |
| [project/setup.md](project/setup.md) | 로컬 개발 환경·재현성 기준 |
| [project/worktrees.md](project/worktrees.md) | 멀티 브랜치·멀티 에이전트 워크트리 운영 |
| [project/github-governance.md](project/github-governance.md) | GitHub 이슈·PR·브랜치 보호 운영 기준 |
| [project/documentation-standards.md](project/documentation-standards.md) | 문서 위치·갱신 규칙 |

## 프로젝트 공통 (`docs/project/`)

| 문서 | 설명 |
|------|------|
| [workflow.md](project/workflow.md) | Git 워크플로 |
| [setup.md](project/setup.md) | 개발 환경·Git 안전 설정·클라우드 호환 원칙 |
| [worktrees.md](project/worktrees.md) | `git worktree` 운영 규칙 |
| [local-preferences.md](project/local-preferences.md) | Git 미추적 로컬 작업 취향 설정 |
| [github-governance.md](project/github-governance.md) | GitHub 이슈·PR 템플릿과 브랜치 보호 기준 |
| [documentation-standards.md](project/documentation-standards.md) | 문서 전용 폴더 구조·작성 규칙 |

프로젝트 스크립트:

- `scripts/bootstrap.ps1`: Windows 초기 부트스트랩
- `scripts/bootstrap.sh`: macOS/Linux 초기 부트스트랩

## 체스 (`docs/chess/`)

| 문서 | 설명 |
|------|------|
| [Unity_설정가이드.md](chess/Unity_설정가이드.md) | 씬·오브젝트 빠른 설정 |
| [체스게임_완전가이드.md](chess/체스게임_완전가이드.md) | 게임·구조 상세 |

## SRPG (`docs/srpg/`)

| 문서 | 설명 |
|------|------|
| [SRPG_README.md](srpg/SRPG_README.md) | 씬 구성·실행 방법·조작 안내 |
| [SRPG_GDD.md](srpg/SRPG_GDD.md) | 게임 디자인(턴 구조·이동·전투·스킬) v0.2 |
| [SRPG_TDD.md](srpg/SRPG_TDD.md) | 기술 설계·스키마·코드 구조 v0.2 |
| [SRPG_진행이력.md](srpg/SRPG_진행이력.md) | 구현 단계별 이력·변경 요약 |
| [SRPG_FUTURE_NETWORK.md](srpg/SRPG_FUTURE_NETWORK.md) | 향후 멀티·AI·슬롯 기획 메모 |
| [SRPG_BACKLOG.md](srpg/SRPG_BACKLOG.md) | 후속 과제(우선순위 포함) |
| [SRPG_점검_및_로드맵.md](srpg/SRPG_점검_및_로드맵.md) | 프로젝트 점검 보고서 + 추천 로드맵 |

새 범주를 추가하면 `docs/<이름>/`를 만들고 이 표에 행을 추가한다.

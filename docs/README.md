# 문서 인덱스

프로젝트의 **마크다운 문서는 모두 `docs/` 아래 범주 폴더**에 둔다. 상세 규칙은 [project/documentation-standards.md](project/documentation-standards.md)를 본다.

## 빠른 링크

| 문서 | 설명 |
| ---- | ---- |
| [AGENTS.md](../AGENTS.md) | 사람·에이전트 공통 진입점 |
| [project/workflow.md](project/workflow.md) | Git·브랜치·PR·첫 커밋 |
| [project/setup.md](project/setup.md) | 로컬 개발 환경·재현성 기준 |
| [project/worktrees.md](project/worktrees.md) | 멀티 브랜치·멀티 에이전트 워크트리 운영 |
| [project/github-governance.md](project/github-governance.md) | GitHub 이슈·PR·브랜치 보호 운영 기준 |
| [project/documentation-standards.md](project/documentation-standards.md) | 문서 위치·갱신 규칙 |

## 프로젝트 공통 (`docs/project/`)

| 문서 | 설명 |
| ---- | ---- |
| [workflow.md](project/workflow.md) | Git 워크플로 |
| [setup.md](project/setup.md) | 개발 환경·Git 안전 설정·클라우드 호환 원칙 |
| [worktrees.md](project/worktrees.md) | `git worktree` 운영 규칙 |
| [local-preferences.md](project/local-preferences.md) | Git 미추적 로컬 작업 취향 설정 |
| [github-governance.md](project/github-governance.md) | GitHub 이슈·PR 템플릿과 브랜치 보호 기준 |
| [documentation-standards.md](project/documentation-standards.md) | 문서 전용 폴더 구조·작성 규칙 |

프로젝트 스크립트:

- `scripts/bootstrap.ps1`: Windows 초기 부트스트랩
- `scripts/bootstrap.sh`: macOS/Linux 초기 부트스트랩

## SRPG (`docs/srpg/`)

| 문서 | 설명 |
| ---- | ---- |
| [SRPG_README.md](srpg/SRPG_README.md) | v1 프로토타입 실행/검증 가이드 |
| [SRPG_프로토타입_마스터플랜.md](srpg/SRPG_프로토타입_마스터플랜.md) | M0~M4 단계별 실행 기준 문서 |
| [SRPG_전투규칙_기준서_v2.md](srpg/SRPG_전투규칙_기준서_v2.md) | 신규 대화 원본 기반 단일 전투 규칙 기준서 |
| [SRPG_GDD.md](srpg/SRPG_GDD.md) | v2 게임 디자인 문서 |
| [SRPG_TDD.md](srpg/SRPG_TDD.md) | v2 기술 설계 문서 |
| [SRPG_레거시_코드_분류.md](srpg/SRPG_레거시_코드_분류.md) | 기존 SRPG 코드 분류(Discard/Rework/Keep/New) |
| [SRPG_다음미팅_논의사항.md](srpg/SRPG_다음미팅_논의사항.md) | 다음 미팅 필수 의사결정 항목 |
| [SRPG_BACKLOG.md](srpg/SRPG_BACKLOG.md) | v2 후속 과제 목록 |
| [SRPG_PHASE2_CODE_BACKLOG.md](srpg/SRPG_PHASE2_CODE_BACKLOG.md) | 코드 2차 착수용 파일 단위 백로그 |
| [SRPG_CHANGELOG.md](srpg/SRPG_CHANGELOG.md) | v1 전환 이력 |
| [SRPG_AI_SIMULATION_GUIDE.md](srpg/SRPG_AI_SIMULATION_GUIDE.md) | AI 스텁 하이브리드 시뮬레이션 실행/판정 가이드 |
| [SRPG_GDD_TEST_TRACEABILITY.md](srpg/SRPG_GDD_TEST_TRACEABILITY.md) | GDD 항목별 자동화 테스트 커버 매핑 |
| [SRPG_FUTURE_NETWORK.md](srpg/SRPG_FUTURE_NETWORK.md) | 향후 네트워크·AI 메모 |
| [SRPG_V0_ARCHIVE.md](srpg/SRPG_V0_ARCHIVE.md) | v0 문서 아카이브 안내 |
| [SRPG_NEW_DIALOG_POLICY_LOCK.md](srpg/new/SRPG_NEW_DIALOG_POLICY_LOCK.md) | 신규 대화(06~10) 확정/미정 잠금표 |
| [프로젝트-초기-기획서-초안-외.md](srpg/new/프로젝트-초기-기획서-초안-외.md) | 신규 기획 PDF 변환 마크다운 |

새 범주를 추가하면 `docs/<이름>/`를 만들고 이 표에 행을 추가한다.

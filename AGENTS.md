# AGENTS — 프로젝트 진입점

## 한 줄 요약

Unity 6 기반 SRPG 프로토타입 프로젝트. 주 개발 대상은 `Assets/Scripts/SRPG/`이며, 문서는 모두 `docs/` 하위에서 관리한다.

## 환경

- **Unity 에디터**: 6000.3.13f1 (`ProjectSettings/ProjectVersion.txt` 기준)
- **실행 진입점**: `SrpgLobby` 씬
- **핵심 문서**: `docs/srpg/README.md`, `docs/srpg/SRPG_전투규칙_기준서_v2.md`, `docs/srpg/SRPG_BACKLOG.md`
- **공통 개발 기준**: [docs/project/work-contract.md](docs/project/work-contract.md), [docs/project/setup.md](docs/project/setup.md), [docs/project/workflow.md](docs/project/workflow.md), [docs/project/worktrees.md](docs/project/worktrees.md)
- **로컬 취향 설정(선택)**: `project.local.json`이 있으면 공통 규칙과 충돌하지 않는 범위에서 참고

## 도메인 구조

SRPG 코드는 전투·HUD·렌더링·메이커·데이터·로비 도메인으로 분리되어 있다.

- 도메인 규칙: `.cursor/rules/srpg-*.mdc`
- 분기 규칙: `.cursor/rules/srpg-dispatch.mdc`
- 파일 맵: `Assets/Scripts/SRPG/AGENTS.md`

## 작업 원칙

1. 요청 범위에 맞게 **최소 변경**으로 수정한다 (전역 규칙: `.cursor/rules/project-core.mdc`).
2. 구현 전에 가정·모호함·트레이드오프가 있으면 먼저 드러낸다.
3. 작업 완료 기준을 검증 가능한 형태로 잡고, 가능한 범위에서 확인한다.
4. 씬·프리팹·인스펙터 레퍼런스가 바뀔 수 있으면, 사용자에게 확인할 항목을 짧은 체크리스트로 남긴다.
5. **새·수정 문서**는 `Assets/`가 아니라 `docs/<범주>/`에 둔다 (`.cursor/rules/documentation.mdc`).

## 문서·규칙 링크

| 목적 | 위치 |
|------|------|
| 문서 목록·맵 | [docs/README.md](docs/README.md) |
| 작업 처리 계약 | [docs/project/work-contract.md](docs/project/work-contract.md) |
| 개발 환경·재현성 기준 | [docs/project/setup.md](docs/project/setup.md) |
| Git·브랜치·PR | [docs/project/workflow.md](docs/project/workflow.md) |
| 멀티 워크트리 운영 | [docs/project/worktrees.md](docs/project/worktrees.md) |
| 로컬 전용 취향 설정 | [docs/project/local-preferences.md](docs/project/local-preferences.md) |
| 문서 작성 규칙 | [docs/project/documentation-standards.md](docs/project/documentation-standards.md) |
| GitHub 운영 기준 | [docs/project/github-governance.md](docs/project/github-governance.md) |
| 최신 플레이 가이드 | [docs/project/latest-play-guide.md](docs/project/latest-play-guide.md) |
| SRPG 문서 맵 | [docs/srpg/README.md](docs/srpg/README.md) |
| SRPG 실행 가이드 | [docs/srpg/SRPG_README.md](docs/srpg/SRPG_README.md) |
| SRPG 전투규칙 기준서(v2) | [docs/srpg/SRPG_전투규칙_기준서_v2.md](docs/srpg/SRPG_전투규칙_기준서_v2.md) |
| SRPG 백로그 | [docs/srpg/SRPG_BACKLOG.md](docs/srpg/SRPG_BACKLOG.md) |
| SRPG 코드 백로그 | [docs/srpg/SRPG_PHASE2_CODE_BACKLOG.md](docs/srpg/SRPG_PHASE2_CODE_BACKLOG.md) |
| SRPG 마스터플랜 | [docs/srpg/SRPG_프로토타입_마스터플랜.md](docs/srpg/SRPG_프로토타입_마스터플랜.md) |
| SRPG GDD | [docs/srpg/SRPG_GDD.md](docs/srpg/SRPG_GDD.md) |
| SRPG TDD | [docs/srpg/SRPG_TDD.md](docs/srpg/SRPG_TDD.md) |
| SRPG 코드 분류 | [docs/srpg/SRPG_레거시_코드_분류.md](docs/srpg/SRPG_레거시_코드_분류.md) |
| 과거 미팅 메모 | [docs/srpg/SRPG_다음미팅_논의사항.md](docs/srpg/SRPG_다음미팅_논의사항.md) |

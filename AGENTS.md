# AGENTS — 프로젝트 진입점

## 한 줄 요약

Unity 6 기반 SRPG 프로토타입 프로젝트. 주 개발 대상은 `Assets/Scripts/SRPG/`이며, 문서는 모두 `docs/` 하위에서 관리한다.

## 환경

- **Unity 에디터**: 6000.0.45f1 (`ProjectSettings/ProjectVersion.txt`)
- **실행 진입점**: `SrpgLobby` 씬
- **핵심 문서**: `docs/srpg/SRPG_전투규칙_기준서_v2.md`, `docs/srpg/SRPG_README.md`, `docs/srpg/SRPG_프로토타입_마스터플랜.md`

## 도메인 구조

SRPG 코드는 전투·HUD·렌더링·메이커·데이터·로비 도메인으로 분리되어 있다.

- 도메인 규칙: `.cursor/rules/srpg-*.mdc`
- 분기 규칙: `.cursor/rules/srpg-dispatch.mdc`
- 파일 맵: `Assets/Scripts/SRPG/AGENTS.md`

## 작업 원칙

1. 요청 범위에 맞는 최소 변경을 적용한다.
2. 씬/인스펙터 레퍼런스가 바뀌면 확인 체크리스트를 남긴다.
3. 문서는 `docs/<범주>/`에만 둔다.

## 문서·규칙 링크

| 목적 | 위치 |
| ---- | ---- |
| 문서 인덱스 | [docs/README.md](docs/README.md) |
| Git 워크플로 | [docs/project/workflow.md](docs/project/workflow.md) |
| 문서 작성 규칙 | [docs/project/documentation-standards.md](docs/project/documentation-standards.md) |
| SRPG 실행 가이드 | [docs/srpg/SRPG_README.md](docs/srpg/SRPG_README.md) |
| SRPG 전투규칙 기준서(v2) | [docs/srpg/SRPG_전투규칙_기준서_v2.md](docs/srpg/SRPG_전투규칙_기준서_v2.md) |
| SRPG 마스터플랜 | [docs/srpg/SRPG_프로토타입_마스터플랜.md](docs/srpg/SRPG_프로토타입_마스터플랜.md) |
| SRPG GDD | [docs/srpg/SRPG_GDD.md](docs/srpg/SRPG_GDD.md) |
| SRPG TDD | [docs/srpg/SRPG_TDD.md](docs/srpg/SRPG_TDD.md) |
| SRPG 코드 분류 | [docs/srpg/SRPG_레거시_코드_분류.md](docs/srpg/SRPG_레거시_코드_분류.md) |
| 다음 미팅 안건 | [docs/srpg/SRPG_다음미팅_논의사항.md](docs/srpg/SRPG_다음미팅_논의사항.md) |

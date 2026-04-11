# Git 워크플로

## 브랜치 이름

- 기능: `feature/짧은-설명` (예: `feature/chess-undo`)
- 수정: `fix/이슈-요약`
- 문서만: `docs/요약` 허용

## 커밋 메시지

- 한 커밋에 한 주제. 첫 줄은 요약(약 50자 이내, 명령형 권장).
- 필요 시 본문에 이유·에디터에서 확인할 일을 적는다.

예:

```
Add pawn promotion check in ChessGameManager

- 인스펙터에 새 필드 추가 시 씬에서 재할당 필요
```

## Pull request 전 확인 (Unity)

- [ ] 관련 씬·프리팹·인스펙터 참조가 깨지지 않았는지 에디터에서 확인
- [ ] Play 모드에서 요청한 동작이 재현되는지
- [ ] 공개 API나 인스펙터 필드가 바뀌었다면 인접 문서 또는 `docs/` 갱신

## 첫 커밋에 포함할 것

- **포함 권장**: `Assets/`(스크립트, 씬, 프리팹 등), `ProjectSettings/`, `Packages/`, 루트 `AGENTS.md`, `docs/`, `.cursor/rules/`, `.gitignore`
- **제외됨**: `.gitignore`에 따라 `Library/`, `Temp/`, `Logs/`, `UserSettings/` 등은 커밋하지 않음

처음 `git add` 후 `git status`로 불필요한 대용량·로컬 폴더가 올라가지 않았는지 본다.

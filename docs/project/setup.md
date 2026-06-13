# 개발 환경 세팅

이 문서는 **사람·에이전트 공통의 로컬 진입점**이다. 특정 IDE나 특정 AI 도구 대신, 저장소 자체를 기준으로 환경을 맞춘다.

관련 추적 이슈: GitHub [#1](https://github.com/LHTCP/srpg/issues/1)

## 저장소 구조

- 권장 워크스페이스 루트: `.../LHTCP/`
- 메인 저장소 체크아웃: `.../LHTCP/srpg/`
- 향후 워크트리 루트: `.../LHTCP/worktrees/`

즉, 저장소 자체와 로컬 작업 공간 루트를 분리해서 본다. 이 구조는 멀티 에이전트, 멀티 브랜치, 임시 실험 작업을 안전하게 분리하기 쉽다.

## 필수 도구

- Git
- GitHub 계정 및 저장소 접근 권한
- Unity Hub
- Unity Editor `6000.3.13f1`

선택 도구:

- Rider 또는 VS Code
- GitHub CLI (`gh`)
- Codex, Claude, Cursor, Copilot 같은 에이전트 도구

선택 도구는 바뀔 수 있지만, 저장소 규칙과 문서는 동일하게 따른다.

## 로컬 전용 취향 설정

공식 문서와 별개로, 현재 로컬에서만 쓰는 작업 취향은 `project.local.json`에 둘 수 있다.

- 예시: `project.local.example.json`
- 실사용: `project.local.json` (`.gitignore` 처리)

상세 규칙은 [local-preferences.md](local-preferences.md)를 본다.

## 첫 세팅

1. `LHTCP/srpg` 저장소를 로컬에 클론한다.
2. 부트스트랩 스크립트를 실행한다.
3. Unity Hub에 `srpg/` 폴더를 프로젝트로 등록한다.
4. Unity Editor 버전이 `6000.3.13f1`인지 확인한다.
5. 프로젝트를 한 번 열어 패키지 복원을 끝낸다.
6. 필요하면 IDE 프로젝트 파일을 재생성한다.

### Windows

```powershell
./scripts/bootstrap.ps1
```

### macOS / Linux

```bash
./scripts/bootstrap.sh
```

이 스크립트는 다음을 자동으로 처리한다.

- 워크스페이스 루트 아래 `worktrees/` 폴더 생성
- 현재 저장소 경로를 Git `safe.directory`에 등록
- Unity 버전 파일 존재 여부 확인

옵션:

- Git 안전 설정을 건너뛰려면 `-SkipGitSafeDirectory` 또는 `--skip-git-safe-directory`
- `worktrees/` 생성만 건너뛰려면 `-SkipWorktreeRoot` 또는 `--skip-worktree-root`

## Git 안전 설정

로컬에서 저장소 소유권이 다르면 Git이 `safe.directory` 경고를 낼 수 있다. 기본적으로는 `scripts/bootstrap.ps1` 또는 `scripts/bootstrap.sh`가 이 설정을 처리한다.

수동으로 등록해야 한다면 현재 머신에서만 아래처럼 신뢰 디렉터리를 등록한다.

```powershell
git config --global --add safe.directory <workspace-root>/srpg
```

워크트리를 추가로 만들면 해당 경로도 같은 방식으로 등록할 수 있다.

## 로컬 LFS 설정

이 저장소는 `.lfsconfig`로 사설 LFS 서버 위치를 공유한다. URL 자체는 secret이 아니지만 public repo에서 인프라 식별자가 노출될 수 있으므로, 실제 인증 정보는 절대 문서나 PR에 쓰지 않는다.

```powershell
git lfs pull
```

CI에서는 `.lfsconfig` 값과 GitHub repository secret `LFS_URL`이 일치하는지 대조한다. mirror 또는 공개용 LFS endpoint로 전환하면 `.lfsconfig`와 `LFS_URL`을 같은 PR에서 함께 갱신한다.

## Unity 확인 항목

- `ProjectSettings/ProjectVersion.txt`의 Unity 버전과 로컬 에디터 버전이 일치하는지
- `Packages/manifest.json`이 자동 변경되지 않았는지
- `Assets/Scenes/`와 `ProjectSettings/EditorBuildSettings.asset`의 씬 구성이 맞는지

## 로컬 개발 원칙

- 로컬에서 자유롭게 실험할 수 있다.
- 하지만 기본 브랜치에 반영되는 변경은 **브랜치 + PR + CI** 경로를 따른다.
- 개인 IDE 설정이나 개인 에이전트 설정은 보조 수단일 뿐, 저장소 규칙보다 우선하지 않는다.

## 클라우드/에이전트 호환 원칙

다음 작업은 Unity 에디터 없이도 다루기 쉽게 유지한다.

- 문서 수정
- C# 코드 리뷰 및 일부 로직 수정
- 패키지/설정 파일 검토
- CI 설정
- 작업 체크리스트와 운영 정책 정리
- 저장소 기본 구조 검증 스크립트 실행

다음 작업은 여전히 로컬 Unity 확인이 필요할 수 있다.

- 씬 배치 변경
- 인스펙터 참조 연결
- 프리팹 구조 수정
- 에셋 저작 및 시각 확인

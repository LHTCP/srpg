# 워크트리 운영 규칙

이 문서는 로컬 멀티 브랜치·멀티 에이전트 작업을 안전하게 하기 위한 `git worktree` 운영 기준이다.

관련 추적 이슈: GitHub [#1](https://github.com/LHTCP/srpg/issues/1)

## 목표

- 메인 체크아웃을 깨뜨리지 않고 병렬 작업을 분리한다.
- 여러 에이전트가 같은 저장소를 동시에 다룰 때 충돌을 줄인다.
- 브랜치별 실험, CI 대응, 핫픽스 작업을 빠르게 분기한다.

## 권장 로컬 구조

```text
LHTCP/
├─ srpg/
└─ worktrees/
   ├─ feature-docs-workflow/
   ├─ feature-ci-fast-checks/
   └─ fix-unity-version-docs/
```

- `srpg/`는 메인 체크아웃이다.
- `worktrees/`는 브랜치별 보조 작업 디렉터리다.
- 워크트리는 저장소 루트 바깥이 아니라, **같은 워크스페이스 루트 아래 별도 폴더**에 둔다.

## 브랜치/폴더 규칙

- 브랜치 이름은 [workflow.md](workflow.md)의 규칙을 따른다.
- 워크트리 폴더 이름은 브랜치 이름을 읽기 쉬운 파일 시스템 이름으로 바꿔 쓴다.
- 하나의 워크트리에는 하나의 작업 주제만 둔다.

예:

- 브랜치: `feature/ci-fast-checks`
- 폴더: `worktrees/feature-ci-fast-checks`

## 생성 예시

먼저 메인 체크아웃에서 부트스트랩 스크립트를 실행해 `worktrees/` 폴더와 Git 안전 설정을 맞춘다.

```powershell
cd <workspace-root>/srpg
./scripts/bootstrap.ps1
```

```powershell
cd <workspace-root>/srpg
git worktree add ../worktrees/feature-ci-fast-checks -b feature/ci-fast-checks
```

기존 원격 브랜치를 붙일 때:

```powershell
cd <workspace-root>/srpg
git worktree add ../worktrees/fix-doc-version origin/fix/doc-version
```

## 사용 규칙

- 메인 체크아웃과 워크트리에서 같은 파일을 동시에 수정하지 않는다.
- 에이전트 하나당 워크트리 하나를 기본으로 잡는다.
- 큰 작업은 한 워크트리 안에서 여러 이슈를 섞지 않는다.
- 작업이 끝난 워크트리는 정리한다.

## 정리 예시

```powershell
cd <workspace-root>/srpg
git worktree remove ../worktrees/feature-ci-fast-checks
```

브랜치까지 지우려면 워크트리 제거 후 별도로 삭제한다.

```powershell
git branch -d feature/ci-fast-checks
```

## Git 안전 설정

Windows 로컬 환경에서는 저장소 소유권 차이로 `safe.directory` 설정이 추가로 필요할 수 있다.

- 메인 체크아웃: `<workspace-root>/srpg`
- 워크트리 예시: `<workspace-root>/worktrees/feature-ci-fast-checks`

필요 시 각 경로를 개별 등록한다.

## GitHub-first와의 관계

워크트리는 **로컬 작업 방식**이고, GitHub-first는 **통합 방식**이다. 둘은 충돌하지 않는다.

- 로컬에서는 워크트리로 자유롭게 병렬 작업 가능
- 통합은 브랜치 푸시 후 PR 기준으로 진행
- 머지 가능 여부는 로컬 상태가 아니라 저장소의 PR/체크 기준으로 판단

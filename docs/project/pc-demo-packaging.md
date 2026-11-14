# PC 데모 패키징 기준

이 문서는 issue #70의 산출물이다. 목표는 Windows PC 데모를 itch.io, GitHub Release, GitHub Actions artifact 어디에 올리더라도 같은 구조로 검증할 수 있게 만드는 것이다.

## 결론

초기 PC 데모는 itch.io 프로젝트 페이지 <https://lhtcp.itch.io/lhtcp-srpg>에 올릴 수 있는 zip 하나를 기준 산출물로 둔다. WebGL과 모바일은 이번 기준의 완료조건이 아니며, PC zip이 정상적으로 내려받아지고 실행되는지를 먼저 닫는다.

## 현재 상태

Windows zip 생성과 `BUILD_INFO.txt` 생성은 `Windows PC 데모 빌드` workflow와 `scripts/package-windows-demo.sh` 기준으로 구현되어 있다. itch.io 수동 업로드 또는 butler 업로드는 이 zip을 배포면에 게시하는 후속 단계이며, `development` 채널용 butler 수동 업로드 workflow는 저장소에 추가되어 있다.

현재 확정된 사실:

- itch.io 프로젝트 페이지는 <https://lhtcp.itch.io/lhtcp-srpg>다.
- 첫 delivery 구현 목표는 Windows PC zip이다.
- GitHub Actions `Windows PC 데모 빌드` workflow가 `srpg-demo-windows-<a.b.c.build>-<short-sha>` artifact를 생성한다.
- GitHub Actions artifact는 바깥 컨테이너 zip을 한 번 더 감쌀 수 있으므로, itch.io 게시에는 내부 실제 게임 zip을 사용한다.
- `Windows PC 데모 빌드` workflow는 `main`에서 성공한 실행 기록이 있다.
- `itch.io Delivery 설정 헬스체크` workflow(`.github/workflows/itchio-delivery-healthcheck.yml`)가 있다. 2026-11-14 `gh run list` 확인 기준 실행 기록은 2건이며, `main` 실행 1건은 실패했고 수정 브랜치 `ci/fix-butler-healthcheck` 실행 1건은 성공했다. 수정이 `main`에 병합된 뒤 `main`에서 성공한 실행 기록은 아직 없다.
- `itch.io Development 업로드` workflow(`.github/workflows/itchio-development-upload.yml`)가 있다. `workflow_dispatch` 수동 실행 전용이며, 기존 Windows artifact를 butler로 `development` 채널에 올린다. 2026-11-14 확인 기준 실행 기록이 0건이므로 실제 업로드 성공 여부는 미검증이다.

- `Windows Release asset 업로드` workflow(`.github/workflows/windows-release-asset.yml`)가 있다. `workflow_dispatch` 수동 실행 전용이며, 기존 Windows artifact를 GitHub Release asset으로 올린다. 2026-11-14 확인 기준 실행 기록과 게시된 Release가 없으므로 미검증이다. 정책은 [release-versioning.md](release-versioning.md)의 `GitHub Release asset 정책`을 따른다.

- `Windows Delivery 파이프라인` workflow(`.github/workflows/windows-delivery.yml`)가 있다. `main` push와 `v<a.b.c>` tag push에서 빌드부터 업로드까지 자동으로 잇는다. 2026-11-14 추가했으며 실행 기록이 없으므로 미검증이다.

아직 구현 전인 설계 후보:

- `release.json` 추가와 production patch 증가 검증
- itch.io production 채널 첫 게시 검증(업로드 경로는 구현했지만 기본값은 꺼짐)

## 자동화 범위와 트리거

`Windows Delivery 파이프라인` workflow(`.github/workflows/windows-delivery.yml`)가 빌드, 패키징, 업로드를 한 run으로 잇는다. 사람이 run id와 artifact 이름을 옮겨 적을 필요가 없다. 2026-11-14에 추가했으며, 기본 브랜치에 병합된 뒤 첫 실행으로 확인하기 전까지 미검증이다.

| 트리거 | 자동으로 하는 일 | 사람이 하는 일 |
| ------ | ---------------- | -------------- |
| `main`에 push(병합) | Windows 빌드 → zip → itch.io `development` 업로드 | 없음 |
| `v<a.b.c>` tag push | Windows 빌드 → zip → GitHub Release(prerelease) 생성. 설정 시 itch.io `production` 업로드 | `bundleVersion`을 올린 커밋에 tag를 만들어 push |
| 수동 실행 | Windows 빌드 → zip → 선택 시 itch.io `development` 업로드 | `upload_itchio` 선택 후 실행 |

`main` push는 빌드 결과가 달라지는 경로가 바뀔 때만 실행된다: `Assets/**`(`Assets/Tests/**` 제외), `Packages/**`, `ProjectSettings/**`, `scripts/package-windows-demo.sh`. 문서, 테스트, workflow 파일만 바꾼 병합은 빌드와 업로드를 돌리지 않는다.

tag push는 빌드 전에 tag와 `ProjectSettings/ProjectSettings.asset`의 `bundleVersion`이 일치하는지 먼저 확인하고, 다르면 빌드 없이 실패한다.

자동 업로드는 repository variable로 끄고 켠다.

| variable | 값이 없을 때 | 용도 |
| -------- | ------------ | ---- |
| `DELIVERY_ITCHIO_DEVELOPMENT` | 켜짐 | `false`로 두면 `main` push에서 itch.io `development` 업로드를 건너뛴다. 빌드와 artifact 생성은 계속한다. |
| `DELIVERY_ITCHIO_PRODUCTION` | 꺼짐 | `true`로 두면 tag push에서 itch.io `production` 채널에도 올린다. production 채널은 게시한 적이 없으므로 첫 게시 전에 사람이 켠다. |

아직 사람이 해야 하는 일:

- 릴리즈컷: `bundleVersion`을 올리는 PR 병합과 tag push
- Release의 prerelease 해제: 자동 생성 Release는 정책 기본값대로 prerelease다. 정식 전환은 Release 화면에서 한다.
- 게시 후 smoke test와 known issue 안내
- Actions cache 정리: 업로드 전 cache 예산 확인에서 막히면 `Actions 저장소 정리`를 수동 실행한다.
- secret과 variable 준비: 파이프라인은 `UNITY_*`, `LFS_*`, `BUTLER_API_KEY`, `ITCHIO_USERNAME`, `ITCHIO_GAME`이 이미 설정되어 있다고 가정한다.

버전의 build number는 실행한 workflow의 run number다. `Windows Delivery 파이프라인`과 `Windows PC 데모 빌드`는 run number를 따로 세므로, 두 경로로 만든 빌드는 같은 `a.b.c.<build>`를 가질 수 있다. 이때도 파일명의 `<short-sha>`와 `BUILD_INFO.txt`의 `workflow_run`으로 구분할 수 있다.

개별 workflow는 재실행과 복구용 수동 경로로 남아 있다. 파이프라인은 이 workflow들을 호출해서 쓴다.

| 단계 | workflow | 단독 트리거 | 입력값 |
| ---- | -------- | ----------- | ------ |
| Windows 빌드와 zip 패키징 | `Windows PC 데모 빌드` | 수동 실행 | 없음 |
| itch.io development 업로드 | `itch.io Development 업로드` | 수동 실행 | `run_id`, `artifact_name`, 선택 `version` |
| GitHub Release asset 업로드 | `Windows Release asset 업로드` | 수동 실행 | `run_id`, `artifact_name`, `tag`, `prerelease`, `overwrite` |
| 패키징 경로 검증 | `Windows 패키징 헬스체크` | 수동 실행, PR 자동 | 없음 |

`itch.io Development 업로드`는 수동 실행에서는 `development` 채널만 올린다. `production` 채널은 파이프라인이 호출할 때만 지정할 수 있다.

PR에서 자동으로 실행되는 패키징 검증은 `Windows 패키징 헬스체크`뿐이다. PR이 `.github/workflows/windows-demo-build.yml`, `.github/workflows/windows-packaging-healthcheck.yml`, `scripts/package-windows-demo.sh` 중 하나를 바꿀 때 실행된다. Unity 빌드 없이 모의 산출물로 zip 패키징과 artifact 업로드 경로만 확인하므로, 통과해도 실제 빌드 성공을 보장하지 않는다.

전체 workflow 목록은 [cicd-runbook.md](cicd-runbook.md)의 `Workflow와 트리거`를 본다.

## 산출물 이름

현재 workflow가 만드는 zip 이름은 다음 형식이다.

```text
srpg-demo-windows-<a.b.c.build>-<short-sha>.zip
```

`<a.b.c.build>`는 `ProjectSettings/ProjectSettings.asset`의 `bundleVersion`에 GitHub Actions run number를 붙인 값이고, `<short-sha>`는 추적성을 위한 커밋 SHA 앞 7자리다.

예시:

```text
srpg-demo-windows-0.1.0.123-9861a2f.zip
```

production 게시에서 파일명에 커밋 SHA를 유지할지, 표시 버전만 남길지는 production 릴리즈컷 workflow를 만들 때 다시 결정한다.

## zip 구조

zip을 풀었을 때 최상위 폴더 하나가 나오도록 만든다. 사용자가 다운로드 폴더에 여러 Unity 파일을 흩뿌리지 않게 하기 위해서다.

```text
srpg-demo-windows-<a.b.c.build>-<short-sha>/
  WindowsDemo.exe
  WindowsDemo_Data/
  UnityPlayer.dll
  README.txt
  BUILD_INFO.txt
```

실행 파일과 `_Data` 폴더 이름은 `Windows PC 데모 빌드` workflow의 `buildName: WindowsDemo`를 따른다. `buildName`을 바꾸면 함께 달라진다. 단, 실행 파일과 `_Data` 폴더가 같은 폴더에 있어야 한다.

## 필수 포함 파일

| 항목 | 필수 여부 | 기준 |
| ---- | --------- | ---- |
| `.exe` 실행 파일 | 필수 | 사용자가 직접 실행하는 진입점이다. |
| `<buildName>_Data/` | 필수 | Unity standalone player 실행에 필요하다. |
| `UnityPlayer.dll` | 필수 | Windows Unity player 런타임이다. |
| `README.txt` | 권장 | 실행 방법, known issue, 문의 위치를 짧게 적는다. |
| `BUILD_INFO.txt` | 권장 | 커밋 SHA, 빌드 일시, workflow run URL을 적는다. |

## README.txt 최소 내용

`README.txt`는 게임 소개 문서가 아니라 실행 안내다. 긴 설명은 itch.io 페이지나 릴리스 노트에 둔다. 아래는 `scripts/package-windows-demo.sh`가 실제로 쓰는 내용이다. 버전과 커밋 같은 빌드 정보는 `README.txt`가 아니라 `BUILD_INFO.txt`에 넣는다.

```text
SRPG Demo

실행 방법:
1. zip을 원하는 폴더에 압축 해제합니다.
2. Windows 실행 파일을 실행합니다.
3. Windows 보안 경고가 뜨면 배포 출처와 버전을 확인한 뒤 실행 여부를 결정합니다.

Known issue:
- 최신 known issue는 GitHub issue 또는 itch.io 페이지를 확인합니다.
```

## BUILD_INFO.txt 최소 내용

```text
version=
base_version=
build_number=
channel=
commit=
short_commit=
branch=
build_time_utc=
workflow_run=
source_repo=https://github.com/LHTCP/srpg
itch_project=https://lhtcp.itch.io/lhtcp-srpg
```

로컬 수동 빌드라면 `workflow_run=local`로 둔다.

## 로컬에서 패키징 스크립트 실행

`scripts/package-windows-demo.sh`는 Unity 빌드를 하지 않는다. 이미 만들어진 Windows 빌드 폴더를 복사하고 `README.txt`, `BUILD_INFO.txt`를 추가한 뒤 zip으로 묶는다. 로컬에서 쓰려면 Unity Editor에서 Windows 빌드를 먼저 만든다.

필요한 것:

- bash (Windows는 Git Bash 또는 WSL)
- `zip` 명령. Git for Windows 기본 설치에는 없을 수 있으므로 없으면 WSL을 쓰거나 따로 설치한다.

환경변수:

| 변수 | 필수 여부 | 기본값 | 용도 |
| ---- | --------- | ------ | ---- |
| `ARTIFACT_NAME` | 필수 | 없음 | zip과 최상위 폴더 이름 |
| `BUILD_DIR` | 선택 | `build/StandaloneWindows64` | Windows 빌드 산출물 폴더 |
| `PACKAGE_ROOT` | 선택 | `dist/<ARTIFACT_NAME>` | zip으로 묶기 전 조립 폴더 |
| `BASE_VERSION`, `FULL_VERSION`, `BUILD_NUMBER` | 선택 | `unknown` | `BUILD_INFO.txt`의 버전 값 |
| `CHANNEL` | 선택 | `development` | `BUILD_INFO.txt`의 `channel` |
| `COMMIT_SHA`, `SHORT_SHA`, `BRANCH_NAME` | 선택 | `unknown` | `BUILD_INFO.txt`의 커밋과 브랜치 값 |
| `WORKFLOW_RUN_URL` | 선택 | `unknown` | `BUILD_INFO.txt`의 `workflow_run`. 로컬은 `local` |

저장소 루트에서 실행하는 예시:

```bash
BUILD_DIR=build/StandaloneWindows64 \
ARTIFACT_NAME="srpg-demo-windows-0.1.0.0-$(git rev-parse --short=7 HEAD)" \
BASE_VERSION=0.1.0 FULL_VERSION=0.1.0.0 BUILD_NUMBER=0 \
COMMIT_SHA="$(git rev-parse HEAD)" SHORT_SHA="$(git rev-parse --short=7 HEAD)" \
BRANCH_NAME="$(git branch --show-current)" WORKFLOW_RUN_URL=local \
bash scripts/package-windows-demo.sh
```

결과는 `dist/<ARTIFACT_NAME>.zip`에 생긴다. 로컬 빌드는 run number가 없으므로 build number를 `0`처럼 CI 빌드와 구분되는 값으로 둔다. 로컬 zip은 내부 확인용이며, itch.io와 GitHub Release 업로드 workflow는 `Windows PC 데모 빌드` run의 artifact만 받는다.

## itch.io 프로젝트와 채널명

프로젝트 URL은 <https://lhtcp.itch.io/lhtcp-srpg>다.

채널은 development와 production을 분리한다.

| 채널 | 용도 |
| ---- | ---- |
| `development` | 최신 검증 빌드. main 또는 수동 workflow에서 반복 갱신할 수 있다. |
| `production` | 릴리즈컷으로 고정한 빌드. GitHub Release와 대응한다. |

기존 후보였던 `windows-demo`는 플랫폼 의미는 명확하지만, 릴리즈 단계 구분이 약하다. PC/Windows 전용 프로젝트인 동안에는 itch.io 파일 platform을 Windows로 지정하고, 채널명은 release channel의 의미가 드러나는 `development`/`production`을 우선한다.

butler 업로드는 다음 형태를 기준으로 한다. 두 채널 모두 `itch.io Development 업로드` workflow가 처리한다. `production` 채널 push는 `Windows Delivery 파이프라인`이 tag push에서 호출할 때만, 그리고 `DELIVERY_ITCHIO_PRODUCTION=true`일 때만 실행된다.

```text
butler push <zip-or-folder> lhtcp/lhtcp-srpg:development
butler push <zip-or-folder> lhtcp/lhtcp-srpg:production
```

실제 계정/slug 값은 GitHub Actions variable 또는 secret에서 주입한다. 현재 사람이 확인한 프로젝트 URL 기준으로는 `lhtcp/lhtcp-srpg`를 기본 후보로 둔다.

## 수동 smoke test

PC 데모 zip을 게시하기 전에는 다음을 확인한다.

- [ ] zip 파일명이 `srpg-demo-windows-<a.b.c.build>-<short-sha>.zip` 형식이다.
- [ ] zip을 풀면 최상위 폴더 하나가 나온다.
- [ ] 최상위 폴더 안에 `.exe`, `_Data` 폴더, `UnityPlayer.dll`이 있다.
- [ ] `README.txt` 또는 itch.io 페이지에 실행 방법이 있다.
- [ ] `BUILD_INFO.txt`에 `version`, `base_version`, `build_number`, `channel`, 커밋 SHA가 있다.
- [ ] Windows에서 실행 파일이 시작된다.
- [ ] 로비 화면에 진입한다.
- [ ] 전투 시작 또는 핵심 플레이 루프를 1회 확인한다.
- [ ] 시작 즉시 크래시, 치명적 콘솔 오류, 읽을 수 없는 텍스트 문제가 있으면 known issue 또는 block으로 기록한다.

## GitHub Actions artifact 기준

자동 빌드 artifact 이름은 zip 이름과 맞춘다.

```text
srpg-demo-windows-<a.b.c.build>-<short-sha>
```

현재 workflow는 Unity `bundleVersion`을 기준 버전으로 읽고 GitHub Actions run number를 붙여 `a.b.c.<github.run_number>`를 만든다. artifact retention은 7일이다. 장기 보관이 필요한 빌드는 GitHub Release asset 또는 itch.io production 업로드 결과를 기준으로 삼는다.

## 무료 운영 가드레일

- public repo standard GitHub-hosted runner 범위에서 먼저 검증한다.
- larger runner, self-hosted runner, 유료 스토리지, S3/CloudFront는 이번 기준의 기본 완료조건이 아니다.
- artifact/cache retention을 늘리는 PR은 비용과 보관 정책 영향을 PR 본문에 남긴다.
- itch.io 업로드는 수동 실행으로 시작했고, 2026-11-14 지시자 요청으로 `main` push 자동 업로드(`development` 채널)를 `Windows Delivery 파이프라인`에 추가했다. 공개를 멈춰야 하면 repository variable `DELIVERY_ITCHIO_DEVELOPMENT=false`로 끈다.
- 자동 실행도 public repo standard GitHub-hosted runner만 쓴다. 다만 `main` 병합마다 사설 LFS 서버 트래픽과 Unity 라이선스 활성화가 한 번씩 발생한다.

## 관련

- Parent: #69
- Closes: #70
- Related: #32
- Related: #73

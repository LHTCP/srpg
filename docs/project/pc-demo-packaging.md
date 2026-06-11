# PC 데모 패키징 기준

이 문서는 issue #70의 산출물이다. 목표는 Windows PC 데모를 itch.io, GitHub Release, GitHub Actions artifact 어디에 올리더라도 같은 구조로 검증할 수 있게 만드는 것이다.

## 결론

초기 PC 데모는 `windows-demo` 채널에 올릴 수 있는 zip 하나를 기준 산출물로 둔다. WebGL과 모바일은 이번 기준의 완료조건이 아니며, PC zip이 정상적으로 내려받아지고 실행되는지를 먼저 닫는다.

권장 산출물 이름은 다음 형식이다.

```text
srpg-demo-windows-<version>.zip
```

`<version>`은 수동 배포에서는 짧은 커밋 SHA를 우선 사용하고, 릴리스 태그가 생기면 태그를 우선한다.

예시:

```text
srpg-demo-windows-9861a2f.zip
srpg-demo-windows-v0.1.0.zip
```

## zip 구조

zip을 풀었을 때 최상위 폴더 하나가 나오도록 만든다. 사용자가 다운로드 폴더에 여러 Unity 파일을 흩뿌리지 않게 하기 위해서다.

```text
srpg-demo-windows-<version>/
  Srpg.exe
  Srpg_Data/
  UnityPlayer.dll
  README.txt
  BUILD_INFO.txt
```

파일명은 실제 Unity product name에 맞춰 달라질 수 있다. 단, 실행 파일과 `_Data` 폴더가 같은 폴더에 있어야 한다.

## 필수 포함 파일

| 항목 | 필수 여부 | 기준 |
| ---- | --------- | ---- |
| `.exe` 실행 파일 | 필수 | 사용자가 직접 실행하는 진입점이다. |
| `<ProductName>_Data/` | 필수 | Unity standalone player 실행에 필요하다. |
| `UnityPlayer.dll` | 필수 | Windows Unity player 런타임이다. |
| `README.txt` | 권장 | 실행 방법, known issue, 문의 위치를 짧게 적는다. |
| `BUILD_INFO.txt` | 권장 | 커밋 SHA, 빌드 일시, workflow run URL을 적는다. |

## README.txt 최소 내용

`README.txt`는 게임 소개 문서가 아니라 실행 안내다. 긴 설명은 itch.io 페이지나 릴리스 노트에 둔다.

```text
SRPG Demo

실행 방법:
1. zip을 원하는 폴더에 압축 해제합니다.
2. Srpg.exe를 실행합니다.
3. Windows 보안 경고가 뜨면 배포 출처와 버전을 확인한 뒤 실행 여부를 결정합니다.

빌드 정보:
- Version:
- Commit:
- Build date:

Known issue:
- 최신 known issue는 GitHub issue 또는 itch.io devlog를 확인합니다.
```

## BUILD_INFO.txt 최소 내용

```text
version=
commit=
branch=
build_time_utc=
workflow_run=
source_repo=https://github.com/LHTCP/srpg
```

로컬 수동 빌드라면 `workflow_run=local`로 둔다.

## itch.io 채널명

초기 추천 채널명은 `windows-demo`다.

- `windows`: 장기적으로 정식 Windows 배포 채널처럼 보일 수 있다.
- `pc-demo`: PC 우선 의도는 잘 드러나지만 itch.io 앱/플랫폼 필터에서 Windows임이 덜 명확하다.
- `windows-demo`: 현재 상태가 Windows 데모임을 가장 직접적으로 설명한다.

butler 자동화를 붙일 때는 다음 형태를 기준으로 한다.

```text
butler push <zip-or-folder> <itch-user>/<itch-game>:windows-demo
```

실제 `<itch-user>/<itch-game>` 값은 저장소 secret이나 수동 게시 체크리스트에서 정한다.

## 수동 smoke test

PC 데모 zip을 게시하기 전에는 다음을 확인한다.

- [ ] zip 파일명이 `srpg-demo-windows-<version>.zip` 형식이다.
- [ ] zip을 풀면 최상위 폴더 하나가 나온다.
- [ ] 최상위 폴더 안에 `.exe`, `_Data` 폴더, `UnityPlayer.dll`이 있다.
- [ ] `README.txt` 또는 itch.io 페이지에 실행 방법이 있다.
- [ ] `BUILD_INFO.txt`에 커밋 SHA 또는 릴리스 태그가 있다.
- [ ] Windows에서 실행 파일이 시작된다.
- [ ] 로비 화면에 진입한다.
- [ ] 전투 시작 또는 핵심 플레이 루프를 1회 확인한다.
- [ ] 시작 즉시 크래시, 치명적 콘솔 오류, 읽을 수 없는 텍스트 문제가 있으면 known issue 또는 block으로 기록한다.

## GitHub Actions artifact 기준

자동 빌드가 붙으면 artifact 이름은 zip 이름과 최대한 맞춘다.

```text
srpg-demo-windows-<short-sha>
```

초기 retention은 7일을 권장한다. 장기 보관이 필요한 빌드는 GitHub Release asset 또는 itch.io 업로드 결과를 기준으로 삼는다.

## 무료 운영 가드레일

- public repo standard GitHub-hosted runner 범위에서 먼저 검증한다.
- larger runner, self-hosted runner, 유료 스토리지, S3/CloudFront는 이번 기준의 기본 완료조건이 아니다.
- artifact/cache retention을 늘리는 PR은 비용과 보관 정책 영향을 PR 본문에 남긴다.
- itch.io 자동 업로드는 처음부터 main merge 자동 배포로 켜지 않고 `workflow_dispatch` 수동 실행 후보로 둔다.

## 관련

- Parent: #69
- Closes: #70
- Related: #32
- Related: #73

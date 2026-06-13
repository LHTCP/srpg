# PC 데모 패키징 기준

이 문서는 issue #70의 산출물이다. 목표는 Windows PC 데모를 itch.io, GitHub Release, GitHub Actions artifact 어디에 올리더라도 같은 구조로 검증할 수 있게 만드는 것이다.

## 결론

초기 PC 데모는 itch.io 프로젝트 페이지 <https://lhtcp.itch.io/lhtcp-srpg>에 올릴 수 있는 zip 하나를 기준 산출물로 둔다. WebGL과 모바일은 이번 기준의 완료조건이 아니며, PC zip이 정상적으로 내려받아지고 실행되는지를 먼저 닫는다.

## 현재 상태

이 문서는 구현 완료 보고서가 아니라 패키징 목표와 검증 기준이다. 아직 Windows zip 자동 생성, `BUILD_INFO.txt` 자동 생성, itch.io 업로드 자동화는 구현되지 않았다.

현재 확정된 사실:

- itch.io 프로젝트 페이지는 <https://lhtcp.itch.io/lhtcp-srpg>다.
- 첫 delivery 구현 목표는 Windows PC zip이다.

아직 구현 전인 설계 후보:

- `a.b.c.<build>` 파일명 자동 부여
- `BUILD_INFO.txt` 자동 생성
- development/production 채널 업로드 자동화
- GitHub Release와 production 업로드 연결

권장 산출물 이름은 다음 형식이다.

```text
srpg-demo-windows-<version>.zip
```

`<version>`은 저장소의 릴리스 메타데이터가 가진 `a.b.c`와 GitHub Actions run number를 합친 `a.b.c.<build>` 형식을 목표로 한다. 자동화가 붙기 전의 수동 배포에서는 짧은 커밋 SHA를 임시로 사용할 수 있지만, production 릴리즈컷에는 사용하지 않는다.

예시:

```text
srpg-demo-windows-9861a2f.zip
srpg-demo-windows-0.1.0.123.zip
```

임시 커밋 SHA 파일명은 development 검증용이다. production 게시 파일명은 `srpg-demo-windows-<a.b.c.build>.zip` 형식을 사용한다.

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
base_version=
build_number=
channel=
commit=
branch=
build_time_utc=
workflow_run=
source_repo=https://github.com/LHTCP/srpg
itch_project=https://lhtcp.itch.io/lhtcp-srpg
```

로컬 수동 빌드라면 `workflow_run=local`로 둔다.

## itch.io 프로젝트와 채널명

프로젝트 URL은 <https://lhtcp.itch.io/lhtcp-srpg>다.

채널은 development와 production을 분리한다.

| 채널 | 용도 |
| ---- | ---- |
| `development` | 최신 검증 빌드. main 또는 수동 workflow에서 반복 갱신할 수 있다. |
| `production` | 릴리즈컷으로 고정한 빌드. GitHub Release와 대응한다. |

기존 후보였던 `windows-demo`는 플랫폼 의미는 명확하지만, 릴리즈 단계 구분이 약하다. PC/Windows 전용 프로젝트인 동안에는 itch.io 파일 platform을 Windows로 지정하고, 채널명은 release channel의 의미가 드러나는 `development`/`production`을 우선한다.

butler 자동화를 붙일 때는 다음 형태를 후보로 둔다. 이 명령은 아직 이 저장소의 workflow로 구현되지 않았다.

```text
butler push <zip-or-folder> lhtcp/lhtcp-srpg:development
butler push <zip-or-folder> lhtcp/lhtcp-srpg:production
```

실제 계정/slug 값은 GitHub Actions variable 또는 secret에서 주입한다. 현재 사람이 확인한 프로젝트 URL 기준으로는 `lhtcp/lhtcp-srpg`를 기본 후보로 둔다.

## 수동 smoke test

PC 데모 zip을 게시하기 전에는 다음을 확인한다.

- [ ] zip 파일명이 `srpg-demo-windows-<version>.zip` 형식이다.
- [ ] zip을 풀면 최상위 폴더 하나가 나온다.
- [ ] 최상위 폴더 안에 `.exe`, `_Data` 폴더, `UnityPlayer.dll`이 있다.
- [ ] `README.txt` 또는 itch.io 페이지에 실행 방법이 있다.
- [ ] `BUILD_INFO.txt`에 `version`, `base_version`, `build_number`, `channel`, 커밋 SHA가 있다.
- [ ] Windows에서 실행 파일이 시작된다.
- [ ] 로비 화면에 진입한다.
- [ ] 전투 시작 또는 핵심 플레이 루프를 1회 확인한다.
- [ ] 시작 즉시 크래시, 치명적 콘솔 오류, 읽을 수 없는 텍스트 문제가 있으면 known issue 또는 block으로 기록한다.

## GitHub Actions artifact 기준

자동 빌드가 붙으면 artifact 이름은 zip 이름과 최대한 맞추는 것을 목표로 한다. 현재 이 문서는 artifact 생성 workflow를 추가하지 않는다.

```text
srpg-demo-windows-<a.b.c.build>
```

자동화가 붙은 뒤에는 `a.b.c.<github.run_number>`를 사용한다. 초기 retention은 7일을 권장한다. 장기 보관이 필요한 빌드는 GitHub Release asset 또는 itch.io production 업로드 결과를 기준으로 삼는다.

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

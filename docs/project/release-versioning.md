# 릴리스 버전 운영 원칙

이 문서는 배포 자동화 구현 전까지 유지할 버전 운영 원칙을 정리한다. 목표는 itch.io development/production 배포, GitHub Release, zip 파일명, 빌드 내부 표시 버전이 서로 다른 값을 말하지 않게 만드는 것이다.

## 결론

기준 버전은 `a.b.c` 형식으로 관리하고, 빌드 번호는 GitHub Actions run number를 붙인다.

```text
표시 버전 = a.b.c.<github.run_number>
예: 0.1.0.123
```

development 빌드는 같은 `a.b.c`에서 여러 build number를 허용한다. production 릴리즈컷은 이전 production보다 최소 `c` patch를 증가시킨다.

## 현재 상태

이 문서는 운영 원칙 문서다. 아직 `release.json`, 버전 주입 스크립트, production patch 증가 검증은 구현되지 않았다.

GitHub Release 생성은 수동 workflow `Windows Release asset 업로드`(`.github/workflows/windows-release-asset.yml`)로 추가했다. 다만 이 workflow는 아직 실제로 실행한 적이 없고, 게시된 GitHub Release와 tag도 없다(2026-11-14 확인). 기본 브랜치에 merge된 뒤 첫 실행으로 동작을 확인해야 한다.

현재 확정된 사실:

- itch.io 프로젝트 페이지는 <https://lhtcp.itch.io/lhtcp-srpg>다.
- zip 파일 기준 delivery 완성이 현재 최우선이다.
- `Windows PC 데모 빌드` workflow는 `bundleVersion`에 `GITHUB_RUN_NUMBER`를 붙여 `a.b.c.<build>`를 계산하고, zip 파일명과 GameCI `versioning: Custom` 입력에 쓴다(workflow 정의 기준).

아직 구현 전인 설계 후보:

- `release.json` 파일 추가
- `release.json` 기준 버전 계산과 주입(현재는 `bundleVersion` 기준)
- 최신 production tag 대비 patch 증가 검증
- itch.io production 업로드 검증(경로는 구현했지만 기본값은 꺼짐, 게시 이력 없음)

tag push 기준 자동 릴리스는 2026-11-14 `Windows Delivery 파이프라인` workflow로 추가했다. `v<a.b.c>` tag를 push하면 tag와 `bundleVersion` 일치를 확인한 뒤 빌드하고 prerelease Release를 만든다. 실행 기록이 없으므로 미검증이다. 트리거와 스위치는 [pc-demo-packaging.md](pc-demo-packaging.md)의 `자동화 범위와 트리거`를 본다.

## 기준 메타데이터

저장소 루트에 `release.json`을 두는 것을 권장한다. Node 패키지를 의미하는 `package.json`도 익숙한 선택지지만, Unity 프로젝트에서는 배포 메타데이터라는 의미가 더 직접적인 `release.json`이 낫다.

초기 후보:

```json
{
  "name": "lhtcp-srpg",
  "version": "0.1.0",
  "itch": {
    "projectUrl": "https://lhtcp.itch.io/lhtcp-srpg",
    "user": "lhtcp",
    "game": "lhtcp-srpg",
    "channels": {
      "development": "development",
      "production": "production"
    }
  }
}
```

아직 이 파일을 만들지는 않는다. zip 파일 기준 delivery가 먼저 완성된 뒤, 버전 주입과 릴리즈 검증 PR에서 추가한다.

## 채널

| 채널 | 용도 | 버전 규칙 |
| ---- | ---- | --------- |
| `development` | 최신 개발 검증 빌드 | 같은 `a.b.c`에서 build number만 증가 가능 |
| `production` | 릴리즈컷으로 고정한 빌드 | 이전 production보다 최소 patch 증가 |

현재 itch.io 프로젝트 URL은 <https://lhtcp.itch.io/lhtcp-srpg>다.

## 파일명과 표시 버전

현재 Windows zip 파일명:

```text
srpg-demo-windows-<a.b.c.build>-<short-sha>.zip
```

예시:

```text
srpg-demo-windows-0.1.0.123-9861a2f.zip
```

`BUILD_INFO.txt`에는 최소한 다음 값을 넣는다.

```text
version=0.1.0.123
base_version=0.1.0
build_number=123
channel=development
commit=<sha>
workflow_run=<url>
itch_project=https://lhtcp.itch.io/lhtcp-srpg
```

## 릴리즈컷 원칙

production 릴리즈컷은 사람이 GitHub Release를 직접 만들기보다, release workflow가 검증 후 생성하는 방향을 목표로 한다.

검증 후보:

- `release.json`의 `version`이 `a.b.c` 형식이다.
- production 릴리즈 입력 버전이 `release.json`의 `version`과 일치한다.
- 최신 production tag보다 최소 patch가 증가했다.
- GitHub Release tag는 `v<a.b.c>` 형식이다.
- zip 파일명, itch.io `--userversion`, GitHub Release 이름이 같은 `a.b.c.<run_number>`를 사용한다.

## GitHub Release asset 정책

Windows zip을 GitHub Release asset으로 올릴 때는 수동 workflow `Windows Release asset 업로드`를 사용한다. 이 workflow는 빌드를 새로 만들지 않고, 성공한 `Windows PC 데모 빌드` run의 artifact를 그대로 올린다.

| 항목 | 정책 |
| ---- | ---- |
| tag 형식 | `v<a.b.c>`. `ProjectSettings/ProjectSettings.asset`의 `bundleVersion`과 일치해야 한다. |
| 원본 | 성공으로 끝난 `Windows PC 데모 빌드` run의 artifact. artifact 이름이 `srpg-demo-windows-<a.b.c>.`로 시작해야 한다. |
| tag 대상 커밋 | 원본 run의 head sha. tag가 이미 다른 커밋을 가리키면 실패한다. |
| asset 파일명 | 빌드가 만든 이름 그대로 `srpg-demo-windows-<a.b.c.build>-<short-sha>.zip`. 이름을 바꾸지 않는다. |
| 같은 tag 재업로드 | 기본 금지. Release가 이미 있으면 실패한다. |
| overwrite | `overwrite=true`일 때만 기존 Release에 올리고 같은 이름의 asset을 덮어쓴다. Release 제목, 본문, prerelease 여부는 바꾸지 않는다. |
| prerelease | 기본값 `true`. 정식 릴리스로 만들 때만 `prerelease=false`를 지정한다. |
| Release 제목 | tag와 같은 `v<a.b.c>`. 릴리즈컷 원칙의 `a.b.c.<run_number>` 이름 통일은 아직 적용하지 않았다. |
| Release 본문 | 원본 run, 커밋 sha, artifact 이름, asset 파일명을 자동으로 적는다. |

같은 `a.b.c`에서 build number가 다른 zip은 파일명이 달라서 `overwrite=true`로 올려도 기존 asset이 지워지지 않고 함께 남는다. 이전 asset을 내려야 하면 Release 화면에서 직접 삭제한다.

수정된 빌드를 다시 배포할 때는 같은 tag에 덮어쓰기보다 `bundleVersion`의 patch를 올려 새 tag로 올리는 것을 기본으로 한다. overwrite는 업로드가 중간에 실패했거나 잘못된 파일을 올린 경우의 복구용이다.

Actions artifact 보관 기간은 7일이므로, 기간이 지난 run은 Release asset으로 올릴 수 없다. 이 경우 Windows 빌드부터 다시 만든다.

이 workflow는 최신 production tag 대비 patch 증가 여부와 원본 run이 기본 브랜치에서 실행됐는지를 검증하지 않는다. 릴리즈컷 원칙의 나머지 검증 후보는 아직 구현 전이다.

## 현재 우선순위

지금 당장은 버전 자동화 구현보다 zip 파일 기준 delivery 완성이 우선이다.

따라서 이번 원칙은 다음 작업을 막지 않는다.

- 로컬 Windows zip 산출물 생성
- itch.io development 채널 수동 업로드
- 수동 smoke test
- WebGL 즉시 플레이 PoC

버전 검증 CI는 production 배포 workflow를 만들 때 추가한다.

## 관련

- Parent: #69
- Related: #73
- Related: #74
- Related: #76
- Related: #33

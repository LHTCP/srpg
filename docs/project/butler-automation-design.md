# butler 배포 자동화 설계

이 문서는 issue #73의 산출물이다. 목표는 itch.io 업로드 자동화를 바로 켜기 전에 필요한 secret, trigger, 실패 대응, 비용 가드레일을 정리하는 것이다.

## 결론

butler 자동화는 처음부터 main merge 자동 배포로 두지 않는다. 초기에는 `workflow_dispatch` 수동 실행으로만 열고, Windows PC zip이 이미 만들어진 상태에서 itch.io에 업로드하는 얇은 job으로 시작한다.

자동화의 1차 목적은 “빌드 자동 생성”이 아니라 “검증된 PC zip을 반복 가능한 방식으로 itch.io에 올리는 것”이다.

itch.io 프로젝트 URL은 <https://lhtcp.itch.io/lhtcp-srpg>다.

butler가 유일한 게시 방법은 아니다. itch.io 웹 대시보드 수동 업로드와 itch app의 GUI 업로드도 가능하다. 다만 GitHub Actions에서 반복 가능한 자동 딜리버리를 붙이는 경로로는 itch.io 공식 CLI인 butler가 가장 단순한 1차 후보다.

## 현재 상태

이 문서는 butler 자동화 설계와 현재 구현 상태를 함께 정리한다. 현재 저장소에는 secret/variable 헬스체크와 development 수동 업로드 workflow가 있으며, `release.json`, production 릴리즈컷 검증, production 자동 업로드는 아직 구현 전이다.

현재 확정된 사실:

- itch.io 프로젝트 페이지는 <https://lhtcp.itch.io/lhtcp-srpg>다.
- butler는 itch.io 공식 CLI 업로드 도구다.
- 첫 자동화는 main 자동 배포가 아니라 수동 실행으로 둔다.
- `itch.io Delivery 설정 헬스체크` workflow는 secret/variable과 butler 설치를 확인한다.
- `itch.io Development 업로드` workflow는 이미 생성된 Windows artifact를 `development` 채널에 업로드한다.

아직 구현 전인 설계 후보:

- `release.json` 추가
- production patch 증가 검증
- production 업로드 workflow

## 권장 단계

1. 수동 웹 업로드로 첫 게시를 검증한다.
2. Windows zip 패키징 기준과 수동 게시 체크리스트를 확정한다.
3. butler를 로컬에서 한 번 실행해 채널명과 권한을 확인한다.
4. GitHub Actions secret을 설정한다.
5. `itch.io Delivery 설정 헬스체크` workflow를 수동 실행해 secret/variable과 API key 인증을 확인한다.
6. `itch.io Development 업로드` workflow로 성공한 Windows artifact를 development 채널에 업로드한다.
7. 업로드 후 itch.io 페이지에서 다운로드 smoke test를 한다.
8. main 자동 업로드 여부는 별도 decision으로 판단한다.

## secret과 variable 후보

| 이름 | 종류 | 필수 여부 | 설명 |
| ---- | ---- | --------- | ---- |
| `BUTLER_API_KEY` | secret | 필수 | butler 인증에 사용하는 itch.io API key |
| `ITCHIO_USERNAME` | variable | 권장 | itch.io 사용자 또는 조직 이름 |
| `ITCHIO_GAME` | variable | 권장 | itch.io 게임 slug |

butler 공식 문서는 CI에서 `BUTLER_API_KEY` 환경변수를 사용하라고 안내한다. 이 저장소도 GitHub secret 이름을 `BUTLER_API_KEY`로 맞춰 별도 매핑 없이 사용한다.

현재 프로젝트 URL 기준 기본 후보는 `ITCHIO_USERNAME=lhtcp`, `ITCHIO_GAME=lhtcp-srpg`다. 사용자명과 게임 slug는 공개 URL에 이미 포함되는 값이므로 repository variable로 둔다. API key는 secret으로만 둔다.

secret 값은 PR 본문, issue, 로그에 직접 쓰지 않는다.

## 채널명

development와 production을 분리한다.

| 채널 | 용도 | 트리거 |
| ---- | ---- | ------ |
| `development` | 최신 개발 검증 빌드 | 수동 workflow 또는 main 기준 수동 배포 |
| `production` | 릴리즈컷으로 고정한 빌드 | GitHub Release 생성 workflow |

PC/Windows 플랫폼 표시는 itch.io 파일 metadata에서 처리하고, butler 채널은 운영 단계 구분에 사용한다.

## 버전 운영 원칙

상세 원칙은 [release-versioning.md](release-versioning.md)를 따른다. 저장소에는 Node 패키지를 의미하는 `package.json` 대신 프로젝트 배포 메타데이터를 명시하는 `release.json`을 두는 것을 권장한다.

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

운영 원칙:

- `release.json`의 `version`은 `a.b.c` 기준 버전의 진실이다.
- GitHub Actions 빌드는 `a.b.c.<github.run_number>`를 표시 버전으로 사용한다.
- development 업로드는 같은 `a.b.c`에서 여러 build number를 허용한다.
- production 릴리즈컷은 이전 production보다 최소 `c` patch 증가를 요구한다.
- GitHub Release, itch.io production 업로드, zip 파일명, `BUILD_INFO.txt`는 같은 `a.b.c.<build>`를 사용한다.
- 이 검증은 zip delivery가 완성된 뒤 CI에 추가한다.

## workflow trigger

초기 trigger는 수동 실행만 허용한다. 현재 구현된 `itch.io Development 업로드` workflow는 Windows 빌드를 새로 만들지 않고, 입력으로 받은 기존 `Windows PC 데모 빌드` run id와 artifact 이름을 사용한다.

```yaml
on:
  workflow_dispatch:
    inputs:
      run_id:
        description: "Windows PC 데모 빌드 workflow run id"
        required: true
      artifact_name:
        description: "업로드할 GitHub Actions artifact 이름"
        required: true
      version:
        description: "선택: itch.io에 표시할 전체 버전(a.b.c.build)"
        required: false
```

권장하지 않는 초기 trigger:

- `push` to `main`
- 모든 PR마다 자동 업로드
- nightly 자동 업로드

이유는 업로드 대상 파일, 공개 범위, known issue 판단이 아직 사람 검토를 필요로 하기 때문이다.

## 업로드 명령 후보

```bash
butler push "${BUILD_PATH}" "${ITCHIO_USERNAME}/${ITCHIO_GAME}:${CHANNEL}" --userversion "${FULL_VERSION}"
```

`BUILD_PATH`는 zip 파일 또는 빌드 폴더가 될 수 있다. 첫 자동화에서는 PC 패키징 기준과 맞춘 zip 파일을 입력으로 받는 편이 가장 명확하다. 실제 명령은 butler 설치 방식과 인증 환경변수를 dry-run으로 확인한 뒤 확정한다.

## workflow 구현

`itch.io Development 업로드` workflow는 `.github/workflows/itchio-development-upload.yml`에 있다. 핵심 동작은 다음과 같다.

- `workflow_dispatch`로만 실행한다.
- `run_id`와 `artifact_name`으로 기존 Windows artifact를 다운로드한다.
- `version` 입력값이 비어 있으면 `srpg-demo-windows-<a.b.c.build>-<short-sha>` artifact 이름에서 `a.b.c.build`를 추출한다.
- `remarkablegames/setup-butler@v3`로 butler를 설치한다.
- `BUTLER_API_KEY` secret과 `ITCHIO_USERNAME`, `ITCHIO_GAME` variable을 사용해 `development` 채널로만 업로드한다.

production 업로드는 이 workflow에 옵션으로 열지 않는다. production 릴리즈컷은 version 검증, 릴리즈 노트, smoke test 결과, known issue 판단이 필요하므로 별도 workflow와 PR로 다룬다.

## 실패 대응

| 증상 | 먼저 볼 위치 | 조치 |
| ---- | ------------ | ---- |
| 인증 실패 | `butler login` 또는 `butler push` 로그 | `BUTLER_API_KEY` secret 존재와 권한 확인 |
| 게임을 찾지 못함 | push 대상 문자열 | `ITCHIO_USERNAME`, `ITCHIO_GAME`, itch.io slug 확인 |
| 파일 없음 | artifact 다운로드 또는 path | 업로드 입력 경로와 artifact 이름 확인 |
| artifact 다운로드 실패 | `Windows artifact 다운로드` step | run id, artifact 이름, 7일 retention 만료 여부 확인 |
| 업로드는 성공했지만 페이지에 안 보임 | itch.io dashboard | 채널, 공개 범위, 파일 platform 설정 확인 |

## 선행 헬스체크

`itch.io Delivery 설정 헬스체크` workflow는 자동 업로드를 수행하지 않는다. 다음만 확인한다.

- `BUTLER_API_KEY` repository secret 존재 여부
- `ITCHIO_USERNAME`, `ITCHIO_GAME` repository variable 존재 여부
- itch.io server-side API의 `credentials/info` endpoint를 통한 API key 인증 가능 여부
- `remarkablegames/setup-butler@v3` 액션을 통한 butler 설치와 버전 출력 가능 여부

이 workflow가 실패하면 butler 업로드 workflow를 추가하거나 실행하지 않는다.

## 비용 가드레일

- public repo standard GitHub-hosted runner 범위에서만 시작한다.
- larger runner는 사용하지 않는다.
- 자동 업로드가 artifact 보관 기간을 늘리는 이유가 되면 PR 본문에 근거를 남긴다.
- S3, CloudFront, 모바일 스토어, 유료 CDN은 이 설계의 일부가 아니다.
- secret 권한이 커지면 workflow를 required check로 만들기 전에 별도 리뷰한다.

## PR 리뷰 포인트

butler workflow 구현 PR에서는 다음을 셀프리뷰 또는 PR 본문에 남긴다.

- 업로드가 자동 공개인지, 수동 실행인지
- 업로드 대상 itch.io channel
- `release.json` 기준 버전과 실제 `a.b.c.<run_number>`가 일치하는지
- production 릴리즈컷에서 최소 patch 증가 검증이 필요한지
- 사용한 secret 이름과 노출 방지 방식
- runner 종류와 유료 리소스 개입 여부
- artifact retention 변경 여부
- 실패 시 어떤 로그로 원인을 확인하는지

## 관련

- Parent: #69
- Closes: #73
- Blocked by: #71
- Related: #32
- Related: #70

## 참고 문서

- [butler manual: Logging in / authentication](https://itch.io/docs/butler/login.html)
- [butler manual: Pushing builds](https://itch.io/docs/butler/pushing.html)
- [butler manual: Introduction](https://itch.io/docs/butler/)
- [itch.io butler manual: Third-party integrations](https://itch.io/docs/butler/integration.html)
- [GitHub Marketplace: setup-butler](https://github.com/marketplace/actions/setup-butler)
- [itch.io creator docs: Access control](https://itch.io/docs/creators/access-control)

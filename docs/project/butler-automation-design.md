# butler 배포 자동화 설계

이 문서는 issue #73의 산출물이다. 목표는 itch.io 업로드 자동화를 바로 켜기 전에 필요한 secret, trigger, 실패 대응, 비용 가드레일을 정리하는 것이다.

## 결론

butler 자동화는 처음부터 main merge 자동 배포로 두지 않는다. 초기에는 `workflow_dispatch` 수동 실행으로만 열고, Windows PC zip이 이미 만들어진 상태에서 itch.io에 업로드하는 얇은 job으로 시작한다.

자동화의 1차 목적은 “빌드 자동 생성”이 아니라 “검증된 PC zip을 반복 가능한 방식으로 itch.io에 올리는 것”이다.

itch.io 프로젝트 URL은 <https://lhtcp.itch.io/lhtcp-srpg>다.

## 현재 상태

이 문서는 butler 자동화 설계안이다. 아직 이 저장소에 butler workflow, `release.json`, production 릴리즈컷 검증, itch.io 자동 업로드가 구현된 것은 아니다.

현재 확정된 사실:

- itch.io 프로젝트 페이지는 <https://lhtcp.itch.io/lhtcp-srpg>다.
- butler는 itch.io 공식 CLI 업로드 도구다.
- 첫 자동화는 main 자동 배포가 아니라 수동 실행 후보로 둔다.

아직 구현 전인 설계 후보:

- `ITCHIO_API_KEY` secret 등록
- `ITCHIO_USERNAME`, `ITCHIO_GAME` variable 또는 secret 등록
- butler 설치/업로드 workflow
- `release.json` 추가
- `a.b.c.<github.run_number>` 버전 주입
- production patch 증가 검증

## 권장 단계

1. 수동 웹 업로드로 첫 게시를 검증한다.
2. Windows zip 패키징 기준과 수동 게시 체크리스트를 확정한다.
3. butler를 로컬에서 한 번 실행해 채널명과 권한을 확인한다.
4. GitHub Actions secret을 설정한다.
5. `workflow_dispatch` 수동 업로드 workflow를 추가한다.
6. 업로드 후 itch.io 페이지에서 다운로드 smoke test를 한다.
7. main 자동 업로드 여부는 별도 decision으로 판단한다.

## secret 후보

| Secret | 필수 여부 | 설명 |
| ------ | --------- | ---- |
| `ITCHIO_API_KEY` | 필수 | butler 인증에 사용하는 itch.io API key 후보 |
| `ITCHIO_USERNAME` | 권장 | itch.io 사용자 또는 조직 이름 |
| `ITCHIO_GAME` | 권장 | itch.io 게임 slug |

현재 프로젝트 URL 기준 기본 후보는 `ITCHIO_USERNAME=lhtcp`, `ITCHIO_GAME=lhtcp-srpg`다. `ITCHIO_USERNAME`과 `ITCHIO_GAME`은 secret 대신 repository variable로 둘 수도 있다. 공개되어도 되는 값인지 애매하면 secret으로 시작한다.

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

초기 trigger는 수동 실행만 허용하는 것을 목표로 한다. 아래 YAML은 설계 스케치이며 현재 저장소에 추가된 workflow가 아니다.

```yaml
on:
  workflow_dispatch:
    inputs:
      build_path:
        description: "업로드할 Windows zip 또는 폴더 경로"
        required: true
      version:
        description: "a.b.c 기준 버전. build 번호는 GitHub run number를 사용한다."
        required: true
      channel:
        description: "development 또는 production"
        required: true
        default: "development"
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

## workflow 골격

아래는 설계 예시이며, 그대로 복사해 쓰는 구현안이 아니다. 실제 workflow PR에서는 butler 설치 방식, artifact 다운로드 경로, 인증 환경변수 이름을 공식 문서와 dry-run으로 다시 확인한다.

```yaml
name: itch.io 수동 업로드

on:
  workflow_dispatch:
    inputs:
      artifact_name:
        description: "다운로드할 GitHub Actions artifact 이름"
        required: true
      version:
        description: "a.b.c 기준 버전"
        required: true
      channel:
        description: "development 또는 production"
        required: true
        default: "development"

jobs:
  upload:
    runs-on: ubuntu-latest
    steps:
      - name: butler 설치
        run: |
          curl -L -o butler.zip https://broth.itch.ovh/butler/linux-amd64/LATEST/archive/default
          unzip butler.zip -d butler
          echo "$PWD/butler" >> "$GITHUB_PATH"

      - name: butler 인증 정보 존재 확인
        env:
          BUTLER_API_KEY: ${{ secrets.ITCHIO_API_KEY }}
        run: |
          if [ -z "$BUTLER_API_KEY" ]; then
            echo "ITCHIO_API_KEY secret이 비어 있습니다."
            exit 1
          fi

      - name: 업로드
        env:
          BUTLER_API_KEY: ${{ secrets.ITCHIO_API_KEY }}
          ITCHIO_USERNAME: ${{ secrets.ITCHIO_USERNAME }}
          ITCHIO_GAME: ${{ secrets.ITCHIO_GAME }}
          CHANNEL: ${{ inputs.channel }}
          FULL_VERSION: ${{ inputs.version }}.${{ github.run_number }}
        run: |
          butler push "./dist" "${ITCHIO_USERNAME}/${ITCHIO_GAME}:${CHANNEL}" --userversion "${FULL_VERSION}"
```

실제 구현 PR에서는 artifact 다운로드 step, zip 경로, butler 설치 공식 권장 방식, `BUTLER_API_KEY` 환경변수 동작을 다시 확인한다. 이 예시가 바뀌는 경우에는 PR 본문이나 코드 라인 셀프리뷰에 근거를 남긴다.

## 실패 대응

| 증상 | 먼저 볼 위치 | 조치 |
| ---- | ------------ | ---- |
| 인증 실패 | `butler login` 또는 `butler push` 로그 | `ITCHIO_API_KEY` secret 존재와 권한 확인 |
| 게임을 찾지 못함 | push 대상 문자열 | `ITCHIO_USERNAME`, `ITCHIO_GAME`, itch.io slug 확인 |
| 파일 없음 | artifact 다운로드 또는 path | 업로드 입력 경로와 artifact 이름 확인 |
| 업로드는 성공했지만 페이지에 안 보임 | itch.io dashboard | 채널, 공개 범위, 파일 platform 설정 확인 |

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

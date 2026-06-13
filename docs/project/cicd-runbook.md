# CI/CD 장애 대응 Runbook

이 문서는 GitHub Actions 기반 Unity 테스트·빌드·배포 파이프라인이 실패했을 때의 1차 대응 절차를 정리한다.

## 기본 원칙

- 실패한 workflow run의 URL, 브랜치, 커밋 SHA, 실패 job/step 이름을 먼저 기록한다.
- 로그에서 최초 실패 지점을 찾는다. 뒤따르는 artifact 업로드 실패나 정리 step 실패는 2차 증상일 수 있다.
- secret 값은 로그, 이슈, PR 본문에 직접 쓰지 않는다.
- 이번 무료 우선 스프린트에서는 PC/Windows artifact를 주 Delivery 경로로 두고, GitHub Actions public repo standard runner와 짧은 retention의 Actions artifact/GitHub Release asset 안에서 먼저 닫는다.
- 비용 또는 보관 정책에 영향을 줄 수 있는 cache, artifact, runner 변경은 PR 본문이나 셀프리뷰에 근거를 남긴다.
- Apple Developer Program, Google Play Console, S3+CloudFront처럼 명시적 비용 또는 사용량 과금이 있는 경로는 별도 decision/approval 없이 기본 완료조건에 넣지 않는다.
- 배포 실패가 반복되면 임시 우회보다 재현 가능한 문서·workflow 수정 PR을 우선 만든다.

## 빠른 분류

| 증상 | 먼저 볼 위치 | 대표 원인 |
| ---- | ------------ | --------- |
| LFS 다운로드 실패 | `LFS 오브젝트 가져오기` step | secret 누락, 커스텀 LFS 서버 접근 불가, 권한 만료 |
| Unity license 실패 | Unity test/build action step | `UNITY_LICENSE` 또는 `UNITY_SERIAL` 누락·만료 |
| 테스트 실패 | Unity test result, Editor log | 컴파일 오류, 테스트 회귀, 에셋 import 오류 |
| WebGL 빌드 실패 | WebGL build step, Editor log | 플랫폼 모듈 누락, 메모리 부족, Player Settings 오류 |
| Windows 빌드 실패 | Windows build step, Editor log | 플랫폼 모듈/runner 차이, 경로 문제, 실행 파일 패키징 누락 |
| artifact/cache 실패 | upload-artifact/cache step | 경로 없음, 보관 기간/용량 문제, cache key 충돌 |

## 공통 확인 절차

1. GitHub Actions run에서 실패한 job을 연다.
2. 가장 먼저 빨간색으로 실패한 step을 확인한다.
3. 해당 step의 로그를 펼치고 `error`, `fatal`, `exception`, `license`, `lfs`, `artifact`, `cache` 키워드를 검색한다.
4. 실패가 Unity 내부에서 발생했다면 Editor log 또는 test result artifact가 업로드됐는지 확인한다.
5. 같은 브랜치에서 재실행할지, main 최신화 후 재실행할지 결정한다.

## LFS 실패

### 선행 헬스체크

Windows/WebGL 빌드 workflow를 실행하기 전에 `LFS 서버 헬스체크` workflow를 수동 실행해 GitHub-hosted runner가 사설 LFS 서버에 접근할 수 있는지 확인한다. 이 workflow는 Unity 빌드 없이 `.lfsconfig`, repository secret, `git lfs pull`만 검증한다.

public repo standard GitHub-hosted runner 실행 자체는 무료 범위지만, `git lfs pull`은 사설 LFS 서버의 트래픽, 계정, 접근성 한도를 사용한다. LFS 서버는 항시 가동을 전제로 하더라도 외부 runner에서 접근 가능한지, secret이 유효한지, 트래픽 한도에 문제가 없는지는 별도로 확인한다.

### 대표 증상

```text
fatal: could not read Username for 'https://...'
batch response: Git credentials ... not found.
Error downloading object
```

또는:

```text
404
Object does not exist on the server
```

### 확인할 것

- repository secret `LFS_ACCOUNT_ID`가 설정되어 있는지 확인한다.
- repository secret `LFS_ACCOUNT_PASSWORD`가 설정되어 있는지 확인한다.
- `.lfsconfig`의 커스텀 LFS 서버 URL이 현재도 유효한지 확인한다.
- GitHub-hosted runner에서 커스텀 LFS 서버에 접근 가능한지 확인한다.
- 실패 로그가 GitHub LFS URL을 보고 있는지, 커스텀 LFS URL을 보고 있는지 구분한다.

### 조치

- secret이 없다면 `Settings > Secrets and variables > Actions > Repository secrets`에 `LFS_ACCOUNT_ID`, `LFS_ACCOUNT_PASSWORD`를 추가한다.
- `actions/checkout`에서 `lfs: true`로 너무 이른 LFS pull이 발생한다면 checkout 이후 별도 `git lfs pull` step으로 분리한다.
- 인증은 가능하지만 다운로드가 실패하면 커스텀 LFS 서버의 계정 권한, 토큰 만료, 방화벽, IP 차단, 트래픽 한도를 확인한다.
- GitHub Actions에서 LFS를 받지 않는 방향이 필요하면 LFS-free CI 이슈를 열고, 테스트·빌드가 대용량 에셋 없이 가능한지 분리 검토한다.

### 에스컬레이션 기준

- 동일 secret으로 로컬에서는 성공하지만 GitHub Actions에서만 실패한다.
- 커스텀 LFS 서버가 외부 네트워크에서 접근 불가하다.
- 대용량 에셋 다운로드 시간이 CI 전체 시간을 지배한다.

## Unity license 실패

### 대표 증상

```text
Missing Unity License File and no Serial was found.
```

또는:

```text
No valid Unity Editor license found.
```

### 확인할 것

- repository secret `UNITY_LICENSE`가 설정되어 있는지 확인한다.
- `UNITY_LICENSE`를 쓰지 않는 방식이면 repository secret `UNITY_SERIAL`이 설정되어 있는지 확인한다.
- GameCI 또는 Unity action이 현재 workflow에서 어떤 license 방식을 기대하는지 확인한다.
- Unity Personal/Pro/Enterprise 라이선스 정책과 활성화 한도를 확인한다.

### 조치

- 라이선스 파일 방식을 쓰면 `UNITY_LICENSE` secret에 라이선스 파일 내용을 설정한다.
- serial 방식을 쓰면 `UNITY_SERIAL`과 필요한 계정 secret 조합을 workflow 문서에 맞춰 설정한다.
- license secret을 바꾼 뒤에는 같은 브랜치에서 수동 workflow를 재실행한다.
- 라이선스 실패가 테스트 전에 발생했다면 test result artifact가 없는 것은 정상적인 2차 증상으로 본다.

### 에스컬레이션 기준

- license secret이 설정되어 있는데도 활성화 한도 또는 seat 문제로 실패한다.
- 개인 계정 라이선스를 CI에 넣어야 하는 상황이 생긴다.
- macOS runner 또는 모바일 서명처럼 별도 비용·계정 정책이 함께 걸린다.

## 테스트 실패

### 대표 증상

```text
Compilation failed
Tests failed
RunFinished: Failed
```

### 확인할 것

- 실패가 컴파일 단계인지, 테스트 assertion 단계인지 구분한다.
- Unity Editor log와 test result XML이 artifact로 업로드됐는지 확인한다.
- 로컬 Unity Editor의 Test Runner에서 같은 EditMode/PlayMode 테스트가 재현되는지 확인한다.
- 최근 PR에서 테스트 대상 로직, asmdef, package, scene, prefab, ProjectSettings 변경이 있었는지 확인한다.

### 조치

- 컴파일 오류라면 테스트 수정 전에 컴파일 복구 이슈 또는 PR을 우선 처리한다.
- assertion 실패라면 실패 테스트명, 기대값, 실제값을 이슈나 PR 코멘트에 정리한다.
- 에셋 import 오류라면 LFS 누락, meta GUID 충돌, package 누락 여부를 함께 확인한다.
- CI에서만 실패하면 경로 대소문자, OS 차이, 시간 의존성, 비결정적 테스트 순서를 의심한다.

### 에스컬레이션 기준

- 로컬 Run All은 통과하지만 CI에서만 반복 실패한다.
- 테스트가 씬·프리팹·ProjectSettings에 의존해서 원인 범위가 넓다.
- 실패가 현재 PR 범위를 넘어 main 컴파일 상태와 연결된다.

## WebGL 빌드 실패

### 대표 증상

```text
Build failed
WebGL module is not installed
Failed running Unity build
```

### 확인할 것

- 사용 중인 Unity 버전에 WebGL build support가 설치된 runner 이미지를 쓰는지 확인한다.
- Build Settings에 필수 씬이 등록되어 있는지 확인한다.
- Player Settings의 WebGL 설정이 현재 Unity 버전과 호환되는지 확인한다.
- GitHub Pages 무료 배포 경로에서는 서버의 `Content-Encoding` 헤더를 세밀하게 제어하기 어렵기 때문에 `Decompression Fallback` 설정이 켜져 있는지 확인한다.
- 빌드 산출물 경로가 artifact 업로드 또는 정적 호스팅 step의 path와 일치하는지 확인한다.
- GitHub Pages 배포는 저장소 `Settings > Pages`의 source가 GitHub Actions로 되어 있어야 한다.

### 조치

- 플랫폼 모듈 누락이면 GameCI 이미지 또는 action 설정의 target platform을 확인한다.
- 메모리 부족이면 runner 종류, compression 설정, 빌드 캐시 전략을 검토한다.
- 산출물 경로가 비어 있으면 Unity build step의 output path와 upload-artifact path를 맞춘다.
- 정적 호스팅 배포가 실패하면 먼저 WebGL artifact 생성이 성공했는지 분리해서 확인한다.
- Pages 권한 오류가 나면 workflow의 `pages: write`, `id-token: write` 권한과 저장소 Pages 설정을 확인한다.

### 에스컬레이션 기준

- 빌드는 성공하지만 호스팅된 페이지에서 로딩이 실패한다.
- GitHub Pages, Cloudflare Pages, S3 등 배포 대상 선택이 아직 결정되지 않았다.
- compression, CORS, MIME type처럼 호스팅 설정이 필요한 오류가 발생한다.

## Windows 빌드 실패

### 대표 증상

```text
Build failed
StandaloneWindows64
File not found
```

### 확인할 것

- 초기 버전 플랫폼은 PC로 고정되어 있으므로, Windows artifact 생성 여부를 WebGL/모바일 배포보다 우선 확인한다.
- Windows 빌드를 Linux runner에서 cross-build할지, Windows runner에서 빌드할지 결정되어 있는지 확인한다.
- target platform이 `StandaloneWindows64`인지 확인한다.
- 빌드 결과 폴더에 실행 파일과 필요한 데이터 폴더가 함께 생성됐는지 확인한다.
- zip 패키징 step이 실제 output path를 보고 있는지 확인한다.

### 조치

- 플랫폼/runner 전략이 불명확하면 implementation PR로 바로 고치지 말고 decision 이슈를 먼저 닫는다.
- output path가 바뀌었다면 build step과 artifact step을 함께 수정한다.
- Windows 전용 경로 구분자 문제는 PowerShell 또는 bash 중 어느 shell에서 실행되는지 확인한다.
- 실행 파일은 생겼지만 실행이 안 되면 로컬 Windows에서 artifact를 내려받아 smoke test를 수행한다.

### 에스컬레이션 기준

- Windows runner 사용으로 비용·시간 증가가 예상된다.
- Release asset 배포와 Actions artifact 배포 중 선택이 필요하다.
- 빌드 산출물에 포함해야 할 추가 런타임 파일이 불명확하다.
- 모바일 배포 요구가 다시 등장하면 현재 PC 우선 범위를 벗어나는 별도 decision 이슈로 분리한다.

## Artifact 문제

### 대표 증상

```text
No files were found with the provided path
Artifact upload failed
```

### 확인할 것

- 실패한 artifact step이 진짜 원인인지, 이전 step 실패 뒤 산출물이 없어 생긴 2차 증상인지 확인한다.
- `path`가 실제 빌드/test output 경로와 일치하는지 확인한다.
- `if: always()`로 실패 로그를 올리는 step인지 확인한다.
- artifact 보관 기간이 필요한 기간보다 길거나 짧지 않은지 확인한다.

### 조치

- 테스트나 빌드 전에 실패했다면 artifact 없음 경고는 우선순위를 낮춘다.
- 로그·테스트 결과를 보존해야 한다면 Unity output 경로를 고정하고 `if: always()`를 유지한다.
- 불필요하게 큰 artifact는 압축 범위와 retention days를 줄인다.
- 배포물 artifact와 디버깅용 artifact는 이름을 분리한다.

### 에스컬레이션 기준

- artifact 크기가 커져 업로드 시간이 CI 병목이 된다.
- 보관 기간 변경이 비용 또는 플랜 한도에 영향을 줄 수 있다.
- 배포용 artifact와 임시 진단 artifact의 책임 경계가 모호하다.

## Cache 문제

### 대표 증상

```text
Cache not found
Failed to save cache
Cache size is over the limit
```

### 확인할 것

- cache miss인지, cache 저장 실패인지 구분한다.
- key가 Unity 버전, platform, lockfile, package 상태를 충분히 반영하는지 확인한다.
- Library cache가 너무 커져 저장 실패하는지 확인한다.
- 오래된 cache가 깨진 import 상태를 재사용하고 있지 않은지 확인한다.

### 조치

- cache miss만 발생하고 workflow가 성공하면 장애로 보지 않는다.
- 반복되는 import 오류가 cache와 관련 있어 보이면 cache key를 의도적으로 갱신한다.
- cache 크기가 과도하면 Library 전체가 아니라 필요한 범위만 캐시할 수 있는지 검토한다.
- cache 정책을 바꾸는 PR에는 실행 시간 개선 기대와 비용/용량 리스크를 함께 적는다.

### 에스컬레이션 기준

- cache 때문에 성공/실패가 비결정적으로 갈린다.
- cache 용량이 저장소 운영 한도나 비용 검토 대상이 된다.
- 플랫폼별 cache key 분리가 필요하지만 build matrix 구조가 아직 정리되지 않았다.

## 배포 실패 후 기록 템플릿

이슈나 PR 코멘트에 다음 형식으로 남긴다.

````md
## 실패 위치

- Workflow:
- Branch:
- Commit:
- Job:
- Step:

## 최초 오류

```text
여기에 secret 값을 제외한 핵심 오류만 붙인다.
```

## 1차 판단

- 분류:
- 원인 후보:
- 재실행 여부:

## 다음 조치

- [ ] secret/설정 확인
- [ ] 로컬 재현
- [ ] workflow 수정 PR
- [ ] decision/research 이슈 필요 여부 확인
````

## 관련 문서

- [github-governance.md](github-governance.md): GitHub Actions 비용·브랜치 보호 운영 기준
- [workflow.md](workflow.md): PR, 셀프리뷰, 스크립트 작성 기준
- [setup.md](setup.md): 로컬 개발 환경과 재현성 기준

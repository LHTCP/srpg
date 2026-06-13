# itch.io 게시·임베드 방식 조사

이 문서는 issue #71의 산출물이다. 목표는 초기 PC 데모를 itch.io로 게시할 때 필요한 선택지와 제약을 정리하고, 후속 패키징·수동 게시·자동화 작업이 바로 이어질 수 있게 만드는 것이다.

## 결론

초기 itch.io 프로젝트 URL은 <https://lhtcp.itch.io/lhtcp-srpg>다.

초기 배포 경로는 Windows PC zip을 itch.io downloadable build로 올리는 방식이 가장 단순하다. 다만 itch.io 페이지 안에서 바로 플레이하는 경험은 WebGL/HTML5 빌드가 담당하므로, 플레이어 접근성을 높이는 우선 후보로 별도 PoC를 잡을 만하다.

## 현재 상태

이 문서는 조사 결과와 추천 순서를 정리한 문서다. 아직 이 저장소에 itch.io 업로드 workflow, butler workflow, WebGL itch.io 업로드 workflow가 구현된 것은 아니다.

현재 확정된 사실:

- itch.io 프로젝트 페이지는 <https://lhtcp.itch.io/lhtcp-srpg>다.
- Windows PC zip delivery 완성이 최우선이다.

아직 구현 전인 설계 후보:

- WebGL/HTML5 즉시 플레이 PoC
- butler 기반 수동 업로드 workflow
- development/production 채널 운영
- `a.b.c.<github.run_number>` 버전 강제

권장 순서는 다음과 같다.

1. Windows zip 패키징 기준을 확정한다.
2. itch.io 프로젝트 페이지를 비공개 또는 제한 공개 상태로 유지한다.
3. `development` 채널에 PC zip을 수동 업로드한다.
4. 페이지 최소 메타데이터와 실행 안내를 채운다.
5. 로컬 Unity WebGL 빌드가 가능한지 PoC로 확인한다.
6. WebGL 빌드가 작고 안정적이면 HTML Game/iframe 플레이를 development 채널에 붙인다.
7. 첫 게시가 안정화되면 butler 수동 workflow를 검토한다.

## 공식 문서 기준 요약

### butler

itch.io의 butler는 게임 빌드를 빠르고 안정적으로 업로드하기 위한 CLI다. 공식 문서도 자동 빌드/배포 파이프라인에 통합하기 쉽다고 설명한다.

이번 프로젝트에서는 butler를 즉시 필수 자동화로 두지 않고, 다음 단계의 후보로 둔다.

- 수동 로컬 업로드: 사람이 `butler push`를 실행한다.
- 수동 GitHub Actions 업로드: `workflow_dispatch`에서만 실행한다.
- 자동 main 배포: 초기에는 하지 않는다.

기준 명령 후보:

```text
butler push <build-path> lhtcp/lhtcp-srpg:development
butler push <build-path> lhtcp/lhtcp-srpg:production
```

### HTML5/WebGL

itch.io는 HTML, JavaScript, CSS 기반 프로젝트를 브라우저에서 직접 플레이할 수 있는 HTML game으로 업로드할 수 있다. 공식 문서 기준으로는 게임 페이지의 Kind를 `HTML Game`으로 설정하고 ZIP을 업로드하는 흐름이다.

Unity WebGL은 이 경로에 올릴 수 있는 후보이며, “다운로드 없이 바로 플레이”라는 itch.io의 장점을 살리려면 빠르게 PoC를 해볼 가치가 있다. 단, production 완료조건으로 바로 묶지는 않고 다음 조건을 만족할 때 승격한다.

- WebGL 빌드가 로컬 Unity Editor에서 재현 가능하다.
- itch.io HTML5 업로드 제한, 압축, 로딩 시간이 실제 산출물과 충돌하지 않는다.
- 브라우저 입력, 해상도, 폰트 표시가 현재 프로토타입 플레이를 막지 않는다.
- Windows PC zip을 대체하기보다 development 플레이 링크로 먼저 검증한다.

### Widget/iframe 임베드

itch.io는 외부 페이지에 붙일 수 있는 widget/embed 경로를 제공한다. 이 프로젝트에서는 README에 바로 iframe을 넣기보다, 다음 용도로만 검토한다.

- 프로젝트 소개 페이지에서 itch.io 데모 카드 노출
- 외부 문서나 홈페이지에서 다운로드/플레이 버튼 제공
- 최신 플레이 방법 문서(#41)에 itch.io 페이지 링크 보강

GitHub README는 임베드 iframe 렌더링이 제한될 수 있으므로, README에는 일반 링크를 두는 방식이 더 안전하다.

## 게시 상태 선택

첫 게시는 공개 상태보다 비공개 또는 제한 공개를 추천한다.

| 상태 | 추천 용도 |
| ---- | --------- |
| Draft/비공개 | 페이지 메타데이터, 파일 업로드, 실행 안내를 내부에서 검증할 때 |
| 제한 공개 | 링크를 받은 사람만 플레이하게 하고 싶을 때 |
| 공개 | smoke test와 known issue 정리가 끝난 뒤 |

정확한 상태명과 UI 문구는 itch.io 대시보드에서 확인한다. 저장소 문서에는 “누가 링크를 볼 수 있는지”와 “어떤 빌드가 최신인지”를 중심으로 기록한다.

## 페이지 최소 메타데이터

첫 페이지에는 아래 정보만 있어도 된다.

- 제목: `SRPG Prototype` 또는 프로젝트 공식 이름
- 짧은 설명: 전술 RPG 프로토타입, PC 데모
- 프로젝트 URL: <https://lhtcp.itch.io/lhtcp-srpg>
- 플랫폼: Windows
- 다운로드 파일: `srpg-demo-windows-<version>.zip`
- 조작법: 마우스/키보드 기준 최소 입력
- 실행 안내: zip 압축 해제 후 exe 실행
- known issue: GitHub issue 링크 또는 간단 목록
- 버전: 커밋 SHA 또는 릴리스 태그
- 문의/피드백: GitHub issue 또는 지정 채널

## 채널명

| 채널 | 판단 |
| ---- | ---- |
| `development` | 최신 개발 검증 빌드. PC zip과 WebGL PoC 모두 이 채널에서 먼저 확인한다. |
| `production` | 릴리즈컷으로 고정한 빌드. GitHub Release와 대응한다. |
| `windows-demo` | Windows 플랫폼 의미는 명확하지만 development/production 구분이 약해 보류한다. |

PC/Windows 전용인 동안 플랫폼 정보는 itch.io 파일 platform과 페이지 본문에서 표현하고, 채널명은 운영 단계 구분을 우선한다.

## 버전 운영 원칙

- 저장소의 릴리스 메타데이터 파일이 `a.b.c` 기준 버전의 진실이다.
- development 빌드는 `a.b.c.<github.run_number>`를 사용한다.
- production 릴리즈컷은 최소 `c` patch 증가를 요구한다.
- production GitHub Release와 itch.io production 업로드는 같은 `a.b.c.<build>`를 사용한다.
- 이 원칙은 먼저 문서화하고, zip 파일 기준 delivery가 완성된 뒤 CI 검증으로 강제한다.
- 현재 PR은 버전 파일이나 CI 검증을 추가하지 않는다.

## 비용과 secret 가드레일

itch.io 페이지와 수동 업로드 자체를 유료 인프라로 가정하지는 않는다. 다만 자동화를 붙일 때는 secret과 계정 권한이 생긴다.

- GitHub Actions 자동 업로드에는 itch.io API key 또는 butler 인증 정보가 필요하다.
- public repo에서 secret 값은 PR 본문, 로그, 이슈에 직접 쓰지 않는다.
- 자동 업로드 workflow는 처음에는 `workflow_dispatch`로만 둔다.
- GitHub runner는 public repo standard runner 무료 범위 안에서 먼저 검증한다.
- 유료 runner, S3, CloudFront, 모바일 스토어 배포는 이번 범위 밖이다.

## 후속 작업

- #70: Windows PC zip 패키징 기준 확정
- #72: itch.io 수동 게시 체크리스트 작성
- #73: butler 배포 자동화 후보 설계
- #41: 최신 버전 플레이 방법 문서화

## 참고 문서

- [butler manual](https://itch.io/docs/butler/)
- [Uploading HTML5 games](https://itch.io/docs/creators/html5)
- [Creator FAQ](https://itch.io/docs/creators/faq)
- [itch.io API overview](https://itch.io/docs/api/overview)

## 관련

- Parent: #69
- Closes: #71
- Blocks: #72
- Blocks: #73

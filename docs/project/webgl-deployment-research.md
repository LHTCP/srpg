# WebGL 배포 대상 조사

이 문서는 Unity WebGL 빌드를 "비개발자가 최신 버전을 바로 플레이할 수 있는 링크"로 배포하기 위한 후보를 비교한다. 검토 대상은 GitHub Pages, Cloudflare Pages, S3+CloudFront이며, 기준은 public repo 비용, Unity WebGL 압축/파일 제약, 배포 속도/캐시, 커스텀 도메인이다.

## 결론

이번 스프린트의 최소 Delivery 조건은 "추가 유료 계정 없이 최신 PC 빌드를 플레이하거나 내려받을 수 있는 상태"로 둔다. 초기 버전 플랫폼은 PC로 고정되었으므로 WebGL은 1차 플랫폼이 아니라 무료 공유 링크 후보로 다룬다. S3+CloudFront는 비용 승인 이후의 승격 후보로만 둔다.

WebGL이 필요해지는 경우에는 GitHub Pages로 시작하는 것을 추천한다. GitHub 저장소와 Actions만으로 닫힌 루프를 만들기 쉽고, public repo에서는 진입 비용이 가장 낮다. 단, GitHub Pages는 Unity WebGL의 `Content-Encoding` 세부 제어가 약하므로 초기에는 Unity의 Decompression Fallback을 켠 빌드를 대상으로 삼는 것이 안전하다.

운영성 있는 공개 플레이 링크는 비용 승인을 전제로 S3+CloudFront를 후보로 둔다. Unity WebGL의 gzip/Brotli 네이티브 압축 헤더, 캐시 TTL, invalidation, 커스텀 도메인/TLS를 가장 명확하게 제어할 수 있기 때문이다. 대신 AWS 계정, 사용량 과금, 권한, 배포 스크립트, CloudFront invalidation 운영이 추가되므로 이번 무료 우선 스프린트의 완료조건에는 넣지 않는다.

Cloudflare Pages는 편리한 프리뷰/배포 경험이 장점이지만, Free plan 기준 단일 파일 25 MiB 제한이 Unity WebGL 빌드 산출물과 충돌할 가능성이 높다. 빌드 산출물이 충분히 작거나 큰 파일을 R2 등으로 분리할 때만 1차 후보로 올린다.

## 비교표

| 항목 | GitHub Pages | Cloudflare Pages | S3+CloudFront |
| ---- | ---- | ---- | ---- |
| public repo 비용 | public repo의 GitHub Free에서 사용 가능. Pages 사이트 1 GB, 월 100 GB soft bandwidth 등 제한 확인 필요 | Free plan에서 월 500 builds, 20,000 files, 단일 asset 25 MiB 제한 | S3 storage/request/data transfer, CloudFront request/data transfer/invalidation 비용 관리 필요 |
| WebGL 압축 | 임의 `Content-Encoding` 헤더 제어가 어렵다. Decompression Fallback 빌드가 안전 | `_headers` 파일로 헤더 제어 가능. 단, 단일 asset 25 MiB 제한이 병목 가능 | 파일별 `Content-Encoding`, `Content-Type`, cache metadata를 가장 직접적으로 제어 가능 |
| 배포 속도 | GitHub Actions에서 Pages artifact 업로드로 단순. 첫 도입 빠름 | Wrangler direct upload 또는 Git 연동 가능. preview deployment 경험 좋음 | S3 sync + CloudFront invalidation이 필요해 설정은 무겁지만 운영 제어력 높음 |
| 캐시 | Pages 캐시는 세부 제어가 제한적. 파일명 해시/버전 디렉터리 전략 필요 | `_headers`로 일부 캐시 헤더 제어 가능 | CloudFront TTL, Cache-Control, invalidation을 명시적으로 운영 가능 |
| 커스텀 도메인 | GitHub Pages custom domain과 HTTPS 지원 | 프로젝트당 custom domain 제한 있음. Free plan 기준 100개 | CloudFront alternate domain name과 TLS 인증서 필요 |
| 추천 위치 | PC artifact를 보조하는 무료 공유 링크 | preview deployment 또는 작은 WebGL 빌드 | 안정 배포, 큰 WebGL 파일, 압축/캐시 최적화가 필요한 공개 링크 |

## 후보별 메모

### GitHub Pages

GitHub Pages는 public repository에서 사용할 수 있고, 공식 문서 기준 Published Pages site 1 GB, 월 100 GB soft bandwidth, 배포 10분 timeout, 시간당 10 builds soft limit 등의 제한이 있다. GitHub Actions custom workflow로 빌드/배포하면 Pages 자체 build limit 일부는 피할 수 있지만, 사이트 크기와 bandwidth soft limit은 계속 봐야 한다.

Unity WebGL 관점의 핵심 리스크는 서버 헤더 제어다. Unity 문서는 gzip/Brotli 네이티브 압축을 쓰려면 빌드 시 선택한 압축 방식과 맞는 `Content-Encoding` 헤더가 필요하다고 설명한다. GitHub Pages에서는 `_headers` 같은 정적 헤더 설정을 기본 제공하지 않으므로, 초기는 Decompression Fallback을 켜서 `.unityweb` 파일로 배포하는 전략이 안전하다. 이 방식은 서버 설정 부담을 줄이지만 loader가 커지고 로딩 효율이 떨어질 수 있다.

추천 사용 방식은 `main` 또는 release tag에서 WebGL 빌드 후 Pages artifact로 배포하고, 최신 플레이 링크를 README에 노출하는 것이다. 파일명에 해시가 붙는 Unity 산출물은 장기 캐시를 활용하기 좋고, `index.html`은 짧은 캐시 또는 버전 디렉터리로 우회하는 방식을 검토한다.

### Cloudflare Pages

Cloudflare Pages는 Git 연동과 Direct Upload 모두 가능하고, preview deployment가 강하다. Free plan 기준 월 500 builds, 20,000 files, 단일 파일 25 MiB 제한이 있다. Unity WebGL은 `.data`, `.wasm`, `.bundle`이 25 MiB를 넘기 쉬우므로 이 제한이 가장 큰 판단 기준이다.

헤더 측면에서는 `_headers` 파일로 정적 asset 응답 헤더를 추가/override할 수 있어 Unity WebGL 압축 헤더를 다루기 쉽다. 다만 파일 크기 제한을 넘는 경우 Cloudflare Pages 단독 배포는 막힐 수 있고, Cloudflare 문서도 큰 파일은 R2 public bucket 같은 대안을 고려하라고 안내한다.

추천 사용 방식은 빌드 산출물 크기를 먼저 측정한 뒤, 모든 단일 파일이 25 MiB 이하이면 preview deployment 후보로 검토하는 것이다. 그 이상이면 Pages 단독 대신 R2 또는 S3+CloudFront 쪽으로 넘기는 편이 단순하다.

### S3+CloudFront

S3+CloudFront는 세 후보 중 가장 무겁지만 Unity WebGL 운영 요구사항에는 가장 잘 맞는다. S3에 정적 파일을 올리고 CloudFront로 배포하면 파일별 metadata로 `Content-Encoding`, `Content-Type`, `Cache-Control`을 명시할 수 있고, CloudFront cache behavior에서 TTL을 제어할 수 있다. CloudFront는 alternate domain name을 쓰려면 해당 도메인을 포함하는 유효한 TLS 인증서가 필요하다.

비용은 무료라고 가정하지 않는다. AWS 공식 가격표 기준 S3는 storage, request, data transfer 비용 요소가 있고, CloudFront도 request/data transfer 사용량과 플랜/allowance를 확인해야 한다. CloudFront invalidation은 경로 단위로 과금될 수 있으므로, 매 배포마다 `/*` invalidation에 의존하기보다 버전 디렉터리 또는 해시 파일명을 우선한다.

추천 사용 방식은 `s3://<bucket>/builds/<version>/`에 산출물을 업로드하고, `latest/` 또는 작은 manifest만 갱신하는 구조다. `index.html`은 짧게 캐시하고, 해시가 붙은 `.wasm`, `.data`, `.js`, texture/bundle 파일은 길게 캐시한다.

## WebGL 압축 판단

Unity 공식 문서 기준 gzip은 기본 옵션이고 Brotli보다 파일은 크지만 빌드가 빠르며, Brotli는 압축률이 좋지만 release build 반복 시간이 늘 수 있다. 네이티브 브라우저 압축 해제를 쓰려면 `.gz`는 `Content-Encoding: gzip`, `.br`은 `Content-Encoding: br` 응답 헤더가 필요하다. 서버 헤더 설정이 어렵거나 불가능하면 Decompression Fallback을 켤 수 있지만, loader 크기와 로딩 효율을 대가로 치른다.

따라서 배포 후보는 압축 방식과 함께 결정한다.

- GitHub Pages: Decompression Fallback 우선.
- Cloudflare Pages: `_headers`로 네이티브 gzip/Brotli 가능성을 검토하되 25 MiB 파일 제한을 먼저 확인.
- S3+CloudFront: 네이티브 gzip/Brotli와 장기 캐시 운영 후보.

## 추천 로드맵

1. WebGL 빌드 산출물 크기와 최대 단일 파일 크기를 측정한다.
2. 단일 파일이 25 MiB를 넘는지 확인한다.
3. PC/Windows artifact Delivery가 막히거나 공유 링크가 필요하면 GitHub Pages + Decompression Fallback로 WebGL 링크를 만든다.
4. GitHub Pages 링크가 PC 우선 흐름을 보조하는지 확인한다.
5. 로딩 속도, 파일 크기, 캐시 문제가 확인되고 비용 승인이 있으면 S3+CloudFront로 승격한다.
6. Cloudflare Pages는 preview deployment 가치가 크거나 산출물이 25 MiB 이하일 때만 별도 PoC를 진행한다.

## 후속 이슈 제안

- WebGL 빌드 산출물 크기 측정 스크립트 추가
- GitHub Pages WebGL 배포 PoC
- S3+CloudFront 배포 PoC와 비용 가드레일 문서화(비용 승인 이후)
- WebGL 압축 방식 결정: Decompression Fallback, gzip, Brotli
- 최신 플레이 링크 갱신 방식 결정: main 자동 배포, 수동 workflow, release tag

## 참고 문서

- [GitHub Pages limits](https://docs.github.com/en/pages/getting-started-with-github-pages/github-pages-limits)
- [GitHub Pages custom domain](https://docs.github.com/en/pages/configuring-a-custom-domain-for-your-github-pages-site)
- [Cloudflare Pages limits](https://developers.cloudflare.com/pages/platform/limits/)
- [Cloudflare Pages custom headers](https://developers.cloudflare.com/pages/configuration/headers/)
- [Cloudflare Pages Direct Upload with CI](https://developers.cloudflare.com/pages/how-to/use-direct-upload-with-continuous-integration/)
- [Unity Manual: Deploy a Web application](https://docs.unity3d.com/Manual/webgl-deploying.html)
- [Unity Manual: Server configuration code samples](https://docs.unity3d.com/Manual/webgl-server-configuration-code-samples.html)
- [Amazon S3 pricing](https://aws.amazon.com/s3/pricing/)
- [Amazon CloudFront pricing](https://aws.amazon.com/cloudfront/pricing/)
- [CloudFront alternate domain names](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/CNAMEs.html)
- [CloudFront cache behavior settings](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/DownloadDistValuesCacheBehavior.html)
- [CloudFront invalidation pricing note](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/PayingForInvalidation.html)

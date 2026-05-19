# iOS/TestFlight 배포 조사

## 질문

iOS/TestFlight 배포를 자동화하려면 어떤 계정, runner, 서명, 비용 조건이 필요한가?

## 결론

iOS/TestFlight는 모바일 배포 중 가장 비용과 운영 부담이 크다. Apple Developer Program은 공식 문서 기준 연 US$99 멤버십이 필요하고, 자동화에는 macOS runner와 signing 운영도 따라온다. 이번 CI/CD 1차 마일스톤에서는 무료 Delivery 경로인 WebGL/Windows를 먼저 완성하고, iOS는 Apple Developer 계정, App Store Connect 앱, 인증서/provisioning profile, macOS runner 비용을 확인한 뒤 별도 마일스톤으로 진행하는 편이 안전하다.

추천 순서:

1. WebGL Pages 또는 Windows artifact로 “무료 바로 플레이” 경로를 먼저 만든다.
2. Apple Developer Program 참여 비용과 bundle identifier를 확정한다.
3. 로컬 Mac에서 Xcode archive/TestFlight 업로드가 되는지 먼저 검증한다.
4. 이후 GitHub Actions macOS runner 또는 자체 Mac runner 자동화를 검토한다.

## 필요한 계정과 설정

- Apple Developer Program 멤버십
- App Store Connect 앱 등록
- Bundle identifier 확정
- Signing certificate
- Provisioning profile
- App Store Connect API key
- GitHub Actions repository secret
- `APP_STORE_CONNECT_API_KEY_ID`
- `APP_STORE_CONNECT_API_ISSUER_ID`
- `APP_STORE_CONNECT_API_KEY_P8`
- signing certificate/provisioning profile 관련 secret

secret 이름은 후속 구현 PR에서 확정한다. 인증서와 profile은 만료/갱신 주기가 있으므로 운영 문서가 반드시 필요하다.

## TestFlight 테스터 구분

| 구분 | 용도 | 운영 메모 |
| --- | --- | --- |
| Internal testers | App Store Connect 접근 권한이 있는 내부 인원 테스트 | 빠른 검증에 적합 |
| External testers | 외부 베타 테스터 테스트 | Apple 검토/테스터 그룹 운영 고려 |

TestFlight는 내부/외부 테스터 초대가 가능하지만, 외부 테스터 운영은 앱 메타데이터와 검토 흐름까지 함께 고려해야 한다.

## runner 선택지

| 선택지 | 장점 | 단점 | 추천 |
| --- | --- | --- | --- |
| GitHub-hosted macOS runner | 설정이 단순하고 GitHub Actions와 통합 쉬움 | macOS runner 비용/시간 부담 | 조사 후 결정 |
| 자체 Mac runner | Apple 서명/키체인 관리가 쉬울 수 있음 | 장비 운영 부담 | 장기 후보 |
| 수동 로컬 업로드 | 초기 검증이 빠름 | 자동화 아님 | 0단계 검증 |

GitHub Actions의 macOS runner는 Linux/Windows보다 비용 영향이 크므로, 자동화 전에 빌드 시간과 실행 빈도를 반드시 추정한다.

## CI/CD 구현 메모

fastlane은 App Store Connect API key를 사용해 Apple 배포 작업을 자동화할 수 있다. 다만 Unity iOS build는 Xcode project 생성, archive, signing, upload 단계가 분리되므로, 한 PR에서 끝내기보다 다음처럼 나눈다.

- Unity iOS Xcode project build
- signing assets/secret 문서화
- Xcode archive 생성
- TestFlight upload
- tester 배포/검증 문서화

## 비용과 리스크

- Apple Developer Program 연 US$99 비용이 필요하다.
- macOS runner 비용과 대기 시간이 생길 수 있다.
- signing certificate/profile 만료와 키체인 처리가 실패 지점이 된다.
- public repo에서 secret 노출은 GitHub가 마스킹하지만, fork PR 실행 정책을 특히 조심한다.

## 후속 이슈 후보

- Apple 계정/Bundle ID 준비 체크리스트
- iOS signing secret 문서화
- Unity iOS Xcode project build workflow
- TestFlight upload workflow
- TestFlight internal tester 운영 문서

## 참고 문서

- Apple Developer Program: https://developer.apple.com/programs/
- TestFlight: https://developer.apple.com/testflight
- App Store Connect API: https://developer.apple.com/documentation/AppStoreConnectAPI
- Xcode App Store Connect upload: https://help.apple.com/xcode/mac/current/en.lproj/dev442d7f2ca.html
- fastlane App Store Connect API key: https://docs.fastlane.tools/actions/app_store_connect_api_key/
- GitHub Actions runner pricing: https://docs.github.com/billing/reference/actions-minute-multipliers

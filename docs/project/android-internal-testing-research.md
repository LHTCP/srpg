# Android 내부 테스트 배포 조사

## 질문

Google Play internal testing 또는 internal app sharing을 이 프로젝트의 모바일 배포 경로로 사용할 수 있는가?

## 결론

Android는 모바일 배포 중 iOS보다 먼저 검토할 만하다. 다만 WebGL/Windows보다 선행하기에는 keystore, Play Console 앱 등록, tester opt-in, AAB 업로드 자동화가 필요하므로 Phase 4의 별도 결정 이슈에서 진행 여부를 확정한다.

추천 순서:

1. CI에서 Android AAB를 수동 workflow artifact로 먼저 생성한다.
2. keystore와 package name/version code 정책을 문서화한다.
3. Play Console 내부 테스트 트랙 업로드는 fastlane 연결 확인 후 추가한다.

## 필요한 계정과 저장소 설정

- Google Play Console 개발자 계정
- Play Console 앱 등록 및 package name 확정
- Android App Bundle(AAB) 서명용 keystore
- GitHub Actions repository secret
- `ANDROID_KEYSTORE_BASE64`
- `ANDROID_KEYSTORE_PASSWORD`
- `ANDROID_KEY_ALIAS`
- `ANDROID_KEY_PASSWORD`
- Google Play API service account JSON

secret 이름은 후속 구현 PR에서 확정한다. 실제 계정 비밀번호 대신 최소 권한 service account와 배포 전용 keystore를 사용한다.

## 배포 선택지

| 선택지 | 장점 | 단점 | 추천 |
| --- | --- | --- | --- |
| Actions artifact로 AAB 보관 | 가장 단순하고 PR 단위 검증에 좋음 | 비개발자가 바로 설치하기 어려움 | 1차 |
| Google Play internal testing | tester opt-in 후 Play Store 경유 설치 가능 | Play Console, 서명, 심사/검토 흐름 필요 | 2차 |
| Google Play internal app sharing | 빠른 공유에 적합 | 조직 정책/권한 확인 필요 | 보조 |

## tester opt-in 흐름

Google Play internal testing은 테스터가 opt-in 링크를 통해 테스트에 참여한 뒤 Play Store에서 설치하는 흐름이다. 테스터는 일반 검색으로 내부 테스트 앱을 찾지 못할 수 있으므로, 문서에는 opt-in 링크와 테스트 참여 절차를 함께 남겨야 한다.

## CI/CD 구현 메모

GameCI Android 배포 문서는 fastlane을 사용해 Google Play internal track으로 AAB를 업로드하는 흐름을 안내한다. 이 프로젝트에서는 곧바로 업로드 자동화로 가지 말고, 먼저 AAB build artifact를 안정화한 뒤 fastlane `upload_to_play_store(track: 'internal')` 계열 설정을 추가한다.

## 비용과 리스크

- public repo의 standard GitHub-hosted runner 실행 시간 자체는 무료 범위지만 artifact/cache/storage는 별도 한도와 비용 검토가 필요하다.
- Android 빌드는 Unity import와 Gradle 단계 때문에 WebGL보다 시간이 길 수 있다.
- keystore 유출은 치명적이므로 secret과 권한 범위를 좁힌다.
- Play Console 앱 등록과 내부 테스트 링크 운영은 개발 외 운영 작업을 동반한다.

## 후속 이슈 후보

- Android AAB build workflow 추가
- Android signing secret 문서화
- Google Play service account 연결 검증
- Google Play internal track 업로드 workflow 추가
- Android tester opt-in 링크 문서화

## 참고 문서

- Google Play internal testing: https://support.google.com/googleplay/android-developer/answer/9845334
- GameCI Android deployment: https://game.ci/docs/github/deployment/android/
- fastlane Google Play internal app sharing: https://docs.fastlane.tools/actions/upload_to_play_store_internal_app_sharing/

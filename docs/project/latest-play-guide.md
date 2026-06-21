# 최신 Windows 데모 플레이 가이드

이 문서는 최신 PC/Windows 데모를 어디서 내려받고 어떻게 실행하는지 설명한다. 현재 GitHub Actions artifact는 개발자와 내부 검증자가 빌드 산출물을 확인하는 임시 경로이고, 비개발자와 외부 테스터에게 안내할 기본 경로는 itch.io 다운로드 페이지다.

## 현재 기준

- 최신 빌드 생성 workflow: GitHub Actions `Windows PC 데모 빌드`
- 내부 검증 산출물: `srpg-demo-windows-<a.b.c.build>-<short-sha>` artifact
- 외부 테스터 다운로드 경로: <https://lhtcp.itch.io/lhtcp-srpg>
- artifact 보관 기간: 7일
- 초기 플랫폼: PC/Windows
- WebGL/모바일: 현재 최신 플레이 경로의 완료조건이 아님

`Windows PC 데모 빌드` workflow는 main 기준 수동 실행으로 시작한다. main merge마다 자동 게시하지 않는 이유는 최신 빌드를 실제 공개해도 되는지, known issue 안내가 필요한지 아직 사람이 판단해야 하기 때문이다.

## 권장 다운로드 경로

| 대상 | 권장 경로 | 이유 |
| ---- | --------- | ---- |
| 외부 테스터, 비개발자 | itch.io 다운로드 | GitHub UI, Actions run, artifact 보관 기간을 몰라도 받을 수 있다. |
| 개발자, 배포 담당자, 내부 검증자 | GitHub Actions artifact | 어떤 커밋과 workflow run에서 만들어졌는지 바로 추적할 수 있다. |
| production 릴리즈 | itch.io production 또는 GitHub Release asset | 장기 보관과 릴리스 기록을 함께 남긴다. |

GitHub 저장소는 public이지만, Actions artifact는 GitHub UI와 7일 보관 기간에 의존한다. 그래서 사용자-facing 배포면으로는 itch.io를 우선한다.

## GitHub Actions artifact로 받기

이 절차는 개발자, 배포 담당자, 내부 검증자를 위한 경로다. 외부 테스터에게는 itch.io 페이지를 먼저 안내한다.

1. GitHub 저장소 <https://github.com/LHTCP/srpg>를 연다.
2. `Actions` 탭으로 이동한다.
3. 왼쪽 workflow 목록에서 `Windows PC 데모 빌드`를 선택한다.
4. 최신 성공 run을 연다.
5. 페이지 하단의 `Artifacts` 영역에서 `srpg-demo-windows-<version>-<short-sha>` artifact를 다운로드한다.
6. GitHub가 내려주는 artifact zip을 압축 해제한다.
7. 내부의 실제 게임 zip인 `srpg-demo-windows-<version>-<short-sha>.zip`을 다시 압축 해제한다.
8. 압축 해제된 폴더에서 Windows 실행 파일을 실행한다.

GitHub Actions artifact는 다운로드용 컨테이너 zip을 한 번 더 감쌀 수 있다. itch.io나 GitHub Release에 게시할 때는 바깥 artifact zip이 아니라 내부의 실제 게임 zip을 사용한다.

## 실행 전 확인

- zip을 풀면 최상위 폴더 하나가 나온다.
- 최상위 폴더 안에 `.exe`, `_Data` 폴더, `UnityPlayer.dll`이 있다.
- `README.txt`와 `BUILD_INFO.txt`가 포함되어 있다.
- `BUILD_INFO.txt`의 `commit`, `version`, `workflow_run`이 게시하려는 빌드와 일치한다.

Windows 보안 경고가 뜰 수 있다. 이 경우 `BUILD_INFO.txt`와 workflow run URL을 확인해 빌드 출처와 버전이 의도한 값인지 먼저 확인한다.

## smoke test

최신 빌드를 받았으면 장시간 QA가 아니라 다음 최소 확인만 먼저 수행한다.

- [ ] 실행 파일이 시작된다.
- [ ] 로비 화면에 진입한다.
- [ ] 전투 시작 또는 핵심 플레이 루프를 1회 확인한다.
- [ ] 시작 즉시 크래시가 없다.
- [ ] 텍스트가 읽기 어려울 정도로 깨지거나 누락되지 않는다.
- [ ] Windows 보안 경고, 백신 경고, 차단 메시지가 있으면 기록한다.

문제가 있으면 GitHub issue에 빌드 버전, 커밋 SHA, 실행 환경, 재현 절차를 남긴다.

## itch.io에서 받기

itch.io 페이지 <https://lhtcp.itch.io/lhtcp-srpg>는 비개발자와 외부 테스터에게 안내할 기본 배포면이다. 다만 GitHub Actions artifact가 성공했다고 해서 itch.io에 자동으로 파일이 보이는 것은 아니다.

itch.io에서 다운로드가 보이려면 다음 중 하나가 필요하다.

- 지시자 또는 배포 담당자가 실제 게임 zip을 itch.io 프로젝트 파일로 수동 업로드한다.
- `itch.io Development 업로드` workflow가 성공한 Windows artifact의 실제 게임 zip을 `lhtcp/lhtcp-srpg:development` 채널에 업로드한다.

`itch.io Development 업로드` workflow를 실행하기 전에는 `itch.io Delivery 설정 헬스체크` workflow를 수동 실행해 `BUTLER_API_KEY` secret, `ITCHIO_USERNAME` variable, `ITCHIO_GAME` variable이 준비됐는지 확인한다.

`itch.io Development 업로드` workflow는 Windows 빌드를 새로 만들지 않는다. 먼저 `Windows PC 데모 빌드` workflow가 성공해야 하며, 업로드 실행 시 해당 run id와 artifact 이름을 입력한다. artifact 보관 기간이 지나면 다시 Windows 빌드부터 만들어야 한다.

수동 업로드 절차는 [itchio-manual-publishing-checklist.md](itchio-manual-publishing-checklist.md)를 따른다. butler 자동화 설계는 [butler-automation-design.md](butler-automation-design.md)를 따른다.

## 최신 버전 판단

최신 development 빌드는 다음 순서로 판단한다.

1. 외부 안내 기준: itch.io `development` 파일
2. 빌드 원본 확인 기준: GitHub Actions `Windows PC 데모 빌드`의 최신 성공 run
3. production 릴리즈가 필요할 때만 GitHub Release 또는 itch.io `production` 파일

현재 production 릴리즈컷 자동화는 구현 전이다. production으로 안내할 빌드는 별도 릴리스 기록과 smoke test 결과가 있어야 한다.

## 현재 제한

- Actions artifact 보관 기간은 7일이므로 오래된 development 빌드는 사라질 수 있다.
- itch.io development 업로드는 수동 workflow로 수행한다. production 자동 업로드는 아직 별도 구현 PR이 필요하다.
- WebGL 즉시 플레이는 후속 PoC 후보이며, 현재 최신 플레이 경로는 Windows zip 다운로드다.
- Android/iOS 배포는 크로스플랫폼 지원 결정 전까지 보류한다.

## 관련 문서

- [pc-demo-packaging.md](pc-demo-packaging.md): Windows zip 구조와 패키징 기준
- [itchio-manual-publishing-checklist.md](itchio-manual-publishing-checklist.md): itch.io 수동 게시 절차
- [butler-automation-design.md](butler-automation-design.md): itch.io 자동 업로드 설계
- [release-checklist.md](release-checklist.md): 배포 전후 검증 기준

# itch.io 수동 게시 체크리스트

이 문서는 issue #72의 산출물이다. 목표는 자동화 없이도 처음 온 사람이 Windows PC 데모를 itch.io에 안전하게 게시하고, 어떤 버전이 올라갔는지 추적할 수 있게 만드는 것이다.

## 전제

- 초기 플랫폼은 PC/Windows다.
- 첫 게시 대상은 Windows zip이다.
- itch.io 프로젝트 URL은 <https://lhtcp.itch.io/lhtcp-srpg>다.
- 모바일 배포는 이번 체크리스트의 범위가 아니다.
- WebGL/HTML5 플레이는 즉시 플레이 경험을 위한 후속 PoC 후보이며, PC zip 게시를 막지 않는다.
- 자동 업로드는 아직 필수가 아니며, 첫 게시와 smoke test는 수동으로 수행한다.

## 현재 상태

이 문서는 수동 게시를 위한 체크리스트다. Windows zip 산출물은 GitHub Actions `Windows PC 데모 빌드` workflow에서 생성할 수 있다. itch.io 업로드, butler 자동화, production 릴리즈컷 자동화는 별도 단계다.

현재 확정된 사실:

- itch.io 프로젝트 페이지는 <https://lhtcp.itch.io/lhtcp-srpg>다.
- 첫 게시 절차는 수동 검증을 전제로 한다.
- 게시할 파일은 GitHub Actions artifact zip 안의 내부 실제 게임 zip이다.

아직 구현 전인 설계 후보:

- development/production 채널 자동 업로드
- production patch 증가 자동 검증

## 게시 전 준비

- [ ] 게시 대상 커밋 SHA를 확인한다.
- [ ] 게시 대상 브랜치 또는 태그를 확인한다.
- [ ] `Windows PC 데모 빌드` workflow의 최신 성공 run에서 artifact를 다운로드한다.
- [ ] GitHub artifact zip을 푼 뒤 내부 실제 게임 zip을 찾는다.
- [ ] 자동화가 붙은 빌드라면 Windows zip 파일명이 `srpg-demo-windows-<a.b.c.build>-<short-sha>.zip` 형식인지 확인한다.
- [ ] 자동화 전 임시 검증이라면 커밋 SHA 파일명을 사용했는지, production용이 아님을 기록한다.
- [ ] zip을 풀면 최상위 폴더 하나가 나오는지 확인한다.
- [ ] 최상위 폴더에 `.exe`, `_Data` 폴더, `UnityPlayer.dll`이 있는지 확인한다.
- [ ] `README.txt` 또는 itch.io 페이지에 실행 안내가 있는지 확인한다.
- [ ] known issue로 안내할 문제가 있으면 GitHub issue 링크를 준비한다.
- [ ] 이번 게시가 공개인지, 제한 공개인지, 비공개 검증인지 결정한다.

## itch.io 페이지 생성

itch.io 대시보드에서 <https://lhtcp.itch.io/lhtcp-srpg> 프로젝트를 연다.

- [ ] 제목이 `LHTCP SRPG` 또는 현재 프로젝트명과 맞는지 확인한다.
- [ ] 짧은 설명에 “PC/Windows demo” 성격을 명시한다.
- [ ] Kind 또는 플랫폼 설정에서 Windows 다운로드 빌드임을 드러낸다.
- [ ] 가격은 초기 데모 기준 무료 또는 다운로드 제한 정책에 맞게 설정한다.
- [ ] 공개 범위는 첫 검증 전에는 비공개 또는 제한 공개로 둔다.
- [ ] 페이지 URL slug가 `lhtcp-srpg`인지 확인한다.
- [ ] 프로젝트가 아직 프로토타입이면 페이지 본문에도 프로토타입임을 명시한다.

## 파일 업로드

- [ ] 내부 실제 게임 zip인 `srpg-demo-windows-<a.b.c.build>-<short-sha>.zip`을 업로드한다.
- [ ] 업로드 파일의 플랫폼을 Windows로 표시한다.
- [ ] 다운로드 이름 또는 설명에 버전, 커밋 SHA, 날짜를 적는다.
- [ ] 기존 파일을 교체하는 경우, 이전 빌드가 필요한지 먼저 확인한다.
- [ ] 채널 기능을 쓰는 경우 개발 검증 빌드는 `development` 채널에 둔다.
- [ ] production 릴리즈컷 빌드는 GitHub Release와 대응되는 `production` 채널에 둔다. 아직 자동 릴리즈컷 workflow는 구현되지 않았다.

## 페이지 최소 본문

itch.io 페이지 본문에는 최소한 다음 내용을 포함한다.

```md
## SRPG Prototype

Windows PC 데모입니다.

프로젝트 페이지: https://lhtcp.itch.io/lhtcp-srpg

### 실행 방법

1. zip 파일을 다운로드합니다.
2. 원하는 폴더에 압축을 풉니다.
3. 실행 파일을 실행합니다.

### 현재 버전

- Version:
- Channel:
- Commit:

### Known issue

- GitHub issue:
```

## 게시 후 smoke test

게시 후에는 실제 사용자가 받는 경로를 기준으로 확인한다.

- [ ] itch.io 페이지에서 파일을 다운로드할 수 있다.
- [ ] 다운로드한 zip 파일명이 게시 대상과 일치한다.
- [ ] 압축 해제 후 실행 파일이 시작된다.
- [ ] 로비 화면에 진입한다.
- [ ] 전투 시작 또는 핵심 플레이 루프를 1회 확인한다.
- [ ] Windows 보안 경고, 바이러스 검사 경고, 차단 메시지가 있으면 기록한다.
- [ ] 텍스트가 읽기 어려울 정도로 깨지거나 누락되지 않는지 확인한다.
- [ ] smoke test 환경을 기록한다. 예: Windows 버전, GPU, 입력 장치.

## 게시 기록 템플릿

GitHub issue, PR 댓글, 릴리스 노트 중 하나에 다음 기록을 남긴다.

```md
## itch.io 게시 기록

- 게시 일시:
- itch.io 페이지:
- itch.io 채널: development / production
- 공개 범위:
- 파일:
- 버전:
- 커밋 SHA:
- 업로드 방식: 수동 웹 업로드 / butler / GitHub Actions
- smoke test 환경:
- smoke test 결과:
- known issue:
- 후속 이슈:
```

## 공개 전 확인

비공개 또는 제한 공개 상태에서 공개로 바꾸기 전에는 다음을 다시 본다.

- [ ] 페이지 제목과 설명이 실제 빌드 상태를 과장하지 않는다.
- [ ] 다운로드 파일이 최신 의도 버전이다.
- [ ] production 게시라면 `a.b.c` patch가 이전 production보다 증가했다.
- [ ] 알려진 치명적 문제는 페이지에 안내되어 있다.
- [ ] 피드백을 받을 GitHub issue 또는 연락 경로가 있다.
- [ ] 모바일 지원처럼 아직 보류한 내용을 지원한다고 쓰지 않았다.
- [ ] 유료 결제, 후원, 판매 설정을 켜는 경우 별도 결정이 있다.

## 실패 시 중단 기준

다음 중 하나라도 있으면 공개 게시를 중단한다.

- 다운로드 파일이 잘못된 커밋 또는 잘못된 플랫폼이다.
- zip 압축 해제 후 실행 파일이 없다.
- 실행 즉시 크래시가 발생한다.
- 로비 또는 핵심 플레이 흐름에 진입할 수 없다.
- secret, 계정 정보, 로컬 절대경로가 페이지나 파일에 노출되었다.
- known issue로 안내하기에는 사용자가 플레이를 시작할 수 없다.

## 관련

- Parent: #69
- Closes: #72
- Blocked by: #70
- Blocked by: #71
- Blocks: #41

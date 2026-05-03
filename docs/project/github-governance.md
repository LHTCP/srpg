# GitHub 운영 기준

이 문서는 GitHub 이슈, PR, 브랜치 보호, required checks 운영 기준을 다룬다.

## 기본 원칙

- 로컬 개발 방식은 자유롭게 둔다.
- 기본 브랜치 통합은 PR 기준으로 관리한다.
- PR은 작게 유지하고, 한 PR은 한 주제를 다룬다.
- 템플릿은 인지 부하를 낮추기 위한 기본값이며, 간단한 작업에서는 불필요한 칸을 짧게 처리해도 된다.

## 이슈 템플릿

- 작업 이슈: 구현, 문서, CI, 운영 작업을 작게 추적한다.
- 버그 리포트: 재현 절차와 기대/실제 동작을 분리해 기록한다.
- 엄브렐라 이슈는 큰 목표와 PR 분할을 추적한다.

## PR 템플릿

PR에는 다음을 남긴다.

- 요약
- 변경 내용
- 검증
- Unity 확인 필요 여부
- 셀프리뷰
- 관련 이슈

Unity 에디터 확인이 필요한 경우에는 실제 확인 여부와 남은 확인 항목을 분리해서 적는다.

## 브랜치 보호 권장값

`main` 브랜치에는 다음 설정을 권장한다.

- PR 없이 직접 push 금지
- required status checks 사용
- 빠른 PR 체크(`PR 빠른 체크 / 저장소 구조 및 문서 검증`)를 required check로 지정
- 브랜치 최신화 요구는 초기에는 선택으로 둔다
- 관리자에게도 동일 규칙 적용 여부는 저장소 운영자가 결정한다
- squash merge는 로컬/팀 선호 설정에 따라 선택한다

## required checks 단계

초기 required checks:

- `PR 빠른 체크 / 저장소 구조 및 문서 검증`

Unity 테스트 워크플로가 안정화된 뒤 추가 후보:

- EditMode 테스트
- PlayMode 테스트

플랫폼 빌드 워크플로는 무겁기 때문에 초기에는 required check로 두지 않는다. 릴리스 또는 수동 실행 기준을 먼저 정한다.

`Unity EditMode 테스트` 워크플로는 초기에는 수동 실행(`workflow_dispatch`)으로 둔다. GitHub Actions의 `workflow_dispatch`는 워크플로 파일이 기본 브랜치에 있어야 수동 실행 이벤트를 받으므로, 신규 워크플로 PR에서는 병합 후 첫 수동 실행을 확인한다. `UNITY_LICENSE` 시크릿 설정, 최초 실행 성공 여부, 실행 시간, artifact/cache 사용량을 확인한 뒤 required check 승격 여부를 결정한다.

## 비용 리뷰 포인트

public 저장소의 standard GitHub-hosted runner는 무료로 사용할 수 있지만, 다음 변경은 비용 또는 플랜 한도에 영향을 줄 수 있다.

- larger runner 사용
- artifact 업로드
- cache 용량 증가
- artifact/cache 보관 기간 증가
- 빌드 매트릭스 확대
- GitHub LFS bandwidth/storage를 쓰도록 LFS 원격 설정 변경

이 항목을 바꾸는 PR은 셀프리뷰 코멘트나 PR 본문에 비용 검토 내용을 남긴다.

현재 저장소는 `.lfsconfig`로 커스텀 LFS 서버를 사용한다. 이 경우 GitHub Actions standard runner 실행 시간 자체와 별개로, LFS 다운로드는 커스텀 서버의 네트워크 접근성, 인증, 트래픽 한도를 함께 확인한다.

GitHub-hosted runner에서 커스텀 LFS 서버를 사용하려면 repository secret `LFS_USERNAME`, `LFS_PASSWORD`를 설정한다. `LFS_PASSWORD`에는 계정 비밀번호 대신 LFS 서버에서 발급한 토큰 또는 최소 권한 자격 증명을 우선 사용한다.

## Unity 에디터 확인 경계

다음 변경은 가능하면 Unity 에디터 확인 항목을 PR에 남긴다.

- 씬 추가/삭제/이름 변경
- 프리팹 또는 인스펙터 참조 변경
- ScriptableObject나 `.asset` 직렬화 값 변경
- 플랫폼 빌드 설정 변경

문서, GitHub 템플릿, 빠른 CI, 순수 C# 로직 검토는 에디터 없이 진행할 수 있다.

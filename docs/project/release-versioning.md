# 릴리스 버전 운영 원칙

이 문서는 배포 자동화 구현 전까지 유지할 버전 운영 원칙을 정리한다. 목표는 itch.io development/production 배포, GitHub Release, zip 파일명, 빌드 내부 표시 버전이 서로 다른 값을 말하지 않게 만드는 것이다.

## 결론

기준 버전은 `a.b.c` 형식으로 관리하고, 빌드 번호는 GitHub Actions run number를 붙인다.

```text
표시 버전 = a.b.c.<github.run_number>
예: 0.1.0.123
```

development 빌드는 같은 `a.b.c`에서 여러 build number를 허용한다. production 릴리즈컷은 이전 production보다 최소 `c` patch를 증가시킨다.

## 현재 상태

이 문서는 운영 원칙 문서다. 아직 `release.json`, 버전 주입 스크립트, GitHub Release 생성 workflow, production patch 증가 검증은 구현되지 않았다.

현재 확정된 사실:

- itch.io 프로젝트 페이지는 <https://lhtcp.itch.io/lhtcp-srpg>다.
- zip 파일 기준 delivery 완성이 현재 최우선이다.

아직 구현 전인 설계 후보:

- `release.json` 파일 추가
- `a.b.c.<github.run_number>` 자동 생성
- Unity build version 주입
- GitHub Release tag 검증
- itch.io production 업로드 검증

## 기준 메타데이터

저장소 루트에 `release.json`을 두는 것을 권장한다. Node 패키지를 의미하는 `package.json`도 익숙한 선택지지만, Unity 프로젝트에서는 배포 메타데이터라는 의미가 더 직접적인 `release.json`이 낫다.

초기 후보:

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

아직 이 파일을 만들지는 않는다. zip 파일 기준 delivery가 먼저 완성된 뒤, 버전 주입과 릴리즈 검증 PR에서 추가한다.

## 채널

| 채널 | 용도 | 버전 규칙 |
| ---- | ---- | --------- |
| `development` | 최신 개발 검증 빌드 | 같은 `a.b.c`에서 build number만 증가 가능 |
| `production` | 릴리즈컷으로 고정한 빌드 | 이전 production보다 최소 patch 증가 |

현재 itch.io 프로젝트 URL은 <https://lhtcp.itch.io/lhtcp-srpg>다.

## 파일명과 표시 버전

현재 Windows zip 파일명:

```text
srpg-demo-windows-<a.b.c.build>-<short-sha>.zip
```

예시:

```text
srpg-demo-windows-0.1.0.123-9861a2f.zip
```

`BUILD_INFO.txt`에는 최소한 다음 값을 넣는다.

```text
version=0.1.0.123
base_version=0.1.0
build_number=123
channel=development
commit=<sha>
workflow_run=<url>
itch_project=https://lhtcp.itch.io/lhtcp-srpg
```

## 릴리즈컷 원칙

production 릴리즈컷은 사람이 GitHub Release를 직접 만들기보다, release workflow가 검증 후 생성하는 방향을 목표로 한다.

검증 후보:

- `release.json`의 `version`이 `a.b.c` 형식이다.
- production 릴리즈 입력 버전이 `release.json`의 `version`과 일치한다.
- 최신 production tag보다 최소 patch가 증가했다.
- GitHub Release tag는 `v<a.b.c>` 형식이다.
- zip 파일명, itch.io `--userversion`, GitHub Release 이름이 같은 `a.b.c.<run_number>`를 사용한다.

## 현재 우선순위

지금 당장은 버전 자동화 구현보다 zip 파일 기준 delivery 완성이 우선이다.

따라서 이번 원칙은 다음 작업을 막지 않는다.

- 로컬 Windows zip 산출물 생성
- itch.io development 채널 수동 업로드
- 수동 smoke test
- WebGL 즉시 플레이 PoC

버전 검증 CI는 production 배포 workflow를 만들 때 추가한다.

## 관련

- Parent: #69
- Related: #73
- Related: #74
- Related: #76

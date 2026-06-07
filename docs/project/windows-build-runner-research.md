# Windows 빌드 runner 전략 조사

이 문서는 GitHub issue #30의 산출물이다. 목표는 Windows 플레이어 빌드를 어떤 runner에서 만들고, 어떤 수준까지 자동 검증할지 결정하기 위한 근거를 정리하는 것이다.

## 결론

초기 추천안은 다음과 같다.

1. 이번 무료 우선 스프린트에서는 GitHub Actions public repo standard runner와 Actions artifact/GitHub Release asset 안에서 닫히는 Windows Delivery를 목표로 한다.
2. 1차 Windows 빌드는 `ubuntu-latest` + `game-ci/unity-builder@v4` + `targetPlatform: StandaloneWindows64`로 시작한다.
3. Windows 실행 검증은 빌드 job과 분리하여 `windows-latest` smoke job에서 artifact를 내려받아 짧게 실행한다.
4. 프로젝트가 IL2CPP Windows 빌드로 전환되면 빌드 job도 `windows-2022` 또는 `windows-latest`로 옮긴다.
5. Windows 빌드는 초기 required check로 두지 않고, 수동 실행 또는 release 후보 브랜치/태그 기준으로 먼저 안정화한다.

이 접근은 public 저장소의 standard runner 무료 범위를 활용하면서도, 실제 Windows 실행 가능성은 Windows runner에서 확인하는 절충안이다.

## 비교표

| 선택지 | 장점 | 단점 | 추천 용도 |
| ---- | ---- | ---- | ---- |
| Linux runner에서 Windows target 빌드 | `ubuntu-latest`가 빠르고 기존 Unity 테스트 workflow와 운영 방식이 가깝다. GameCI 기본 예시도 `StandaloneWindows64`를 Linux runner에서 빌드하는 흐름을 제공한다. | IL2CPP Windows 빌드는 host OS 제약이 있다. 빌드 산출물이 Windows에서 실제 실행되는지는 별도 검증이 필요하다. | 초기 Mono/기본 Windows artifact 생성 |
| Windows runner에서 Windows target 빌드 | host OS와 target OS가 일치한다. IL2CPP 전환 시 GameCI 권장 조건과 맞는다. 빌드 후 같은 job 또는 후속 job에서 실행 검증을 붙이기 쉽다. | Linux보다 queue/실행 시간이 길 수 있고, Unity import/cache가 더 무거울 수 있다. | IL2CPP Windows 빌드, release 후보 빌드 |
| Self-hosted Windows runner | 커스텀 LFS 서버, Unity 설치, 캐시, 실행 검증 환경을 통제하기 쉽다. | runner 운영, 보안 업데이트, secret 노출면, 장애 대응 부담이 생긴다. public repo라도 runner 머신이 신뢰 경계 안에 들어온다. | GitHub-hosted runner에서 LFS/라이선스/성능 문제가 반복될 때 |

## GameCI Windows target 지원 방식

GameCI `unity-builder`는 `targetPlatform` 입력으로 Unity build target을 받는다. 공식 문서의 complete example에는 `StandaloneWindows`와 `StandaloneWindows64`가 포함되어 있고, 빌드 결과는 기본적으로 `build` 폴더에 생성한 뒤 `actions/upload-artifact`로 올리는 흐름을 안내한다.

Windows 64-bit 빌드는 다음 형태가 기준이다.

```yaml
- uses: game-ci/unity-builder@v4
  env:
    UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
  with:
    unityVersion: 6000.3.13f1
    targetPlatform: StandaloneWindows64
    buildsPath: build
```

참고 문서:

- [GameCI Builder](https://game.ci/docs/github/builder/)
- [GameCI Getting started](https://game.ci/docs/github/getting-started/)

## Linux runner cross-build 가능성

GameCI getting started 문서는 `ubuntu-latest` job에서 `targetPlatform: StandaloneWindows64`를 빌드하는 예시를 제공한다. 따라서 초기 Windows artifact 생성은 Linux runner에서 시작할 수 있다.

다만 같은 문서의 IL2CPP 예시는 “IL2CPP 빌드는 base OS가 build target과 일치해야 한다”고 설명한다. 즉 Windows IL2CPP가 필요해지는 순간에는 `windows-2022` 또는 `windows-latest`에서 빌드하는 쪽으로 전환해야 한다.

Unity 6000.3 공식 문서도 command line build에서 `-buildTarget` 또는 build profile을 명령행에 직접 지정하라고 안내한다. CI에서는 한 Unity 프로세스에서 여러 target으로 바꾸기보다, target별 job을 분리하는 편이 안전하다.

참고 문서:

- [Unity 6000.3: Build a player from the command line](https://docs.unity3d.com/6000.3/Documentation/Manual/build-command-line.html)

## Windows runner 비용과 시간

GitHub 공식 문서 기준으로 public repository에서 standard GitHub-hosted runner 사용은 무료이며, public repo의 `windows-latest`, `windows-2022` standard runner는 4 CPU, 16 GB RAM, 14 GB SSD 사양이다.

주의할 점은 runner 분당 비용만의 문제가 아니라는 점이다.

- larger runner는 public repository에서도 항상 과금 대상이다.
- Actions artifact와 cache는 storage 한도/비용 검토 대상이다.
- 빌드 artifact 기본 보관 기간은 90일이며, workflow에서 artifact별 retention을 짧게 지정할 수 있다.
- 커스텀 LFS 서버를 사용하는 현재 저장소에서는 GitHub 요금과 별도로 LFS 서버 접근성, 인증, 트래픽 한도를 확인해야 한다.

운영 권장값:

- 초기 Windows artifact retention은 7일로 둔다.
- release 태그 빌드만 GitHub Release asset으로 장기 보관한다.
- PR마다 Windows full build를 required로 두지 않는다.
- nightly 또는 수동 workflow로 실행 시간과 artifact 크기를 먼저 측정한다.

참고 문서:

- [GitHub Actions billing](https://docs.github.com/en/billing/concepts/product-billing/github-actions)
- [GitHub-hosted runners reference](https://docs.github.com/en/actions/reference/runners/github-hosted-runners)
- [Removing workflow artifacts](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/remove-workflow-artifacts)

## 빌드 결과 실행 검증 방법

Windows artifact가 생성되는 것과 실제 실행되는 것은 별개로 본다. 최소 smoke 검증은 다음 단계로 충분하다.

1. 빌드 job에서 `build/StandaloneWindows64`를 artifact로 업로드한다.
2. `windows-latest` smoke job에서 artifact를 다운로드한다.
3. `.exe`와 `_Data` 폴더가 함께 있는지 확인한다.
4. Player를 `-batchmode -nographics`로 실행하고 짧은 timeout 안에 비정상 종료가 없는지 확인한다.
5. 로그 파일을 artifact로 업로드한다.

예시:

```powershell
$exe = Get-ChildItem -Path ".\build" -Recurse -Filter "*.exe" | Select-Object -First 1
if (-not $exe) { throw "Windows player exe를 찾지 못했습니다." }

$process = Start-Process -FilePath $exe.FullName `
  -ArgumentList "-batchmode", "-nographics", "-logFile", ".\player-smoke.log" `
  -PassThru

if (-not $process.WaitForExit(30_000)) {
  Stop-Process -Id $process.Id -Force
}

if ($process.ExitCode -ne 0 -and $process.ExitCode -ne $null) {
  throw "Windows player smoke 실행이 실패했습니다. ExitCode=$($process.ExitCode)"
}
```

Unity standalone player는 `-batchmode`, `-nographics` 같은 command line argument를 받을 수 있다. 다만 일반 클라이언트 게임은 스스로 종료하지 않을 수 있으므로, smoke job은 timeout 기반으로 “짧게 켜지는지”를 확인하는 정도로 시작한다. 더 엄격한 검증이 필요하면 테스트 전용 bootstrap scene이나 `Application.isBatchMode` 감지 후 자동 종료하는 smoke 전용 코드를 별도 이슈로 추가한다.

참고 문서:

- [Unity Standalone Player command line arguments](https://docs.unity.cn/Manual/PlayerCommandLineArguments.html)

## 후속 구현 이슈 권장 분할

- Windows build workflow 추가: Linux runner에서 `StandaloneWindows64` artifact 생성
- Windows artifact smoke 검증 추가: `windows-latest`에서 exe 존재/실행 확인
- Windows release asset 배포 결정: Actions artifact와 GitHub Release asset 중 운영 기준 확정
- IL2CPP 전환 여부 결정: 전환 시 Windows runner build로 변경

## 현재 프로젝트 기준 권장안

초기 버전 플랫폼은 PC로 고정한다. 따라서 Windows 빌드 artifact가 현재 Delivery의 중심 경로이고, WebGL 링크는 무료 공유 링크 또는 보조 확인 경로로 둔다. Windows는 다음 순서로 붙이는 것이 좋다.

1. Windows artifact 생성 workflow 추가
2. Windows smoke 검증 job 추가
3. 무료 범위 안에서 보관 가능한 짧은 retention의 Actions artifact를 먼저 사용
4. 수동 release 태그 기준 Windows zip을 GitHub Release asset으로 승격
5. WebGL Pages 링크는 PC artifact가 막힐 때의 보조 공유 경로로 유지

따라서 issue #30의 결정값은 “초기 Windows build는 Linux runner cross-build, 실행 검증은 Windows runner smoke job, IL2CPP 전환 시 Windows runner build로 승격”으로 둔다.

# GameCI Unity 6000.3.13f1 지원과 캐시 전략 조사

이 문서는 GitHub issue #21의 산출물이다. 목표는 Unity 6000.3.13f1에서 GameCI build/test runner와 Library cache를 어떻게 운영할지 결정하기 위한 근거를 정리하는 것이다.

- 조사 기준일: 2026-11-14. upstream 버전·태그 관측값은 모두 이 날짜 기준이며 이후 바뀔 수 있다.
- workflow 설정 인용은 main `b6477f5` 시점의 `.github/workflows/*.yml` 기준이다. 이후 변경된 workflow는 반영되어 있지 않다.
- 이 문서를 작성한 작업 브랜치에서 `unity-editmode-tests.yml`의 cache key에 `EditMode` segment를 넣고 `game-ci/unity-test-runner`를 `@v4.3.2`로 고정하는 변경이 병행되고 있다(미커밋 상태에서 확인). 그 변경은 아직 실행으로 검증되지 않았고, 아래 "현재 설정"은 변경 전 기준이다.
- 이 문서는 workflow 파일을 수정하지 않는다. 변경은 [후속 이슈 후보](#후속-이슈-후보)로 분리한다.
- 표기: **[측정]** 은 이 저장소의 run/API 또는 upstream API를 직접 조회한 값, **[문서]** 는 GitHub/GameCI 공식 문서·릴리스 노트에 근거하지만 이 저장소에서 재현하지 않은 내용, **[미검증]** 은 확인하지 못한 내용이다.

## 결론

1. GameCI는 6000.3.13f1을 지원한다. 단, 이 저장소에서 실제 성공으로 증명된 것은 `game-ci/unity-builder@v4`의 `StandaloneWindows64` 빌드뿐이다(2026-06-13, 2026-06-21). `unity-test-runner`(EditMode)와 WebGL 빌드는 Unity 실행 단계까지 간 run이 없어 **미검증**이다.
2. 마지막 Unity 빌드 성공(2026-06-21) 이후 Unity를 실행하는 workflow run이 없다. 현재 `@v4`가 가리키는 커밋은 두 action 모두 그 이후(2026-09 게시 릴리스)의 것이고, runner의 Node.js 20 제거 예고일(2026-09-16)도 지났다. 따라서 **현재 시점의 `@v4` 동작은 Windows 빌드 포함 전부 재확인이 필요하다.**
3. Library cache key는 빌드 2종(Windows, WebGL)은 target별로 분리되어 있으나, EditMode 테스트의 `restore-keys` prefix가 빌드 target cache까지 매칭하는 구조다. EditMode key에 전용 segment를 넣어 분리할 것을 추천한다.
4. 비용 면에서 현재 설정(artifact retention 1~7일, 수동 실행 위주, 수동 cache 정리 workflow, 업로드 전 cache 예산 가드)은 무료 범위 운영에 충분하다. 기준일 현재 이 저장소의 활성 cache와 미만료 artifact는 모두 0이다.
5. Node.js 20 경고는 실제로 발생했다. 이 저장소가 쓰는 `actions/*` major 전부(`@v4`, `configure-pages@v5`, `upload-pages-artifact@v3` 내부)와 `game-ci/unity-builder@v4`가 Node.js 20 선언 action이다. Node.js 24 선언 major로 올리는 후속 PR을 추천하되, GameCI는 major 변경 폭이 크므로 `actions/*`와 분리해서 올린다.

## 조사 항목별 근거

### 1. GameCI Unity 6000.x 지원 상태

#### 이 저장소 run 기록 [측정]

`gh run list --workflow <file>`과 `gh api .../actions/runs/<id>/jobs` 기준이다. 아래가 해당 workflow의 전체 run이다. 일시는 run 생성 시각이고, 비고는 마지막 attempt 기준이다.

| workflow | run id | 일시(UTC) | ref | 결론 | 비고 |
| ---- | ---- | ---- | ---- | ---- | ---- |
| `windows-demo-build.yml` | 27468806735 | 2026-06-13 13:58 | main | failure | attempt 3회 모두 failure. 1차(13:58): `LFS 오브젝트 가져오기` 실패(`.lfsconfig`의 LFS URL과 repository variable `LFS_URL` 불일치). 2차(14:08): 빌드 step이 `Missing Unity License File and no Serial was found`로 실패. 3차(14:39): Unity 빌드 step은 성공(5분 51초), `Windows zip 패키징` step 실패(`Windows 빌드 산출물 폴더가 없습니다: build/WindowsDemo`). 3차 실패는 GameCI가 아닌 패키징 경로 문제 |
| `windows-demo-build.yml` | 27470111585 | 2026-06-13 14:54 | main | success | 빌드 step 5분 56초. `Library folder does not exist` 경고(cache 없음) |
| `windows-demo-build.yml` | 27906272150 | 2026-06-21 13:44 | main | success | 빌드 step 2분 59초 |
| `unity-editmode-tests.yml` | 25284725827 | 2026-05-03 16:37 | main | failure | LFS fetch 실패. Unity 미실행 |
| `unity-editmode-tests.yml` | 25285015681 | 2026-05-03 16:50 | fix/unity-actions-lfs-checkout | failure | `Process completed with exit code 2`(job 6초). Unity 미실행 |
| `unity-editmode-tests.yml` | 25320474654 | 2026-05-04 13:01 | fix/unity-actions-lfs-checkout | failure | `Missing Unity License File and no Serial was found`. 라이선스 활성화 전 단계에서 실패 |
| `webgl-free-delivery.yml` | (없음) | - | - | - | run 기록 0건 |
| `windows-packaging-healthcheck.yml` | 27470447299 | 2026-06-13 15:08 | PR | success | Unity 미실행. `actions/cache/restore@v4` `lookup-only`만 사용 |
| `unity-license-healthcheck.yml` | 27469757064 | 2026-06-13 14:39 | main | success | secret 존재 점검. Unity 미실행 |

저장소 전체에서 가장 최근 run은 2026-06-21 13:56 UTC의 `PR 빠른 체크`(27906582076)다. 즉 2026-06-21 이후 약 5개월간 Actions run이 없다.

증명 범위 정리:

| 대상 | 상태 | 근거 |
| ---- | ---- | ---- |
| `unity-builder@v4` + 6000.3.13f1 + `StandaloneWindows64` (`ubuntu-latest`) | 증명됨(2026-06-21 시점) | run 27470111585, 27906272150. run 27468806735의 3차 attempt도 빌드 step까지는 성공 |
| `unity-test-runner@v4` + 6000.3.13f1 EditMode | 미검증 | 3회 모두 Unity 실행 전 실패. 라이선스 secret 점검은 2026-06-13에 성공했지만 그 이후 EditMode workflow를 다시 실행한 기록이 없다 |
| `unity-builder@v4` + 6000.3.13f1 + `WebGL` | 미검증 | run 0건 |
| 2026-09 이후 `@v4`가 가리키는 새 커밋에서의 동작 | 미검증 | 2026-06-21 이후 Unity 실행 run 없음 |

run 로그 본문은 보존 기간이 지나 조회할 수 없다(`gh run view 27906272150 --log` → HTTP 410). 그래서 당시 사용된 `unityci/editor` 이미지 tag와 cache hit 여부는 로그로 직접 확인하지 못했고, step 소요 시간과 annotation으로만 판단했다.

#### upstream 릴리스와 `@v4` 태그 해석 [측정]

`gh api repos/game-ci/<repo>/releases`, `.../tags`, `.../contents/action.yml?ref=<tag>` 기준(2026-11-14).

| action | 저장소가 쓰는 ref | 현재 가리키는 커밋 | 같은 커밋의 버전 태그 | 선언 runtime |
| ---- | ---- | ---- | ---- | ---- |
| `game-ci/unity-builder` | `@v4` | `4423ee75828d` | v4.8.2 (2026-09-01 게시) | `node20` |
| `game-ci/unity-test-runner` | `@v4` | `32e57712352b` | v4.4.0 (2026-09-09 게시) | `node24` |

`unity-builder` 릴리스 계열:

| 태그 | 게시일 | runtime | 비고 |
| ---- | ---- | ---- | ---- |
| v4.8.1 | 2025-11-22 | `node20` | 2026-06 성공 run 시점에 v4 계열 최신이던 릴리스 |
| v4.8.2 | 2026-09-01 | `node20` | Unity 6.6+ shared memory 수정 backport(`dockerShmSize` 입력 추가). 현재 `@v4` |
| v5.0.0 / v5.0.1 | 2026-05-07 / 2026-09-01 | `node24` | v5.0.0 릴리스 노트에 Node.js 20 → 24 runtime 변경, Orchestrator 코드 분리 포함. v5.0.1은 v4.8.2와 같은 shared memory 수정. `@v5` = v5.0.1 |
| v6.0.0 | 2026-09-10 | `node24` | `game-ci/cli`를 감싸는 thin wrapper로 재구성. 릴리스 노트는 입력·secret이 `@v5`와 같다고 설명하며, beta에서 `unityVersion` 입력이 무시되던 문제를 고쳤다고 적혀 있다. API상 latest 릴리스. `@v6` = v6.0.0 |

`unity-test-runner` 릴리스 계열:

| 태그 | 게시일 | runtime | 비고 |
| ---- | ---- | ---- | ---- |
| v4.3.1 | 2024-07-05 | `node20` | 2026-05 실패 run 시점의 v4 최신 |
| v4.4.0 | 2026-09-09 | `node24` | Node.js 24 전환, `UNITY_LICENSE` 활성화 복원 수정, `game-ci/cli` subprocess 호출 구조(thin wrapper)로 변경. 현재 `@v4` |
| v4.3.2 | 2026-09-09 | `node24` | v4.3.1에 이미 개별 반영된 수정 커밋 6건과 shared memory 수정을 더한 릴리스로, 구조 변경이 없다고 릴리스 노트에 적혀 있다. API상 latest 릴리스 |
| v5.0.0-beta.1 | 2026-08-18 | - | pre-release |

해석:

- 2026-06 성공 run 당시 `@v4`가 정확히 어느 커밋이었는지는 로그가 없어 확정할 수 없다. 당시 v4 계열 최신 릴리스가 v4.8.1이었다는 점만 측정값이다. 태그가 이동한 정확한 시각도 API로는 알 수 없고, 현재 대상 커밋의 날짜(`unity-builder` 2026-09-01, `unity-test-runner` 2026-08-29)와 릴리스 게시일로 "2026-06-21 이후"라는 것만 확인된다.
- `unity-test-runner@v4`는 2026-09에 같은 major 안에서 내부 구조가 바뀌었다(v4.4.0 릴리스 노트). 이 저장소의 EditMode workflow는 그 전에도 성공한 적이 없으므로, 기존 대비 회귀 여부를 판단할 기준 run이 없다.
- floating major 태그(`@v4`)는 이렇게 조용히 이동한다. 재현성을 우선하면 정확한 버전 태그 또는 커밋 SHA 고정이 낫다.

#### Unity editor 이미지 [측정]

Docker Hub `unityci/editor` tag 조회(2026-11-14)에서 `ubuntu-6000.3.13f1-{base,windows-mono,webgl,linux-il2cpp,android,ios,mac-mono}-3`(및 `-3.2`, `-3.2.2`)와 `windows-6000.3.13f1-{base,windows-il2cpp,...}-3` tag가 존재한다. 따라서 EditMode(`base`)와 WebGL(`webgl`)용 이미지가 없어서 실패할 가능성은 낮다. 이미지가 존재한다는 것과 이 프로젝트가 그 이미지에서 테스트/빌드에 성공한다는 것은 별개이며, 후자는 미검증이다.

### 2. Library cache key 전략

#### 현재 설정 [측정]

`.github/workflows/*.yml` 기준. 공통 hash 입력은 `hashFiles('Packages/manifest.json', 'Packages/packages-lock.json', 'ProjectSettings/ProjectVersion.txt')`이고 `path`는 모두 `Library`다. `runner.os`는 세 workflow 모두 `ubuntu-latest`라서 `Linux`로 풀린다.

| workflow | action | key | restore-keys |
| ---- | ---- | ---- | ---- |
| `unity-editmode-tests.yml` | `actions/cache@v4` | `Library-Linux-Unity-6000.3.13f1-<hash>` | `Library-Linux-Unity-6000.3.13f1-` |
| `windows-demo-build.yml` | `actions/cache@v4` | `Library-Linux-Unity-6000.3.13f1-StandaloneWindows64-<hash>` | `Library-Linux-Unity-6000.3.13f1-StandaloneWindows64-` |
| `webgl-free-delivery.yml` | `actions/cache@v4` | `Library-Linux-Unity-6000.3.13f1-WebGL-<hash>` | `Library-Linux-Unity-6000.3.13f1-WebGL-` |
| `windows-packaging-healthcheck.yml` | `actions/cache/restore@v4` (`lookup-only: true`) | Windows 빌드와 동일 | Windows 빌드와 동일 |

관련 운영 도구:

- `.github/workflows/actions-storage-maintenance.yml` + `scripts/cleanup-github-actions-caches.mjs`: 수동(`workflow_dispatch`) 정리. 기본 prefix는 `Library-Linux-Unity-6000.3.13f1-StandaloneWindows64-`, 기본 `keep_count=1`, 기본 `dry_run=true`. run 기록 0건.
- `.github/workflows/itchio-development-upload.yml`의 `Actions cache 예산 확인` step(`scripts/check-github-actions-cache-budget.mjs`): 10GiB 기준 여유가 2GiB 미만이면 업로드를 막는다.

#### cache 동작 관측 [측정]

| run | `Unity Library 캐시 복원` | Unity 빌드 step | `Post Unity Library 캐시 복원` | 해석 |
| ---- | ---- | ---- | ---- | ---- |
| 27468806735 3차 attempt (2026-06-13) | 1초 미만 | 5분 51초 | skipped | `Library folder does not exist` 경고. job 실패로 Post step이 건너뛰어져 cache가 저장되지 않음 |
| 27470111585 (2026-06-13) | 1초 | 5분 56초 | 10초 | `Library folder does not exist` 경고가 있어 cache miss. Post step에서 저장한 것으로 추정 |
| 27906272150 (2026-06-21) | 9초 | 2분 59초 | 1초 | 위 경고가 없고 복원에 9초가 걸려 cache hit으로 추정. 빌드 step 약 50% 단축 |

"추정"으로 적은 부분은 로그 만료로 hit/miss 문구를 직접 확인하지 못했기 때문이다. cache 크기도 당시 값을 조회할 수 없다. `actions-storage-maintenance.yml` 주석의 "수백 MB~1GB"는 이 조사에서 재측정하지 못했다(미검증).

기준일 현재 `gh api repos/LHTCP/srpg/actions/caches`는 `total_count: 0`, `actions/cache/usage`는 `active_caches_size_in_bytes: 0`이다. 2026-06에 만들어진 cache가 남아 있지 않다는 뜻이며, 7일 미접근 eviction 정책([문서])과 일치한다.

#### 문제점

1. **EditMode restore-keys가 빌드 target cache와 namespace를 공유한다.** `Library-Linux-Unity-6000.3.13f1-`는 `...-StandaloneWindows64-<hash>`와 `...-WebGL-<hash>`의 prefix이기도 하다. EditMode 정확 key가 miss이면 `actions/cache`는 prefix가 맞는 것 중 가장 최근에 생성된 cache를 복원하므로([문서]), Windows 또는 WebGL target으로 import된 Library가 EditMode job에 들어올 수 있다. Unity가 target 불일치를 감지해 재import하므로 결과가 틀려지지는 않을 것으로 예상하지만, 복원 시간만 쓰고 이득이 없으며 그 상태가 EditMode key로 다시 저장된다. 실제 발생 여부는 EditMode run 성공 기록이 없어 미검증이다.
2. **key에 `Assets` 변화가 반영되지 않는다.** 정확 key가 hit하면 `actions/cache`는 Post step에서 저장을 건너뛴다([문서]). 따라서 패키지/에디터 버전이 그대로인 동안 cache는 처음 저장된 시점의 Library로 고정되고, 이후 추가·변경된 에셋은 매 run마다 다시 import된다. 대신 cache가 target당 1개로 유지되어 용량이 늘지 않는다. 지금처럼 실행 빈도가 낮을 때는 합리적인 절충이다.
3. **cache가 사실상 유지되지 않는다.** 7일 동안 접근이 없으면 삭제되는데([문서]), 빌드 workflow가 모두 수동 실행이고 실제 실행 간격이 그보다 길다. 현재 cache는 "연속 작업 기간에만 효과가 있는 가속"으로 봐야 한다.
4. **정리 workflow 기본 prefix가 Windows만 가리킨다.** WebGL·EditMode cache가 생기기 시작하면 prefix를 바꿔 여러 번 실행해야 한다. 예산 가드는 itch.io 업로드 workflow에만 있다.
5. **cache scope는 브랜치 단위다.** [문서] 기본 브랜치에서 만든 cache는 다른 브랜치/PR에서 복원할 수 있지만 그 반대는 안 된다. feature 브랜치에서 `workflow_dispatch`로 빌드하면 그 브랜치 전용 cache가 따로 생겨 용량을 차지한다. EditMode workflow의 2026-05 run 2건이 feature 브랜치에서 실행된 것처럼, 이 패턴은 실제로 쓰이고 있다.

### 3. cache/artifact 비용과 retention 기준

#### 현재 설정 [측정]

| workflow | artifact | `retention-days` |
| ---- | ---- | ---- |
| `unity-editmode-tests.yml` | `unity-editmode-test-results` | 7 |
| `windows-demo-build.yml` | Windows zip | 7 |
| `webgl-free-delivery.yml` | `srpg-webgl-<sha>` | 7 |
| `webgl-free-delivery.yml` | Pages artifact (`actions/upload-pages-artifact@v3`) | workflow에서 미지정(action 기본값) |
| `windows-packaging-healthcheck.yml` | 패키징 헬스체크 zip | 1 |

기준일 현재 `gh api repos/LHTCP/srpg/actions/artifacts`의 미만료 artifact는 0건이다. 2026-06 run들의 artifact는 run 단위 조회에서도 반환되지 않아, 당시 Windows zip 크기는 이 조사에서 확인하지 못했다(미검증).

저장소는 public이다(`gh repo view` → `visibility: PUBLIC`).

#### GitHub 정책 [문서]

- public 저장소의 standard GitHub-hosted runner 사용과 artifact 저장은 무료 범위다.
- artifact 기본 보관 기간은 90일이고, public 저장소는 1~90일 범위에서 조정할 수 있다.
- Actions cache는 저장소당 기본 10GB이며, 7일 동안 접근되지 않은 cache는 삭제된다. 한도를 넘으면 가장 오래 접근되지 않은 cache부터 eviction된다.
- 2025-11 이후 저장소 cache 한도를 10GB 이상으로 올릴 수 있게 되었으나 초과분은 과금 대상이다. 이 저장소는 올리지 않는 것을 전제로 한다([cicd-runbook.md](cicd-runbook.md)의 cache 항목과 같은 기준).

위 수치는 공식 문서 기준이며 이 저장소의 설정 화면에서 실제 한도 값을 확인하지는 않았다(미검증). 7일 eviction은 "2026-06에 저장된 cache가 현재 0건"이라는 측정값과 일치한다.

참고 문서:

- [GitHub Actions billing](https://docs.github.com/en/billing/concepts/product-billing/github-actions)
- [Dependency caching reference](https://docs.github.com/en/actions/reference/workflows-and-actions/dependency-caching)
- [Removing workflow artifacts](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/remove-workflow-artifacts)

#### 판단

- cache: target 3종(EditMode, Windows, WebGL) × 기본 브랜치 1개씩이면, cache 1개가 1GB 수준이라는 주석값을 그대로 믿어도 10GB 한도에 여유가 있다. 위험은 feature 브랜치 수동 실행과 key hash 변경(패키지 갱신)이 겹쳐 cache가 여러 벌 쌓이는 경우다. 이때는 7일 eviction과 기존 수동 정리 workflow로 충분히 대응된다.
- artifact: retention 1~7일은 무료 우선 기준에 맞다. 장기 보관이 필요한 빌드는 [windows-build-runner-research.md](windows-build-runner-research.md)의 기준대로 release 태그 빌드만 GitHub Release asset으로 승격한다.

### 4. Node.js 20 deprecation 경고

#### 실제 annotation [측정]

`gh api repos/LHTCP/srpg/check-runs/<job id>/annotations` 기준.

| run (일자) | 경고에 나열된 action | 경고 문구 요지 |
| ---- | ---- | ---- |
| 25320474654 (2026-05-04) | `actions/cache@v4`, `actions/checkout@v4`, `actions/upload-artifact@v4`, `game-ci/unity-test-runner@v4` | Node.js 20 action deprecated. 2026-06-02부터 Node.js 24 강제, 2026-09-16에 runner에서 Node.js 20 제거 |
| 27470111585 (2026-06-13) | `actions/cache@v4`, `actions/checkout@v4`, `actions/upload-artifact@v4`, `game-ci/unity-builder@v4` | 같은 내용. 강제 시작일이 2026-06-16으로 표기됨 |
| 27470447299 (2026-06-13) | `actions/cache/restore@v4`, `actions/checkout@v4`, `actions/upload-artifact@v4` | 같은 내용 |
| 27906272150 (2026-06-21) | `actions/cache@v4`, `actions/checkout@v4`, `actions/upload-artifact@v4`, `game-ci/unity-builder@v4` | "Node.js 20 대상이지만 Node.js 24로 강제 실행됨" |

마지막 줄이 중요하다. 2026-06-21 run은 Node.js 20 선언 action들이 **Node.js 24로 강제 실행된 상태에서 성공**했다. 즉 당시 버전의 `unity-builder@v4`와 `actions/*@v4`는 Node.js 24 강제 실행에서 동작했다. 다만 runner에서 Node.js 20이 제거된다고 예고된 2026-09-16 이후에는 이 저장소에서 실행된 run이 없어, 지금도 경고만 뜨고 동작하는지는 미검증이다.

#### 사용 중인 action의 major와 runtime [측정]

`action.yml`의 `runs.using` 값을 태그별로 조회했다(2026-11-14). latest는 `releases/latest` 기준이다.

| action | 사용 중 | 사용 중 runtime | latest 릴리스 | Node.js 24 선언이 시작되는 major |
| ---- | ---- | ---- | ---- | ---- |
| `actions/checkout` | v4 | `node20` | v7.0.1 | v5 |
| `actions/cache`, `actions/cache/restore` | v4 | `node20` | v6.1.0 | v5 |
| `actions/upload-artifact` | v4 | `node20` | v7.0.2 | v6 (v5는 `node20`) |
| `actions/download-artifact` | v4 | `node20` | v8.0.2 | v7 (v5·v6은 `node20`) |
| `actions/configure-pages` | v5 | `node20` | v6.0.0 | v6 |
| `actions/deploy-pages` | v4 | `node20` | v5.0.1 | v5 |
| `actions/upload-pages-artifact` | v3 | composite(내부에서 `actions/upload-artifact@v4` 호출) | v5.0.0 | v5(내부에서 `upload-artifact` v7.0.0 SHA 고정). v4는 내부 `upload-artifact` v4.6.2(`node20`) |
| `remarkablegames/setup-butler` | v3 | `node24` | v3.0.4 | 조치 불필요 |
| `game-ci/unity-builder` | v4 (= v4.8.2) | `node20` | v6.0.0 | v5 |
| `game-ci/unity-test-runner` | v4 (= v4.4.0) | `node24` | v4.3.2 | 현재 `@v4`가 이미 `node24` |

각 major 사이의 breaking change(입력 이름, 최소 runner 버전, artifact 호환성 등)는 이 조사에서 릴리스 노트를 전부 검토하지 않았다. 올리는 PR에서 action별로 확인해야 한다(미검증).

## 추천 설정

아래는 추천이며 이 문서에서 workflow에 적용하지 않았다.

### GameCI 버전

- 지금 바로 major를 올리기 전에, **현재 설정 그대로 세 workflow를 한 번씩 수동 실행해 기준선을 다시 만든다.** 2026-06-21 이후 `@v4` 대상 커밋과 runner 환경이 모두 바뀌었기 때문에, 변경 전 상태가 성공하는지 모르는 채로 버전을 올리면 실패 원인을 분리할 수 없다.
- `unity-builder`: 기준선이 성공하면 `@v5`(Node.js 24 선언)로 올리는 것을 1차 후보로 둔다. `@v6`은 `game-ci/cli` wrapper로 구조가 바뀐 지 약 2개월 된 릴리스이므로, v5에서 안정화한 뒤 별도 PR로 검토한다.
- `unity-test-runner`: `@v4`가 이미 Node.js 24 선언이다. 다만 `@v4`(v4.4.0)는 thin wrapper 구조이고, upstream이 기존 구조 유지판으로 v4.3.2를 따로 내고 있다. EditMode 기준선이 `@v4`에서 실패하면 `@v4.3.2` 고정으로 비교한다.
- 기준선 확보 후에는 floating `@v4` 대신 버전 태그(예: `@v4.8.2`) 또는 커밋 SHA 고정을 검토한다. 이번처럼 실행 공백 동안 태그가 이동해도 재현성이 유지된다. 대신 보안/버그 수정이 자동 반영되지 않으므로 갱신 주기를 정해야 한다.

### Library cache key

| 대상 | 추천 key | 추천 restore-keys | 현재 대비 |
| ---- | ---- | ---- | ---- |
| EditMode 테스트 | `Library-${{ runner.os }}-Unity-6000.3.13f1-EditMode-<hash>` | `Library-${{ runner.os }}-Unity-6000.3.13f1-EditMode-` | `EditMode` segment 추가(변경 필요) |
| Windows 빌드 | `Library-${{ runner.os }}-Unity-6000.3.13f1-StandaloneWindows64-<hash>` | `Library-${{ runner.os }}-Unity-6000.3.13f1-StandaloneWindows64-` | 유지 |
| WebGL 빌드 | `Library-${{ runner.os }}-Unity-6000.3.13f1-WebGL-<hash>` | `Library-${{ runner.os }}-Unity-6000.3.13f1-WebGL-` | 유지 |
| 패키징 헬스체크 | Windows 빌드와 동일, `lookup-only` | 동일 | 유지 |

- `<hash>` 입력은 현행(`Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/ProjectVersion.txt`)을 유지한다. GameCI 문서 예시처럼 `Assets/**`를 hash에 넣으면 최신 Library가 저장되지만 커밋마다 새 cache가 생긴다. cache 1개 크기가 큰 Unity 프로젝트에서 10GB 한도를 빠르게 소모하므로, 실행 빈도가 낮은 현재는 채택하지 않는다.
- restore-keys는 target segment까지만 포함한 prefix 1개로 제한한다. target을 넘는 fallback(`Library-Linux-Unity-6000.3.13f1-`)은 두지 않는다.
- 에디터 버전을 올릴 때는 key의 `6000.3.13f1` 문자열 4곳(workflow 4개)과 정리 workflow의 기본 prefix를 함께 바꾼다. `ProjectVersion.txt`가 hash에 들어 있어 정확 key는 자동으로 바뀌지만, prefix 문자열이 남으면 restore-keys가 이전 버전 Library를 복원한다.
- cache가 깨진 것으로 의심되면 key 구조를 바꾸기보다 기존 `Actions 저장소 정리` workflow로 해당 prefix를 지운다.

### 비용·retention

- artifact `retention-days`는 현행(빌드·테스트 7일, 헬스체크 1일)을 유지한다.
- cache 한도는 기본값에서 올리지 않는다.
- `Actions 저장소 정리` workflow는 EditMode·WebGL cache가 실제로 생긴 뒤 prefix별로 `dry_run=true` 실행해 대상이 맞는지 확인한다. 자동(schedule) 정리는 7일 eviction이 이미 같은 역할을 하므로 지금은 추가하지 않는다.
- feature 브랜치에서 Unity workflow를 수동 실행하면 브랜치 전용 cache가 생긴다는 점을 [cicd-runbook.md](cicd-runbook.md)에 안내한다.

### Node.js runtime

- `actions/*`는 Node.js 24 선언 major로 올린다. 최소 목표는 `checkout@v5` 이상, `cache@v5` 이상, `upload-artifact@v6` 이상, `download-artifact@v7` 이상, `configure-pages@v6`, `deploy-pages@v5`, `upload-pages-artifact@v5`다. latest major까지 한 번에 올릴지는 PR에서 릴리스 노트를 보고 정한다.
- `upload-artifact`와 `download-artifact`는 major 조합에 따라 artifact 호환성이 달라질 수 있으므로, `windows-demo-build.yml`(업로드)과 `itchio-development-upload.yml`(다운로드)을 같은 PR에서 함께 올리고 실제 업로드→다운로드 경로로 검증한다.
- `actions/*` 갱신 PR과 GameCI major 갱신 PR은 분리한다.

## 후속 이슈 후보

| 후보 | 내용 | 완료 기준 |
| ---- | ---- | ---- |
| Unity workflow 기준선 재실행 | 현재 설정 그대로 `Unity EditMode 테스트`, `Windows PC 데모 빌드`, `WebGL 무료 Delivery`(Pages 배포 없이)를 main에서 수동 실행 | 세 run의 id·결론·annotation을 이 문서 표에 추가. 실패 시 원인 분류 |
| EditMode cache key 분리 | `unity-editmode-tests.yml` key/restore-keys에 `EditMode` segment 추가 | EditMode run 2회에서 miss → hit 확인, Windows/WebGL cache를 복원하지 않음을 로그로 확인 |
| `actions/*` Node.js 24 major 갱신 | 위 추천 major로 일괄 갱신 | 갱신 후 run annotation에 Node.js 20 경고가 없음. Windows 빌드 → itch.io 업로드 경로의 artifact 전달 성공 |
| `unity-builder` major 갱신 | `@v4` → `@v5` (이후 `@v6` 별도 검토) | Windows 빌드 성공, 빌드 시간·artifact 크기를 기준선과 비교 |
| GameCI action 버전 고정 정책 | floating major 유지 vs 버전 태그/SHA 고정 결정 | 결정과 갱신 주기를 [cicd-runbook.md](cicd-runbook.md)에 기록 |
| cache 정리 workflow 범위 확장 | 기본 prefix를 target 공통으로 바꾸거나 여러 prefix를 한 번에 처리 | EditMode·WebGL·Windows cache를 `dry_run=true`로 한 번에 보고 |
| cache·artifact 크기 실측 | 기준선 run에서 Library cache 크기와 Windows zip/WebGL artifact 크기 기록 | 이 문서의 미검증 크기 항목을 측정값으로 교체 |

## 미검증 항목 요약

- `unity-test-runner@v4`의 6000.3.13f1 EditMode 실행 성공 여부
- `unity-builder@v4`의 6000.3.13f1 WebGL 빌드 성공 여부
- 2026-09 이후 `@v4`(v4.8.2 / v4.4.0)와 Node.js 20 제거 예고일 이후 runner에서의 현재 동작
- Library cache 실제 크기, Windows zip/WebGL artifact 실제 크기
- 2026-06 run의 cache hit/miss 로그 문구와 사용된 `unityci/editor` 이미지 tag(로그 만료)
- EditMode restore-keys가 실제로 다른 target cache를 복원한 사례
- 저장소 설정 화면의 cache 한도·기본 artifact retention 값
- `actions/*` 각 major 사이의 breaking change 상세

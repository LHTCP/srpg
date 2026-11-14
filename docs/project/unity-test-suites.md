# Unity 테스트 묶음과 실행 기준

이 문서는 `Assets/Tests/` 아래 테스트를 폴더·어셈블리·카테고리 기준으로 정리하고, CI 후보와 로컬/수동 전용 묶음, 최소 smoke 기준을 고정한다. (GitHub issue #14)

- 기준 소스: `Assets/Tests/`, `.github/workflows/unity-editmode-tests.yml` (테스트 소스는 main `b6477f5` 시점)
- Unity 에디터: 6000.3.13f1, Test Framework `com.unity.test-framework` 1.6.0 (`Packages/manifest.json`)
- 이 문서의 숫자는 소스 파일에서 `[Test]`/`[TestCase]`/`[UnityTest]` 어트리뷰트 줄 수를 센 값이다. Test Runner가 실제로 표시하는 개수나 통과 여부를 확인한 값이 아니다.
- 이 문서를 작성하면서 테스트를 실행하지는 않았다. 통과 여부는 [한계](#한계와-미검증-항목)를 따른다.

## 폴더별 목적

| 폴더 | 어셈블리 | Test Runner 모드 | 목적 |
| ---- | ---- | ---- | ---- |
| `Assets/Tests/EditMode/Editor/` | asmdef 없음. `Editor` 폴더 규칙에 따라 기본 `Assembly-CSharp-Editor`로 컴파일된다 | EditMode | 전투 규칙·데이터·머티리얼의 순수 로직 회귀 테스트와 AI 시뮬레이션 배치 엔트리 |
| `Assets/Tests/Editor/` | asmdef 없음. 기본 `Assembly-CSharp-Editor` | 테스트 아님 | `TestRunnerApi`로 카테고리 묶음을 실행하는 에디터 메뉴 (`SRPG/...`) |
| `Assets/Tests/PlayMode/` | `Srp.PlayMode.Tests` (`Srp.Runtime` 참조, `optionalUnityReferences: TestAssemblies`) | PlayMode | `SrpGameController`를 런타임에 생성해 HUD·입력·전투 흐름을 확인하는 통합 테스트와 화면 관찰 캡처 |
| `Assets/Tests/Simulation/` | `Srp.Simulation` (`Srp.Runtime` 참조) | 테스트 아님 | AI 정책·배틀 시뮬레이션 러너·지표·임계치·JSON 리포트 작성기. 테스트 클래스가 호출하는 지원 코드 |

참고:

- `Srp.Simulation` asmdef에는 테스트 어셈블리 참조, 플랫폼 제한, define constraint가 없고 `autoReferenced: true`다. 설정만 보면 플레이어 빌드에도 포함되는 일반 어셈블리로 읽힌다. 실제 빌드 포함 여부는 확인하지 않았다.
- EditMode 테스트가 기본 에디터 어셈블리에 들어 있어서, 어셈블리 이름으로 EditMode 테스트만 골라 실행할 수 없다. 묶음 구분은 카테고리와 클래스 이름으로 한다.

## 테스트 클래스 목록

### EditMode (`Assets/Tests/EditMode/Editor/`)

| 클래스 | 카테고리 | 어트리뷰트 수 | 내용 |
| ---- | ---- | ---- | ---- |
| `SrpM1CoreTests` | `SrpM1All` | 33 | 턴 순서, 화기 탄약, 엄폐, 상호작용 지점, 스킬 충전·쿨다운·오버클럭 등 핵심 규칙 |
| `SrpM1RuleSpecTests` | `SrpM1All` | 32 | ZOC·교전 이탈, 태세, 처단, 반응(방어·회피·패링·기회공격), 경계태세, 시야 |
| `SrpMakerMetadataTests` | `SrpM1All` | 13 | 메이커 메타데이터 JSON round-trip, 프리셋 로딩, 스킬 필터 |
| `SrpRuntimeMaterialTests` | `SrpM1All` | 1 | 런타임 primitive 색 적용 시 URP 호환 머티리얼 사용 (PR #93) |
| `SrpM1AllTestsEntry` | `SrpM1All` | 1 | `SrpM1CoreTests` 2개, `SrpM1RuleSpecTests` 4개 메서드를 다시 호출하는 단일 엔트리. 위 클래스와 중복 실행된다 |
| `SrpM1AiSimAllEntry` | `SrpM1All`, `SrpAiSim` | 3 | AI 시뮬레이션 배치와 임계치 검증. 소스 기준 500회 1건 + 정책 매트릭스 4케이스×300회 2건 = 전투 2,900회. `TestResults/SrpSim/`에 JSON 리포트를 쓴다 |
| `SrpCombatResolverTests` | 없음 | 13 (`[TestCase]` 8 + `[Test]` 5) | 화기 PG 파급, 근접 PG 압박, 그로기 처단, HP 취약도 등 전투 해석기 규칙 |
| `SrpM1VictoryTests` | `SrpM1All` | 10 (`[TestCase]`) | 승리 조건 판정(전멸, 지휘관 처치, 상호작용 승리 목표)과 맵 데이터 계약, 맵 메이커 보존 묶음 (2026-11-14 추가) |
| `SrpM1BattleFlowTests` | `SrpM1All` | 10 (`[TestCase]`) | 런타임과 AI 시뮬레이션이 공유하는 전투 진행 로직(`SrpBattleFlow`), 적 AI 경계태세, 시뮬레이션 행동 지표 (2026-11-14 추가) |
| `SrpM1SkillMapTests` | `SrpM1All` | 25 (`[TestCase]`) | 시야(LOS) 모듈, 범위 스킬, 전투 태그 지속시간, 맵 메이커 엄폐 편집 (2026-11-14 추가. 아래 합계에는 넣지 않았다) |
| `SrpM1ContentTests` | `SrpM1All`, 일부 `SrpAiSim` | 14 (`[TestCase]`) | 내장 프리셋·기본 데이터 정합성, 새 스킬·유닛·방벽선 돌파 맵, 로컬 DB 기본 데이터 갱신, 맵 메이커 승리 조건 편집 (2026-11-15 추가. 아래 합계에는 넣지 않았다) |

EditMode 합계: 어트리뷰트 96개. 이 중 `SrpM1All` 카테고리에 속한 것은 83개다.

### PlayMode (`Assets/Tests/PlayMode/`)

| 클래스 | 카테고리 | 어트리뷰트 수 | 내용 |
| ---- | ---- | ---- | ---- |
| `SrpM1PlayModeTests` | `SrpM1All` | 8 | 프리셋 초기화와 HUD, 화기 조준 프리뷰, 경계태세 반응, 기본 공격 hover/실행, 위험영역, 직접 조작 UI |
| `SrpM1AiPlaySampleTests` | `SrpM1All`, `SrpAiSim` | 1 | AI QA용 런타임 재검증. HUD·행동 순서 패널 문자열 계약 확인 |
| `SrpM1OpeningObservationTests` | `SrpObservation` | 1 | 첫 전투 화면을 1280×720으로 캡처해 `TestResults/SrpPlayObservation/`에 PNG와 관찰 메모를 쓴다 |

PlayMode 합계: 어트리뷰트 10개. 이 중 `SrpM1All` 카테고리에 속한 것은 9개다.

`TestResults/`는 `.gitignore` 대상이다.

## 카테고리

| 카테고리 | EditMode 구성 | PlayMode 구성 | 용도 |
| ---- | ---- | ---- | ---- |
| `SrpM1All` | `SrpM1CoreTests`, `SrpM1RuleSpecTests`, `SrpMakerMetadataTests`, `SrpRuntimeMaterialTests`, `SrpM1AllTestsEntry`, `SrpM1AiSimAllEntry` | `SrpM1PlayModeTests`, `SrpM1AiPlaySampleTests` | M1 자동화 QA 전체 묶음 |
| `SrpAiSim` | `SrpM1AiSimAllEntry` | `SrpM1AiPlaySampleTests` | AI 시뮬레이션 하이브리드 QA |
| `SrpObservation` | 없음 | `SrpM1OpeningObservationTests` | 화면 캡처 기반 수동 관찰 |
| `RequiresUnity` | `SrpM1CoreTests`, `SrpMakerMetadataTests`의 각 1개 | 없음 | Unity 타입·컨트롤러에 의존해 .NET 로직 테스트([logic-tests.md](logic-tests.md))에서 빼는 표시. Unity에서는 그대로 실행된다 |
| (카테고리 없음) | `SrpCombatResolverTests` | 없음 | - |

카테고리 관련 주의:

- `SrpCombatResolverTests`에는 `[Category]`가 없다. `SrpM1All` 필터를 쓰는 실행(메뉴 `SRPG/Run M1 Automated QA (Edit+Play)`, `-testCategory SrpM1All`)에서는 이 클래스가 빠진다. 필터 없이 EditMode 전체를 실행하는 경로(Test Runner의 Run All, 현재 CI workflow)에서만 실행된다.
- `SrpM1AiSimAllEntry`가 `SrpM1All`에도 속해 있어서, `SrpM1All` 실행에는 전투 2,900회 시뮬레이션이 항상 포함된다. `SrpM1All`은 빠른 smoke 묶음이 아니다.
- `SrpObservation`은 어떤 메뉴에서도 실행하지 않는다. Test Runner 창이나 CLI에서 직접 골라야 한다.

## CI 후보와 로컬/수동 전용

분류 기준은 에디터 화면·GPU 의존 여부, 파일 산출물, 실행 비용이다. 실행 시간은 측정하지 않았다.

| 묶음 | 분류 | 이유 |
| ---- | ---- | ---- |
| `SrpM1CoreTests`, `SrpM1RuleSpecTests`, `SrpMakerMetadataTests`, `SrpCombatResolverTests` | CI 후보 (1순위) | 씬·렌더링 없이 상태 객체만 다루는 EditMode 로직 테스트 |
| `SrpRuntimeMaterialTests` | CI 후보 | EditMode. 셰이더 조회에 의존하므로 CI 컨테이너에서 URP 셰이더를 찾는지는 첫 실행에서 확인해야 한다 (미검증) |
| `SrpM1AllTestsEntry` | CI 후보 (중복) | 위 클래스 메서드를 재호출하므로 추가 커버리지는 없다 |
| `SrpM1AiSimAllEntry` | CI 후보 (조건부) | EditMode이고 시드가 소스 상수로 고정(`baseSeed` 20260418, 첫 전투 매트릭스는 20260608)이지만 전투 2,900회를 돌리고 밸런스 임계치로 실패할 수 있다. 실행 시간과 실패 패턴을 본 뒤 PR 체크에 넣을지 정한다 (#22) |
| `SrpM1PlayModeTests`, `SrpM1AiPlaySampleTests` | 로컬/수동 (CI 2차 후보) | PlayMode에서 `SrpGameController`와 HUD를 실제로 생성하고 프레임 대기에 의존한다. 현재 CI workflow는 `testMode: EditMode`만 실행한다 |
| `SrpM1OpeningObservationTests` | 로컬/수동 전용 | 화면 캡처와 해상도 설정에 의존하고, 산출물(PNG·메모)을 사람이 보는 것이 목적이다. 소스상 그래픽 장치가 Null이면 PNG 대신 `.skipped.txt`만 남기므로 그래픽 없는 환경에서는 관찰 목적을 채우지 못한다 |
| `Assets/Tests/Simulation/` | 해당 없음 | 테스트가 아니라 지원 코드 |

현재 CI 상태:

- `.github/workflows/unity-editmode-tests.yml`(`Unity EditMode 테스트`)는 `workflow_dispatch` 수동 실행 전용이고 PR에서 자동 실행되지 않는다.
- 이 workflow는 `game-ci/unity-test-runner`에 `testMode: EditMode`만 넘기고 카테고리 필터를 주지 않는다. 따라서 성공하면 `SrpCombatResolverTests`와 `SrpM1AiSimAllEntry`를 포함한 EditMode 전체가 실행된다.
- PR에서 자동 실행되는 `PR 빠른 체크`는 `scripts/validate-repo.sh`만 실행하며 Unity 테스트를 돌리지 않는다.

## 최소 smoke 기준

Test Runner 창에서 실행하는 최소 기준이다. 전투 로직·HUD·런타임 머티리얼을 건드린 PR은 머지 전에 아래를 확인한다.

1. EditMode: `SrpM1CoreTests`, `SrpM1RuleSpecTests`, `SrpCombatResolverTests`, `SrpMakerMetadataTests`, `SrpRuntimeMaterialTests`가 모두 통과한다.
2. PlayMode: `SrpM1PlayModeTests`와 `SrpM1AiPlaySampleTests`가 모두 통과한다.

변경 범위별 추가 실행:

| 변경 범위 | 추가 실행 |
| ---- | ---- |
| 밸런스 수치, AI 정책, 맵 프리셋 | `SrpAiSim` 카테고리 (메뉴 `SRPG/Run AI Simulation QA (Hybrid)`) |
| HUD 배치, 오버레이, 첫 화면 가독성 | `SrpObservation` 카테고리 실행 후 `TestResults/SrpPlayObservation/` 산출물 육안 확인 |
| 릴리스 전 | `SRPG/Run M1 Automated QA (Edit+Play)` + `SrpCombatResolverTests` 별도 실행 |
| 문서만 변경 | Unity 테스트 생략 가능 |

smoke 기준에서 `SrpM1AiSimAllEntry`와 `SrpM1AllTestsEntry`를 뺀 이유는 각각 실행 비용과 중복이다.

## 실행 방법

### Test Runner 창

1. Unity에서 `Window > General > Test Runner`를 연다.
2. `EditMode` 또는 `PlayMode` 탭을 고른다.
3. 클래스나 테스트를 선택해 `Run Selected`를 누르거나, 카테고리 드롭다운으로 `SrpM1All`·`SrpAiSim`·`SrpObservation`을 고른 뒤 `Run All`을 누른다.

카테고리 없는 `SrpCombatResolverTests`는 카테고리 필터를 해제한 상태에서 클래스를 직접 선택해 실행한다.

### 에디터 메뉴

| 메뉴 | 구현 | 동작 |
| ---- | ---- | ---- |
| `SRPG/Run M1 Automated QA (Edit+Play)` | `Assets/Tests/Editor/SrpM1TestRunnerMenu.cs` | `SrpM1All` EditMode 실행 후 실패가 없으면 `SrpM1All` PlayMode 실행 |
| `SRPG/Run AI Simulation QA (Hybrid)` | `Assets/Tests/Editor/SrpAiSimMenu.cs` | `SrpAiSim` EditMode 실행 후 실패가 없으면 `SrpAiSim` PlayMode 실행. 마지막 JSON 리포트 경로를 콘솔에 출력 |

- 두 메뉴 모두 EditMode에서 실패나 Inconclusive가 나오면 PlayMode를 건너뛰고 콘솔에 오류를 남긴다.
- 결과는 Test Runner 창과 콘솔 로그(`[SRPG][TestRunner]`, `[SRPG][AI-Sim]`)로 확인한다.
- 두 메뉴 모두 `SrpCombatResolverTests`와 `SrpObservation`을 실행하지 않는다.

### batchmode CLI

Unity Test Framework의 명령줄 인자를 쓴다. 아래 명령은 이 저장소에서 성공을 확인하지 못한 형태다([한계](#한계와-미검증-항목) 참고).

```powershell
# EditMode 전체
& "<Unity 6000.3.13f1 경로>\Unity.exe" -batchmode -projectPath . `
  -runTests -testPlatform EditMode `
  -testResults TestResults/editmode-results.xml -logFile Logs/editmode-tests.log

# 카테고리 지정
& "<Unity 6000.3.13f1 경로>\Unity.exe" -batchmode -projectPath . `
  -runTests -testPlatform PlayMode -testCategory "SrpM1All" `
  -testResults TestResults/playmode-results.xml -logFile Logs/playmode-tests.log
```

- `-testPlatform`: `EditMode` 또는 `PlayMode`.
- `-testCategory`: 카테고리 이름. 여러 개는 `;`로 구분한다. 생략하면 해당 플랫폼 전체를 실행한다. `SrpCombatResolverTests`는 카테고리 필터를 준 실행에는 포함되지 않으므로, 전체 실행이나 아래 `-testFilter`로 실행한다.
- `-testFilter`: 테스트 이름으로 고를 때 쓴다. 예: `-testFilter "SrpCombatResolverTests"`.
- `-testResults`: NUnit XML 결과 경로. 종료 코드와 이 파일로 결과를 판단한다.
- `-runTests`와 `-quit`를 함께 쓰지 않는다. Unity Test Framework 문서 기준으로 `-quit`는 테스트가 끝나기 전에 에디터를 종료시킬 수 있다.
- 같은 프로젝트를 Unity 에디터로 열어 둔 상태에서는 batchmode 실행이 프로젝트 잠금으로 실패한다. 워크트리별로 `Library/`가 따로 있으므로 첫 실행은 import 시간이 길다.

## 한계와 미검증 항목

- **CI 성공 기록 없음**: `Unity EditMode 테스트` workflow 실행 기록은 3건이고 모두 실패다(2026-05-03 ~ 2026-05-04, `gh run list` 기준). EditMode 테스트가 CI에서 통과한 기록이 없다. 세 건 모두 16~45초 만에 끝났고 `gh run view`의 step 정보가 비어 있어, 어느 step에서 실패했는지와 테스트 실행 단계까지 도달했는지는 이 문서에서 확인하지 못했다. 녹색화는 #20에서 추적한다.
- **로컬 CLI 미확인**: PR #93 본문에 로컬 Unity CLI `-runTests`가 결과 XML을 생성하지 않아 테스트 러너 결과를 확인하지 못했다는 기록이 있다. 원인은 밝혀지지 않았다. 위 CLI 명령은 Unity Test Framework 문서 기준 형태이며 이 저장소에서 결과 XML 생성까지 확인된 적이 없다.
- **통과 여부 미확인**: 이 문서는 소스 정적 분석으로 작성했다. 현재 main에서 각 묶음이 통과하는지는 확인하지 않았다.
- **실행 시간 미측정**: `SrpM1AiSimAllEntry`와 PlayMode 묶음의 소요 시간을 재지 않았다. CI 후보 분류의 "조건부"는 이 측정이 끝나야 확정된다.
- **PlayMode CI 미구성**: PlayMode를 실행하는 workflow가 없다. headless 컨테이너에서 HUD·캡처 의존 테스트가 동작하는지 모른다.
- **카테고리 누락**: `SrpCombatResolverTests`는 카테고리 기반 실행에서 빠진다. 이 문서는 현재 상태만 기록하며 코드는 바꾸지 않았다.

## 후속 후보

- #20 EditMode 테스트 workflow 녹색화: LFS·라이선스 step 통과 후 첫 성공 기록 확보. 이때 `SrpRuntimeMaterialTests`의 CI 동작과 `SrpM1AiSimAllEntry` 실행 시간을 함께 확인한다.
- #22 Unity 테스트 required check 승격 여부 결정: #20의 성공 기록, 평균 실행 시간, 실패 패턴이 선행 조건이다. AI 시뮬레이션을 required 범위에 넣을지도 여기서 정한다.
- `SrpCombatResolverTests`에 `SrpM1All` 카테고리를 붙일지 결정 (별도 이슈 없음).
- 빠른 smoke 전용 카테고리를 만들어 `SrpM1All`에서 AI 시뮬레이션을 분리할지 결정 (별도 이슈 없음).
- 로컬 batchmode `-runTests`가 결과 XML을 만들지 않는 원인 확인 (별도 이슈 없음).
- PlayMode 테스트의 CI 실행 가능성 검토 (별도 이슈 없음).

## 관련 문서

- [cicd-runbook.md](cicd-runbook.md): CI 테스트 실패 대응
- [release-checklist.md](release-checklist.md): 릴리스 전 테스트 확인 항목
- [../srpg/SRPG_M1_QA_TEST_RUNNER_CHECKLIST.md](../srpg/SRPG_M1_QA_TEST_RUNNER_CHECKLIST.md): SRPG v2 QA 체크리스트
- [../srpg/SRPG_AI_SIMULATION_GUIDE.md](../srpg/SRPG_AI_SIMULATION_GUIDE.md): AI 시뮬레이션 QA 가이드
- [../srpg/SRPG_GDD_TEST_TRACEABILITY.md](../srpg/SRPG_GDD_TEST_TRACEABILITY.md): GDD 항목별 테스트 매핑

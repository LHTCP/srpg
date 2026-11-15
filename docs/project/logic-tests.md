# Unity 없이 하는 검증 (.NET)

Unity 에디터를 띄우지 않고 .NET SDK로 돌리는 검증 두 가지다. 둘 다 Unity에서 하는 확인을 대신하지는 않는다.

| | 전투 로직 테스트 | 어셈블리별 컴파일 확인 |
| --- | --- | --- |
| 실행 | `bash ./scripts/run-logic-tests.sh` | `bash ./scripts/run-compile-check.sh` |
| 하는 일 | 순수 전투 로직과 EditMode 테스트를 컴파일하고 **실행**한다 | `Assets`의 C# 전부를 Unity와 같은 어셈블리 구성으로 **컴파일만** 한다 |
| 컨트롤러·HUD·메이커·로비 코드 | 컴파일하지 않는다 | 컴파일한다 |
| 필요한 것 | .NET SDK | .NET SDK, 설치된 Unity(라이선스는 필요 없다), Unity가 만든 `Library` |
| PR CI | 돈다 | 돌지 않는다(로컬 전용) |

C# 코드를 고쳤다면 둘 다 돌린다. 로직 테스트는 전투 규칙의 동작을 보고, 컴파일 확인은 로직 테스트가 보지 않는 코드까지 컴파일되는지를 본다.

## 전투 로직 테스트

순수 전투 로직과 EditMode 테스트를 .NET SDK로 컴파일·실행한다. Unity 설치도 필요 없다. PR CI(`PR 빠른 체크 / 전투 로직 테스트 (.NET)`)가 같은 스크립트를 돌린다.

### 실행

```bash
bash ./scripts/run-logic-tests.sh
```

- 필요한 것: .NET SDK 8 이상. 첫 실행은 NuGet 패키지를 내려받는다.
- 걸리는 시간: 로컬 기준 약 1분. 대부분 AI 시뮬레이션(전투 2,900회)이다.
- 일부만 돌리기: `bash ./scripts/run-logic-tests.sh --filter "FullyQualifiedName~SrpM1SkillMapTests"`
- 추가 인자(`--logger trx` 등)는 `dotnet test`로 그대로 넘어간다. `--filter`를 주더라도 `RequiresUnity` 제외는 항상 유지된다.
- AI 시뮬레이션 리포트는 테스트 실행 폴더 아래 `tools/logic-tests/bin/Debug/net8.0/TestResults/SrpSim/`에 생긴다(`.gitignore` 대상).

### 무엇을 검증하는가

| 대상 | 포함 여부 |
| --- | --- |
| `Assets/Scripts/SRPG/`의 순수 로직(전투 상태, 해석기, 스킬, AI, 진행, 승패, 시야, 맵·데이터 모델) | 컴파일하고 테스트한다 |
| `Assets/Tests/EditMode/Editor/`의 테스트, `Assets/Tests/Simulation/` | 컴파일하고 실행한다 |
| 컨트롤러·HUD·렌더링·메이커 화면(`SrpGameController*.cs`, `Srp*MakerController.cs`, `SrpLobbyController.cs`, 뷰·카메라·머티리얼) | **컴파일하지 않는다** |
| PlayMode 테스트, `SrpRuntimeMaterialTests` | 실행하지 않는다 |
| `[Category("RequiresUnity")]` 테스트 | 실행하지 않는다 |

즉 이 체크가 통과해도 **컨트롤러 쪽 컴파일 오류와 화면 동작은 보장되지 않는다.** 컴파일 오류는 아래 [어셈블리별 컴파일 확인](#어셈블리별-컴파일-확인)으로 미리 보고, 화면 동작은 Unity 에디터에서 확인한다. 실제로 PR #101에서 `SrpGameController.Hud.cs`의 컴파일 오류가 main에 들어갔는데, 그런 오류는 이 테스트로는 잡지 못한다.

### 구성

- `tools/logic-tests/LogicTests.csproj`: 저장소의 소스 파일을 직접 참조한다. `Assets` 밖이라 Unity는 이 폴더를 보지 않는다.
- `tools/logic-tests/UnityStubs.cs`: `UnityEngine`의 최소 스텁(`Mathf`, `Vector2Int`, `JsonUtility` 등). 로직이 쓰는 멤버만 있다.
- `scripts/run-logic-tests.sh`: 실행 스크립트.

### Unity와 다른 점

- `JsonUtility`는 Newtonsoft.Json으로 대신한다. 직렬화 규칙이 Unity와 완전히 같지 않다. 저장 형식 왕복 테스트는 "필드가 보존되는가" 수준으로만 믿는다.
- `Application.persistentDataPath`는 실행마다 새 임시 폴더다. 개발자 PC의 실제 저장 데이터를 읽거나 덮어쓰지 않는다.
- C# 문법은 9.0으로 제한했다. NUnit은 NuGet의 3.14를 쓰므로, Unity의 NUnit(3.5)에 없는 API(예: `Assert.Multiple`)는 여기서 통과해도 Unity에서 컴파일 오류가 난다. 아래 컴파일 확인은 Unity의 NUnit DLL을 참조하므로 이런 것을 잡는다.

### 유지보수

- 새 순수 로직 파일은 자동으로 포함된다.
- 새 컨트롤러·뷰 파일(MonoBehaviour, UI)을 추가해 컴파일이 깨지면 `LogicTests.csproj`의 `Exclude` 목록에 더한다.
- 로직이 스텁에 없는 `UnityEngine` 멤버를 쓰면 컴파일이 깨진다. 필요한 멤버만 `UnityStubs.cs`에 더한다. 스텁으로 흉내 낼 수 없는 테스트는 `[Category("RequiresUnity")]`를 붙인다.
- CI 비용: standard GitHub-hosted runner만 쓰고 캐시·아티팩트를 쓰지 않는다.

## 어셈블리별 컴파일 확인

Unity를 실행하지 않고, 설치된 Unity의 DLL과 Unity가 만든 `Library`를 참조해 `Assets`의 C# 소스 전부를 Unity와 같은 어셈블리 구성으로 컴파일해 본다. 테스트는 실행하지 않는다. `main`에 병합된 뒤에야 Unity 빌드(Windows Delivery 파이프라인)에서 드러나던 컴파일 오류를 PR 전에 찾으려는 것이다.

### 실행

```bash
bash ./scripts/run-compile-check.sh
```

- 필요한 것: .NET SDK 8 이상, `ProjectSettings/ProjectVersion.txt` 버전의 Unity 설치, Unity가 이 프로젝트를 한 번 이상 열어 만든 `Library`. Unity의 DLL을 읽기만 하므로 라이선스가 활성화돼 있지 않아도 된다. NuGet 패키지는 내려받지 않는다.
- 걸리는 시간: 로컬 기준 약 20초.
- Unity는 Unity Hub 기본 설치 위치(Windows `C:/Program Files/Unity/Hub/Editor/<버전>`, macOS `/Applications/Unity/Hub/Editor/<버전>`, Linux `~/Unity/Hub/Editor/<버전>`)에서 찾는다. 다른 곳에 있으면 `--unity <경로>` 또는 환경 변수 `SRPG_UNITY_PATH`로 준다. 실제로 돌려 본 것은 Windows뿐이다.
- `Library`는 저장소 루트에서 찾고, 워크트리처럼 거기에 없으면 메인 체크아웃에서 찾는다. 다른 곳에 있으면 `--library <경로>` 또는 환경 변수 `SRPG_UNITY_LIBRARY`로 준다.
- 한 변형만 돌리기: `--variant editor` 또는 `--variant player`.
- 다른 워크트리나 내보낸 사본 확인하기: `--source <프로젝트 루트>`. 병합된 `main`과 견줄 때 쓴다.
- 경고 목록까지 보기: `--warnings`. 기본은 개수만 출력한다.
- 컴파일러 메시지는 Unity 콘솔과 견주기 쉽게 영어로 출력한다. 환경 변수 `DOTNET_CLI_UI_LANGUAGE`로 바꿀 수 있다.
- 로그는 `tools/compile-check/obj/<변형>/<어셈블리>.log`에 남는다(`.gitignore` 대상). 실행할 때마다 `tools/compile-check/obj`와 `bin`을 지우고 다시 만들므로, 같은 체크아웃에서 동시에 두 번 돌리지 않는다.

종료 코드:

| 코드 | 뜻 |
| --- | --- |
| 0 | 통과. 고른 변형의 모든 어셈블리를 컴파일했고 오류가 없다 |
| 1 | 실패. 컴파일 오류가 있거나 확인 자체가 성립하지 않는다(소스 0개, 참조 DLL 없음, 대조 불일치) |
| 2 | 건너뜀. Unity 설치나 `Library`가 없어 컴파일하지 않았다. **통과가 아니다** |

PR CI에는 넣지 않는다. GitHub runner에는 Unity DLL과 `Library`가 없어서, 돌려도 건너뜀(2)으로 끝난다.

### 무엇을 컴파일하는가

두 변형을 따로 컴파일한다.

| 변형 | 구성 |
| --- | --- |
| `editor` | 에디터에서 컴파일되는 구성. `UNITY_EDITOR`와 `UNITY_INCLUDE_TESTS`를 정의하고 UnityEditor, nunit, 테스트 러너를 참조한다 |
| `player` | 플레이어 빌드 구성. 두 기호가 없고 UnityEditor와 nunit도 없다. 엔진 DLL은 플레이어용을 쓴다 |

| 어셈블리 | 소스 | 변형 |
| --- | --- | --- |
| `Srp.Runtime` | `Assets/Scripts/SRPG` (asmdef) | editor, player |
| `Srp.Simulation` | `Assets/Tests/Simulation` (asmdef) | editor, player |
| `Srp.PlayMode.Tests` | `Assets/Tests/PlayMode` (asmdef, 테스트 어셈블리) | editor |
| `Assembly-CSharp` | asmdef 폴더에도 `Editor` 폴더에도 들지 않은 나머지(`Assets/TutorialInfo/Scripts` 등) | editor, player |
| `Assembly-CSharp-Editor` | asmdef 밖의 `Editor` 폴더(`Assets/Tests/EditMode/Editor`, `Assets/Tests/Editor` 등) | editor |

참조 구성은 Unity 6000.3.13f1이 실제로 쓴 컴파일러 응답 파일(`Library/Bee/artifacts/*.dag/<어셈블리>.rsp`)과 대조해 맞췄다. 알아 둘 점은 다음과 같다.

- asmdef 어셈블리는 asmdef에 적은 어셈블리만 본다. 다만 `UnityEngine.UI`(에디터에서는 `UnityEditor.UI`도)는 Unity가 asmdef에 없어도 자동으로 참조에 넣는다. 그래서 `Srp.Simulation`이 `UnityEngine.UI`를 쓰는 것은 Unity에서도 오류가 아니고, `TMPro`를 쓰는 것은 오류다.
- 기본 라이브러리는 Unity가 컴파일러에 넘기는 것과 같은 DLL을 쓴다(.NET Standard 2.1, `Assembly-CSharp-Editor`는 .NET Framework 4.8 프로필).
- `player` 변형의 엔진 DLL은 Windows 플레이어용(`PlaybackEngines/windowsstandalonesupport/Variations/mono/Managed`)이다. 에디터 전용 멤버가 빠져 있다. 이 폴더가 없는 설치에서는 에디터용으로 대신하고 그 사실을 출력한다.

### 무엇을 잡는가

- 컨트롤러·HUD·메이커·로비·테스트 코드의 컴파일 오류: 없는 멤버 호출, 타입 불일치, 문법 오류. PR #101로 들어간 `SrpGameController.Hud.cs`의 오류(CS1039)도 그 커밋에 돌리면 그대로 나온다.
- asmdef 경계를 넘는 참조: `Srp.Runtime`이 `Srp.Simulation`의 타입이나 asmdef에 없는 패키지(`UnityEngine.InputSystem` 등)를 쓰는 경우.
- 플레이어 빌드에서만 나는 오류: `#if UNITY_EDITOR` 밖의 `UnityEditor` 사용, `#if UNITY_INCLUDE_TESTS` 안에만 있는 테스트 훅을 밖에서 호출, 그 `#if` 밖에서 NUnit 사용, `UnityEngine` 타입의 에디터 전용 멤버(예: `Texture2D.alphaIsTransparency`) 사용.
- Unity의 NUnit(3.5)에 없는 API 사용(예: `Assert.Multiple`), C# 9.0을 넘는 문법.

### 무엇을 못 잡는가

- 씬, 프리팹, 인스펙터 레퍼런스, 화면 배치, 실제 동작. 컴파일만 하고 아무것도 실행하지 않는다.
- Unity 고유의 검사: MonoBehaviour 클래스 이름과 파일 이름의 일치, 직렬화 규칙, `.meta` 누락, 에셋 임포트 오류.
- 컴파일러 차이: Unity에 든 컴파일러가 아니라 .NET SDK의 컴파일러를 C# 9.0으로 맞춰 쓴다. 드물게 한쪽만 받아들이는 코드가 있을 수 있다. Unity의 소스 생성기도 돌지 않는다.
- API 프로필 차이: 기본 라이브러리 참조는 지금 프로젝트 설정(API 호환 수준 .NET Standard 2.1)에 맞춰 고정했다. 설정을 바꾸면 따라가지 않는다. IL2CPP에서만 생기는 문제도 보지 못한다.
- 패키지 타입의 에디터 전용 멤버: 패키지 DLL(`UnityEngine.UI`, `Unity.TextMeshPro` 등)은 `Library/ScriptAssemblies`에 있는 에디터용 빌드를 쓴다. `player` 변형에서도 그 타입의 에디터 전용 멤버가 보인다.
- 낡은 `Library`: 패키지 DLL은 Unity가 마지막으로 컴파일한 시점의 것이다. 소스의 `Packages/packages-lock.json`과 패키지 버전이 다르면 주의를 출력하지만 실패로 막지는 않는다. Unity에서 프로젝트를 열어 `Library`를 갱신해야 맞는 결과가 나온다.
- 참조가 Unity보다 조금 넓거나 좁은 곳: 프로젝트가 끈 엔진 모듈(AMD, NVIDIA, Hierarchy)이 보이고, `Assembly-CSharp`와 `Assembly-CSharp-Editor`에서는 자동 참조가 아닌 패키지 어셈블리 몇 개가 더 보인다. 반대로 패키지에 든 플러그인 DLL(Mono.Cecil 등)은 보이지 않는다.
- 테스트를 포함한 플레이어 빌드(플레이어에서 `UNITY_INCLUDE_TESTS`가 켜지는 구성)는 보지 않는다.

통과는 "이 구성에서 컴파일 오류가 없다"는 뜻이다. Unity에서 확인한 것이 아니므로, 작업 보고에는 Unity를 실행하지 못했다는 사실을 그대로 적는다.

### 헛돌기 방지

확인용 프로젝트의 경로가 틀려 아무 파일도 컴파일하지 않았는데 "오류 0"으로 보고된 적이 있다([SRPG_CHANGELOG.md](../srpg/SRPG_CHANGELOG.md)의 PR #101 후속 항목). 그래서 다음을 통과로 치지 않는다.

- 어셈블리마다 컴파일한 파일 수를 출력한다. 0개면 실패다.
- 필요한 참조 DLL이 없으면 컴파일 전에 실패한다.
- 매번 이전 산출물을 지우고 처음부터 컴파일하며, 컴파일러가 DLL을 실제로 만들었는지 확인한다.
- 소스 대조: `Assets`의 `.cs`가 `editor` 변형에서 빠짐없이 한 번씩 컴파일됐는지 본다. 새 asmdef 폴더나 `Assets/Plugins` 같은 firstpass 폴더가 생기면 여기서 걸린다.
- asmdef 대조: asmdef 내용이 프로젝트 파일이 흉내 내는 상태와 다르면 실패한다.
- 전처리 기호: 소스가 `#if`에 `UNITY_EDITOR`, `UNITY_INCLUDE_TESTS` 말고 다른 기호를 쓰면 실패한다. 그 기호의 정의 여부를 정해 두지 않았기 때문이다.
- Unity 설치나 `Library`가 없으면 무엇이 없는지 출력하고 종료 코드 2로 끝낸다. 이때는 "오류 0"을 출력하지 않는다.

### 구성과 유지보수

- `tools/compile-check/Directory.Build.props`, `Directory.Build.targets`: 공통 설정, Unity가 자동으로 붙이는 참조, 위의 확인.
- `tools/compile-check/<어셈블리>/<어셈블리>.csproj`: 어셈블리별 소스와 참조. `Assets` 밖이라 Unity는 이 폴더를 임포트하지 않는다.
- `scripts/run-compile-check.sh`: Unity와 `Library`를 찾고, 어셈블리를 순서대로 컴파일하고, 결과를 모은다.

고쳐야 하는 때:

- asmdef를 고쳤다: 그 어셈블리의 csproj에서 참조를 같은 뜻으로 고치고, `SrpAsmdefExpected`를 오류 메시지가 알려 주는 값으로 바꾼다.
- asmdef 폴더를 새로 만들었다: 같은 이름의 프로젝트 폴더와 csproj를 더하고 `scripts/run-compile-check.sh`의 어셈블리 목록에 넣는다.
- 새 전처리 기호를 쓴다: `Directory.Build.targets`의 `DefineConstants`에서 변형별 정의 여부를 정하고 스크립트의 `known_symbols`에 더한다.
- Unity 버전을 올렸다: Unity에서 프로젝트를 열어 `Library`를 갱신한 뒤 응답 파일과 참조 구성을 다시 대조한다. 기본 라이브러리 폴더 이름(`NetStandard/ref/2.1.0` 등)이 바뀌었다면 참조 DLL 누락으로 실패하므로 `Directory.Build.targets`를 고친다.

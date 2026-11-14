# 전투 로직 테스트 (.NET, Unity 없이)

Unity 에디터나 라이선스 없이 순수 전투 로직과 EditMode 테스트를 .NET SDK로 컴파일·실행하는 방법이다. PR CI(`PR 빠른 체크 / 전투 로직 테스트 (.NET)`)가 같은 스크립트를 돌린다.

## 실행

```bash
bash ./scripts/run-logic-tests.sh
```

- 필요한 것: .NET SDK 8 이상. 첫 실행은 NuGet 패키지를 내려받는다.
- 걸리는 시간: 로컬 기준 약 1분. 대부분 AI 시뮬레이션(전투 2,900회)이다.
- 일부만 돌리기: `bash ./scripts/run-logic-tests.sh --filter "FullyQualifiedName~SrpM1SkillMapTests"`
- 추가 인자(`--logger trx` 등)는 `dotnet test`로 그대로 넘어간다. `--filter`를 주더라도 `RequiresUnity` 제외는 항상 유지된다.
- AI 시뮬레이션 리포트는 테스트 실행 폴더 아래 `tools/logic-tests/bin/Debug/net8.0/TestResults/SrpSim/`에 생긴다(`.gitignore` 대상).

## 무엇을 검증하는가

| 대상 | 포함 여부 |
| --- | --- |
| `Assets/Scripts/SRPG/`의 순수 로직(전투 상태, 해석기, 스킬, AI, 진행, 승패, 시야, 맵·데이터 모델) | 컴파일하고 테스트한다 |
| `Assets/Tests/EditMode/Editor/`의 테스트, `Assets/Tests/Simulation/` | 컴파일하고 실행한다 |
| 컨트롤러·HUD·렌더링·메이커 화면(`SrpGameController*.cs`, `Srp*MakerController.cs`, `SrpLobbyController.cs`, 뷰·카메라·머티리얼) | **컴파일하지 않는다** |
| PlayMode 테스트, `SrpRuntimeMaterialTests` | 실행하지 않는다 |
| `[Category("RequiresUnity")]` 테스트 | 실행하지 않는다 |

즉 이 체크가 통과해도 **컨트롤러 쪽 컴파일 오류와 화면 동작은 보장되지 않는다.** 그쪽은 Unity 에디터에서 확인한다. 실제로 PR #101에서 `SrpGameController.Hud.cs`의 컴파일 오류가 main에 들어갔는데, 그런 오류는 이 체크로도 잡지 못한다.

## 구성

- `tools/logic-tests/LogicTests.csproj`: 저장소의 소스 파일을 직접 참조한다. `Assets` 밖이라 Unity는 이 폴더를 보지 않는다.
- `tools/logic-tests/UnityStubs.cs`: `UnityEngine`의 최소 스텁(`Mathf`, `Vector2Int`, `JsonUtility` 등). 로직이 쓰는 멤버만 있다.
- `scripts/run-logic-tests.sh`: 실행 스크립트.

## Unity와 다른 점

- `JsonUtility`는 Newtonsoft.Json으로 대신한다. 직렬화 규칙이 Unity와 완전히 같지 않다. 저장 형식 왕복 테스트는 "필드가 보존되는가" 수준으로만 믿는다.
- `Application.persistentDataPath`는 실행마다 새 임시 폴더다. 개발자 PC의 실제 저장 데이터를 읽거나 덮어쓰지 않는다.
- C# 문법은 9.0으로 제한했다. Unity의 NUnit에 없는 API(예: `Does.Contain(object)`)는 여기서 통과해도 Unity에서 컴파일 오류가 난다.

## 유지보수

- 새 순수 로직 파일은 자동으로 포함된다.
- 새 컨트롤러·뷰 파일(MonoBehaviour, UI)을 추가해 컴파일이 깨지면 `LogicTests.csproj`의 `Exclude` 목록에 더한다.
- 로직이 스텁에 없는 `UnityEngine` 멤버를 쓰면 컴파일이 깨진다. 필요한 멤버만 `UnityStubs.cs`에 더한다. 스텁으로 흉내 낼 수 없는 테스트는 `[Category("RequiresUnity")]`를 붙인다.
- CI 비용: standard GitHub-hosted runner만 쓰고 캐시·아티팩트를 쓰지 않는다.

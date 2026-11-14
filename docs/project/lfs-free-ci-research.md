# LFS-free CI 가능성 조사

이 문서는 GitHub issue #15의 산출물이다. 목표는 CI가 사설 LFS 서버에 접근하지 않고도 최소 테스트/빌드를 돌릴 수 있는지 근거를 정리하는 것이다. 최종 선택은 #16(사람이 내리는 결정)에서 한다.

- 조사 기준 커밋: `b6477f5` (2026-11-14 시점의 `origin/main` tip)
- 조사일: 2026-11-14

## 결론

1. **실측은 완료하지 못했다.** pointer-only 클론에서 Unity 6000.3.13f1 batchmode를 실행했으나, 세 번 모두 Unity 라이선스 단계에서 return code 198로 종료되어 프로젝트 import·컴파일·테스트에 도달하지 못했다. 따라서 "pointer만으로 import 통과", "폰트 없이 EditMode 테스트 통과"는 **미검증**이다.
2. 정적 분석으로는 EditMode 테스트가 LFS 파일 3개(폰트)에 의존하는 코드 경로를 찾지 못했다. LFS-free EditMode는 **가능성이 높지만 확정은 아니다.**
3. 플레이어 빌드(Windows, WebGL)는 TMP 기본 폰트가 LFS 파일이므로 LFS 없이 만들면 텍스트가 깨진 산출물이 된다. **빌드 workflow는 LFS를 유지해야 한다.**
4. 잠정 추천: 빌드는 LFS 유지(#16 선택지 A), EditMode workflow만 LFS step을 빼는 분리는 아래 "남은 검증"을 1회 통과한 뒤에 채택한다. 검증 전에는 현재 구조를 바꾸지 않는다.

## 조사 항목별 상태

| issue #15 조사 항목 | 상태 | 근거 |
| ---- | ---- | ---- |
| EditMode 테스트가 폰트/대형 에셋 없이 실행 가능한지 | 미검증 (정적 분석만 완료) | 실측이 라이선스에서 막힘. 정적 분석은 "의존 없음" |
| Unity import가 LFS pointer만 있어도 통과 가능한지 | 미검증 | 실측이 import 이전에 종료됨 |
| CI 전용 stub/경량 에셋 전략 검토 | 검토 완료 (문서 검토) | "stub 전략 평가" 절 |
| `GIT_LFS_SKIP_SMUDGE` 기반 workflow 가능성 검토 | 검토 완료 (초안만, 미실행) | "workflow 변경 초안" 절 |

## LFS 추적 대상

`.gitattributes` 규칙:

```gitattributes
*.otf filter=lfs diff=lfs merge=lfs -text
*.ttf filter=lfs diff=lfs merge=lfs -text
*.OTF filter=lfs diff=lfs merge=lfs -text
*.TTF filter=lfs diff=lfs merge=lfs -text
Assets/Fonts/*.asset filter=lfs diff=lfs merge=lfs -text
```

`git lfs ls-files -s` 결과(조사 기준 커밋). LFS 파일은 폰트 3개뿐이고 합계 10,585,474 bytes(약 10.6 MB)다.

| 파일 | 실제 크기 | pointer 크기 | GUID | 참조하는 곳 |
| ---- | ---- | ---- | ---- | ---- |
| `Assets/Fonts/Pretendard-Regular SDF.asset` | 8,660,922 bytes | 132 bytes | `d755922d978e7a842b5883e151cae48a` | `Assets/TextMesh Pro/Resources/TMP Settings.asset`의 `m_defaultFontAsset` |
| `Assets/Fonts/Pretendard-Regular.otf` | 1,574,352 bytes | 132 bytes | `5a1d28fa21b5ee14cab93509d1ca1012` | LFS 밖 파일에서는 GUID 참조 없음. `Pretendard-Regular SDF.asset` 내부에서 `m_SourceFontFile`로 참조한다(`m_AtlasPopulationMode: 1`, Dynamic). 로컬 LFS 캐시(`.git/lfs/objects`)의 원본을 읽어 확인했다 |
| `Assets/TextMesh Pro/Fonts/LiberationSans.ttf` | 350,200 bytes | 131 bytes | `e3265ab4bf004d28a9537516768c1c75` | `LiberationSans SDF.asset`, `LiberationSans SDF - Fallback.asset`의 source font 참조 |

참조 확인 방법: 세 GUID를 저장소 전체에서 grep했다. 씬·프리팹에서 직접 참조하는 곳은 없었고, 위 표의 에셋만 나왔다.

코드에서의 사용:

- `Assets/Scripts/SRPG/SrpFontWarmup.cs`가 `TMP_Settings.defaultFontAsset`을 읽어 한글 글리프를 미리 넣는다. 폰트가 `null`이면 바로 반환한다.
- `Assets/Scripts`, `Assets/Tests`에 `Resources.Load`, `AssetDatabase.Load`로 폰트를 직접 읽는 코드는 없다.
- TMP를 참조하는 스크립트는 `Assets/Scripts` 아래 7개 파일이다(`TMP`/`TextMesh` grep 기준, `SrpFontWarmup.cs` 포함). 폰트를 따로 지정하지 않은 TMP 텍스트는 TMP 기본 폰트(= LFS 파일)를 쓴다.

## EditMode 테스트의 에셋 의존성 (정적 분석)

대상은 `Assets/Tests/EditMode/Editor/`의 7개 파일이다. 별도 asmdef가 없어 기본 Editor 어셈블리로 컴파일된다.

| 파일 | 에셋 의존성 | LFS 의존 |
| ---- | ---- | ---- |
| `SrpCombatResolverTests.cs` | 없음 (순수 규칙 계산) | 없음 |
| `SrpM1CoreTests.cs` | 없음 | 없음 |
| `SrpM1RuleSpecTests.cs` | 없음 | 없음 |
| `SrpMakerMetadataTests.cs` | 없음 | 없음 |
| `SrpM1AllTestsEntry.cs` | 위 테스트 메서드 재호출 | 없음 |
| `SrpRuntimeMaterialTests.cs` | `GameObject.CreatePrimitive` + `SrpRuntimeMaterial`의 `Shader.Find`(URP Unlit 등) | 없음. 셰이더 의존은 있으나 LFS 대상이 아님 |
| `SrpM1AiSimAllEntry.cs` | `TestResults/SrpSim/`에 JSON 리포트를 쓴다 (파일 시스템) | 없음 |

- 테스트 코드에 `AssetDatabase`, `Resources.Load`, `TMPro`, 씬 로드 호출이 없다.
- `Assets/Tests/EditMode/` 안의 `[Test]`/`[TestCase]` 속성 줄은 합계 96줄이다(이 중 `[TestCase]` 8줄). 실제 실행 개수는 측정하지 못했다.
- 한계: 정적 분석은 "테스트 코드가 폰트를 읽지 않는다"까지만 말해 준다. pointer 파일을 import할 때 Unity가 내는 오류가 테스트 러너의 종료 코드나 결과에 영향을 주는지는 실행해야 알 수 있다.

## pointer-only 실측 (2026-11-14)

### 환경

- Windows 11 Pro 10.0.26300, Unity 6000.3.13f1 (`C:/Program Files/Unity/Hub/Editor/6000.3.13f1/Editor/Unity.exe`)
- git 2.49.0.windows.1, git-lfs 3.6.1
- 작업 워크트리가 아닌 격리 클론에서 실행했다.

### 준비

```bash
export GIT_LFS_SKIP_SMUDGE=1
git clone --local --no-hardlinks <저장소> lfsfree-clone
cd lfsfree-clone
git checkout --detach b6477f5ae187715a075d0fde778e0a0696b790b1
git lfs ls-files -s   # 3개 모두 '-' (pointer) 표시 확인
```

세 파일 모두 `version https://git-lfs.github.com/spec/v1`로 시작하는 131~132 bytes 텍스트임을 확인했다.

### 실행한 명령

```bash
# 1) 컴파일/import만
Unity.exe -batchmode -nographics -quit -projectPath <clone> -logFile <log1>

# 2) EditMode 테스트
Unity.exe -batchmode -nographics -projectPath <clone> -runTests -testPlatform EditMode -testResults <xml> -logFile <log2>
```

### 결과

| 실행 | return code | 소요 | 도달 단계 | 비고 |
| ---- | ---- | ---- | ---- | ---- |
| 1) 컴파일/import | 198 | 약 2초 | 라이선스 확인에서 종료 | 로그 91줄, `error CS` 0건(컴파일 자체를 시작하지 않음) |
| 1) 재시도 | 198 | 수 초 | 동일 | 동일 |
| 2) EditMode 테스트 | 198 | 수 초 | 동일 | 결과 XML 생성되지 않음 |

로그의 종료 직전 메시지(세 실행 공통):

```text
[Licensing::Module] Error: Access token is unavailable; failed to update
[Licensing::Client] Error: Code 404 while processing request (status: Found 0 entitlement groups and 0 free entitlements matching requested entitlement ids)
[Licensing::Module] Error: 'com.unity.editor.headless' was not found.
No valid Unity Editor license found. Please activate your license.
Exiting without the bug reporter. Application will terminate with return code 198
```

해석과 한계:

- 로그상 access token을 얻지 못했고 `com.unity.editor.headless` entitlement를 찾지 못했다. 실행한 에이전트는 당시 Unity Hub 프로세스가 떠 있지 않았다고 기록했다(로그로는 재확인 불가). 종료 시점이 프로젝트를 열기 전이므로 LFS와 무관한 이 PC의 라이선스 활성 상태 문제로 본다.
- 클론에 `Library/`가 생성되지 않았다. 즉 에셋 import와 스크립트 컴파일은 **시작되지 않았다.** `error CS` 0건은 "컴파일 오류 없음"의 근거가 아니다.
- pointer 폰트 3개에 대한 import 메시지는 로그에 한 줄도 없다(도달하지 못함).
- 테스트 total/passed/failed 수치는 없다.

### 남은 검증 (사람이 수행)

1. Unity Hub에 로그인해 라이선스를 활성화한 뒤 위 "준비"와 "실행한 명령"을 그대로 다시 돌린다.
2. 기록할 값: 두 실행의 return code, `error CS` 건수, 세 폰트 파일 이름이 들어간 import 오류 줄, 결과 XML 생성 여부, total/passed/failed, 실패 테스트 이름과 메시지, 실패가 폰트 누락 때문인지 여부.
3. 로컬 대신 CI로 확인하려면 아래 workflow 초안을 브랜치에 올려 `workflow_dispatch`로 1회 실행한다. 다만 EditMode workflow는 아직 성공 이력이 없으므로(#20), 실패했을 때 LFS-free 때문인지 다른 원인인지 구분하려면 #20의 녹색화가 먼저다.

## 기존 CI 이력에서 확인한 것

`gh run list`로 확인했다(2026-11-14 조회).

| workflow | 결과 | 의미 |
| ---- | ---- | ---- |
| `Unity EditMode 테스트` | 3회 모두 failure (2026-05-03 ~ 05-04, run `25284725827`, `25285015681`, `25320474654`) | 성공 이력이 없어 LFS 유무를 비교할 기준선이 없다 |
| `LFS 서버 헬스체크` | 최근 2회 success (2026-06-13, run `27467839139`, `27469011155`) | GitHub-hosted runner에서 사설 LFS 접근이 된 적이 있다 |
| `Windows PC 데모 빌드` | 최근 2회 success (run `27470111585`, `27906272150`, 마지막은 2026-06-21 `2d45301`) | LFS pull을 포함한 빌드 경로는 동작한 적이 있다. 조사 기준 커밋 `b6477f5`에서는 실행 이력이 없다 |

## stub 전략 평가

| 전략 | 내용 | 장점 | 단점 / 위험 | 평가 |
| ---- | ---- | ---- | ---- | ---- |
| S0. pointer 그대로 | LFS step만 빼고 pointer 파일을 Unity가 import하게 둔다 | 변경이 가장 작다. stub 관리가 없다 | Unity가 pointer를 폰트/YAML로 읽지 못해 import 오류를 낼 것으로 예상되나, 그 오류가 테스트 실행을 막는지 **미검증** | 1순위 후보. 실측 통과가 전제 |
| S1. CI에서 pointer 파일 삭제 | 테스트 전 step에서 LFS 파일 3개와 `.meta`를 지운다 | import 오류 원인을 제거한다. 저장소에 stub을 넣지 않는다 | TMP 기본 폰트 참조가 missing이 된다. EditMode 테스트는 폰트를 쓰지 않아 영향이 없을 것으로 보이나 미검증. 삭제 목록을 `.gitattributes`와 같이 관리해야 한다 | S0가 import 오류로 막힐 때의 대안 |
| S2. 경량 stub 폰트 커밋 | LFS 밖에 작은 폰트/SDF 에셋을 두고 CI에서 같은 GUID로 바꿔 넣는다 | PlayMode나 UI 검증까지 넓힐 수 있다 | 에셋 이중 관리, GUID 유지, 폰트 라이선스 확인이 필요하다. 현재 EditMode 범위에는 과하다 | 지금은 비추천 |
| S3. 폰트를 LFS 밖으로 이동 | 약 10.6 MB를 일반 git 객체로 전환한다 | 테스트와 빌드 모두 LFS 서버 의존이 사라진다 | Dynamic SDF 폰트(`m_AtlasPopulationMode: 1`)라 에셋이 갱신될 때마다 수 MB가 히스토리에 쌓일 수 있다. public repo에 폰트 바이너리가 직접 들어간다. LFS를 도입한 원래 결정과 충돌한다 | #16에서 별도 선택지로 검토할 가치가 있음. 이 문서는 추천하지 않고 후보로만 남긴다 |

## workflow 변경 초안 (미적용, 미실행)

`actions/checkout@v4`는 `lfs` 입력의 기본값이 `false`라서, LFS step을 빼면 작업 트리에 pointer가 그대로 남는다. `GIT_LFS_SKIP_SMUDGE=1`은 runner에 git-lfs filter가 전역 설정된 경우를 막는 방어용이다.

아래는 `.github/workflows/unity-editmode-tests.yml`에 대한 초안이다. 이 문서에만 있고 workflow 파일은 수정하지 않았다.

```diff
 jobs:
   editmode-tests:
     name: EditMode 테스트 실행
     runs-on: ubuntu-latest
+    env:
+      # EditMode 테스트는 LFS 폰트를 쓰지 않는다. 사설 LFS 서버에 접근하지 않도록 smudge를 끈다.
+      GIT_LFS_SKIP_SMUDGE: "1"
     steps:
       - name: 저장소 체크아웃
         uses: actions/checkout@v4
+        with:
+          lfs: false

-      - name: LFS 오브젝트 가져오기
-        env:
-          LFS_ACCOUNT_ID: ${{ secrets.LFS_ACCOUNT_ID }}
-          LFS_ACCOUNT_PASSWORD: ${{ secrets.LFS_ACCOUNT_PASSWORD }}
-          LFS_URL: ${{ vars.LFS_URL }}
-        run: |
-          ... (LFS URL 대조, credential 주입, git lfs pull 전체 삭제)
+      - name: LFS pointer 상태 기록
+        # LFS-free 실행임을 로그에 남긴다. 실패 원인 분석 때 LFS 누락 여부를 바로 알 수 있다.
+        run: git lfs ls-files -s || true

       - name: Unity Library 캐시 복원
         uses: actions/cache@v4
         with:
           path: Library
-          key: Library-${{ runner.os }}-Unity-6000.3.13f1-${{ hashFiles(...) }}
+          key: Library-${{ runner.os }}-Unity-6000.3.13f1-EditMode-lfsfree-${{ hashFiles(...) }}
           restore-keys: |
-            Library-${{ runner.os }}-Unity-6000.3.13f1-
+            Library-${{ runner.os }}-Unity-6000.3.13f1-EditMode-lfsfree-
```

초안의 주의점:

- Library 캐시 key를 분리한다. 조사 기준 커밋에서 빌드 workflow의 key와 restore-keys는 `...-StandaloneWindows64-`, `...-WebGL-`로 target이 들어가 있어 EditMode 캐시를 복원하지 않는다. 반대로 EditMode workflow의 restore-keys는 `Library-<os>-Unity-6000.3.13f1-`까지만 있어 빌드 캐시(실제 폰트로 import된 Library)를 복원할 수 있다. LFS-free 실행 결과가 캐시 출처에 따라 달라지지 않도록 EditMode 전용 prefix로 좁힌다.
- S1(삭제)로 가게 되면 `LFS pointer 상태 기록` 뒤에 `git lfs ls-files -n`으로 나온 파일과 `.meta`를 지우는 step을 추가한다.
- 이 diff는 조사 기준 커밋 `b6477f5`의 파일을 기준으로 쓴 것이다. #20 작업이 같은 파일(라이선스 env, 캐시 key, permissions)을 바꾸면 줄이 맞지 않으므로, #20을 먼저 반영하고 그 위에 다시 맞춰 적용한다. #20에서 캐시 key가 이미 EditMode 전용 prefix로 좁혀지면 이 초안의 key 변경은 `lfsfree` 구분만 남기면 된다.
- 초안은 실행해 보지 않았다. GitHub-hosted runner에서 LFS step 없이 pointer가 남는지도 CI에서 직접 확인한 것은 아니다.

## 추천안과 한계

추천(잠정):

- **빌드(Windows, WebGL, itch.io 업로드 경로)**: LFS 유지. TMP 기본 폰트가 LFS 파일이라 LFS 없이 만든 빌드는 배포할 수 없다.
- **EditMode 테스트**: LFS-free 분리를 목표로 하되, "남은 검증"을 통과한 뒤에 적용한다. 통과하면 PR 단계 테스트가 사설 LFS 서버의 가용성, secret, 트래픽에서 독립한다.
- **PlayMode 테스트**: 이번 조사 범위 밖이다. UI 텍스트를 띄우는 씬 기반 테스트는 폰트 영향을 받을 수 있어 별도 확인이 필요하다.

한계:

- 이 문서의 "가능성이 높다"는 정적 분석에만 근거한다. Unity 실행으로 확인한 사실은 "이 PC에서는 라이선스가 없어 실행이 안 됐다"뿐이다.
- CI에서 LFS 서버를 계속 쓸지, 분리할지, 자체 runner로 갈지는 #16에서 사람이 결정한다. 이 문서는 근거만 제공한다.
- LFS 서버 트래픽·비용 수치는 조사하지 않았다.

## 후속 이슈 후보

- pointer-only EditMode 실측 재실행: 라이선스가 활성화된 PC 또는 CI dispatch 1회로 위 "남은 검증" 값을 채우고 이 문서를 갱신한다. (#15를 닫기 전 필요)
- EditMode workflow LFS step 제거: 실측 통과 후 위 초안을 적용한다. #20 이후에 진행한다.
- 빌드 workflow와 테스트 workflow의 Library 캐시 key 분리 확인: #21의 캐시 조사와 함께 다룬다.
- PlayMode 테스트의 폰트 의존성 조사: PlayMode를 CI에 올릴 때 연다.
- (선택) 폰트를 LFS 밖으로 옮기는 안(S3) 검토: #16에서 선택지로 다룰지 결정한다.

## 관련 문서

- [cicd-runbook.md](cicd-runbook.md): LFS 실패 대응 절차
- [setup.md](setup.md): 로컬 LFS 설정
- [windows-build-runner-research.md](windows-build-runner-research.md): Windows 빌드 runner 전략 조사

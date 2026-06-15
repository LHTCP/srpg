# SRPG 플레이테스트 피드백 후속 작업 문서 (2026-06-11)

## 2026-06-15 implementation note

- `TBD-017R` is implemented as a structural correction: secondary controls are no longer fixed beside the command rail and are opened through a default-closed tab/drawer with `태세/방향`, `전술 보조`, and `시스템` pages.
- `TBD-016R` is implemented as a semantics correction: occupying cover/obstacles are `SrpCoverObjectData`; directional edge cover remains `SrpCoverSegmentData`.
- `M1OpeningPrototype` central non-walkable cells are currently treated as visible ruin cover objects. Edge cover segments render as low walls/boards on tile edges so they can coexist with units standing on the tile.
- Remaining manual QA: confirm in editor play that the drawer feels like optional depth rather than a second always-visible panel, the battlefield is not over-covered, central ruins explain cover visually, and units never move onto or overlap occupying cover objects.

## 2026-06-11 implementation note

- `TBD-015` correction is implemented: move-hover threat lines are now world-space parabolic `LineRenderer` objects rather than tile marker chains, with separate basic/overwatch visual tiers and overwatch endpoint pulse markers.
- `TBD-016` first pass is implemented: tactical camera controller, perspective/top orthographic toggle on `C`, zoom/pan/focus input, facing arrows, one-tile cover cube visualization, and pan/zoom/focus drift guard are in code and PlayMode smoke coverage.
- `TBD-017` first pass is implemented: command rail/context/inspector/log drawer split, wide collapsible log drawer, player-facing `행동 종료` only, action hover preview continuity, and turn-order hover battlefield highlight are in code and PlayMode smoke coverage.
- Remaining follow-up is polish, not contract: art pass for arrow/cover shapes, camera clamp/edge-scroll feel, final threat line animation/color tuning, and tactical console density in actual editor play.

## 목적

이번 문서는 2026-06-11 플레이 피드백 15개 항목을 다음 구현 목표로 정리한다.

핵심 방향은 전투 화면을 "상시 이동 판단 -> 행동 버튼 hover preview -> 이동 후보 위치 위험 예측"의 3단계 문법으로 재정렬하는 것이다. 지금 화면은 여러 범위가 동시에 보이면서 정보 밀도가 높아졌고, 실제 플레이에서는 무엇을 먼저 판단해야 하는지 흐려진다. 다음 작업은 표시량을 줄이는 것이 아니라, 상태별로 언제 무엇을 보여줄지 계약을 다시 잡는 쪽이 맞다.

## 외부 참고와 적용 방향

| 참고 | 관찰 | 이번 프로젝트 적용 |
| --- | --- | --- |
| [XCOM 2 manual](https://www.feralinteractive.com/en/manuals/xcom2/latest/steam/) | 선택된 병사의 이동 범위, 적 시야 위험, 엄폐 아이콘을 이동 판단에 직접 붙인다. | 기본 상태에서 가장 먼저 보여야 하는 것은 현재 행동 유닛의 이동 가능 범위다. 공격/경계태세/엄폐/스킬 범위는 버튼 hover 때 열어 정보 과밀을 줄인다. |
| [Gotcha Again - XCOM 2 LOS preview mod](https://steamcommunity.com/sharedfiles/filedetails/?id=866874504) | 이동 후보 위치에서 사격 가능, 엄폐 우위, 경계사격 발동 위치를 미리 보여주는 QoL 문법이 강하게 요구된다. | 이동 칸 hover 시 ghost 유닛, 목적지 기준 엄폐 가능성, 적 공격 가능 경고선, 경계사격 강화 경고선을 함께 계산한다. 단, 플레이어가 이미 아는 적 정보만 표시한다. |
| [Enemy Preview Extended - XCOM 2 mod](https://steamcommunity.com/workshop/filedetails/?id=2924342363) | 이동 preview 위치를 기준으로 스킬/효과 범위 아이콘을 다시 평가한다. | 스킬 hover와 이동 tile hover는 같은 preview evaluator를 공유해야 한다. "현재 위치 기준"과 "이동 후보 위치 기준"을 분리한다. |
| [Into the Breach Design Postmortem](https://media.gdcvault.com/gdc2019/presentations/Into%20the%20Breach%20Postmortem%20Final.pdf) | 적 의도와 공격 타입을 UI 제약으로 먼저 정하고 전투 규칙을 그 제약 안에 넣었다. | threat line은 단순 장식이 아니라 "누가, 어디서, 무엇으로, 얼마나 위험하게"를 읽히는 규칙 표시다. 경계사격은 일반 공격 경고보다 강한 계층으로 둔다. |
| [Gears Tactics tactical clarity 해설](https://www.aiandgames.com/p/how-ai-helps-achieve-tactical-clarity) | 전술 명확성은 공격 방향, 이동 의도, 즉시 위협을 카메라 이동 없이 이해시키는 데 있다. | 경고선은 화면 이동 없이 유닛 머리 위 포물선으로 출발지와 목적지를 연결한다. 카메라가 부감/탑뷰로 바뀌어도 읽히도록 world-space 높이를 가진다. |
| [Unity Camera manual](https://docs.unity3d.com/6000.4/Documentation/Manual/CamerasOverview.html) | Unity 카메라는 perspective/orthographic projection을 모두 지원하며, orthographic은 원근 축소를 제거한다. | 카메라 rig를 별도 컨트롤러로 분리하고 부감 perspective와 top/orthographic 모드를 토글한다. |

## 피드백 정규화

1. 기본 표시 범위는 현재 행동 유닛의 이동 가능 범위여야 한다.
2. 경계태세 범위는 경계태세 버튼 hover 때 열린다.
3. 엄폐 가능 범위는 엄폐 버튼 hover 때 열린다.
4. 각 스킬 범위는 해당 스킬 hover 때 열린다.
5. 일반 공격도 버튼을 만들고 hover 때 범위가 열린다.
6. 이동 칸 hover 때 목적지 ghost 유닛, 목적지 기준 엄폐 가능성, 적 공격 가능 경고선, 경계사격 강화 경고선을 보여준다.
7. 방향 표시는 텍스트보다 화살표 아이콘이 낫다.
8. 경계사격에 맞을 수 있으면 일반 공격 경고보다 강한 경고 라인이 필요하다.
9. 로그 창은 넓히되 숨기면 화면 공간을 완전히 반환해야 한다.
10. 전술 콘솔은 한 사이드바에 모두 넣지 말고 depth 있는 추가 사이드바/패널을 켜고 끄는 구조로 개편한다.
11. 행동 종료와 턴 종료의 차이가 불명확하다. 현재 구조에서는 턴 종료 버튼이 필요 없어 보인다.
12. 모든 인간 유닛이 총기를 소지해야 하므로 기본 맵 구성을 다시 짠다. 사거리만 다른 총기로 역할 차이를 만든다.
13. 카메라는 부감과 탑뷰를 모두 지원하고, 마우스 휠 버튼 drag, WASD, 화살표 키 이동을 지원한다.
14. 엄폐물은 눈에 보이는 구조물이어야 한다. 지금은 우선 한 칸을 차지하는 육면체 오브젝트로 세운다.
15. 행동 순서 패널의 유닛 hover 때 해당 유닛을 타일 위에서 하이라이트하고 미리보기 패널에 정보를 보여준다.

## 추천 UX 계약

### 1. 화면 상태 계층

상태는 다음 우선순위로 나눈다.

| 상태 | 항상 보일 것 | 임시 hover preview | 숨길 것 |
| --- | --- | --- | --- |
| 아무 행동도 hover하지 않음 | 현재 행동 유닛 ring, 선택 ring, 이동 가능 marker | 유닛/타일 hover 설명 | 공격/경계태세/스킬/엄폐 범위 |
| 일반 공격 버튼 hover | 이동 marker + 공격 가능 범위 | 사격 가능 적 reticle, LOS/aim line | 경계태세 범위 |
| 경계태세 버튼 hover | 이동 marker + 경계태세 범위 | 경계사격 예상 발동 안내 | 일반 공격 범위 |
| 엄폐 버튼 hover | 이동 marker + 현재 위치 기준 엄폐 가능 source | 엄폐 적용 시 방어/GRD preview | 공격/경계태세 범위 |
| 스킬 버튼 hover | 이동 marker + 해당 스킬 범위 | 대상별 예상 효과 preview | 다른 스킬 범위 |
| 이동 칸 hover | 이동 marker + ghost 유닛 | 목적지 기준 엄폐/공격 가능/피격 위험/경계사격 위험 | 현재 위치 기준 행동 범위 |

권장 원칙:

- `RefreshActiveHighlights`는 기본적으로 이동 가능 범위만 유지한다.
- 공격, 경계태세, 엄폐, 스킬, 상호작용 범위는 `ShowActionPreview(kind)` 계열로 분리한다.
- hover preview는 포인터가 빠지면 반드시 사라져야 한다. 실행 상태와 preview 상태가 섞이면 안 된다.
- 위험 정보는 플레이어가 이미 볼 수 있는 적과 상태만 사용한다. 숨은 적까지 미리 보여주는 것은 QA 전용 플래그로 분리한다.

### 2. 이동 칸 hover preview

이동 후보 칸에 마우스를 올렸을 때 다음을 표시한다.

| 요소 | 표시 방식 | 구현 메모 |
| --- | --- | --- |
| 목적지 ghost 유닛 | 반투명 unit mesh 또는 단순 capsule/cylinder placeholder | 실제 유닛 상태를 변경하지 않는 preview clone. collider 없음. |
| 목적지 기준 엄폐 | 해당 칸에서 엄폐 가능한 source를 초록/연두 계열 marker로 표시 | `TryGetAdjacentCover`를 현재 위치 고정이 아니라 hypothetical anchor 기준으로 평가하는 helper 추가. |
| 목적지 기준 공격 가능 적 | 적 머리 위/발밑에 작은 target reticle 또는 얇은 선 | 사격 가능 여부는 `SrpFirearmAim`/`SrpCombatResolver.CanAttack` 계열과 같은 계약 사용. |
| 적의 일반 공격 위협 | 적 머리 위에서 ghost 유닛 쪽으로 낮은 강도의 포물선 line | LineRenderer 또는 mesh polyline. 머리 위 높이에서 시작해 tile top/ring보다 높게 둔다. |
| 경계사격 위협 | 일반 위협보다 두껍고 채도 높은 포물선 line + endpoint marker | `SrpOverwatch`의 LOS/range helper를 공유한다. 경계사격 가능 적이 있으면 경고 우선순위를 가장 높게 둔다. |
| 위험 요약 | preview panel에 "피격 가능 N, 경계사격 N, 엄폐 가능/불가" | 전장 위 표시와 HUD 텍스트가 같은 evaluator 결과를 쓰도록 한다. |

추천 색상:

- 일반 피격 위험: 낮은 채도의 적색/주황, alpha 0.45.
- 경계사격 위험: 선명한 적색, alpha 0.85, endpoint pulse.
- 내가 공격 가능한 적: 노랑/흰색 reticle, alpha 0.65.
- 엄폐 가능: 연두/청록 계열, 현재 marker보다 작게.

### 3. 행동 버튼 hover preview

일반 공격 버튼을 새로 만든다. 현재는 적 유닛/타일 클릭으로 공격이 가능하지만, 플레이어에게 "공격이라는 모드와 범위"가 UI에서 직접 보이지 않는다.

버튼 hover 계약:

- `일반 공격`: 현재 위치 기준 기본 공격 범위와 사격 가능한 적 preview.
- `경계태세`: 현재 위치 기준 경계태세 범위. 클릭은 기존 경계태세 예약.
- `엄폐`: 현재 위치 기준 엄폐 source와 예상 방어 보정.
- `스킬`: 스킬별 target tile/effect preview. 클릭 전 hover만으로 범위 확인 가능.
- `재장전`, `오버클럭`, `상호작용`: 범위가 있는 경우만 preview. 범위가 없으면 preview panel만 갱신.

구현 추천:

- runtime 생성 uGUI button에 hover binding helper를 추가한다.
- `EventTrigger`로 `PointerEnter`, `PointerExit`를 붙이는 bridge가 가장 빠르다.
- 장기적으로는 `SrpActionButtonView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler`로 분리한다.
- preview 호출은 HUD 코드가 직접 overlay를 만들지 말고 controller의 `ShowActionPreview`/`ClearActionPreview`만 호출한다.

### 4. 방향 화살표

방향은 텍스트보다 world-space 화살표가 낫다.

추천 구현:

- 유닛 발밑 ring 위 또는 유닛 머리 아래에 작은 arrow mesh/decal을 둔다.
- 방향은 `SrpFacing`에 따라 4방향 우선으로 회전한다.
- 카메라 top/부감 토글이 들어가도 읽히도록 화살표는 world-space 바닥 decal + optional billboard label 중 바닥 decal을 1차로 쓴다.
- 기존 ring과 겹치지 않게 y offset은 ring보다 약간 높거나, ring 안쪽에 들어가는 작은 삼각형으로 둔다.

### 5. 로그와 전술 콘솔 재구성

현재 로그/전술 콘솔은 조작, 정보, 목록이 한 사이드바에 섞여 있다. 다음 구조를 권장한다.

| 영역 | 역할 | 표시 정책 |
| --- | --- | --- |
| Command rail | 핵심 행동 버튼: 이동 기본, 일반 공격, 스킬, 엄폐, 경계태세, 재장전, 행동 종료 | 항상 보이되 폭은 작게 |
| Context panel | 선택 행동의 상세 정보, 스킬 목록, 예상 결과 | 버튼 hover/click에 따라 열림 |
| Inspector panel | 유닛/행동 순서 hover 정보 | hover 중에만 열리거나 pinned |
| Log drawer | 넓은 전투 로그 | 펼치면 넓게, 접으면 `SetActive(false)` 또는 `LayoutElement.ignoreLayout=true`로 공간 반환 |

UX 원칙:

- 스크롤이 필요한 장문 정보와 즉시 누르는 버튼을 같은 패널에 넣지 않는다.
- 전술 콘솔 depth는 최대 2단계로 제한한다. 3단계 이상은 modal/tooltip으로 분리한다.
- 숨기기 버튼은 "시각적으로 접힘"이 아니라 실제 layout 공간 반환을 완료 조건으로 둔다.

### 6. 행동 종료/턴 종료

현재 로그와 코드에는 라운드 큐 기반의 활성화 구조가 있고, UI에서는 행동 종료/턴 종료가 혼재한다. 지금 플레이 감각에서는 "턴 종료"가 별도 의미를 만들지 못한다.

추천:

- 플레이어 버튼은 `행동 종료` 하나로 정리한다.
- 라운드 전환은 자동 로그 `라운드 N 시작`으로만 보여준다.
- 미래에 진영 단위 턴이나 전체 player turn이 생기면 그때 `턴 종료`를 다시 도입한다.
- 기존 `OnEndTurnSoft`는 내부 이름은 유지해도 되지만 버튼/tooltip/player-facing text는 `행동 종료`로 통일한다.

### 7. 기본 맵과 총기 규칙

피드백 기준으로는 "인간 유닛은 모두 총기 소지"가 세계관/전투 문법에 더 맞다. 따라서 `M1OpeningPrototype`의 player/enemy 인간 유닛은 모두 `weaponClass = Firearm`을 갖게 하고, 역할 차이는 사거리, 탄약, 스킬, 패시브, AP/RP로 만든다.

권장 1차 데이터 방향:

| 역할 | 무기 감각 | 기본 사거리 | 메모 |
| --- | --- | --- | --- |
| 주인공 | 표준 권총/카빈 | 4 | 균형형. 스킬/패시브로 전장 적응 유지. |
| 탱커 | 짧은 산탄총/권총 | 3 | 사거리는 짧지만 생존/전열 고정 역할 유지. |
| 사격수 | 장총 | 6 | 장거리 공격과 노출 처벌 역할. |
| 마도사 | 의식용 권총/보조 총기 | 3 또는 4 | 기본 공격은 총기, 마법성은 스킬과 태그에서 표현. |
| 일반 적 | 권총/소총 혼합 | 3~5 | 근접 적도 총기 기반으로 바꾸되, 돌격형은 사거리를 짧게 둔다. |

주의:

- `weaponClass == Magic`으로 역할을 구분하던 UI/테스트가 있으면 `tags` 또는 skill set 기반으로 옮겨야 한다.
- 경계태세는 총기 전용이므로 모든 인간 유닛이 경계태세 후보가 된다. 난이도 급상승을 막기 위해 enemy maxAmmo, RP, AI 사용 빈도를 함께 조정한다.
- `M1QaIntegrated`는 기능 검증용이므로 melee/magic 예외 케이스를 남겨도 된다. player-facing `M1OpeningPrototype`만 먼저 재구성한다.

### 8. 카메라

새 `SrpTacticalCameraController`를 권장한다. `SrpGameController.FrameBoardCamera`는 초기 framing만 맡기고, 매 프레임 입력/토글은 별도 컴포넌트로 분리한다.

필수 입력:

- 마우스 휠: zoom.
- 마우스 휠 버튼 drag: pan.
- WASD 또는 화살표 키: pan.
- 버튼 또는 단축키: 부감 perspective / top orthographic 토글.
- Home 또는 별도 버튼: 현재 행동 유닛으로 focus.

카메라 모드:

| 모드 | 용도 | 권장값 |
| --- | --- | --- |
| 부감 perspective | 실제 플레이 기본 후보 | 약 45~55도 pitch, 약한 perspective |
| top orthographic | 전술 판단/QA | orthographic, board bounds 기준 zoom |

주의:

- tile picking은 projection 모드와 무관하게 raycast 기반으로 유지한다.
- threat line 높이와 billboard 정보는 두 카메라 모드에서 모두 읽혀야 한다.

### 9. 엄폐물 시각화

현재 엄폐 규칙은 segment/cover tile 데이터가 있지만, 플레이 화면에서는 구조물로 읽히지 않는다.

1차 구현:

- 한 칸을 차지하는 cover object를 육면체로 렌더링한다.
- 높이는 유닛 허리~가슴 정도로 시작한다.
- walkable false 또는 obstacle tile과 연동해 유닛이 겹치지 않게 한다.
- `blocksLineOfSight`가 true인 cover object는 조금 더 높거나 진한 재질로 구분한다.
- 이후 판자/타일 특정 면 엄폐 segment는 별도 후속으로 둔다.

완료 기준:

- 플레이어가 엄폐 가능한 타일을 "바닥 marker"가 아니라 "전장 오브젝트"로 먼저 인식할 수 있어야 한다.
- 이동 hover preview에서 ghost 유닛과 cover object의 관계가 한눈에 보여야 한다.

### 10. 행동 순서 패널 hover

행동 순서 패널의 token hover는 전장과 inspector panel을 동시에 갱신한다.

계약:

- hover한 token의 유닛 ring/outline을 전장 위에 표시한다.
- 화면 밖이면 카메라를 강제 이동하지 않고 edge indicator 또는 soft pulse만 둔다.
- inspector panel에는 이름, owner, HP/PG/AP/RP, 무기/사거리/탄약, 현재 상태, 다음 행동 순서 정보를 표시한다.
- hover exit 시 전장 highlight와 inspector preview를 정리한다.

## 추천 작업 분할

### P1: `TBD-015` 전투 preview 문법 재정렬

포함 피드백: 1, 2, 3, 4, 5, 6, 8, 15

작업:

- 기본 상태 overlay를 이동 가능 범위 중심으로 축소한다.
- 일반 공격 버튼을 추가하고 hover preview를 연결한다.
- 경계태세/엄폐/스킬 버튼 hover preview를 연결한다.
- 이동 tile hover preview evaluator를 만든다.
- ghost 유닛, 목적지 기준 엄폐 marker, incoming threat line, overwatch threat line을 렌더링한다.
- 행동 순서 token hover 시 전장 highlight와 inspector preview를 연결한다.

검증:

- PlayMode: 활성 유닛 기본 화면에서 이동 marker만 기본 표시되고 공격/경계태세/스킬 범위는 표시되지 않는다.
- PlayMode: 각 행동 버튼 hover enter/exit가 해당 범위를 열고 닫는다.
- PlayMode: 이동 후보 tile hover 시 ghost, 엄폐 가능성, 일반 threat line, 경계사격 강화 threat line이 표시된다.
- PlayMode: 행동 순서 token hover 시 해당 유닛 highlight와 preview panel이 표시된다.
- EditMode: preview evaluator가 실제 state를 변경하지 않는다.

### P1: `TBD-016` 카메라/방향/엄폐물 시각화

포함 피드백: 7, 13, 14

작업:

- `SrpTacticalCameraController`를 추가한다.
- 부감 perspective와 top orthographic 토글을 지원한다.
- MMB drag, WASD, 화살표 키 pan, wheel zoom을 지원한다.
- 방향 표시를 화살표 mesh/decal로 바꾼다.
- 한 칸짜리 cover object cube를 렌더링하고 LOS blocker와 시각 차이를 둔다.

검증:

- PlayMode: 카메라 pan/zoom/toggle/focus가 동작한다.
- PlayMode: 방향 화살표가 facing 변경과 함께 회전한다.
- PlayMode: cover object가 visible mesh로 보이고 tile/ring/ghost와 겹쳐도 읽힌다.
- EditMode: cover object 데이터가 occupancy/LOS 규칙과 충돌하지 않는다.

### P1/P2: `TBD-017` 전술 콘솔/로그/행동 종료 정리

Status: playtest correction pass implemented after the first pass. Remaining work is short editor-play QA for visual density and camera feel, not a new feature expansion.

포함 피드백: 9, 10, 11

작업:

- command rail, context panel, inspector panel, log drawer 구조로 분리한다.
- 전투 HUD에서는 floating tooltip을 기본 설명 수단으로 쓰지 않는다. 일반 공격/경계태세/엄폐/스킬/행동 순서 hover 정보는 `ContextPanel`과 `InspectorPreviewPanel`로 통합한다.
- command rail에는 핵심 행동만 남긴다: 일반 공격, 스킬, 경계태세, 엄폐, 재장전, 상호작용, 행동 종료.
- 태세/방향/오버클럭/위험영역/스킬 취소/되감기/로비는 좁은 context column에서 빼고 별도 `SecondaryActionPanel` 또는 상태 기반 노출로 유지한다.
- 로그는 넓은 drawer로 만들고 collapse 시 layout 공간을 반환한다.
- player-facing `턴 종료`를 제거하거나 `행동 종료`로 통일한다.
- 패널 스크롤 영역과 버튼 영역을 분리한다.

검증:

- PlayMode: player-facing `Tooltip` 오브젝트가 생성되지 않고 hover 설명은 context/inspector panel에 반영된다.
- PlayMode: command rail은 핵심 행동만 포함하고 보조 조작은 secondary panel에 있다.
- PlayMode: 로그 hide 시 화면 layout 공간이 실제로 반환된다.
- PlayMode: action buttons는 항상 접근 가능하고, 스킬/상세 정보는 context panel에서만 스크롤된다.
- PlayMode: 행동 종료 버튼 하나로 현재 활성화가 종료되고 라운드 전환은 자동 로그로만 표시된다.

수동 QA:

- command rail이 덜 답답하게 읽히는지, secondary panel이 전장 조작을 과하게 막지 않는지 확인한다.
- hover 정보가 floating tooltip 없이도 `ContextPanel`/`InspectorPreviewPanel`만으로 이해되는지 확인한다.
- UI 위에서는 카메라 입력이 막히고, 전장 위에서는 정상 동작하는지 확인한다.

카메라 보정 계약:

- 부감/perspective zoom은 별도 focus point와 camera distance를 기준으로 동작한다.
- pan은 focus point를 이동시키고, zoom은 focus point를 유지한 채 distance 또는 orthographic size만 바꾼다.
- perspective zoom 후 focus가 카메라 위치로 튀면 회귀로 본다.

### P1/P2: `TBD-018` M1OpeningPrototype 총기 기반 재구성

포함 피드백: 12

작업:

- `M1OpeningPrototype`의 인간 유닛을 모두 총기 기본 공격으로 바꾼다.
- 역할별 기본 사거리를 재설계한다.
- 마도사/탱커 정체성은 weaponClass가 아니라 skill/tag/passive로 유지한다.
- enemy 경계태세 빈도와 ammo/RP를 같이 조정한다.
- `M1QaIntegrated`에는 melee/magic 예외 QA 케이스를 남긴다.

검증:

- EditMode: `M1OpeningPrototype` 인간 유닛의 `weaponClass == Firearm`, `attackRange > 1`, `maxAmmo > 0`.
- EditMode: `M1QaIntegrated`에는 melee/magic 예외 검증이 유지된다.
- PlayMode/AI sim: 첫 전투 평균 종료 라운드가 기존 목표 범위에서 크게 벗어나지 않는다.
- PlayMode: 일반 공격/경계태세/위험 preview가 총기 기반으로 일관되게 표시된다.

## 구현 접점

| 영역 | 주요 파일 |
| --- | --- |
| 기본 전투 흐름, tile/unit hover | `Assets/Scripts/SRPG/SrpGameController.cs` |
| HUD/버튼/행동 순서/로그 | `Assets/Scripts/SRPG/SrpGameController.Hud.cs` |
| overlay/ring/floating text/aim line | `Assets/Scripts/SRPG/SrpGameController.Rendering.cs` |
| 총기 LOS/경계태세 helper | `Assets/Scripts/SRPG/SrpOverwatch.cs` |
| 공격 가능/피해/엄폐 판정 | `Assets/Scripts/SRPG/SrpCombatResolver.cs`, `Assets/Scripts/SRPG/SrpBattleState.cs` |
| 기본 맵/유닛 데이터 | `Assets/Scripts/SRPG/SrpDefaultMaps.cs`, `Assets/Scripts/SRPG/SrpDefaultUnits.cs` |
| PlayMode 테스트 | `Assets/Tests/PlayMode/SrpM1PlayModeTests.cs`, `Assets/Tests/PlayMode/SrpM1OpeningObservationTests.cs` |

## 다음 에이전트 핸드오프 프롬프트

```text
/goal C:\workdir\srpg 에서 SRPG 플레이테스트 후속 UX 1차를 구현해줘.

기준 문서:
- docs/srpg/SRPG_PLAYTEST_FEEDBACK_2026-06-11_NEXT_WORK.md
- docs/srpg/SRPG_BACKLOG.md
- docs/srpg/SRPG_TDD.md
- docs/srpg/SRPG_GDD_TEST_TRACEABILITY.md

우선 범위는 TBD-015 "전투 preview 문법 재정렬"로 제한한다.
기본 상태에서는 현재 행동 유닛의 이동 가능 범위만 보이게 하고, 일반 공격/경계태세/엄폐/각 스킬 범위는 버튼 hover 때만 열린다.
일반 공격 버튼을 추가하고 hover preview를 구현한다.
이동 가능 칸 hover 시 반투명 ghost 유닛, 목적지 기준 엄폐 가능 표시, 목적지에서 적에게 공격받을 수 있는 포물선 threat line을 보여준다.
경계사격에 맞을 수 있는 경우에는 일반 threat line보다 더 강한 경고선과 endpoint marker를 사용한다.
행동 순서 패널 token hover 시 해당 유닛을 전장 위에 highlight하고 preview/inspector panel에 정보를 표시한다.

구현은 기존 SrpGameController partial 구조를 유지하고, preview evaluator가 실제 battle state를 변경하지 않게 해라.
관련 PlayMode/EditMode 테스트와 docs/srpg 문서를 함께 갱신해라.
로컬 dirty 파일, 특히 Assets/Fonts/Pretendard-Regular SDF.asset 같은 기존 변경은 작업 범위에 포함하지 마라.
검증은 가능한 범위에서 git diff --check, scripts/validate-repo.sh, Unity EditMode/PlayMode batch를 실행하고 결과를 기록해라.
```

## 남은 의사결정

- 경계사격 threat line의 정확한 색상/굵기/애니메이션은 첫 구현 후 실제 화면 QA로 조정한다.
- 이동 preview가 숨은 적 위험까지 보여줄지는 "플레이어가 이미 아는 정보만 표시"를 기본값으로 하고, QA debug mode에서만 전체 표시를 허용한다.
- 카메라 projection 토글 단축키는 `C` 또는 HUD icon button을 1차 후보로 둔다. `Tab`은 이후 target cycling과 충돌할 수 있어 피한다.
- 모든 인간 유닛 총기화는 `M1OpeningPrototype`부터 적용한다. `M1QaIntegrated`는 melee/magic 예외 검증을 위해 유지한다.
- `턴 종료`가 필요한 진영 단위 턴 구조를 도입할 계획이 생기기 전까지 player-facing 버튼은 `행동 종료` 하나로 둔다.
## 2026-06-15 skill selection readability note

- `TBD-017S` is implemented as a skill-selection readability correction: skill choices now open in `SkillSelectionDrawer`, not inside the narrow `ContextPanel`.
  - Width policy: preferred 520px, minimum readable 420px.
  - Row policy: minimum 56px height, `NoWrap + Ellipsis` labels.
  - HUD observation captures now include GameView/ScreenCapture samples for skill drawer open, secondary drawer open, log expanded, and log collapsed states.
  - Manual QA should confirm that skill choices do not crush into one-character columns and that the drawer feels like a deliberate selection surface.

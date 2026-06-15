# SRPG Tactical UI Redesign Research - 2026-06-15

## 목적

이번 조사는 "비슷한 게임을 따라 하기"가 아니라 첫 전투 `M1OpeningPrototype`에서 전장 가시성, 행동 버튼, 선택 유닛 정보, hover/preview 정보, 로그, 턴 순서를 어디에 둬야 플레이어 판단이 끊기지 않는지 정리하기 위한 것이다.

참고 자료:

- XCOM 2 manual, action point and movement range explanation: https://www.feralinteractive.com/en/manual/xcom2/latest/steam/
- XCOM 2 tactical UI reference screenshots: https://interfaceingame.com/games/xcom-2/
- Into the Breach UI screenshot archive: https://interfaceingame.com/games/into-the-breach/
- Into the Breach enemy intention readability discussion: https://atomicbobomb.home.blog/2020/05/17/into-the-breach-enemy-intentions/
- Triangle Strategy tactical tips, speed-based order: https://www.nintendo.com/en-za/News/2022/March/Ten-tactical-tips-for-TRIANGLE-STRATEGY--2181496.html
- Triangle Strategy turn-order notes: https://gamefaqs.gamespot.com/switch/313526-triangle-strategy/faqs/79838/principle-of-combat

Wireframe:

- `docs/srpg/ux/tactical_hud_redesign_wireframe_2026-06-15.svg`

## 레퍼런스 관찰

| 게임 | 전장 가시성 | 행동 버튼 | 선택 유닛 정보 | hover/preview | 로그/상태 | 턴 순서 |
| --- | --- | --- | --- | --- | --- | --- |
| XCOM 2 | 전장을 크게 두고 이동/사격 가능 범위를 전장 overlay로 직접 표시한다. | 선택 유닛 기준 행동이 하단/측면 command 영역에 모인다. | 유닛 flag와 하단 정보로 AP, 체력, 상태를 읽게 한다. | 명중률, 엄폐, 행동 종료 여부처럼 결정 직전 정보가 강하게 드러난다. | 미션/상태 정보는 전장 가장자리로 밀어낸다. | 명시적 긴 AT bar보다는 현재 분대 선택 흐름이 중심이다. |
| Into the Breach | 작은 보드가 핵심이다. UI는 보드 칸, 적 의도, 피해 예고를 가리지 않는다. | 유닛 선택 후 소수 행동을 명확히 보여준다. | 선택 mech와 무기/패시브 정보가 간결하다. | 적 의도와 결과 preview가 거의 게임의 핵심 정보로 동작한다. | 별도 긴 로그보다 즉시 결과 예고와 objective 상태가 중요하다. | 턴 순서 자체보다 적 행동 예고가 우선이다. |
| Triangle Strategy | 전장은 넓게 보이되 하단 정보와 커맨드가 전술 RPG 문법을 만든다. | Move, command, item, wait 흐름이 유닛 턴에 종속된다. | HP/TP/상태와 고저차, 방향, 위치 정보가 중요하다. | 커서와 선택 대상에 따라 공격 범위와 상세 정보가 바뀐다. | 전투 로그는 보조적이고, 화면 내 즉시 정보가 우선이다. | 속도 기반 행동 순서가 플레이 의사결정에 중요하다. |

## SRPG 현재 문제

- `SecondaryActionPanel`이 command rail 바로 오른쪽에 고정 floating panel로 떠서, 전장 위에 또 하나의 항상 보이는 조작판을 만든다.
- 보조 조작이 태세/방향, 전술 보조, 시스템으로 구분되지 않아 "지금 필요한 조작"과 "항상 보여야 하는 핵심 행동"이 섞인다.
- hover 정보는 tooltip 제거 이후 `ContextPanel`/`InspectorPreviewPanel`로 가는 방향이 맞지만, 보조 패널이 고정 노출되면 전장 가시성을 다시 갉아먹는다.
- 방향성 `SrpCoverSegmentData`를 타일 중앙 cube로 렌더링하면 유닛이 같은 타일에 서 있는 상황이 점유형 장애물처럼 보인다.

## 레이아웃 후보

### 후보 A: 좌측 command rail + context, 우측 log drawer

```text
[CommandRail][Context]  Battlefield  [TurnOrder][LogDrawer]
                         [ActiveUnit] [InspectorPreview]
          [Secondary tabs -> drawer opens only when needed]
```

장점:

- 핵심 행동이 항상 같은 위치에 남는다.
- hover/preview가 tooltip 없이 context/inspector에 유지된다.
- 보조 조작은 닫힌 탭만 남기므로 전장 조작 공간을 거의 차지하지 않는다.
- 로그는 기본 drawer로 읽기 폭을 보장하고, 닫으면 전장 공간을 반환한다.

단점:

- 좌측에 command rail과 context가 같이 있어 화면 폭이 좁은 환경에서는 좌측 정보 밀도가 높다.
- tab strip과 drawer 위치가 너무 왼쪽에 붙으면 command rail의 일부처럼 오해될 수 있다.

### 후보 B: 하단 action dock + 우측 inspector/log stack

```text
TopStatus + TurnOrder
Battlefield
[ActiveUnit][ActionDock][InspectorPreview]      [Log drawer]
```

장점:

- 전장 좌우를 더 열어 둘 수 있다.
- 전술 RPG의 하단 command 문법과 가깝다.
- 마우스 이동 동선이 하단 preview와 행동 버튼 사이에 짧다.

단점:

- 현재 프로젝트의 좌측 command rail/context 분리 작업을 많이 뒤집는다.
- 하단에 active unit, action dock, inspector가 몰리면 preview와 행동 버튼이 서로 밀린다.
- `M1OpeningPrototype`처럼 위아래 이동 루트 판단이 중요한 맵에서 하단 전장 일부를 계속 가릴 수 있다.

### 후보 C: 우측 통합 drawer 중심

```text
Battlefield
TopStatus + TurnOrder
Right drawer: command / inspector / log tabs
```

장점:

- 전장 좌측을 크게 비울 수 있다.
- 모든 보조 정보를 drawer tab으로 강제할 수 있다.

단점:

- 핵심 행동까지 drawer에 들어가면 턴마다 반복하는 기본 조작이 숨겨진다.
- command, preview, log가 같은 영역에서 경쟁해 탭 전환 비용이 커진다.
- XCOM/Into the Breach식 즉시 행동 가시성과 멀어진다.

## 최종 권장안

후보 A를 채택한다.

- 중앙 전장은 최대한 남긴다.
- 상단은 `TopStatusPanel`과 `TurnOrderTracker`로 전투 상태와 행동 순서를 분리한다.
- 좌측은 `CommandRailPanel`과 `ContextPanel`만 기본 노출한다.
- 하단은 `ActiveUnitCardPanel`과 `InspectorPreviewPanel`로 선택 유닛/hover/preview 정보를 받는다.
- 우측은 `LogDrawerPanel`로 둔다. 열린 폭은 최소 320px 이상, 닫힌 상태는 작은 tab 폭만 남긴다.
- 보조 조작은 `SecondaryActionTabStripPanel` + `SecondaryActionPanel` drawer로 둔다. 기본은 닫힘이며, `태세/방향`, `전술 보조`, `시스템` 중 하나의 tab page만 열린다.

## 엄폐 시각화 권장안

- 점유형 엄폐물/장애물: `SrpCoverObjectData`. 해당 타일은 `walkable=false`, `CanStandAt=false`, 중앙 폐허/장애물 mesh로 렌더링한다.
- 방향성 edge 엄폐: `SrpCoverSegmentData`. 타일 중앙을 점유하지 않고 edge 위 낮은 벽/판자/선형 구조로 렌더링한다.
- 이번 `M1OpeningPrototype`의 중앙 비보행 칸은 "빈 구멍"이 아니라 "폐허 엄폐물"로 해석한다. 따라서 중앙 칸에 점유형 cover object visual을 둔다.
- 장기적으로 중앙 칸을 진짜 구멍/빈 공간으로 해석해야 한다면 `terrain semantics`를 별도 도입해 `walkable=false`와 `cover object`를 분리 유지한다.

## 검증 기준

- `SecondaryActionPanel`은 기본 고정 노출되지 않는다.
- 보조 drawer를 열면 폭이 320px 이상이고 한 tab page만 열린다.
- drawer를 닫으면 전장 공간을 반환한다.
- command rail, context, inspector, log, turn order가 겹치지 않는다.
- hover 정보는 floating tooltip이 아니라 `ContextPanel`/`InspectorPreviewPanel`에 남는다.
- 점유형 cover object는 유닛 시작 위치와 겹치지 않고 `CanStandAt=false`다.
- edge cover segment는 이동 점유를 막지 않으며 중앙 cube가 아니라 edge wall visual이다.

## 레퍼런스 원칙의 구현 매핑

| 원칙 | 참고 레퍼런스 | SRPG 구현 정책 | 현재 코드/검증 계약 |
| --- | --- | --- | --- |
| 핵심 명령은 항상 같은 자리에 둔다 | XCOM 2, Triangle Strategy | `CommandRailPanel`은 일반 공격, 스킬, 경계태세, 엄폐, 재장전, 상호작용, 행동 종료만 고정 노출한다. | `SrpGameController.Hud.BuildLeftPanel`, `SrpM1PlayModeTests.DefaultOpeningPrototypePreset_InitializesRoundAndHud` |
| 스킬 선택은 좁은 요약 패널에 끼우지 않는다 | XCOM 2의 능력 선택 영역, Triangle Strategy의 command 선택 흐름 | 스킬 목록은 `ContextPanel` 자식이 아니라 캔버스 직속 `SkillSelectionDrawer`에 열린다. | drawer는 기본 닫힘, 열림 시 `TestSkillSelectionDrawerDetachedFromCommandContext`가 true여야 한다. |
| 선택지는 한눈에 읽히는 폭과 행 높이를 가진다 | Into the Breach의 짧고 명확한 무기 선택, XCOM 2의 action affordance | `SkillSelectionDrawer` 폭은 선호 520px, 최소 420px이다. 스킬 행 높이는 최소 56px이다. | `TestSkillSelectionDrawerWidth >= 420`, `TestSkillSelectionMinRowHeight >= 56` |
| 긴 이름은 글자 하나 단위로 찌그러지지 않는다 | 세 레퍼런스 공통의 action label 안정성 | 스킬 행 텍스트는 `NoWrap + Ellipsis` 정책을 쓴다. 세부 설명은 hover/preview 패널로 보낸다. | `TestSkillSelectionTextUsesNoWrapEllipsis` |
| hover/preview 정보는 tooltip 대신 고정 정보 패널로 보낸다 | Into the Breach의 결과 preview, XCOM 2의 명중/엄폐 preview | 스킬 hover는 `ContextPanel`/`InspectorPreviewPanel`을 갱신하고, 선택 drawer는 선택지만 담당한다. | `TestShowFirstSkillHoverPreview`, `TestHasPlayerFacingFloatingTooltip == false` |
| UI 검수 이미지는 실제 HUD가 포함되어야 한다 | 구현 검수 원칙 | camera-render 캡처는 전장 표본으로 유지하고, ScreenSpaceOverlay HUD 검수는 GameView/ScreenCapture 캡처를 별도로 남긴다. | `SrpM1OpeningObservationTests`가 `05_gameview_hud_skill_selection_drawer.png`부터 `08_gameview_hud_log_collapsed.png`까지 생성한다. |

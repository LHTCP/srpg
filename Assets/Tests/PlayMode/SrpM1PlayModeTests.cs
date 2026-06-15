using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

[Category("SrpM1All")]
public class SrpM1PlayModeTests
{
    [UnityTest]
    public IEnumerator DefaultOpeningPrototypePreset_InitializesRoundAndHud()
    {
        SrpGameSettings.CustomMap = null;
        SrpGameSettings.SelectedPreset = SrpMapPreset.M1OpeningPrototype;
        SrpGameSettings.HasSelectedPreset = true;
        var go = new GameObject("SrpM1PlayModeTests_Controller");
        var controller = go.AddComponent<SrpGameController>();

        // HUD 초기화 타이밍 변동(도메인 리로드/테스트 러너 부하)을 흡수하기 위해
        // 최대 2초(120프레임)까지 준비 상태를 폴링한다.
        const int maxWaitFrames = 120;
        int waited = 0;
        while (!controller.TestHudReady && waited < maxWaitFrames)
        {
            waited++;
            yield return null;
        }

        Assert.IsTrue(controller.TestHudReady, $"HUD 초기화 실패 (waitedFrames={waited})");
        Assert.GreaterOrEqual(controller.TestRoundNumber, 1, "라운드 번호 초기화 실패");
        Assert.Greater(controller.TestCurrentUnitId, 0, "현재 행동 유닛 미설정");
        Assert.AreEqual(9, controller.TestAliveUnitCount(), "기본 첫 전투 프리셋 유닛 수가 초기화 계약과 다릅니다.");
        Assert.IsTrue(controller.TestHasTopStatusPanel, "상단 전투 상태 헤더가 생성되지 않았습니다.");
        Assert.IsTrue(controller.TestHasLeftConsolePanel, "좌측 조작 콘솔이 생성되지 않았습니다.");
        Assert.IsTrue(controller.TestHasCommandRailPanel, "핵심 행동 command rail이 생성되지 않았습니다.");
        Assert.IsFalse(controller.TestHasContextPanel, "legacy command-adjacent ContextPanel should not stay beside the command rail.");
        Assert.IsTrue(controller.TestCommandRailIsOnlyLeftConsoleContent, "left console should contain only the command rail.");
        Assert.LessOrEqual(controller.TestLeftConsoleWidth, 190f, "left console kept the old context column width.");
        Assert.IsTrue(controller.TestHasSkillSelectionDrawer, "skill selection drawer was not created");
        Assert.IsTrue(controller.TestSkillSelectionDrawerDetachedFromCommandContext, "skill selection must not be parented under command rail/context");
        Assert.IsFalse(controller.TestSkillSelectionDrawerOpen, "skill selection drawer should be closed by default");
        Assert.IsTrue(controller.TestHasSecondaryActionPanel, "secondary action drawer root was not created");
        Assert.IsTrue(controller.TestHasSecondaryActionTabStrip, "secondary action tab strip was not created");
        Assert.IsFalse(controller.TestSecondaryActionDrawerOpen, "secondary action drawer should not be fixed-open by default");
        Assert.IsTrue(controller.TestHasInspectorPreviewPanel, "inspector/preview panel이 생성되지 않았습니다.");
        Assert.IsFalse(controller.TestHasPlayerFacingFloatingTooltip, "전투 HUD는 player-facing floating Tooltip 오브젝트를 생성하지 않아야 합니다.");
        Assert.IsTrue(controller.TestHasLogDrawerPanel, "log drawer panel이 생성되지 않았습니다.");
        Assert.IsFalse(controller.TestLogDrawerVisible, "log drawer should be collapsed by default.");
        Assert.IsTrue(controller.TestLogDrawerBodyCollapsed, "default log drawer should return screen/layout space.");
        Assert.LessOrEqual(controller.TestLogDrawerWidth, 93f, "default log drawer width should be the small reopen rail.");
        Assert.IsTrue(controller.TestPrimaryHudPanelsDoNotOverlap, "command/context/inspector/log/turn-order panels should not overlap");
        Assert.IsTrue(controller.TestHasTurnOrderTrackerPanel, "행동 순서 패널이 생성되지 않았습니다.");
        Assert.IsFalse(controller.TestTurnOrderTrackerIsLogChild, "행동 순서 패널이 로그 패널 위/안에 배치되었습니다.");
        Assert.IsTrue(controller.TestTurnOrderCurrentIconHighlighted, "현재 행동 유닛 아이콘 강조가 없습니다.");
        Assert.GreaterOrEqual(controller.TestTurnOrderVisibleIconCount, 4, "행동 순서 아이콘 preview가 부족합니다.");
        Assert.LessOrEqual(controller.TestTurnOrderVisibleIconCount, 6, "행동 순서 아이콘이 너무 많이 노출됩니다.");
        Assert.IsTrue(controller.TestHasActiveUnitCardPanel, "좌측 하단 현재 유닛 카드가 생성되지 않았습니다.");
        Assert.IsTrue(controller.TestHasActionPreviewPanel, "우측 하단 행동 preview 카드가 생성되지 않았습니다.");
        Assert.LessOrEqual(controller.TestLeftConsoleWidth, 190f, "좌측 콘솔이 설명/스킬 영역까지 품는 이전 폭으로 유지되고 있습니다.");
        Assert.IsFalse(controller.TestLogDrawerVisible, "로그 패널은 기본 접힘 상태로 시작해야 합니다.");
        Assert.IsTrue(controller.TestLogDrawerBodyCollapsed, "접힌 로그는 본문 영역을 숨겨야 합니다.");
        Assert.LessOrEqual(controller.TestLogDrawerWidth, 93f, "접힌 로그는 작은 재열기 레일만 차지해야 합니다.");

        var turnHud = controller.TestTurnHudText;
        var statusHud = controller.TestStatusHudText;
        var unitHud = controller.TestUnitHudText;
        var activeCard = controller.TestActiveUnitCardText;
        var previewCard = controller.TestActionPreviewText;
        Assert.IsTrue(controller.TestHasCurrentActionRing, "current action unit ring missing");
        Assert.IsTrue(controller.TestHasSelectedUnitRing, "selected unit ring missing");
        Assert.IsTrue(controller.TestHasTacticalCameraController, "tactical camera controller missing");
        Assert.IsTrue(controller.TestTacticalCameraToggleKeyIsC, "tactical camera view toggle should use C, not Tab");
        Assert.IsTrue(controller.TestTacticalCameraToggleMode(), "tactical camera mode toggle failed");
        Assert.IsTrue(controller.TestTacticalCameraPanZoomFocusStable, "tactical camera pan/zoom/focus drift guard failed");
        Assert.IsTrue(controller.TestPerspectiveZoomChangesFocusDistance, "perspective zoom should change camera-focus distance");
        Assert.IsTrue(controller.TestPanThenZoomKeepsFocusPoint, "perspective zoom after pan should keep the panned focus point");
        Assert.Greater(controller.TestUnitFacingArrowCount, 0, "world-space facing arrows missing");
        Assert.Greater(controller.TestCoverObjectCount, 0, "cover visuals missing");
        Assert.Greater(controller.TestOccupyingCoverObjectCount, 0, "occupying cover object visuals missing");
        Assert.Greater(controller.TestEdgeCoverSegmentVisualCount, 0, "edge cover segment visuals missing");
        Assert.IsTrue(controller.TestEdgeCoverSegmentsRenderOnEdges, "edge cover segments should render as edge walls, not central cubes");
        Assert.IsTrue(controller.TestOccupyingCoverObjectsAvoidUnits, "occupying cover object visual overlaps a unit");
        Assert.Greater(controller.TestLineOfSightCoverObjectCount, 0, "line-of-sight blocking cover visual tier missing");
        Assert.Greater(controller.TestCurrentActionRingWorldY, controller.TestTileSurfaceY, "current action ring is buried in tile");
        Assert.Greater(controller.TestSelectedUnitRingWorldY, controller.TestCurrentActionRingWorldY, "selected ring y offset must be distinct");
        Assert.Greater(controller.TestCurrentActionRingRadiusScale, controller.TestSelectedUnitRingRadiusScale, "current action ring should be the outer ring");
        Assert.Greater(controller.TestMoveOverlayMarkerCount, 0, "movement overlay center markers missing");
        Assert.AreEqual(0, controller.TestAttackPreviewMarkerCount, "basic attack range should stay hidden until button hover");
        Assert.AreEqual(0, controller.TestOverwatchMeshVisualCount, "overwatch range should stay hidden until button hover");
        Assert.AreEqual(0, controller.TestCoverPreviewMarkerCount, "cover range should stay hidden until button hover");
        Assert.AreEqual(0, controller.TestInteractionObjectiveMarkerCount, "interaction range should stay hidden until button hover");
        Assert.Less(controller.TestTileOverlayMaxWorldY, controller.TestCurrentActionRingWorldY, "tile overlay markers should stay below PR #61 unit rings");
        Assert.GreaterOrEqual(controller.TestWorldFeedbackDuration, 1.8f, "feedback text lifetime is too short");
        Assert.GreaterOrEqual(controller.TestWorldFeedbackHoldDuration, 1.0f, "feedback text hold time is too short");
        Assert.GreaterOrEqual(controller.TestFloatingFeedbackSpawnCount, 1, "turn start floating feedback missing");
        StringAssert.Contains("\uD134 \uC2DC\uC791", controller.TestFloatingFeedbackHistory);
        Assert.IsTrue(controller.TestSpawnTwoFeedbackOnCurrentUnit(), "stacked feedback text starts at the same position");
        StringAssert.Contains($"라운드 {controller.TestRoundNumber}", turnHud);
        StringAssert.Contains("m1_opening_prototype", turnHud);
        StringAssert.Contains("라운드", turnHud);
        StringAssert.Contains("상태:", turnHud);
        StringAssert.Contains("위험영역", turnHud);
        Assert.IsFalse(turnHud.Contains("현재:"), "상단 HUD에 현재 유닛 정보가 다시 섞였습니다.");
        Assert.IsFalse(turnHud.Contains("대기:"), "상단 HUD에 행동 순서 preview가 다시 섞였습니다.");
        StringAssert.Contains("맵:", turnHud);
        StringAssert.Contains($"({controller.TestCurrentUnitId})", controller.TestTurnOrderCurrentText);
        StringAssert.Contains("NOW >", controller.TestTurnOrderCurrentText);
        StringAssert.Contains("NEXT 1.", controller.TestTurnOrderPreviewText);
        StringAssert.Contains("SPD", controller.TestTurnOrderTrackerText);
        StringAssert.Contains("P", controller.TestTurnOrderTrackerText);
        Assert.GreaterOrEqual(controller.TestTurnOrderPreviewLineCount, 3, "다음 행동 순서 preview가 부족합니다.");
        Assert.LessOrEqual(controller.TestTurnOrderPreviewLineCount, 5, "다음 행동 순서 preview가 너무 깁니다.");
        StringAssert.Contains("행동 단계", statusHud);
        StringAssert.Contains("공격 후 행동 종료", statusHud);
        StringAssert.Contains("범례:", statusHud);
        StringAssert.Contains("초록=이동", statusHud);
        StringAssert.Contains("청록=패링 가능 스킬", statusHud);
        StringAssert.Contains("파랑=경계태세 marker", statusHud);
        StringAssert.Contains("연두=엄폐", statusHud);
        StringAssert.Contains("방향엄폐", statusHud);
        StringAssert.Contains("노랑=상호작용", statusHud);
        StringAssert.Contains("AP", unitHud);
        StringAssert.Contains("반응:", unitHud);
        StringAssert.Contains("PG", unitHud);
        StringAssert.Contains("태세", unitHud);
        StringAssert.Contains("방향", unitHud);
        StringAssert.Contains("경계태세", controller.TestOverwatchButtonText);
        StringAssert.Contains("일반 공격", controller.TestCommandRailText);
        StringAssert.Contains("스킬", controller.TestCommandRailText);
        StringAssert.Contains("경계태세", controller.TestCommandRailText);
        StringAssert.Contains("엄폐", controller.TestCommandRailText);
        StringAssert.Contains("재장전", controller.TestCommandRailText);
        StringAssert.Contains("상호작용", controller.TestCommandRailText);
        StringAssert.Contains("행동 종료", controller.TestCommandRailText);
        Assert.IsFalse(controller.TestCommandRailText.Contains("오버클럭"), "보조 조작은 command rail에 남기지 않습니다.");
        Assert.IsFalse(controller.TestCommandRailText.Contains("되감기"), "되감기는 secondary panel로 분리되어야 합니다.");
        Assert.IsTrue(controller.TestShowSkillList(), "skill selection drawer should open from command rail skill button");
        Assert.IsTrue(controller.TestSkillSelectionDrawerDetachedFromCommandContext, "opened skill selection must stay detached from command/context panels");
        Assert.IsTrue(controller.TestSkillSelectionDrawerAdjacentToCommandRail, "skill selection drawer must open immediately to the right of CommandRailPanel");
        Assert.GreaterOrEqual(controller.TestSkillSelectionDrawerWidth, 420f, "skill selection drawer collapsed below readable width");
        Assert.LessOrEqual(controller.TestSkillSelectionDrawerWidth, 540f, "skill selection drawer grew beyond the intended action-detail width");
        Assert.Greater(controller.TestSkillSelectionDrawerVisibleScreenArea, 30000f, $"skill selection drawer must be visibly inside the GameView ({controller.TestSkillSelectionDrawerScreenRect})");
        StringAssert.Contains("스킬 선택", controller.TestSkillSelectionDrawerText);
        Assert.IsTrue(controller.TestSkillSelectionDrawerHasCloseButton, "skill selection drawer needs an explicit close button.");
        Assert.GreaterOrEqual(controller.TestSkillSelectionMinRowHeight, 56f, "skill rows are too short for readable action choices");
        Assert.IsTrue(controller.TestSkillSelectionTextUsesNoWrapEllipsis, "skill labels should avoid one-character wrapping and use ellipsis");
        Assert.IsNotEmpty(controller.TestSkillListText, "opened skill selection drawer should list at least one active skill or an empty-state row");
        Assert.IsTrue(controller.TestToggleSkillSelectionDrawerClosedFromCommandButton(), "clicking the skill command again should close the open skill drawer");
        StringAssert.Contains("태세/방향", controller.TestSecondaryActionTabStripText);
        StringAssert.Contains("전술 보조", controller.TestSecondaryActionTabStripText);
        StringAssert.Contains("시스템", controller.TestSecondaryActionTabStripText);
        Assert.IsTrue(controller.TestOpenSecondaryDrawerStance(), "stance/facing drawer tab should open at readable width");
        Assert.GreaterOrEqual(controller.TestSecondaryActionDrawerWidth, 320f, "secondary drawer width collapsed below readable minimum");
        Assert.GreaterOrEqual(controller.TestSecondaryActionDrawerHeight, 180f, "stance/facing drawer is too short");
        Assert.LessOrEqual(controller.TestSecondaryActionDrawerHeight, 220f, "stance/facing drawer is taller than the compact contract");
        Assert.Greater(controller.TestSecondaryActionVisibleScreenArea, 10000f, $"stance/facing drawer must be visibly inside the GameView ({controller.TestSecondaryActionScreenRect})");
        StringAssert.Contains("공격", controller.TestSecondaryActionPanelText);
        StringAssert.Contains("태세", controller.TestSecondaryActionVisibleText);
        Assert.IsTrue(controller.TestOpenSecondaryDrawerTactical(), "tactical assist drawer tab should open as the only active drawer page");
        Assert.GreaterOrEqual(controller.TestSecondaryActionDrawerHeight, 96f, "tactical assist drawer is too short");
        Assert.LessOrEqual(controller.TestSecondaryActionDrawerHeight, 128f, "tactical assist drawer should use compact height");
        Assert.Greater(controller.TestSecondaryActionVisibleScreenArea, 6000f, $"tactical assist drawer must be visibly inside the GameView ({controller.TestSecondaryActionScreenRect})");
        StringAssert.Contains("오버클럭", controller.TestSecondaryActionVisibleText);
        Assert.IsTrue(controller.TestOpenSecondaryDrawerSystem(), "system drawer tab should open as the only active drawer page");
        Assert.GreaterOrEqual(controller.TestSecondaryActionDrawerHeight, 80f, "system drawer is too short");
        Assert.LessOrEqual(controller.TestSecondaryActionDrawerHeight, 110f, "system drawer should use compact height");
        Assert.Greater(controller.TestSecondaryActionVisibleScreenArea, 5000f, $"system drawer must be visibly inside the GameView ({controller.TestSecondaryActionScreenRect})");
        StringAssert.Contains("로비", controller.TestSecondaryActionVisibleText);
        Assert.IsTrue(controller.TestCloseSecondaryDrawerReturnsSpace(), "closing secondary drawer should return battlefield space");
        Assert.IsFalse(controller.TestSecondaryActionDrawerOpen, "secondary drawer stayed visible after close");
        Assert.IsFalse(controller.TestOverwatchButtonText.Contains("오버워치"), "플레이어-facing 버튼에 이전 오버워치 명칭이 남았습니다.");
        StringAssert.Contains("일반 공격", controller.TestBasicAttackButtonText);
        StringAssert.Contains("행동 종료", controller.TestEndActionButtonText);
        Assert.IsTrue(string.IsNullOrEmpty(controller.TestDebugEndTurnButtonText), "플레이어-facing 턴 종료 버튼이 남아 있습니다.");
        StringAssert.Contains("오버클럭", controller.TestOverclockButtonText);
        StringAssert.Contains("재장전", controller.TestReloadButtonText);
        StringAssert.Contains("엄폐", controller.TestCoverButtonText);
        StringAssert.Contains("상호작용", controller.TestInteractButtonText);
        StringAssert.Contains("탄약", unitHud);
        StringAssert.Contains("HP", activeCard);
        StringAssert.Contains("PG", activeCard);
        StringAssert.Contains("AP", activeCard);
        StringAssert.Contains("탄약", activeCard);
        StringAssert.Contains("1/1", activeCard);
        StringAssert.Contains("Preview", previewCard);
        StringAssert.Contains("공격", controller.TestStanceAggressiveButtonText);
        StringAssert.Contains("수비", controller.TestStanceDefensiveButtonText);
        StringAssert.Contains("북", controller.TestFacingNorthButtonText);
        StringAssert.Contains("동", controller.TestFacingEastButtonText);
        StringAssert.Contains("남", controller.TestFacingSouthButtonText);
        StringAssert.Contains("서", controller.TestFacingWestButtonText);
        StringAssert.Contains("SRPG 프로토타입", controller.TestLogText);
        Assert.IsTrue(controller.TestShowLogDrawer(), "로그는 필요할 때 다시 열 수 있어야 합니다.");
        Assert.IsTrue(controller.TestHideLogDrawer(), "로그를 다시 접으면 화면 공간을 반환해야 합니다.");

        int feedbackBeforeEndTurn = controller.TestFloatingFeedbackSpawnCount;
        int currentBeforeEndTurn = controller.TestCurrentUnitId;
        string trackerCurrentBeforeEndTurn = controller.TestTurnOrderCurrentText;
        Assert.IsTrue(controller.TestEndTurnSelectedUnit(), "test end turn failed");
        yield return null;
        Assert.AreNotEqual(currentBeforeEndTurn, controller.TestCurrentUnitId, "행동 종료 후 현재 유닛이 갱신되지 않았습니다.");
        Assert.AreNotEqual(trackerCurrentBeforeEndTurn, controller.TestTurnOrderCurrentText, "행동 순서 패널 현재 유닛 강조가 갱신되지 않았습니다.");
        Assert.IsTrue(controller.TestTurnOrderCurrentIconHighlighted, "턴 진행 후 현재 행동 유닛 아이콘 강조가 사라졌습니다.");
        StringAssert.Contains($"({controller.TestCurrentUnitId})", controller.TestTurnOrderCurrentText);
        StringAssert.Contains("NEXT 1.", controller.TestTurnOrderPreviewText);
        Assert.GreaterOrEqual(controller.TestFloatingFeedbackSpawnCount, feedbackBeforeEndTurn + 2, "turn end/start feedback contract broke");
        StringAssert.Contains("\uD589\uB3D9 \uC885\uB8CC", controller.TestFloatingFeedbackHistory);

        Assert.IsTrue(controller.TestShowLogDrawer(), "log drawer should still be expandable on demand.");
        Assert.IsTrue(controller.TestLogDrawerConsumesLayoutSpace, "expanded log drawer should participate in layout.");
        Assert.IsTrue(controller.TestToggleLogDrawerHiddenReturnsLayoutSpace(), "log drawer hide should remove body layout space and collapse drawer width");
        Assert.IsFalse(controller.TestLogDrawerVisible, "log drawer body is still visible after hide");
        Assert.IsTrue(controller.TestLogDrawerBodyCollapsed, "log drawer capture state must be visibly collapsed");

        Object.Destroy(go);
        yield return null;
    }

    [UnityTest]
    public IEnumerator M1OpeningPrototypePreset_InitializesFromGameSettings()
    {
        var previousPreset = SrpGameSettings.SelectedPreset;
        SrpGameSettings.CustomMap = null;
        SrpGameSettings.SelectedPreset = SrpMapPreset.M1OpeningPrototype;
        SrpGameSettings.HasSelectedPreset = true;

        var go = new GameObject("SrpM1PlayModeTests_OpeningPrototypeController");
        var controller = go.AddComponent<SrpGameController>();

        const int maxWaitFrames = 120;
        int waited = 0;
        while (!controller.TestHudReady && waited < maxWaitFrames)
        {
            waited++;
            yield return null;
        }

        Assert.IsTrue(controller.TestHudReady, $"첫 전투 프리셋 HUD 초기화 실패 (waitedFrames={waited})");
        Assert.AreEqual(9, controller.TestAliveUnitCount(), "첫 전투 프리셋 유닛 수가 초기화 계약과 다릅니다.");
        StringAssert.Contains("m1_opening_prototype", controller.TestTurnHudText);
        StringAssert.Contains("SRPG 프로토타입", controller.TestLogText);

        Object.Destroy(go);
        SrpGameSettings.SelectedPreset = previousPreset;
        SrpGameSettings.HasSelectedPreset = false;
        SrpGameSettings.CustomMap = null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator M1CombatSplit_FirearmVsMelee_IsMaintainedInPlayMode()
    {
        var firearm = new SrpUnitRuntime
        {
            attackPower = 12,
            weaponClass = SrpWeaponClass.Firearm,
            stance = SrpStance.Aggressive,
        };
        var melee = new SrpUnitRuntime
        {
            attackPower = 12,
            weaponClass = SrpWeaponClass.Melee,
            stance = SrpStance.Aggressive,
        };

        var defForFirearm = CreateDefender();
        var defForMelee = CreateDefender();

        var firearmOutcome = SrpCombatResolver.ApplyAttack(firearm, defForFirearm);
        var meleeOutcome = SrpCombatResolver.ApplyAttack(melee, defForMelee);

        Assert.Greater(firearmOutcome.damageToHp, meleeOutcome.damageToHp);
        Assert.Greater(meleeOutcome.damageToPg, firearmOutcome.damageToPg);
        yield return null;
    }

    [UnityTest]
    public IEnumerator FirearmAimPreview_ShowsNonEightDirectionAimLine()
    {
        SrpGameSettings.CustomMap = CreateNonEightDirectionFirearmMap();
        SrpGameSettings.HasSelectedPreset = false;
        var go = new GameObject("SrpM1PlayModeTests_FirearmAimController");
        var controller = go.AddComponent<SrpGameController>();

        const int maxWaitFrames = 120;
        int waited = 0;
        while (!controller.TestHudReady && waited < maxWaitFrames)
        {
            waited++;
            yield return null;
        }
        Assert.IsTrue(controller.TestHudReady, "firearm aim smoke HUD setup failed");

        Assert.IsTrue(controller.TestTryHoverFirstAttackTarget(), "non-8-direction firearm target was not exposed as a basic attack target");
        yield return null;
        Assert.IsTrue(controller.TestHasAimLineOverlay, "firearm aim line overlay was not rendered for hovered target");
        Assert.GreaterOrEqual(controller.TestAimLineOverlayCount, 2, "firearm aim line should show the shot path, not only the target tile");
        StringAssert.Contains("총기 기본 조준", controller.TestActionPreviewText);
        StringAssert.Contains("벡터 조준", controller.TestActionPreviewText);
        StringAssert.Contains("sector", controller.TestActionPreviewText);

        Object.Destroy(go);
        SrpGameSettings.CustomMap = null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator OverwatchReactionKill_RefreshesWorldAndHudImmediately()
    {
        SrpGameSettings.CustomMap = CreateLethalOverwatchMap();
        SrpGameSettings.HasSelectedPreset = false;
        var go = new GameObject("SrpM1PlayModeTests_LethalOverwatchController");
        var controller = go.AddComponent<SrpGameController>();

        const int maxWaitFrames = 120;
        int waited = 0;
        while (!controller.TestHudReady && waited < maxWaitFrames)
        {
            waited++;
            yield return null;
        }
        Assert.IsTrue(controller.TestHudReady, "lethal overwatch HUD setup failed");

        int targetId = controller.TestCurrentUnitId;
        Assert.IsTrue(controller.TestForceLethalOverwatchAgainstCurrentUnit(), "경계태세 사망 직후 mesh/HUD/행동 순서 갱신이 실패했습니다.");
        yield return null;

        Assert.AreEqual(2, controller.TestAliveUnitCount(), "경계태세 사망 결과가 즉시 전투 상태에 반영되지 않았습니다.");
        Assert.AreNotEqual(targetId, controller.TestCurrentUnitId, "사망한 현재 유닛이 행동 순서에 남았습니다.");
        StringAssert.Contains("경계사격", controller.TestLogText);
        StringAssert.Contains("사망", controller.TestLogText);
        StringAssert.Contains("경계사격!", controller.TestFloatingFeedbackHistory);
        Assert.IsFalse(controller.TestLogText.Contains("오버워치"), "플레이어-facing 로그에 이전 오버워치 명칭이 남았습니다.");

        Object.Destroy(go);
        SrpGameSettings.CustomMap = null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator BasicAttackHoverAndExecution_ResolveByDistance()
    {
        SrpGameSettings.CustomMap = CreateAdjacentAndDistantBasicAttackMap();
        SrpGameSettings.HasSelectedPreset = false;
        var go = new GameObject("SrpM1PlayModeTests_BasicAttackKindController");
        var controller = go.AddComponent<SrpGameController>();

        const int maxWaitFrames = 120;
        int waited = 0;
        while (!controller.TestHudReady && waited < maxWaitFrames)
        {
            waited++;
            yield return null;
        }
        Assert.IsTrue(controller.TestHudReady, "basic attack kind HUD setup failed");

        Assert.IsTrue(controller.TestTryHoverFirstAttackTargetOfKind(SrpBasicAttackKind.Melee), "adjacent melee target was not exposed");
        yield return null;
        Assert.IsFalse(controller.TestHasAimLineOverlay, "adjacent basic attack must not render firearm aim line");
        StringAssert.Contains("근접 공격", controller.TestActionPreviewText);

        Assert.IsTrue(controller.TestSetFirstAttackTargetGuardState(SrpBasicAttackKind.Melee, 0, true), "failed to prepare adjacent execution target");
        Assert.IsTrue(controller.TestTryHoverFirstAttackTargetOfKind(SrpBasicAttackKind.Melee), "adjacent execution hover failed");
        yield return null;
        StringAssert.Contains("근접 처단", controller.TestActionPreviewText);

        Assert.IsTrue(controller.TestTryHoverFirstAttackTargetOfKind(SrpBasicAttackKind.Firearm), "non-adjacent firearm target was not exposed");
        yield return null;
        Assert.IsTrue(controller.TestHasAimLineOverlay, "non-adjacent firearm target must render aim line");
        StringAssert.Contains("총기 공격", controller.TestActionPreviewText);

        Assert.IsTrue(controller.TestClickFirstAttackTargetOfKind(SrpBasicAttackKind.Melee), "adjacent melee execution click failed");
        yield return null;
        StringAssert.Contains("공격(근접)", controller.TestLogText);
        StringAssert.Contains("처단:True", controller.TestLogText);

        Object.Destroy(go);
        SrpGameSettings.CustomMap = null;
        yield return null;
    }


    [UnityTest]
    public IEnumerator DangerAreaAndHoverPreview_UpdatesStatusText()
    {
        SrpGameSettings.CustomMap = null;
        SrpGameSettings.SelectedPreset = SrpMapPreset.M1QaIntegrated;
        SrpGameSettings.HasSelectedPreset = true;
        var go = new GameObject("SrpM1PlayModeTests_UxController");
        var controller = go.AddComponent<SrpGameController>();

        const int maxWaitFrames = 120;
        int waited = 0;
        while (!controller.TestHudReady && waited < maxWaitFrames)
        {
            waited++;
            yield return null;
        }
        Assert.IsTrue(controller.TestHudReady, "HUD 준비 실패");

        StringAssert.Contains("위험영역 OFF", controller.TestStatusHudText);
        controller.ToggleDangerArea();
        yield return null;
        Assert.IsTrue(controller.TestDangerAreaVisible, "위험영역 토글 상태 반영 실패");
        Assert.AreEqual(0, controller.TestDangerAttackTintTileCount, "공격/위험 범위는 타일 전체 tint를 쓰지 않아야 합니다.");
        Assert.Greater(controller.TestDangerAttackMeshVisualCount, 0, "공격/위험 범위 marker가 없습니다.");
        Assert.Greater(controller.TestDangerZocWarningRingCount, 0, "danger ZOC warning rings missing");
        Assert.Less(controller.TestTileOverlayMaxWorldY, controller.TestCurrentActionRingWorldY, "danger overlays should not cover unit foot rings");
        StringAssert.Contains("위험영역 ON", controller.TestStatusHudText);
        StringAssert.Contains("범례:", controller.TestStatusHudText);
        StringAssert.Contains("빨강=공격/위험 marker", controller.TestStatusHudText);
        Assert.IsTrue(controller.TestShowCurrentOverwatchRange(), "경계태세 범위 marker 표시 실패");
        Assert.AreEqual(0, controller.TestOverwatchTintTileCount, "경계태세 범위는 타일 전체 tint를 쓰지 않아야 합니다.");
        Assert.Greater(controller.TestOverwatchMeshVisualCount, 0, "경계태세 범위 marker가 없습니다.");
        Assert.IsTrue(controller.TestShowOverwatchPreview(), "경계태세 hover preview가 inspector panel에 반영되지 않았습니다.");
        Assert.IsTrue(controller.TestShowBasicAttackPreview(), "일반 공격 버튼 hover preview가 행동 preview 카드에 반영되지 않았습니다.");
        StringAssert.Contains("일반 공격 Preview", controller.TestActionPreviewText);
        Assert.IsFalse(controller.TestHasContextPanel, "hover preview should route to inspector/preview UI, not a command-adjacent context column.");
        Assert.IsFalse(controller.TestHasPlayerFacingFloatingTooltip, "hover 설명은 floating tooltip 대신 context/inspector panel에 표시되어야 합니다.");
        Assert.IsTrue(controller.TestShowCoverPreview(), "엄폐 hover preview가 유지되지 않았습니다.");
        Assert.IsTrue(controller.TestShowFirstSkillHoverPreview(), "스킬 버튼 hover preview가 범위 또는 preview 카드에 반영되지 않았습니다.");

        bool hovered = controller.TestTryHoverFirstMoveTile();
        Assert.IsTrue(hovered, "hover 가능한 이동 타일이 없음");
        yield return null;
        StringAssert.Contains("행동 단계", controller.TestStatusHudText);
        StringAssert.Contains("범례:", controller.TestStatusHudText);
        StringAssert.Contains("이동 Preview", controller.TestActionPreviewText);
        StringAssert.Contains("이동 비용", controller.TestActionPreviewText);
        Assert.IsTrue(controller.TestHasMovePreviewGhost, "이동 hover ghost 유닛이 표시되지 않았습니다.");
        Assert.IsTrue(controller.TestMovePreviewGhostAvoidsCoverObjects, "move preview ghost unit should not enter occupying cover object tiles");
        StringAssert.Contains("엄폐", controller.TestActionPreviewText);

        if (controller.TestTryHoverFirstThreatenedMoveTile())
        {
            yield return null;
            Assert.Greater(controller.TestCurrentMovePreviewThreatCount, 0, "위협 이동 preview evaluator 결과가 비어 있습니다.");
            Assert.Greater(
                controller.TestMovePreviewThreatLineCount + controller.TestMovePreviewOverwatchThreatLineCount,
                0,
                "이동 목적지 threat line marker가 표시되지 않았습니다.");
            Assert.AreEqual(0, controller.TestMovePreviewThreatTileMarkerCount, "move preview threat should not be rendered as tile marker dots");
            Assert.IsTrue(controller.TestMovePreviewThreatLinesAreWorldSpace, "move preview threat line should be a world-space LineRenderer above tile overlays");
            if (controller.TestMovePreviewThreatLineCount > 0 && controller.TestMovePreviewOverwatchThreatLineCount > 0)
            {
                Assert.Greater(
                    controller.TestMovePreviewOverwatchThreatLineWidth,
                    controller.TestMovePreviewBasicThreatLineWidth,
                    "overwatch threat line should be stronger than basic attack threat line");
            }
            if (controller.TestCurrentMovePreviewOverwatchThreatCount > 0)
                Assert.Greater(controller.TestMovePreviewOverwatchEndpointCount, 0, "경계사격 위험 endpoint marker가 없습니다.");
        }
        controller.OnTileHoverExit(0, 0);
        yield return null;
        Assert.AreEqual(0, controller.TestMovePreviewThreatLineCount + controller.TestMovePreviewOverwatchThreatLineCount, "move preview threat lines should be cleared on hover exit");
        Assert.AreEqual(0, controller.TestMovePreviewOverwatchEndpointCount, "overwatch endpoint marker should be cleared on hover exit");

        controller.OnUnitHoverEnter(controller.TestCurrentUnitId);
        yield return null;
        Assert.IsTrue(controller.TestHasHoverUnitRing, "hover unit ring missing");
        Assert.Greater(controller.TestHoverUnitRingWorldY, controller.TestSelectedUnitRingWorldY, "hover ring y offset must be distinct");
        Assert.Greater(controller.TestSelectedUnitRingRadiusScale, controller.TestHoverUnitRingRadiusScale, "hover ring should be visually inside selected ring");
        StringAssert.Contains("유닛 미리보기", controller.TestStatusHudText);
        StringAssert.Contains("ZOC", controller.TestStatusHudText);
        Assert.AreEqual(controller.TestCurrentUnitId, controller.TestHoveredUnitId);
        Assert.IsTrue(controller.TestForceCurrentUnitIntoEnemyZoc(), "failed to place test unit in ZOC");
        Assert.Greater(controller.TestVisibleUnitStatusBadgeCount, 0, "ZOC/engagement unit badge missing");
        StringAssert.Contains("대상 정보", controller.TestActionPreviewText);

        Assert.IsTrue(controller.TestTryHoverFirstInteractionPoint(), "hover 가능한 상호작용 포인트가 없음");
        yield return null;
        StringAssert.Contains("상호작용 Preview", controller.TestActionPreviewText);
        StringAssert.Contains("AP-1", controller.TestActionPreviewText);

        Assert.IsTrue(controller.TestHoverFirstTurnOrderToken(), "행동 순서 패널 hover 진입 실패");
        yield return null;
        Assert.IsTrue(controller.TestHasHoverUnitRing, "행동 순서 hover가 전장 유닛 하이라이트를 만들지 않았습니다.");
        StringAssert.Contains("대상 정보", controller.TestActionPreviewText);

        if (controller.TestShowSkillList())
        {
            string skillList = controller.TestSkillListText;
            Assert.IsNotEmpty(skillList, "프리셋 v2 스킬 목록이 비어 있습니다.");
            Assert.IsTrue(controller.TestSkillSelectionDrawerDetachedFromLeftConsole, "스킬 선택 UI는 좌측 콘솔 내부에 끼워 넣지 않고 별도 drawer여야 합니다.");
            Assert.IsTrue(controller.TestSkillSelectionDrawerHasCloseButton, "스킬 선택 drawer에는 명시적인 닫기 버튼이 필요합니다.");
            Assert.IsFalse(skillList.Contains("CD:"), "스킬 목록에 이전 쿨다운 약어가 남아 있습니다.");
            Assert.IsFalse(skillList.Contains("CH:"), "스킬 목록에 이전 충전 약어가 남아 있습니다.");
            Assert.IsTrue(
                skillList.Contains("충전") || skillList.Contains("쿨다운") || skillList.Contains("오버클럭") || skillList.Contains("패링 가능"),
                "스킬 목록이 최신 자원/태그 정보를 노출하지 않습니다.");
            Assert.IsTrue(controller.TestCloseSkillListWithCloseButton(), "스킬 선택 drawer 닫기 버튼이 패널을 닫아야 합니다.");
            Assert.IsTrue(controller.TestShowSkillList(), "스킬 선택 drawer는 닫은 뒤에도 다시 열 수 있어야 합니다.");
            Assert.IsTrue(controller.TestToggleSkillListClosedFromCommandButton(), "열린 스킬 선택 drawer는 스킬 버튼 재클릭으로 닫혀야 합니다.");
        }

        controller.OnUnitHoverExit(controller.TestCurrentUnitId);
        yield return null;

        Object.Destroy(go);
        SrpGameSettings.SelectedPreset = SrpMapPreset.M1OpeningPrototype;
        SrpGameSettings.HasSelectedPreset = false;
        SrpGameSettings.CustomMap = null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator DirectControlUi_ChangesStanceFacingAndOverclocksSkill()
    {
        SrpGameSettings.CustomMap = null;
        SrpGameSettings.SelectedPreset = SrpMapPreset.M1QaIntegrated;
        SrpGameSettings.HasSelectedPreset = true;
        var go = new GameObject("SrpM1PlayModeTests_DirectControlController");
        var controller = go.AddComponent<SrpGameController>();

        const int maxWaitFrames = 120;
        int waited = 0;
        while (!controller.TestHudReady && waited < maxWaitFrames)
        {
            waited++;
            yield return null;
        }
        Assert.IsTrue(controller.TestHudReady, "HUD 준비 실패");

        Assert.IsTrue(controller.TestSetSelectedStance(SrpStance.Defensive), "태세 전환 실패");
        yield return null;
        StringAssert.Contains("태세: Defensive", controller.TestUnitHudText);

        Assert.IsTrue(controller.TestSetSelectedFacing(SrpFacing.West), "방향 전환 실패");
        yield return null;
        StringAssert.Contains("방향: West", controller.TestUnitHudText);

        Assert.IsTrue(controller.TestPrepareSelectedUnitForCover(), "엄폐 가능한 유닛 준비 실패");
        yield return null;
        Assert.IsTrue(controller.TestTakeCoverSelectedUnit(), "엄폐 실행 실패");
        yield return null;
        StringAssert.Contains("엄폐", controller.TestLogText);
        StringAssert.Contains("엄폐 중", controller.TestUnitHudText);

        Assert.IsTrue(controller.TestPrepareSelectedUnitForReload(), "재장전 가능한 총기 유닛 준비 실패");
        yield return null;
        Assert.IsTrue(controller.TestReloadSelectedUnit(), "재장전 실행 실패");
        yield return null;
        StringAssert.Contains("재장전", controller.TestLogText);
        StringAssert.Contains("탄약", controller.TestUnitHudText);

        Assert.IsTrue(controller.TestPrepareSelectedUnitForInteraction(), "상호작용 가능한 유닛 준비 실패");
        yield return null;
        StringAssert.Contains("상호작용", controller.TestInteractButtonText);
        StringAssert.Contains("상호작용 가능", controller.TestUnitHudText);
        Assert.IsTrue(controller.TestInteractSelectedUnit(), "상호작용 실행 실패");
        yield return null;
        StringAssert.Contains("상호작용", controller.TestLogText);

        Assert.IsTrue(controller.TestPrepareFirstOverclockableSkill(), "오버클럭 가능한 스킬 준비 실패");
        yield return null;
        Assert.IsTrue(controller.TestOverclockSelectedSkill(), "오버클럭 실행 실패");
        yield return null;
        StringAssert.Contains("오버클럭", controller.TestLogText);
        StringAssert.Contains("피해/회복", controller.TestLogText);
        StringAssert.Contains("강화 대기", controller.TestUnitHudText);

        int feedbackBeforeSkill = controller.TestFloatingFeedbackSpawnCount;
        Assert.IsTrue(controller.TestBeginFirstTargetedSkill(), "targeted skill prepare failed");
        yield return null;
        Assert.Greater(controller.TestFloatingFeedbackSpawnCount, feedbackBeforeSkill, "skill prepare feedback missing");
        StringAssert.Contains("\uC900\uBE44", controller.TestFloatingFeedbackHistory);
        Assert.IsTrue(controller.TestUsePendingSkillOnFirstTarget(), "targeted skill use failed");
        yield return null;
        StringAssert.Contains("!", controller.TestFloatingFeedbackHistory);

        Object.Destroy(go);
        SrpGameSettings.SelectedPreset = SrpMapPreset.M1OpeningPrototype;
        SrpGameSettings.HasSelectedPreset = false;
        SrpGameSettings.CustomMap = null;
        yield return null;
    }

    static SrpUnitRuntime CreateDefender()
    {
        return new SrpUnitRuntime
        {
            hp = 40,
            maxHp = 40,
            pg = 24,
            maxPg = 24,
            stance = SrpStance.Defensive,
        };
    }

    static SrpMapFileV1 CreateNonEightDirectionFirearmMap()
    {
        int width = 5;
        int height = 5;
        var walkable = new bool[width * height];
        for (int i = 0; i < walkable.Length; i++)
            walkable[i] = true;

        return new SrpMapFileV1
        {
            version = 2,
            name = "firearm_non_eight_aim_smoke",
            width = width,
            height = height,
            walkable = walkable,
            playerOrder = new[] { 0, 1 },
            templates = new[]
            {
                new SrpUnitTemplateData
                {
                    id = "shooter",
                    displayName = "Shooter",
                    moveRange = 2,
                    attackRange = 4,
                    attackPower = 8,
                    maxHp = 30,
                    maxPg = 18,
                    maxActionPoints = 2,
                    maxReactionPoints = 1,
                    speed = 20,
                    weaponClass = SrpWeaponClass.Firearm,
                    stance = SrpStance.Aggressive,
                    facing = SrpFacing.North,
                    maxAmmo = 1,
                },
                new SrpUnitTemplateData
                {
                    id = "target",
                    displayName = "Target",
                    moveRange = 2,
                    attackRange = 1,
                    attackPower = 1,
                    maxHp = 30,
                    maxPg = 18,
                    maxActionPoints = 2,
                    maxReactionPoints = 1,
                    speed = 10,
                    weaponClass = SrpWeaponClass.Melee,
                    stance = SrpStance.Aggressive,
                    facing = SrpFacing.West,
                },
            },
            placements = new[]
            {
                new SrpPlacementData { templateId = "shooter", owner = 0, x = 1, y = 1 },
                new SrpPlacementData { templateId = "target", owner = 1, x = 3, y = 2 },
            },
        };
    }

    static SrpMapFileV1 CreateAdjacentAndDistantBasicAttackMap()
    {
        int width = 5;
        int height = 5;
        var walkable = new bool[width * height];
        for (int i = 0; i < walkable.Length; i++)
            walkable[i] = true;

        return new SrpMapFileV1
        {
            version = 2,
            name = "basic_attack_kind_distance_smoke",
            width = width,
            height = height,
            walkable = walkable,
            playerOrder = new[] { 0, 1 },
            templates = new[]
            {
                new SrpUnitTemplateData
                {
                    id = "shooter",
                    displayName = "Shooter",
                    moveRange = 2,
                    attackRange = 4,
                    attackPower = 8,
                    maxHp = 30,
                    maxPg = 18,
                    maxActionPoints = 2,
                    maxReactionPoints = 1,
                    speed = 20,
                    weaponClass = SrpWeaponClass.Firearm,
                    stance = SrpStance.Aggressive,
                    facing = SrpFacing.North,
                    maxAmmo = 1,
                },
                new SrpUnitTemplateData
                {
                    id = "adjacent",
                    displayName = "Adjacent",
                    moveRange = 2,
                    attackRange = 3,
                    attackPower = 1,
                    maxHp = 30,
                    maxPg = 18,
                    maxActionPoints = 2,
                    maxReactionPoints = 1,
                    speed = 10,
                    weaponClass = SrpWeaponClass.Melee,
                    stance = SrpStance.Aggressive,
                    facing = SrpFacing.West,
                },
                new SrpUnitTemplateData
                {
                    id = "distant",
                    displayName = "Distant",
                    moveRange = 2,
                    attackRange = 3,
                    attackPower = 1,
                    maxHp = 30,
                    maxPg = 18,
                    maxActionPoints = 2,
                    maxReactionPoints = 1,
                    speed = 5,
                    weaponClass = SrpWeaponClass.Melee,
                    stance = SrpStance.Aggressive,
                    facing = SrpFacing.West,
                },
            },
            placements = new[]
            {
                new SrpPlacementData { templateId = "shooter", owner = 0, x = 1, y = 1 },
                new SrpPlacementData { templateId = "adjacent", owner = 1, x = 2, y = 1 },
                new SrpPlacementData { templateId = "distant", owner = 1, x = 1, y = 4 },
            },
        };
    }

    static SrpMapFileV1 CreateLethalOverwatchMap()
    {
        int width = 5;
        int height = 5;
        var walkable = new bool[width * height];
        for (int i = 0; i < walkable.Length; i++)
            walkable[i] = true;

        return new SrpMapFileV1
        {
            version = 2,
            name = "lethal_overwatch_refresh_smoke",
            width = width,
            height = height,
            walkable = walkable,
            playerOrder = new[] { 0, 1 },
            templates = new[]
            {
                new SrpUnitTemplateData
                {
                    id = "runner",
                    displayName = "Runner",
                    moveRange = 3,
                    attackRange = 1,
                    attackPower = 1,
                    maxHp = 12,
                    maxPg = 8,
                    maxActionPoints = 2,
                    maxReactionPoints = 1,
                    speed = 30,
                    weaponClass = SrpWeaponClass.Melee,
                    stance = SrpStance.Aggressive,
                    facing = SrpFacing.West,
                },
                new SrpUnitTemplateData
                {
                    id = "ally",
                    displayName = "Ally",
                    moveRange = 3,
                    attackRange = 1,
                    attackPower = 1,
                    maxHp = 12,
                    maxPg = 8,
                    maxActionPoints = 2,
                    maxReactionPoints = 1,
                    speed = 10,
                    weaponClass = SrpWeaponClass.Melee,
                    stance = SrpStance.Defensive,
                    facing = SrpFacing.North,
                },
                new SrpUnitTemplateData
                {
                    id = "watcher",
                    displayName = "Watcher",
                    moveRange = 2,
                    attackRange = 4,
                    attackPower = 8,
                    maxHp = 16,
                    maxPg = 8,
                    maxActionPoints = 2,
                    maxReactionPoints = 1,
                    speed = 20,
                    weaponClass = SrpWeaponClass.Firearm,
                    stance = SrpStance.Aggressive,
                    facing = SrpFacing.East,
                    maxAmmo = 1,
                },
            },
            placements = new[]
            {
                new SrpPlacementData { templateId = "runner", owner = 0, x = 2, y = 1 },
                new SrpPlacementData { templateId = "ally", owner = 0, x = 4, y = 4 },
                new SrpPlacementData { templateId = "watcher", owner = 1, x = 0, y = 1 },
            },
        };
    }
}

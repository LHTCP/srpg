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
        Assert.IsTrue(controller.TestHasActiveUnitCardPanel, "좌측 하단 현재 유닛 카드가 생성되지 않았습니다.");
        Assert.IsTrue(controller.TestHasActionPreviewPanel, "우측 하단 행동 preview 카드가 생성되지 않았습니다.");

        var turnHud = controller.TestTurnHudText;
        var statusHud = controller.TestStatusHudText;
        var unitHud = controller.TestUnitHudText;
        var activeCard = controller.TestActiveUnitCardText;
        var previewCard = controller.TestActionPreviewText;
        Assert.IsTrue(controller.TestHasCurrentActionRing, "current action unit ring missing");
        Assert.IsTrue(controller.TestHasSelectedUnitRing, "selected unit ring missing");
        Assert.GreaterOrEqual(controller.TestFloatingFeedbackSpawnCount, 1, "turn start floating feedback missing");
        StringAssert.Contains("\uD134 \uC2DC\uC791", controller.TestFloatingFeedbackHistory);
        StringAssert.Contains($"라운드 {controller.TestRoundNumber}", turnHud);
        StringAssert.Contains($"({controller.TestCurrentUnitId})", turnHud);
        StringAssert.Contains("m1_opening_prototype", turnHud);
        StringAssert.Contains("라운드", turnHud);
        StringAssert.Contains("현재:", turnHud);
        StringAssert.Contains("대기:", turnHud);
        StringAssert.Contains("맵:", turnHud);
        StringAssert.Contains("행동 단계", statusHud);
        StringAssert.Contains("공격 후 턴 종료", statusHud);
        StringAssert.Contains("범례:", statusHud);
        StringAssert.Contains("초록=이동", statusHud);
        StringAssert.Contains("청록=패링 가능 스킬", statusHud);
        StringAssert.Contains("파랑=오버워치", statusHud);
        StringAssert.Contains("연두=엄폐", statusHud);
        StringAssert.Contains("방향엄폐", statusHud);
        StringAssert.Contains("노랑=상호작용", statusHud);
        StringAssert.Contains("AP", unitHud);
        StringAssert.Contains("반응:", unitHud);
        StringAssert.Contains("PG", unitHud);
        StringAssert.Contains("태세", unitHud);
        StringAssert.Contains("방향", unitHud);
        StringAssert.Contains("오버워치", controller.TestOverwatchButtonText);
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

        int feedbackBeforeEndTurn = controller.TestFloatingFeedbackSpawnCount;
        Assert.IsTrue(controller.TestEndTurnSelectedUnit(), "test end turn failed");
        yield return null;
        Assert.GreaterOrEqual(controller.TestFloatingFeedbackSpawnCount, feedbackBeforeEndTurn + 2, "turn end/start feedback contract broke");
        StringAssert.Contains("\uD134 \uC885\uB8CC", controller.TestFloatingFeedbackHistory);

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
        StringAssert.Contains("위험영역 ON", controller.TestStatusHudText);
        StringAssert.Contains("범례:", controller.TestStatusHudText);
        StringAssert.Contains("빨강=공격/위험", controller.TestStatusHudText);

        bool hovered = controller.TestTryHoverFirstMoveTile();
        Assert.IsTrue(hovered, "hover 가능한 이동 타일이 없음");
        yield return null;
        StringAssert.Contains("행동 단계", controller.TestStatusHudText);
        StringAssert.Contains("범례:", controller.TestStatusHudText);
        StringAssert.Contains("이동 Preview", controller.TestActionPreviewText);
        StringAssert.Contains("이동 비용", controller.TestActionPreviewText);

        controller.OnUnitHoverEnter(controller.TestCurrentUnitId);
        yield return null;
        Assert.IsTrue(controller.TestHasHoverUnitRing, "hover unit ring missing");
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

        if (controller.TestShowSkillList())
        {
            string skillList = controller.TestSkillListText;
            Assert.IsNotEmpty(skillList, "프리셋 v2 스킬 목록이 비어 있습니다.");
            Assert.IsFalse(skillList.Contains("CD:"), "스킬 목록에 이전 쿨다운 약어가 남아 있습니다.");
            Assert.IsFalse(skillList.Contains("CH:"), "스킬 목록에 이전 충전 약어가 남아 있습니다.");
            Assert.IsTrue(
                skillList.Contains("충전") || skillList.Contains("쿨다운") || skillList.Contains("오버클럭") || skillList.Contains("패링 가능"),
                "스킬 목록이 최신 자원/태그 정보를 노출하지 않습니다.");
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
}

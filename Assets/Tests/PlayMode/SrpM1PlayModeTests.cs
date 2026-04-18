using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

[Category("SrpM1All")]
public class SrpM1PlayModeTests
{
    [UnityTest]
    public IEnumerator M1IntegratedPreset_InitializesRoundAndHud()
    {
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
        Assert.GreaterOrEqual(controller.TestAliveUnitCount(), 4, "M1 통합 프리셋 유닛 수 부족");

        var turnHud = controller.TestTurnHudText;
        var statusHud = controller.TestStatusHudText;
        var unitHud = controller.TestUnitHudText;
        StringAssert.Contains($"라운드 {controller.TestRoundNumber}", turnHud);
        StringAssert.Contains($"({controller.TestCurrentUnitId})", turnHud);
        StringAssert.Contains("라운드", turnHud);
        StringAssert.Contains("현재:", turnHud);
        StringAssert.Contains("대기:", turnHud);
        StringAssert.Contains("행동 단계", statusHud);
        StringAssert.Contains("공격 후 턴 종료", statusHud);
        StringAssert.Contains("AP", unitHud);
        StringAssert.Contains("RP", unitHud);
        StringAssert.Contains("PG", unitHud);
        StringAssert.Contains("태세", unitHud);
        StringAssert.Contains("방향", unitHud);

        Object.Destroy(go);
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

        bool hovered = controller.TestTryHoverFirstMoveTile();
        Assert.IsTrue(hovered, "hover 가능한 이동 타일이 없음");
        yield return null;
        StringAssert.Contains("행동 단계", controller.TestStatusHudText);

        controller.OnUnitHoverEnter(controller.TestCurrentUnitId);
        yield return null;
        StringAssert.Contains("유닛 미리보기", controller.TestStatusHudText);
        Assert.AreEqual(controller.TestCurrentUnitId, controller.TestHoveredUnitId);

        controller.OnUnitHoverExit(controller.TestCurrentUnitId);
        yield return null;

        Object.Destroy(go);
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

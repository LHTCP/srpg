using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 8차 테스트: 적 AI 마무리(태세와 방향 선택, 오버클럭, 회복하러 다가가는 이동)와
/// 지휘관 처치 승리가 켜진 맵에서 지휘관이 위험한 칸으로 나서지 않는 판단.
/// </summary>
[Category("SrpM1All")]
public class SrpM1Phase8Tests
{
    // ── 적 AI: 태세 ──────────────────────────────────────────────────────────

    [TestCase(TestName = "적 AI는 사격 위협이 있고 교전하지 않았으면 첫 행동 전에 공격 태세를 고른다")]
    public void EnemyAi_PicksAggressiveStanceAgainstRangedThreatBeforeActing()
    {
        var state = CreateState(8, 5);
        var actor = AddUnit(state, id: 1, owner: 1, x: 0, y: 2, attackPower: 6, moveRange: 3);
        actor.facing = SrpFacing.East;
        // 한 번 움직이면 쏠 수 있는 거리(사거리 4 + 이동 2)의 사격수.
        AddUnit(state, id: 2, owner: 0, x: 5, y: 2, attackPower: 8, attackRange: 4, moveRange: 2,
            weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1, ammo: 1);

        int remaining = 3;
        var first = SrpEnemyAi.DecideStep(state, actor, remaining, false);
        Assert.That(first.type, Is.EqualTo(SrpEnemyAiActionType.SetStance));
        Assert.That(first.stance, Is.EqualTo(SrpStance.Aggressive));

        var step = SrpBattleFlow.ExecuteAiAction(state, actor, first, ref remaining, null);
        Assert.That(step.executed, Is.True);
        Assert.That(actor.stance, Is.EqualTo(SrpStance.Aggressive));
        Assert.That(actor.actionPoints, Is.EqualTo(2), "태세 변경이 AP를 썼습니다.");

        // 태세를 정한 뒤에는 AP 행동으로 넘어간다.
        var second = SrpEnemyAi.DecideStep(state, actor, remaining, false);
        Assert.That(second.type, Is.EqualTo(SrpEnemyAiActionType.Move));
        Assert.That(SrpBattleFlow.ExecuteAiAction(state, actor, second, ref remaining, null).executed, Is.True);

        // 행동한 뒤에는 태세를 바꿀 수 없다(기준서 §4).
        Assert.That(SrpBattleFlow.CanChangeStance(actor), Is.False);
        var late = SrpBattleFlow.ExecuteAiAction(
            state, actor, SrpEnemyAiAction.SetStance(SrpStance.Defensive), ref remaining, null);
        Assert.That(late.executed, Is.False, "행동한 뒤에 태세 변경이 실행됐습니다.");
        Assert.That(actor.stance, Is.EqualTo(SrpStance.Aggressive));
        Assert.That(SrpEnemyAi.DecideStep(state, actor, remaining, false).type,
            Is.Not.EqualTo(SrpEnemyAiActionType.SetStance));
    }

    [TestCase(TestName = "적 AI는 교전 중이거나 탱커이거나 사격 위협이 없으면 수비 태세를 고른다")]
    public void EnemyAi_PicksDefensiveStanceWhenEngagedTankOrUnthreatened()
    {
        // 교전 중: 인접 공격은 회피할 수 없다.
        var engaged = CreateState(8, 5);
        var brawler = AddUnit(engaged, id: 1, owner: 1, x: 2, y: 2, attackPower: 6);
        brawler.stance = SrpStance.Aggressive;
        AddUnit(engaged, id: 2, owner: 0, x: 3, y: 2, attackPower: 6);
        AddUnit(engaged, id: 3, owner: 0, x: 6, y: 2, attackPower: 8, attackRange: 4, moveRange: 2,
            weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1, ammo: 1);
        var engagedPick = SrpEnemyAi.DecideStep(engaged, brawler, 0, false);
        Assert.That(engagedPick.type, Is.EqualTo(SrpEnemyAiActionType.SetStance));
        Assert.That(engagedPick.stance, Is.EqualTo(SrpStance.Defensive));

        // 탱커: 완벽한 수비와 다중 교전 완충은 수비 태세에서만 작동한다.
        var tankState = CreateState(8, 5);
        var tank = AddUnit(tankState, id: 1, owner: 1, x: 0, y: 2, attackPower: 6);
        tank.stance = SrpStance.Aggressive;
        tank.tags = (int)SrpUnitTags.Tank;
        AddUnit(tankState, id: 2, owner: 0, x: 5, y: 2, attackPower: 8, attackRange: 4, moveRange: 2,
            weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1, ammo: 1);
        var tankPick = SrpEnemyAi.DecideStep(tankState, tank, 0, false);
        Assert.That(tankPick.type, Is.EqualTo(SrpEnemyAiActionType.SetStance));
        Assert.That(tankPick.stance, Is.EqualTo(SrpStance.Defensive));

        // 사격 위협이 없으면 수비. 이미 수비면 태세 행동을 내지 않는다.
        var calm = CreateState(8, 5);
        var idle = AddUnit(calm, id: 1, owner: 1, x: 0, y: 2, attackPower: 6);
        idle.stance = SrpStance.Aggressive;
        idle.facing = SrpFacing.East;
        AddUnit(calm, id: 2, owner: 0, x: 7, y: 2, attackPower: 6, moveRange: 2);
        var calmPick = SrpEnemyAi.DecideStep(calm, idle, 0, false);
        Assert.That(calmPick.type, Is.EqualTo(SrpEnemyAiActionType.SetStance));
        Assert.That(calmPick.stance, Is.EqualTo(SrpStance.Defensive));

        idle.stance = SrpStance.Defensive;
        Assert.That(SrpEnemyAi.DecideStep(calm, idle, 0, false).type,
            Is.Not.EqualTo(SrpEnemyAiActionType.SetStance));
    }

    [TestCase(TestName = "태세는 AP를 쓴 뒤에는 바꿀 수 없다(엄폐처럼 전용 플래그가 없는 행동 포함)")]
    public void CanChangeStance_OnlyBeforeFirstAction()
    {
        var state = CreateState(5, 5);
        var unit = AddUnit(state, id: 1, owner: 0, x: 2, y: 2);
        Assert.That(SrpBattleFlow.CanChangeStance(unit), Is.True);

        unit.actionPoints = 1;
        Assert.That(SrpBattleFlow.CanChangeStance(unit), Is.False, "AP를 쓴 뒤에도 태세를 바꿀 수 있습니다.");

        unit.actionPoints = 2;
        unit.overwatchArmed = true;
        Assert.That(SrpBattleFlow.CanChangeStance(unit), Is.False);

        Assert.That(SrpBattleFlow.CanChangeStance(null), Is.False);
    }

    // ── 적 AI: 방향 ──────────────────────────────────────────────────────────

    [TestCase(TestName = "적 AI는 제자리에서 행동하기 전에 가까운 적을 정면에 두도록 돌아선다")]
    public void EnemyAi_TurnsToFaceThreatBeforeActingInPlace()
    {
        var state = CreateState(6, 5);
        var actor = AddUnit(state, id: 1, owner: 1, x: 2, y: 2, attackPower: 6);
        actor.facing = SrpFacing.North;
        var enemy = AddUnit(state, id: 2, owner: 0, x: 3, y: 2, attackPower: 6);

        int remaining = 0;
        var first = SrpEnemyAi.DecideStep(state, actor, remaining, false);
        Assert.That(first.type, Is.EqualTo(SrpEnemyAiActionType.SetFacing));
        Assert.That(first.facing, Is.EqualTo(SrpFacing.East));

        Assert.That(SrpBattleFlow.ExecuteAiAction(state, actor, first, ref remaining, null).executed, Is.True);
        Assert.That(actor.facing, Is.EqualTo(SrpFacing.East));
        Assert.That(actor.actionPoints, Is.EqualTo(2), "방향 변경이 AP를 썼습니다.");
        Assert.That(SrpCombatResolver.IsAttackerInDefenderFrontArc(enemy, actor), Is.True);

        var second = SrpEnemyAi.DecideStep(state, actor, remaining, false);
        Assert.That(second.type, Is.EqualTo(SrpEnemyAiActionType.Attack));
        Assert.That(second.targetUnitId, Is.EqualTo(enemy.id));

        // 같은 방향으로 다시 바꾸는 행동은 실행되지 않는다.
        Assert.That(SrpBattleFlow.ExecuteAiAction(
            state, actor, SrpEnemyAiAction.SetFacing(SrpFacing.East), ref remaining, null).executed, Is.False);
    }

    [TestCase(TestName = "적 AI는 더 나은 방향이 없으면 돌아서지 않고, 후면을 내주는 방향은 피한다")]
    public void EnemyAi_KeepsFacingOnTieAndAvoidsExposingBack()
    {
        // 북쪽과 동쪽에 같은 거리의 적: 어느 쪽을 봐도 한 명은 측면이라 지금 방향을 유지한다.
        var tie = CreateState(7, 7);
        var actor = AddUnit(tie, id: 1, owner: 1, x: 3, y: 3);
        actor.facing = SrpFacing.North;
        AddUnit(tie, id: 2, owner: 0, x: 3, y: 6);
        AddUnit(tie, id: 3, owner: 0, x: 6, y: 3);
        Assert.That(SrpEnemyAi.DecideStep(tie, actor, 0, false).type, Is.EqualTo(SrpEnemyAiActionType.EndTurn));

        // 등 뒤에 적이 붙으면 그쪽으로 돌아선다(인접한 적이 멀리 있는 적보다 무겁다).
        var back = CreateState(7, 7);
        var guard = AddUnit(back, id: 1, owner: 1, x: 3, y: 3);
        guard.facing = SrpFacing.North;
        AddUnit(back, id: 2, owner: 0, x: 3, y: 6);
        AddUnit(back, id: 3, owner: 0, x: 3, y: 2, attackPower: 6);
        var turn = SrpEnemyAi.DecideStep(back, guard, 0, false);
        Assert.That(turn.type, Is.EqualTo(SrpEnemyAiActionType.SetFacing));
        Assert.That(turn.facing, Is.EqualTo(SrpFacing.South));
    }

    [TestCase(TestName = "적 AI는 마지막 AP로 이동할 때 도착 칸 기준으로 미리 돌아서고, AP가 남으면 도착한 뒤에 돌아선다")]
    public void EnemyAi_FacesFromDestinationWhenMoveSpendsLastAp()
    {
        // (0,0)에서 보면 적(0,3)은 북쪽이다. 접근해서 도착하는 칸에서도 북쪽이므로, 남쪽을 보던 유닛은 미리 돌아선다.
        var state = CreateState(5, 8);
        var actor = AddUnit(state, id: 1, owner: 1, x: 0, y: 0, attackPower: 6, moveRange: 2, actionPoints: 1);
        actor.facing = SrpFacing.East;
        AddUnit(state, id: 2, owner: 0, x: 0, y: 6);

        int remaining = 2;
        var first = SrpEnemyAi.DecideStep(state, actor, remaining, false);
        Assert.That(first.type, Is.EqualTo(SrpEnemyAiActionType.SetFacing), "마지막 AP로 이동하기 전에 방향을 정하지 않았습니다.");
        Assert.That(first.facing, Is.EqualTo(SrpFacing.North));
        SrpBattleFlow.ExecuteAiAction(state, actor, first, ref remaining, null);
        var move = SrpEnemyAi.DecideStep(state, actor, remaining, false);
        Assert.That(move.type, Is.EqualTo(SrpEnemyAiActionType.Move));

        // AP가 2면 먼저 움직이고, 도착한 자리에서 다음 행동 전에 돌아선다.
        var state2 = CreateState(5, 8);
        var actor2 = AddUnit(state2, id: 1, owner: 1, x: 0, y: 0, attackPower: 6, moveRange: 2);
        actor2.facing = SrpFacing.East;
        AddUnit(state2, id: 2, owner: 0, x: 0, y: 6);
        int remaining2 = 2;
        var first2 = SrpEnemyAi.DecideStep(state2, actor2, remaining2, false);
        Assert.That(first2.type, Is.EqualTo(SrpEnemyAiActionType.Move));
        SrpBattleFlow.ExecuteAiAction(state2, actor2, first2, ref remaining2, null);
        var after = SrpEnemyAi.DecideStep(state2, actor2, remaining2, false);
        Assert.That(after.type, Is.EqualTo(SrpEnemyAiActionType.SetFacing));
        Assert.That(after.facing, Is.EqualTo(SrpFacing.North));
    }

    [TestCase(TestName = "적 AI는 사격 전에는 따로 돌아서지 않는다(사격이 쏘는 쪽으로 돌려 준다)")]
    public void EnemyAi_DoesNotPreTurnBeforeFirearmAttack()
    {
        var state = CreateState(7, 5);
        var shooter = AddUnit(state, id: 1, owner: 1, x: 0, y: 2, attackPower: 8, attackRange: 4,
            weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1, ammo: 1, actionPoints: 1);
        shooter.facing = SrpFacing.North;
        var enemy = AddUnit(state, id: 2, owner: 0, x: 3, y: 2);

        int remaining = 0;
        var action = SrpEnemyAi.DecideStep(state, shooter, remaining, false);
        Assert.That(action.type, Is.EqualTo(SrpEnemyAiActionType.Attack));
        Assert.That(action.targetUnitId, Is.EqualTo(enemy.id));

        SrpBattleFlow.ExecuteAiAction(state, shooter, action, ref remaining, null);
        Assert.That(shooter.facing, Is.EqualTo(SrpFacing.East));
    }

    // ── 적 AI: 오버클럭 ──────────────────────────────────────────────────────

    [TestCase(TestName = "적 AI는 쓰려는 스킬을 오버클럭으로 강화한 뒤 쓰고, 안정도가 모자라면 그냥 쓴다")]
    public void EnemyAi_OverclocksSkillForPowerBeforeUsingIt()
    {
        var state = CreateState(7, 5);
        var bolt = AddSkill(state, new SrpSkillData
        {
            id = "p8_bolt",
            displayName = "p8_bolt",
            skillType = SrpSkillType.Active,
            trigger = SrpSkillTrigger.OnActivate,
            targetType = SrpTargetType.SingleEnemy,
            range = 4,
            overclockFrozenHeartCost = 5,
            overclockPowerBonus = 4,
            effects = new[] { new SrpSkillEffect { type = SrpEffectType.Damage, value = 6 } },
        });
        var actor = AddUnit(state, id: 1, owner: 1, x: 0, y: 2, actionPoints: 1, skills: new[] { bolt.id });
        actor.facing = SrpFacing.East;
        actor.frozenHeart = 5;
        var target = AddUnit(state, id: 2, owner: 0, x: 3, y: 2);
        // 정면에서 맞게 해 방향 취약 가산이 섞이지 않게 한다.
        target.facing = SrpFacing.West;

        int remaining = 0;
        var first = SrpEnemyAi.DecideStep(state, actor, remaining, false);
        Assert.That(first.type, Is.EqualTo(SrpEnemyAiActionType.Overclock));
        Assert.That(first.skillId, Is.EqualTo(bolt.id));

        var step = SrpBattleFlow.ExecuteAiAction(state, actor, first, ref remaining, null);
        Assert.That(step.executed, Is.True);
        Assert.That(actor.frozenHeart, Is.EqualTo(0));
        Assert.That(actor.actionPoints, Is.EqualTo(1), "오버클럭이 AP를 썼습니다.");
        Assert.That(actor.skillRuntimes[0].overclockedUsesRemaining, Is.EqualTo(1));

        var second = SrpEnemyAi.DecideStep(state, actor, remaining, false);
        Assert.That(second.type, Is.EqualTo(SrpEnemyAiActionType.UseSkill), "오버클럭한 스킬을 이어서 쓰지 않았습니다.");
        Assert.That(second.skillId, Is.EqualTo(bolt.id));
        SrpBattleFlow.ExecuteAiAction(state, actor, second, ref remaining, null);
        Assert.That(target.hp, Is.EqualTo(30 - 10), "오버클럭 강화(+4)가 피해에 붙지 않았습니다.");
        Assert.That(actor.skillRuntimes[0].overclockedUsesRemaining, Is.EqualTo(0));

        // 안정도가 비용에 못 미치면 오버클럭 없이 쓴다.
        var poor = CreateState(7, 5);
        AddSkill(poor, bolt);
        var weak = AddUnit(poor, id: 1, owner: 1, x: 0, y: 2, actionPoints: 1, skills: new[] { bolt.id });
        weak.facing = SrpFacing.East;
        weak.frozenHeart = 4;
        AddUnit(poor, id: 2, owner: 0, x: 3, y: 2);
        Assert.That(SrpEnemyAi.DecideStep(poor, weak, 0, false).type, Is.EqualTo(SrpEnemyAiActionType.UseSkill));

        // 유닛이 갖지 않은 스킬의 오버클럭은 실행되지 않는다.
        int none = 0;
        Assert.That(SrpBattleFlow.ExecuteAiAction(
            poor, weak, SrpEnemyAiAction.Overclock("unknown_skill"), ref none, null).executed, Is.False);
    }

    [TestCase(TestName = "적 AI는 쿨다운 중인 스킬을 오버클럭으로 되살려 쓰고, 되살아나지 않으면 안정도를 쓰지 않는다")]
    public void EnemyAi_OverclocksToReviveSkillOnCooldownOnlyWhenItBecomesUsable()
    {
        var state = CreateState(7, 5);
        var strike = AddSkill(state, new SrpSkillData
        {
            id = "p8_strike",
            displayName = "p8_strike",
            skillType = SrpSkillType.Active,
            trigger = SrpSkillTrigger.OnActivate,
            targetType = SrpTargetType.SingleEnemy,
            range = 4,
            cooldown = 3,
            overclockFrozenHeartCost = 5,
            overclockCooldownReduction = 1,
            effects = new[] { new SrpSkillEffect { type = SrpEffectType.Damage, value = 6 } },
        });
        var actor = AddUnit(state, id: 1, owner: 1, x: 0, y: 2, actionPoints: 1, skills: new[] { strike.id });
        actor.facing = SrpFacing.East;
        actor.frozenHeart = 5;
        var runtime = actor.skillRuntimes[0];
        AddUnit(state, id: 2, owner: 0, x: 3, y: 2);

        // 쿨다운 2: 한 번 줄여도 못 쓴다. 판단만 하고 상태는 그대로 둔다.
        runtime.cooldownRemaining = 2;
        var wait = SrpEnemyAi.DecideStep(state, actor, 0, false);
        Assert.That(wait.type, Is.Not.EqualTo(SrpEnemyAiActionType.Overclock));
        Assert.That(runtime.cooldownRemaining, Is.EqualTo(2), "판단 중에 쿨다운이 바뀌었습니다.");
        Assert.That(actor.frozenHeart, Is.EqualTo(5), "판단 중에 안정도가 바뀌었습니다.");

        // 쿨다운 1: 줄이면 바로 쓸 수 있다.
        runtime.cooldownRemaining = 1;
        var first = SrpEnemyAi.DecideStep(state, actor, 0, false);
        Assert.That(first.type, Is.EqualTo(SrpEnemyAiActionType.Overclock));
        Assert.That(runtime.cooldownRemaining, Is.EqualTo(1), "판단 중에 쿨다운이 바뀌었습니다.");

        int remaining = 0;
        Assert.That(SrpBattleFlow.ExecuteAiAction(state, actor, first, ref remaining, null).executed, Is.True);
        Assert.That(runtime.cooldownRemaining, Is.EqualTo(0));
        var second = SrpEnemyAi.DecideStep(state, actor, remaining, false);
        Assert.That(second.type, Is.EqualTo(SrpEnemyAiActionType.UseSkill));
        Assert.That(second.skillId, Is.EqualTo(strike.id));
    }

    // ── 적 AI: 회복하러 다가가는 이동 ────────────────────────────────────────

    [TestCase(TestName = "적 AI는 크게 다친 아군에게 회복이 닿는 칸으로 옮겨 가 회복시킨다")]
    public void EnemyAi_MovesIntoRangeToHealBadlyWoundedAlly()
    {
        var state = CreateHealState(out var healer, out var ally);

        // 크게 다침(HP 절반 이하): 적을 치러 가는 칸보다 회복이 닿는 칸이 먼저다.
        ally.hp = 8;
        var move = SrpEnemyAi.Decide(state, healer, 3, false);
        Assert.That(move.type, Is.EqualTo(SrpEnemyAiActionType.Move));
        Assert.That((move.x, move.y), Is.EqualTo((3, 0)), "회복 사거리(2) 안으로 들어가는 칸이 아닙니다.");

        int remaining = 3;
        Assert.That(SrpBattleFlow.ExecuteAiAction(state, healer, move, ref remaining, null).executed, Is.True);
        var heal = SrpEnemyAi.Decide(state, healer, remaining, false);
        Assert.That(heal.type, Is.EqualTo(SrpEnemyAiActionType.UseSkill));
        Assert.That(heal.targetUnitId, Is.EqualTo(ally.id));
        SrpBattleFlow.ExecuteAiAction(state, healer, heal, ref remaining, null);
        Assert.That(ally.hp, Is.EqualTo(18));
    }

    [TestCase(TestName = "적 AI는 급하지 않은 회복보다 공격할 수 있는 칸을 고르고, AP가 1이면 회복하러 옮기지 않는다")]
    public void EnemyAi_HealApproachYieldsToAttackAndNeedsSpareAp()
    {
        // 조금 다침(회복량은 다 들어가지만 급하지 않음): 공격할 수 있는 칸이 먼저다.
        var state = CreateHealState(out var healer, out var ally);
        ally.hp = 20;
        var strike = SrpEnemyAi.Decide(state, healer, 3, false);
        Assert.That(strike.type, Is.EqualTo(SrpEnemyAiActionType.Move));
        Assert.That((strike.x, strike.y), Is.EqualTo((0, 2)), "적 옆 칸으로 가지 않았습니다.");

        // 다치지 않았으면 회복 칸은 아무 가산도 받지 않는다.
        ally.hp = 30;
        var plain = SrpEnemyAi.Decide(state, healer, 3, false);
        Assert.That((plain.x, plain.y), Is.EqualTo((0, 2)));

        // AP가 1뿐이면 옮겨 가도 이번 차례에 회복할 수 없으므로 회복 칸을 고르지 않는다.
        ally.hp = 8;
        healer.actionPoints = 1;
        var oneAp = SrpEnemyAi.Decide(state, healer, 3, false);
        Assert.That((oneAp.x, oneAp.y), Is.Not.EqualTo((3, 0)));
    }

    // ── 적 AI: 지휘관의 위험 판단 ────────────────────────────────────────────

    [TestCase(TestName = "적 AI 지휘관은 다음 적 차례에 쓰러질 수 있는 칸으로는 공격하러 나서지 않는다")]
    public void EnemyAi_CommanderDoesNotAdvanceIntoLethalTile()
    {
        var state = CreateCommanderState(out var commander, out var ally, out var shooterA, out var shooterB);

        // 전진하면 사격수 둘에게 한 차례에 HP 40(20 + 20)을 맞을 수 있다. 지휘관 HP는 30이다.
        var hold = SrpEnemyAi.Decide(state, commander, 3, false);
        Assert.That(hold.type, Is.Not.EqualTo(SrpEnemyAiActionType.Move), "지휘관이 위험한 칸으로 전진했습니다.");

        // 위협이 약하면(한 명, HP 12) 공격하러 나선다.
        state.RemoveUnit(shooterB);
        shooterA.attackPower = 4;
        var advance = SrpEnemyAi.Decide(state, commander, 3, false);
        Assert.That(advance.type, Is.EqualTo(SrpEnemyAiActionType.Move));
        int remaining = 3;
        SrpBattleFlow.ExecuteAiAction(state, commander, advance, ref remaining, null);
        Assert.That(SrpEnemyAi.Decide(state, commander, remaining, false).type, Is.EqualTo(SrpEnemyAiActionType.Attack));
        Assert.That(ally.eliminated, Is.False);
    }

    [TestCase(TestName = "적 AI 지휘관의 위험 판단은 혼자 남았거나 지휘관 처치 규칙이 꺼진 맵에서는 적용하지 않는다")]
    public void EnemyAi_CommanderDangerCheckOnlyAppliesWhileHolding()
    {
        // 혼자 남으면 고착을 피하려고 위험해도 움직인다.
        var alone = CreateCommanderState(out var commander, out var ally, out _, out _);
        alone.RemoveUnit(ally);
        Assert.That(SrpEnemyAi.Decide(alone, commander, 3, false).type, Is.EqualTo(SrpEnemyAiActionType.Move));

        // 지휘관 처치 규칙이 꺼진 맵에서는 지휘관 표시가 있어도 평소처럼 공격하러 간다.
        var ruleOff = CreateCommanderState(out var leader, out _, out _, out _, commanderRule: false);
        Assert.That(SrpEnemyAi.Decide(ruleOff, leader, 3, false).type, Is.EqualTo(SrpEnemyAiActionType.Move));
    }

    // ── 시뮬레이션 ───────────────────────────────────────────────────────────

    [Category("SrpAiSim")]
    [TestCase(TestName = "시뮬레이션의 Heuristic 정책이 태세·방향 변경과 오버클럭을 쓴다")]
    public void Simulation_UsesStanceFacingAndOverclock()
    {
        var config = new SrpBattleSimConfig
        {
            mapPreset = SrpMapPreset.M1QaIntegrated,
            trials = 40,
            maxRounds = 24,
            baseSeed = 880001,
            sampleSeedCount = 1,
            owner0Policy = new SrpHeuristicAiPolicy(),
            owner1Policy = new SrpHeuristicAiPolicy(),
            thresholds = SrpSimThresholdConfig.Default(),
        };

        var report = SrpBattleSimRunner.RunBatch(config, writeReport: false).report;

        Assert.That(report.actions.stanceChangesPerBattle, Is.GreaterThan(0f), "시뮬레이션이 태세를 바꾸지 않았습니다.");
        Assert.That(report.actions.facingChangesPerBattle, Is.GreaterThan(0f), "시뮬레이션이 방향을 바꾸지 않았습니다.");
        Assert.That(report.actions.overclocksPerBattle, Is.GreaterThan(0f), "시뮬레이션이 오버클럭을 쓰지 않았습니다.");
    }

    [Category("SrpAiSim")]
    [TestCase(TestName = "승리 조건 실험실: AI끼리 붙어도 지휘관이 첫 라운드에 쓰러지며 끝나지 않는다")]
    public void ObjectiveLab_SimulationDoesNotEndWithCommanderRush()
    {
        var config = new SrpBattleSimConfig
        {
            mapPreset = SrpMapPreset.M1ObjectiveLab,
            trials = 200,
            maxRounds = 24,
            baseSeed = 770001,
            sampleSeedCount = 1,
            owner0Policy = new SrpHeuristicAiPolicy(),
            owner1Policy = new SrpHeuristicAiPolicy(),
            thresholds = SrpSimThresholdConfig.Default(),
        };

        var outcome = SrpBattleSimRunner.RunBatch(config, writeReport: false).report.outcome;
        Debug.Log(
            $"[SRPG][AI-Sim][ObjectiveLab] w0={outcome.owner0WinRate:F3}, w1={outcome.owner1WinRate:F3}, " +
            $"draw={outcome.drawRate:F3}, avgR={outcome.averageRounds:F2}, cmd={outcome.commanderKillRate:F3}");

        // 측정값(2026-11-15, 200회): 7차 평균 1.47라운드, 전부 지휘관 처치(먼저 움직인 진영이 항상 패배).
        // 8차(지휘관이 위험한 칸으로 나서지 않음) 평균 4.00라운드, 승률 0.310 / 0.690, 지휘관 처치 69.0%.
        Assert.That(outcome.averageRounds, Is.GreaterThanOrEqualTo(2.5f), "전투가 너무 빨리 끝납니다. 지휘관이 다시 돌진하는지 확인하세요.");
        Assert.That(outcome.owner0WinRate, Is.GreaterThan(0.05f), "먼저 움직이는 진영이 이기는 판이 없습니다.");
        Assert.That(outcome.drawRate, Is.LessThanOrEqualTo(0.10f), "무승부가 많습니다. 지휘관끼리 버티기만 하는지 확인하세요.");
    }

    // ── 전장 구성 ────────────────────────────────────────────────────────────

    /// <summary>
    /// 회복 이동 판단용 전장. 회복수(0,0, 이동 3)는 사거리 2의 회복 스킬을 갖고, 아군(5,0)은 사거리 밖이다.
    /// 적(0,3)은 반대쪽에 있어, 치러 가는 칸(0,2)과 회복이 닿는 칸(3,0)이 갈린다.
    /// 적 옆 칸은 들어갈 때 이동 비용이 1 더 든다((0,2)까지 비용 3).
    /// </summary>
    static SrpBattleState CreateHealState(out SrpUnitRuntime healer, out SrpUnitRuntime ally)
    {
        var state = CreateState(9, 5);
        var heal = AddSkill(state, new SrpSkillData
        {
            id = "p8_heal",
            displayName = "p8_heal",
            skillType = SrpSkillType.Active,
            trigger = SrpSkillTrigger.OnActivate,
            targetType = SrpTargetType.SingleAlly,
            range = 2,
            effects = new[] { new SrpSkillEffect { type = SrpEffectType.Heal, stat = "hp", value = 10 } },
        });
        healer = AddUnit(state, id: 1, owner: 1, x: 0, y: 0, attackPower: 6, moveRange: 3, skills: new[] { heal.id });
        ally = AddUnit(state, id: 2, owner: 1, x: 5, y: 0);
        AddUnit(state, id: 3, owner: 0, x: 0, y: 3);
        return state;
    }

    /// <summary>
    /// 지휘관 위험 판단용 전장. 지휘관(0,2, HP 30)은 부무장 사격(사거리 3)이 있어 (3,2)로 나가면 사격수 A(6,2)를 쏠 수 있다.
    /// 사격수 A(6,2)와 B(6,3)는 공격력 8(사격 HP 20), 사거리 4, 이동 2다.
    /// </summary>
    static SrpBattleState CreateCommanderState(
        out SrpUnitRuntime commander,
        out SrpUnitRuntime ally,
        out SrpUnitRuntime shooterA,
        out SrpUnitRuntime shooterB,
        bool commanderRule = true)
    {
        var walk = new bool[9 * 5];
        for (int i = 0; i < walk.Length; i++)
            walk[i] = true;
        var state = SrpBattleState.FromMap(new SrpMapFileV1
        {
            width = 9,
            height = 5,
            walkable = walk,
            playerOrder = new[] { 0, 1 },
            victory = new SrpVictoryRuleData { commanderKill = commanderRule },
        });

        commander = AddUnit(state, id: 1, owner: 1, x: 0, y: 2, attackPower: 6, attackRange: 3, moveRange: 3,
            maxAmmo: 1, ammo: 1);
        commander.isCommander = true;
        ally = AddUnit(state, id: 2, owner: 1, x: 0, y: 0);
        shooterA = AddUnit(state, id: 3, owner: 0, x: 6, y: 2, attackPower: 8, attackRange: 4, moveRange: 2,
            weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1, ammo: 1);
        shooterB = AddUnit(state, id: 4, owner: 0, x: 6, y: 3, attackPower: 8, attackRange: 4, moveRange: 2,
            weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1, ammo: 1);
        return state;
    }

    // ── 공용 ─────────────────────────────────────────────────────────────────

    static SrpSkillData AddSkill(SrpBattleState state, SrpSkillData skill)
    {
        state.SkillLookup[skill.id] = skill;
        return skill;
    }

    /// <summary>AI 테스트용 유닛. 총이 없는 근접 유닛이 기본이고, 반응(RP)은 0이라 피해 계산이 단순하다.</summary>
    static SrpUnitRuntime AddUnit(
        SrpBattleState state,
        int id,
        int owner,
        int x,
        int y,
        int attackPower = 0,
        int attackRange = 1,
        int moveRange = 0,
        SrpWeaponClass weaponClass = SrpWeaponClass.Melee,
        int maxAmmo = 0,
        int ammo = 0,
        int actionPoints = 2,
        string[] skills = null)
    {
        var unit = new SrpUnitRuntime
        {
            id = id,
            owner = owner,
            displayName = $"U{id}",
            anchorX = x,
            anchorY = y,
            hp = 30,
            maxHp = 30,
            pg = 20,
            maxPg = 20,
            attackPower = attackPower,
            attackRange = attackRange,
            moveRange = moveRange,
            weaponClass = weaponClass,
            maxAmmo = maxAmmo,
            ammo = ammo,
            actionPoints = actionPoints,
            maxActionPoints = 2,
            reactionPoints = 0,
            maxReactionPoints = 1,
            stance = SrpStance.Defensive,
        };
        if (skills != null)
        {
            foreach (string skillId in skills)
            {
                unit.skillIds.Add(skillId);
                unit.skillRuntimes.Add(new SrpSkillRuntime(skillId));
            }
        }
        state.Units.Add(unit);
        state.RebuildEngagements();
        return unit;
    }

    static SrpBattleState CreateState(int width = 5, int height = 5)
    {
        var walk = new bool[width * height];
        for (int i = 0; i < walk.Length; i++)
            walk[i] = true;

        return SrpBattleState.FromMap(new SrpMapFileV1
        {
            width = width,
            height = height,
            walkable = walk,
            playerOrder = new[] { 0, 1 },
        });
    }
}

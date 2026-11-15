using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 8차 테스트: 적 AI 마무리(태세와 방향 선택, 오버클럭, 회복하러 다가가는 이동)와
/// 지휘관 처치 승리가 켜진 맵에서 지휘관이 위험한 칸으로 나서지 않는 판단.
/// 뒤쪽은 8차에서 받은 기획 결정(Q16 추가 승리 조건 4종과 패배 조건 목록, Q18 권총/소총 구분과 회피 가중치)을 고정한다.
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
    [TestCase(TestName = "승리 조건 실험실: AI끼리 붙어도 지휘관의 돌진으로 한두 라운드에 끝나지 않는다")]
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
        // 8차 결정 반영 뒤(권총 피해, 회피 가중치) 평균 5.08라운드, 승률 0.000 / 1.000, 지휘관 처치 74.0%.
        // 이 맵은 2명 대 3명이라 AI끼리는 수가 많은 쪽이 이긴다. 승률은 고정하지 않고 "돌진으로 끝나지 않는다"만 본다.
        Assert.That(outcome.averageRounds, Is.GreaterThanOrEqualTo(2.5f), "전투가 너무 빨리 끝납니다. 지휘관이 다시 돌진하는지 확인하세요.");
        Assert.That(outcome.drawRate, Is.LessThanOrEqualTo(0.10f), "무승부가 많습니다. 지휘관끼리 버티기만 하는지 확인하세요.");
    }

    // ── 기획 결정 Q16: 추가 승리 조건과 패배 조건 목록 ───────────────────────

    [TestCase(TestName = "N라운드 버티기는 그 라운드가 끝나는 순간 성립하고, 패배 목록에 넣으면 제한 라운드가 된다")]
    public void Victory_SurviveRoundsDecidesWhenRoundEnds()
    {
        var state = CreateRuleState(victory: new[]
        {
            new SrpVictoryConditionData { type = SrpVictoryConditionTypes.SurviveRounds, owner = 1, value = 2 },
        });
        AddUnit(state, id: 1, owner: 0, x: 0, y: 0);
        AddUnit(state, id: 2, owner: 1, x: 4, y: 4);
        Assert.That(SrpVictory.DescribeRule(state), Is.EqualTo("적 전멸 / P1 2라운드 버티기"));
        Assert.That(SrpVictory.DescribeDefeatRule(state), Is.Null);

        state.RoundNumber = 1;
        SrpBattleFlow.EndRound(state);
        Assert.That(SrpVictory.Evaluate(state).decided, Is.False);

        // 2라운드가 진행 중일 때는 아직 아니다.
        state.RoundNumber = 2;
        Assert.That(SrpVictory.Evaluate(state).decided, Is.False, "버텨야 할 라운드가 끝나기 전에 결판이 났습니다.");

        SrpBattleFlow.EndRound(state);
        var result = SrpVictory.Evaluate(state);
        Assert.That(result.decided, Is.True);
        Assert.That(result.winnerOwner, Is.EqualTo(1));
        Assert.That(result.reason, Is.EqualTo(SrpVictoryReason.Custom));
        Assert.That(result.conditionType, Is.EqualTo(SrpVictoryConditionTypes.SurviveRounds));
        Assert.That(result.byDefeatCondition, Is.False);
        Assert.That(SrpVictory.DescribeResult(state, result), Is.EqualTo("P1 2라운드 버티기"));

        // 패배 목록: P0은 2라운드 안에 끝내지 못하면 진다.
        var limit = CreateRuleState(defeat: new[]
        {
            new SrpVictoryConditionData { type = SrpVictoryConditionTypes.SurviveRounds, owner = 0, value = 2 },
        });
        AddUnit(limit, id: 1, owner: 0, x: 0, y: 0);
        AddUnit(limit, id: 2, owner: 1, x: 4, y: 4);
        Assert.That(SrpVictory.DescribeRule(limit), Is.EqualTo("적 전멸"));
        Assert.That(SrpVictory.DescribeDefeatRule(limit), Is.EqualTo("P0 2라운드 안에 끝내지 못함"));
        limit.RoundNumber = 2;
        SrpBattleFlow.EndRound(limit);
        var byLimit = SrpVictory.Evaluate(limit);
        Assert.That(byLimit.winnerOwner, Is.EqualTo(1));
        Assert.That(byLimit.byDefeatCondition, Is.True);
    }

    [TestCase(TestName = "칸 도달은 조건이 정한 진영의 대상 유닛이 그 칸에 섰을 때만 성립한다")]
    public void Victory_ReachTileNeedsMatchingUnitOnTile()
    {
        var state = CreateRuleState(victory: new[]
        {
            new SrpVictoryConditionData { type = SrpVictoryConditionTypes.ReachTile, owner = 0, targetId = "p8_courier", x = 4, y = 2 },
        });
        var courier = AddUnit(state, id: 1, owner: 0, x: 0, y: 2);
        courier.templateId = "p8_courier";
        var escort = AddUnit(state, id: 2, owner: 0, x: 0, y: 0);
        var enemy = AddUnit(state, id: 3, owner: 1, x: 4, y: 4);
        Assert.That(SrpVictory.DescribeRule(state), Is.EqualTo("적 전멸 / P0 p8_courier (4,2) 도달"));

        // 대상이 아닌 아군이나 적이 서 있어도 성립하지 않는다.
        escort.anchorX = 4;
        escort.anchorY = 2;
        Assert.That(SrpVictory.Evaluate(state).decided, Is.False, "대상이 아닌 유닛으로 조건이 성립했습니다.");
        escort.anchorX = 0;
        escort.anchorY = 0;
        enemy.anchorX = 4;
        enemy.anchorY = 2;
        Assert.That(SrpVictory.Evaluate(state).decided, Is.False, "상대 유닛으로 조건이 성립했습니다.");
        enemy.anchorX = 4;
        enemy.anchorY = 4;

        courier.anchorX = 4;
        courier.anchorY = 2;
        var result = SrpVictory.Evaluate(state);
        Assert.That(result.winnerOwner, Is.EqualTo(0));
        Assert.That(result.conditionType, Is.EqualTo(SrpVictoryConditionTypes.ReachTile));

        // 대상이 비어 있으면 그 진영의 아무 유닛이나, "commander"면 지휘관만 본다.
        var any = CreateRuleState(victory: new[]
        {
            new SrpVictoryConditionData { type = SrpVictoryConditionTypes.ReachTile, owner = 0, targetId = SrpVictoryConditionTypes.CommanderTarget, x = 4, y = 2 },
        });
        var runner = AddUnit(any, id: 1, owner: 0, x: 4, y: 2);
        AddUnit(any, id: 2, owner: 1, x: 4, y: 4);
        Assert.That(SrpVictory.Evaluate(any).decided, Is.False);
        runner.isCommander = true;
        Assert.That(SrpVictory.Evaluate(any).winnerOwner, Is.EqualTo(0));
        any.VictoryRule.conditions[0].targetId = null;
        runner.isCommander = false;
        Assert.That(SrpVictory.Evaluate(any).winnerOwner, Is.EqualTo(0));
    }

    [TestCase(TestName = "유닛 보호는 패배 목록에 넣으며, 대상 유닛을 모두 잃은 진영이 진다")]
    public void Victory_UnitLostInDefeatListDefeatsOwner()
    {
        var state = CreateRuleState(defeat: new[]
        {
            new SrpVictoryConditionData { type = SrpVictoryConditionTypes.UnitLost, owner = 0, targetId = "p8_courier" },
        });
        var first = AddUnit(state, id: 1, owner: 0, x: 0, y: 0);
        first.templateId = "p8_courier";
        var second = AddUnit(state, id: 2, owner: 0, x: 0, y: 1);
        second.templateId = "p8_courier";
        AddUnit(state, id: 3, owner: 0, x: 0, y: 2);
        var enemy = AddUnit(state, id: 4, owner: 1, x: 4, y: 4);
        // 상대에게 같은 템플릿의 유닛이 있어도 조건은 P0에만 걸려 있다.
        enemy.templateId = "p8_courier";
        Assert.That(SrpVictory.DescribeDefeatRule(state), Is.EqualTo("P0 p8_courier 보호 실패"));

        state.RemoveUnit(first);
        Assert.That(SrpVictory.Evaluate(state).decided, Is.False, "대상이 남아 있는데 패배했습니다.");

        state.RemoveUnit(second);
        var result = SrpVictory.Evaluate(state);
        Assert.That(result.winnerOwner, Is.EqualTo(1));
        Assert.That(result.reason, Is.EqualTo(SrpVictoryReason.Custom));
        Assert.That(result.conditionType, Is.EqualTo(SrpVictoryConditionTypes.UnitLost));
        Assert.That(result.byDefeatCondition, Is.True);

        // 대상 유닛이 처음부터 없던 진영에는 성립하지 않는다.
        var none = CreateRuleState(defeat: new[]
        {
            new SrpVictoryConditionData { type = SrpVictoryConditionTypes.UnitLost, owner = 0, targetId = "p8_courier" },
        });
        AddUnit(none, id: 1, owner: 0, x: 0, y: 0);
        AddUnit(none, id: 2, owner: 1, x: 4, y: 4);
        Assert.That(SrpVictory.Evaluate(none).decided, Is.False);
    }

    [TestCase(TestName = "거점 유지는 라운드가 끝날 때 혼자 점유하고 있어야 진행도가 오르고, 적이 들어오면 처음부터 다시 센다")]
    public void Victory_HoldPointCountsConsecutiveRoundEnds()
    {
        var state = CreateRuleState(victory: new[]
        {
            new SrpVictoryConditionData { type = SrpVictoryConditionTypes.HoldPoint, owner = 0, x = 2, y = 2, radius = 1, value = 2 },
        });
        var holder = AddUnit(state, id: 1, owner: 0, x: 1, y: 1);
        var enemy = AddUnit(state, id: 2, owner: 1, x: 4, y: 4);
        Assert.That(SrpVictory.DescribeRule(state), Is.EqualTo("적 전멸 / P0 거점 (2,2) 2라운드 유지"));
        Assert.That(SrpVictory.DescribeProgress(state), Is.Null);

        state.RoundNumber = 1;
        SrpBattleFlow.EndRound(state);
        Assert.That(SrpVictory.DescribeProgress(state), Is.EqualTo("P0 거점 (2,2) 유지 1/2"));
        // 같은 라운드에 다시 불러도 진행도가 두 번 오르지 않는다.
        SrpBattleFlow.EndRound(state);
        Assert.That(SrpVictory.Evaluate(state).decided, Is.False);

        // 적이 거점에 들어온 채로 라운드가 끝나면 진행도가 0으로 돌아간다.
        state.RoundNumber = 2;
        enemy.anchorX = 3;
        enemy.anchorY = 3;
        SrpBattleFlow.EndRound(state);
        Assert.That(SrpVictory.DescribeProgress(state), Is.Null);
        Assert.That(SrpVictory.Evaluate(state).decided, Is.False);

        enemy.anchorX = 4;
        enemy.anchorY = 4;
        state.RoundNumber = 3;
        SrpBattleFlow.EndRound(state);
        // 진행도는 복제한 상태(되감기, 시뮬레이션)에도 따라간다.
        var clone = state.Clone();
        clone.RoundNumber = 4;
        SrpBattleFlow.EndRound(clone);
        Assert.That(SrpVictory.Evaluate(clone).winnerOwner, Is.EqualTo(0));
        Assert.That(SrpVictory.Evaluate(state).decided, Is.False, "복제본의 진행도가 원본에 섞였습니다.");

        // 거점을 비운 채로 라운드가 끝나도 처음부터 다시 센다.
        holder.anchorX = 0;
        holder.anchorY = 4;
        state.RoundNumber = 4;
        SrpBattleFlow.EndRound(state);
        Assert.That(SrpVictory.Evaluate(state).decided, Is.False);
        Assert.That(SrpVictory.DescribeProgress(state), Is.Null);
    }

    [TestCase(TestName = "패배 조건은 승리 조건보다 먼저 판정하고, 조건 목록은 저장 형식을 거쳐도 그대로다")]
    public void Victory_DefeatListRanksBeforeVictoryListAndSurvivesJson()
    {
        var rule = new SrpVictoryRuleData
        {
            conditions = new[]
            {
                new SrpVictoryConditionData { type = SrpVictoryConditionTypes.ReachTile, owner = 0, x = 4, y = 2 },
            },
            defeatConditions = new[]
            {
                new SrpVictoryConditionData { type = SrpVictoryConditionTypes.UnitLost, owner = 0, targetId = "p8_courier" },
            },
        };
        var map = new SrpMapFileV1
        {
            width = 5,
            height = 5,
            walkable = AllWalkable(5, 5),
            playerOrder = new[] { 0, 1 },
            victory = rule,
        };
        var restored = JsonUtility.FromJson<SrpMapFileV1>(JsonUtility.ToJson(map));
        Assert.That(restored.victory.conditions.Length, Is.EqualTo(1));
        Assert.That(restored.victory.defeatConditions.Length, Is.EqualTo(1));
        Assert.That((restored.victory.conditions[0].x, restored.victory.conditions[0].y), Is.EqualTo((4, 2)));
        Assert.That(restored.victory.defeatConditions[0].targetId, Is.EqualTo("p8_courier"));

        var state = SrpBattleState.FromMap(restored);
        var courier = AddUnit(state, id: 1, owner: 0, x: 0, y: 0);
        courier.templateId = "p8_courier";
        AddUnit(state, id: 2, owner: 0, x: 4, y: 2);
        AddUnit(state, id: 3, owner: 1, x: 4, y: 4);
        Assert.That(SrpVictory.Evaluate(state).winnerOwner, Is.EqualTo(0), "도달 조건이 성립하지 않았습니다.");

        // 도달과 보호 실패가 함께 성립하면 패배가 먼저다.
        state.RemoveUnit(courier);
        var result = SrpVictory.Evaluate(state);
        Assert.That(result.winnerOwner, Is.EqualTo(1));
        Assert.That(result.conditionType, Is.EqualTo(SrpVictoryConditionTypes.UnitLost));
    }

    [TestCase(TestName = "추가 조건 검증 랩은 네 조건을 모두 걸고 있고 설명이 진영별로 나온다")]
    public void ConditionLab_DeclaresAllFourConditions()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.CreateM1ConditionLab());

        Assert.That(SrpVictory.DescribeRule(state),
            Is.EqualTo("적 전멸 / P0 전령 (10,3) 도달 / P0 거점 (5,3) 3라운드 유지 / P1 9라운드 버티기"));
        Assert.That(SrpVictory.DescribeDefeatRule(state), Is.EqualTo("P0 전령 보호 실패"));
        Assert.That(SrpVictory.Evaluate(state).decided, Is.False);
        foreach (string type in SrpVictoryConditionTypes.BuiltIn)
            Assert.That(SrpVictory.IsRegistered(type), Is.True, $"내장 조건이 등록되지 않았습니다: {type}");

        // 도달 칸은 걸을 수 있는 빈 칸이어야 한다.
        Assert.That(state.IsWalkableTile(10, 3), Is.True);
        Assert.That(state.GetOccupant(10, 3), Is.Null);
    }

    // ── 맵 메이커: 추가 조건 편집 규칙 ───────────────────────────────────────

    [TestCase(TestName = "메이커의 조건 만들기는 조건이 쓰는 값만 넣고, 만들 수 없는 조합은 이유와 함께 거부한다")]
    public void MakerCondition_BuildKeepsOnlyUsedFieldsAndRejectsInvalid()
    {
        var survive = SrpMapConditionEdit.Build(SrpVictoryConditionTypes.SurviveRounds, 1, 0, 7, 7, "x", out string error);
        Assert.That(error, Is.Null);
        Assert.That((survive.owner, survive.value), Is.EqualTo((1, 1)), "라운드 수는 1 이상이어야 합니다.");
        Assert.That((survive.x, survive.y, survive.targetId), Is.EqualTo((0, 0, (string)null)), "쓰지 않는 값이 들어갔습니다.");

        var reach = SrpMapConditionEdit.Build(SrpVictoryConditionTypes.ReachTile, 0, 5, 3, 4,
            SrpMapConditionEdit.ResolveTargetId(SrpMapConditionEdit.TargetKind.Commander, null), out error);
        Assert.That(error, Is.Null);
        Assert.That((reach.x, reach.y, reach.value), Is.EqualTo((3, 4, 0)));
        Assert.That(reach.targetId, Is.EqualTo(SrpVictoryConditionTypes.CommanderTarget));

        var hold = SrpMapConditionEdit.Build(SrpVictoryConditionTypes.HoldPoint, 0, 3, 5, 3, "ignored", out error);
        Assert.That((hold.x, hold.y, hold.value, hold.radius), Is.EqualTo((5, 3, 3, SrpMapConditionEdit.DefaultHoldRadius)));
        Assert.That(hold.targetId, Is.Null);

        // 유닛 상실은 대상이 "모든 유닛"이면 전멸과 같아 만들지 않는다.
        string anyTarget = SrpMapConditionEdit.ResolveTargetId(SrpMapConditionEdit.TargetKind.Any, "mage");
        Assert.That(SrpMapConditionEdit.Build(SrpVictoryConditionTypes.UnitLost, 0, 0, 0, 0, anyTarget, out error), Is.Null);
        Assert.That(error, Is.Not.Empty);
        // "선택한 유닛"인데 고른 유닛이 없으면 만들지 않는다.
        string noUnit = SrpMapConditionEdit.ResolveTargetId(SrpMapConditionEdit.TargetKind.SelectedUnit, null);
        Assert.That(SrpMapConditionEdit.Build(SrpVictoryConditionTypes.UnitLost, 0, 0, 0, 0, noUnit, out error), Is.Null);
        Assert.That(SrpMapConditionEdit.Build("p8_unknown", 0, 0, 0, 0, null, out error), Is.Null);

        // 조건 종류 버튼은 내장 조건 네 가지를 돈다.
        string type = SrpVictoryConditionTypes.SurviveRounds;
        var seen = new List<string>();
        for (int i = 0; i < SrpVictoryConditionTypes.BuiltIn.Length; i++)
        {
            seen.Add(type);
            Assert.That(SrpMapConditionEdit.GetTypeLabel(type), Is.Not.EqualTo(type), "조건 이름표가 없습니다.");
            type = SrpMapConditionEdit.NextType(type);
        }
        Assert.That(seen, Is.EquivalentTo(SrpVictoryConditionTypes.BuiltIn));
        Assert.That(type, Is.EqualTo(SrpVictoryConditionTypes.SurviveRounds));
    }

    [TestCase(TestName = "메이커에서 승리·패배 목록에 넣은 조건은 저장 형식을 거쳐 전투에서 그대로 판정된다")]
    public void MakerCondition_AddedConditionsWorkInBattleAfterSave()
    {
        var carry = new SrpMapCarryOver();
        Assert.That(SrpMapConditionEdit.Describe(carry), Is.EqualTo("(추가 조건 없음)"));
        Assert.That(SrpMapConditionEdit.RemoveLast(carry, false), Is.False);

        string courierId = SrpMapConditionEdit.ResolveTargetId(SrpMapConditionEdit.TargetKind.SelectedUnit, "p8_courier");
        SrpMapConditionEdit.Add(carry,
            SrpMapConditionEdit.Build(SrpVictoryConditionTypes.ReachTile, 0, 0, 4, 2, courierId, out _), false);
        SrpMapConditionEdit.Add(carry,
            SrpMapConditionEdit.Build(SrpVictoryConditionTypes.HoldPoint, 0, 2, 2, 2, null, out _), false);
        SrpMapConditionEdit.Add(carry,
            SrpMapConditionEdit.Build(SrpVictoryConditionTypes.SurviveRounds, 1, 9, 0, 0, null, out _), false);
        SrpMapConditionEdit.Add(carry,
            SrpMapConditionEdit.Build(SrpVictoryConditionTypes.UnitLost, 0, 0, 0, 0, courierId, out _), true);
        Assert.That(SrpMapConditionEdit.Describe(carry), Is.EqualTo(
            "승리 1. P0 p8_courier (4,2) 도달\n" +
            "승리 2. P0 거점 (2,2) 2라운드 유지\n" +
            "승리 3. P1 9라운드 버티기\n" +
            "패배 1. P0 p8_courier 보호 실패"));

        // 삭제는 고른 목록의 마지막 조건만 뺀다.
        Assert.That(SrpMapConditionEdit.RemoveLast(carry, false), Is.True);
        Assert.That(carry.victory.conditions.Length, Is.EqualTo(2));
        Assert.That(carry.victory.defeatConditions.Length, Is.EqualTo(1));

        // 조건이 가리키는 칸: 도달 칸 1개 + 거점 3x3.
        var marks = SrpVictory.GetConditionTiles(carry.victory);
        Assert.That(marks.Count, Is.EqualTo(1 + 9));
        int centers = 0;
        foreach (var mark in marks)
        {
            Assert.That(mark.asDefeat, Is.False);
            if (mark.isCenter)
                centers++;
        }
        Assert.That(centers, Is.EqualTo(2));

        // 메이커 저장처럼 맵에 실어 저장 형식을 거친 뒤 전투에 올린다.
        var map = new SrpMapFileV1
        {
            width = 5,
            height = 5,
            walkable = AllWalkable(5, 5),
            playerOrder = new[] { 0, 1 },
        };
        carry.ApplyTo(map);
        var state = SrpBattleState.FromMap(JsonUtility.FromJson<SrpMapFileV1>(JsonUtility.ToJson(map)));
        var courier = AddUnit(state, id: 1, owner: 0, x: 0, y: 0);
        courier.templateId = "p8_courier";
        AddUnit(state, id: 2, owner: 0, x: 0, y: 4);
        AddUnit(state, id: 3, owner: 1, x: 4, y: 4);
        Assert.That(SrpVictory.Evaluate(state).decided, Is.False);

        courier.anchorX = 4;
        courier.anchorY = 2;
        Assert.That(SrpVictory.Evaluate(state).conditionType, Is.EqualTo(SrpVictoryConditionTypes.ReachTile));
        state.RemoveUnit(courier);
        var lost = SrpVictory.Evaluate(state);
        Assert.That(lost.winnerOwner, Is.EqualTo(1));
        Assert.That(lost.conditionType, Is.EqualTo(SrpVictoryConditionTypes.UnitLost));
    }

    // ── 적 AI: 추가 조건 ─────────────────────────────────────────────────────

    [TestCase(TestName = "적 AI는 이번 이동으로 도달 칸에 설 수 있으면 공격보다 먼저 가고, 멀면 그쪽으로 다가간다")]
    public void EnemyAi_MovesOntoReachTile()
    {
        var state = CreateRuleState(victory: new[]
        {
            new SrpVictoryConditionData { type = SrpVictoryConditionTypes.ReachTile, owner = 1, x = 5, y = 2 },
        }, width: 9, height: 5);
        var runner = AddUnit(state, id: 1, owner: 1, x: 3, y: 2, attackPower: 8, moveRange: 4);
        // 바로 옆에 공격할 적이 있어도 도달이 먼저다. 적 옆 칸을 지나고 교전에서 벗어나는 비용까지 이동력 4로 닿는다.
        AddUnit(state, id: 2, owner: 0, x: 3, y: 3);

        var win = SrpEnemyAi.Decide(state, runner, 4, false);
        Assert.That(win.type, Is.EqualTo(SrpEnemyAiActionType.Move));
        Assert.That((win.x, win.y), Is.EqualTo((5, 2)));
        int remaining = 4;
        SrpBattleFlow.ExecuteAiAction(state, runner, win, ref remaining, null);
        Assert.That(SrpVictory.Evaluate(state).winnerOwner, Is.EqualTo(1));

        // 한 번에 닿지 않으면 도달 칸 쪽으로 다가간다.
        var far = CreateRuleState(victory: new[]
        {
            new SrpVictoryConditionData { type = SrpVictoryConditionTypes.ReachTile, owner = 1, x = 8, y = 2 },
        }, width: 9, height: 5);
        // 적(0,2)은 이번 차례에 공격할 수 없는 거리에 있다(적 옆 칸까지 비용 4).
        var walker = AddUnit(far, id: 1, owner: 1, x: 4, y: 2, attackPower: 8, moveRange: 3);
        AddUnit(far, id: 2, owner: 0, x: 0, y: 2);
        var approach = SrpEnemyAi.Decide(far, walker, 3, false);
        Assert.That(approach.type, Is.EqualTo(SrpEnemyAiActionType.Move));
        Assert.That((approach.x, approach.y), Is.EqualTo((7, 2)));

        // 조건이 상대 진영 것이면 그 칸은 접근 대상이 아니다. 적 쪽으로 간다.
        far.VictoryRule.conditions[0].owner = 0;
        var chase = SrpEnemyAi.Decide(far, walker, 3, false);
        Assert.That(chase.x, Is.LessThan(4), "상대의 도달 칸으로 다가갔습니다.");
    }

    [TestCase(TestName = "적 AI는 서면 자기 진영이 패배하는 칸으로는 가지 않는다")]
    public void EnemyAi_AvoidsTileThatDefeatsItsOwner()
    {
        // 적(4,2)의 서쪽 옆 칸 (3,2)가 가장 가까운 공격 칸이지만, 서면 패배하는 칸이다.
        var state = CreateRuleState(defeat: new[]
        {
            new SrpVictoryConditionData { type = SrpVictoryConditionTypes.ReachTile, owner = 1, x = 3, y = 2 },
        }, width: 7, height: 5);
        var actor = AddUnit(state, id: 1, owner: 1, x: 0, y: 2, attackPower: 8, moveRange: 4);
        AddUnit(state, id: 2, owner: 0, x: 4, y: 2);

        var move = SrpEnemyAi.Decide(state, actor, 4, false);
        Assert.That(move.type, Is.EqualTo(SrpEnemyAiActionType.Move));
        Assert.That((move.x, move.y), Is.Not.EqualTo((3, 2)), "서면 패배하는 칸으로 이동했습니다.");

        // 조건이 없으면 그 칸을 고른다(비교 기준).
        state.VictoryRule.defeatConditions = new SrpVictoryConditionData[0];
        var plain = SrpEnemyAi.Decide(state, actor, 4, false);
        Assert.That((plain.x, plain.y), Is.EqualTo((3, 2)));
    }

    [TestCase(TestName = "적 AI는 보호 대상을 지휘관처럼 다룬다: 상대의 보호 대상을 먼저 노리고, 자기 보호 대상은 나서지 않는다")]
    public void EnemyAi_TreatsProtectedUnitLikeCommander()
    {
        var state = CreateRuleState(defeat: new[]
        {
            new SrpVictoryConditionData { type = SrpVictoryConditionTypes.UnitLost, owner = 0, targetId = "p8_courier" },
            new SrpVictoryConditionData { type = SrpVictoryConditionTypes.UnitLost, owner = 1, targetId = "p8_envoy" },
        }, width: 9, height: 5);
        var attacker = AddUnit(state, id: 1, owner: 1, x: 2, y: 2, attackPower: 8);
        // 더 다친 일반 유닛이 있어도 보호 대상을 먼저 공격한다.
        var wounded = AddUnit(state, id: 2, owner: 0, x: 3, y: 2);
        wounded.hp = 20;
        var courier = AddUnit(state, id: 3, owner: 0, x: 2, y: 3);
        courier.templateId = "p8_courier";
        Assert.That(SrpEnemyAi.Decide(state, attacker, 0, false).targetUnitId, Is.EqualTo(courier.id));

        // 자기 진영의 보호 대상은 아군이 남아 있는 동안 공격으로 이어지지 않는 접근 이동을 하지 않는다.
        var envoy = AddUnit(state, id: 4, owner: 1, x: 8, y: 0, attackPower: 6, moveRange: 2);
        envoy.templateId = "p8_envoy";
        Assert.That(SrpEnemyAi.Decide(state, envoy, 2, false).type, Is.EqualTo(SrpEnemyAiActionType.EndTurn));
        envoy.templateId = "p8_plain";
        Assert.That(SrpEnemyAi.Decide(state, envoy, 2, false).type, Is.EqualTo(SrpEnemyAiActionType.Move));
    }

    [TestCase(TestName = "적 AI는 우리 편이 없는 거점으로 들어가고, 혼자 지키는 거점은 공격하러 비우지 않는다")]
    public void EnemyAi_EntersAndKeepsHoldZone()
    {
        // 거점은 (4,2) 한 칸 주변 3x3. 유지하는 쪽은 P0이지만 P1도 들어가서 막아야 한다.
        var state = CreateRuleState(victory: new[]
        {
            new SrpVictoryConditionData { type = SrpVictoryConditionTypes.HoldPoint, owner = 0, x = 4, y = 2, radius = 1, value = 3 },
        }, width: 11, height: 5);
        var blocker = AddUnit(state, id: 1, owner: 1, x: 7, y: 2, attackPower: 8, moveRange: 2, actionPoints: 1);
        var enemy = AddUnit(state, id: 2, owner: 0, x: 10, y: 4);

        // AP가 1이라 이동 뒤 공격할 수 없다. 적 쪽이 아니라 거점으로 들어간다.
        var enter = SrpEnemyAi.Decide(state, blocker, 2, false);
        Assert.That(enter.type, Is.EqualTo(SrpEnemyAiActionType.Move));
        Assert.That(SrpVictoryConditionTypes.IsInZone(state.VictoryRule.conditions[0], enter.x, enter.y), Is.True,
            "거점 안으로 들어가지 않았습니다.");

        // 거점을 혼자 지키는 유닛은 이동해서 공격할 수 있는 적이 밖에 있어도 거점을 비우지 않는다.
        // 적 옆 칸 (7,2)는 들어가는 비용이 1 더 들어 이동력 3으로 닿는다.
        blocker.anchorX = 5;
        blocker.anchorY = 2;
        blocker.actionPoints = 2;
        blocker.moveRange = 3;
        enemy.anchorX = 8;
        enemy.anchorY = 2;
        var keep = SrpEnemyAi.Decide(state, blocker, 3, false);
        if (keep.type == SrpEnemyAiActionType.Move)
            Assert.That(SrpVictoryConditionTypes.IsInZone(state.VictoryRule.conditions[0], keep.x, keep.y), Is.True,
                "혼자 지키던 거점을 비웠습니다.");

        // 다른 아군이 거점에 있으면 공격하러 나간다.
        AddUnit(state, id: 3, owner: 1, x: 4, y: 2);
        var leave = SrpEnemyAi.Decide(state, blocker, 3, false);
        Assert.That(leave.type, Is.EqualTo(SrpEnemyAiActionType.Move));
        Assert.That(System.Math.Max(System.Math.Abs(leave.x - 8), System.Math.Abs(leave.y - 2)), Is.EqualTo(1));
    }

    [TestCase(TestName = "적 AI는 시간이 상대 편이면 버티지 않고, 시간이 자기 편이면 공격으로 이어지지 않는 전진을 하지 않는다")]
    public void EnemyAi_TimePressureChangesHoldAndAdvance()
    {
        // 상대(P0)가 버티면 이기는 맵: 사격수는 엄폐 + 경계태세로 버티지 않고 전진한다.
        var press = CreateHoldState(out var shooter, new SrpVictoryRuleData
        {
            conditions = new[] { new SrpVictoryConditionData { type = SrpVictoryConditionTypes.SurviveRounds, owner = 0, value = 6 } },
        });
        Assert.That(SrpEnemyAi.Decide(press, shooter, shooter.moveRange, false).type, Is.EqualTo(SrpEnemyAiActionType.Move),
            "시간에 쫓기는데 버텼습니다.");

        // 조건이 없으면 같은 상황에서 버틴다(비교 기준, 7차 Q15).
        var hold = CreateHoldState(out var holder, new SrpVictoryRuleData());
        Assert.That(SrpEnemyAi.Decide(hold, holder, holder.moveRange, false).type, Is.EqualTo(SrpEnemyAiActionType.TakeCover));

        // 자기(P1)가 버티면 이기는 맵: 근접 유닛은 공격으로 이어지지 않는 접근 이동을 하지 않는다.
        var wait = CreateRuleState(victory: new[]
        {
            new SrpVictoryConditionData { type = SrpVictoryConditionTypes.SurviveRounds, owner = 1, value = 6 },
        }, width: 9, height: 5);
        var defender = AddUnit(wait, id: 1, owner: 1, x: 0, y: 2, attackPower: 8, moveRange: 3);
        var enemy = AddUnit(wait, id: 2, owner: 0, x: 8, y: 2);
        Assert.That(SrpEnemyAi.Decide(wait, defender, 3, false).type, Is.EqualTo(SrpEnemyAiActionType.EndTurn));
        // 이동해서 바로 공격할 수 있으면 나간다.
        enemy.anchorX = 3;
        Assert.That(SrpEnemyAi.Decide(wait, defender, 3, false).type, Is.EqualTo(SrpEnemyAiActionType.Move));

        // 제한 라운드가 상대에게 걸려 있어도 같다.
        var limit = CreateRuleState(defeat: new[]
        {
            new SrpVictoryConditionData { type = SrpVictoryConditionTypes.SurviveRounds, owner = 0, value = 6 },
        }, width: 9, height: 5);
        var waiter = AddUnit(limit, id: 1, owner: 1, x: 0, y: 2, attackPower: 8, moveRange: 3);
        AddUnit(limit, id: 2, owner: 0, x: 8, y: 2);
        Assert.That(SrpEnemyAi.Decide(limit, waiter, 3, false).type, Is.EqualTo(SrpEnemyAiActionType.EndTurn));
    }

    [Category("SrpAiSim")]
    [TestCase(TestName = "추가 조건 검증 랩: AI끼리의 전투가 추가 조건을 포함해 여러 방식으로 끝난다")]
    public void ConditionLab_SimulationEndsByListedConditions()
    {
        var config = new SrpBattleSimConfig
        {
            mapPreset = SrpMapPreset.M1ConditionLab,
            trials = 200,
            maxRounds = 24,
            baseSeed = 880101,
            sampleSeedCount = 1,
            owner0Policy = new SrpHeuristicAiPolicy(),
            owner1Policy = new SrpHeuristicAiPolicy(),
            thresholds = SrpSimThresholdConfig.Default(),
        };

        var outcome = SrpBattleSimRunner.RunBatch(config, writeReport: false).report.outcome;
        Debug.Log(
            $"[SRPG][AI-Sim][ConditionLab] w0={outcome.owner0WinRate:F3}, w1={outcome.owner1WinRate:F3}, " +
            $"draw={outcome.drawRate:F3}, avgR={outcome.averageRounds:F2}, custom={outcome.customConditionRate:F3}");

        // 측정값(2026-11-15, 200회, 8차 결정을 모두 반영한 뒤): 평균 8.43라운드, 승률 0.765 / 0.235, 무승부 0.
        // 종료 방식은 전멸 35.5%, 추가 조건 64.5%(거점 유지 43.5%, 전령 보호 실패 10.5%, 9라운드 버티기 10.5%).
        // AI의 전령은 위험한 칸을 피하느라 출구에 닿지 못한다(도달 0%). 도달 판정은 위의 단위 테스트가 고정한다.
        Assert.That(outcome.drawRate, Is.LessThanOrEqualTo(0.05f), "9라운드 버티기가 있어 무승부가 나오면 안 됩니다.");
        Assert.That(outcome.averageRounds, Is.LessThanOrEqualTo(9f), "9라운드가 끝나면 승패가 나야 합니다.");
        Assert.That(outcome.customConditionRate, Is.GreaterThan(0.2f), "추가 조건으로 끝난 판이 거의 없습니다.");
        Assert.That(outcome.owner0WinRate, Is.InRange(0.1f, 0.9f), "한쪽이 일방적으로 이깁니다.");
    }

    // ── 기획 결정 Q18: 권총/소총 구분, 회피 가중치 ───────────────────────────

    [TestCase(TestName = "부무장 사격(권총)은 소총보다 약하고, 총기 종류를 데이터로 정하면 주무장과 무관하게 따른다")]
    public void Firearm_PistolDealsLessThanRifleAndTypeCanBeSetInData()
    {
        var state = CreateState(9, 5);
        // 주무장이 총기인 유닛은 소총, 아닌 유닛의 사격은 권총(부무장)이다. 둘 다 공격력 8.
        var rifle = AddUnit(state, id: 1, owner: 0, x: 0, y: 1, attackPower: 8, attackRange: 4,
            weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1, ammo: 1);
        var sidearm = AddUnit(state, id: 2, owner: 0, x: 0, y: 3, attackPower: 8, attackRange: 4,
            weaponClass: SrpWeaponClass.Melee, maxAmmo: 1, ammo: 1);
        Assert.That(SrpCombatResolver.ResolveFirearmType(rifle), Is.EqualTo(SrpFirearmType.Rifle));
        Assert.That(SrpCombatResolver.ResolveFirearmType(sidearm), Is.EqualTo(SrpFirearmType.Pistol));

        SrpCombatResolver.GetBaseBasicAttackDamage(SrpBasicAttackKind.Firearm, rifle, out int rifleHp, out _);
        SrpCombatResolver.GetBaseBasicAttackDamage(SrpBasicAttackKind.Firearm, sidearm, out int pistolHp, out _);
        Assert.That(rifleHp, Is.EqualTo(8 * 2 + 4));
        Assert.That(pistolHp, Is.EqualTo(12), "권총 피해 식(공격력 + 4)과 다릅니다.");

        // 실제 사격도 같은 기준 피해를 쓴다(대상은 정면, 반응 없음).
        var targetA = AddUnit(state, id: 3, owner: 1, x: 3, y: 1);
        targetA.facing = SrpFacing.West;
        var targetB = AddUnit(state, id: 4, owner: 1, x: 3, y: 3);
        targetB.facing = SrpFacing.West;
        Assert.That(SrpCombatResolver.ApplyAttack(state, rifle, targetA).damageToHp, Is.EqualTo(rifleHp));
        Assert.That(SrpCombatResolver.ApplyAttack(state, sidearm, targetB).damageToHp, Is.EqualTo(pistolHp));

        // 근접 기본 공격은 총기 종류와 무관하다.
        SrpCombatResolver.GetBaseBasicAttackDamage(SrpBasicAttackKind.Melee, sidearm, out int meleeHp, out int meleePg);
        Assert.That((meleeHp, meleePg), Is.EqualTo((2, 8)));

        // 데이터로 정한 종류가 주무장보다 먼저다.
        sidearm.firearmType = SrpFirearmType.Rifle;
        rifle.firearmType = SrpFirearmType.Pistol;
        SrpCombatResolver.GetBaseBasicAttackDamage(SrpBasicAttackKind.Firearm, sidearm, out int forcedRifleHp, out _);
        SrpCombatResolver.GetBaseBasicAttackDamage(SrpBasicAttackKind.Firearm, rifle, out int forcedPistolHp, out _);
        Assert.That(forcedRifleHp, Is.EqualTo(rifleHp));
        Assert.That(forcedPistolHp, Is.EqualTo(pistolHp));

        // 유닛 템플릿의 값은 저장 형식을 거쳐 전투 유닛까지 따라간다. 값이 없는 기존 데이터는 Auto다.
        var map = new SrpMapFileV1
        {
            width = 3,
            height = 3,
            walkable = AllWalkable(3, 3),
            playerOrder = new[] { 0, 1 },
            templates = new[]
            {
                new SrpUnitTemplateData { id = "p8_gunner", weaponClass = SrpWeaponClass.Melee, firearmType = SrpFirearmType.Rifle },
                new SrpUnitTemplateData { id = "p8_plain", weaponClass = SrpWeaponClass.Melee },
            },
            placements = new[]
            {
                new SrpPlacementData { templateId = "p8_gunner", owner = 0, x = 0, y = 0, footprint = new SrpOffset[0] },
                new SrpPlacementData { templateId = "p8_plain", owner = 1, x = 2, y = 2, footprint = new SrpOffset[0] },
            },
        };
        var loaded = SrpBattleState.FromMap(JsonUtility.FromJson<SrpMapFileV1>(JsonUtility.ToJson(map)));
        Assert.That(loaded.Units[0].firearmType, Is.EqualTo(SrpFirearmType.Rifle));
        Assert.That(loaded.Units[1].firearmType, Is.EqualTo(SrpFirearmType.Auto));
        Assert.That(loaded.Clone().Units[0].firearmType, Is.EqualTo(SrpFirearmType.Rifle));
    }

    [TestCase(TestName = "회피 성공률은 기준 50%에서 측면·후면 공격이면 낮아지고 속도 차이만큼 오르내리며 10~80% 안에 머문다")]
    public void Dodge_ChanceDependsOnDirectionAndSpeed()
    {
        var state = CreateState(9, 9);
        var defender = AddUnit(state, id: 1, owner: 1, x: 4, y: 4);
        defender.stance = SrpStance.Aggressive;
        defender.facing = SrpFacing.East;
        defender.speed = 10;
        var front = AddUnit(state, id: 2, owner: 0, x: 7, y: 4);
        var side = AddUnit(state, id: 3, owner: 0, x: 4, y: 7);
        var rear = AddUnit(state, id: 4, owner: 0, x: 1, y: 4);
        front.speed = side.speed = rear.speed = 10;

        Assert.That(SrpCombatResolver.DodgeChancePercent, Is.EqualTo(50));
        Assert.That(SrpCombatResolver.GetDodgeChancePercent(state, front, defender), Is.EqualTo(50));
        Assert.That(SrpCombatResolver.GetDodgeChancePercent(state, side, defender), Is.EqualTo(50 - 15));
        Assert.That(SrpCombatResolver.GetDodgeChancePercent(state, rear, defender), Is.EqualTo(50 - 30));

        // 속도 차이(방어자 - 공격자) 1마다 2%p.
        front.speed = 7;
        Assert.That(SrpCombatResolver.GetDodgeChancePercent(state, front, defender), Is.EqualTo(50 + 3 * 2));
        front.speed = 14;
        Assert.That(SrpCombatResolver.GetDodgeChancePercent(state, front, defender), Is.EqualTo(50 - 4 * 2));

        // 하한과 상한.
        defender.speed = 40;
        Assert.That(SrpCombatResolver.GetDodgeChancePercent(state, front, defender), Is.EqualTo(80));
        defender.speed = 0;
        rear.speed = 30;
        Assert.That(SrpCombatResolver.GetDodgeChancePercent(state, rear, defender), Is.EqualTo(10));
    }

    [TestCase(TestName = "실제 회피 판정은 대상별 성공률을 쓴다: 후면 사격은 정면 사격보다 덜 빗나간다")]
    public void Dodge_ResolutionUsesPerTargetChance()
    {
        int frontDodges = CountDodges(attackerX: 7, attackerY: 4);
        int rearDodges = CountDodges(attackerX: 1, attackerY: 4);

        const int rolls = 2000;
        int frontChance = 50;
        int rearChance = 50 - 30;
        Assert.That(frontDodges, Is.InRange(rolls * (frontChance - 5) / 100, rolls * (frontChance + 5) / 100));
        Assert.That(rearDodges, Is.InRange(rolls * (rearChance - 5) / 100, rolls * (rearChance + 5) / 100));
    }

    /// <summary>동쪽을 보는 공격 태세 유닛(4,4)에게 같은 속도의 사격수가 2000번 쏴서 회피된 횟수.</summary>
    static int CountDodges(int attackerX, int attackerY)
    {
        var state = CreateState(9, 9);
        state.SeedRandom(4242);
        var defender = AddUnit(state, id: 1, owner: 1, x: 4, y: 4);
        defender.stance = SrpStance.Aggressive;
        defender.facing = SrpFacing.East;
        defender.hp = defender.maxHp = 100000;
        defender.pg = defender.maxPg = 100000;
        var shooter = AddUnit(state, id: 2, owner: 0, x: attackerX, y: attackerY, attackPower: 1, attackRange: 4,
            weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1, ammo: 1);

        int dodged = 0;
        for (int i = 0; i < 2000; i++)
        {
            defender.reactionPoints = 1;
            if (SrpCombatResolver.ApplyAttack(state, shooter, defender).wasDodged)
                dodged++;
        }
        return dodged;
    }

    // ── 전장 구성 ────────────────────────────────────────────────────────────

    static bool[] AllWalkable(int width, int height)
    {
        var walk = new bool[width * height];
        for (int i = 0; i < walk.Length; i++)
            walk[i] = true;
        return walk;
    }

    /// <summary>승리 조건 목록과 패배 조건 목록을 건 빈 맵.</summary>
    static SrpBattleState CreateRuleState(
        SrpVictoryConditionData[] victory = null,
        SrpVictoryConditionData[] defeat = null,
        int width = 5,
        int height = 5)
    {
        return SrpBattleState.FromMap(new SrpMapFileV1
        {
            width = width,
            height = height,
            walkable = AllWalkable(width, height),
            playerOrder = new[] { 0, 1 },
            victory = new SrpVictoryRuleData
            {
                conditions = victory ?? new SrpVictoryConditionData[0],
                defeatConditions = defeat ?? new SrpVictoryConditionData[0],
            },
        });
    }

    /// <summary>
    /// 버티기 판단용 전장(7차 Q15와 같은 배치). 사격수(2,2)는 이동해도 이번 차례에 쏠 수 없고 옆에 엄폐물이 있다.
    /// 상대 사격수(10,2)는 한 번 움직이면 쏠 수 있고, 근접 적(10,3)은 다음 이동으로 사거리에 들어온다.
    /// </summary>
    static SrpBattleState CreateHoldState(out SrpUnitRuntime shooter, SrpVictoryRuleData rule)
    {
        var state = SrpBattleState.FromMap(new SrpMapFileV1
        {
            width = 13,
            height = 5,
            walkable = AllWalkable(13, 5),
            playerOrder = new[] { 0, 1 },
            victory = rule,
        });
        state.Walkable[state.Index(2, 1)] = false;
        state.CoverObjects.Add(new SrpCoverObjectData { x = 2, y = 1, coverDef = 2, coverGrd = 1, blocksLineOfSight = true });
        shooter = AddUnit(state, id: 1, owner: 1, x: 2, y: 2, attackPower: 8, attackRange: 4, moveRange: 2,
            weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1, ammo: 1);
        shooter.reactionPoints = 1;
        AddUnit(state, id: 2, owner: 0, x: 10, y: 2, attackPower: 8, attackRange: 5, moveRange: 3,
            weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1, ammo: 1);
        AddUnit(state, id: 3, owner: 0, x: 10, y: 3, attackPower: 6, moveRange: 4);
        return state;
    }

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

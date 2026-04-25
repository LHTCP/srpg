using NUnit.Framework;
using UnityEngine;

[Category("SrpM1All")]
public class SrpM1CoreTests
{
    [Test]
    public void TurnOrder_UsesSpeedDescending()
    {
        var map = SrpDefaultMaps.GetPreset(SrpMapPreset.M1QaIntegrated);
        var state = SrpBattleState.FromMap(map);

        var queue = SrpTurnOrder.BuildRoundQueue(state);
        Assert.Greater(queue.Count, 0);

        int prevSpeed = int.MaxValue;
        foreach (int id in queue)
        {
            var unit = FindUnit(state, id);
            Assert.IsNotNull(unit);
            Assert.LessOrEqual(unit.speed, prevSpeed);
            prevSpeed = unit.speed;
        }
    }

    [Test]
    public void TurnOrder_UsesOwnerAndIdAsTieBreaker_WhenSpeedSame()
    {
        var map = SrpDefaultMaps.GetPreset(SrpMapPreset.M1QaIntegrated);
        var state = SrpBattleState.FromMap(map);
        foreach (var unit in state.Units)
            unit.speed = 10;

        var queue = SrpTurnOrder.BuildRoundQueue(state);
        Assert.Greater(queue.Count, 1);

        int prevOwner = int.MinValue;
        int prevId = int.MinValue;
        foreach (int id in queue)
        {
            var unit = FindUnit(state, id);
            Assert.IsNotNull(unit);

            if (unit.owner == prevOwner)
                Assert.Greater(unit.id, prevId, "동일 owner 내 id 오름차순 타이브레이크 불일치");
            else
                Assert.GreaterOrEqual(unit.owner, prevOwner, "owner 오름차순 타이브레이크 불일치");

            prevOwner = unit.owner;
            prevId = unit.id;
        }
    }

    [Test]
    public void CombatSplit_FirearmAndMeleeProduceDifferentPressure()
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
    }

    [Test]
    public void TurnOrder_SkipsEliminatedUnits_WhenAdvancing()
    {
        var map = SrpDefaultMaps.GetPreset(SrpMapPreset.M1QaIntegrated);
        var state = SrpBattleState.FromMap(map);
        state.RoundQueue.Clear();
        state.RoundQueue.AddRange(SrpTurnOrder.BuildRoundQueue(state));
        Assert.Greater(state.RoundQueue.Count, 2);

        int removedId = state.RoundQueue[0];
        var removed = FindUnit(state, removedId);
        Assert.IsNotNull(removed);
        removed.eliminated = true;

        int nextId = SrpTurnOrder.AdvanceToNextUnit(state);
        Assert.AreNotEqual(removedId, nextId, "제거된 유닛이 턴 큐에서 건너뛰어지지 않았습니다.");
    }

    [Test]
    public void TurnOrder_ResetRoundResources_RestoresRpAndClearsReactionState()
    {
        var map = SrpDefaultMaps.GetPreset(SrpMapPreset.M1QaIntegrated);
        var state = SrpBattleState.FromMap(map);
        var unit = state.Units[0];
        unit.actionPoints = 0;
        unit.reactionPoints = 0;
        unit.passiveAppliedThisTurn = true;
        unit.lastReactionKind = SrpReactionKind.Guard;
        unit.lastReactionRound = 99;
        unit.lastReactionSourceId = 1234;
        unit.overwatchArmed = true;
        unit.overwatchRange = 4;
        unit.overwatchRound = 99;
        unit.defensiveHitsTakenThisRound = 3;
        unit.defensiveHitsRound = 99;

        SrpTurnOrder.ResetRoundResources(state);

        Assert.AreEqual(unit.maxActionPoints, unit.actionPoints, "라운드 리셋 시 AP가 회복되지 않았습니다.");
        Assert.AreEqual(unit.maxReactionPoints > 0 ? unit.maxReactionPoints : 1, unit.reactionPoints, "라운드 리셋 시 RP 정책이 불일치합니다.");
        Assert.IsFalse(unit.passiveAppliedThisTurn, "라운드 리셋 시 패시브 플래그가 초기화되지 않았습니다.");
        Assert.AreEqual(SrpReactionKind.None, unit.lastReactionKind, "라운드 리셋 시 반응 상태가 초기화되지 않았습니다.");
        Assert.AreEqual(state.RoundNumber, unit.lastReactionRound, "반응 상태의 라운드 기준이 현재 라운드로 갱신되지 않았습니다.");
        Assert.AreEqual(-1, unit.lastReactionSourceId, "라운드 리셋 시 반응 원천 ID가 초기화되지 않았습니다.");
        Assert.IsFalse(unit.overwatchArmed, "라운드 리셋 시 오버워치 예약이 해제되지 않았습니다.");
        Assert.AreEqual(0, unit.overwatchRange, "라운드 리셋 시 오버워치 사거리가 초기화되지 않았습니다.");
        Assert.AreEqual(0, unit.overwatchRound, "라운드 리셋 시 오버워치 라운드가 초기화되지 않았습니다.");
        Assert.AreEqual(0, unit.defensiveHitsTakenThisRound, "라운드 리셋 시 수비 피격 누적이 초기화되지 않았습니다.");
        Assert.AreEqual(state.RoundNumber, unit.defensiveHitsRound, "라운드 리셋 시 수비 피격 라운드가 갱신되지 않았습니다.");
    }

    [Test]
    public void SkillCharges_BlockUse_WhenNoChargesRemain()
    {
        var skill = CreateChargedSkill();
        var runtime = new SrpSkillRuntime(skill.id)
        {
            chargesRemaining = 0,
            chargesInitialized = true,
        };

        Assert.IsFalse(SrpSkills.CanUseActiveSkill(skill, runtime), "충전이 0인 스킬이 사용 가능으로 판정되었습니다.");
    }

    [Test]
    public void SkillUse_ConsumesChargeAndAppliesCooldown()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.GetPreset(SrpMapPreset.M1QaIntegrated));
        var caster = state.Units[0];
        var skill = CreateChargedSkill();
        var runtime = new SrpSkillRuntime(skill.id)
        {
            chargesRemaining = 2,
            chargesInitialized = true,
        };

        SrpSkills.ResolveActiveSkill(skill, runtime, caster, caster.anchorX, caster.anchorY, state, null);

        Assert.AreEqual(1, runtime.chargesRemaining, "스킬 사용 후 충전이 감소하지 않았습니다.");
        Assert.AreEqual(skill.cooldown, runtime.cooldownRemaining, "스킬 사용 후 쿨다운이 설정되지 않았습니다.");
        Assert.Greater(runtime.chargeRecoveryRemaining, 0, "충전 회복 타이머가 시작되지 않았습니다.");
    }

    [Test]
    public void SkillResourceTick_ReducesCooldownAndRestoresCharge()
    {
        var skill = CreateChargedSkill();
        var runtime = new SrpSkillRuntime(skill.id)
        {
            cooldownRemaining = 2,
            chargesRemaining = 0,
            chargeRecoveryRemaining = 1,
            chargesInitialized = true,
        };

        SrpSkills.TickSkillResources(skill, runtime);

        Assert.AreEqual(1, runtime.cooldownRemaining, "스킬 자원 틱이 쿨다운을 감소시키지 않았습니다.");
        Assert.AreEqual(1, runtime.chargesRemaining, "스킬 자원 틱이 충전을 회복하지 않았습니다.");
    }

    [Test]
    public void SkillOverclock_SpendsFrozenHeartAndRestoresSkillResource()
    {
        var caster = new SrpUnitRuntime
        {
            displayName = "Caster",
            frozenHeart = 10,
        };
        var skill = CreateChargedSkill();
        skill.overclockFrozenHeartCost = 5;
        skill.overclockCooldownReduction = 2;
        skill.overclockChargeRestore = 1;
        var runtime = new SrpSkillRuntime(skill.id)
        {
            cooldownRemaining = 3,
            chargesRemaining = 0,
            chargesInitialized = true,
        };

        bool applied = SrpSkills.TryOverclockSkill(caster, skill, runtime, null);

        Assert.IsTrue(applied, "오버클럭이 적용되지 않았습니다.");
        Assert.AreEqual(5, caster.frozenHeart, "오버클럭이 FH 비용을 소비하지 않았습니다.");
        Assert.AreEqual(1, runtime.cooldownRemaining, "오버클럭이 쿨다운을 단축하지 않았습니다.");
        Assert.AreEqual(1, runtime.chargesRemaining, "오버클럭이 충전을 복구하지 않았습니다.");
    }

    [Test]
    public void UnitViewFacingRotation_MapsFacingToWedgeForwardDirection()
    {
        AssertFacingForward(SrpFacing.North, Vector3.forward);
        AssertFacingForward(SrpFacing.East, Vector3.right);
        AssertFacingForward(SrpFacing.South, Vector3.back);
        AssertFacingForward(SrpFacing.West, Vector3.left);
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

    static SrpSkillData CreateChargedSkill()
    {
        return new SrpSkillData
        {
            id = "charged_test",
            displayName = "Charged Test",
            skillType = SrpSkillType.Active,
            trigger = SrpSkillTrigger.OnActivate,
            targetType = SrpTargetType.Self,
            cooldown = 2,
            maxCharges = 2,
            chargeRecoveryTurns = 1,
            effects = new[]
            {
                new SrpSkillEffect
                {
                    type = SrpEffectType.Heal,
                    stat = "hp",
                    value = 1,
                },
            },
        };
    }

    static void AssertFacingForward(SrpFacing facing, Vector3 expected)
    {
        Vector3 actual = SrpGameController.GetFacingRotation(facing) * Vector3.forward;
        Assert.Less(Vector3.Distance(expected, actual), 0.001f, $"{facing} 유닛 뷰 전방 방향 불일치");
    }

    static SrpUnitRuntime FindUnit(SrpBattleState state, int id)
    {
        foreach (var unit in state.Units)
            if (unit.id == id)
                return unit;
        return null;
    }
}

using NUnit.Framework;

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

    static SrpUnitRuntime FindUnit(SrpBattleState state, int id)
    {
        foreach (var unit in state.Units)
            if (unit.id == id)
                return unit;
        return null;
    }
}

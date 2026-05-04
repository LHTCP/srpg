using NUnit.Framework;

public class SrpCombatResolverTests
{
    [TestCase(TestName = "총기 피해는 실제 HP 피해의 50%를 PG로 파급한다")]
    public void ApplyAttack_FirearmSpillsHalfFinalHpDamageToPg()
    {
        var attacker = Unit(attackPower: 8, weaponClass: SrpWeaponClass.Firearm, stance: SrpStance.Defensive);
        var defender = Unit(hp: 30, pg: 20, maxPg: 20);

        var outcome = SrpCombatResolver.ApplyAttack(attacker, defender);

        Assert.That(outcome.damageToHp, Is.EqualTo(22));
        Assert.That(outcome.damageToPg, Is.EqualTo(11));
        Assert.That(defender.hp, Is.EqualTo(8));
        Assert.That(defender.pg, Is.EqualTo(9));
    }

    [TestCase(TestName = "근접 공격은 HP보다 PG 붕괴를 우선한다")]
    public void ApplyAttack_MeleeBuildsPgPressureAndGroggy()
    {
        var attacker = Unit(attackPower: 6, weaponClass: SrpWeaponClass.Melee, stance: SrpStance.Aggressive);
        var defender = Unit(hp: 20, pg: 7, maxPg: 7);

        var outcome = SrpCombatResolver.ApplyAttack(attacker, defender);

        Assert.That(outcome.damageToHp, Is.EqualTo(1));
        Assert.That(outcome.damageToPg, Is.EqualTo(11));
        Assert.That(outcome.becameGroggy, Is.True);
        Assert.That(defender.groggy, Is.True);
        Assert.That(defender.pg, Is.EqualTo(0));
    }

    [TestCase(TestName = "그로기 대상은 처단 피해를 받고 그로기 상태가 해제된다")]
    public void ApplyAttack_GroggyExecution()
    {
        var attacker = Unit(attackPower: 8, weaponClass: SrpWeaponClass.Melee);
        var defender = Unit(hp: 20, pg: 5, maxPg: 5);
        defender.groggy = true;

        var outcome = SrpCombatResolver.ApplyAttack(attacker, defender);

        Assert.That(outcome.wasExecution, Is.True);
        Assert.That(outcome.damageToPg, Is.EqualTo(0));
        Assert.That(outcome.damageToHp, Is.EqualTo(14));
        Assert.That(defender.hp, Is.EqualTo(6));
        Assert.That(defender.groggy, Is.False);
        Assert.That(defender.pg, Is.EqualTo(0));
    }

    static SrpUnitRuntime Unit(
        int hp = 10,
        int pg = 10,
        int maxPg = 10,
        int attackPower = 0,
        SrpWeaponClass weaponClass = SrpWeaponClass.Melee,
        SrpStance stance = SrpStance.Aggressive)
    {
        return new SrpUnitRuntime
        {
            hp = hp,
            maxHp = hp,
            pg = pg,
            maxPg = maxPg,
            attackPower = attackPower,
            attackRange = 1,
            weaponClass = weaponClass,
            stance = stance,
        };
    }
}

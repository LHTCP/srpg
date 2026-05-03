using NUnit.Framework;

public class SrpCombatResolverTests
{
    [TestCase(TestName = "공격하면 AP 피해를 먼저 적용하고 남은 피해만 HP에 적용한다")]
    public void ApplyAttack_ApFirst()
    {
        var attacker = Unit(attackPower: 7);
        var defender = Unit(hp: 20, ap: 5);

        var outcome = SrpCombatResolver.ApplyAttack(attacker, defender);

        Assert.That(outcome.damageToAp, Is.EqualTo(5));
        Assert.That(outcome.damageToHp, Is.EqualTo(2));
        Assert.That(defender.ap, Is.EqualTo(0));
        Assert.That(defender.hp, Is.EqualTo(18));
    }

    [TestCase(TestName = "HP 피해가 발생하면 자세가 누적되고 최대치에 도달하면 그로기가 된다")]
    public void ApplyAttack_HpDamageBuildsPosture()
    {
        var attacker = Unit(attackPower: 6);
        var defender = Unit(hp: 20, ap: 0, posture: 2, maxPosture: 5);

        var outcome = SrpCombatResolver.ApplyAttack(attacker, defender);

        Assert.That(outcome.damageToHp, Is.EqualTo(6));
        Assert.That(outcome.postureGained, Is.EqualTo(3));
        Assert.That(outcome.becameGroggy, Is.True);
        Assert.That(defender.groggy, Is.True);
        Assert.That(defender.posture, Is.EqualTo(5));
    }

    [TestCase(TestName = "그로기 대상은 처단 피해를 받고 그로기와 자세가 초기화된다")]
    public void ApplyAttack_GroggyExecution()
    {
        var attacker = Unit(attackPower: 8);
        var defender = Unit(hp: 20, ap: 6, posture: 5, maxPosture: 5);
        defender.groggy = true;

        var outcome = SrpCombatResolver.ApplyAttack(attacker, defender);

        Assert.That(outcome.wasExecution, Is.True);
        Assert.That(outcome.damageToAp, Is.EqualTo(0));
        Assert.That(outcome.damageToHp, Is.EqualTo(8));
        Assert.That(defender.ap, Is.EqualTo(6));
        Assert.That(defender.hp, Is.EqualTo(12));
        Assert.That(defender.groggy, Is.False);
        Assert.That(defender.posture, Is.EqualTo(0));
    }

    static SrpUnitRuntime Unit(
        int hp = 10,
        int ap = 0,
        int posture = 0,
        int maxPosture = 5,
        int attackPower = 0)
    {
        return new SrpUnitRuntime
        {
            hp = hp,
            maxHp = hp,
            ap = ap,
            maxAp = ap,
            posture = posture,
            maxPosture = maxPosture,
            attackPower = attackPower,
            attackRange = 1,
        };
    }
}

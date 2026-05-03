using NUnit.Framework;

public class SrpCombatResolverTests
{
    [Test]
    public void ApplyAttack_ConsumesApBeforeHpDamage()
    {
        var attacker = Unit(attackPower: 7);
        var defender = Unit(hp: 20, ap: 5);

        var outcome = SrpCombatResolver.ApplyAttack(attacker, defender);

        Assert.That(outcome.damageToAp, Is.EqualTo(5));
        Assert.That(outcome.damageToHp, Is.EqualTo(2));
        Assert.That(defender.ap, Is.EqualTo(0));
        Assert.That(defender.hp, Is.EqualTo(18));
    }

    [Test]
    public void ApplyAttack_HpDamageBuildsPostureAndCanCauseGroggy()
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

    [Test]
    public void ApplyAttack_GroggyDefenderReceivesExecutionAndResetsPosture()
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

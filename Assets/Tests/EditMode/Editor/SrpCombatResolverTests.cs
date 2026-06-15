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

    [TestCase(TestName = "그로기 대상은 인접 처단으로 확정 사망한다")]
    public void ApplyAttack_GroggyExecution()
    {
        var state = CreateState();
        var attacker = Unit(id: 1, owner: 0, x: 1, y: 1, attackPower: 8, weaponClass: SrpWeaponClass.Melee);
        var defender = Unit(id: 2, owner: 1, x: 2, y: 1, hp: 20, pg: 5, maxPg: 5);
        defender.groggy = true;
        state.Units.Add(attacker);
        state.Units.Add(defender);

        var outcome = SrpCombatResolver.ApplyAttack(state, attacker, defender);

        Assert.That(outcome.wasExecution, Is.True);
        Assert.That(outcome.defenderDied, Is.True);
        Assert.That(outcome.damageToPg, Is.EqualTo(0));
        Assert.That(outcome.damageToHp, Is.GreaterThanOrEqualTo(20));
        Assert.That(defender.hp, Is.LessThanOrEqualTo(0));
        Assert.That(defender.groggy, Is.False);
        Assert.That(defender.pg, Is.EqualTo(0));
    }

    [Test]
    public void ApplyAttack_FirearmRoleExecutesAdjacentPgBrokenTarget()
    {
        var state = CreateState();
        var attacker = Unit(id: 1, owner: 0, x: 1, y: 1, attackPower: 8, weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1, ammo: 0);
        var defender = Unit(id: 2, owner: 1, x: 2, y: 1, hp: 20, pg: 0, maxPg: 5);
        defender.groggy = true;
        state.Units.Add(attacker);
        state.Units.Add(defender);

        Assert.That(SrpCombatResolver.CanAttack(state, attacker, defender), Is.True);

        var outcome = SrpCombatResolver.ApplyAttack(state, attacker, defender);

        Assert.That(outcome.basicAttackKind, Is.EqualTo(SrpBasicAttackKind.Melee));
        Assert.That(outcome.wasExecution, Is.True);
        Assert.That(outcome.defenderDied, Is.True);
        Assert.That(defender.hp, Is.LessThanOrEqualTo(0));
        Assert.That(attacker.ammo, Is.EqualTo(0));
    }

    [Test]
    public void ApplyAttack_FirearmRoleAdjacentAttackDoesNotUseFirearmSpillover()
    {
        var state = CreateState();
        var attacker = Unit(id: 1, owner: 0, x: 1, y: 1, attackPower: 8, attackRange: 4, weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1, ammo: 1);
        var defender = Unit(id: 2, owner: 1, x: 2, y: 1, hp: 30, pg: 20, maxPg: 20);
        state.Units.Add(attacker);
        state.Units.Add(defender);

        var outcome = SrpCombatResolver.ApplyAttack(state, attacker, defender);

        Assert.That(outcome.basicAttackKind, Is.EqualTo(SrpBasicAttackKind.Melee));
        Assert.That(outcome.firearmPgSpillover, Is.EqualTo(0));
        Assert.That(outcome.damageToPg, Is.GreaterThan(0));
    }

    [Test]
    public void CanAttack_AdjacentMeleeIgnoresAmmoButNonAdjacentFirearmRequiresAmmo()
    {
        var state = CreateState();
        var attacker = Unit(id: 1, owner: 0, x: 1, y: 1, attackPower: 8, attackRange: 4, weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1, ammo: 0);
        var adjacent = Unit(id: 2, owner: 1, x: 2, y: 1);
        var distant = Unit(id: 3, owner: 1, x: 1, y: 4);
        state.Units.Add(attacker);
        state.Units.Add(adjacent);
        state.Units.Add(distant);

        Assert.That(SrpCombatResolver.ResolveBasicAttackKind(state, attacker, adjacent), Is.EqualTo(SrpBasicAttackKind.Melee));
        Assert.That(SrpCombatResolver.CanAttack(state, attacker, adjacent), Is.True);
        Assert.That(SrpCombatResolver.SpendAmmoForBasicAttack(SrpBasicAttackKind.Melee, attacker), Is.True);
        Assert.That(attacker.ammo, Is.EqualTo(0));

        Assert.That(SrpCombatResolver.ResolveBasicAttackKind(state, attacker, distant), Is.EqualTo(SrpBasicAttackKind.Firearm));
        Assert.That(SrpCombatResolver.CanAttack(state, attacker, distant), Is.False);
    }

    [Test]
    public void ApplyAttack_NonAdjacentFirearmConsumesAmmoAndAppliesSpillover()
    {
        var state = CreateState();
        var attacker = Unit(id: 1, owner: 0, x: 1, y: 1, attackPower: 8, attackRange: 4, weaponClass: SrpWeaponClass.Melee, maxAmmo: 1, ammo: 1);
        var defender = Unit(id: 2, owner: 1, x: 1, y: 4, hp: 30, pg: 20, maxPg: 20);
        state.Units.Add(attacker);
        state.Units.Add(defender);

        Assert.That(SrpCombatResolver.CanAttack(state, attacker, defender), Is.True);
        var kind = SrpCombatResolver.ResolveBasicAttackKind(state, attacker, defender);
        Assert.That(kind, Is.EqualTo(SrpBasicAttackKind.Firearm));
        Assert.That(SrpCombatResolver.SpendAmmoForBasicAttack(kind, attacker), Is.True);

        var outcome = SrpCombatResolver.ApplyAttack(state, attacker, defender);

        Assert.That(attacker.ammo, Is.EqualTo(0));
        Assert.That(outcome.basicAttackKind, Is.EqualTo(SrpBasicAttackKind.Firearm));
        Assert.That(outcome.firearmPgSpillover, Is.GreaterThan(0));
    }

    [Test]
    public void SimulationMetrics_RecordActualResolvedAttackKind()
    {
        var state = CreateState();
        var attacker = Unit(id: 1, owner: 0, x: 1, y: 1, hp: 40, maxHp: 40, attackPower: 8, weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1, ammo: 1);
        attacker.maxActionPoints = 1;
        attacker.actionPoints = 1;
        attacker.speed = 10;
        var defender = Unit(id: 2, owner: 1, x: 2, y: 1, hp: 40, maxHp: 40, pg: 20, maxPg: 20);
        defender.maxActionPoints = 1;
        defender.actionPoints = 1;
        defender.speed = 1;
        state.Units.Add(attacker);
        state.Units.Add(defender);

        var result = SrpBattleSimRunner.RunSingle(state, new SrpBattleSimConfig
        {
            maxRounds = 1,
            owner0Policy = new AttackFirstPolicy(),
            owner1Policy = new EndTurnPolicy(),
        }, 7);

        Assert.That(result.meleeHpDamage + result.meleePgDamage, Is.GreaterThan(0));
        Assert.That(result.firearmHpDamage + result.firearmPgDamage, Is.EqualTo(0));
    }

    [TestCase(TestName = "원거리 또는 비인접 공격은 그로기 대상을 자동 처단하지 않는다")]
    public void ApplyAttack_NonAdjacentThreatDoesNotExecuteGroggyTarget()
    {
        var state = CreateState();
        var attacker = Unit(id: 1, owner: 0, x: 0, y: 4, attackPower: 8, attackRange: 4, weaponClass: SrpWeaponClass.Firearm);
        var defender = Unit(id: 2, owner: 1, x: 0, y: 1, hp: 40, pg: 0, maxPg: 24);
        defender.groggy = true;
        state.Units.Add(attacker);
        state.Units.Add(defender);

        var outcome = SrpCombatResolver.ApplyAttack(state, attacker, defender);

        Assert.That(outcome.wasExecution, Is.False);
        Assert.That(outcome.damageToHp, Is.GreaterThan(0));
        Assert.That(defender.hp, Is.GreaterThan(0));
        Assert.That(defender.groggy, Is.True);
    }

    [TestCase(TestName = "비인접 근접 무기는 그로기 대상을 처단 위협으로 보지 않는다")]
    public void ApplyAttack_NonAdjacentMeleeDoesNotExecuteGroggyTarget()
    {
        var state = CreateState();
        var attacker = Unit(id: 1, owner: 0, x: 0, y: 4, attackPower: 8, attackRange: 4, weaponClass: SrpWeaponClass.Melee);
        var defender = Unit(id: 2, owner: 1, x: 0, y: 1, hp: 40, pg: 0, maxPg: 24);
        defender.groggy = true;
        state.Units.Add(attacker);
        state.Units.Add(defender);

        var outcome = SrpCombatResolver.ApplyAttack(state, attacker, defender);

        Assert.That(outcome.wasExecution, Is.False);
        Assert.That(defender.hp, Is.GreaterThan(0));
        Assert.That(defender.groggy, Is.True);
    }

    [TestCase(TestName = "HP 50/25 이하에서는 기본 공격 최종 PG 피해에 취약도 보정이 붙는다")]
    public void ApplyAttack_LowHpIncreasesFinalIncomingPgDamage()
    {
        var attacker = Unit(attackPower: 6, weaponClass: SrpWeaponClass.Melee, stance: SrpStance.Aggressive);
        var halfHp = Unit(hp: 20, maxHp: 40, pg: 30, maxPg: 30);
        var quarterHp = Unit(hp: 10, maxHp: 40, pg: 30, maxPg: 30);

        var half = SrpCombatResolver.ApplyAttack(attacker, halfHp);
        var quarter = SrpCombatResolver.ApplyAttack(attacker, quarterHp);

        Assert.That(half.damageToPg, Is.EqualTo(14));
        Assert.That(half.bonusPgFromLowHpVulnerability, Is.EqualTo(3));
        Assert.That(half.lowHpPgVulnerabilityApplied, Is.True);
        Assert.That(quarter.damageToPg, Is.EqualTo(17));
        Assert.That(quarter.bonusPgFromLowHpVulnerability, Is.EqualTo(6));
    }

    [TestCase(TestName = "HP 취약도는 스킬 피해 경로에도 적용된다")]
    public void ApplySkillReaction_LowHpVulnerabilityAppliesToSkillPgDamage()
    {
        var attacker = Unit(owner: 0);
        var defender = Unit(owner: 1, hp: 20, maxHp: 40, pg: 30, maxPg: 30);
        var skill = new SrpSkillData { id = "test_damage_skill" };
        int hpDamage = 10;
        int pgDamage = 5;

        var outcome = SrpCombatResolver.ApplySkillReaction(null, attacker, defender, skill, ref hpDamage, ref pgDamage);

        Assert.That(pgDamage, Is.EqualTo(7));
        Assert.That(outcome.bonusPgFromLowHpVulnerability, Is.EqualTo(2));
        Assert.That(outcome.lowHpPgVulnerabilityApplied, Is.True);
    }

    [TestCase(TestName = "스킬 HP 취약도는 태그 보정 이후 최종 PG 피해에 적용된다")]
    public void ApplySkillReaction_LowHpVulnerabilityAppliesAfterTagPressure()
    {
        var attacker = Unit(owner: 0);
        var defender = Unit(owner: 1, hp: 20, maxHp: 40, pg: 30, maxPg: 30);
        defender.AddCombatTag(SrpCombatTag.Marked);
        var skill = new SrpSkillData { id = "test_marked_damage_skill" };
        int hpDamage = 10;
        int pgDamage = 5;

        var outcome = SrpCombatResolver.ApplySkillReaction(null, attacker, defender, skill, ref hpDamage, ref pgDamage);

        Assert.That(outcome.bonusPgFromCombatTags, Is.EqualTo(2));
        Assert.That(pgDamage, Is.EqualTo(9));
        Assert.That(outcome.bonusPgFromLowHpVulnerability, Is.EqualTo(2));
    }

    static SrpUnitRuntime Unit(
        int id = 0,
        int owner = 0,
        int x = 0,
        int y = 0,
        int hp = 10,
        int maxHp = 0,
        int pg = 10,
        int maxPg = 10,
        int attackPower = 0,
        int attackRange = 1,
        SrpWeaponClass weaponClass = SrpWeaponClass.Melee,
        SrpStance stance = SrpStance.Aggressive,
        int maxAmmo = 0,
        int ammo = 0)
    {
        return new SrpUnitRuntime
        {
            id = id,
            owner = owner,
            anchorX = x,
            anchorY = y,
            hp = hp,
            maxHp = maxHp > 0 ? maxHp : hp,
            pg = pg,
            maxPg = maxPg,
            attackPower = attackPower,
            attackRange = attackRange,
            weaponClass = weaponClass,
            stance = stance,
            maxAmmo = maxAmmo,
            ammo = ammo,
        };
    }

    sealed class AttackFirstPolicy : ISrpAiPolicy
    {
        public string Name => "attack-first";

        public SrpAiCommand SelectAction(SrpAiDecisionContext ctx)
        {
            return ctx.attacks != null && ctx.attacks.Count > 0
                ? SrpAiCommand.Attack(ctx.attacks[0].targetUnitId)
                : SrpAiCommand.EndTurn();
        }
    }

    sealed class EndTurnPolicy : ISrpAiPolicy
    {
        public string Name => "end-turn";

        public SrpAiCommand SelectAction(SrpAiDecisionContext ctx)
        {
            return SrpAiCommand.EndTurn();
        }
    }

    static SrpBattleState CreateState()
    {
        var walk = new bool[25];
        for (int i = 0; i < walk.Length; i++)
            walk[i] = true;

        return SrpBattleState.FromMap(new SrpMapFileV1
        {
            width = 5,
            height = 5,
            walkable = walk,
            playerOrder = new[] { 0, 1 },
        });
    }
}

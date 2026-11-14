using NUnit.Framework;

[Category("SrpM1All")]
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

    [TestCase(TestName = "회피된 공격은 공격 적중 패시브를 발동하지 않는다")]
    public void OnAttackResolved_DodgedAttackDoesNotTriggerOnHitPassive()
    {
        var state = CreateState();
        var attacker = Unit(id: 1, owner: 0, x: 0, y: 0);
        var defender = Unit(id: 2, owner: 1, x: 3, y: 0);
        attacker.skillIds.Add("hero_adaptive_heart");
        attacker.skillRuntimes.Add(new SrpSkillRuntime("hero_adaptive_heart"));

        SrpSkills.OnAttackResolved(attacker, defender, new SrpCombatResolver.AttackOutcome { wasDodged = true }, state, null);
        Assert.That(attacker.frozenHeart, Is.EqualTo(0), "회피된 공격에 적중 패시브가 발동했습니다.");

        SrpSkills.OnAttackResolved(attacker, defender, new SrpCombatResolver.AttackOutcome { damageToHp = 1 }, state, null);
        Assert.That(attacker.frozenHeart, Is.EqualTo(3), "적중한 공격에 전장 적응 패시브가 발동하지 않았습니다.");
    }

    [TestCase(TestName = "스킬 처치 직후 교전 상태에서 사망 유닛이 빠진다")]
    public void ResolveActiveSkill_KillRebuildsEngagements()
    {
        var state = CreateState();
        var caster = Unit(id: 1, owner: 0, x: 1, y: 1);
        var target = Unit(id: 2, owner: 1, x: 2, y: 1, hp: 5, pg: 10, maxPg: 10);
        state.Units.Add(caster);
        state.Units.Add(target);
        state.RebuildEngagements();
        Assert.That(state.CountEngagingEnemies(caster), Is.EqualTo(1), "테스트 전 교전 상태 구성 실패");
        var skill = DamageSkill(10);

        SrpSkills.ResolveActiveSkill(skill, new SrpSkillRuntime(skill.id), caster, target.anchorX, target.anchorY, state, null);

        Assert.That(target.eliminated, Is.True);
        Assert.That(state.CountEngagingEnemies(caster), Is.EqualTo(0), "사망한 적이 교전 목록에 남아 있습니다.");
        Assert.That(state.IsUnitEngaged(caster.id), Is.False);
    }

    [TestCase(TestName = "스킬 피해도 대상의 피격 패시브를 발동한다")]
    public void ResolveActiveSkill_DamageTriggersTargetOnTakeDamagePassive()
    {
        var state = CreateState();
        var caster = Unit(id: 1, owner: 0, x: 1, y: 1);
        var target = Unit(id: 2, owner: 1, x: 2, y: 1, hp: 40, pg: 20, maxPg: 30);
        target.facing = SrpFacing.West;
        target.skillIds.Add("tank_line_anchor");
        target.skillRuntimes.Add(new SrpSkillRuntime("tank_line_anchor"));
        state.Units.Add(caster);
        state.Units.Add(target);
        var skill = DamageSkill(6);

        SrpSkills.ResolveActiveSkill(skill, new SrpSkillRuntime(skill.id), caster, target.anchorX, target.anchorY, state, null);

        Assert.That(target.hp, Is.EqualTo(34));
        Assert.That(target.pg, Is.EqualTo(19), "스킬 피해 PG-3 뒤 전열 고정 PG+2가 적용되지 않았습니다.");
    }

    [TestCase(TestName = "그로기 유닛은 다음 활성화를 잃고 PG를 회복하며 재붕괴 보호를 받는다")]
    public void GroggyRecovery_ConsumesActivationAndGrantsBreakGuard()
    {
        var unit = Unit(id: 1, hp: 20, pg: 0, maxPg: 20);
        unit.groggy = true;
        unit.actionPoints = 2;
        unit.maxActionPoints = 2;

        bool skipped = SrpTurnOrder.TryConsumeActivationForGroggyRecovery(unit, out int recovered);

        Assert.That(skipped, Is.True, "그로기 유닛의 활성화가 소모되지 않았습니다.");
        Assert.That(unit.groggy, Is.False);
        Assert.That(unit.pg, Is.EqualTo(7), "PG 회복량은 최대 PG의 35% 올림이어야 합니다.");
        Assert.That(recovered, Is.EqualTo(7));
        Assert.That(unit.actionPoints, Is.EqualTo(0));
        Assert.That(unit.pgBreakGuard, Is.True);

        bool skippedAgain = SrpTurnOrder.TryConsumeActivationForGroggyRecovery(unit, out _);

        Assert.That(skippedAgain, Is.False, "회복한 유닛의 다음 활성화는 정상 진행이어야 합니다.");
        Assert.That(unit.pgBreakGuard, Is.False, "재붕괴 보호는 다음 활성화 시작에 풀려야 합니다.");
    }

    [TestCase(TestName = "재붕괴 보호 중에는 PG가 1에서 멈추고 그로기에 빠지지 않는다")]
    public void BreakGuard_KeepsPgAtOneUntilNextActivation()
    {
        var state = CreateState();
        var attacker = Unit(id: 1, owner: 0, x: 1, y: 1, attackPower: 10, weaponClass: SrpWeaponClass.Melee);
        var defender = Unit(id: 2, owner: 1, x: 2, y: 1, hp: 30, pg: 5, maxPg: 20);
        defender.pgBreakGuard = true;
        state.Units.Add(attacker);
        state.Units.Add(defender);

        var guarded = SrpCombatResolver.ApplyAttack(state, attacker, defender);

        Assert.That(defender.pg, Is.EqualTo(1));
        Assert.That(defender.groggy, Is.False);
        Assert.That(guarded.becameGroggy, Is.False);
        Assert.That(guarded.wasExecution, Is.False);
        Assert.That(guarded.pgBreakGuardApplied, Is.True);

        SrpTurnOrder.TryConsumeActivationForGroggyRecovery(defender, out _);
        var unguarded = SrpCombatResolver.ApplyAttack(state, attacker, defender);

        Assert.That(defender.pg, Is.EqualTo(0));
        Assert.That(unguarded.becameGroggy, Is.True, "보호가 풀린 뒤에는 다시 붕괴할 수 있어야 합니다.");
    }

    [TestCase(TestName = "그로기 유닛은 가드 기회공격 경계사격 같은 반응을 하지 못한다")]
    public void GroggyUnit_CannotReact()
    {
        var state = CreateState();
        var groggy = Unit(id: 1, owner: 0, x: 1, y: 1, hp: 30, pg: 0, maxPg: 20, attackPower: 8,
            attackRange: 4, stance: SrpStance.Defensive, maxAmmo: 1, ammo: 1);
        groggy.groggy = true;
        groggy.reactionPoints = 1;
        groggy.maxReactionPoints = 1;
        var enemy = Unit(id: 2, owner: 1, x: 4, y: 1, hp: 30, pg: 20, maxPg: 20, attackPower: 8,
            attackRange: 4, weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1, ammo: 1);
        state.Units.Add(groggy);
        state.Units.Add(enemy);

        var shot = SrpCombatResolver.ApplyAttack(state, enemy, groggy);
        Assert.That(shot.reactionSpentRp, Is.False, "그로기 유닛이 방어 반응을 했습니다.");
        Assert.That(groggy.reactionPoints, Is.EqualTo(1));

        Assert.That(SrpCombatResolver.TryApplyOpportunityAttack(state, groggy, enemy, out _), Is.False,
            "그로기 유닛이 기회공격을 했습니다.");

        groggy.overwatchArmed = true;
        groggy.overwatchRange = 4;
        groggy.overwatchRound = state.RoundNumber;
        Assert.That(SrpOverwatch.CanTrigger(state, groggy, enemy), Is.False, "그로기 유닛이 경계사격을 했습니다.");
    }

    [TestCase(TestName = "적 AI는 인접한 붕괴 대상 처단을 최우선으로 한다")]
    public void EnemyAi_PrefersExecutionOfAdjacentBrokenTarget()
    {
        var state = CreateState();
        var actor = Unit(id: 1, owner: 1, x: 2, y: 2, hp: 30, attackPower: 8);
        actor.actionPoints = 2;
        var healthy = Unit(id: 2, owner: 0, x: 1, y: 2, hp: 5, maxHp: 30, pg: 10, maxPg: 10);
        var broken = Unit(id: 3, owner: 0, x: 3, y: 2, hp: 30, pg: 0, maxPg: 10);
        broken.groggy = true;
        state.Units.Add(actor);
        state.Units.Add(healthy);
        state.Units.Add(broken);

        var action = SrpEnemyAi.Decide(state, actor, 3, false);

        Assert.That(action.type, Is.EqualTo(SrpEnemyAiActionType.Attack));
        Assert.That(action.targetUnitId, Is.EqualTo(broken.id));
    }

    [TestCase(TestName = "적 AI 사격수는 빈 총을 장전한 뒤 사격하고 공격 후에는 다시 장전한다")]
    public void EnemyAi_ShooterReloadsThenFires()
    {
        var state = CreateState();
        var shooter = Unit(id: 1, owner: 1, x: 0, y: 2, hp: 30, attackPower: 8, attackRange: 4,
            weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1, ammo: 0);
        shooter.actionPoints = 2;
        var target = Unit(id: 2, owner: 0, x: 3, y: 2, hp: 30, pg: 10, maxPg: 10);
        state.Units.Add(shooter);
        state.Units.Add(target);

        var first = SrpEnemyAi.Decide(state, shooter, 3, false);
        Assert.That(first.type, Is.EqualTo(SrpEnemyAiActionType.Reload));

        shooter.ReloadAmmo();
        shooter.actionPoints = 1;
        var second = SrpEnemyAi.Decide(state, shooter, 3, false);
        Assert.That(second.type, Is.EqualTo(SrpEnemyAiActionType.Attack));
        Assert.That(second.targetUnitId, Is.EqualTo(target.id));

        shooter.ammo = 0;
        shooter.actionPoints = 1;
        var afterAttack = SrpEnemyAi.Decide(state, shooter, 3, true);
        Assert.That(afterAttack.type, Is.EqualTo(SrpEnemyAiActionType.Reload));
    }

    [TestCase(TestName = "적 AI 근접 유닛은 공격할 수 있는 칸으로 접근하고 판단 중 상태를 바꾸지 않는다")]
    public void EnemyAi_MeleeMovesIntoAttackRangeWithoutMutatingState()
    {
        var state = CreateState();
        var actor = Unit(id: 1, owner: 1, x: 0, y: 0, hp: 30, attackPower: 8, maxAmmo: 1, ammo: 0);
        actor.actionPoints = 2;
        var target = Unit(id: 2, owner: 0, x: 4, y: 0, hp: 30, pg: 10, maxPg: 10);
        state.Units.Add(actor);
        state.Units.Add(target);

        var action = SrpEnemyAi.Decide(state, actor, 4, false);

        Assert.That(action.type, Is.EqualTo(SrpEnemyAiActionType.Move));
        Assert.That(actor.anchorX, Is.EqualTo(0), "판단 과정에서 유닛 위치가 바뀌었습니다.");
        Assert.That(actor.anchorY, Is.EqualTo(0));
        int distanceAfterMove = System.Math.Max(System.Math.Abs(action.x - target.anchorX), System.Math.Abs(action.y - target.anchorY));
        Assert.That(distanceAfterMove, Is.EqualTo(1), "이동 후 근접 공격이 가능한 칸이어야 합니다.");
    }

    [TestCase(TestName = "적 AI는 AP가 1이면 기본 공격보다 피해 스킬을 쓴다")]
    public void EnemyAi_UsesDamageSkillWhenSingleActionLeft()
    {
        var state = CreateState();
        var actor = Unit(id: 1, owner: 1, x: 2, y: 2, hp: 30, attackPower: 8);
        actor.actionPoints = 1;
        actor.skillIds.Add("cleave");
        actor.skillRuntimes.Add(new SrpSkillRuntime("cleave"));
        var target = Unit(id: 2, owner: 0, x: 3, y: 2, hp: 30, pg: 10, maxPg: 10);
        state.Units.Add(actor);
        state.Units.Add(target);

        var action = SrpEnemyAi.Decide(state, actor, 3, false);

        Assert.That(action.type, Is.EqualTo(SrpEnemyAiActionType.UseSkill));
        Assert.That(action.skillId, Is.EqualTo("cleave"));
        Assert.That(action.targetUnitId, Is.EqualTo(target.id));
    }

    static SrpSkillData DamageSkill(int value)
    {
        return new SrpSkillData
        {
            id = "test_direct_damage",
            displayName = "테스트 피해",
            skillType = SrpSkillType.Active,
            trigger = SrpSkillTrigger.OnActivate,
            targetType = SrpTargetType.SingleEnemy,
            range = 1,
            effects = new[]
            {
                new SrpSkillEffect { type = SrpEffectType.Damage, value = value },
            },
        };
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

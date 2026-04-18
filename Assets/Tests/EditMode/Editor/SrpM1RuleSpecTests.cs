using NUnit.Framework;

[Category("SrpM1All")]
public class SrpM1RuleSpecTests
{
    [Test]
    public void ZocPenalty_IncreasesMoveCost_WhenEnemyAdjacent()
    {
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var mover = FindUnit(state, owner: 0, templateId: "mover");
        Assert.IsNotNull(mover, "이동 테스트 유닛 생성 실패");

        var costs = SrpPathfinder.GetReachableWithCosts(state, mover, maxCost: 3);

        // (2,1)은 적(3,1)에 인접해 ZOC 페널티가 붙어 비용 2가 되어야 한다.
        Assert.IsTrue(costs.TryGetValue(new UnityEngine.Vector2Int(2, 1), out int zocCost), "ZOC 칸 비용 계산 실패");
        // (1,2)는 동일 거리(1)지만 ZOC 인접이 아니므로 비용 1이어야 한다.
        Assert.IsTrue(costs.TryGetValue(new UnityEngine.Vector2Int(1, 2), out int normalCost), "일반 칸 비용 계산 실패");

        Assert.AreEqual(1, normalCost, "일반 칸 이동 비용 기준값 불일치");
        Assert.AreEqual(2, zocCost, "ZOC 칸 이동 비용 기준값(1+패널티1) 불일치");
    }

    [Test]
    public void Stance_Aggressive_IncreasesPgPressure()
    {
        var aggressive = new SrpUnitRuntime
        {
            attackPower = 12,
            weaponClass = SrpWeaponClass.Melee,
            stance = SrpStance.Aggressive,
        };
        var neutral = new SrpUnitRuntime
        {
            attackPower = 12,
            weaponClass = SrpWeaponClass.Melee,
            stance = SrpStance.Defensive,
        };

        var targetForAggressive = CreateDefender(SrpStance.Aggressive);
        var targetForNeutral = CreateDefender(SrpStance.Aggressive);

        var a = SrpCombatResolver.ApplyAttack(aggressive, targetForAggressive);
        var b = SrpCombatResolver.ApplyAttack(neutral, targetForNeutral);

        Assert.Greater(a.damageToPg, b.damageToPg, "공격 태세 PG 압박 강화가 반영되지 않음");
    }

    [Test]
    public void Stance_Defensive_ReducesIncomingDamage()
    {
        var attacker = new SrpUnitRuntime
        {
            attackPower = 12,
            weaponClass = SrpWeaponClass.Firearm,
            stance = SrpStance.Aggressive,
        };

        var defensiveTarget = CreateDefender(SrpStance.Defensive);
        var nonDefensiveTarget = CreateDefender(SrpStance.Aggressive);

        var reduced = SrpCombatResolver.ApplyAttack(attacker, defensiveTarget);
        var baseline = SrpCombatResolver.ApplyAttack(attacker, nonDefensiveTarget);

        Assert.Less(reduced.damageToHp, baseline.damageToHp, "수비 태세 HP 피해 감소 미적용");
        Assert.Less(reduced.damageToPg, baseline.damageToPg, "수비 태세 PG 피해 감소 미적용");
    }

    [Test]
    public void Execution_Triggers_WhenDefenderPgZeroOrGroggy()
    {
        var attacker = new SrpUnitRuntime
        {
            attackPower = 10,
            weaponClass = SrpWeaponClass.Melee,
            stance = SrpStance.Aggressive,
        };

        var pgZeroTarget = CreateDefender(SrpStance.Defensive);
        pgZeroTarget.pg = 0;
        var pgZero = SrpCombatResolver.ApplyAttack(attacker, pgZeroTarget);
        Assert.IsTrue(pgZero.wasExecution, "PG 0에서 처단 판정 미발생");
        Assert.AreEqual(16, pgZero.damageToHp, "PG 0 처단 HP 피해값 불일치");
        Assert.AreEqual(0, pgZero.damageToPg, "처단 시 PG 피해는 0이어야 함");

        var groggyTarget = CreateDefender(SrpStance.Defensive);
        groggyTarget.pg = 5;
        groggyTarget.groggy = true;
        var groggy = SrpCombatResolver.ApplyAttack(attacker, groggyTarget);
        Assert.IsTrue(groggy.wasExecution, "그로기 상태에서 처단 판정 미발생");
        Assert.AreEqual(0, groggyTarget.pg, "처단 후 PG는 0으로 정규화되어야 함");
        Assert.IsFalse(groggyTarget.groggy, "처단 처리 후 groggy 상태는 해제되어야 함");
    }

    static SrpMapFileV1 CreateZocTestMap()
    {
        var walk = new bool[25];
        for (int i = 0; i < walk.Length; i++)
            walk[i] = true;

        return new SrpMapFileV1
        {
            version = 2,
            name = "zoc_spec",
            width = 5,
            height = 5,
            walkable = walk,
            playerOrder = new[] { 0, 1 },
            templates = new[]
            {
                new SrpUnitTemplateData
                {
                    id = "mover",
                    displayName = "Mover",
                    moveRange = 3,
                    attackRange = 1,
                    attackPower = 1,
                    maxHp = 30,
                    maxPg = 10,
                    maxActionPoints = 2,
                    maxReactionPoints = 1,
                    speed = 10,
                    weaponClass = SrpWeaponClass.Melee,
                    stance = SrpStance.Aggressive,
                },
                new SrpUnitTemplateData
                {
                    id = "enemy",
                    displayName = "Enemy",
                    moveRange = 3,
                    attackRange = 1,
                    attackPower = 1,
                    maxHp = 30,
                    maxPg = 10,
                    maxActionPoints = 2,
                    maxReactionPoints = 1,
                    speed = 10,
                    weaponClass = SrpWeaponClass.Melee,
                    stance = SrpStance.Aggressive,
                },
            },
            placements = new[]
            {
                new SrpPlacementData { templateId = "mover", owner = 0, x = 1, y = 1 },
                new SrpPlacementData { templateId = "enemy", owner = 1, x = 3, y = 1 },
            },
        };
    }

    static SrpUnitRuntime FindUnit(SrpBattleState state, int owner, string templateId)
    {
        foreach (var unit in state.Units)
        {
            if (!unit.eliminated && unit.owner == owner && unit.templateId == templateId)
                return unit;
        }
        return null;
    }

    static SrpUnitRuntime CreateDefender(SrpStance stance)
    {
        return new SrpUnitRuntime
        {
            hp = 40,
            maxHp = 40,
            pg = 24,
            maxPg = 24,
            stance = stance,
        };
    }
}

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
    public void EngagementExit_IncreasesMoveCost_WhenLeavingEnemyAdjacency()
    {
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var mover = FindUnit(state, owner: 0, templateId: "mover");
        Assert.IsNotNull(mover, "교전 이탈 테스트 유닛 생성 실패");

        mover.anchorX = 2;
        mover.anchorY = 1;
        state.RebuildEngagements();
        Assert.AreEqual(1, state.CountEngagingEnemies(mover), "테스트 전 교전 상태 구성 실패");

        var costs = SrpPathfinder.GetReachableWithCosts(state, mover, maxCost: 3);

        Assert.IsTrue(costs.TryGetValue(new UnityEngine.Vector2Int(1, 1), out int exitCost), "교전 이탈 칸 비용 계산 실패");
        Assert.AreEqual(2, exitCost, "교전 이탈 이동 비용 기준값(기본1+이탈1) 불일치");
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
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var attacker = new SrpUnitRuntime
        {
            anchorX = 2,
            anchorY = 3,
            attackPower = 10,
            weaponClass = SrpWeaponClass.Melee,
            stance = SrpStance.Aggressive,
        };

        var pgZeroTarget = CreateDefender(SrpStance.Defensive);
        pgZeroTarget.anchorX = 2;
        pgZeroTarget.anchorY = 2;
        pgZeroTarget.pg = 0;
        var pgZero = SrpCombatResolver.ApplyAttack(state, attacker, pgZeroTarget);
        Assert.IsTrue(pgZero.wasExecution, "PG 0에서 처단 판정 미발생");
        Assert.IsTrue(pgZero.defenderDied, "PG 0 처단이 확정 사망으로 처리되지 않았습니다.");
        Assert.LessOrEqual(pgZeroTarget.hp, 0, "PG 0 처단 후 대상 HP가 남아 있습니다.");
        Assert.AreEqual(0, pgZero.damageToPg, "처단 시 PG 피해는 0이어야 함");

        var groggyTarget = CreateDefender(SrpStance.Defensive);
        groggyTarget.anchorX = 2;
        groggyTarget.anchorY = 2;
        groggyTarget.pg = 5;
        groggyTarget.groggy = true;
        var groggy = SrpCombatResolver.ApplyAttack(state, attacker, groggyTarget);
        Assert.IsTrue(groggy.wasExecution, "그로기 상태에서 처단 판정 미발생");
        Assert.IsTrue(groggy.defenderDied, "그로기 처단이 확정 사망으로 처리되지 않았습니다.");
        Assert.LessOrEqual(groggyTarget.hp, 0, "그로기 처단 후 대상 HP가 남아 있습니다.");
        Assert.AreEqual(0, groggyTarget.pg, "처단 후 PG는 0으로 정규화되어야 함");
        Assert.IsFalse(groggyTarget.groggy, "처단 처리 후 groggy 상태는 해제되어야 함");
    }

    [Test]
    public void BattleStateClone_CopiesEngagementAndReactionStateIndependently()
    {
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var mover = FindUnit(state, owner: 0, templateId: "mover");
        var enemy = FindUnit(state, owner: 1, templateId: "enemy");
        Assert.IsNotNull(mover);
        Assert.IsNotNull(enemy);

        mover.anchorX = 2;
        mover.anchorY = 1;
        mover.lastReactionKind = SrpReactionKind.Guard;
        mover.lastReactionRound = 2;
        mover.lastReactionSourceId = enemy.id;
        mover.defensiveHitsTakenThisRound = 2;
        mover.defensiveHitsRound = state.RoundNumber;
        state.RebuildEngagements();
        Assert.AreEqual(1, state.CountEngagingEnemies(mover), "테스트 전 교전 상태 구성 실패");

        var clone = state.Clone();
        var clonedMover = FindUnit(clone, owner: 0, templateId: "mover");
        Assert.AreEqual(SrpReactionKind.Guard, clonedMover.lastReactionKind);
        Assert.AreEqual(2, clonedMover.lastReactionRound);
        Assert.AreEqual(enemy.id, clonedMover.lastReactionSourceId);
        Assert.AreEqual(2, clonedMover.defensiveHitsTakenThisRound);
        Assert.AreEqual(state.RoundNumber, clonedMover.defensiveHitsRound);
        Assert.AreEqual(1, clone.CountEngagingEnemies(clonedMover), "클론 교전 상태 복사 실패");

        clone.Engagements[clonedMover.id].Clear();
        clonedMover.lastReactionKind = SrpReactionKind.None;
        clonedMover.defensiveHitsTakenThisRound = 0;

        Assert.AreEqual(1, state.CountEngagingEnemies(mover), "클론 변경이 원본 교전 상태를 오염했습니다.");
        Assert.AreEqual(SrpReactionKind.Guard, mover.lastReactionKind, "클론 변경이 원본 반응 상태를 오염했습니다.");
        Assert.AreEqual(2, mover.defensiveHitsTakenThisRound, "클론 변경이 원본 수비 완충 상태를 오염했습니다.");
    }

    [Test]
    public void DefensiveReaction_ConsumesRpAndReducesIncomingDamage_WhenStateProvided()
    {
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var attacker = FindUnit(state, owner: 1, templateId: "enemy");
        var defender = FindUnit(state, owner: 0, templateId: "mover");
        defender.stance = SrpStance.Defensive;
        defender.reactionPoints = 1;

        var reacted = SrpCombatResolver.ApplyAttack(state, attacker, defender);

        Assert.AreEqual(SrpReactionKind.Guard, reacted.reactionKind, "수비 태세 반응 Guard가 선택되지 않았습니다.");
        Assert.IsTrue(reacted.reactionSpentRp, "반응행동이 RP를 소비하지 않았습니다.");
        Assert.AreEqual(0, defender.reactionPoints, "반응행동 후 RP가 감소하지 않았습니다.");
        Assert.AreEqual(SrpReactionKind.Guard, defender.lastReactionKind, "마지막 반응 상태가 기록되지 않았습니다.");
        Assert.Greater(reacted.reducedHpByDef, 0, "Guard/DEF HP 감쇠가 기록되지 않았습니다.");
        Assert.Greater(reacted.reducedPgByGrd, 0, "Guard/GRD PG 감쇠가 기록되지 않았습니다.");
    }

    [Test]
    public void OpportunityAttack_ConsumesEnemyRpAndDamagesMover_WhenLeavingEngagement()
    {
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var mover = FindUnit(state, owner: 0, templateId: "mover");
        var enemy = FindUnit(state, owner: 1, templateId: "enemy");
        mover.anchorX = 2;
        mover.anchorY = 1;
        enemy.reactionPoints = 1;
        state.RebuildEngagements();
        var previousEngagers = state.GetEngagedEnemyIds(mover.id);
        Assert.Contains(enemy.id, previousEngagers, "테스트 전 교전 상대 기록 실패");

        mover.anchorX = 1;
        mover.anchorY = 1;
        int beforeHp = mover.hp;
        int beforePg = mover.pg;
        state.RebuildEngagements();
        Assert.IsFalse(state.GetEngagedEnemyIds(mover.id).Contains(enemy.id), "테스트 전 교전 이탈 상태 구성 실패");

        bool triggered = SrpCombatResolver.TryApplyOpportunityAttack(state, enemy, mover, out var outcome);

        Assert.IsTrue(triggered, "교전 이탈 기회공격이 발동하지 않았습니다.");
        Assert.AreEqual(0, enemy.reactionPoints, "기회공격 후 적 RP가 감소하지 않았습니다.");
        Assert.AreEqual(SrpReactionKind.ReactionShot, enemy.lastReactionKind, "기회공격 반응 종류가 기록되지 않았습니다.");
        Assert.AreEqual(mover.id, enemy.lastReactionSourceId, "기회공격 출처 대상이 기록되지 않았습니다.");
        Assert.Greater(outcome.damageToHp + outcome.damageToPg, 0, "기회공격 피해가 발생하지 않았습니다.");
        Assert.Less(mover.hp + mover.pg, beforeHp + beforePg, "기회공격 피해가 유닛 상태에 반영되지 않았습니다.");
    }

    [Test]
    public void OpportunityAttack_DoesNotTrigger_WhenEnemyHasNoRp()
    {
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var mover = FindUnit(state, owner: 0, templateId: "mover");
        var enemy = FindUnit(state, owner: 1, templateId: "enemy");
        int beforeHp = mover.hp;
        int beforePg = mover.pg;
        enemy.reactionPoints = 0;

        bool triggered = SrpCombatResolver.TryApplyOpportunityAttack(state, enemy, mover, out var outcome);

        Assert.IsFalse(triggered, "RP가 없는 적이 기회공격을 발동했습니다.");
        Assert.AreEqual(SrpReactionKind.None, enemy.lastReactionKind, "실패한 기회공격이 반응 상태를 오염했습니다.");
        Assert.AreEqual(0, outcome.damageToHp + outcome.damageToPg, "실패한 기회공격이 피해를 기록했습니다.");
        Assert.AreEqual(beforeHp, mover.hp, "실패한 기회공격이 HP를 변경했습니다.");
        Assert.AreEqual(beforePg, mover.pg, "실패한 기회공격이 PG를 변경했습니다.");
    }

    [Test]
    public void ParryCondition_AllowsTaggedFrontMeleeSkillParry_WhenDefenderHasTagAndRp()
    {
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var attacker = FindUnit(state, owner: 1, templateId: "enemy");
        var defender = FindUnit(state, owner: 0, templateId: "mover");
        PlaceParryPair(attacker, defender, attackerX: 2, attackerY: 3, defenderFacing: SrpFacing.North);

        bool canParryBasic = SrpCombatResolver.CanDefenderParry(state, attacker, defender);
        bool canParrySkill = SrpCombatResolver.CanDefenderParry(state, attacker, defender, CreateParryableSkill());

        Assert.IsFalse(canParryBasic, "기본 근접 공격이 패링 가능으로 판정되었습니다.");
        Assert.IsTrue(canParrySkill, "정면 패링 가능 스킬을 패링 가능으로 판정하지 않았습니다.");
    }

    [Test]
    public void ParryCondition_BlocksInvalidThreats()
    {
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var attacker = FindUnit(state, owner: 1, templateId: "enemy");
        var defender = FindUnit(state, owner: 0, templateId: "mover");

        PlaceParryPair(attacker, defender, attackerX: 3, attackerY: 2, defenderFacing: SrpFacing.North);
        Assert.IsFalse(SrpCombatResolver.CanDefenderParry(state, attacker, defender), "측후면 공격이 패링 가능으로 판정되었습니다.");

        PlaceParryPair(attacker, defender, attackerX: 2, attackerY: 3, defenderFacing: SrpFacing.North);
        attacker.weaponClass = SrpWeaponClass.Firearm;
        Assert.IsFalse(SrpCombatResolver.CanDefenderParry(state, attacker, defender), "기본 원거리 공격이 패링 가능으로 판정되었습니다.");

        attacker.weaponClass = SrpWeaponClass.Melee;
        defender.tags = 0;
        Assert.IsFalse(SrpCombatResolver.CanDefenderParry(state, attacker, defender), "패링 가능자 태그가 없는 수비자가 패링 가능으로 판정되었습니다.");

        defender.tags = (int)SrpUnitTags.ParryUser;
        defender.reactionPoints = 0;
        Assert.IsFalse(SrpCombatResolver.CanDefenderParry(state, attacker, defender), "RP 0 수비자가 패링 가능으로 판정되었습니다.");
    }

    [Test]
    public void ParryReaction_ConsumesRpAndNullifiesDamage_WhenTaggedSkillMatches()
    {
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var attacker = FindUnit(state, owner: 1, templateId: "enemy");
        var defender = FindUnit(state, owner: 0, templateId: "mover");
        PlaceParryPair(attacker, defender, attackerX: 2, attackerY: 3, defenderFacing: SrpFacing.North);
        attacker.pg = 20;
        attacker.maxPg = 20;
        int beforeHp = defender.hp;
        int beforePg = defender.pg;

        var outcome = SrpCombatResolver.ApplyAttack(state, attacker, defender, CreateParryableSkill());

        Assert.AreEqual(SrpReactionKind.Parry, outcome.reactionKind, "패링 조건에서 Parry 반응이 선택되지 않았습니다.");
        Assert.IsTrue(outcome.reactionSpentRp, "패링이 RP를 소비하지 않았습니다.");
        Assert.IsTrue(outcome.wasParried, "패링 결과 플래그가 기록되지 않았습니다.");
        Assert.AreEqual(0, defender.reactionPoints, "패링 후 RP가 감소하지 않았습니다.");
        Assert.AreEqual(beforeHp, defender.hp, "패링이 HP 피해를 무효화하지 않았습니다.");
        Assert.AreEqual(beforePg, defender.pg, "패링이 PG 피해를 무효화하지 않았습니다.");
        Assert.AreEqual(12, attacker.pg, "패링 성공 보상 PG 피해가 공격자에게 적용되지 않았습니다.");
        Assert.IsTrue(attacker.HasCombatTag(SrpCombatTag.BalanceBroken), "패링 성공이 공격자에게 균형 붕괴를 부여하지 않았습니다.");
        Assert.AreEqual(8, outcome.parryCounterDamageToPg, "패링 PG 보상 수치가 결과에 기록되지 않았습니다.");
        Assert.IsTrue(outcome.parryAppliedBalanceBreak, "패링 균형 붕괴 보상 플래그가 기록되지 않았습니다.");
    }

    [Test]
    public void CombatTags_RefreshConsumeAndApplyNextAttackPressure()
    {
        var attacker = new SrpUnitRuntime
        {
            owner = 0,
            attackPower = 8,
            weaponClass = SrpWeaponClass.Melee,
            stance = SrpStance.Aggressive,
        };
        var defender = new SrpUnitRuntime
        {
            owner = 1,
            hp = 30,
            maxHp = 30,
            pg = 30,
            maxPg = 30,
            reactionPoints = 0,
        };
        defender.AddCombatTag(SrpCombatTag.Marked);
        defender.AddCombatTag(SrpCombatTag.BalanceBroken);
        defender.AddCombatTag(SrpCombatTag.KillOrder);

        var outcome = SrpCombatResolver.ApplyAttack(null, attacker, defender);

        Assert.IsTrue(outcome.combatTagBonusApplied, "전투 태그 보너스가 적용되지 않았습니다.");
        Assert.AreEqual(SrpCombatTag.Marked | SrpCombatTag.BalanceBroken | SrpCombatTag.KillOrder, outcome.consumedCombatTags);
        Assert.AreEqual(2, outcome.bonusHpFromCombatTags, "사살 지시 HP 보너스가 기록되지 않았습니다.");
        Assert.AreEqual(8, outcome.bonusPgFromCombatTags, "표식/균형 붕괴/사살 지시 PG 보너스가 기록되지 않았습니다.");
        Assert.AreEqual(0, defender.combatTags, "소모형 전투 태그가 다음 공격 후 제거되지 않았습니다.");
        Assert.AreEqual(26, defender.hp, "전투 태그 HP 보너스가 실제 피해에 반영되지 않았습니다.");
        Assert.AreEqual(9, defender.pg, "전투 태그 PG 보너스가 실제 피해에 반영되지 않았습니다.");
    }

    [Test]
    public void DodgeReaction_ConsumesRpAndResolvesByChance_ForAggressiveDefender()
    {
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var attacker = FindUnit(state, owner: 1, templateId: "enemy");
        var defender = FindUnit(state, owner: 0, templateId: "mover");
        attacker.anchorX = 2;
        attacker.anchorY = 4;
        attacker.weaponClass = SrpWeaponClass.Firearm;
        defender.anchorX = 2;
        defender.anchorY = 2;
        defender.stance = SrpStance.Aggressive;
        defender.reactionPoints = 1;
        int beforeHp = defender.hp;
        int beforePg = defender.pg;

        var outcome = SrpCombatResolver.ApplyAttack(state, attacker, defender);

        Assert.AreEqual(SrpReactionKind.Dodge, outcome.reactionKind, "공격 태세 원거리 위협에서 Dodge 반응이 선택되지 않았습니다.");
        Assert.IsTrue(outcome.reactionSpentRp, "Dodge가 RP를 소비하지 않았습니다.");
        Assert.IsTrue(outcome.wasDodged, "Dodge 결과 플래그가 기록되지 않았습니다.");
        Assert.AreEqual(0, defender.reactionPoints, "Dodge 후 RP가 감소하지 않았습니다.");
        Assert.AreEqual(beforeHp, defender.hp, "Dodge가 HP 피해를 무효화하지 않았습니다.");
        Assert.AreEqual(beforePg, defender.pg, "Dodge가 PG 피해를 무효화하지 않았습니다.");
    }

    [Test]
    public void DodgeReaction_FailureKeepsMitigatedDamageWithoutGuardBackup()
    {
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var attacker = FindUnit(state, owner: 1, templateId: "enemy");
        var defender = FindUnit(state, owner: 0, templateId: "mover");
        attacker.anchorX = 40;
        attacker.anchorY = 4;
        attacker.weaponClass = SrpWeaponClass.Firearm;
        defender.anchorX = 2;
        defender.anchorY = 2;
        defender.stance = SrpStance.Aggressive;
        defender.reactionPoints = 1;
        int beforeHp = defender.hp;
        int beforePg = defender.pg;

        var outcome = SrpCombatResolver.ApplyAttack(state, attacker, defender);

        Assert.AreEqual(SrpReactionKind.Dodge, outcome.reactionKind, "공격 태세 원거리 위협에서 Dodge 반응이 선택되지 않았습니다.");
        Assert.IsTrue(outcome.reactionSpentRp, "실패한 Dodge도 RP를 소비해야 합니다.");
        Assert.IsFalse(outcome.wasDodged, "실패한 Dodge가 피해 무효 플래그를 기록했습니다.");
        Assert.IsTrue(outcome.dodgeFailed, "Dodge 실패 플래그가 기록되지 않았습니다.");
        Assert.AreEqual(0, defender.reactionPoints, "Dodge 후 RP가 감소하지 않았습니다.");
        Assert.Less(defender.hp + defender.pg, beforeHp + beforePg, "실패한 Dodge가 기본 감쇠 후 피해를 유지하지 않았습니다.");
    }

    [Test]
    public void DirectionalVulnerability_IncreasesDamage_WhenHitFromBack()
    {
        var frontAttacker = new SrpUnitRuntime
        {
            anchorX = 2,
            anchorY = 3,
            attackPower = 8,
            weaponClass = SrpWeaponClass.Melee,
        };
        var backAttacker = new SrpUnitRuntime
        {
            anchorX = 2,
            anchorY = 1,
            attackPower = 8,
            weaponClass = SrpWeaponClass.Melee,
        };
        var frontTarget = CreateDefender(SrpStance.Defensive);
        var backTarget = CreateDefender(SrpStance.Defensive);
        frontTarget.anchorX = 2;
        frontTarget.anchorY = 2;
        frontTarget.facing = SrpFacing.North;
        frontTarget.reactionPoints = 0;
        backTarget.anchorX = 2;
        backTarget.anchorY = 2;
        backTarget.facing = SrpFacing.North;
        backTarget.reactionPoints = 0;

        var front = SrpCombatResolver.ApplyAttack(frontAttacker, frontTarget);
        var back = SrpCombatResolver.ApplyAttack(backAttacker, backTarget);

        Assert.Greater(back.damageToHp + back.damageToPg, front.damageToHp + front.damageToPg, "후면 피격 방어 불리 브릿지가 피해에 반영되지 않았습니다.");
    }

    [Test]
    public void Overwatch_ArmCloneAndTrigger_UsesApReservationAndRpReactionShot()
    {
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var watcher = FindUnit(state, owner: 0, templateId: "mover");
        var target = FindUnit(state, owner: 1, templateId: "enemy");
        watcher.weaponClass = SrpWeaponClass.Firearm;
        watcher.attackRange = 3;
        watcher.attackPower = 8;
        watcher.actionPoints = 2;
        watcher.reactionPoints = 1;
        target.reactionPoints = 0;
        int beforeTargetHp = target.hp;

        Assert.IsTrue(SrpOverwatch.Arm(state, watcher), "오버워치 예약 실패");
        Assert.AreEqual(1, watcher.actionPoints, "오버워치 예약이 AP를 소비하지 않았습니다.");
        Assert.IsTrue(watcher.overwatchArmed, "오버워치 예약 상태가 기록되지 않았습니다.");
        Assert.AreEqual(3, watcher.overwatchRange, "오버워치 사거리 기록이 불일치합니다.");

        var clone = state.Clone();
        var clonedWatcher = FindUnit(clone, owner: 0, templateId: "mover");
        Assert.IsTrue(clonedWatcher.overwatchArmed, "클론에 오버워치 예약 상태가 복사되지 않았습니다.");
        clonedWatcher.overwatchArmed = false;
        Assert.IsTrue(watcher.overwatchArmed, "클론 변경이 원본 오버워치 예약 상태를 오염했습니다.");

        Assert.IsTrue(SrpOverwatch.TryTrigger(state, watcher, target, out var outcome), "오버워치 ReactionShot이 발동하지 않았습니다.");
        Assert.IsFalse(watcher.overwatchArmed, "오버워치 발동 후 예약 상태가 해제되지 않았습니다.");
        Assert.AreEqual(0, watcher.reactionPoints, "오버워치 발동이 RP를 소비하지 않았습니다.");
        Assert.AreEqual(SrpReactionKind.ReactionShot, watcher.lastReactionKind, "오버워치 발동 반응 종류가 기록되지 않았습니다.");
        Assert.AreEqual(target.id, watcher.lastReactionSourceId, "오버워치 발동 대상이 기록되지 않았습니다.");
        Assert.Greater(beforeTargetHp, target.hp, "오버워치 피해가 대상 HP에 반영되지 않았습니다.");
        Assert.Greater(outcome.damageToHp + outcome.damageToPg, 0, "오버워치 결과 피해가 기록되지 않았습니다.");
    }

    [Test]
    public void Overwatch_MeleeRoleSidearm_BlocksAdjacentAndTriggersFirearmAtRange()
    {
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var watcher = FindUnit(state, owner: 0, templateId: "mover");
        var target = FindUnit(state, owner: 1, templateId: "enemy");
        watcher.weaponClass = SrpWeaponClass.Melee;
        watcher.attackRange = 4;
        watcher.attackPower = 8;
        watcher.actionPoints = 2;
        watcher.reactionPoints = 1;
        watcher.maxAmmo = 2;
        watcher.ammo = 2;
        target.anchorX = watcher.anchorX + 1;
        target.anchorY = watcher.anchorY;
        target.reactionPoints = 0;
        int hpBefore = target.hp;
        int pgBefore = target.pg;

        Assert.AreEqual(SrpOverwatchArmStatus.Ready, SrpOverwatch.GetArmStatus(watcher));
        Assert.IsTrue(SrpOverwatch.Arm(state, watcher), "Melee role with sidearm ammo/range should be able to reserve overwatch.");
        Assert.IsFalse(SrpOverwatch.CanTrigger(state, watcher, target), "overwatch firearm reaction must not trigger against adjacent targets.");
        Assert.IsFalse(SrpOverwatch.TryTrigger(state, watcher, target, out _), "adjacent targets must not consume a reserved overwatch shot.");
        Assert.IsTrue(watcher.overwatchArmed, "failed adjacent trigger should keep the reservation intact.");
        Assert.AreEqual(2, watcher.ammo, "failed adjacent trigger should not consume ammo.");

        target.anchorX = watcher.anchorX + 2;
        target.anchorY = watcher.anchorY;
        Assert.IsTrue(SrpOverwatch.CanTrigger(state, watcher, target), "non-adjacent target with LOS/range/ammo should trigger overwatch.");
        Assert.IsTrue(SrpOverwatch.TryTrigger(state, watcher, target, out var outcome), "non-adjacent sidearm overwatch should fire.");

        Assert.AreEqual(SrpBasicAttackKind.Firearm, outcome.basicAttackKind, "overwatch result should use the firearm damage model at range.");
        Assert.Greater(hpBefore, target.hp, "firearm overwatch should damage HP.");
        Assert.Greater(pgBefore, target.pg, "firearm overwatch should apply PG spillover.");
        Assert.Greater(outcome.firearmPgSpillover, 0, "firearm overwatch should record firearm PG spillover.");
        Assert.AreEqual(1, watcher.ammo, "successful overwatch should consume exactly one ammo.");
        Assert.AreEqual(SrpReactionKind.ReactionShot, watcher.lastReactionKind);
    }

    [Test]
    public void Overwatch_CanTrigger_AllowsVectorAimOutsideAxisOrDiagonalLine()
    {
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var watcher = FindUnit(state, owner: 0, templateId: "mover");
        var target = FindUnit(state, owner: 1, templateId: "enemy");
        watcher.weaponClass = SrpWeaponClass.Firearm;
        watcher.attackRange = 4;
        watcher.actionPoints = 2;
        watcher.reactionPoints = 1;
        target.anchorX = 3;
        target.anchorY = 2;
        target.reactionPoints = 0;

        Assert.IsTrue(SrpOverwatch.Arm(state, watcher), "오버워치 예약 실패");

        Assert.IsTrue(SrpOverwatch.CanTrigger(state, watcher, target), "비8방향 목표가 사거리/LOS를 통과했는데 오버워치가 발동 불가로 판정되었습니다.");

        target.anchorX = 3;
        target.anchorY = 1;
        Assert.IsTrue(SrpOverwatch.CanTrigger(state, watcher, target), "직선 목표도 오버워치 발동 가능해야 합니다.");
    }

    [Test]
    public void FirearmAim_HelperIsSharedByBasicAttackAndOverwatch()
    {
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var shooter = FindUnit(state, owner: 0, templateId: "mover");
        var target = FindUnit(state, owner: 1, templateId: "enemy");
        shooter.weaponClass = SrpWeaponClass.Firearm;
        shooter.attackRange = 4;
        shooter.attackPower = 8;
        shooter.actionPoints = 2;
        shooter.reactionPoints = 1;
        shooter.maxAmmo = 1;
        shooter.ammo = 1;
        shooter.facing = SrpFacing.North;
        target.anchorX = 3;
        target.anchorY = 2;
        target.reactionPoints = 0;

        Assert.IsTrue(SrpFirearmAim.CanBasicAttack(state, shooter, target, out var aim), "clear non-8-direction firearm aim should be valid for basic attack");
        Assert.IsTrue(SrpCombatResolver.CanAttack(state, shooter, target), "basic firearm attack should use vector LOS targetability");
        Assert.AreEqual(SrpAimSector8.NorthEast, aim.sector8, "non-8-direction aim should still expose an atan2-based 8-sector display value");
        Assert.AreEqual(SrpFacing.East, aim.facing, "basic firearm aim should face the dominant target vector");
        Assert.IsTrue(SrpOverwatch.Arm(state, shooter), "overwatch reservation setup failed");
        Assert.IsTrue(SrpOverwatch.CanTrigger(state, shooter, target), "overwatch should share the same vector aim targetability as basic firearm attack");

        Assert.IsTrue(SrpFirearmAim.TurnShooterTowardTarget(shooter, target), "facing helper failed to turn firearm shooter");
        Assert.AreEqual(SrpFacing.East, shooter.facing, "firearm facing did not update toward the target vector");
    }

    [Test]
    public void FirearmBasicAttack_NonEightDirectionAimStillRespectsBlockers()
    {
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var shooter = FindUnit(state, owner: 0, templateId: "mover");
        var target = FindUnit(state, owner: 1, templateId: "enemy");
        shooter.weaponClass = SrpWeaponClass.Firearm;
        shooter.attackRange = 4;
        target.anchorX = 3;
        target.anchorY = 2;

        state.Units.Add(CreateExtraEnemy(id: 100, x: 2, y: 1));
        Assert.IsFalse(SrpFirearmAim.CanBasicAttack(state, shooter, target, out _), "intermediate unit should block non-8-direction basic firearm aim");
        state.Units.RemoveAt(state.Units.Count - 1);

        state.CoverSegments.Add(new SrpCoverSegmentData
        {
            x = 2,
            y = 1,
            edge = SrpCoverEdge.West,
            shape = SrpCoverShape.Linear,
            blocksLineOfSight = true,
        });
        Assert.IsFalse(SrpFirearmAim.CanBasicAttack(state, shooter, target, out _), "blocking cover segment should block non-8-direction basic firearm aim");
    }

    [Test]
    public void Overwatch_CanTrigger_BlocksWhenLineOfSightIsObstructed()
    {
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var watcher = FindUnit(state, owner: 0, templateId: "mover");
        var target = FindUnit(state, owner: 1, templateId: "enemy");
        var blocker = CreateExtraEnemy(id: 100, x: 2, y: 1);
        watcher.weaponClass = SrpWeaponClass.Firearm;
        watcher.attackRange = 4;
        watcher.actionPoints = 2;
        watcher.reactionPoints = 1;
        target.anchorX = 4;
        target.anchorY = 1;
        target.reactionPoints = 0;
        state.Units.Add(blocker);

        Assert.IsTrue(SrpOverwatch.Arm(state, watcher), "오버워치 예약 실패");

        Assert.IsFalse(SrpOverwatch.CanTrigger(state, watcher, target), "중간 유닛이 사선을 막는데 오버워치가 발동 가능으로 판정되었습니다.");
        Assert.IsFalse(SrpOverwatch.TryTrigger(state, watcher, target, out var outcome), "차단된 사선에서 오버워치가 발동했습니다.");
        Assert.AreEqual(0, outcome.damageToHp + outcome.damageToPg, "차단된 오버워치가 피해를 기록했습니다.");

        state.Units.Remove(blocker);
        state.Walkable[state.Index(2, 1)] = false;
        Assert.IsFalse(SrpOverwatch.CanTrigger(state, watcher, target), "장애물 타일이 사선을 막는데 오버워치가 발동 가능으로 판정되었습니다.");
    }

    [Test]
    public void LineOfSight_CoverSegmentBlocksOverwatchAndFirearmBasicAttack()
    {
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var watcher = FindUnit(state, owner: 0, templateId: "mover");
        var target = FindUnit(state, owner: 1, templateId: "enemy");
        watcher.weaponClass = SrpWeaponClass.Firearm;
        watcher.attackRange = 4;
        watcher.actionPoints = 2;
        watcher.reactionPoints = 1;
        watcher.maxAmmo = 1;
        watcher.ammo = 1;
        target.reactionPoints = 0;
        state.CoverSegments.Add(new SrpCoverSegmentData
        {
            x = target.anchorX,
            y = target.anchorY,
            edge = SrpCoverEdge.West,
            shape = SrpCoverShape.Linear,
            blocksLineOfSight = true,
        });

        Assert.IsTrue(SrpOverwatch.Arm(state, watcher), "오버워치 예약 실패");
        Assert.IsFalse(SrpOverwatch.CanTrigger(state, watcher, target), "blocksLineOfSight segment가 오버워치 사선을 차단하지 않았습니다.");
        Assert.IsFalse(SrpCombatResolver.CanAttack(state, watcher, target), "blocksLineOfSight segment가 총기 기본 공격 사선을 차단하지 않았습니다.");

        state.CoverSegments[0].blocksLineOfSight = false;
        Assert.IsTrue(SrpOverwatch.CanTrigger(state, watcher, target), "사선 차단이 꺼진 segment가 오버워치를 막았습니다.");
        Assert.IsTrue(SrpCombatResolver.CanAttack(state, watcher, target), "사선 차단이 꺼진 segment가 총기 기본 공격을 막았습니다.");
    }

    [Test]
    public void Overwatch_SelectTriggerWatcher_UsesDistanceSpeedAndIdPriority()
    {
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var slow = FindUnit(state, owner: 0, templateId: "mover");
        var target = FindUnit(state, owner: 1, templateId: "enemy");
        slow.weaponClass = SrpWeaponClass.Firearm;
        slow.attackRange = 4;
        slow.actionPoints = 2;
        slow.reactionPoints = 1;
        slow.speed = 10;
        var fast = new SrpUnitRuntime
        {
            id = 200,
            templateId = "fast_watcher",
            displayName = "Fast Watcher",
            owner = 0,
            anchorX = 3,
            anchorY = 3,
            hp = 30,
            maxHp = 30,
            pg = 10,
            maxPg = 10,
            actionPoints = 2,
            maxActionPoints = 2,
            reactionPoints = 1,
            maxReactionPoints = 1,
            speed = 20,
            weaponClass = SrpWeaponClass.Firearm,
            attackRange = 4,
            attackPower = 8,
            maxAmmo = 1,
            ammo = 1,
        };
        state.Units.Add(fast);

        Assert.IsTrue(SrpOverwatch.Arm(state, slow), "첫 번째 오버워치 예약 실패");
        Assert.IsTrue(SrpOverwatch.Arm(state, fast), "두 번째 오버워치 예약 실패");

        var selected = SrpOverwatch.SelectTriggerWatcher(state, target);

        Assert.AreEqual(fast.id, selected.id, "동일 거리 오버워치 후보 중 더 빠른 유닛이 우선되지 않았습니다.");
    }

    [Test]
    public void Overwatch_ArmStatus_ExplainsHudPolicyConditions()
    {
        var unit = CreateOverwatchReadyUnit();

        Assert.AreEqual(SrpOverwatchArmStatus.Ready, SrpOverwatch.GetArmStatus(unit));
        Assert.IsTrue(SrpOverwatch.CanArm(unit), "Ready 상태는 예약 가능해야 합니다.");

        unit.overwatchArmed = true;
        Assert.AreEqual(SrpOverwatchArmStatus.AlreadyArmed, SrpOverwatch.GetArmStatus(unit));
        unit.overwatchArmed = false;

        unit.actionPoints = 0;
        Assert.AreEqual(SrpOverwatchArmStatus.NoAction, SrpOverwatch.GetArmStatus(unit));
        unit.actionPoints = 1;

        unit.reactionPoints = 0;
        Assert.AreEqual(SrpOverwatchArmStatus.NoReaction, SrpOverwatch.GetArmStatus(unit));
        unit.reactionPoints = 1;

        unit.weaponClass = SrpWeaponClass.Melee;
        Assert.AreEqual(SrpOverwatchArmStatus.Ready, SrpOverwatch.GetArmStatus(unit), "Melee role units with sidearm ammo/range should still arm overwatch.");

        unit.ammo = 0;
        Assert.AreEqual(SrpOverwatchArmStatus.NoAmmo, SrpOverwatch.GetArmStatus(unit));
        unit.ammo = 1;

        unit.attackRange = 1;
        Assert.AreEqual(SrpOverwatchArmStatus.RangeTooShort, SrpOverwatch.GetArmStatus(unit));
    }

    [Test]
    public void Overwatch_ArmStatus_TracksReservationAndRoundReset()
    {
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var watcher = FindUnit(state, owner: 0, templateId: "mover");
        watcher.weaponClass = SrpWeaponClass.Firearm;
        watcher.attackRange = 3;
        watcher.actionPoints = 2;
        watcher.reactionPoints = 1;

        Assert.AreEqual(SrpOverwatchArmStatus.Ready, SrpOverwatch.GetArmStatus(watcher));
        Assert.IsTrue(SrpOverwatch.Arm(state, watcher));
        Assert.AreEqual(SrpOverwatchArmStatus.AlreadyArmed, SrpOverwatch.GetArmStatus(watcher));

        SrpTurnOrder.ResetRoundResources(state);

        Assert.IsFalse(watcher.overwatchArmed, "라운드 리셋 후 오버워치 예약이 해제되지 않았습니다.");
        Assert.AreEqual(SrpReactionKind.None, watcher.lastReactionKind, "라운드 리셋 후 반응 상태가 초기화되지 않았습니다.");
        Assert.AreEqual(SrpOverwatchArmStatus.Ready, SrpOverwatch.GetArmStatus(watcher), "라운드 리셋 후 예약 가능 상태로 돌아오지 않았습니다.");
    }

    [Test]
    public void SustainedDefenseBuffer_AppliesOnFollowUpHit_WhenDefensiveAndEngaged()
    {
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var defender = FindUnit(state, owner: 0, templateId: "mover");
        var attacker = FindUnit(state, owner: 1, templateId: "enemy");
        defender.anchorX = 2;
        defender.anchorY = 1;
        defender.stance = SrpStance.Defensive;
        defender.reactionPoints = 0;
        defender.hp = 50;
        defender.maxHp = 50;
        defender.pg = 50;
        defender.maxPg = 50;
        attacker.attackPower = 8;
        state.RebuildEngagements();

        var first = SrpCombatResolver.ApplyAttack(state, attacker, defender);
        var second = SrpCombatResolver.ApplyAttack(state, attacker, defender);

        Assert.IsFalse(first.sustainedDefenseBufferApplied, "첫 피격에 후속 수비 완충이 적용되었습니다.");
        Assert.IsTrue(second.sustainedDefenseBufferApplied, "후속 피격에 수비 완충이 적용되지 않았습니다.");
        Assert.Greater(second.reducedHpBySustainedDefense + second.reducedPgBySustainedDefense, 0, "후속 수비 완충 감쇠량이 기록되지 않았습니다.");
        Assert.AreEqual(2, defender.defensiveHitsTakenThisRound, "수비 피격 누적이 라운드 내에서 증가하지 않았습니다.");
    }

    [Test]
    public void TankMultiEngagementBuffer_AppliesOnlyForTank_WhenEngagedByMultipleEnemies()
    {
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var tank = FindUnit(state, owner: 0, templateId: "mover");
        var attacker = FindUnit(state, owner: 1, templateId: "enemy");
        var secondEnemy = CreateExtraEnemy(id: 99, x: 2, y: 2);
        state.Units.Add(secondEnemy);
        tank.anchorX = 2;
        tank.anchorY = 1;
        tank.stance = SrpStance.Defensive;
        tank.tags = (int)SrpUnitTags.Tank;
        tank.reactionPoints = 0;
        attacker.attackPower = 8;
        state.RebuildEngagements();
        Assert.AreEqual(2, state.CountEngagingEnemies(tank), "테스트 전 다중 교전 상태 구성 실패");

        var tanked = SrpCombatResolver.ApplyAttack(state, attacker, tank);

        Assert.IsTrue(tanked.tankMultiEngagementBufferApplied, "탱커 다중 대응 완충이 적용되지 않았습니다.");
        Assert.Greater(tanked.reducedHpByTank + tanked.reducedPgByTank, 0, "탱커 다중 대응 감쇠량이 기록되지 않았습니다.");

        tank.hp = tank.maxHp;
        tank.pg = tank.maxPg;
        tank.tags = 0;
        tank.defensiveHitsTakenThisRound = 0;
        tank.defensiveHitsRound = state.RoundNumber;
        var normal = SrpCombatResolver.ApplyAttack(state, attacker, tank);

        Assert.IsFalse(normal.tankMultiEngagementBufferApplied, "일반 유닛에 탱커 전용 완충이 적용되었습니다.");
    }

    [Test]
    public void PerfectDefense_NullsMinorHpDamage_ButNotMajorOrBackAttack()
    {
        var state = SrpBattleState.FromMap(CreateZocTestMap());
        var tank = FindUnit(state, owner: 0, templateId: "mover");
        var attacker = FindUnit(state, owner: 1, templateId: "enemy");

        tank.anchorX = 2;
        tank.anchorY = 2;
        tank.facing = SrpFacing.North;
        tank.stance = SrpStance.Defensive;
        tank.tags = (int)SrpUnitTags.Tank;
        tank.reactionPoints = 0;
        tank.hp = 30;
        tank.pg = 10;
        attacker.anchorX = 2;
        attacker.anchorY = 3;
        attacker.weaponClass = SrpWeaponClass.Melee;
        attacker.attackPower = 8;

        var minor = SrpCombatResolver.ApplyAttack(state, attacker, tank);

        Assert.IsTrue(minor.perfectDefenseApplied, "정면 경미 HP 피해에 완벽한 수비가 적용되지 않았습니다.");
        Assert.AreEqual(0, minor.damageToHp, "완벽한 수비가 경미 HP 피해를 무효화하지 않았습니다.");
        Assert.Greater(minor.damageToPg, 0, "완벽한 수비가 PG 압박까지 지워서는 안 됩니다.");

        tank.hp = 30;
        tank.pg = 10;
        tank.defensiveHitsRound = 0;
        tank.defensiveHitsTakenThisRound = 0;
        attacker.weaponClass = SrpWeaponClass.Firearm;
        attacker.attackRange = 4;
        attacker.anchorY = 4;
        var major = SrpCombatResolver.ApplyAttack(state, attacker, tank);
        Assert.IsFalse(major.perfectDefenseApplied, "총격 중대 HP 피해가 완벽한 수비로 무효화되었습니다.");
        Assert.Greater(major.damageToHp, 0, "총격 HP 피해가 사라졌습니다.");

        tank.hp = 30;
        tank.pg = 10;
        tank.defensiveHitsRound = 0;
        tank.defensiveHitsTakenThisRound = 0;
        attacker.weaponClass = SrpWeaponClass.Melee;
        attacker.attackRange = 1;
        attacker.anchorX = 2;
        attacker.anchorY = 1;
        var back = SrpCombatResolver.ApplyAttack(state, attacker, tank);
        Assert.IsFalse(back.perfectDefenseApplied, "후방 경미 HP 피해가 완벽한 수비로 무효화되었습니다.");
        Assert.Greater(back.damageToHp, 0, "후방 HP 피해가 사라졌습니다.");
    }

    [Test]
    public void EngagementLabPreset_StartsWithTankInMultiEngagement()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.GetPreset(SrpMapPreset.M1EngagementLab));
        var tank = FindUnit(state, owner: 0, templateId: "engage_tank");

        Assert.IsNotNull(tank, "교전 랩 탱커가 배치되지 않았습니다.");
        Assert.IsTrue(tank.HasTag(SrpUnitTags.Tank), "교전 랩 탱커에 Tank 태그가 없습니다.");
        Assert.AreEqual(SrpStance.Defensive, tank.stance, "교전 랩 탱커가 수비 태세가 아닙니다.");
        Assert.AreEqual(2, state.CountEngagingEnemies(tank), "교전 랩 탱커가 2명에게 둘러싸여 시작하지 않습니다.");
    }

    [Test]
    public void EngagementLabPreset_DisengageMoveHasExitCostAndOpportunityAttack()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.GetPreset(SrpMapPreset.M1EngagementLab));
        var tank = FindUnit(state, owner: 0, templateId: "engage_tank");
        var raider = FindUnit(state, owner: 1, templateId: "engage_raider");
        tank.reactionPoints = 0;
        raider.reactionPoints = 1;
        state.RebuildEngagements();

        var costs = SrpPathfinder.GetReachableWithCosts(state, tank, tank.moveRange);
        var escapeTile = new UnityEngine.Vector2Int(2, 1);

        Assert.IsTrue(costs.TryGetValue(escapeTile, out int escapeCost), "교전 랩 이탈 타일이 도달 가능하지 않습니다.");
        Assert.Greater(escapeCost, 2, "교전 이탈/포지셔닝 비용이 프리셋 이동 비용에 반영되지 않았습니다.");

        var previousEngagers = state.GetEngagedEnemyIds(tank.id);
        Assert.Contains(raider.id, previousEngagers, "교전 랩 기회공격 후보가 기록되지 않았습니다.");
        tank.anchorX = escapeTile.x;
        tank.anchorY = escapeTile.y;
        state.RebuildEngagements();
        Assert.IsFalse(state.IsUnitEngaged(tank.id), "교전 랩 이탈 타일에서 여전히 교전 중입니다.");

        int beforeHp = tank.hp;
        bool triggered = SrpCombatResolver.TryApplyOpportunityAttack(state, raider, tank, out var outcome);

        Assert.IsTrue(triggered, "교전 랩 이탈 기회공격이 발동하지 않았습니다.");
        Assert.AreEqual(0, raider.reactionPoints, "교전 랩 기회공격이 RP를 소비하지 않았습니다.");
        Assert.Greater(outcome.damageToHp + outcome.damageToPg, 0, "교전 랩 기회공격 피해가 기록되지 않았습니다.");
        Assert.Less(tank.hp, beforeHp, "교전 랩 기회공격 HP 피해가 반영되지 않았습니다.");
    }

    [Test]
    public void EngagementLabPreset_AppliesTankAndSustainedDefenseBuffers()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.GetPreset(SrpMapPreset.M1EngagementLab));
        var tank = FindUnit(state, owner: 0, templateId: "engage_tank");
        var raider = FindUnit(state, owner: 1, templateId: "engage_raider");
        tank.reactionPoints = 0;
        raider.attackPower = 8;
        state.RebuildEngagements();

        var first = SrpCombatResolver.ApplyAttack(state, raider, tank);
        var second = SrpCombatResolver.ApplyAttack(state, raider, tank);

        Assert.IsTrue(first.tankMultiEngagementBufferApplied, "교전 랩 첫 피격에 탱커 다중 대응이 적용되지 않았습니다.");
        Assert.IsFalse(first.sustainedDefenseBufferApplied, "교전 랩 첫 피격에 후속 수비 완충이 적용되었습니다.");
        Assert.IsTrue(second.tankMultiEngagementBufferApplied, "교전 랩 후속 피격에 탱커 다중 대응이 유지되지 않았습니다.");
        Assert.IsTrue(second.sustainedDefenseBufferApplied, "교전 랩 후속 피격에 수비 지속 완충이 적용되지 않았습니다.");
        Assert.AreEqual(2, tank.defensiveHitsTakenThisRound, "교전 랩 수비 피격 누적이 증가하지 않았습니다.");
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

    static void PlaceParryPair(SrpUnitRuntime attacker, SrpUnitRuntime defender, int attackerX, int attackerY, SrpFacing defenderFacing)
    {
        attacker.anchorX = attackerX;
        attacker.anchorY = attackerY;
        attacker.weaponClass = SrpWeaponClass.Melee;
        attacker.owner = 1;
        defender.anchorX = 2;
        defender.anchorY = 2;
        defender.owner = 0;
        defender.facing = defenderFacing;
        defender.tags = (int)SrpUnitTags.ParryUser;
        defender.reactionPoints = 1;
    }

    static SrpSkillData CreateParryableSkill()
    {
        return new SrpSkillData
        {
            id = "test_parryable",
            displayName = "Test Parryable",
            isParryable = true,
            requiresParryTelegraph = true,
        };
    }

    static SrpUnitRuntime CreateExtraEnemy(int id, int x, int y)
    {
        return new SrpUnitRuntime
        {
            id = id,
            templateId = "enemy_extra",
            displayName = "EnemyExtra",
            owner = 1,
            anchorX = x,
            anchorY = y,
            footprintOffsets = new System.Collections.Generic.List<UnityEngine.Vector2Int> { UnityEngine.Vector2Int.zero },
            hp = 30,
            maxHp = 30,
            pg = 10,
            maxPg = 10,
            actionPoints = 2,
            maxActionPoints = 2,
            reactionPoints = 1,
            maxReactionPoints = 1,
            speed = 10,
            weaponClass = SrpWeaponClass.Melee,
            stance = SrpStance.Aggressive,
            moveRange = 3,
            attackRange = 1,
            attackPower = 8,
        };
    }

    static SrpUnitRuntime CreateOverwatchReadyUnit()
    {
        return new SrpUnitRuntime
        {
            id = 900,
            displayName = "OverwatchReady",
            owner = 0,
            hp = 30,
            maxHp = 30,
            pg = 10,
            maxPg = 10,
            actionPoints = 1,
            maxActionPoints = 2,
            reactionPoints = 1,
            maxReactionPoints = 1,
            weaponClass = SrpWeaponClass.Firearm,
            attackRange = 3,
            maxAmmo = 1,
            ammo = 1,
        };
    }
}

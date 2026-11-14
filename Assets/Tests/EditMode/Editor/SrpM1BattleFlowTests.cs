using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// 런타임과 AI 시뮬레이션이 함께 쓰는 전투 진행 로직(SrpBattleFlow).
/// 근거: 진행 계획 3차 "런타임과 시뮬레이션이 같은 흐름(재장전, 스킬, 경계태세, 기회공격 포함)을 타게 한다".
/// </summary>
[Category("SrpM1All")]
public class SrpM1BattleFlowTests
{
    [TestCase(TestName = "이동으로 교전에서 벗어나면 AP 1을 쓰고 직전 교전 상대에게 기회공격을 받는다")]
    public void Move_DisengageTriggersOpportunityAttack()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.GetPreset(SrpMapPreset.M1EngagementLab));
        SrpBattleFlow.StartRound(state);
        var tank = FindUnit(state, "engage_tank");
        var raider = FindUnit(state, "engage_raider");
        var flanker = FindUnit(state, "engage_flanker");
        flanker.reactionPoints = 0;
        tank.reactionPoints = 0;
        int hpBefore = tank.hp;
        int pgBefore = tank.pg;
        var observer = new RecordingObserver();

        var costs = SrpPathfinder.GetReachableWithCosts(state, tank, tank.moveRange);
        Assert.That(costs.TryGetValue(new UnityEngine.Vector2Int(2, 1), out int cost), Is.True);
        var result = SrpBattleFlow.Move(state, tank, 2, 1, cost, observer);

        Assert.That(result.disengaged, Is.True);
        Assert.That(result.opportunity.triggered, Is.True, "기회공격이 발동하지 않았습니다.");
        Assert.That(result.opportunity.reactor, Is.SameAs(raider));
        Assert.That(raider.reactionPoints, Is.EqualTo(0), "기회공격이 RP를 쓰지 않았습니다.");
        Assert.That(tank.actionPoints, Is.EqualTo(tank.maxActionPoints - 1));
        Assert.That(tank.hasMovedThisActivation, Is.True);
        Assert.That((hpBefore - tank.hp) + (pgBefore - tank.pg), Is.GreaterThan(0), "기회공격 피해가 없습니다.");
        Assert.That(observer.events[0], Is.EqualTo("moved"), "이동 기록보다 반응이 먼저 통지됐습니다.");
        Assert.That(observer.events, Does.Contain("attack:Opportunity"));
    }

    [TestCase(TestName = "경계태세를 예약한 적의 사선으로 이동하면 경계사격을 받고 예약이 풀린다")]
    public void Move_IntoArmedOverwatchTriggersReactionShot()
    {
        var state = CreateOpenState();
        var mover = AddUnit(state, id: 1, owner: 0, x: 0, y: 0, hp: 30);
        var watcher = AddUnit(state, id: 2, owner: 1, x: 4, y: 2, hp: 30, attackPower: 8, attackRange: 4,
            weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1);
        SrpBattleFlow.StartRound(state);
        mover.reactionPoints = 0;
        Assert.That(SrpOverwatch.Arm(state, watcher), Is.True);

        var result = SrpBattleFlow.Move(state, mover, 0, 2, 2, null);

        Assert.That(result.opportunity.triggered, Is.False);
        Assert.That(result.overwatch.triggered, Is.True, "경계사격이 발동하지 않았습니다.");
        Assert.That(result.overwatch.reactor, Is.SameAs(watcher));
        Assert.That(watcher.overwatchArmed, Is.False);
        Assert.That(watcher.ammo, Is.EqualTo(0));
        Assert.That(mover.hp, Is.LessThan(30));
    }

    [TestCase(TestName = "경계사격으로 이동한 유닛이 쓰러지면 전장에서 빠지고 활성화가 끝난다")]
    public void Move_LethalOverwatchEndsActivation()
    {
        var state = CreateOpenState();
        var mover = AddUnit(state, id: 1, owner: 0, x: 0, y: 0, hp: 1);
        var ally = AddUnit(state, id: 3, owner: 0, x: 0, y: 4, hp: 30);
        var watcher = AddUnit(state, id: 2, owner: 1, x: 4, y: 2, hp: 30, attackPower: 8, attackRange: 4,
            weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1);
        SrpBattleFlow.StartRound(state);
        mover.reactionPoints = 0;
        SrpOverwatch.Arm(state, watcher);
        var observer = new RecordingObserver();

        var result = SrpBattleFlow.Move(state, mover, 0, 2, 2, observer);

        Assert.That(result.moverDied, Is.True);
        Assert.That(result.activationEnded, Is.True);
        Assert.That(mover.eliminated, Is.True);
        Assert.That(observer.events, Is.EqualTo(new[] { "moved", "attack:Overwatch", "died:Overwatch" }));
        Assert.That(SrpVictory.Evaluate(state).decided, Is.False, "아군이 남아 있는데 전투가 끝났습니다.");
        Assert.That(ally.eliminated, Is.False);
    }

    [TestCase(TestName = "기본 공격은 AP 1과 탄약을 쓰고 활성화당 1회로 기록되며 탄약이 없으면 실행되지 않는다")]
    public void BasicAttack_SpendsResourcesAndRejectsEmptyGun()
    {
        var state = CreateOpenState();
        var shooter = AddUnit(state, id: 1, owner: 1, x: 0, y: 2, hp: 30, attackPower: 8, attackRange: 4,
            weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1);
        var target = AddUnit(state, id: 2, owner: 0, x: 3, y: 2, hp: 30);
        SrpBattleFlow.StartRound(state);
        target.reactionPoints = 0;
        SrpBattleFlow.BeginActivation(state, shooter, null);

        var first = SrpBattleFlow.BasicAttack(state, shooter, target, null);
        Assert.That(first.executed, Is.True);
        Assert.That(shooter.ammo, Is.EqualTo(0));
        Assert.That(shooter.actionPoints, Is.EqualTo(1));
        Assert.That(shooter.hasAttackedThisActivation, Is.True);
        Assert.That(first.activationEnded, Is.False, "AP가 남았는데 활성화가 끝났습니다.");
        Assert.That(target.hp, Is.LessThan(30));

        int hpAfterFirst = target.hp;
        var empty = SrpBattleFlow.BasicAttack(state, shooter, target, null);
        Assert.That(empty.executed, Is.False);
        Assert.That(shooter.actionPoints, Is.EqualTo(1), "실행되지 않은 공격이 AP를 썼습니다.");
        Assert.That(target.hp, Is.EqualTo(hpAfterFirst));
    }

    [TestCase(TestName = "AI 행동 실행은 규칙에 어긋난 행동을 거부하고 상태를 바꾸지 않는다")]
    public void ExecuteAiAction_RejectsInvalidActionsWithoutSideEffects()
    {
        var state = CreateOpenState();
        var actor = AddUnit(state, id: 1, owner: 1, x: 0, y: 0, hp: 30, attackPower: 8);
        var target = AddUnit(state, id: 2, owner: 0, x: 1, y: 0, hp: 30);
        SrpBattleFlow.StartRound(state);
        target.reactionPoints = 0;
        int remainingMove = SrpBattleFlow.BeginActivation(state, actor, null);

        // 이동력 밖의 칸.
        var farMove = SrpBattleFlow.ExecuteAiAction(state, actor, SrpEnemyAiAction.MoveTo(4, 4), ref remainingMove, null);
        Assert.That(farMove.executed, Is.False);
        Assert.That(actor.anchorX, Is.EqualTo(0));
        Assert.That(actor.actionPoints, Is.EqualTo(2));
        Assert.That(remainingMove, Is.EqualTo(actor.moveRange));

        // 근접 유닛은 장전할 총이 가득 차 있어 재장전할 수 없다.
        actor.maxAmmo = 1;
        actor.ammo = 1;
        var reload = SrpBattleFlow.ExecuteAiAction(state, actor, SrpEnemyAiAction.Reload(), ref remainingMove, null);
        Assert.That(reload.executed, Is.False);
        Assert.That(actor.actionPoints, Is.EqualTo(2));

        // 없는 스킬.
        var skill = SrpBattleFlow.ExecuteAiAction(
            state, actor, SrpEnemyAiAction.UseSkill("cleave", target), ref remainingMove, null);
        Assert.That(skill.executed, Is.False);

        var attack = SrpBattleFlow.ExecuteAiAction(state, actor, SrpEnemyAiAction.Attack(target.id), ref remainingMove, null);
        Assert.That(attack.executed, Is.True);
        Assert.That(actor.actionPoints, Is.EqualTo(1));

        // 기본 공격은 활성화당 1회.
        var second = SrpBattleFlow.ExecuteAiAction(state, actor, SrpEnemyAiAction.Attack(target.id), ref remainingMove, null);
        Assert.That(second.executed, Is.False);
        Assert.That(actor.actionPoints, Is.EqualTo(1));
    }

    [TestCase(TestName = "라운드 시작은 AP와 RP를 채우고 지난 라운드의 경계태세 예약을 지운다")]
    public void StartRound_ResetsRoundResources()
    {
        var state = CreateOpenState();
        var unit = AddUnit(state, id: 1, owner: 0, x: 0, y: 0, hp: 30, attackRange: 4,
            weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1);
        AddUnit(state, id: 2, owner: 1, x: 4, y: 4, hp: 30);
        unit.actionPoints = 0;
        unit.reactionPoints = 0;
        unit.overwatchArmed = true;
        unit.passiveAppliedThisTurn = true;

        SrpBattleFlow.StartRound(state);

        Assert.That(state.RoundQueue.Count, Is.EqualTo(2));
        Assert.That(unit.actionPoints, Is.EqualTo(unit.maxActionPoints));
        Assert.That(unit.reactionPoints, Is.EqualTo(unit.maxReactionPoints));
        Assert.That(unit.overwatchArmed, Is.False);
        Assert.That(unit.passiveAppliedThisTurn, Is.False);
    }

    [TestCase(TestName = "적 AI 사격수는 더 할 행동이 없으면 경계태세를 예약하고 근접 유닛은 예약하지 않는다")]
    public void EnemyAi_ShooterArmsOverwatchWhenNothingElseToDo()
    {
        var state = CreateOpenState();
        // 사선이 막혀 쏠 수 없고 이미 이동한 사격수.
        var shooter = AddUnit(state, id: 1, owner: 1, x: 0, y: 0, hp: 30, attackPower: 8, attackRange: 2,
            weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1);
        var melee = AddUnit(state, id: 3, owner: 1, x: 0, y: 4, hp: 30, attackPower: 8, attackRange: 3, maxAmmo: 1);
        AddUnit(state, id: 2, owner: 0, x: 4, y: 2, hp: 30);
        SrpBattleFlow.StartRound(state);

        int shooterMove = SrpBattleFlow.BeginActivation(state, shooter, null);
        shooter.hasMovedThisActivation = true;
        shooter.actionPoints = 1;
        var decision = SrpEnemyAi.Decide(state, shooter, 0, false);
        Assert.That(decision.type, Is.EqualTo(SrpEnemyAiActionType.Overwatch));

        int remaining = 0;
        var step = SrpBattleFlow.ExecuteAiAction(state, shooter, decision, ref remaining, null);
        Assert.That(step.executed, Is.True);
        Assert.That(step.activationEnded, Is.True, "경계태세 예약 뒤에도 활성화가 이어졌습니다.");
        Assert.That(shooter.overwatchArmed, Is.True);
        Assert.That(shooter.actionPoints, Is.EqualTo(0));
        Assert.That(shooterMove, Is.EqualTo(shooter.moveRange));

        SrpBattleFlow.BeginActivation(state, melee, null);
        melee.hasMovedThisActivation = true;
        melee.actionPoints = 1;
        Assert.That(SrpEnemyAi.Decide(state, melee, 0, false).type, Is.EqualTo(SrpEnemyAiActionType.EndTurn));
    }

    [TestCase(TestName = "AI 시뮬레이션은 재장전과 스킬과 경계태세와 기회공격을 실제로 사용한다")]
    public void Simulation_UsesReloadSkillOverwatchAndOpportunityAttack()
    {
        var config = new SrpBattleSimConfig
        {
            mapPreset = SrpMapPreset.M1OpeningPrototype,
            trials = 60,
            maxRounds = 16,
            baseSeed = 20261114,
            sampleSeedCount = 1,
            owner0Policy = new SrpRandomAiPolicy(),
            owner1Policy = new SrpHeuristicAiPolicy(),
            thresholds = SrpSimThresholdConfig.Default(),
        };

        var report = SrpBattleSimRunner.RunBatch(config, writeReport: false).report;

        UnityEngine.Debug.Log(
            $"[SRPG][AI-Sim][Actions] reload={report.actions.reloadsPerBattle:F2}, " +
            $"skill={report.actions.skillUsesPerBattle:F2}, " +
            $"overwatchArm={report.actions.overwatchArmsPerBattle:F2}, " +
            $"overwatchShot={report.actions.overwatchShotsPerBattle:F2}, " +
            $"opportunity={report.actions.opportunityAttacksPerBattle:F2}, " +
            $"groggySkip={report.actions.groggySkipsPerBattle:F2}");
        Assert.That(report.actions.reloadsPerBattle, Is.GreaterThan(0f), "시뮬레이션이 재장전을 쓰지 않았습니다.");
        Assert.That(report.actions.skillUsesPerBattle, Is.GreaterThan(0f), "시뮬레이션이 스킬을 쓰지 않았습니다.");
        Assert.That(report.actions.overwatchArmsPerBattle, Is.GreaterThan(0f), "시뮬레이션이 경계태세를 쓰지 않았습니다.");
        Assert.That(report.actions.overwatchShotsPerBattle, Is.GreaterThan(0f), "시뮬레이션에서 경계사격이 발동하지 않았습니다.");
        Assert.That(report.actions.opportunityAttacksPerBattle, Is.GreaterThan(0f), "시뮬레이션에서 기회공격이 발동하지 않았습니다.");
    }

    [TestCase(TestName = "시뮬레이션은 지휘관 처치 규칙을 켠 맵에서 전멸 전에 승패를 낸다")]
    public void Simulation_UsesSharedVictoryRule()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.CreateM1ObjectiveLab());
        // 적 지휘관을 한 방에 쓰러질 상태로 아군 지휘관 옆에 둔다.
        var leader = FindUnit(state, "objective_leader");
        var enemyOfficer = FindUnit(state, "objective_enemy_officer");
        enemyOfficer.anchorX = leader.anchorX + 1;
        enemyOfficer.anchorY = leader.anchorY;
        enemyOfficer.hp = 1;
        enemyOfficer.pg = 0;
        enemyOfficer.groggy = true;
        var config = new SrpBattleSimConfig
        {
            maxRounds = 4,
            owner0Policy = new SrpHeuristicAiPolicy(),
            owner1Policy = new SrpHeuristicAiPolicy(),
        };

        var result = SrpBattleSimRunner.RunSingle(state, config, seed: 1);

        Assert.That(result.winnerOwner, Is.EqualTo(0));
        Assert.That(result.rounds, Is.EqualTo(1));
        Assert.That(state.OwnerHasAliveUnits(1), Is.True, "전멸이 아니라 지휘관 처치로 끝나야 합니다.");
    }

    sealed class RecordingObserver : ISrpBattleFlowObserver
    {
        public readonly List<string> events = new List<string>();

        public void OnRuleLog(string line)
        {
        }

        public void OnMoved(SrpUnitRuntime mover, int cost, bool disengaged)
        {
            events.Add("moved");
        }

        public void OnAttackResolved(
            SrpAttackSource source,
            SrpUnitRuntime attacker,
            SrpUnitRuntime defender,
            SrpCombatResolver.AttackOutcome outcome)
        {
            events.Add("attack:" + source);
        }

        public void OnUnitDied(SrpAttackSource source, SrpUnitRuntime unit)
        {
            events.Add("died:" + source);
        }
    }

    static SrpBattleState CreateOpenState()
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

    static SrpUnitRuntime AddUnit(
        SrpBattleState state,
        int id,
        int owner,
        int x,
        int y,
        int hp,
        int attackPower = 6,
        int attackRange = 1,
        SrpWeaponClass weaponClass = SrpWeaponClass.Melee,
        int maxAmmo = 0)
    {
        var unit = new SrpUnitRuntime
        {
            id = id,
            displayName = "U" + id,
            owner = owner,
            anchorX = x,
            anchorY = y,
            hp = hp,
            maxHp = 30,
            pg = 20,
            maxPg = 20,
            actionPoints = 2,
            maxActionPoints = 2,
            reactionPoints = 1,
            maxReactionPoints = 1,
            speed = 10,
            moveRange = 3,
            attackPower = attackPower,
            attackRange = attackRange,
            weaponClass = weaponClass,
            maxAmmo = maxAmmo,
            ammo = maxAmmo,
        };
        state.Units.Add(unit);
        return unit;
    }

    static SrpUnitRuntime FindUnit(SrpBattleState state, string templateId)
    {
        foreach (var unit in state.Units)
        {
            if (!unit.eliminated && unit.templateId == templateId)
                return unit;
        }
        Assert.Fail($"{templateId} 유닛을 찾지 못했습니다.");
        return null;
    }
}

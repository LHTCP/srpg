using System;
using System.Collections.Generic;
using UnityEngine;

public struct SrpBattleSimConfig
{
    public SrpMapPreset mapPreset;
    public int trials;
    public int maxRounds;
    public int baseSeed;
    public int sampleSeedCount;
    public ISrpAiPolicy owner0Policy;
    public ISrpAiPolicy owner1Policy;
    public SrpSimThresholdConfig thresholds;
}

public struct SrpSingleBattleResult
{
    public int winnerOwner;
    public int rounds;
    public int executionCount;
    public int firearmHpDamage;
    public int firearmPgDamage;
    public int meleeHpDamage;
    public int meleePgDamage;
    public int magicHpDamage;
    public int magicPgDamage;
    public int aggressiveAttackCount;
    public int defensiveAttackCount;
    public float zocPenaltySum;
    public int zocSampleCount;
    public int reloadCount;
    public int skillUseCount;
    public int overwatchArmCount;
    public int overwatchShotCount;
    public int opportunityAttackCount;
    public int groggySkipCount;
}

public struct SrpBattleSimRunOutput
{
    public SrpSimReport report;
    public string reportPath;
}

public static class SrpBattleSimRunner
{
    public static SrpBattleSimRunOutput RunBatch(SrpBattleSimConfig config, bool writeReport = true)
    {
        var map = SrpDefaultMaps.GetPreset(config.mapPreset);
        var baseState = SrpBattleState.FromMap(map);
        var metrics = new SrpSimMetrics
        {
            totalTrials = Mathf.Max(1, config.trials),
        };

        for (int i = 0; i < metrics.totalTrials; i++)
        {
            int seed = config.baseSeed + (i * 7919);
            if (metrics.sampledSeeds.Count < Mathf.Max(1, config.sampleSeedCount))
                metrics.sampledSeeds.Add(seed);

            var state = baseState.Clone();
            var result = RunSingle(state, config, seed);
            Accumulate(ref metrics, result);
        }

        var report = BuildReport(config, metrics);
        report.threshold = SrpSimThresholds.Evaluate(report, config.thresholds);

        string reportPath = string.Empty;
        if (writeReport)
            reportPath = SrpSimReportWriter.Write(report);

        return new SrpBattleSimRunOutput
        {
            report = report,
            reportPath = reportPath,
        };
    }

    /// <summary>
    /// 전투 한 판을 끝까지 돌린다. 라운드/활성화 진행, 행동 실행, 반응(기회공격·경계사격), 승패 판정은
    /// 런타임과 같은 SrpBattleFlow / SrpTurnOrder / SrpVictory를 쓴다. 이 함수는 정책 호출과 지표 기록만 한다.
    /// </summary>
    public static SrpSingleBattleResult RunSingle(SrpBattleState state, SrpBattleSimConfig config, int seed)
    {
        var rng = new System.Random(seed);
        var result = new SrpSingleBattleResult
        {
            winnerOwner = -1,
            rounds = 0,
        };

        for (int round = 1; round <= Mathf.Max(1, config.maxRounds); round++)
        {
            state.RoundNumber = round;
            SrpBattleFlow.StartRound(state);
            if (state.RoundQueue.Count == 0)
            {
                result.rounds = round;
                result.winnerOwner = -1;
                return result;
            }

            while (SrpTurnOrder.HasRemainingUnitInRound(state))
            {
                int unitId = SrpTurnOrder.AdvanceToNextUnit(state);
                var actor = FindAliveUnit(state, unitId);
                if (actor == null)
                    continue;
                if (SrpTurnOrder.TryConsumeActivationForGroggyRecovery(actor, out _))
                {
                    result.groggySkipCount++;
                    continue;
                }

                RunActivation(state, config, actor, rng, ref result);

                var victory = SrpVictory.Evaluate(state);
                if (victory.decided)
                {
                    result.rounds = round;
                    result.winnerOwner = victory.winnerOwner;
                    return result;
                }
            }
        }

        result.rounds = Mathf.Max(1, config.maxRounds);
        result.winnerOwner = -1;
        return result;
    }

    static void RunActivation(
        SrpBattleState state,
        SrpBattleSimConfig config,
        SrpUnitRuntime actor,
        System.Random rng,
        ref SrpSingleBattleResult result)
    {
        int remainingMove = SrpBattleFlow.BeginActivation(state, actor, null);
        var policy = GetPolicyForOwner(config, actor.owner);

        for (int step = 0; step < SrpBattleFlow.MaxAiActionsPerActivation; step++)
        {
            if (actor.eliminated || actor.actionPoints <= 0)
                break;

            var ctx = new SrpAiDecisionContext
            {
                state = state,
                actor = actor,
                remainingMove = remainingMove,
                moved = actor.hasMovedThisActivation,
                attacked = actor.hasAttackedThisActivation,
                moves = BuildMoveOptions(state, actor, remainingMove),
                attacks = BuildAttackOptions(state, actor),
                rng = rng,
            };
            var command = policy.SelectAction(ctx);
            if (command.actionType == SrpAiActionType.EndTurn)
                break;
            // 정책 계약: 이동은 활성화당 1회.
            if (command.actionType == SrpAiActionType.Move && actor.hasMovedThisActivation)
                break;

            int fromX = actor.anchorX;
            int fromY = actor.anchorY;
            var stance = actor.stance;
            var stepResult = SrpBattleFlow.ExecuteAiAction(state, actor, ToAction(command), ref remainingMove, null);
            if (!stepResult.executed)
                break;

            RecordStep(stepResult, fromX, fromY, command, stance, ref result);
            if (stepResult.activationEnded || SrpVictory.Evaluate(state).decided)
                break;
        }
    }

    static SrpEnemyAiAction ToAction(SrpAiCommand command)
    {
        switch (command.actionType)
        {
            case SrpAiActionType.Move:
                return SrpEnemyAiAction.MoveTo(command.x, command.y);
            case SrpAiActionType.Attack:
                return SrpEnemyAiAction.Attack(command.targetUnitId);
            case SrpAiActionType.Reload:
                return SrpEnemyAiAction.Reload();
            case SrpAiActionType.Overwatch:
                return SrpEnemyAiAction.Overwatch();
            case SrpAiActionType.UseSkill:
                return new SrpEnemyAiAction
                {
                    type = SrpEnemyAiActionType.UseSkill,
                    skillId = command.skillId,
                    targetUnitId = command.targetUnitId,
                    x = command.x,
                    y = command.y,
                };
            default:
                return SrpEnemyAiAction.EndTurn();
        }
    }

    static void RecordStep(
        SrpAiStepResult step,
        int fromX,
        int fromY,
        SrpAiCommand command,
        SrpStance stanceAtAction,
        ref SrpSingleBattleResult result)
    {
        switch (step.type)
        {
            case SrpEnemyAiActionType.Move:
            {
                int baseDistance = Mathf.Abs(command.x - fromX) + Mathf.Abs(command.y - fromY);
                result.zocPenaltySum += Mathf.Max(0, step.move.cost - baseDistance);
                result.zocSampleCount++;
                RecordReaction(step.move.opportunity, ref result);
                RecordReaction(step.move.overwatch, ref result);
                break;
            }
            case SrpEnemyAiActionType.Attack:
                RecordDamage(step.attack.outcome, ref result);
                if (stanceAtAction == SrpStance.Aggressive) result.aggressiveAttackCount++;
                if (stanceAtAction == SrpStance.Defensive) result.defensiveAttackCount++;
                if (step.attack.outcome.wasExecution) result.executionCount++;
                RecordReaction(step.attack.overwatch, ref result);
                break;
            case SrpEnemyAiActionType.UseSkill:
                result.skillUseCount++;
                result.magicHpDamage += step.skill.targetHpDamage;
                result.magicPgDamage += step.skill.targetPgDamage;
                RecordReaction(step.skill.overwatch, ref result);
                break;
            case SrpEnemyAiActionType.Reload:
                result.reloadCount++;
                break;
            case SrpEnemyAiActionType.Overwatch:
                result.overwatchArmCount++;
                break;
        }
    }

    static void RecordReaction(SrpReactionResult reaction, ref SrpSingleBattleResult result)
    {
        if (!reaction.triggered)
            return;
        if (reaction.source == SrpAttackSource.Overwatch)
            result.overwatchShotCount++;
        else
            result.opportunityAttackCount++;
        RecordDamage(reaction.outcome, ref result);
    }

    static void RecordDamage(SrpCombatResolver.AttackOutcome outcome, ref SrpSingleBattleResult result)
    {
        switch (outcome.basicAttackKind)
        {
            case SrpBasicAttackKind.Firearm:
                result.firearmHpDamage += Mathf.Max(0, outcome.damageToHp);
                result.firearmPgDamage += Mathf.Max(0, outcome.damageToPg);
                break;
            case SrpBasicAttackKind.Melee:
            default:
                result.meleeHpDamage += Mathf.Max(0, outcome.damageToHp);
                result.meleePgDamage += Mathf.Max(0, outcome.damageToPg);
                break;
        }
    }

    static List<SrpAiMoveOption> BuildMoveOptions(SrpBattleState state, SrpUnitRuntime actor, int remainingMove)
    {
        var list = new List<SrpAiMoveOption>();
        if (remainingMove <= 0)
            return list;

        var costs = SrpPathfinder.GetReachableWithCosts(state, actor, remainingMove);
        foreach (var kv in costs)
        {
            int baseDistance = Mathf.Abs(kv.Key.x - actor.anchorX) + Mathf.Abs(kv.Key.y - actor.anchorY);
            list.Add(new SrpAiMoveOption
            {
                x = kv.Key.x,
                y = kv.Key.y,
                cost = kv.Value,
                zocPenalty = Mathf.Max(0, kv.Value - baseDistance),
            });
        }
        return list;
    }

    static List<SrpAiAttackOption> BuildAttackOptions(SrpBattleState state, SrpUnitRuntime actor)
    {
        var list = new List<SrpAiAttackOption>();
        foreach (var unit in state.Units)
        {
            if (unit.eliminated || unit.owner == actor.owner)
                continue;
            if (!SrpCombatResolver.CanAttack(state, actor, unit))
                continue;
            list.Add(new SrpAiAttackOption { targetUnitId = unit.id });
        }
        return list;
    }

    static ISrpAiPolicy GetPolicyForOwner(SrpBattleSimConfig config, int owner)
    {
        if (owner == 0 && config.owner0Policy != null)
            return config.owner0Policy;
        if (owner == 1 && config.owner1Policy != null)
            return config.owner1Policy;
        return new SrpRandomAiPolicy();
    }

    static SrpUnitRuntime FindAliveUnit(SrpBattleState state, int id)
    {
        foreach (var unit in state.Units)
        {
            if (unit.id == id && !unit.eliminated)
                return unit;
        }
        return null;
    }

    static void Accumulate(ref SrpSimMetrics metrics, SrpSingleBattleResult result)
    {
        metrics.completedTrials++;
        metrics.totalRounds += result.rounds;
        if (result.winnerOwner == 0) metrics.owner0Wins++;
        else if (result.winnerOwner == 1) metrics.owner1Wins++;
        else metrics.drawCount++;

        metrics.totalExecutionCount += result.executionCount;
        metrics.totalZocPenalty += result.zocPenaltySum;
        metrics.zocSampleCount += result.zocSampleCount;

        metrics.firearmHpDamage += result.firearmHpDamage;
        metrics.firearmPgDamage += result.firearmPgDamage;
        metrics.meleeHpDamage += result.meleeHpDamage;
        metrics.meleePgDamage += result.meleePgDamage;
        metrics.magicHpDamage += result.magicHpDamage;
        metrics.magicPgDamage += result.magicPgDamage;
        metrics.aggressiveAttackCount += result.aggressiveAttackCount;
        metrics.defensiveAttackCount += result.defensiveAttackCount;

        metrics.reloadCount += result.reloadCount;
        metrics.skillUseCount += result.skillUseCount;
        metrics.overwatchArmCount += result.overwatchArmCount;
        metrics.overwatchShotCount += result.overwatchShotCount;
        metrics.opportunityAttackCount += result.opportunityAttackCount;
        metrics.groggySkipCount += result.groggySkipCount;
    }

    static SrpSimReport BuildReport(SrpBattleSimConfig config, SrpSimMetrics metrics)
    {
        var report = new SrpSimReport();
        int trials = Mathf.Max(1, metrics.completedTrials);

        report.runMeta.timestampUtc = DateTime.UtcNow.ToString("o");
        report.runMeta.baseSeed = config.baseSeed;
        report.runMeta.trials = trials;
        report.runMeta.mapPreset = config.mapPreset.ToString();
        report.runMeta.owner0Policy = config.owner0Policy != null ? config.owner0Policy.Name : "Random";
        report.runMeta.owner1Policy = config.owner1Policy != null ? config.owner1Policy.Name : "Random";

        report.outcome.owner0WinRate = SrpSimThresholds.SafeDiv(metrics.owner0Wins, trials);
        report.outcome.owner1WinRate = SrpSimThresholds.SafeDiv(metrics.owner1Wins, trials);
        report.outcome.drawRate = SrpSimThresholds.SafeDiv(metrics.drawCount, trials);
        report.outcome.averageRounds = metrics.totalRounds / (float)trials;

        int firearmTotal = metrics.firearmHpDamage + metrics.firearmPgDamage;
        int meleeTotal = metrics.meleeHpDamage + metrics.meleePgDamage;
        report.combat.firearmHpShare = SrpSimThresholds.SafeDiv(metrics.firearmHpDamage, firearmTotal);
        report.combat.meleePgShare = SrpSimThresholds.SafeDiv(metrics.meleePgDamage, meleeTotal);
        report.combat.executionRate = SrpSimThresholds.SafeDiv(metrics.totalExecutionCount, trials);

        report.control.zocPenaltyAverage =
            metrics.zocSampleCount > 0 ? metrics.totalZocPenalty / metrics.zocSampleCount : 0f;
        report.control.aggressiveAttackCount = metrics.aggressiveAttackCount;
        report.control.defensiveAttackCount = metrics.defensiveAttackCount;

        report.actions.reloadsPerBattle = SrpSimThresholds.SafeDiv(metrics.reloadCount, trials);
        report.actions.skillUsesPerBattle = SrpSimThresholds.SafeDiv(metrics.skillUseCount, trials);
        report.actions.overwatchArmsPerBattle = SrpSimThresholds.SafeDiv(metrics.overwatchArmCount, trials);
        report.actions.overwatchShotsPerBattle = SrpSimThresholds.SafeDiv(metrics.overwatchShotCount, trials);
        report.actions.opportunityAttacksPerBattle = SrpSimThresholds.SafeDiv(metrics.opportunityAttackCount, trials);
        report.actions.groggySkipsPerBattle = SrpSimThresholds.SafeDiv(metrics.groggySkipCount, trials);

        report.sampleSeeds = new List<int>(metrics.sampledSeeds);
        return report;
    }
}

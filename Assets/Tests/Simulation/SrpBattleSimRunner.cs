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
            state.RoundQueue.Clear();
            state.RoundQueue.AddRange(SrpTurnOrder.BuildRoundQueue(state));
            if (state.RoundQueue.Count == 0)
            {
                result.rounds = round;
                result.winnerOwner = -1;
                return result;
            }

            for (int i = 0; i < state.RoundQueue.Count; i++)
            {
                int unitId = state.RoundQueue[i];
                var actor = FindAliveUnit(state, unitId);
                if (actor == null)
                    continue;

                actor.actionPoints = Mathf.Max(1, actor.maxActionPoints);
                actor.reactionPoints = Mathf.Max(0, actor.maxReactionPoints);
                int remainingMove = Mathf.Max(0, actor.moveRange);
                bool moved = false;
                bool attacked = false;

                for (int step = 0; step < 3 && actor.actionPoints > 0; step++)
                {
                    var moves = BuildMoveOptions(state, actor, remainingMove);
                    var attacks = BuildAttackOptions(state, actor);
                    var ctx = new SrpAiDecisionContext
                    {
                        state = state,
                        actor = actor,
                        remainingMove = remainingMove,
                        moved = moved,
                        attacked = attacked,
                        moves = moves,
                        attacks = attacks,
                        rng = rng,
                    };
                    var policy = GetPolicyForOwner(config, actor.owner);
                    var command = policy.SelectAction(ctx);

                    if (command.actionType == SrpAiActionType.EndTurn)
                        break;

                    if (command.actionType == SrpAiActionType.Move && !moved)
                    {
                        if (!TryMove(state, actor, remainingMove, command.x, command.y, ref result, out int moveCost))
                            break;
                        actor.actionPoints = Mathf.Max(0, actor.actionPoints - 1);
                        remainingMove = Mathf.Max(0, remainingMove - moveCost);
                        moved = true;
                        continue;
                    }

                    if (command.actionType == SrpAiActionType.Attack && !attacked)
                    {
                        if (!TryAttack(state, actor, command.targetUnitId, ref result))
                            break;
                        actor.actionPoints = Mathf.Max(0, actor.actionPoints - 1);
                        attacked = true;
                        break;
                    }

                    break;
                }

                int aliveWinner = GetWinnerOwner(state);
                if (aliveWinner >= 0)
                {
                    result.rounds = round;
                    result.winnerOwner = aliveWinner;
                    return result;
                }
            }

            int winner = GetWinnerOwner(state);
            if (winner >= 0)
            {
                result.rounds = round;
                result.winnerOwner = winner;
                return result;
            }
        }

        result.rounds = Mathf.Max(1, config.maxRounds);
        result.winnerOwner = -1;
        return result;
    }

    static bool TryMove(
        SrpBattleState state,
        SrpUnitRuntime actor,
        int remainingMove,
        int targetX,
        int targetY,
        ref SrpSingleBattleResult result,
        out int moveCost)
    {
        moveCost = 0;
        var costs = SrpPathfinder.GetReachableWithCosts(state, actor, remainingMove);
        var key = new Vector2Int(targetX, targetY);
        if (!costs.TryGetValue(key, out int cost))
            return false;

        int baseDistance = Mathf.Abs(targetX - actor.anchorX) + Mathf.Abs(targetY - actor.anchorY);
        int zocPenalty = Mathf.Max(0, cost - baseDistance);
        result.zocPenaltySum += zocPenalty;
        result.zocSampleCount++;

        actor.anchorX = targetX;
        actor.anchorY = targetY;
        moveCost = cost;
        return true;
    }

    static bool TryAttack(SrpBattleState state, SrpUnitRuntime actor, int targetUnitId, ref SrpSingleBattleResult result)
    {
        var target = FindAliveUnit(state, targetUnitId);
        if (target == null)
            return false;
        if (!SrpCombatResolver.CanAttack(state, actor, target))
            return false;

        var attackKind = SrpCombatResolver.ResolveBasicAttackKind(state, actor, target);
        if (!SrpCombatResolver.SpendAmmoForBasicAttack(attackKind, actor))
            return false;
        if (attackKind == SrpBasicAttackKind.Firearm)
            SrpFirearmAim.TurnShooterTowardTarget(actor, target);
        var outcome = SrpCombatResolver.ApplyAttack(state, actor, target);
        RecordDamage(outcome, ref result);

        if (actor.stance == SrpStance.Aggressive) result.aggressiveAttackCount++;
        if (actor.stance == SrpStance.Defensive) result.defensiveAttackCount++;
        if (outcome.wasExecution) result.executionCount++;
        if (outcome.defenderDied) state.RemoveUnit(target);
        return true;
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

    static int GetWinnerOwner(SrpBattleState state)
    {
        int aliveOwner = -1;
        foreach (var unit in state.Units)
        {
            if (unit.eliminated)
                continue;

            if (aliveOwner == -1)
            {
                aliveOwner = unit.owner;
                continue;
            }

            if (aliveOwner != unit.owner)
                return -1;
        }
        return aliveOwner;
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

        report.sampleSeeds = new List<int>(metrics.sampledSeeds);
        return report;
    }
}

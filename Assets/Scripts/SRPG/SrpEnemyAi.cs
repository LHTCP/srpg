using System.Collections.Generic;
using UnityEngine;

public enum SrpEnemyAiActionType
{
    EndTurn,
    Move,
    Attack,
    Reload,
    UseSkill,
    Overwatch,
}

public struct SrpEnemyAiAction
{
    public SrpEnemyAiActionType type;
    public int x;
    public int y;
    public int targetUnitId;
    public string skillId;

    public static SrpEnemyAiAction EndTurn()
    {
        return new SrpEnemyAiAction { type = SrpEnemyAiActionType.EndTurn };
    }

    public static SrpEnemyAiAction MoveTo(int x, int y)
    {
        return new SrpEnemyAiAction { type = SrpEnemyAiActionType.Move, x = x, y = y };
    }

    public static SrpEnemyAiAction Attack(int targetUnitId)
    {
        return new SrpEnemyAiAction { type = SrpEnemyAiActionType.Attack, targetUnitId = targetUnitId };
    }

    public static SrpEnemyAiAction Reload()
    {
        return new SrpEnemyAiAction { type = SrpEnemyAiActionType.Reload };
    }

    public static SrpEnemyAiAction Overwatch()
    {
        return new SrpEnemyAiAction { type = SrpEnemyAiActionType.Overwatch };
    }

    public static SrpEnemyAiAction UseSkill(string skillId, SrpUnitRuntime target)
    {
        return new SrpEnemyAiAction
        {
            type = SrpEnemyAiActionType.UseSkill,
            skillId = skillId,
            targetUnitId = target.id,
            x = target.anchorX,
            y = target.anchorY,
        };
    }
}

/// <summary>
/// 적 진영 기본 AI. 전투 상태만 읽어 다음 행동 하나를 고르는 순수 로직이다.
/// 런타임(SrpGameController)과 AI 시뮬레이션이 같은 판단을 쓴다.
/// 범위: 기본 공격, 단일 적 대상 피해 스킬, 이동, 재장전, 총기 역할의 경계태세.
/// 태그/회복 스킬, 엄폐, 상호작용, 태세/방향 변경은 쓰지 않는다.
/// </summary>
public static class SrpEnemyAi
{
    const int ExecutionScore = 1000;
    const int AttackFromTileScore = 100;
    const int ExecuteFromTileScore = 200;

    /// <summary>현재 활성 유닛의 다음 행동 하나를 고른다. 상태는 변경하지 않는다.</summary>
    public static SrpEnemyAiAction Decide(
        SrpBattleState state,
        SrpUnitRuntime actor,
        int remainingMove,
        bool hasAttacked)
    {
        if (state == null || actor == null || actor.eliminated || actor.actionPoints <= 0 || actor.IsPgBroken)
            return SrpEnemyAiAction.EndTurn();

        bool isShooter = actor.weaponClass == SrpWeaponClass.Firearm;
        bool hasSkill = TryPickDamageSkill(state, actor, out var skill, out var skillTarget);

        if (hasAttacked)
        {
            // 공격을 마쳤으면 남은 AP로 스킬을 잇거나 다음 사격을 준비한다.
            if (hasSkill)
                return SrpEnemyAiAction.UseSkill(skill.id, skillTarget);
            if (isShooter && actor.CanReload())
                return SrpEnemyAiAction.Reload();
            return OverwatchOrEndTurn(actor, isShooter);
        }

        var target = PickAttackTarget(state, actor);
        bool canExecute = target != null
            && target.IsPgBroken
            && state.ChebyshevAnchor(actor, target) <= 1;
        if (canExecute)
            return SrpEnemyAiAction.Attack(target.id);
        // AP가 1뿐이면 기본 공격보다 피해 스킬을 우선한다. AP가 2면 공격 후 스킬로 잇는다.
        if (hasSkill && (target == null || actor.actionPoints <= 1))
            return SrpEnemyAiAction.UseSkill(skill.id, skillTarget);
        if (target != null)
            return SrpEnemyAiAction.Attack(target.id);

        // 주무장이 총기인 유닛은 빈 총으로 돌격하기보다 먼저 장전한다.
        if (isShooter && actor.CanReload() && actor.ammo <= 0)
            return SrpEnemyAiAction.Reload();

        if (!actor.hasMovedThisActivation && remainingMove > 0
            && TryPickMove(state, actor, remainingMove, out int moveX, out int moveY))
            return SrpEnemyAiAction.MoveTo(moveX, moveY);

        if (actor.CanReload() && actor.ammo <= 0)
            return SrpEnemyAiAction.Reload();

        return OverwatchOrEndTurn(actor, isShooter);
    }

    /// <summary>
    /// 더 할 행동이 없을 때, 총기 역할 유닛은 남은 AP로 경계태세를 예약한다.
    /// 근접/마법 역할은 반응(RP)을 패링·가드에 남겨 두도록 경계태세를 쓰지 않는다.
    /// </summary>
    static SrpEnemyAiAction OverwatchOrEndTurn(SrpUnitRuntime actor, bool isShooter)
    {
        return isShooter && SrpOverwatch.CanArm(actor)
            ? SrpEnemyAiAction.Overwatch()
            : SrpEnemyAiAction.EndTurn();
    }

    /// <summary>지금 자리에서 기본 공격할 대상. 처단 가능 대상을 최우선으로 한다.</summary>
    public static SrpUnitRuntime PickAttackTarget(SrpBattleState state, SrpUnitRuntime actor)
    {
        SrpUnitRuntime best = null;
        int bestScore = int.MinValue;
        foreach (var unit in state.Units)
        {
            if (unit == null || unit.eliminated || unit.owner == actor.owner)
                continue;
            if (!SrpCombatResolver.CanAttack(state, actor, unit))
                continue;

            int score = ScoreAttackTarget(state, actor, unit);
            if (score > bestScore || (score == bestScore && best != null && unit.id < best.id))
            {
                bestScore = score;
                best = unit;
            }
        }
        return best;
    }

    /// <summary>지금 자리에서 쓸 수 있는 단일 적 대상 피해 스킬과 대상을 고른다.</summary>
    static bool TryPickDamageSkill(
        SrpBattleState state,
        SrpUnitRuntime actor,
        out SrpSkillData skill,
        out SrpUnitRuntime target)
    {
        skill = null;
        target = null;
        if (actor.hasUsedSkillThisActivation || actor.skillRuntimes == null)
            return false;

        int bestScore = int.MinValue;
        foreach (var runtime in actor.skillRuntimes)
        {
            if (runtime == null || !state.SkillLookup.TryGetValue(runtime.skillId, out var data))
                continue;
            if (data.targetType != SrpTargetType.SingleEnemy || !HasDamageEffect(data))
                continue;
            if (!SrpSkills.CanUseActiveSkill(data, runtime))
                continue;

            foreach (var unit in state.Units)
            {
                if (unit == null || unit.eliminated || unit.owner == actor.owner)
                    continue;
                if (state.ChebyshevAnchor(actor, unit) > data.range)
                    continue;

                int score = (unit.maxHp - unit.hp) * 2 + (unit.maxPg - unit.pg);
                if (score > bestScore || (score == bestScore && target != null && unit.id < target.id))
                {
                    bestScore = score;
                    skill = data;
                    target = unit;
                }
            }
        }
        return skill != null && target != null;
    }

    static bool HasDamageEffect(SrpSkillData data)
    {
        if (data.effects == null)
            return false;
        foreach (var effect in data.effects)
        {
            if (effect != null && (effect.type == SrpEffectType.Damage || effect.type == SrpEffectType.Cleave))
                return true;
        }
        return false;
    }

    static int ScoreAttackTarget(SrpBattleState state, SrpUnitRuntime actor, SrpUnitRuntime target)
    {
        int score = (target.maxHp - target.hp) + (target.maxPg - target.pg);
        bool adjacent = state.ChebyshevAnchor(actor, target) <= 1;
        if (adjacent && target.IsPgBroken)
            score += ExecutionScore;
        if (target.weaponClass == SrpWeaponClass.Firearm)
            score += 5;
        return score;
    }

    static bool TryPickMove(SrpBattleState state, SrpUnitRuntime actor, int remainingMove, out int moveX, out int moveY)
    {
        moveX = actor.anchorX;
        moveY = actor.anchorY;
        var costs = SrpPathfinder.GetReachableWithCosts(state, actor, remainingMove);
        if (costs.Count == 0)
            return false;

        // 이동에 AP 1을 쓰고도 공격할 AP가 남을 때만 "그 칸에서 공격 가능"을 가산한다.
        bool canAttackAfterMove = actor.actionPoints >= 2;
        int originX = actor.anchorX;
        int originY = actor.anchorY;
        int currentScore = ScoreTile(state, actor, originX, originY, 0, false, out _);
        int bestScore = currentScore;
        bool found = false;
        bool bestEnablesAttack = false;

        foreach (var kv in costs)
        {
            int score = ScoreTile(state, actor, kv.Key.x, kv.Key.y, kv.Value, canAttackAfterMove, out bool enablesAttack);
            if (score > bestScore
                || (found && score == bestScore && IsEarlierTile(kv.Key, moveX, moveY)))
            {
                bestScore = score;
                moveX = kv.Key.x;
                moveY = kv.Key.y;
                found = true;
                bestEnablesAttack = enablesAttack;
            }
        }

        actor.anchorX = originX;
        actor.anchorY = originY;

        // 이동 직후 공격할 수 있는 칸을 골랐으면 그대로 쓴다.
        if (found && bestEnablesAttack)
            return true;

        // 위 점수는 직선(맨해튼) 거리 기준이라 장애물을 돌아가야 하는 길을 찾지 못한다.
        // 고른 칸이 없거나 그 칸이 공격 가능한 칸까지의 실제 경로 비용을 늘리면, 최단 경로를 따라 우회한다.
        int unlimited = state.Width * state.Height * 3;
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var pathCosts = SrpPathfinder.GetReachableWithCosts(state, actor, unlimited, cameFrom);
        var attackTiles = CollectAttackTiles(state, actor, pathCosts);
        // 경로로 닿는 공격 칸이 없으면(완전히 막힘, 아군이 자리를 다 차지함) 직선 거리 판단을 유지한다.
        if (attackTiles.Count == 0)
            return found;

        var goal = default(Vector2Int);
        int goalCost = int.MaxValue;
        foreach (var tile in attackTiles)
        {
            int cost = pathCosts[tile];
            if (cost < goalCost || (cost == goalCost && IsEarlierTile(tile, goal.x, goal.y)))
            {
                goalCost = cost;
                goal = tile;
            }
        }

        if (found)
        {
            var picked = new Vector2Int(moveX, moveY);
            if (attackTiles.Contains(picked)
                || RemainingPathCost(state, actor, moveX, moveY, attackTiles, unlimited) <= goalCost)
                return true;
        }

        // 직선 거리 판단은 경로 비용을 늘리지 않을 때만 쓰고 우회는 경로 비용을 반드시 줄이므로,
        // 막힌 쪽으로 되돌아갔다가 다시 나오는 왕복이 생기지 않는다.
        var origin = new Vector2Int(originX, originY);
        var step = goal;
        while (pathCosts[step] > remainingMove)
        {
            if (!cameFrom.TryGetValue(step, out var previous) || previous == origin)
            {
                moveX = originX;
                moveY = originY;
                return false;
            }
            step = previous;
        }

        moveX = step.x;
        moveY = step.y;
        return true;
    }

    /// <summary>경로로 닿는 칸 중 그 자리에서 기본 공격이 가능한 칸. 유닛 위치는 원래대로 되돌린다.</summary>
    static HashSet<Vector2Int> CollectAttackTiles(
        SrpBattleState state,
        SrpUnitRuntime actor,
        Dictionary<Vector2Int, int> pathCosts)
    {
        var tiles = new HashSet<Vector2Int>();
        int originX = actor.anchorX;
        int originY = actor.anchorY;
        try
        {
            foreach (var kv in pathCosts)
            {
                actor.anchorX = kv.Key.x;
                actor.anchorY = kv.Key.y;
                foreach (var unit in state.Units)
                {
                    if (unit == null || unit.eliminated || unit.owner == actor.owner)
                        continue;
                    if (!SrpCombatResolver.CanAttack(state, actor, unit))
                        continue;
                    tiles.Add(kv.Key);
                    break;
                }
            }
        }
        finally
        {
            actor.anchorX = originX;
            actor.anchorY = originY;
        }
        return tiles;
    }

    /// <summary>(x, y)에 섰다고 가정했을 때 가장 가까운 공격 칸까지의 경로 비용. 닿지 않으면 int.MaxValue.</summary>
    static int RemainingPathCost(
        SrpBattleState state,
        SrpUnitRuntime actor,
        int x,
        int y,
        HashSet<Vector2Int> attackTiles,
        int maxCost)
    {
        int originX = actor.anchorX;
        int originY = actor.anchorY;
        Dictionary<Vector2Int, int> costs;
        try
        {
            actor.anchorX = x;
            actor.anchorY = y;
            costs = SrpPathfinder.GetReachableWithCosts(state, actor, maxCost);
        }
        finally
        {
            actor.anchorX = originX;
            actor.anchorY = originY;
        }

        int best = int.MaxValue;
        foreach (var tile in attackTiles)
        {
            if (costs.TryGetValue(tile, out int cost) && cost < best)
                best = cost;
        }
        return best;
    }

    static bool IsEarlierTile(Vector2Int tile, int x, int y)
    {
        return tile.y < y || (tile.y == y && tile.x < x);
    }

    static int ScoreTile(
        SrpBattleState state,
        SrpUnitRuntime actor,
        int x,
        int y,
        int moveCost,
        bool countAttackOpportunity,
        out bool enablesAttack)
    {
        int originX = actor.anchorX;
        int originY = actor.anchorY;
        actor.anchorX = x;
        actor.anchorY = y;

        int nearest = int.MaxValue;
        bool canAttack = false;
        bool canExecute = false;
        foreach (var unit in state.Units)
        {
            if (unit == null || unit.eliminated || unit.owner == actor.owner)
                continue;

            int dist = Mathf.Abs(unit.anchorX - x) + Mathf.Abs(unit.anchorY - y);
            // 붕괴한 적은 처단 기회이므로 더 가깝게 평가한다.
            if (unit.IsPgBroken)
                dist -= 2;
            if (dist < nearest)
                nearest = dist;

            if (!countAttackOpportunity || !SrpCombatResolver.CanAttack(state, actor, unit))
                continue;
            canAttack = true;
            if (unit.IsPgBroken && state.ChebyshevAnchor(actor, unit) <= 1)
                canExecute = true;
        }

        actor.anchorX = originX;
        actor.anchorY = originY;

        if (nearest == int.MaxValue)
            nearest = 0;
        int score = -nearest * 4 - moveCost;
        if (canAttack)
            score += AttackFromTileScore;
        if (canExecute)
            score += ExecuteFromTileScore;
        enablesAttack = canAttack;
        return score;
    }
}

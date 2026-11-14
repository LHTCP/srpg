using System.Collections.Generic;
using UnityEngine;

public enum SrpEnemyAiActionType
{
    EndTurn,
    Move,
    Attack,
    Reload,
    UseSkill,
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
/// 1차 범위: 기본 공격, 단일 적 대상 피해 스킬, 이동, 재장전.
/// 태그/회복 스킬, 경계태세, 엄폐, 상호작용, 태세/방향 변경은 쓰지 않는다.
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
            return SrpEnemyAiAction.EndTurn();
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

        return SrpEnemyAiAction.EndTurn();
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
        int currentScore = ScoreTile(state, actor, originX, originY, 0, false);
        int bestScore = currentScore;
        bool found = false;

        foreach (var kv in costs)
        {
            int score = ScoreTile(state, actor, kv.Key.x, kv.Key.y, kv.Value, canAttackAfterMove);
            if (score > bestScore
                || (found && score == bestScore && IsEarlierTile(kv.Key, moveX, moveY)))
            {
                bestScore = score;
                moveX = kv.Key.x;
                moveY = kv.Key.y;
                found = true;
            }
        }

        actor.anchorX = originX;
        actor.anchorY = originY;
        return found;
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
        bool countAttackOpportunity)
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
        return score;
    }
}

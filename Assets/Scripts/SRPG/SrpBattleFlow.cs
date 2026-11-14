using System.Collections.Generic;
using UnityEngine;

public enum SrpAttackSource
{
    Basic,
    Opportunity,
    Overwatch,
}

/// <summary>
/// 전투 진행 중 일어난 일을 받아 가는 창구. 런타임은 로그/연출에 쓰고, 시뮬레이션은 null을 넘긴다.
/// 규칙 결과는 바꾸지 못한다.
/// </summary>
public interface ISrpBattleFlowObserver
{
    /// <summary>규칙 처리 중 나온 로그(패시브 발동, 스킬 피해 등).</summary>
    void OnRuleLog(string line);

    void OnMoved(SrpUnitRuntime mover, int cost, bool disengaged);

    /// <summary>기본 공격/기회공격/경계사격 1회가 패시브까지 처리된 직후.</summary>
    void OnAttackResolved(
        SrpAttackSource source,
        SrpUnitRuntime attacker,
        SrpUnitRuntime defender,
        SrpCombatResolver.AttackOutcome outcome);

    /// <summary>공격으로 유닛이 전장에서 제거된 직후.</summary>
    void OnUnitDied(SrpAttackSource source, SrpUnitRuntime unit);
}

public struct SrpReactionResult
{
    public bool triggered;
    public SrpAttackSource source;
    public SrpUnitRuntime reactor;
    public SrpCombatResolver.AttackOutcome outcome;
    public bool targetDied;
}

public struct SrpMoveResult
{
    public int cost;
    public bool disengaged;
    public SrpReactionResult opportunity;
    public SrpReactionResult overwatch;
    public bool moverDied;
    /// <summary>이 행동으로 활성화가 강제로 끝났는가(사망, PG 붕괴).</summary>
    public bool activationEnded;
}

public struct SrpBasicAttackResult
{
    public bool executed;
    public SrpCombatResolver.AttackOutcome outcome;
    public bool defenderDied;
    public SrpReactionResult overwatch;
    public bool attackerDied;
    /// <summary>이 행동으로 활성화가 끝났는가(사망, PG 붕괴, AP 소진).</summary>
    public bool activationEnded;
}

public struct SrpSkillUseResult
{
    public bool executed;
    public int targetHpDamage;
    public int targetPgDamage;
    public SrpReactionResult overwatch;
    public bool casterDied;
    /// <summary>이 행동으로 활성화가 끝났는가(사망, PG 붕괴, 활성화 종료 스킬).</summary>
    public bool activationEnded;
}

public struct SrpAiStepResult
{
    /// <summary>행동이 규칙상 실행됐는가. false면 상태는 바뀌지 않았다.</summary>
    public bool executed;
    public SrpEnemyAiActionType type;
    public SrpMoveResult move;
    public SrpBasicAttackResult attack;
    public SrpSkillUseResult skill;
    public bool activationEnded;
}

/// <summary>
/// 전투 진행 규칙(순수 로직). 라운드 시작, 활성화 시작, 이동/기본 공격/스킬/재장전과
/// 그에 따른 기회공격·경계사격 반응을 한곳에서 처리한다.
/// 런타임(SrpGameController)과 AI 시뮬레이션(SrpBattleSimRunner)이 같은 함수를 호출하므로
/// 두 쪽의 전투 흐름이 어긋나지 않는다. 화면 갱신, 되감기, 입력 검증은 컨트롤러 몫이다.
/// </summary>
public static class SrpBattleFlow
{
    /// <summary>AI가 한 활성화에서 고를 수 있는 행동 수 상한(무한 반복 방지).</summary>
    public const int MaxAiActionsPerActivation = 8;

    /// <summary>라운드 시작: 행동 순서 구성, 스킬 자원 틱, AP/RP/경계태세 등 라운드 자원 초기화.</summary>
    public static void StartRound(SrpBattleState state)
    {
        state.RoundQueue.Clear();
        state.RebuildEngagements();
        state.RoundQueue.AddRange(SrpTurnOrder.BuildRoundQueue(state));
        foreach (var unit in state.Units)
        {
            if (unit == null || unit.eliminated)
                continue;
            SrpSkills.TickSkillResourcesForUnit(unit, state);
        }
        SrpTurnOrder.ResetRoundResources(state);
        state.RebuildEngagements();
    }

    /// <summary>
    /// 새 활성화 시작: 턴 시작 패시브(라운드당 1회)와 활성화 플래그 초기화. 이번 활성화의 이동력을 돌려준다.
    /// 그로기 회복으로 활성화를 잃는 경우는 SrpTurnOrder.TryConsumeActivationForGroggyRecovery가 먼저 처리한다.
    /// </summary>
    public static int BeginActivation(SrpBattleState state, SrpUnitRuntime unit, System.Action<string> log)
    {
        if (!unit.passiveAppliedThisTurn)
        {
            SrpSkills.TryApplyPassiveTurnStart(unit, state, log);
            unit.passiveAppliedThisTurn = true;
        }
        unit.hasMovedThisActivation = false;
        unit.hasAttackedThisActivation = false;
        unit.hasUsedSkillThisActivation = false;
        unit.hasReloadedThisActivation = false;
        return Mathf.Max(0, unit.moveRange);
    }

    /// <summary>
    /// 이동 1회(AP 1). 교전에서 벗어나면 기회공격을, 그 뒤 살아 있으면 경계사격을 받는다.
    /// 목적지와 비용 검증은 호출자가 한다.
    /// </summary>
    public static SrpMoveResult Move(
        SrpBattleState state,
        SrpUnitRuntime mover,
        int x,
        int y,
        int cost,
        ISrpBattleFlowObserver observer)
    {
        var result = new SrpMoveResult { cost = cost };
        bool wasEngaged = state.IsUnitEngaged(mover.id);
        var previousEngagers = state.GetEngagedEnemyIds(mover.id);

        mover.anchorX = x;
        mover.anchorY = y;
        mover.ClearCover();
        state.RebuildEngagements();
        result.disengaged = wasEngaged && !state.IsUnitEngaged(mover.id);
        mover.actionPoints = Mathf.Max(0, mover.actionPoints - 1);
        mover.hasMovedThisActivation = true;
        observer?.OnMoved(mover, cost, result.disengaged);

        if (result.disengaged)
            result.opportunity = ResolveOpportunityAttack(state, mover, previousEngagers, observer);
        if (!result.opportunity.targetDied)
            result.overwatch = ResolveOverwatchAgainst(state, mover, observer);

        result.moverDied = result.opportunity.targetDied || result.overwatch.targetDied;
        result.activationEnded = result.moverDied || mover.eliminated || mover.IsPgBroken;
        return result;
    }

    /// <summary>
    /// 기본 공격 1회(AP 1, 활성화당 1회). 탄약이 없으면 실행하지 않는다.
    /// 공격 후 공격자는 경계사격을 받을 수 있다. 공격 가능 여부(CanAttack)는 호출자가 확인한다.
    /// </summary>
    public static SrpBasicAttackResult BasicAttack(
        SrpBattleState state,
        SrpUnitRuntime attacker,
        SrpUnitRuntime defender,
        ISrpBattleFlowObserver observer)
    {
        var result = new SrpBasicAttackResult();
        var attackKind = SrpCombatResolver.ResolveBasicAttackKind(state, attacker, defender);
        if (!SrpCombatResolver.SpendAmmoForBasicAttack(attackKind, attacker))
            return result;

        result.executed = true;
        if (attackKind == SrpBasicAttackKind.Firearm)
            SrpFirearmAim.TurnShooterTowardTarget(attacker, defender);
        var outcome = SrpCombatResolver.ApplyAttack(state, attacker, defender);
        ApplyAttackPassives(state, attacker, defender, outcome, observer);
        attacker.hasAttackedThisActivation = true;
        attacker.actionPoints = Mathf.Max(0, attacker.actionPoints - 1);
        observer?.OnAttackResolved(SrpAttackSource.Basic, attacker, defender, outcome);
        result.outcome = outcome;
        result.defenderDied = RemoveIfDied(state, SrpAttackSource.Basic, defender, outcome, observer);

        result.overwatch = ResolveOverwatchAgainst(state, attacker, observer);
        result.attackerDied = result.overwatch.targetDied;
        result.activationEnded = result.attackerDied
            || attacker.eliminated
            || attacker.actionPoints <= 0
            || attacker.IsPgBroken;
        return result;
    }

    /// <summary>
    /// 대상 지정 액티브 스킬 1회(AP 1). 사용 후 시전자는 경계사격을 받을 수 있다.
    /// 사용 가능 여부와 사거리는 호출자가 확인한다.
    /// </summary>
    public static SrpSkillUseResult UseSkill(
        SrpBattleState state,
        SrpUnitRuntime caster,
        SrpSkillData data,
        SrpSkillRuntime runtime,
        int targetX,
        int targetY,
        ISrpBattleFlowObserver observer)
    {
        var result = new SrpSkillUseResult { executed = true };
        var target = state.GetOccupant(targetX, targetY);
        int hpBefore = target != null ? Mathf.Max(0, target.hp) : 0;
        int pgBefore = target != null ? Mathf.Max(0, target.pg) : 0;

        SrpSkills.ResolveActiveSkill(data, runtime, caster, targetX, targetY, state, RuleLog(observer));
        if (target != null && target != caster)
        {
            result.targetHpDamage = Mathf.Max(0, hpBefore - Mathf.Max(0, target.hp));
            result.targetPgDamage = Mathf.Max(0, pgBefore - Mathf.Max(0, target.pg));
        }
        caster.hasUsedSkillThisActivation = true;
        caster.actionPoints = Mathf.Max(0, caster.actionPoints - 1);

        result.overwatch = ResolveOverwatchAgainst(state, caster, observer);
        result.casterDied = result.overwatch.targetDied;
        result.activationEnded = result.casterDied
            || caster.eliminated
            || data.endsActivation
            || caster.IsPgBroken;
        return result;
    }

    /// <summary>재장전 1회(AP 1). 장전할 수 없으면 false이고 상태는 그대로다.</summary>
    public static bool Reload(SrpUnitRuntime unit)
    {
        if (unit == null || !unit.ReloadAmmo())
            return false;
        unit.actionPoints = Mathf.Max(0, unit.actionPoints - 1);
        unit.hasReloadedThisActivation = true;
        return true;
    }

    /// <summary>교전에서 벗어난 유닛에게, 직전에 교전하던 적 한 명이 기회공격을 한다.</summary>
    public static SrpReactionResult ResolveOpportunityAttack(
        SrpBattleState state,
        SrpUnitRuntime mover,
        List<int> previousEngagers,
        ISrpBattleFlowObserver observer)
    {
        var result = new SrpReactionResult { source = SrpAttackSource.Opportunity };
        if (mover == null || mover.eliminated || previousEngagers == null)
            return result;

        var attacker = FindOpportunityAttacker(state, mover, previousEngagers);
        if (attacker == null)
            return result;
        if (!SrpCombatResolver.TryApplyOpportunityAttack(state, attacker, mover, out var outcome))
            return result;

        return FinishReaction(state, result, attacker, mover, outcome, observer);
    }

    /// <summary>행동한 유닛에게 경계태세를 예약한 적 한 명이 경계사격을 한다.</summary>
    public static SrpReactionResult ResolveOverwatchAgainst(
        SrpBattleState state,
        SrpUnitRuntime target,
        ISrpBattleFlowObserver observer)
    {
        var result = new SrpReactionResult { source = SrpAttackSource.Overwatch };
        if (target == null || target.eliminated)
            return result;

        var watcher = SrpOverwatch.SelectTriggerWatcher(state, target);
        if (watcher == null)
            return result;
        if (!SrpOverwatch.TryTrigger(state, watcher, target, out var outcome))
            return result;

        return FinishReaction(state, result, watcher, target, outcome, observer);
    }

    public static SrpUnitRuntime FindOpportunityAttacker(
        SrpBattleState state,
        SrpUnitRuntime mover,
        List<int> previousEngagers)
    {
        foreach (int enemyId in previousEngagers)
        {
            var enemy = state.FindUnitById(enemyId);
            if (enemy == null || enemy.eliminated || enemy.owner == mover.owner)
                continue;
            if (enemy.reactionPoints <= 0 || enemy.IsPgBroken)
                continue;
            if (state.Engagements.TryGetValue(mover.id, out var current) && current.Contains(enemy.id))
                continue;
            return enemy;
        }

        return null;
    }

    /// <summary>
    /// AI가 고른 행동 하나를 검증하고 실행한다. 런타임 적 AI와 시뮬레이션이 같이 쓴다.
    /// 실행할 수 없는 행동이면 executed가 false이고 상태는 바뀌지 않는다.
    /// </summary>
    public static SrpAiStepResult ExecuteAiAction(
        SrpBattleState state,
        SrpUnitRuntime unit,
        SrpEnemyAiAction action,
        ref int remainingMove,
        ISrpBattleFlowObserver observer)
    {
        var step = new SrpAiStepResult { type = action.type };
        if (state == null || unit == null || unit.eliminated || unit.actionPoints <= 0)
            return step;

        switch (action.type)
        {
            case SrpEnemyAiActionType.Move:
            {
                var costs = SrpPathfinder.GetReachableWithCosts(state, unit, remainingMove);
                if (!costs.TryGetValue(new Vector2Int(action.x, action.y), out int moveCost))
                    return step;
                remainingMove = Mathf.Max(0, remainingMove - moveCost);
                step.move = Move(state, unit, action.x, action.y, moveCost, observer);
                step.executed = true;
                step.activationEnded = step.move.activationEnded;
                return step;
            }
            case SrpEnemyAiActionType.Attack:
            {
                var target = state.FindUnitById(action.targetUnitId);
                if (unit.hasAttackedThisActivation || target == null || target.eliminated
                    || !SrpCombatResolver.CanAttack(state, unit, target))
                    return step;
                step.attack = BasicAttack(state, unit, target, observer);
                step.executed = step.attack.executed;
                step.activationEnded = step.attack.activationEnded;
                return step;
            }
            case SrpEnemyAiActionType.Reload:
            {
                step.executed = Reload(unit);
                return step;
            }
            case SrpEnemyAiActionType.UseSkill:
            {
                var target = state.FindUnitById(action.targetUnitId);
                if (target == null || target.eliminated
                    || !TryGetUsableSkill(state, unit, action.skillId, out var data, out var runtime)
                    || state.ChebyshevAnchor(unit, target) > data.range)
                    return step;
                step.skill = UseSkill(state, unit, data, runtime, target.anchorX, target.anchorY, observer);
                step.executed = true;
                step.activationEnded = step.skill.activationEnded;
                return step;
            }
            case SrpEnemyAiActionType.Overwatch:
            {
                // 경계태세 예약은 그 활성화의 마지막 행동이다.
                step.executed = SrpOverwatch.Arm(state, unit);
                step.activationEnded = step.executed;
                return step;
            }
            default:
                return step;
        }
    }

    /// <summary>유닛이 가진 액티브 스킬을 id로 찾고 지금 쓸 수 있는지(쿨다운/충전) 확인한다.</summary>
    public static bool TryGetUsableSkill(
        SrpBattleState state,
        SrpUnitRuntime unit,
        string skillId,
        out SrpSkillData data,
        out SrpSkillRuntime runtime)
    {
        data = null;
        runtime = null;
        if (string.IsNullOrEmpty(skillId) || unit.skillRuntimes == null
            || !state.SkillLookup.TryGetValue(skillId, out data))
            return false;

        foreach (var candidate in unit.skillRuntimes)
        {
            if (candidate != null && candidate.skillId == skillId)
            {
                runtime = candidate;
                break;
            }
        }
        return runtime != null && SrpSkills.CanUseActiveSkill(data, runtime);
    }

    static SrpReactionResult FinishReaction(
        SrpBattleState state,
        SrpReactionResult result,
        SrpUnitRuntime reactor,
        SrpUnitRuntime target,
        SrpCombatResolver.AttackOutcome outcome,
        ISrpBattleFlowObserver observer)
    {
        result.triggered = true;
        result.reactor = reactor;
        result.outcome = outcome;
        ApplyAttackPassives(state, reactor, target, outcome, observer);
        observer?.OnAttackResolved(result.source, reactor, target, outcome);
        result.targetDied = RemoveIfDied(state, result.source, target, outcome, observer);
        return result;
    }

    static void ApplyAttackPassives(
        SrpBattleState state,
        SrpUnitRuntime attacker,
        SrpUnitRuntime defender,
        SrpCombatResolver.AttackOutcome outcome,
        ISrpBattleFlowObserver observer)
    {
        var log = RuleLog(observer);
        SrpSkills.OnAttackResolved(attacker, defender, outcome, state, log);
        if (outcome.damageToHp > 0 || outcome.damageToPg > 0)
            SrpSkills.OnTakeDamage(defender, state, log);
    }

    static bool RemoveIfDied(
        SrpBattleState state,
        SrpAttackSource source,
        SrpUnitRuntime defender,
        SrpCombatResolver.AttackOutcome outcome,
        ISrpBattleFlowObserver observer)
    {
        if (!outcome.defenderDied)
            return false;
        state.RemoveUnit(defender);
        state.RebuildEngagements();
        observer?.OnUnitDied(source, defender);
        return true;
    }

    static System.Action<string> RuleLog(ISrpBattleFlowObserver observer)
    {
        if (observer == null)
            return null;
        return observer.OnRuleLog;
    }
}

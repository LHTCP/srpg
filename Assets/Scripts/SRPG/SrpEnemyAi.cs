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
    /// <summary>옆의 엄폐물이나 엄폐 벽에 엄폐한다(AP 1). 이동하기 전까지 유지된다.</summary>
    TakeCover,
    /// <summary>인접한 상호작용 포인트를 활성화한다(AP 1).</summary>
    Interact,
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

    public static SrpEnemyAiAction TakeCover()
    {
        return new SrpEnemyAiAction { type = SrpEnemyAiActionType.TakeCover };
    }

    public static SrpEnemyAiAction Interact()
    {
        return new SrpEnemyAiAction { type = SrpEnemyAiActionType.Interact };
    }

    public static SrpEnemyAiAction UseSkill(string skillId, SrpUnitRuntime target)
    {
        return UseSkillAt(skillId, target.anchorX, target.anchorY, target.id);
    }

    /// <summary>칸을 대상으로 스킬을 쓴다. 범위 스킬처럼 대상 유닛이 하나로 정해지지 않으면 targetUnitId는 -1이다.</summary>
    public static SrpEnemyAiAction UseSkillAt(string skillId, int x, int y, int targetUnitId = -1)
    {
        return new SrpEnemyAiAction
        {
            type = SrpEnemyAiActionType.UseSkill,
            skillId = skillId,
            targetUnitId = targetUnitId,
            x = x,
            y = y,
        };
    }
}

/// <summary>
/// 적 진영 기본 AI. 전투 상태만 읽어 다음 행동 하나를 고르는 순수 로직이다.
/// 런타임(SrpGameController)과 AI 시뮬레이션이 같은 판단을 쓴다.
/// 범위: 기본 공격, 스킬(단일·범위 피해, 태그, 회복), 이동, 재장전, 엄폐, 총기 역할의 경계태세,
/// 맵이 켠 승리 조건(지휘관 처치, 상호작용 목표)의 공략과 방어.
/// 오버클럭, 태세/방향 변경, 승리 목표가 아닌 상호작용 포인트는 쓰지 않는다.
/// 가치 비교에 쓰는 점수는 전투 수치가 아니라 판단용 가중치다.
/// </summary>
public static class SrpEnemyAi
{
    const int ExecutionScore = 1000;
    const int AttackFromTileScore = 100;
    const int ExecuteFromTileScore = 200;
    // 이동 후 바로 승리 목표를 확보할 수 있는 칸. 처단보다 높다.
    const int ObjectiveFromTileScore = 300;
    // 우선 대상(지휘관, 목표에 닿으려는 적)을 공격할 수 있는 칸의 가산.
    const int PriorityAttackFromTileScore = 20;
    // 쓸모 있는 엄폐 옆 칸의 가산. 거리 1칸(4점)보다 작아 동점 근처에서만 영향을 준다.
    const int CoverTileScore = 3;
    // 우선 대상은 접근 거리 계산에서 이만큼 가깝게 본다.
    const int PriorityApproachDistanceBonus = 3;

    // 대상 우선순위(기본 공격·스킬 대상 고르기).
    const int CommanderTargetScore = 20;
    const int ObjectiveThreatScore = 30;
    const int CommanderGuardScore = 10;

    // 스킬/공격 가치: HP 1 = 2점, PG 1 = 1점.
    const int HpValue = 2;
    const int KillValue = 40;
    const int ReactionDebuffValue = 2;
    const int SameTargetTagScore = 3;

    /// <summary>현재 활성 유닛의 다음 행동 하나를 고른다. 상태는 변경하지 않는다.</summary>
    public static SrpEnemyAiAction Decide(
        SrpBattleState state,
        SrpUnitRuntime actor,
        int remainingMove,
        bool hasAttacked)
    {
        if (state == null || actor == null || actor.eliminated || actor.actionPoints <= 0 || actor.IsPgBroken)
            return SrpEnemyAiAction.EndTurn();

        var s = Assess(state, actor);

        // 승리 목표에 닿아 있으면 다른 무엇보다 먼저 확보한다.
        if (CanSecureObjectiveNow(s))
            return SrpEnemyAiAction.Interact();

        bool isShooter = actor.weaponClass == SrpWeaponClass.Firearm;

        if (hasAttacked)
        {
            // 공격을 마쳤으면 남은 AP로 스킬을 잇거나, 엄폐하거나, 다음 사격을 준비한다.
            if (TryPickSkillUse(s, null, out var followUp))
                return ToAction(followUp);
            // 엄폐는 움직이기 전까지 유지되므로 장전보다 먼저 한 번 해 둔다. 다음 차례에 장전 후 사격한다.
            if (CanUseCover(s))
                return SrpEnemyAiAction.TakeCover();
            if (isShooter && actor.CanReload())
                return SrpEnemyAiAction.Reload();
            return OverwatchOrEndTurn(actor, isShooter);
        }

        var target = PickAttackTarget(s);
        bool canExecute = target != null
            && target.IsPgBroken
            && state.ChebyshevAnchor(actor, target) <= 1;
        if (canExecute)
            return SrpEnemyAiAction.Attack(target.id);

        bool hasSkill = TryPickSkillUse(s, target, out var skill);
        if (target != null)
        {
            if (hasSkill)
            {
                // AP가 2면 둘 다 한다. 활성화를 끝내는 스킬은 공격 뒤로 미루고,
                // 나머지는 먼저 써서 스킬이 붙인 태그를 이어지는 공격에 싣는다.
                if (actor.actionPoints >= 2)
                    return skill.data.endsActivation ? SrpEnemyAiAction.Attack(target.id) : ToAction(skill);
                // AP가 1뿐이면 더 가치 있는 쪽 하나만 한다.
                if (skill.value > EstimateAttackValue(state, actor, target))
                    return ToAction(skill);
            }
            return SrpEnemyAiAction.Attack(target.id);
        }

        // 여기부터는 지금 자리에서 공격할 수 없는 경우다.
        // 급한 회복(HP 절반 이하, PG 1/3 이하)은 장전·이동보다 먼저 한다.
        if (hasSkill && skill.urgent)
            return ToAction(skill);

        // 주무장이 총기인 유닛은 빈 총으로 돌격하기보다 먼저 장전한다.
        if (isShooter && actor.CanReload() && actor.ammo <= 0)
            return SrpEnemyAiAction.Reload();

        int moveX = actor.anchorX;
        int moveY = actor.anchorY;
        bool moveEnablesAction = false;
        bool canMove = !actor.hasMovedThisActivation && remainingMove > 0
            && TryPickMove(s, remainingMove, out moveX, out moveY, out moveEnablesAction);

        // 이동해서 바로 공격하거나 목표를 확보할 수 있으면 스킬보다 이동이 먼저다.
        if (canMove && moveEnablesAction)
            return SrpEnemyAiAction.MoveTo(moveX, moveY);
        if (hasSkill)
            return ToAction(skill);
        if (canMove)
            return SrpEnemyAiAction.MoveTo(moveX, moveY);

        if (actor.CanReload() && actor.ammo <= 0)
            return SrpEnemyAiAction.Reload();

        return IdleAction(s, isShooter);
    }

    // ── 상황 파악 ────────────────────────────────────────────────────────────

    /// <summary>판단 한 번에 쓰는 전장 요약. 승리 조건이 꺼진 맵에서는 관련 목록이 모두 비어 있다.</summary>
    sealed class Situation
    {
        public SrpBattleState state;
        public SrpUnitRuntime actor;
        public bool commanderRule;
        public bool mapHasCover;
        /// <summary>actor 진영이 아직 확보하지 못한 승리 목표.</summary>
        public readonly List<SrpInteractionPointData> openObjectives = new List<SrpInteractionPointData>();
        /// <summary>다음 차례에 자기 승리 목표에 닿을 수 있는 적.</summary>
        public readonly HashSet<int> objectiveThreats = new HashSet<int>();
        /// <summary>actor를 뺀 아군 지휘관.</summary>
        public readonly List<SrpUnitRuntime> alliedCommanders = new List<SrpUnitRuntime>();
        /// <summary>자리를 지킨다: 이번 차례에 바로 행동으로 이어지지 않는 접근 이동을 하지 않는다.</summary>
        public bool holdPosition;
    }

    static Situation Assess(SrpBattleState state, SrpUnitRuntime actor)
    {
        var s = new Situation { state = state, actor = actor };
        var rule = state.VictoryRule;
        s.commanderRule = rule != null && rule.commanderKill;
        s.mapHasCover = (state.CoverObjects != null && state.CoverObjects.Count > 0)
            || (state.CoverSegments != null && state.CoverSegments.Count > 0);

        if (rule != null && rule.interactionObjective && state.InteractionPoints != null)
        {
            foreach (var point in state.InteractionPoints)
            {
                if (point == null || point.effect != SrpInteractionEffect.VictoryObjective)
                    continue;
                if (IsOpenObjectiveFor(point, actor.owner))
                    s.openObjectives.Add(point);

                // 상대가 쓸 수 있는 목표라면, 다음 이동으로 그 옆에 설 수 있는 적을 위협으로 본다.
                // 장애물을 무시한 직선(맨해튼) 거리로 어림한다.
                foreach (var unit in state.Units)
                {
                    if (unit == null || unit.eliminated || unit.owner == actor.owner)
                        continue;
                    if (!IsOpenObjectiveFor(point, unit.owner))
                        continue;
                    int stepsToAdjacent = Mathf.Abs(unit.anchorX - point.x) + Mathf.Abs(unit.anchorY - point.y) - 1;
                    if (stepsToAdjacent <= unit.moveRange)
                        s.objectiveThreats.Add(unit.id);
                }
            }
        }

        if (s.commanderRule)
        {
            bool hasOtherAlly = false;
            foreach (var unit in state.Units)
            {
                if (unit == null || unit.eliminated || unit.owner != actor.owner || unit == actor)
                    continue;
                hasOtherAlly = true;
                if (unit.isCommander)
                    s.alliedCommanders.Add(unit);
            }
            // 지휘관은 쓰러지면 패배하므로 아군이 남아 있는 동안은 앞으로 나서지 않는다.
            // 혼자 남으면 고착을 피하려고 평소처럼 움직인다.
            s.holdPosition = actor.isCommander && hasOtherAlly;
        }

        return s;
    }

    /// <summary>owner가 지금 활성화해서 승리에 보탤 수 있는 목표인가.</summary>
    static bool IsOpenObjectiveFor(SrpInteractionPointData point, int owner)
    {
        if (!SrpVictory.IsObjectiveFor(point, owner))
            return false;
        if (!point.activated)
            return true;
        // 이미 활성화된 1회용 포인트는 다시 쓸 수 없고, 자기 진영이 보유 중이면 더 할 일이 없다.
        return !point.singleUse && point.owner != owner;
    }

    static bool CanSecureObjectiveNow(Situation s)
    {
        if (s.openObjectives.Count == 0)
            return false;
        // 실행(TryResolveInteractionAction)이 고르는 포인트와 같은 것을 본다.
        return s.state.TryGetAdjacentInteraction(s.actor, out var point)
            && s.openObjectives.Contains(point);
    }

    /// <summary>대상 우선순위 가산: 적 지휘관, 승리 목표에 닿으려는 적, 아군 지휘관에 붙은 적.</summary>
    static int TargetPriority(Situation s, SrpUnitRuntime unit)
    {
        int bonus = 0;
        if (s.commanderRule && unit.isCommander)
            bonus += CommanderTargetScore;
        if (s.objectiveThreats.Contains(unit.id))
            bonus += ObjectiveThreatScore;
        foreach (var commander in s.alliedCommanders)
        {
            if (s.state.ChebyshevAnchor(unit, commander) <= 1)
            {
                bonus += CommanderGuardScore;
                break;
            }
        }
        return bonus;
    }

    // ── 대기 행동 ────────────────────────────────────────────────────────────

    /// <summary>
    /// 더 할 행동이 없을 때. 옆에 쓸모 있는 엄폐가 있으면 엄폐하고, 총기 역할은 경계태세를 예약한다.
    /// AP가 1뿐인 총기 역할은 경계태세가 먼저다(엄폐는 AP가 남을 때).
    /// </summary>
    static SrpEnemyAiAction IdleAction(Situation s, bool isShooter)
    {
        bool canOverwatch = isShooter && SrpOverwatch.CanArm(s.actor);
        if ((!canOverwatch || s.actor.actionPoints >= 2) && CanUseCover(s))
            return SrpEnemyAiAction.TakeCover();
        return OverwatchOrEndTurn(s.actor, isShooter);
    }

    /// <summary>
    /// 총기 역할 유닛은 남은 AP로 경계태세를 예약한다.
    /// 근접/마법 역할은 반응(RP)을 패링·가드에 남겨 두도록 경계태세를 쓰지 않는다.
    /// </summary>
    static SrpEnemyAiAction OverwatchOrEndTurn(SrpUnitRuntime actor, bool isShooter)
    {
        return isShooter && SrpOverwatch.CanArm(actor)
            ? SrpEnemyAiAction.Overwatch()
            : SrpEnemyAiAction.EndTurn();
    }

    // ── 엄폐 ─────────────────────────────────────────────────────────────────

    static bool CanUseCover(Situation s)
    {
        return SrpBattleFlow.CanTakeCover(s.state, s.actor) && IsCoverUsefulHere(s);
    }

    /// <summary>
    /// 지금 서 있는 칸에서 엄폐하면 사격해 올 수 있는 적 중 누구에게든 피해를 줄일 수 있는가.
    /// 엄폐 상태를 잠시 바꿔 실제 피해 규칙(SrpCombatResolver)으로 확인하고 되돌린다.
    /// </summary>
    static bool IsCoverUsefulHere(Situation s)
    {
        var state = s.state;
        var actor = s.actor;
        if (!s.mapHasCover || !state.TryGetAdjacentCover(actor, out int coverX, out int coverY))
            return false;

        bool wasActive = actor.coverActive;
        int wasRound = actor.coverRound;
        int wasSourceX = actor.coverSourceX;
        int wasSourceY = actor.coverSourceY;
        try
        {
            actor.SetCover(state.RoundNumber, coverX, coverY);
            foreach (var enemy in state.Units)
            {
                if (enemy == null || enemy.eliminated || enemy.owner == actor.owner)
                    continue;
                if (enemy.maxAmmo <= 0 || enemy.attackRange <= 1)
                    continue;
                // 한 번 움직여 쏠 수 있는 거리 안의 적만 본다.
                int distance = state.ChebyshevAnchor(enemy, actor);
                if (distance <= 1 || distance > enemy.attackRange + enemy.moveRange)
                    continue;
                if (SrpCombatResolver.HasCoverAgainst(state, enemy, actor))
                    return true;
            }
            return false;
        }
        finally
        {
            actor.coverActive = wasActive;
            actor.coverRound = wasRound;
            actor.coverSourceX = wasSourceX;
            actor.coverSourceY = wasSourceY;
        }
    }

    // ── 기본 공격 대상 ───────────────────────────────────────────────────────

    /// <summary>지금 자리에서 기본 공격할 대상. 처단 가능 대상을 최우선으로 한다.</summary>
    public static SrpUnitRuntime PickAttackTarget(SrpBattleState state, SrpUnitRuntime actor)
    {
        if (state == null || actor == null)
            return null;
        return PickAttackTarget(Assess(state, actor));
    }

    static SrpUnitRuntime PickAttackTarget(Situation s)
    {
        var state = s.state;
        var actor = s.actor;
        SrpUnitRuntime best = null;
        int bestScore = int.MinValue;
        foreach (var unit in state.Units)
        {
            if (unit == null || unit.eliminated || unit.owner == actor.owner)
                continue;
            if (!SrpCombatResolver.CanAttack(state, actor, unit))
                continue;

            int score = ScoreAttackTarget(s, unit);
            if (score > bestScore || (score == bestScore && best != null && unit.id < best.id))
            {
                bestScore = score;
                best = unit;
            }
        }
        return best;
    }

    static int ScoreAttackTarget(Situation s, SrpUnitRuntime target)
    {
        int score = (target.maxHp - target.hp) + (target.maxPg - target.pg);
        bool adjacent = s.state.ChebyshevAnchor(s.actor, target) <= 1;
        if (adjacent && target.IsPgBroken)
            score += ExecutionScore;
        if (target.weaponClass == SrpWeaponClass.Firearm)
            score += 5;
        score += TargetPriority(s, target);
        return score;
    }

    /// <summary>기본 공격 한 번의 가치(방어·반응 적용 전 기준 피해). AP가 1일 때 스킬과 비교하는 데 쓴다.</summary>
    static int EstimateAttackValue(SrpBattleState state, SrpUnitRuntime actor, SrpUnitRuntime target)
    {
        var kind = SrpCombatResolver.ResolveBasicAttackKind(state, actor, target);
        SrpCombatResolver.GetBaseBasicAttackDamage(kind, actor, out int hpDamage, out int pgDamage);
        if (kind == SrpBasicAttackKind.Firearm)
            pgDamage += hpDamage * SrpCombatResolver.FirearmSpilloverPercent / 100;

        int value = hpDamage * HpValue + pgDamage;
        if (hpDamage >= target.hp)
            value += KillValue;
        return value;
    }

    // ── 스킬 ─────────────────────────────────────────────────────────────────

    struct SkillPick
    {
        public SrpSkillData data;
        public int x;
        public int y;
        /// <summary>단일 대상 스킬의 대상. 범위 스킬은 -1.</summary>
        public int targetUnitId;
        /// <summary>스킬 효과의 가치. 기본 공격 가치와 비교한다.</summary>
        public int value;
        /// <summary>대상 고르기용 점수. value에 "다친 대상 우선"과 대상 우선순위를 더한 값.</summary>
        public int score;
        public bool urgent;
    }

    static SrpEnemyAiAction ToAction(SkillPick pick)
    {
        return SrpEnemyAiAction.UseSkillAt(pick.data.id, pick.x, pick.y, pick.targetUnitId);
    }

    /// <summary>
    /// 지금 자리에서 쓸 수 있는 스킬 중 가장 가치 있는 사용처 하나를 고른다(스킬은 활성화당 1회).
    /// 피해(단일·범위), 적에게 붙이는 태그, 반응 약화, HP/PG 회복을 같은 척도로 견준다.
    /// 그 밖의 강화/약화 효과는 값을 매기지 않아, 그런 효과만 있는 스킬은 쓰지 않는다.
    /// </summary>
    static bool TryPickSkillUse(Situation s, SrpUnitRuntime attackTarget, out SkillPick best)
    {
        best = default;
        var state = s.state;
        var actor = s.actor;
        if (actor.hasUsedSkillThisActivation || actor.skillRuntimes == null)
            return false;

        bool found = false;
        foreach (var runtime in actor.skillRuntimes)
        {
            if (runtime == null || !state.SkillLookup.TryGetValue(runtime.skillId, out var data))
                continue;
            if (!SrpSkills.CanUseActiveSkill(data, runtime))
                continue;

            switch (data.targetType)
            {
                case SrpTargetType.Self:
                    ConsiderSkillAt(s, data, actor.anchorX, actor.anchorY, attackTarget, ref best, ref found);
                    break;

                case SrpTargetType.SingleEnemy:
                case SrpTargetType.SingleAlly:
                    foreach (var unit in state.Units)
                    {
                        if (unit == null || unit.eliminated)
                            continue;
                        if ((unit.owner != actor.owner) != (data.targetType == SrpTargetType.SingleEnemy))
                            continue;
                        if (!SrpSkills.CanTargetTile(data, actor, unit.anchorX, unit.anchorY, state))
                            continue;
                        ConsiderSkillAt(s, data, unit.anchorX, unit.anchorY, attackTarget, ref best, ref found);
                    }
                    break;

                case SrpTargetType.AreaEnemy:
                case SrpTargetType.AreaAlly:
                    foreach (var tile in SrpSkills.GetSkillTargetTiles(data, actor, state))
                        ConsiderSkillAt(s, data, tile.x, tile.y, attackTarget, ref best, ref found);
                    break;
            }
        }
        return found;
    }

    /// <summary>대상으로 고를 수 있는 칸(SrpSkills.CanTargetTile)에 스킬을 썼을 때를 평가해, 더 나으면 best를 바꾼다.</summary>
    static void ConsiderSkillAt(
        Situation s,
        SrpSkillData data,
        int x,
        int y,
        SrpUnitRuntime attackTarget,
        ref SkillPick best,
        ref bool found)
    {
        if (!TryEvaluateSkillAt(s, data, x, y, attackTarget, out var pick))
            return;

        bool better = !found || pick.score > best.score;
        // 같은 스킬의 동점은 기존 규칙대로 id가 낮은 대상(범위는 앞선 칸)을 고른다. 스킬끼리 동점이면 먼저 가진 스킬을 쓴다.
        if (!better && pick.score == best.score && pick.data == best.data)
        {
            better = pick.targetUnitId >= 0 && best.targetUnitId >= 0
                ? pick.targetUnitId < best.targetUnitId
                : IsEarlierTile(new Vector2Int(pick.x, pick.y), best.x, best.y);
        }
        if (!better)
            return;
        best = pick;
        found = true;
    }

    static bool TryEvaluateSkillAt(
        Situation s,
        SrpSkillData data,
        int x,
        int y,
        SrpUnitRuntime attackTarget,
        out SkillPick pick)
    {
        pick = new SkillPick { data = data, x = x, y = y, targetUnitId = -1 };
        var affected = SrpSkills.GetSkillAffectedUnits(data, s.actor, x, y, s.state);
        if (affected.Count == 0 || data.effects == null)
            return false;
        if (!SrpSkills.IsAreaSkill(data))
            pick.targetUnitId = affected[0].id;

        int supportValue = 0;
        bool supportUrgent = false;
        foreach (var unit in affected)
        {
            if (unit == null || unit.eliminated)
                continue;
            if (unit.owner != s.actor.owner)
                AddOffensiveValue(s, data, unit, attackTarget, ref pick);
            else
                AddSupportValue(data, unit, ref supportValue, ref supportUrgent);
        }

        // 회복은 한 명 기준 회복량의 3/4 이상이 실제로 들어갈 때만 쓴다(쿨다운·충전 낭비 방지).
        if (supportValue > 0 && supportValue * 4 >= GetSupportNominalValue(data) * 3)
        {
            pick.value += supportValue;
            pick.score += supportValue;
            pick.urgent = supportUrgent;
        }
        return pick.value > 0;
    }

    static void AddOffensiveValue(
        Situation s,
        SrpSkillData data,
        SrpUnitRuntime unit,
        SrpUnitRuntime attackTarget,
        ref SkillPick pick)
    {
        int value = 0;
        bool dealsDamage = false;
        foreach (var eff in data.effects)
        {
            if (eff == null)
                continue;
            switch (eff.type)
            {
                case SrpEffectType.Damage:
                case SrpEffectType.Cleave:
                {
                    // 실제 해결(SrpSkills.ApplyEffects)과 같은 기준 피해: HP = 값, PG = 값/2(최소 1).
                    int damage = eff.type == SrpEffectType.Cleave ? s.actor.attackPower + eff.value : eff.value;
                    if (damage <= 0)
                        break;
                    value += damage * HpValue + Mathf.Max(1, damage / 2);
                    if (damage >= unit.hp)
                        value += KillValue;
                    dealsDamage = true;
                    break;
                }
                case SrpEffectType.ApplyCombatTag:
                {
                    if (eff.stat == "self")
                        break;
                    if (!SrpCombatTagUtility.TryParse(eff.stat, out var tag)
                        && !SrpCombatTagUtility.TryParse(eff.value.ToString(), out tag))
                        break;
                    if (!unit.HasCombatTag(tag))
                        value += GetTagValue(tag, unit);
                    break;
                }
                case SrpEffectType.DebuffStat:
                {
                    if ((eff.stat == "rp" || eff.stat == "reactionPoints") && eff.value > 0 && unit.reactionPoints > 0)
                        value += ReactionDebuffValue;
                    break;
                }
            }
        }

        if (value <= 0)
            return;

        pick.value += value;
        int score = value + TargetPriority(s, unit);
        if (dealsDamage)
            score += (unit.maxHp - unit.hp) * 2 + (unit.maxPg - unit.pg);
        else if (attackTarget != null && unit.id == attackTarget.id)
            score += SameTargetTagScore;
        pick.score += score;
    }

    /// <summary>태그가 다음 공격에 더해 주는 피해의 가치(SrpCombatResolver의 태그 보너스와 같은 크기). 붕괴한 대상에게 PG 보너스는 의미가 없다.</summary>
    static int GetTagValue(SrpCombatTag tag, SrpUnitRuntime unit)
    {
        switch (tag)
        {
            case SrpCombatTag.Marked:
                return unit.IsPgBroken ? 0 : 2;
            case SrpCombatTag.BalanceBroken:
                return unit.IsPgBroken ? 0 : 4;
            case SrpCombatTag.KillOrder:
                return 2 * HpValue + (unit.IsPgBroken ? 0 : 2);
            default:
                return 0;
        }
    }

    static void AddSupportValue(SrpSkillData data, SrpUnitRuntime unit, ref int supportValue, ref bool urgent)
    {
        foreach (var eff in data.effects)
        {
            if (eff == null || eff.value <= 0)
                continue;
            bool healsHp = eff.type == SrpEffectType.Heal
                || (eff.type == SrpEffectType.BuffStat && eff.stat == "hp");
            bool restoresPg = eff.type == SrpEffectType.BuffStat && (eff.stat == "pg" || eff.stat == "posture");
            if (healsHp)
            {
                int amount = Mathf.Min(eff.value, unit.maxHp - unit.hp);
                if (amount <= 0)
                    continue;
                supportValue += amount * HpValue;
                if (unit.hp * 2 <= unit.maxHp)
                    urgent = true;
            }
            else if (restoresPg && !unit.IsPgBroken)
            {
                // 붕괴(그로기)한 유닛은 PG를 채워도 그로기가 풀리지 않는다.
                int amount = Mathf.Min(eff.value, unit.maxPg - unit.pg);
                if (amount <= 0)
                    continue;
                supportValue += amount;
                if (unit.pg * 3 <= unit.maxPg)
                    urgent = true;
            }
        }
    }

    /// <summary>회복 효과가 한 명에게 온전히 들어갔을 때의 가치.</summary>
    static int GetSupportNominalValue(SrpSkillData data)
    {
        int nominal = 0;
        foreach (var eff in data.effects)
        {
            if (eff == null || eff.value <= 0)
                continue;
            if (eff.type == SrpEffectType.Heal || (eff.type == SrpEffectType.BuffStat && eff.stat == "hp"))
                nominal += eff.value * HpValue;
            else if (eff.type == SrpEffectType.BuffStat && (eff.stat == "pg" || eff.stat == "posture"))
                nominal += eff.value;
        }
        return nominal;
    }

    // ── 이동 ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 이동할 칸을 고른다. enablesAction은 그 칸에서 남은 AP로 바로 공격하거나 승리 목표를 확보할 수 있다는 뜻이다.
    /// </summary>
    static bool TryPickMove(Situation s, int remainingMove, out int moveX, out int moveY, out bool enablesAction)
    {
        var state = s.state;
        var actor = s.actor;
        moveX = actor.anchorX;
        moveY = actor.anchorY;
        enablesAction = false;
        var costs = SrpPathfinder.GetReachableWithCosts(state, actor, remainingMove);
        if (costs.Count == 0)
            return false;

        // 이동에 AP 1을 쓰고도 행동할 AP가 남을 때만 "그 칸에서 공격/목표 확보 가능"을 가산한다.
        bool canActAfterMove = actor.actionPoints >= 2;
        int originX = actor.anchorX;
        int originY = actor.anchorY;
        int currentScore = ScoreTile(s, originX, originY, 0, false, out _);
        int bestScore = currentScore;
        bool found = false;
        bool bestEnablesAction = false;

        foreach (var kv in costs)
        {
            int score = ScoreTile(s, kv.Key.x, kv.Key.y, kv.Value, canActAfterMove, out bool tileEnablesAction);
            if (score > bestScore
                || (found && score == bestScore && IsEarlierTile(kv.Key, moveX, moveY)))
            {
                bestScore = score;
                moveX = kv.Key.x;
                moveY = kv.Key.y;
                found = true;
                bestEnablesAction = tileEnablesAction;
            }
        }

        actor.anchorX = originX;
        actor.anchorY = originY;

        // 이동 직후 공격하거나 목표를 확보할 수 있는 칸을 골랐으면 그대로 쓴다.
        if (found && bestEnablesAction)
        {
            enablesAction = true;
            return true;
        }

        // 자리를 지키는 유닛(지휘관)은 바로 행동으로 이어지지 않는 접근 이동을 하지 않는다.
        if (s.holdPosition)
        {
            moveX = originX;
            moveY = originY;
            return false;
        }

        // 위 점수는 직선(맨해튼) 거리 기준이라 장애물을 돌아가야 하는 길을 찾지 못한다.
        // 고른 칸이 없거나 그 칸이 목표 칸까지의 실제 경로 비용을 늘리면, 최단 경로를 따라 우회한다.
        int unlimited = state.Width * state.Height * 3;
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var pathCosts = SrpPathfinder.GetReachableWithCosts(state, actor, unlimited, cameFrom);
        var goalTiles = CollectGoalTiles(s, pathCosts);
        // 경로로 닿는 목표 칸이 없으면(완전히 막힘, 아군이 자리를 다 차지함) 직선 거리 판단을 유지한다.
        if (goalTiles.Count == 0)
            return found;

        var goal = default(Vector2Int);
        int goalCost = int.MaxValue;
        foreach (var tile in goalTiles)
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
            if (goalTiles.Contains(picked)
                || RemainingPathCost(state, actor, moveX, moveY, goalTiles, unlimited) <= goalCost)
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

    /// <summary>
    /// 경로로 닿는 칸 중 그 자리에서 기본 공격을 하거나 승리 목표를 확보할 수 있는 칸.
    /// 유닛 위치는 원래대로 되돌린다.
    /// </summary>
    static HashSet<Vector2Int> CollectGoalTiles(Situation s, Dictionary<Vector2Int, int> pathCosts)
    {
        var state = s.state;
        var actor = s.actor;
        var tiles = new HashSet<Vector2Int>();
        int originX = actor.anchorX;
        int originY = actor.anchorY;
        try
        {
            foreach (var kv in pathCosts)
            {
                actor.anchorX = kv.Key.x;
                actor.anchorY = kv.Key.y;
                if (CanInteractWithOpenObjective(s))
                {
                    tiles.Add(kv.Key);
                    continue;
                }
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

    static bool CanInteractWithOpenObjective(Situation s)
    {
        foreach (var point in s.openObjectives)
        {
            if (s.state.CanUnitInteractWith(s.actor, point))
                return true;
        }
        return false;
    }

    /// <summary>(x, y)에 섰다고 가정했을 때 가장 가까운 목표 칸까지의 경로 비용. 닿지 않으면 int.MaxValue.</summary>
    static int RemainingPathCost(
        SrpBattleState state,
        SrpUnitRuntime actor,
        int x,
        int y,
        HashSet<Vector2Int> goalTiles,
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
        foreach (var tile in goalTiles)
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
        Situation s,
        int x,
        int y,
        int moveCost,
        bool countOpportunity,
        out bool enablesAction)
    {
        var state = s.state;
        var actor = s.actor;
        int originX = actor.anchorX;
        int originY = actor.anchorY;
        actor.anchorX = x;
        actor.anchorY = y;

        int nearest = int.MaxValue;
        bool canAttack = false;
        bool canExecute = false;
        bool canAttackPriority = false;
        bool canSecure = false;
        bool coverUseful;
        try
        {
            foreach (var unit in state.Units)
            {
                if (unit == null || unit.eliminated || unit.owner == actor.owner)
                    continue;

                int dist = Mathf.Abs(unit.anchorX - x) + Mathf.Abs(unit.anchorY - y);
                // 붕괴한 적은 처단 기회이므로 더 가깝게 평가한다.
                if (unit.IsPgBroken)
                    dist -= 2;
                bool priority = TargetPriority(s, unit) > 0;
                if (priority)
                    dist -= PriorityApproachDistanceBonus;
                if (dist < nearest)
                    nearest = dist;

                if (!countOpportunity || !SrpCombatResolver.CanAttack(state, actor, unit))
                    continue;
                canAttack = true;
                if (priority)
                    canAttackPriority = true;
                if (unit.IsPgBroken && state.ChebyshevAnchor(actor, unit) <= 1)
                    canExecute = true;
            }

            // 아직 확보하지 못한 승리 목표도 접근 대상이다. 옆 칸에 서면 거리 0으로 본다.
            foreach (var point in s.openObjectives)
            {
                int dist = Mathf.Max(0, Mathf.Abs(point.x - x) + Mathf.Abs(point.y - y) - 1);
                if (dist < nearest)
                    nearest = dist;
                if (countOpportunity && state.CanUnitInteractWith(actor, point))
                    canSecure = true;
            }

            coverUseful = IsCoverUsefulHere(s);
        }
        finally
        {
            actor.anchorX = originX;
            actor.anchorY = originY;
        }

        if (nearest == int.MaxValue)
            nearest = 0;
        int score = -nearest * 4 - moveCost;
        if (canAttack)
            score += AttackFromTileScore;
        if (canExecute)
            score += ExecuteFromTileScore;
        if (canAttackPriority)
            score += PriorityAttackFromTileScore;
        if (canSecure)
            score += ObjectiveFromTileScore;
        if (coverUseful)
            score += CoverTileScore;
        enablesAction = canAttack || canSecure;
        return score;
    }
}

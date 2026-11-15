using System.Collections.Generic;

public enum SrpVictoryReason
{
    None,
    /// <summary>한 진영만 남았다.</summary>
    Annihilation,
    /// <summary>상대 지휘관이 모두 쓰러졌다.</summary>
    CommanderKilled,
    /// <summary>승리 목표 포인트를 모두 확보했다.</summary>
    ObjectiveSecured,
    /// <summary>남은 진영이 없다.</summary>
    Draw,
    /// <summary>맵이 추가로 건 조건(victory.conditions). 어느 조건인지는 SrpVictoryResult.conditionType에 있다.</summary>
    Custom,
}

public struct SrpVictoryResult
{
    public bool decided;
    /// <summary>승리 진영. 무승부거나 미결이면 -1.</summary>
    public int winnerOwner;
    public SrpVictoryReason reason;
    /// <summary>reason이 Custom일 때 승패를 가른 조건의 type. 그 밖에는 null.</summary>
    public string conditionType;

    public static SrpVictoryResult Undecided()
    {
        return new SrpVictoryResult { decided = false, winnerOwner = -1, reason = SrpVictoryReason.None };
    }
}

/// <summary>
/// 승리 조건 한 종류의 판정기. 지휘관 처치와 목표 확보도 이 형태로 구현돼 있다.
/// 새 조건을 추가하려면 이 인터페이스를 구현해 SrpVictory.Register로 등록하고,
/// 맵의 victory.conditions에 같은 type의 항목을 넣는다. SrpVictory.Evaluate는 고치지 않는다.
/// </summary>
public interface ISrpVictoryCondition
{
    /// <summary>맵 데이터(victory.conditions[].type)와 맞추는 이름.</summary>
    string Type { get; }

    /// <summary>HUD와 로그에 쓰는 한 줄 설명. 이 전투에 해당 사항이 없으면 null.</summary>
    string Describe(SrpBattleState state, SrpVictoryConditionData data);

    /// <summary>owner가 이 조건으로 패배했는가.</summary>
    bool IsDefeated(SrpBattleState state, SrpVictoryConditionData data, int owner);

    /// <summary>owner가 이 조건으로 승리했는가.</summary>
    bool HasWon(SrpBattleState state, SrpVictoryConditionData data, int owner);
}

/// <summary>
/// 승패 판정(순수 로직). 런타임(SrpGameController)과 AI 시뮬레이션이 같은 판정을 쓴다.
/// 적 전멸은 항상 적용하고, 나머지 조건은 맵의 victory 설정에 있는 것만 적용한다.
/// 판정 순서: 전멸 > 패배 조건(지휘관 처치, 추가 조건) > 승리 조건(목표 확보, 추가 조건).
/// 같은 단계 안에서는 지휘관 처치·목표 확보가 먼저고 추가 조건은 맵에 적힌 순서다.
/// </summary>
public static class SrpVictory
{
    static readonly ISrpVictoryCondition CommanderKill = new CommanderKillCondition();
    static readonly ISrpVictoryCondition Objective = new ObjectiveCondition();
    static readonly Dictionary<string, ISrpVictoryCondition> Registered = new Dictionary<string, ISrpVictoryCondition>();

    struct ActiveCondition
    {
        public ISrpVictoryCondition condition;
        public SrpVictoryConditionData data;

        /// <summary>data.owner가 정해져 있으면 그 진영에만 적용한다.</summary>
        public bool AppliesTo(int owner)
        {
            return data == null || data.owner < 0 || data.owner == owner;
        }
    }

    /// <summary>
    /// 추가 승리 조건을 등록한다. 같은 type이 이미 있으면 바꿔 끼운다.
    /// type이 비어 있으면 등록하지 않고 false를 돌려준다.
    /// </summary>
    public static bool Register(ISrpVictoryCondition condition)
    {
        if (condition == null || string.IsNullOrEmpty(condition.Type))
            return false;
        Registered[condition.Type] = condition;
        return true;
    }

    public static bool Unregister(string type)
    {
        return !string.IsNullOrEmpty(type) && Registered.Remove(type);
    }

    public static SrpVictoryResult Evaluate(SrpBattleState state)
    {
        if (state == null || state.Units == null)
            return SrpVictoryResult.Undecided();

        var aliveOwners = new List<int>();
        foreach (var unit in state.Units)
        {
            if (unit == null || unit.eliminated)
                continue;
            if (!aliveOwners.Contains(unit.owner))
                aliveOwners.Add(unit.owner);
        }
        aliveOwners.Sort();

        if (aliveOwners.Count == 0)
            return new SrpVictoryResult { decided = true, winnerOwner = -1, reason = SrpVictoryReason.Draw };

        var active = GetActiveConditions(state.VictoryRule);
        var contenders = new List<int>(aliveOwners);
        bool anyDefeat = false;
        var firstDefeat = default(ActiveCondition);
        foreach (var entry in active)
        {
            for (int i = contenders.Count - 1; i >= 0; i--)
            {
                int owner = contenders[i];
                if (!entry.AppliesTo(owner) || !entry.condition.IsDefeated(state, entry.data, owner))
                    continue;
                contenders.RemoveAt(i);
                if (!anyDefeat)
                {
                    anyDefeat = true;
                    firstDefeat = entry;
                }
            }
        }

        if (contenders.Count == 0)
            return new SrpVictoryResult { decided = true, winnerOwner = -1, reason = SrpVictoryReason.Draw };

        if (contenders.Count == 1)
        {
            if (aliveOwners.Count == 1 || !anyDefeat)
                return new SrpVictoryResult { decided = true, winnerOwner = contenders[0], reason = SrpVictoryReason.Annihilation };
            return Won(contenders[0], firstDefeat.condition);
        }

        foreach (var entry in active)
        {
            foreach (int owner in contenders)
            {
                if (entry.AppliesTo(owner) && entry.condition.HasWon(state, entry.data, owner))
                    return Won(owner, entry.condition);
            }
        }

        return SrpVictoryResult.Undecided();
    }

    /// <summary>지휘관이 한 명이라도 있던 진영이 지휘관을 모두 잃었는가. 지휘관이 없던 진영은 해당 없음.</summary>
    public static bool HasLostAllCommanders(SrpBattleState state, int owner)
    {
        bool hadCommander = false;
        foreach (var unit in state.Units)
        {
            if (unit == null || unit.owner != owner || !unit.isCommander)
                continue;
            hadCommander = true;
            if (!unit.eliminated)
                return false;
        }
        return hadCommander;
    }

    /// <summary>owner가 쓸 수 있는 승리 목표 포인트가 하나 이상 있고, 전부 owner가 활성화해 보유 중인가.</summary>
    public static bool HasSecuredAllObjectives(SrpBattleState state, int owner)
    {
        if (state.InteractionPoints == null)
            return false;

        int total = 0;
        foreach (var point in state.InteractionPoints)
        {
            if (!IsObjectiveFor(point, owner))
                continue;
            total++;
            if (!point.activated || point.owner != owner)
                return false;
        }
        return total > 0;
    }

    public static bool IsObjectiveFor(SrpInteractionPointData point, int owner)
    {
        return point != null
            && point.effect == SrpInteractionEffect.VictoryObjective
            && (point.requiredOwner < 0 || point.requiredOwner == owner);
    }

    /// <summary>이 전투에 걸린 승리 조건을 한 줄로 설명한다(HUD/로그용).</summary>
    public static string DescribeRule(SrpBattleState state)
    {
        var parts = new List<string> { "적 전멸" };
        if (state != null)
        {
            foreach (var entry in GetActiveConditions(state.VictoryRule))
            {
                string text = entry.condition.Describe(state, entry.data);
                if (!string.IsNullOrEmpty(text))
                    parts.Add(text);
            }
        }
        return string.Join(" / ", parts);
    }

    /// <summary>판정 결과의 이유를 설명한다. 추가 조건으로 끝났으면 그 조건의 설명을 쓴다.</summary>
    public static string DescribeResult(SrpBattleState state, SrpVictoryResult result)
    {
        if (result.reason != SrpVictoryReason.Custom)
            return DescribeReason(result.reason);

        if (state != null)
        {
            foreach (var entry in GetActiveConditions(state.VictoryRule))
            {
                if (entry.condition.Type != result.conditionType)
                    continue;
                string text = entry.condition.Describe(state, entry.data);
                if (!string.IsNullOrEmpty(text))
                    return text;
            }
        }
        return string.IsNullOrEmpty(result.conditionType) ? DescribeReason(result.reason) : result.conditionType;
    }

    public static string DescribeReason(SrpVictoryReason reason)
    {
        switch (reason)
        {
            case SrpVictoryReason.Annihilation:
                return "적 전멸";
            case SrpVictoryReason.CommanderKilled:
                return "지휘관 처치";
            case SrpVictoryReason.ObjectiveSecured:
                return "목표 지점 확보";
            case SrpVictoryReason.Draw:
                return "무승부";
            case SrpVictoryReason.Custom:
                return "맵 조건 달성";
            default:
                return "미결";
        }
    }

    /// <summary>
    /// 이 맵에 걸린 조건을 판정 순서대로 모은다.
    /// 등록되지 않은 type은 건너뛴다(그 조건만 빠지고 나머지 판정은 그대로 한다).
    /// </summary>
    static List<ActiveCondition> GetActiveConditions(SrpVictoryRuleData rule)
    {
        var active = new List<ActiveCondition>();
        if (rule == null)
            return active;

        if (rule.commanderKill)
            active.Add(new ActiveCondition { condition = CommanderKill });
        if (rule.interactionObjective)
            active.Add(new ActiveCondition { condition = Objective });
        if (rule.conditions != null)
        {
            foreach (var data in rule.conditions)
            {
                if (data == null || string.IsNullOrEmpty(data.type))
                    continue;
                if (Registered.TryGetValue(data.type, out var condition))
                    active.Add(new ActiveCondition { condition = condition, data = data });
            }
        }
        return active;
    }

    static SrpVictoryResult Won(int owner, ISrpVictoryCondition condition)
    {
        var result = new SrpVictoryResult { decided = true, winnerOwner = owner };
        if (condition == CommanderKill)
            result.reason = SrpVictoryReason.CommanderKilled;
        else if (condition == Objective)
            result.reason = SrpVictoryReason.ObjectiveSecured;
        else
        {
            result.reason = SrpVictoryReason.Custom;
            result.conditionType = condition.Type;
        }
        return result;
    }

    static bool HasAnyCommander(SrpBattleState state)
    {
        foreach (var unit in state.Units)
        {
            if (unit != null && unit.isCommander)
                return true;
        }
        return false;
    }

    static bool HasAnyObjective(SrpBattleState state)
    {
        if (state.InteractionPoints == null)
            return false;
        foreach (var point in state.InteractionPoints)
        {
            if (point != null && point.effect == SrpInteractionEffect.VictoryObjective)
                return true;
        }
        return false;
    }

    /// <summary>맵의 victory.commanderKill로 켠다. 지휘관을 모두 잃은 진영이 패배한다.</summary>
    sealed class CommanderKillCondition : ISrpVictoryCondition
    {
        public string Type { get { return "commander_kill"; } }

        public string Describe(SrpBattleState state, SrpVictoryConditionData data)
        {
            return HasAnyCommander(state) ? "지휘관 처치" : null;
        }

        public bool IsDefeated(SrpBattleState state, SrpVictoryConditionData data, int owner)
        {
            return HasLostAllCommanders(state, owner);
        }

        public bool HasWon(SrpBattleState state, SrpVictoryConditionData data, int owner)
        {
            return false;
        }
    }

    /// <summary>맵의 victory.interactionObjective로 켠다. 자기 승리 목표를 모두 확보한 진영이 승리한다.</summary>
    sealed class ObjectiveCondition : ISrpVictoryCondition
    {
        public string Type { get { return "objective"; } }

        public string Describe(SrpBattleState state, SrpVictoryConditionData data)
        {
            return HasAnyObjective(state) ? "목표 지점 확보" : null;
        }

        public bool IsDefeated(SrpBattleState state, SrpVictoryConditionData data, int owner)
        {
            return false;
        }

        public bool HasWon(SrpBattleState state, SrpVictoryConditionData data, int owner)
        {
            return HasSecuredAllObjectives(state, owner);
        }
    }
}

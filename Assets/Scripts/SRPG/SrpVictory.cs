using System.Collections.Generic;
using UnityEngine;

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
    /// <summary>맵이 추가로 건 조건(victory.conditions, victory.defeatConditions). 어느 조건인지는 SrpVictoryResult.conditionType에 있다.</summary>
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
    /// <summary>reason이 Custom일 때 승패를 가른 조건의 설명(HUD/로그용). 그 밖에는 null.</summary>
    public string conditionText;
    /// <summary>reason이 Custom일 때, 승패를 가른 조건이 패배 조건 목록의 것인가(상대가 패배해 이겼다).</summary>
    public bool byDefeatCondition;

    public static SrpVictoryResult Undecided()
    {
        return new SrpVictoryResult { decided = false, winnerOwner = -1, reason = SrpVictoryReason.None };
    }
}

/// <summary>
/// 조건 한 종류의 판정기. "어느 진영에 대해 이 조건이 성립했는가"만 판정한다.
/// 성립했을 때의 결과는 맵이 조건을 어느 목록에 넣었는지가 정한다:
/// 승리 조건 목록(victory.conditions)이면 그 진영이 승리하고, 패배 조건 목록(victory.defeatConditions)이면 그 진영이 패배한다.
/// 새 조건을 추가하려면 이 인터페이스를 구현해 SrpVictory.Register로 등록하고, 맵의 목록에 같은 type의 항목을 넣는다.
/// SrpVictory.Evaluate는 고치지 않는다.
/// </summary>
public interface ISrpVictoryCondition
{
    /// <summary>맵 데이터(conditions[].type)와 맞추는 이름.</summary>
    string Type { get; }

    /// <summary>
    /// HUD와 로그에 쓰는 한 줄 설명. asDefeat는 패배 조건 목록에 놓였다는 뜻이다.
    /// 이 전투에 해당 사항이 없으면 null.
    /// </summary>
    string Describe(SrpBattleState state, SrpVictoryConditionData data, bool asDefeat);

    /// <summary>
    /// owner에 대해 이 조건이 성립했는가.
    /// progress는 라운드마다 쌓는 조건(ISrpVictoryProgressCondition)의 진행도이고 그 밖에는 0이다.
    /// </summary>
    bool IsMet(SrpBattleState state, SrpVictoryConditionData data, int owner, int progress);
}

/// <summary>라운드가 끝날 때마다 진행도를 쌓는 조건(예: 거점 유지). 진행도는 전투 상태(SrpBattleState.VictoryProgress)에 든다.</summary>
public interface ISrpVictoryProgressCondition : ISrpVictoryCondition
{
    /// <summary>라운드가 끝난 시점의 새 진행도. previous는 지난 라운드까지의 값이다.</summary>
    int UpdateProgress(SrpBattleState state, SrpVictoryConditionData data, int owner, int previous);

    /// <summary>진행 상황 한 줄(로그용). 보여 줄 것이 없으면 null.</summary>
    string DescribeProgress(SrpBattleState state, SrpVictoryConditionData data, int owner, int progress);
}

/// <summary>내장 추가 조건의 type 이름과 대상 지정 규칙.</summary>
public static class SrpVictoryConditionTypes
{
    /// <summary>value 라운드가 끝날 때까지 살아남았다.</summary>
    public const string SurviveRounds = "survive_rounds";
    /// <summary>대상 유닛(targetId)이 (x, y) 칸에 서 있다.</summary>
    public const string ReachTile = "reach_tile";
    /// <summary>대상 유닛(targetId)을 모두 잃었다.</summary>
    public const string UnitLost = "unit_lost";
    /// <summary>(x, y) 중심 radius칸 거점을 value 라운드 연속으로 혼자 점유했다.</summary>
    public const string HoldPoint = "hold_point";

    /// <summary>targetId에 쓰는 값: 지휘관 표시(isCommander)가 있는 유닛.</summary>
    public const string CommanderTarget = "commander";

    public static readonly string[] BuiltIn = { SurviveRounds, ReachTile, UnitLost, HoldPoint };

    /// <summary>targetId가 가리키는 유닛인가. 비어 있으면 모든 유닛, "commander"면 지휘관, 그 밖에는 유닛 템플릿 id.</summary>
    public static bool MatchesUnit(SrpUnitRuntime unit, string targetId)
    {
        if (unit == null)
            return false;
        if (string.IsNullOrEmpty(targetId))
            return true;
        if (targetId == CommanderTarget)
            return unit.isCommander;
        return unit.templateId == targetId;
    }

    /// <summary>(x, y)가 조건의 거점(중심에서 radius칸 이내, 대각선 포함) 안인가.</summary>
    public static bool IsInZone(SrpVictoryConditionData data, int x, int y)
    {
        if (data == null)
            return false;
        int radius = Mathf.Max(0, data.radius);
        return Mathf.Abs(x - data.x) <= radius && Mathf.Abs(y - data.y) <= radius;
    }
}

/// <summary>조건이 가리키는 칸 하나(도달 칸, 거점 칸). 전투 화면과 맵 메이커의 표시에 쓴다.</summary>
public struct SrpVictoryTileMark
{
    public int x;
    public int y;
    /// <summary>패배 조건 목록의 조건인가.</summary>
    public bool asDefeat;
    /// <summary>조건의 대상 칸 자체인가(거점이면 중심). false면 거점에 딸린 주변 칸이다.</summary>
    public bool isCenter;
}

/// <summary>
/// 승패 판정(순수 로직). 런타임(SrpGameController)과 AI 시뮬레이션이 같은 판정을 쓴다.
/// 적 전멸은 항상 적용하고, 나머지 조건은 맵의 victory 설정에 있는 것만 적용한다.
/// 판정 순서: 전멸 > 패배 조건(지휘관 처치, 패배 조건 목록) > 승리 조건(목표 확보, 승리 조건 목록).
/// 같은 단계 안에서는 지휘관 처치·목표 확보가 먼저고 목록의 조건은 맵에 적힌 순서다.
/// </summary>
public static class SrpVictory
{
    static readonly ISrpVictoryCondition CommanderKill = new CommanderKillCondition();
    static readonly ISrpVictoryCondition Objective = new ObjectiveCondition();
    static readonly Dictionary<string, ISrpVictoryCondition> Registered = CreateBuiltInConditions();

    struct ActiveCondition
    {
        public ISrpVictoryCondition condition;
        public SrpVictoryConditionData data;
        /// <summary>패배 조건인가(성립한 진영이 패배한다).</summary>
        public bool asDefeat;
        /// <summary>목록 안의 순서. 맵 데이터로 건 조건만 0 이상이고 스위치로 켠 조건은 -1이다.</summary>
        public int index;

        /// <summary>data.owner가 정해져 있으면 그 진영에만 적용한다.</summary>
        public bool AppliesTo(int owner)
        {
            return data == null || data.owner < 0 || data.owner == owner;
        }

        public string ProgressKey(int owner)
        {
            return (asDefeat ? "d" : "v") + index + ":" + owner;
        }
    }

    static Dictionary<string, ISrpVictoryCondition> CreateBuiltInConditions()
    {
        var conditions = new Dictionary<string, ISrpVictoryCondition>();
        foreach (var condition in new ISrpVictoryCondition[]
        {
            new SurviveRoundsCondition(),
            new ReachTileCondition(),
            new UnitLostCondition(),
            new HoldPointCondition(),
        })
        {
            conditions[condition.Type] = condition;
        }
        return conditions;
    }

    /// <summary>
    /// 추가 조건을 등록한다. 같은 type이 이미 있으면 바꿔 끼운다.
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

    public static bool IsRegistered(string type)
    {
        return !string.IsNullOrEmpty(type) && Registered.ContainsKey(type);
    }

    public static SrpVictoryResult Evaluate(SrpBattleState state)
    {
        if (state == null || state.Units == null)
            return SrpVictoryResult.Undecided();

        var aliveOwners = GetAliveOwners(state);
        if (aliveOwners.Count == 0)
            return new SrpVictoryResult { decided = true, winnerOwner = -1, reason = SrpVictoryReason.Draw };

        var active = GetActiveConditions(state.VictoryRule);
        var contenders = new List<int>(aliveOwners);
        bool anyDefeat = false;
        var firstDefeat = default(ActiveCondition);
        foreach (var entry in active)
        {
            if (!entry.asDefeat)
                continue;
            for (int i = contenders.Count - 1; i >= 0; i--)
            {
                int owner = contenders[i];
                if (!IsMet(state, entry, owner))
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
            return Won(state, contenders[0], firstDefeat);
        }

        foreach (var entry in active)
        {
            if (entry.asDefeat)
                continue;
            foreach (int owner in contenders)
            {
                if (IsMet(state, entry, owner))
                    return Won(state, owner, entry);
            }
        }

        return SrpVictoryResult.Undecided();
    }

    /// <summary>
    /// 라운드가 끝났음을 기록하고, 라운드마다 쌓는 조건의 진행도를 갱신한다.
    /// 라운드의 마지막 활성화가 끝난 뒤, 다음 라운드를 시작하기 전에 한 번 부른다(SrpBattleFlow.EndRound).
    /// </summary>
    public static void OnRoundEnded(SrpBattleState state)
    {
        // 같은 라운드에 두 번 불려도 진행도가 두 번 오르지 않게 한다.
        if (state == null || state.CompletedRounds >= state.RoundNumber)
            return;

        state.CompletedRounds = state.RoundNumber;
        var active = GetActiveConditions(state.VictoryRule);
        if (active.Count == 0)
            return;

        var aliveOwners = GetAliveOwners(state);
        foreach (var entry in active)
        {
            if (!(entry.condition is ISrpVictoryProgressCondition tracked))
                continue;
            foreach (int owner in aliveOwners)
            {
                if (!entry.AppliesTo(owner))
                    continue;
                string key = entry.ProgressKey(owner);
                state.VictoryProgress.TryGetValue(key, out int previous);
                state.VictoryProgress[key] = Mathf.Max(0, tracked.UpdateProgress(state, entry.data, owner, previous));
            }
        }
    }

    /// <summary>지휘관이 한 명이라도 있던 진영이 지휘관을 모두 잃었는가. 지휘관이 없던 진영은 해당 없음.</summary>
    public static bool HasLostAllCommanders(SrpBattleState state, int owner)
    {
        return HasLostAllUnits(state, owner, SrpVictoryConditionTypes.CommanderTarget);
    }

    /// <summary>targetId가 가리키는 유닛이 한 명이라도 있던 진영이 그 유닛을 모두 잃었는가. 없던 진영은 해당 없음.</summary>
    public static bool HasLostAllUnits(SrpBattleState state, int owner, string targetId)
    {
        bool had = false;
        foreach (var unit in state.Units)
        {
            if (unit == null || unit.owner != owner || !SrpVictoryConditionTypes.MatchesUnit(unit, targetId))
                continue;
            had = true;
            if (!unit.eliminated)
                return false;
        }
        return had;
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

    /// <summary>
    /// 이 전투에 걸린 승리 조건을 한 줄로 설명한다(HUD/로그용).
    /// 지휘관 처치는 "상대 지휘관을 쓰러뜨리면 승리"로 읽히므로 여기에 넣는다.
    /// 패배 조건 목록은 DescribeDefeatRule이 따로 설명한다.
    /// </summary>
    public static string DescribeRule(SrpBattleState state)
    {
        var parts = new List<string> { "적 전멸" };
        if (state != null)
        {
            foreach (var entry in GetActiveConditions(state.VictoryRule))
            {
                if (entry.asDefeat && entry.index >= 0)
                    continue;
                string text = entry.condition.Describe(state, entry.data, entry.asDefeat);
                if (!string.IsNullOrEmpty(text))
                    parts.Add(text);
            }
        }
        return string.Join(" / ", parts);
    }

    /// <summary>맵이 건 패배 조건 목록을 한 줄로 설명한다. 없으면 null.</summary>
    public static string DescribeDefeatRule(SrpBattleState state)
    {
        if (state == null)
            return null;

        var parts = new List<string>();
        foreach (var entry in GetActiveConditions(state.VictoryRule))
        {
            if (!entry.asDefeat || entry.index < 0)
                continue;
            string text = entry.condition.Describe(state, entry.data, true);
            if (!string.IsNullOrEmpty(text))
                parts.Add(text);
        }
        return parts.Count > 0 ? string.Join(" / ", parts) : null;
    }

    /// <summary>라운드마다 쌓는 조건의 진행 상황을 한 줄로 설명한다(로그용). 보여 줄 것이 없으면 null.</summary>
    public static string DescribeProgress(SrpBattleState state)
    {
        if (state == null)
            return null;

        var parts = new List<string>();
        var aliveOwners = GetAliveOwners(state);
        foreach (var entry in GetActiveConditions(state.VictoryRule))
        {
            if (!(entry.condition is ISrpVictoryProgressCondition tracked))
                continue;
            foreach (int owner in aliveOwners)
            {
                if (!entry.AppliesTo(owner))
                    continue;
                state.VictoryProgress.TryGetValue(entry.ProgressKey(owner), out int progress);
                string text = tracked.DescribeProgress(state, entry.data, owner, progress);
                if (!string.IsNullOrEmpty(text))
                    parts.Add(text);
            }
        }
        return parts.Count > 0 ? string.Join(" / ", parts) : null;
    }

    /// <summary>판정 결과의 이유를 설명한다. 추가 조건으로 끝났으면 그 조건의 설명을 쓴다.</summary>
    public static string DescribeResult(SrpBattleState state, SrpVictoryResult result)
    {
        if (result.reason != SrpVictoryReason.Custom)
            return DescribeReason(result.reason);
        if (!string.IsNullOrEmpty(result.conditionText))
            return result.conditionText;
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
    /// 조건 한 건의 설명. 등록되지 않은 type이면 type 이름을 그대로 보여 준다.
    /// state가 없으면(맵 메이커) 대상 유닛은 id로 표기한다.
    /// </summary>
    public static string DescribeCondition(SrpVictoryConditionData data, bool asDefeat, SrpBattleState state = null)
    {
        if (data == null || string.IsNullOrEmpty(data.type))
            return "(빈 조건)";
        if (!Registered.TryGetValue(data.type, out var condition))
            return $"(알 수 없는 조건: {data.type})";
        string text = condition.Describe(state, data, asDefeat);
        return string.IsNullOrEmpty(text) ? $"({data.type}: 대상 없음)" : text;
    }

    /// <summary>맵의 조건 목록이 가리키는 칸(도달 칸, 거점의 모든 칸). 맵 밖의 칸도 그대로 돌려주므로 호출한 쪽이 거른다.</summary>
    public static List<SrpVictoryTileMark> GetConditionTiles(SrpVictoryRuleData rule)
    {
        var marks = new List<SrpVictoryTileMark>();
        if (rule == null)
            return marks;
        AddConditionTiles(marks, rule.conditions, false);
        AddConditionTiles(marks, rule.defeatConditions, true);
        return marks;
    }

    static void AddConditionTiles(List<SrpVictoryTileMark> marks, SrpVictoryConditionData[] list, bool asDefeat)
    {
        if (list == null)
            return;
        foreach (var data in list)
        {
            if (data == null)
                continue;
            if (data.type == SrpVictoryConditionTypes.ReachTile)
            {
                marks.Add(new SrpVictoryTileMark { x = data.x, y = data.y, asDefeat = asDefeat, isCenter = true });
            }
            else if (data.type == SrpVictoryConditionTypes.HoldPoint)
            {
                int radius = Mathf.Max(0, data.radius);
                for (int y = data.y - radius; y <= data.y + radius; y++)
                for (int x = data.x - radius; x <= data.x + radius; x++)
                    marks.Add(new SrpVictoryTileMark { x = x, y = y, asDefeat = asDefeat, isCenter = x == data.x && y == data.y });
            }
        }
    }

    /// <summary>진영 표기(P0, P1). owner가 정해지지 않은 조건은 빈 문자열.</summary>
    public static string OwnerLabel(int owner)
    {
        return owner >= 0 ? "P" + owner + " " : string.Empty;
    }

    /// <summary>조건의 대상 유닛 표기: 지휘관, 템플릿의 표시 이름, 또는 "유닛".</summary>
    public static string TargetLabel(SrpBattleState state, string targetId)
    {
        if (string.IsNullOrEmpty(targetId))
            return "유닛";
        if (targetId == SrpVictoryConditionTypes.CommanderTarget)
            return "지휘관";
        if (state != null && state.TemplateLookup != null
            && state.TemplateLookup.TryGetValue(targetId, out var template)
            && template != null && !string.IsNullOrEmpty(template.displayName))
            return template.displayName;
        return targetId;
    }

    static bool IsMet(SrpBattleState state, ActiveCondition entry, int owner)
    {
        if (!entry.AppliesTo(owner))
            return false;
        int progress = 0;
        if (entry.index >= 0 && entry.condition is ISrpVictoryProgressCondition)
            state.VictoryProgress.TryGetValue(entry.ProgressKey(owner), out progress);
        return entry.condition.IsMet(state, entry.data, owner, progress);
    }

    static List<int> GetAliveOwners(SrpBattleState state)
    {
        var owners = new List<int>();
        foreach (var unit in state.Units)
        {
            if (unit == null || unit.eliminated)
                continue;
            if (!owners.Contains(unit.owner))
                owners.Add(unit.owner);
        }
        owners.Sort();
        return owners;
    }

    /// <summary>
    /// 이 맵에 걸린 조건을 판정 순서대로 모은다: 지휘관 처치, 패배 조건 목록, 목표 확보, 승리 조건 목록.
    /// 등록되지 않은 type은 건너뛴다(그 조건만 빠지고 나머지 판정은 그대로 한다).
    /// </summary>
    static List<ActiveCondition> GetActiveConditions(SrpVictoryRuleData rule)
    {
        var active = new List<ActiveCondition>();
        if (rule == null)
            return active;

        if (rule.commanderKill)
            active.Add(new ActiveCondition { condition = CommanderKill, asDefeat = true, index = -1 });
        AddListed(active, rule.defeatConditions, true);
        if (rule.interactionObjective)
            active.Add(new ActiveCondition { condition = Objective, asDefeat = false, index = -1 });
        AddListed(active, rule.conditions, false);
        return active;
    }

    static void AddListed(List<ActiveCondition> active, SrpVictoryConditionData[] list, bool asDefeat)
    {
        if (list == null)
            return;
        for (int i = 0; i < list.Length; i++)
        {
            var data = list[i];
            if (data == null || string.IsNullOrEmpty(data.type))
                continue;
            if (Registered.TryGetValue(data.type, out var condition))
                active.Add(new ActiveCondition { condition = condition, data = data, asDefeat = asDefeat, index = i });
        }
    }

    static SrpVictoryResult Won(SrpBattleState state, int owner, ActiveCondition entry)
    {
        var result = new SrpVictoryResult { decided = true, winnerOwner = owner };
        if (entry.condition == CommanderKill)
            result.reason = SrpVictoryReason.CommanderKilled;
        else if (entry.condition == Objective)
            result.reason = SrpVictoryReason.ObjectiveSecured;
        else
        {
            result.reason = SrpVictoryReason.Custom;
            result.conditionType = entry.condition.Type;
            result.conditionText = entry.condition.Describe(state, entry.data, entry.asDefeat);
            result.byDefeatCondition = entry.asDefeat;
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

        public string Describe(SrpBattleState state, SrpVictoryConditionData data, bool asDefeat)
        {
            return HasAnyCommander(state) ? "지휘관 처치" : null;
        }

        public bool IsMet(SrpBattleState state, SrpVictoryConditionData data, int owner, int progress)
        {
            return HasLostAllCommanders(state, owner);
        }
    }

    /// <summary>맵의 victory.interactionObjective로 켠다. 자기 승리 목표를 모두 확보한 진영이 승리한다.</summary>
    sealed class ObjectiveCondition : ISrpVictoryCondition
    {
        public string Type { get { return "objective"; } }

        public string Describe(SrpBattleState state, SrpVictoryConditionData data, bool asDefeat)
        {
            return HasAnyObjective(state) ? "목표 지점 확보" : null;
        }

        public bool IsMet(SrpBattleState state, SrpVictoryConditionData data, int owner, int progress)
        {
            return HasSecuredAllObjectives(state, owner);
        }
    }

    /// <summary>
    /// N라운드 버티기: value 라운드가 끝날 때까지 살아남은 진영에 성립한다.
    /// 승리 목록에 넣으면 "버티면 승리", 패배 목록에 넣으면 "그때까지 이기지 못하면 패배"(제한 라운드)가 된다.
    /// </summary>
    sealed class SurviveRoundsCondition : ISrpVictoryCondition
    {
        public string Type { get { return SrpVictoryConditionTypes.SurviveRounds; } }

        public string Describe(SrpBattleState state, SrpVictoryConditionData data, bool asDefeat)
        {
            int rounds = Mathf.Max(1, data.value);
            return asDefeat
                ? $"{OwnerLabel(data.owner)}{rounds}라운드 안에 끝내지 못함"
                : $"{OwnerLabel(data.owner)}{rounds}라운드 버티기";
        }

        public bool IsMet(SrpBattleState state, SrpVictoryConditionData data, int owner, int progress)
        {
            return state.CompletedRounds >= Mathf.Max(1, data.value);
        }
    }

    /// <summary>특정 칸 도달·탈출: 대상 유닛(targetId)이 (x, y) 칸에 서 있는 진영에 성립한다.</summary>
    sealed class ReachTileCondition : ISrpVictoryCondition
    {
        public string Type { get { return SrpVictoryConditionTypes.ReachTile; } }

        public string Describe(SrpBattleState state, SrpVictoryConditionData data, bool asDefeat)
        {
            string who = OwnerLabel(data.owner) + TargetLabel(state, data.targetId);
            return asDefeat
                ? $"{who} ({data.x},{data.y}) 진입"
                : $"{who} ({data.x},{data.y}) 도달";
        }

        public bool IsMet(SrpBattleState state, SrpVictoryConditionData data, int owner, int progress)
        {
            var occupant = state.GetOccupant(data.x, data.y);
            return occupant != null
                && !occupant.eliminated
                && occupant.owner == owner
                && SrpVictoryConditionTypes.MatchesUnit(occupant, data.targetId);
        }
    }

    /// <summary>
    /// 특정 유닛 상실: 대상 유닛(targetId)이 있던 진영이 그 유닛을 모두 잃으면 성립한다.
    /// 패배 목록에 넣으면 "특정 유닛 보호"가 된다. targetId가 비어 있으면 전멸과 같아 쓰지 않는다.
    /// </summary>
    sealed class UnitLostCondition : ISrpVictoryCondition
    {
        public string Type { get { return SrpVictoryConditionTypes.UnitLost; } }

        public string Describe(SrpBattleState state, SrpVictoryConditionData data, bool asDefeat)
        {
            if (string.IsNullOrEmpty(data.targetId))
                return null;
            string who = OwnerLabel(data.owner) + TargetLabel(state, data.targetId);
            return asDefeat ? $"{who} 보호 실패" : $"{who} 상실";
        }

        public bool IsMet(SrpBattleState state, SrpVictoryConditionData data, int owner, int progress)
        {
            return !string.IsNullOrEmpty(data.targetId) && HasLostAllUnits(state, owner, data.targetId);
        }
    }

    /// <summary>
    /// 거점 N라운드 유지: 라운드가 끝날 때 거점 안에 자기 유닛이 있고 적 유닛이 없으면 진행도가 1 오르고, 아니면 0으로 돌아간다.
    /// 진행도가 value에 닿은 진영에 성립한다.
    /// </summary>
    sealed class HoldPointCondition : ISrpVictoryProgressCondition
    {
        public string Type { get { return SrpVictoryConditionTypes.HoldPoint; } }

        public string Describe(SrpBattleState state, SrpVictoryConditionData data, bool asDefeat)
        {
            string text = $"{OwnerLabel(data.owner)}거점 ({data.x},{data.y}) {Mathf.Max(1, data.value)}라운드 유지";
            return asDefeat ? text + " 시 패배" : text;
        }

        public bool IsMet(SrpBattleState state, SrpVictoryConditionData data, int owner, int progress)
        {
            return progress >= Mathf.Max(1, data.value);
        }

        public int UpdateProgress(SrpBattleState state, SrpVictoryConditionData data, int owner, int previous)
        {
            bool holds = false;
            foreach (var unit in state.Units)
            {
                if (unit == null || unit.eliminated || !SrpVictoryConditionTypes.IsInZone(data, unit.anchorX, unit.anchorY))
                    continue;
                if (unit.owner != owner)
                    return 0;
                holds = true;
            }
            return holds ? previous + 1 : 0;
        }

        public string DescribeProgress(SrpBattleState state, SrpVictoryConditionData data, int owner, int progress)
        {
            return progress > 0
                ? $"P{owner} 거점 ({data.x},{data.y}) 유지 {progress}/{Mathf.Max(1, data.value)}"
                : null;
        }
    }
}

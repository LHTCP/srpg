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
}

public struct SrpVictoryResult
{
    public bool decided;
    /// <summary>승리 진영. 무승부거나 미결이면 -1.</summary>
    public int winnerOwner;
    public SrpVictoryReason reason;

    public static SrpVictoryResult Undecided()
    {
        return new SrpVictoryResult { decided = false, winnerOwner = -1, reason = SrpVictoryReason.None };
    }
}

/// <summary>
/// 승패 판정(순수 로직). 런타임(SrpGameController)과 AI 시뮬레이션이 같은 판정을 쓴다.
/// 적 전멸은 항상 적용하고, 지휘관 처치와 상호작용 목표는 맵의 victory 설정이 켜져 있을 때만 적용한다.
/// 판정 순서: 전멸 > 지휘관 처치 > 상호작용 목표.
/// </summary>
public static class SrpVictory
{
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

        var rule = state.VictoryRule;
        var contenders = new List<int>(aliveOwners);
        if (rule != null && rule.commanderKill)
        {
            for (int i = contenders.Count - 1; i >= 0; i--)
            {
                if (HasLostAllCommanders(state, contenders[i]))
                    contenders.RemoveAt(i);
            }
        }

        if (contenders.Count == 0)
            return new SrpVictoryResult { decided = true, winnerOwner = -1, reason = SrpVictoryReason.Draw };

        if (contenders.Count == 1)
        {
            return new SrpVictoryResult
            {
                decided = true,
                winnerOwner = contenders[0],
                reason = aliveOwners.Count == 1 ? SrpVictoryReason.Annihilation : SrpVictoryReason.CommanderKilled,
            };
        }

        if (rule != null && rule.interactionObjective)
        {
            foreach (int owner in contenders)
            {
                if (HasSecuredAllObjectives(state, owner))
                    return new SrpVictoryResult { decided = true, winnerOwner = owner, reason = SrpVictoryReason.ObjectiveSecured };
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
        var rule = state != null ? state.VictoryRule : null;
        if (rule != null && rule.commanderKill && HasAnyCommander(state))
            parts.Add("지휘관 처치");
        if (rule != null && rule.interactionObjective && HasAnyObjective(state))
            parts.Add("목표 지점 확보");
        return string.Join(" / ", parts);
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
            default:
                return "미결";
        }
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
}

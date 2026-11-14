using System.Collections.Generic;

public static class SrpTurnOrder
{
    public static void ResetRoundResources(SrpBattleState state)
    {
        if (state == null || state.Units == null)
            return;

        foreach (var unit in state.Units)
        {
            if (unit == null || unit.eliminated)
                continue;

            unit.passiveAppliedThisTurn = false;
            unit.actionPoints = unit.maxActionPoints;
            unit.reactionPoints = unit.maxReactionPoints > 0 ? unit.maxReactionPoints : 1;
            unit.lastReactionKind = SrpReactionKind.None;
            unit.lastReactionRound = state.RoundNumber;
            unit.lastReactionSourceId = -1;
            unit.overwatchArmed = false;
            unit.overwatchRange = 0;
            unit.overwatchRound = 0;
            unit.defensiveHitsTakenThisRound = 0;
            unit.defensiveHitsRound = state.RoundNumber;
        }
    }

    /// <summary>
    /// 활성화 시작 처리(그로기 브릿지).
    /// PG 붕괴 유닛은 이번 활성화를 잃고 PG 일부를 회복하며, 다음 활성화까지 재붕괴 보호를 받는다. 이때 true.
    /// 정상 유닛은 재붕괴 보호를 해제하고 false를 돌려준다.
    /// </summary>
    public static bool TryConsumeActivationForGroggyRecovery(SrpUnitRuntime unit, out int recoveredPg)
    {
        recoveredPg = 0;
        if (unit == null || unit.eliminated)
            return false;
        if (!unit.IsPgBroken)
        {
            unit.pgBreakGuard = false;
            return false;
        }

        int before = unit.pg > 0 ? unit.pg : 0;
        int maxPg = unit.maxPg > 0 ? unit.maxPg : 1;
        int target = (maxPg * SrpUnitRuntime.GroggyRecoveryPgPercent + 99) / 100;
        if (target < 1)
            target = 1;
        if (target > maxPg)
            target = maxPg;

        unit.pg = before > target ? before : target;
        unit.groggy = false;
        unit.pgBreakGuard = true;
        unit.actionPoints = 0;
        recoveredPg = unit.pg - before;
        return true;
    }

    public static List<int> BuildRoundQueue(SrpBattleState state)
    {
        var queue = new List<SrpUnitRuntime>();
        foreach (var unit in state.Units)
        {
            if (!unit.eliminated)
                queue.Add(unit);
        }

        queue.Sort((a, b) =>
        {
            int speedCompare = b.speed.CompareTo(a.speed);
            if (speedCompare != 0)
                return speedCompare;

            int ownerCompare = a.owner.CompareTo(b.owner);
            if (ownerCompare != 0)
                return ownerCompare;

            return a.id.CompareTo(b.id);
        });

        var ids = new List<int>(queue.Count);
        foreach (var u in queue)
            ids.Add(u.id);
        return ids;
    }

    public static bool HasRemainingUnitInRound(SrpBattleState state)
    {
        if (state.RoundQueue == null || state.RoundQueue.Count == 0)
            return false;

        for (int i = state.RoundQueue.Count - 1; i >= 0; i--)
        {
            var unit = state.FindUnitById(state.RoundQueue[i]);
            if (unit == null || unit.eliminated)
                state.RoundQueue.RemoveAt(i);
        }
        return state.RoundQueue.Count > 0;
    }

    public static int AdvanceToNextUnit(SrpBattleState state)
    {
        while (state.RoundQueue != null && state.RoundQueue.Count > 0)
        {
            int next = state.RoundQueue[0];
            state.RoundQueue.RemoveAt(0);
            var unit = state.FindUnitById(next);
            if (unit == null || unit.eliminated)
                continue;

            state.CurrentUnitId = next;
            return next;
        }

        state.CurrentUnitId = -1;
        return -1;
    }
}

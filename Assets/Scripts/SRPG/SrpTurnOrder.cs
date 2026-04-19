using System.Collections.Generic;

public static class SrpTurnOrder
{
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

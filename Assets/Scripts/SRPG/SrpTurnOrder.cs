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
        return state.RoundQueue != null && state.RoundQueue.Count > 0;
    }

    public static int AdvanceToNextUnit(SrpBattleState state)
    {
        if (state.RoundQueue == null || state.RoundQueue.Count == 0)
            return -1;

        int next = state.RoundQueue[0];
        state.RoundQueue.RemoveAt(0);
        state.CurrentUnitId = next;
        return next;
    }
}

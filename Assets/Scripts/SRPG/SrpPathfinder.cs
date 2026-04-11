using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 가중 그래프(칸당 1, ZOC 인접 시 +1) 다익스트라. 이동 가능 앵커 목록.
/// </summary>
public static class SrpPathfinder
{
    static readonly Vector2Int[] Dirs =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
    };

    /// <summary>도달 가능 앵커 목록만 반환 (기존 호환).</summary>
    public static List<Vector2Int> GetReachableAnchors(SrpBattleState state, SrpUnitRuntime u)
    {
        var costMap = GetReachableWithCosts(state, u, u.moveRange);
        return new List<Vector2Int>(costMap.Keys);
    }

    /// <summary>도달 가능 앵커 → 이동 비용 맵 반환. maxCost 로 잔여 이동력 제한 가능.</summary>
    public static Dictionary<Vector2Int, int> GetReachableWithCosts(
        SrpBattleState state, SrpUnitRuntime u, int maxCost)
    {
        var result = new Dictionary<Vector2Int, int>();
        if (u.eliminated || maxCost <= 0)
            return result;

        var dist = new Dictionary<int, int>();
        var open = new List<(int x, int y, int cost)>();

        int sx = u.anchorX, sy = u.anchorY;
        int sKey = state.Index(sx, sy);
        dist[sKey] = 0;
        open.Add((sx, sy, 0));

        while (open.Count > 0)
        {
            int bestIdx = 0;
            for (int i = 1; i < open.Count; i++)
            {
                if (open[i].cost < open[bestIdx].cost)
                    bestIdx = i;
            }

            var cur = open[bestIdx];
            open.RemoveAt(bestIdx);
            int cost = cur.cost;
            int curKey = state.Index(cur.x, cur.y);
            if (!dist.TryGetValue(curKey, out int best) || best != cost)
                continue;
            if (cost > maxCost)
                continue;

            if (cost > 0 && state.CanStandAt(u, cur.x, cur.y, u.id))
                result[new Vector2Int(cur.x, cur.y)] = cost;

            for (int d = 0; d < 4; d++)
            {
                int nx = cur.x + Dirs[d].x;
                int ny = cur.y + Dirs[d].y;
                if (!state.InBounds(nx, ny))
                    continue;

                int enter = 1;
                if (state.IsEnemyAdjacentToTile(nx, ny, u.owner))
                    enter++;

                int nextCost = cost + enter;
                if (nextCost > maxCost)
                    continue;

                if (!state.CanStandAt(u, nx, ny, u.id))
                    continue;

                int nk = state.Index(nx, ny);
                if (!dist.TryGetValue(nk, out int oldCost) || nextCost < oldCost)
                {
                    dist[nk] = nextCost;
                    open.Add((nx, ny, nextCost));
                }
            }
        }

        return result;
    }
}

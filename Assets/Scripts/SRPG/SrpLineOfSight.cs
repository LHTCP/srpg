using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시야(LOS) 규칙. 스킬 데이터가 값으로 저장하므로 번호를 바꾸지 않는다.
/// </summary>
public enum SrpSightRule
{
    /// <summary>시야를 보지 않는다. 사거리만 맞으면 된다.</summary>
    None = 0,
    /// <summary>지형만 막는다: 이동 불가 타일(시야를 막지 않는 엄폐물 칸은 제외)과 LOS 차단 엄폐 segment.</summary>
    Terrain = 1,
    /// <summary>지형과 사선 중간의 유닛이 막는다. 총기 기본 공격과 같은 규칙이다.</summary>
    TerrainAndUnits = 2,
}

/// <summary>
/// 시야(LOS) 판정 모듈(순수 로직). 두 칸 사이에 주어진 규칙으로 사선이 통하는지 본다.
/// 사선이 지나는 칸은 총기 조준(SrpFirearmAim)과 같은 방식으로 구한다.
/// </summary>
public static class SrpLineOfSight
{
    /// <summary>
    /// (fromX, fromY)에서 (toX, toY)로 시야가 통하는가.
    /// 같은 칸과 인접 칸(대각선 포함)은 규칙과 무관하게 통한다.
    /// 도착 칸 자체의 점유나 이동 가능 여부는 보지 않는다. 사선 중간만 본다.
    /// </summary>
    public static bool HasSight(
        SrpBattleState state,
        int fromX,
        int fromY,
        int toX,
        int toY,
        SrpSightRule rule,
        int ignoreUnitIdA = -1,
        int ignoreUnitIdB = -1)
    {
        return FindBlockingTile(state, fromX, fromY, toX, toY, rule, ignoreUnitIdA, ignoreUnitIdB, out _) == false;
    }

    /// <summary>
    /// 시야를 막는 첫 칸을 찾는다. 막히면 true이고 blocker에 그 칸을 넣는다.
    /// 엄폐 segment가 막은 경우 blocker는 segment를 넘어 들어가려던 칸이다.
    /// </summary>
    public static bool FindBlockingTile(
        SrpBattleState state,
        int fromX,
        int fromY,
        int toX,
        int toY,
        SrpSightRule rule,
        int ignoreUnitIdA,
        int ignoreUnitIdB,
        out Vector2Int blocker)
    {
        blocker = new Vector2Int(toX, toY);
        if (state == null)
            return true;
        if (rule == SrpSightRule.None)
            return false;
        if (!state.InBounds(fromX, fromY) || !state.InBounds(toX, toY))
            return true;
        if (Mathf.Max(Mathf.Abs(toX - fromX), Mathf.Abs(toY - fromY)) <= 1)
            return false;

        var tiles = SrpFirearmAim.BuildAimTiles(fromX, fromY, toX, toY);
        int prevX = fromX;
        int prevY = fromY;
        for (int i = 0; i < tiles.Count; i++)
        {
            int x = tiles[i].x;
            int y = tiles[i].y;
            blocker = new Vector2Int(x, y);
            if (state.HasLineBlockingCoverSegmentBetween(prevX, prevY, x, y))
                return true;

            bool isDestination = x == toX && y == toY;
            if (!isDestination)
            {
                if (state.BlocksSightTile(x, y))
                    return true;
                if (rule == SrpSightRule.TerrainAndUnits)
                {
                    var occupant = state.GetOccupant(x, y);
                    if (occupant != null && occupant.id != ignoreUnitIdA && occupant.id != ignoreUnitIdB)
                        return true;
                }
            }
            prevX = x;
            prevY = y;
        }

        return false;
    }

    public static string GetDisplayName(SrpSightRule rule)
    {
        switch (rule)
        {
            case SrpSightRule.Terrain:
                return "지형 차단";
            case SrpSightRule.TerrainAndUnits:
                return "지형·유닛 차단";
            default:
                return "시야 불필요";
        }
    }
}

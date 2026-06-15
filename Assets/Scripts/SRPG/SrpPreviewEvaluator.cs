using System.Collections.Generic;
using UnityEngine;

public sealed class SrpMovePreviewEvaluation
{
    public bool valid;
    public int unitId;
    public int destinationX;
    public int destinationY;
    public int coverX;
    public int coverY;
    public bool hasCover;
    public readonly List<SrpMovePreviewThreat> threats = new List<SrpMovePreviewThreat>();
}

public sealed class SrpMovePreviewThreat
{
    public int attackerId;
    public bool isOverwatch;
    public readonly List<Vector2Int> lineTiles = new List<Vector2Int>();
}

public static class SrpPreviewEvaluator
{
    public static SrpMovePreviewEvaluation EvaluateMove(
        SrpBattleState state,
        SrpUnitRuntime unit,
        int destinationX,
        int destinationY)
    {
        var result = new SrpMovePreviewEvaluation
        {
            valid = false,
            unitId = unit != null ? unit.id : -1,
            destinationX = destinationX,
            destinationY = destinationY,
        };

        if (state == null || unit == null || unit.eliminated)
            return result;

        var clone = state.Clone();
        var cloneUnit = clone.FindUnitById(unit.id);
        if (cloneUnit == null || cloneUnit.eliminated)
            return result;

        cloneUnit.anchorX = destinationX;
        cloneUnit.anchorY = destinationY;
        clone.RebuildEngagements();
        result.valid = true;

        if (clone.TryGetAdjacentCover(cloneUnit, out int coverX, out int coverY))
        {
            result.hasCover = true;
            result.coverX = coverX;
            result.coverY = coverY;
        }

        foreach (var enemy in clone.Units)
        {
            if (enemy == null || enemy.eliminated || enemy.owner == cloneUnit.owner)
                continue;

            bool canAttack = SrpCombatResolver.CanAttack(clone, enemy, cloneUnit);
            bool canOverwatch = SrpOverwatch.CanTrigger(clone, enemy, cloneUnit);
            if (!canAttack && !canOverwatch)
                continue;

            var threat = new SrpMovePreviewThreat
            {
                attackerId = enemy.id,
                isOverwatch = canOverwatch,
            };
            BuildThreatLine(clone, enemy, cloneUnit, threat.lineTiles);
            result.threats.Add(threat);
        }

        return result;
    }

    static void BuildThreatLine(
        SrpBattleState state,
        SrpUnitRuntime attacker,
        SrpUnitRuntime target,
        List<Vector2Int> output)
    {
        output.Clear();
        if (state == null || attacker == null || target == null)
            return;

        bool isFirearmThreat = SrpOverwatch.CanTrigger(state, attacker, target)
            || SrpCombatResolver.ResolveBasicAttackKind(state, attacker, target) == SrpBasicAttackKind.Firearm;
        if (isFirearmThreat
            && SrpFirearmAim.TryBuildAimLine(
                state,
                attacker,
                target.anchorX,
                target.anchorY,
                attacker.overwatchArmed && attacker.overwatchRange > 0 ? attacker.overwatchRange : attacker.attackRange,
                target.id,
                out var aimLine))
        {
            output.AddRange(aimLine.tiles);
            return;
        }

        foreach (var tile in BuildStraightLine(attacker.anchorX, attacker.anchorY, target.anchorX, target.anchorY))
            output.Add(tile);
    }

    static IEnumerable<Vector2Int> BuildStraightLine(int fromX, int fromY, int toX, int toY)
    {
        int dx = Mathf.Abs(toX - fromX);
        int dy = Mathf.Abs(toY - fromY);
        int sx = fromX < toX ? 1 : -1;
        int sy = fromY < toY ? 1 : -1;
        int err = dx - dy;
        int x = fromX;
        int y = fromY;

        while (true)
        {
            yield return new Vector2Int(x, y);
            if (x == toX && y == toY)
                break;

            int e2 = err * 2;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }
    }
}

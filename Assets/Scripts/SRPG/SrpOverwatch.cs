using System.Collections.Generic;
using UnityEngine;

public enum SrpOverwatchArmStatus
{
    Ready,
    AlreadyArmed,
    NoUnit,
    Eliminated,
    NoAction,
    NoReaction,
    NotFirearm,
    RangeTooShort,
    NoAmmo,
}

public static class SrpOverwatch
{
    public const int MaxTriggersPerReservation = 1;

    public static bool CanArm(SrpUnitRuntime unit)
    {
        return GetArmStatus(unit) == SrpOverwatchArmStatus.Ready;
    }

    public static SrpOverwatchArmStatus GetArmStatus(SrpUnitRuntime unit)
    {
        if (unit == null)
            return SrpOverwatchArmStatus.NoUnit;
        if (unit.eliminated)
            return SrpOverwatchArmStatus.Eliminated;
        if (unit.overwatchArmed)
            return SrpOverwatchArmStatus.AlreadyArmed;
        if (unit.actionPoints <= 0)
            return SrpOverwatchArmStatus.NoAction;
        if (unit.reactionPoints <= 0)
            return SrpOverwatchArmStatus.NoReaction;
        if (unit.weaponClass != SrpWeaponClass.Firearm)
            return SrpOverwatchArmStatus.NotFirearm;
        if (unit.attackRange <= 1)
            return SrpOverwatchArmStatus.RangeTooShort;
        if (!unit.HasAmmoForAttack())
            return SrpOverwatchArmStatus.NoAmmo;
        return SrpOverwatchArmStatus.Ready;
    }

    public static bool Arm(SrpBattleState state, SrpUnitRuntime unit)
    {
        if (state == null || !CanArm(unit))
            return false;

        unit.actionPoints = Mathf.Max(0, unit.actionPoints - 1);
        unit.overwatchArmed = true;
        unit.overwatchRange = Mathf.Max(1, unit.attackRange);
        unit.overwatchRound = state.RoundNumber;
        return true;
    }

    public static bool CanTrigger(SrpBattleState state, SrpUnitRuntime watcher, SrpUnitRuntime target)
    {
        if (state == null || watcher == null || target == null)
            return false;
        if (!watcher.overwatchArmed || watcher.eliminated || target.eliminated)
            return false;
        if (watcher.overwatchRound != state.RoundNumber)
            return false;
        if (watcher.owner == target.owner || watcher.reactionPoints <= 0)
            return false;
        if (!watcher.HasAmmoForAttack())
            return false;
        int range = watcher.overwatchRange > 0 ? watcher.overwatchRange : watcher.attackRange;
        return IsTileInLineOfSight(state, watcher, target.anchorX, target.anchorY, range, target.id);
    }

    public static SrpUnitRuntime SelectTriggerWatcher(SrpBattleState state, SrpUnitRuntime target)
    {
        if (state == null || target == null)
            return null;

        SrpUnitRuntime best = null;
        foreach (var watcher in state.Units)
        {
            if (!CanTrigger(state, watcher, target))
                continue;
            if (IsHigherPriority(state, watcher, best, target))
                best = watcher;
        }
        return best;
    }

    public static bool IsTileInLineOfSight(
        SrpBattleState state,
        SrpUnitRuntime watcher,
        int targetX,
        int targetY,
        int range = -1,
        int exceptUnitId = -1)
    {
        return SrpFirearmAim.TryBuildAimLine(
            state,
            watcher,
            targetX,
            targetY,
            range,
            exceptUnitId,
            true,
            out _);
    }

    static bool IsHigherPriority(SrpBattleState state, SrpUnitRuntime candidate, SrpUnitRuntime current, SrpUnitRuntime target)
    {
        if (candidate == null)
            return false;
        if (current == null)
            return true;

        int candidateDistance = state.ChebyshevAnchor(candidate, target);
        int currentDistance = state.ChebyshevAnchor(current, target);
        if (candidateDistance != currentDistance)
            return candidateDistance < currentDistance;
        if (candidate.speed != current.speed)
            return candidate.speed > current.speed;
        return candidate.id < current.id;
    }

    public static bool TryTrigger(
        SrpBattleState state,
        SrpUnitRuntime watcher,
        SrpUnitRuntime target,
        out SrpCombatResolver.AttackOutcome outcome)
    {
        outcome = new SrpCombatResolver.AttackOutcome();
        if (!CanTrigger(state, watcher, target))
            return false;

        watcher.overwatchArmed = false;
        watcher.overwatchRange = 0;
        watcher.overwatchRound = 0;
        watcher.reactionPoints = Mathf.Max(0, watcher.reactionPoints - 1);
        watcher.SpendAmmoForAttack();
        SrpFirearmAim.TurnShooterTowardTarget(watcher, target);
        watcher.lastReactionKind = SrpReactionKind.ReactionShot;
        watcher.lastReactionRound = state.RoundNumber;
        watcher.lastReactionSourceId = target.id;
        outcome = SrpCombatResolver.ApplyAttack(state, watcher, target);
        return true;
    }
}

public struct SrpFirearmAimLine
{
    public bool canAim;
    public bool isEightDirectionLane;
    public int distance;
    public int dx;
    public int dy;
    public SrpFacing facing;
    public List<Vector2Int> tiles;
}

public static class SrpFirearmAim
{
    public static bool CanBasicAttack(
        SrpBattleState state,
        SrpUnitRuntime attacker,
        SrpUnitRuntime target,
        out SrpFirearmAimLine line)
    {
        line = default;
        if (target == null)
            return false;
        return TryBuildAimLine(
            state,
            attacker,
            target.anchorX,
            target.anchorY,
            attacker != null ? attacker.attackRange : -1,
            target.id,
            false,
            out line);
    }

    public static bool TryBuildAimLine(
        SrpBattleState state,
        SrpUnitRuntime shooter,
        int targetX,
        int targetY,
        int range,
        int exceptUnitId,
        bool requireEightDirectionLane,
        out SrpFirearmAimLine line)
    {
        line = new SrpFirearmAimLine
        {
            canAim = false,
            tiles = new List<Vector2Int>(),
        };

        if (state == null || shooter == null || shooter.eliminated)
            return false;
        if (!state.InBounds(targetX, targetY) || !state.IsWalkableTile(targetX, targetY))
            return false;

        int dx = targetX - shooter.anchorX;
        int dy = targetY - shooter.anchorY;
        if (dx == 0 && dy == 0)
            return false;

        int distance = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
        int effectiveRange = range > 0 ? range : shooter.attackRange;
        if (distance > effectiveRange)
            return false;

        bool isEightDirectionLane = IsStraightEightDirection(dx, dy);
        if (requireEightDirectionLane && !isEightDirectionLane)
            return false;

        var tiles = BuildAimTiles(shooter.anchorX, shooter.anchorY, targetX, targetY);
        int fromX = shooter.anchorX;
        int fromY = shooter.anchorY;
        for (int i = 0; i < tiles.Count; i++)
        {
            int x = tiles[i].x;
            int y = tiles[i].y;
            if (state.HasLineBlockingCoverSegmentBetween(fromX, fromY, x, y))
                return false;
            if ((x != targetX || y != targetY)
                && !IsLineTileOpen(state, shooter.id, exceptUnitId, x, y))
                return false;
            fromX = x;
            fromY = y;
        }

        line.canAim = true;
        line.isEightDirectionLane = isEightDirectionLane;
        line.distance = distance;
        line.dx = dx;
        line.dy = dy;
        line.facing = ResolveFacing(dx, dy);
        line.tiles = tiles;
        return true;
    }

    public static bool TurnShooterTowardTarget(SrpUnitRuntime shooter, SrpUnitRuntime target)
    {
        if (shooter == null || target == null)
            return false;
        int dx = target.anchorX - shooter.anchorX;
        int dy = target.anchorY - shooter.anchorY;
        if (dx == 0 && dy == 0)
            return false;
        shooter.facing = ResolveFacing(dx, dy);
        return true;
    }

    static List<Vector2Int> BuildAimTiles(int startX, int startY, int targetX, int targetY)
    {
        var tiles = new List<Vector2Int>();
        int dx = targetX - startX;
        int dy = targetY - startY;
        int samples = Mathf.Max(1, Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) * 2);

        for (int i = 1; i <= samples; i++)
        {
            float t = i / (float)samples;
            int x = Mathf.FloorToInt(startX + dx * t + 0.5f);
            int y = Mathf.FloorToInt(startY + dy * t + 0.5f);
            if (tiles.Count > 0 && tiles[tiles.Count - 1].x == x && tiles[tiles.Count - 1].y == y)
                continue;
            tiles.Add(new Vector2Int(x, y));
        }

        if (tiles.Count == 0 || tiles[tiles.Count - 1].x != targetX || tiles[tiles.Count - 1].y != targetY)
            tiles.Add(new Vector2Int(targetX, targetY));
        return tiles;
    }

    static bool IsStraightEightDirection(int dx, int dy)
    {
        return dx == 0 || dy == 0 || Mathf.Abs(dx) == Mathf.Abs(dy);
    }

    static bool IsLineTileOpen(SrpBattleState state, int shooterId, int exceptUnitId, int x, int y)
    {
        if (!state.InBounds(x, y) || !state.IsWalkableTile(x, y))
            return false;

        var occupant = state.GetOccupant(x, y);
        return occupant == null || occupant.id == shooterId || occupant.id == exceptUnitId;
    }

    static SrpFacing ResolveFacing(int dx, int dy)
    {
        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
            return dx >= 0 ? SrpFacing.East : SrpFacing.West;
        return dy >= 0 ? SrpFacing.North : SrpFacing.South;
    }
}

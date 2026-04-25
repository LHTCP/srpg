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
        int range = watcher.overwatchRange > 0 ? watcher.overwatchRange : watcher.attackRange;
        return IsTileInLineOfSight(state, watcher, target.anchorX, target.anchorY, range, target.id);
    }

    public static bool IsTileInLineOfSight(
        SrpBattleState state,
        SrpUnitRuntime watcher,
        int targetX,
        int targetY,
        int range = -1,
        int exceptUnitId = -1)
    {
        if (state == null || watcher == null || watcher.eliminated)
            return false;
        if (!state.InBounds(targetX, targetY) || !state.IsWalkableTile(targetX, targetY))
            return false;

        int dx = targetX - watcher.anchorX;
        int dy = targetY - watcher.anchorY;
        if (dx == 0 && dy == 0)
            return false;
        int distance = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
        int effectiveRange = range > 0 ? range : watcher.attackRange;
        if (distance > effectiveRange)
            return false;
        if (!IsStraightEightDirection(dx, dy))
            return false;

        int stepX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
        int stepY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);
        int x = watcher.anchorX + stepX;
        int y = watcher.anchorY + stepY;
        while (x != targetX || y != targetY)
        {
            if (!IsLineTileOpen(state, watcher.id, exceptUnitId, x, y))
                return false;
            x += stepX;
            y += stepY;
        }

        return true;
    }

    static bool IsStraightEightDirection(int dx, int dy)
    {
        return dx == 0 || dy == 0 || Mathf.Abs(dx) == Mathf.Abs(dy);
    }

    static bool IsLineTileOpen(SrpBattleState state, int watcherId, int exceptUnitId, int x, int y)
    {
        if (!state.InBounds(x, y) || !state.IsWalkableTile(x, y))
            return false;

        var occupant = state.GetOccupant(x, y);
        return occupant == null || occupant.id == watcherId || occupant.id == exceptUnitId;
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
        watcher.lastReactionKind = SrpReactionKind.ReactionShot;
        watcher.lastReactionRound = state.RoundNumber;
        watcher.lastReactionSourceId = target.id;
        outcome = SrpCombatResolver.ApplyAttack(state, watcher, target);
        return true;
    }
}

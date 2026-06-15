using System;
using System.Collections.Generic;
using UnityEngine;

public enum SrpWeaponClass
{
    Firearm,
    Melee,
    Magic,
}

public enum SrpBasicAttackKind
{
    Melee,
    Firearm,
}

public enum SrpStance
{
    Aggressive,
    Defensive,
}

public enum SrpFacing
{
    North,
    East,
    South,
    West,
}

public enum SrpReactionKind
{
    None,
    Guard,
    Dodge,
    Parry,
    ReactionShot,
}

[System.Flags]
public enum SrpCombatTag
{
    None = 0,
    Marked = 1 << 0,
    BalanceBroken = 1 << 1,
    KillOrder = 1 << 2,
}

public static class SrpCombatTagUtility
{
    public static bool TryParse(string raw, out SrpCombatTag tag)
    {
        tag = SrpCombatTag.None;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string key = raw.Trim().ToLowerInvariant()
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty);
        switch (key)
        {
            case "mark":
            case "marked":
            case "표식":
                tag = SrpCombatTag.Marked;
                return true;
            case "balancebroken":
            case "breakbalance":
            case "균형붕괴":
                tag = SrpCombatTag.BalanceBroken;
                return true;
            case "killorder":
            case "executeorder":
            case "사살지시":
                tag = SrpCombatTag.KillOrder;
                return true;
            default:
                return false;
        }
    }

    public static string GetDisplayName(SrpCombatTag tag)
    {
        switch (tag)
        {
            case SrpCombatTag.Marked:
                return "표식";
            case SrpCombatTag.BalanceBroken:
                return "균형 붕괴";
            case SrpCombatTag.KillOrder:
                return "사살 지시";
            default:
                return "없음";
        }
    }

    public static string BuildSummary(SrpCombatTag tags)
    {
        if (tags == SrpCombatTag.None)
            return string.Empty;

        var parts = new List<string>();
        if ((tags & SrpCombatTag.Marked) != 0)
            parts.Add(GetDisplayName(SrpCombatTag.Marked));
        if ((tags & SrpCombatTag.BalanceBroken) != 0)
            parts.Add(GetDisplayName(SrpCombatTag.BalanceBroken));
        if ((tags & SrpCombatTag.KillOrder) != 0)
            parts.Add(GetDisplayName(SrpCombatTag.KillOrder));
        return string.Join("/", parts);
    }
}

/// <summary>
/// 전장 위 유닛 인스턴스 (시뮬레이션).
/// </summary>
[Serializable]
public class SrpUnitRuntime
{
    public const int DefaultFirearmMaxAmmo = 1;
    public const int DefaultHumanFirearmRange = 3;

    public int id;
    public string templateId;
    public string displayName = "Unit";
    public int owner;
    public int anchorX;
    public int anchorY;
    public List<Vector2Int> footprintOffsets = new List<Vector2Int> { Vector2Int.zero };

    public int hp;
    public int maxHp;
    public int pg;
    public int maxPg;
    public int actionPoints;
    public int maxActionPoints;
    public int reactionPoints;
    public int maxReactionPoints;
    public int speed;
    public SrpWeaponClass weaponClass;
    public SrpStance stance;
    public SrpFacing facing;

    // legacy template compatibility is handled in SrpUnitTemplateData only.
    public int moveRange;
    public int attackRange;
    public int attackPower;
    public int ammo;
    public int maxAmmo;
    public int frozenHeart;
    public int tags;
    public int combatTags;
    public List<string> skillIds = new List<string>();
    public List<SrpSkillRuntime> skillRuntimes = new List<SrpSkillRuntime>();

    public bool groggy;
    public bool eliminated;

    public bool hasMovedThisActivation;
    public bool hasAttackedThisActivation;
    public bool hasUsedSkillThisActivation;
    public bool hasReloadedThisActivation;
    public bool passiveAppliedThisTurn;
    public SrpReactionKind lastReactionKind;
    public int lastReactionRound;
    public int lastReactionSourceId;
    public bool overwatchArmed;
    public int overwatchRange;
    public int overwatchRound;
    public bool coverActive;
    public int coverRound;
    public int coverSourceX;
    public int coverSourceY;
    public int defensiveHitsTakenThisRound;
    public int defensiveHitsRound;

    public SrpUnitRuntime Clone()
    {
        var u = new SrpUnitRuntime
        {
            id = id,
            templateId = templateId,
            displayName = displayName,
            owner = owner,
            anchorX = anchorX,
            anchorY = anchorY,
            footprintOffsets = new List<Vector2Int>(footprintOffsets),
            hp = hp,
            maxHp = maxHp,
            pg = pg,
            maxPg = maxPg,
            actionPoints = actionPoints,
            maxActionPoints = maxActionPoints,
            reactionPoints = reactionPoints,
            maxReactionPoints = maxReactionPoints,
            speed = speed,
            weaponClass = weaponClass,
            stance = stance,
            facing = facing,
            moveRange = moveRange,
            attackRange = attackRange,
            attackPower = attackPower,
            ammo = ammo,
            maxAmmo = maxAmmo,
            frozenHeart = frozenHeart,
            tags = tags,
            combatTags = combatTags,
            skillIds = new List<string>(skillIds),
            groggy = groggy,
            eliminated = eliminated,
            hasMovedThisActivation = hasMovedThisActivation,
            hasAttackedThisActivation = hasAttackedThisActivation,
            hasUsedSkillThisActivation = hasUsedSkillThisActivation,
            hasReloadedThisActivation = hasReloadedThisActivation,
            passiveAppliedThisTurn = passiveAppliedThisTurn,
            lastReactionKind = lastReactionKind,
            lastReactionRound = lastReactionRound,
            lastReactionSourceId = lastReactionSourceId,
            overwatchArmed = overwatchArmed,
            overwatchRange = overwatchRange,
            overwatchRound = overwatchRound,
            coverActive = coverActive,
            coverRound = coverRound,
            coverSourceX = coverSourceX,
            coverSourceY = coverSourceY,
            defensiveHitsTakenThisRound = defensiveHitsTakenThisRound,
            defensiveHitsRound = defensiveHitsRound,
        };
        foreach (var sr in skillRuntimes)
            u.skillRuntimes.Add(sr.Clone());
        return u;
    }

    public bool HasTag(SrpUnitTags t)
    {
        return (tags & (int)t) != 0;
    }

    public bool HasCombatTag(SrpCombatTag tag)
    {
        return (combatTags & (int)tag) != 0;
    }

    public void AddCombatTag(SrpCombatTag tag)
    {
        combatTags |= (int)tag;
    }

    public void RemoveCombatTag(SrpCombatTag tag)
    {
        combatTags &= ~(int)tag;
    }

    public SrpCombatTag ConsumeCombatTags(SrpCombatTag mask)
    {
        var consumed = (SrpCombatTag)(combatTags & (int)mask);
        combatTags &= ~(int)consumed;
        return consumed;
    }

    public bool UsesAmmo => maxAmmo > 0;

    public bool HasAmmoForAttack()
    {
        return !UsesAmmo || ammo > 0;
    }

    public bool SpendAmmoForAttack()
    {
        if (!UsesAmmo)
            return true;
        if (ammo <= 0)
            return false;
        ammo = Mathf.Max(0, ammo - 1);
        return true;
    }

    public bool CanReload()
    {
        return UsesAmmo && ammo < maxAmmo;
    }

    public bool ReloadAmmo()
    {
        if (!CanReload())
            return false;
        ammo = maxAmmo;
        return true;
    }

    public void ClearCover()
    {
        coverActive = false;
        coverRound = 0;
        coverSourceX = 0;
        coverSourceY = 0;
    }

    public void SetCover(int round, int sourceX, int sourceY)
    {
        coverActive = true;
        coverRound = round;
        coverSourceX = sourceX;
        coverSourceY = sourceY;
    }
}

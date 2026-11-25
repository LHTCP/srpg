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

/// <summary>
/// 사격에 쓰는 총기의 종류. 기본 사격의 HP 피해 식이 다르다(SrpCombatResolver.GetBaseBasicAttackDamage).
/// 값이 없는 기존 데이터는 Auto로 읽힌다.
/// </summary>
public enum SrpFirearmType
{
    /// <summary>주무장을 따른다: 주무장(weaponClass)이 총기면 소총, 아니면 권총(부무장).</summary>
    Auto = 0,
    Rifle = 1,
    Pistol = 2,
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

    /// <summary>유닛이 가진 태그와 남은 지속(만료 라운드까지 남은 라운드 수)을 함께 적는다.</summary>
    public static string BuildSummary(SrpUnitRuntime unit, int currentRound)
    {
        if (unit == null || unit.combatTags == 0)
            return string.Empty;

        var parts = new List<string>();
        foreach (var tag in new[] { SrpCombatTag.Marked, SrpCombatTag.BalanceBroken, SrpCombatTag.KillOrder })
        {
            if (!unit.HasCombatTag(tag))
                continue;
            int expireRound = unit.GetCombatTagExpireRound(tag);
            parts.Add(expireRound > 0
                ? $"{GetDisplayName(tag)}({Mathf.Max(0, expireRound - currentRound) + 1}R)"
                : GetDisplayName(tag));
        }
        return string.Join("/", parts);
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
    public const int GroggyRecoveryPgPercent = 35;
    const int CombatTagSlots = 3;

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
    /// <summary>사격에 쓰는 총기의 종류. Auto면 주무장을 따른다(SrpCombatResolver.ResolveFirearmType).</summary>
    public SrpFirearmType firearmType;
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
    /// <summary>
    /// 전투 태그별 만료 라운드(태그 비트 순서). 0이면 소모 전까지 유지한다.
    /// 값이 R이면 R라운드가 끝날 때까지 유지하고 R+1라운드 시작에 사라진다.
    /// </summary>
    public int[] combatTagExpireRounds = new int[CombatTagSlots];
    public List<string> skillIds = new List<string>();
    public List<SrpSkillRuntime> skillRuntimes = new List<SrpSkillRuntime>();

    public bool groggy;
    /// <summary>재붕괴 보호: 그로기에서 회복한 뒤 자신의 다음 활성화가 시작될 때까지 PG가 1 밑으로 내려가지 않는다.</summary>
    public bool pgBreakGuard;
    public bool eliminated;

    /// <summary>지휘관 표시(맵 배치 데이터). 지휘관 처치 승리 조건을 켠 맵에서만 승패에 쓰인다.</summary>
    public bool isCommander;

    public bool hasMovedThisActivation;
    /// <summary>이번 활성화에 한 이동 횟수. 짧은 이동/긴 이동 규칙의 활성화당 이동 횟수 제한에 쓴다.</summary>
    public int movesThisActivation;
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
            firearmType = firearmType,
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
            combatTagExpireRounds = (int[])combatTagExpireRounds.Clone(),
            skillIds = new List<string>(skillIds),
            groggy = groggy,
            pgBreakGuard = pgBreakGuard,
            eliminated = eliminated,
            isCommander = isCommander,
            hasMovedThisActivation = hasMovedThisActivation,
            movesThisActivation = movesThisActivation,
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

    /// <summary>
    /// 전투 태그를 부여하거나 갱신한다. expireRound가 0이면 소모 전까지 유지하고,
    /// 양수면 그 라운드가 끝날 때까지 유지한다. 이미 있는 태그는 만료가 새 값으로 바뀐다.
    /// </summary>
    public void AddCombatTag(SrpCombatTag tag, int expireRound = 0)
    {
        combatTags |= (int)tag;
        SetCombatTagExpireRounds(tag, expireRound > 0 ? expireRound : 0);
    }

    public void RemoveCombatTag(SrpCombatTag tag)
    {
        combatTags &= ~(int)tag;
        SetCombatTagExpireRounds(tag, 0);
    }

    public SrpCombatTag ConsumeCombatTags(SrpCombatTag mask)
    {
        var consumed = (SrpCombatTag)(combatTags & (int)mask);
        combatTags &= ~(int)consumed;
        SetCombatTagExpireRounds(consumed, 0);
        return consumed;
    }

    /// <summary>태그의 만료 라운드. 0이면 만료 없음(또는 태그 없음).</summary>
    public int GetCombatTagExpireRound(SrpCombatTag tag)
    {
        for (int slot = 0; slot < CombatTagSlots; slot++)
        {
            if (((int)tag & (1 << slot)) != 0)
                return combatTagExpireRounds[slot];
        }
        return 0;
    }

    /// <summary>만료 라운드가 지난 태그를 지운다. 지운 태그를 돌려준다.</summary>
    public SrpCombatTag ExpireCombatTags(int currentRound)
    {
        var expired = SrpCombatTag.None;
        for (int slot = 0; slot < CombatTagSlots; slot++)
        {
            int bit = 1 << slot;
            if ((combatTags & bit) == 0)
                continue;
            int expireRound = combatTagExpireRounds[slot];
            if (expireRound > 0 && currentRound > expireRound)
                expired |= (SrpCombatTag)bit;
        }
        if (expired != SrpCombatTag.None)
            RemoveCombatTag(expired);
        return expired;
    }

    void SetCombatTagExpireRounds(SrpCombatTag tags, int expireRound)
    {
        for (int slot = 0; slot < CombatTagSlots; slot++)
        {
            if (((int)tags & (1 << slot)) != 0)
                combatTagExpireRounds[slot] = expireRound;
        }
    }

    /// <summary>PG 붕괴(그로기 포함) 상태. 다음 활성화에서 회복할 때까지 행동·반응을 하지 못한다.</summary>
    public bool IsPgBroken => groggy || pg <= 0;

    /// <summary>
    /// PG 피해를 적용한다. 재붕괴 보호 중이면 PG를 1에서 멈춘다.
    /// 이번 피해로 PG가 0이 되어 그로기에 들어가면 true.
    /// </summary>
    public bool ApplyPgDamage(int pgDamage, out int appliedDamage)
    {
        int before = pg;
        int floor = pgBreakGuard && before > 0 ? 1 : 0;
        int after = before - (pgDamage > 0 ? pgDamage : 0);
        if (after < floor)
            after = floor;
        if (after > before)
            after = before;
        pg = after;
        appliedDamage = before - after;
        if (before > 0 && pg <= 0)
        {
            groggy = true;
            return true;
        }
        return false;
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

using System;
using System.Collections.Generic;
using UnityEngine;

public enum SrpWeaponClass
{
    Firearm,
    Melee,
    Magic,
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

/// <summary>
/// 전장 위 유닛 인스턴스 (시뮬레이션).
/// </summary>
[Serializable]
public class SrpUnitRuntime
{
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
    public int frozenHeart;
    public int tags;
    public List<string> skillIds = new List<string>();
    public List<SrpSkillRuntime> skillRuntimes = new List<SrpSkillRuntime>();

    public bool groggy;
    public bool eliminated;

    public bool hasMovedThisActivation;
    public bool hasAttackedThisActivation;
    public bool hasUsedSkillThisActivation;
    public bool passiveAppliedThisTurn;
    public SrpReactionKind lastReactionKind;
    public int lastReactionRound;
    public int lastReactionSourceId;
    public bool overwatchArmed;
    public int overwatchRange;
    public int overwatchRound;
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
            frozenHeart = frozenHeart,
            tags = tags,
            skillIds = new List<string>(skillIds),
            groggy = groggy,
            eliminated = eliminated,
            hasMovedThisActivation = hasMovedThisActivation,
            hasAttackedThisActivation = hasAttackedThisActivation,
            hasUsedSkillThisActivation = hasUsedSkillThisActivation,
            passiveAppliedThisTurn = passiveAppliedThisTurn,
            lastReactionKind = lastReactionKind,
            lastReactionRound = lastReactionRound,
            lastReactionSourceId = lastReactionSourceId,
            overwatchArmed = overwatchArmed,
            overwatchRange = overwatchRange,
            overwatchRound = overwatchRound,
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
}

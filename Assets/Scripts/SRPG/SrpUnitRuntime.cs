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

    // legacy fields (기존 데이터/메이커 호환)
    public int ap;
    public int maxAp;
    public int posture;
    public int maxPosture;
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
            ap = ap,
            maxAp = maxAp,
            posture = posture,
            maxPosture = maxPosture,
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

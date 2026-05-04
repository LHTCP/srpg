using System;
using UnityEngine;

/// <summary>
/// JSON 직렬화용 맵/유닛 템플릿 스키마 v1 (JsonUtility).
/// </summary>
[Serializable]
public class SrpOffset
{
    public int dx;
    public int dy;
}

[Serializable]
public class SrpUnitTemplateData
{
    public string id;
    public string displayName = "Unit";
    public int moveRange = 4;
    public int attackRange = 1;
    public int attackPower = 10;
    public int maxAmmo;
    public int maxHp = 30;
    public int maxPg = 18;
    public int maxActionPoints = 2;
    public int maxReactionPoints = 1;
    public int speed = 10;
    public SrpWeaponClass weaponClass = SrpWeaponClass.Melee;
    public SrpStance stance = SrpStance.Aggressive;
    public SrpFacing facing = SrpFacing.South;

    // legacy fields (v0/v1 초기 스키마 호환)
    public int maxAp = 10;
    public int maxPosture = 100;
    public string[] skillIds = Array.Empty<string>();
    public int maxSkills = 4;
    public int frozenHeart;
    public int tags;
    public int footprintWidth = 1;
    public int footprintHeight = 1;
}

[Serializable]
public class SrpPlacementData
{
    public string templateId;
    public int owner;
    public int x;
    public int y;
    public SrpOffset[] footprint = Array.Empty<SrpOffset>();
    public string[] disabledSkillIds = Array.Empty<string>();
}

[Serializable]
public class SrpInteractionPointData
{
    public string id;
    public string displayName = "Interaction";
    public int x;
    public int y;
    public int owner = -1;
    public int requiredOwner = -1;
    public bool singleUse = true;
    public bool activated;

    public SrpInteractionPointData Clone()
    {
        return new SrpInteractionPointData
        {
            id = id,
            displayName = displayName,
            x = x,
            y = y,
            owner = owner,
            requiredOwner = requiredOwner,
            singleUse = singleUse,
            activated = activated,
        };
    }
}

public enum SrpCoverEdge
{
    North,
    East,
    South,
    West,
}

public enum SrpCoverShape
{
    Linear,
    Corner,
    UShape,
}

[Serializable]
public class SrpCoverSegmentData
{
    public int x;
    public int y;
    public SrpCoverEdge edge = SrpCoverEdge.North;
    public SrpCoverShape shape = SrpCoverShape.Linear;
    public int coverDef;
    public int coverGrd;
    public bool blocksLineOfSight;

    public SrpCoverSegmentData Clone()
    {
        return new SrpCoverSegmentData
        {
            x = x,
            y = y,
            edge = edge,
            shape = shape,
            coverDef = coverDef,
            coverGrd = coverGrd,
            blocksLineOfSight = blocksLineOfSight,
        };
    }
}

[Serializable]
public class SrpMapFileV1
{
    public int version = 2;
    public string name = "map";
    public int width = 8;
    public int height = 8;
    public bool[] walkable = Array.Empty<bool>();
    public int[] playerOrder = new[] { 0, 1 };
    public SrpUnitTemplateData[] templates = Array.Empty<SrpUnitTemplateData>();
    public SrpPlacementData[] placements = Array.Empty<SrpPlacementData>();
    public SrpInteractionPointData[] interactionPoints = Array.Empty<SrpInteractionPointData>();
    public SrpCoverSegmentData[] coverSegments = Array.Empty<SrpCoverSegmentData>();
    public string[] allowedSkillIds = Array.Empty<string>();
}

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

    /// <summary>지휘관 표시. 맵의 victory.commanderKill이 켜져 있을 때만 승패에 쓰인다.</summary>
    public bool isCommander;
}

/// <summary>상호작용 포인트를 활성화했을 때의 효과. 기존 맵(값 없음)은 None이다.</summary>
public enum SrpInteractionEffect
{
    None = 0,
    /// <summary>승리 목표. 맵의 victory.interactionObjective가 켜져 있을 때만 승패에 쓰인다.</summary>
    VictoryObjective = 1,
}

/// <summary>
/// 맵별 승리 조건. 적 전멸은 항상 적용되고, 아래 조건은 켠 맵에서만 추가로 적용된다.
/// 기존 맵(값 없음)은 모두 꺼져 있어 전멸 규칙만 쓴다.
/// </summary>
[Serializable]
public class SrpVictoryRuleData
{
    /// <summary>지휘관(isCommander)이 있던 진영은 지휘관이 모두 쓰러지면 패배한다.</summary>
    public bool commanderKill;

    /// <summary>자기 진영이 쓸 수 있는 승리 목표 포인트를 모두 활성화해 보유한 진영이 승리한다.</summary>
    public bool interactionObjective;

    public SrpVictoryRuleData Clone()
    {
        return new SrpVictoryRuleData
        {
            commanderKill = commanderKill,
            interactionObjective = interactionObjective,
        };
    }
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
    public SrpInteractionEffect effect = SrpInteractionEffect.None;

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
            effect = effect,
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
public class SrpCoverObjectData
{
    public int x;
    public int y;
    public int coverDef = 2;
    public int coverGrd = 1;
    public bool blocksLineOfSight = true;
    public string visualKey = "ruin";

    public SrpCoverObjectData Clone()
    {
        return new SrpCoverObjectData
        {
            x = x,
            y = y,
            coverDef = coverDef,
            coverGrd = coverGrd,
            blocksLineOfSight = blocksLineOfSight,
            visualKey = visualKey,
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
    public SrpCoverObjectData[] coverObjects = Array.Empty<SrpCoverObjectData>();
    public SrpCoverSegmentData[] coverSegments = Array.Empty<SrpCoverSegmentData>();
    public string[] allowedSkillIds = Array.Empty<string>();
    public SrpVictoryRuleData victory = new SrpVictoryRuleData();
}

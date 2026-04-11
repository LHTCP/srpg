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
    public int maxHp = 30;
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
public class SrpMapFileV1
{
    public int version = 1;
    public string name = "map";
    public int width = 8;
    public int height = 8;
    public bool[] walkable = Array.Empty<bool>();
    public int[] playerOrder = new[] { 0, 1 };
    public SrpUnitTemplateData[] templates = Array.Empty<SrpUnitTemplateData>();
    public SrpPlacementData[] placements = Array.Empty<SrpPlacementData>();
    public string[] allowedSkillIds = Array.Empty<string>();
}

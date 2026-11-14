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

    public SrpUnitTemplateData Clone()
    {
        var copy = (SrpUnitTemplateData)MemberwiseClone();
        copy.skillIds = skillIds != null ? (string[])skillIds.Clone() : Array.Empty<string>();
        return copy;
    }
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

/// <summary>
/// 맵 메이커가 편집하지 않는 맵 데이터를 불러오기에서 저장까지 그대로 들고 가는 묶음.
/// 메이커는 저장할 때 맵 객체를 새로 만들기 때문에, 이 묶음에 담지 않은 값은 저장 시 기본값으로 돌아간다.
/// 대상: 승리 조건(victory), 상호작용 포인트(승리 목표 효과 포함), 허용 스킬 목록(allowedSkillIds), 유닛 템플릿(templates).
/// 승리 조건과 상호작용 포인트는 메이커가 이 묶음 안에서 직접 고친다(SrpMapObjectiveEdit). 허용 스킬 목록은 보존만 한다.
/// 유닛 템플릿은 유닛 DB에 없는 템플릿을 쓰는 배치가 저장에서 빠지지 않게 하는 데만 쓴다(ResolveTemplates).
/// 배치의 지휘관 표시는 메이커가 배치별로 들고 있고, 엄폐물과 엄폐 segment는 메이커가 따로 편집한다.
/// </summary>
public class SrpMapCarryOver
{
    public SrpVictoryRuleData victory = new SrpVictoryRuleData();
    public SrpInteractionPointData[] interactionPoints = Array.Empty<SrpInteractionPointData>();
    public string[] allowedSkillIds = Array.Empty<string>();
    /// <summary>불러온 맵에 들어 있던 유닛 템플릿. 프리셋 전용 유닛처럼 유닛 DB에 없는 템플릿을 되살리는 데 쓴다.</summary>
    public SrpUnitTemplateData[] templates = Array.Empty<SrpUnitTemplateData>();

    public static SrpMapCarryOver Capture(SrpMapFileV1 map)
    {
        var carry = new SrpMapCarryOver();
        if (map == null)
            return carry;

        if (map.victory != null)
            carry.victory = map.victory.Clone();
        if (map.allowedSkillIds != null)
            carry.allowedSkillIds = (string[])map.allowedSkillIds.Clone();
        if (map.interactionPoints != null)
        {
            var points = new System.Collections.Generic.List<SrpInteractionPointData>();
            foreach (var point in map.interactionPoints)
            {
                if (point != null)
                    points.Add(point.Clone());
            }
            carry.interactionPoints = points.ToArray();
        }
        if (map.templates != null)
        {
            var templates = new System.Collections.Generic.List<SrpUnitTemplateData>();
            foreach (var template in map.templates)
            {
                if (template != null && !string.IsNullOrEmpty(template.id))
                    templates.Add(template.Clone());
            }
            carry.templates = templates.ToArray();
        }
        return carry;
    }

    /// <summary>
    /// 저장할 맵에 넣을 템플릿 목록. 배치에 쓰인 템플릿만, 처음 쓰인 순서대로 담는다.
    /// 유닛 DB에 있는 템플릿은 DB 값을 쓰고(메이커의 기존 동작), DB에 없으면 불러온 맵에 들어 있던 템플릿을 쓴다.
    /// 어느 쪽에도 없는 id는 건너뛴다.
    /// </summary>
    public SrpUnitTemplateData[] ResolveTemplates(
        System.Collections.Generic.IEnumerable<string> placedTemplateIds,
        SrpUnitTemplateData[] unitDb)
    {
        var fromDb = new System.Collections.Generic.Dictionary<string, SrpUnitTemplateData>();
        if (unitDb != null)
        {
            foreach (var unit in unitDb)
            {
                if (unit != null && !string.IsNullOrEmpty(unit.id))
                    fromDb[unit.id] = unit;
            }
        }

        var used = new System.Collections.Generic.List<SrpUnitTemplateData>();
        var usedIds = new System.Collections.Generic.HashSet<string>();
        if (placedTemplateIds == null)
            return used.ToArray();

        foreach (string id in placedTemplateIds)
        {
            if (string.IsNullOrEmpty(id) || usedIds.Contains(id))
                continue;
            if (!fromDb.TryGetValue(id, out var template))
                template = FindCarriedTemplate(id);
            if (template == null)
                continue;
            used.Add(template);
            usedIds.Add(id);
        }
        return used.ToArray();
    }

    SrpUnitTemplateData FindCarriedTemplate(string id)
    {
        if (templates == null)
            return null;
        foreach (var template in templates)
        {
            if (template != null && template.id == id)
                return template.Clone();
        }
        return null;
    }

    /// <summary>보존한 값을 저장할 맵에 되돌려 넣는다. 맵 크기를 벗어난 상호작용 포인트는 뺀다.</summary>
    public void ApplyTo(SrpMapFileV1 map)
    {
        if (map == null)
            return;

        map.victory = victory != null ? victory.Clone() : new SrpVictoryRuleData();
        map.allowedSkillIds = allowedSkillIds != null ? (string[])allowedSkillIds.Clone() : Array.Empty<string>();
        var points = new System.Collections.Generic.List<SrpInteractionPointData>();
        if (interactionPoints != null)
        {
            foreach (var point in interactionPoints)
            {
                if (point == null || point.x < 0 || point.y < 0 || point.x >= map.width || point.y >= map.height)
                    continue;
                points.Add(point.Clone());
            }
        }
        map.interactionPoints = points.ToArray();
    }
}

/// <summary>
/// 맵 메이커의 엄폐 편집 규칙(순수 로직). 화면 없이 테스트할 수 있도록 컨트롤러에서 분리했다.
/// </summary>
public static class SrpMapCoverEdit
{
    /// <summary>
    /// 칸 안에서 클릭한 위치(칸 중심 기준 -0.5~0.5)로 가장 가까운 가장자리를 고른다.
    /// 북쪽은 y가 커지는 쪽, 동쪽은 x가 커지는 쪽이다.
    /// </summary>
    public static SrpCoverEdge ResolveEdge(float offsetX, float offsetY)
    {
        float ax = offsetX < 0f ? -offsetX : offsetX;
        float ay = offsetY < 0f ? -offsetY : offsetY;
        if (ax >= ay)
            return offsetX >= 0f ? SrpCoverEdge.East : SrpCoverEdge.West;
        return offsetY >= 0f ? SrpCoverEdge.North : SrpCoverEdge.South;
    }

    /// <summary>메이커에서 고르는 엄폐 강도. 새로 놓는 엄폐의 감쇠 수치(coverDef/coverGrd)를 정한다.</summary>
    public enum Strength
    {
        /// <summary>규칙 기본값. 엄폐 벽은 0/0으로 저장해 전투 규칙의 기본 감쇠를 쓰고, 엄폐물은 2/1이다.</summary>
        Default = 0,
        /// <summary>약한 엄폐: 1/1.</summary>
        Light = 1,
        /// <summary>강한 엄폐: 3/2.</summary>
        Heavy = 2,
    }

    public static Strength NextStrength(Strength current)
    {
        switch (current)
        {
            case Strength.Default: return Strength.Light;
            case Strength.Light: return Strength.Heavy;
            default: return Strength.Default;
        }
    }

    public static string GetStrengthLabel(Strength strength)
    {
        switch (strength)
        {
            case Strength.Light: return "약";
            case Strength.Heavy: return "강";
            default: return "기본";
        }
    }

    /// <summary>강도에 따른 감쇠 수치. 엄폐물과 엄폐 벽의 기본값이 달라 종류를 받는다.</summary>
    public static void ResolveStrength(Strength strength, bool isCoverObject, out int coverDef, out int coverGrd)
    {
        switch (strength)
        {
            case Strength.Light:
                coverDef = 1;
                coverGrd = 1;
                break;
            case Strength.Heavy:
                coverDef = 3;
                coverGrd = 2;
                break;
            default:
                coverDef = isCoverObject ? 2 : 0;
                coverGrd = isCoverObject ? 1 : 0;
                break;
        }
    }

    /// <summary>칸의 엄폐물을 켜고 끈다. 새로 놓았으면 true, 있던 것을 지웠으면 false.</summary>
    public static bool ToggleCoverObject(
        System.Collections.Generic.List<SrpCoverObjectData> objects, int x, int y, bool blocksLineOfSight,
        Strength strength = Strength.Default)
    {
        for (int i = objects.Count - 1; i >= 0; i--)
        {
            if (objects[i] != null && objects[i].x == x && objects[i].y == y)
            {
                objects.RemoveAt(i);
                return false;
            }
        }
        ResolveStrength(strength, true, out int coverDef, out int coverGrd);
        objects.Add(new SrpCoverObjectData
        {
            x = x,
            y = y,
            coverDef = coverDef,
            coverGrd = coverGrd,
            blocksLineOfSight = blocksLineOfSight,
        });
        return true;
    }

    /// <summary>
    /// 칸 가장자리의 엄폐 segment를 켜고 끈다. 새로 놓았으면 true, 같은 칸·같은 가장자리의 것을 지웠으면 false.
    /// 강도가 기본이면 감쇠 수치를 0으로 두어 전투 규칙의 기본값을 쓴다.
    /// </summary>
    public static bool ToggleCoverSegment(
        System.Collections.Generic.List<SrpCoverSegmentData> segments, int x, int y, SrpCoverEdge edge, bool blocksLineOfSight,
        Strength strength = Strength.Default)
    {
        for (int i = segments.Count - 1; i >= 0; i--)
        {
            var segment = segments[i];
            if (segment != null && segment.x == x && segment.y == y && segment.edge == edge)
            {
                segments.RemoveAt(i);
                return false;
            }
        }
        ResolveStrength(strength, false, out int coverDef, out int coverGrd);
        segments.Add(new SrpCoverSegmentData
        {
            x = x,
            y = y,
            edge = edge,
            shape = SrpCoverShape.Linear,
            coverDef = coverDef,
            coverGrd = coverGrd,
            blocksLineOfSight = blocksLineOfSight,
        });
        return true;
    }

    /// <summary>맵에서 불러온 엄폐 데이터를 편집용 목록으로 복사한다. 맵 크기를 벗어난 것은 뺀다.</summary>
    public static void Load(
        SrpMapFileV1 map,
        System.Collections.Generic.List<SrpCoverObjectData> objects,
        System.Collections.Generic.List<SrpCoverSegmentData> segments)
    {
        objects.Clear();
        segments.Clear();
        if (map == null)
            return;

        if (map.coverObjects != null)
        {
            foreach (var coverObject in map.coverObjects)
            {
                if (coverObject != null && InBounds(map, coverObject.x, coverObject.y))
                    objects.Add(coverObject.Clone());
            }
        }
        if (map.coverSegments != null)
        {
            foreach (var segment in map.coverSegments)
            {
                if (segment != null && InBounds(map, segment.x, segment.y))
                    segments.Add(segment.Clone());
            }
        }
    }

    /// <summary>편집한 엄폐 데이터를 저장할 맵에 넣는다. 맵 크기를 벗어난 것은 뺀다.</summary>
    public static void ApplyTo(
        SrpMapFileV1 map,
        System.Collections.Generic.List<SrpCoverObjectData> objects,
        System.Collections.Generic.List<SrpCoverSegmentData> segments)
    {
        if (map == null)
            return;

        var outObjects = new System.Collections.Generic.List<SrpCoverObjectData>();
        foreach (var coverObject in objects)
        {
            if (coverObject != null && InBounds(map, coverObject.x, coverObject.y))
                outObjects.Add(coverObject.Clone());
        }
        var outSegments = new System.Collections.Generic.List<SrpCoverSegmentData>();
        foreach (var segment in segments)
        {
            if (segment != null && InBounds(map, segment.x, segment.y))
                outSegments.Add(segment.Clone());
        }
        map.coverObjects = outObjects.ToArray();
        map.coverSegments = outSegments.ToArray();
    }

    static bool InBounds(SrpMapFileV1 map, int x, int y)
    {
        return x >= 0 && y >= 0 && x < map.width && y < map.height;
    }
}

/// <summary>
/// 맵 메이커의 상호작용 포인트 편집 규칙(순수 로직).
/// </summary>
public static class SrpMapObjectiveEdit
{
    /// <summary>
    /// 칸의 상호작용 포인트를 켜고 끈다. 새로 놓았으면 true, 있던 것을 지웠으면 false.
    /// requiredOwner는 이 포인트를 쓸 수 있는 진영(-1이면 누구나)이고,
    /// victoryObjective가 true면 승리 목표 효과를 붙인다(맵의 victory.interactionObjective가 켜져 있어야 승패에 쓰인다).
    /// </summary>
    public static bool ToggleInteractionPoint(
        SrpMapCarryOver carry, int x, int y, int requiredOwner, bool victoryObjective)
    {
        var points = new System.Collections.Generic.List<SrpInteractionPointData>();
        bool removed = false;
        if (carry.interactionPoints != null)
        {
            foreach (var point in carry.interactionPoints)
            {
                if (point == null)
                    continue;
                if (point.x == x && point.y == y)
                {
                    removed = true;
                    continue;
                }
                points.Add(point);
            }
        }

        if (!removed)
        {
            points.Add(new SrpInteractionPointData
            {
                id = NextPointId(points),
                displayName = victoryObjective ? "승리 목표" : "상호작용 지점",
                x = x,
                y = y,
                owner = -1,
                requiredOwner = requiredOwner,
                singleUse = true,
                activated = false,
                effect = victoryObjective ? SrpInteractionEffect.VictoryObjective : SrpInteractionEffect.None,
            });
        }

        carry.interactionPoints = points.ToArray();
        return !removed;
    }

    /// <summary>이미 있는 id와 겹치지 않는 "point_N"을 만든다.</summary>
    static string NextPointId(System.Collections.Generic.List<SrpInteractionPointData> points)
    {
        for (int n = 1; ; n++)
        {
            string candidate = "point_" + n;
            bool used = false;
            foreach (var point in points)
            {
                if (point.id == candidate)
                {
                    used = true;
                    break;
                }
            }
            if (!used)
                return candidate;
        }
    }
}

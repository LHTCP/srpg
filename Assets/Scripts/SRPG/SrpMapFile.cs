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
    /// <summary>사격에 쓰는 총기의 종류. 비워 두면(Auto) 주무장이 총기일 때 소총, 아니면 권총(부무장)이다.</summary>
    public SrpFirearmType firearmType = SrpFirearmType.Auto;
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
/// 맵이 추가로 거는 조건 한 건. 판정은 SrpVictory에 등록된 같은 type의 판정기(ISrpVictoryCondition)가 한다.
/// 승리 조건 목록(victory.conditions)에 넣으면 조건이 성립한 진영이 승리하고,
/// 패배 조건 목록(victory.defeatConditions)에 넣으면 조건이 성립한 진영이 패배한다.
/// 등록되지 않은 type은 판정에서 빠진다. 내장 type은 SrpVictoryConditionTypes에 있다.
/// </summary>
[Serializable]
public class SrpVictoryConditionData
{
    /// <summary>조건 종류. ISrpVictoryCondition.Type과 같아야 한다.</summary>
    public string type;

    /// <summary>이 조건이 적용되는 진영. -1이면 모든 진영.</summary>
    public int owner = -1;

    /// <summary>조건별 수치(예: 라운드 수). 뜻은 판정기가 정한다.</summary>
    public int value;

    /// <summary>
    /// 조건별 대상 id. 뜻은 판정기가 정한다.
    /// 내장 조건에서는 대상 유닛이다: 비어 있으면 모든 유닛, "commander"면 지휘관, 그 밖에는 유닛 템플릿 id.
    /// </summary>
    public string targetId;

    /// <summary>조건별 대상 칸(도달 칸, 거점 중심). 칸을 쓰지 않는 조건에서는 무시한다.</summary>
    public int x;
    public int y;

    /// <summary>거점의 반경(대각선 포함). 0이면 대상 칸 하나다. 거점을 쓰지 않는 조건에서는 무시한다.</summary>
    public int radius;

    public SrpVictoryConditionData Clone()
    {
        return (SrpVictoryConditionData)MemberwiseClone();
    }
}

/// <summary>
/// 맵별 승리 조건. 적 전멸은 항상 적용되고, 아래 조건은 켠 맵에서만 추가로 적용된다.
/// 기존 맵(값 없음)은 모두 꺼져 있어 전멸 규칙만 쓴다.
/// 맵마다 조건 조합이 다를 수 있다. 기본 제공 두 조건은 스위치로 켜고, 그 밖의 조건은 승리 조건 목록(conditions)과
/// 패배 조건 목록(defeatConditions)에 나열한다. 판정 순서는 전멸 > 패배 조건 > 승리 조건이다(SrpVictory).
/// </summary>
[Serializable]
public class SrpVictoryRuleData
{
    /// <summary>지휘관(isCommander)이 있던 진영은 지휘관이 모두 쓰러지면 패배한다.</summary>
    public bool commanderKill;

    /// <summary>자기 진영이 쓸 수 있는 승리 목표 포인트를 모두 활성화해 보유한 진영이 승리한다.</summary>
    public bool interactionObjective;

    /// <summary>승리 조건 목록. 조건이 성립한 진영이 승리한다. 적힌 순서대로 판정하며 목표 확보보다 뒤다.</summary>
    public SrpVictoryConditionData[] conditions = Array.Empty<SrpVictoryConditionData>();

    /// <summary>패배 조건 목록. 조건이 성립한 진영이 패배한다. 적힌 순서대로 판정하며 지휘관 처치보다 뒤다.</summary>
    public SrpVictoryConditionData[] defeatConditions = Array.Empty<SrpVictoryConditionData>();

    public SrpVictoryRuleData Clone()
    {
        var clone = new SrpVictoryRuleData
        {
            commanderKill = commanderKill,
            interactionObjective = interactionObjective,
        };
        clone.conditions = CloneList(conditions);
        clone.defeatConditions = CloneList(defeatConditions);
        return clone;
    }

    static SrpVictoryConditionData[] CloneList(SrpVictoryConditionData[] source)
    {
        if (source == null || source.Length == 0)
            return Array.Empty<SrpVictoryConditionData>();
        var copy = new SrpVictoryConditionData[source.Length];
        for (int i = 0; i < source.Length; i++)
            copy[i] = source[i] != null ? source[i].Clone() : null;
        return copy;
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
/// <summary>
/// 맵 메이커의 추가 조건 편집 규칙(순수 로직). 승리 조건 목록과 패배 조건 목록에 내장 조건을 넣고 뺀다.
/// 화면(SrpMapMakerController)은 버튼 값만 넘긴다.
/// </summary>
public static class SrpMapConditionEdit
{
    /// <summary>거점 유지 조건을 새로 만들 때의 반경(3x3). 다른 값은 맵 JSON에서 고친다.</summary>
    public const int DefaultHoldRadius = 1;

    /// <summary>조건의 대상 유닛을 고르는 방식.</summary>
    public enum TargetKind
    {
        /// <summary>그 진영의 모든 유닛.</summary>
        Any,
        /// <summary>지휘관 표시가 있는 유닛.</summary>
        Commander,
        /// <summary>메이커의 "배치할 유닛"에서 고른 유닛 템플릿.</summary>
        SelectedUnit,
    }

    public static string NextType(string type)
    {
        var types = SrpVictoryConditionTypes.BuiltIn;
        int index = Array.IndexOf(types, type);
        return types[(index + 1 + types.Length) % types.Length];
    }

    public static TargetKind NextTargetKind(TargetKind kind)
    {
        return (TargetKind)(((int)kind + 1) % 3);
    }

    public static string GetTypeLabel(string type)
    {
        switch (type)
        {
            case SrpVictoryConditionTypes.SurviveRounds:
                return "N라운드 버티기";
            case SrpVictoryConditionTypes.ReachTile:
                return "칸 도달";
            case SrpVictoryConditionTypes.UnitLost:
                return "유닛 상실(보호)";
            case SrpVictoryConditionTypes.HoldPoint:
                return "거점 유지";
            default:
                return type ?? string.Empty;
        }
    }

    public static string GetTargetLabel(TargetKind kind)
    {
        switch (kind)
        {
            case TargetKind.Commander:
                return "지휘관";
            case TargetKind.SelectedUnit:
                return "선택한 유닛";
            default:
                return "모든 유닛";
        }
    }

    /// <summary>수치(N라운드)를 쓰는 조건인가.</summary>
    public static bool UsesValue(string type)
    {
        return type == SrpVictoryConditionTypes.SurviveRounds || type == SrpVictoryConditionTypes.HoldPoint;
    }

    /// <summary>대상 칸을 쓰는 조건인가.</summary>
    public static bool UsesTile(string type)
    {
        return type == SrpVictoryConditionTypes.ReachTile || type == SrpVictoryConditionTypes.HoldPoint;
    }

    /// <summary>대상 유닛을 쓰는 조건인가.</summary>
    public static bool UsesTarget(string type)
    {
        return type == SrpVictoryConditionTypes.ReachTile || type == SrpVictoryConditionTypes.UnitLost;
    }

    /// <summary>대상 유닛 선택을 조건의 targetId로 바꾼다. 선택한 유닛이 없으면 null.</summary>
    public static string ResolveTargetId(TargetKind kind, string selectedUnitId)
    {
        switch (kind)
        {
            case TargetKind.Commander:
                return SrpVictoryConditionTypes.CommanderTarget;
            case TargetKind.SelectedUnit:
                return string.IsNullOrEmpty(selectedUnitId) ? null : selectedUnitId;
            default:
                return string.Empty;
        }
    }

    /// <summary>
    /// 버튼 값으로 조건 한 건을 만든다. 조건이 쓰지 않는 값은 넣지 않는다.
    /// 만들 수 없으면 null이고 error에 이유가 든다(내장 조건이 아님, 유닛 상실인데 대상이 "모든 유닛"이거나 없음).
    /// </summary>
    public static SrpVictoryConditionData Build(
        string type, int owner, int value, int x, int y, string targetId, out string error)
    {
        error = null;
        if (Array.IndexOf(SrpVictoryConditionTypes.BuiltIn, type) < 0)
        {
            error = "알 수 없는 조건입니다.";
            return null;
        }
        if (UsesTarget(type) && targetId == null)
        {
            error = "대상으로 쓸 유닛을 먼저 선택하세요.";
            return null;
        }
        if (type == SrpVictoryConditionTypes.UnitLost && string.IsNullOrEmpty(targetId))
        {
            error = "유닛 상실 조건은 대상을 지휘관이나 선택한 유닛으로 정해야 합니다(모든 유닛은 전멸과 같습니다).";
            return null;
        }

        var data = new SrpVictoryConditionData { type = type, owner = owner };
        if (UsesValue(type))
            data.value = Math.Max(1, value);
        if (UsesTile(type))
        {
            data.x = x;
            data.y = y;
        }
        if (UsesTarget(type))
            data.targetId = targetId;
        if (type == SrpVictoryConditionTypes.HoldPoint)
            data.radius = DefaultHoldRadius;
        return data;
    }

    /// <summary>조건을 승리 조건 목록 또는 패배 조건 목록의 끝에 넣는다.</summary>
    public static void Add(SrpMapCarryOver carry, SrpVictoryConditionData condition, bool asDefeat)
    {
        if (carry == null || condition == null)
            return;
        if (carry.victory == null)
            carry.victory = new SrpVictoryRuleData();
        var list = new System.Collections.Generic.List<SrpVictoryConditionData>(
            (asDefeat ? carry.victory.defeatConditions : carry.victory.conditions) ?? Array.Empty<SrpVictoryConditionData>());
        list.Add(condition);
        if (asDefeat)
            carry.victory.defeatConditions = list.ToArray();
        else
            carry.victory.conditions = list.ToArray();
    }

    /// <summary>목록의 마지막 조건을 뺀다. 뺄 것이 없으면 false.</summary>
    public static bool RemoveLast(SrpMapCarryOver carry, bool asDefeat)
    {
        if (carry == null || carry.victory == null)
            return false;
        var source = asDefeat ? carry.victory.defeatConditions : carry.victory.conditions;
        if (source == null || source.Length == 0)
            return false;
        var list = new System.Collections.Generic.List<SrpVictoryConditionData>(source);
        list.RemoveAt(list.Count - 1);
        if (asDefeat)
            carry.victory.defeatConditions = list.ToArray();
        else
            carry.victory.conditions = list.ToArray();
        return true;
    }

    /// <summary>두 목록을 여러 줄로 설명한다(메이커 화면용). 조건이 없으면 "(추가 조건 없음)".</summary>
    public static string Describe(SrpMapCarryOver carry)
    {
        var lines = new System.Collections.Generic.List<string>();
        if (carry != null && carry.victory != null)
        {
            AppendLines(lines, "승리", carry.victory.conditions, false);
            AppendLines(lines, "패배", carry.victory.defeatConditions, true);
        }
        return lines.Count > 0 ? string.Join("\n", lines) : "(추가 조건 없음)";
    }

    static void AppendLines(
        System.Collections.Generic.List<string> lines, string label, SrpVictoryConditionData[] list, bool asDefeat)
    {
        if (list == null)
            return;
        for (int i = 0; i < list.Length; i++)
            lines.Add($"{label} {i + 1}. {SrpVictory.DescribeCondition(list[i], asDefeat)}");
    }
}

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

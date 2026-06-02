using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 직사각형 그리드 + 유닛 점유. ZOC·이동·공격 판정에 사용.
/// </summary>
public class SrpBattleState
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public bool[] Walkable { get; private set; }
    public List<SrpUnitRuntime> Units { get; private set; } = new List<SrpUnitRuntime>();
    public int[] PlayerOrder { get; private set; }
    public int CurrentPlayerIndex { get; set; }
    public int RoundNumber { get; set; }
    public int CurrentUnitId { get; set; }
    public List<int> RoundQueue { get; private set; } = new List<int>();
    public Dictionary<int, List<int>> Engagements { get; private set; } = new Dictionary<int, List<int>>();
    public Dictionary<string, SrpUnitTemplateData> TemplateLookup { get; private set; } = new Dictionary<string, SrpUnitTemplateData>();
    public Dictionary<string, SrpSkillData> SkillLookup { get; private set; } = new Dictionary<string, SrpSkillData>();
    public List<SrpInteractionPointData> InteractionPoints { get; private set; } = new List<SrpInteractionPointData>();
    public List<SrpCoverSegmentData> CoverSegments { get; private set; } = new List<SrpCoverSegmentData>();

    int _nextUnitId = 1;

    public SrpBattleState Clone()
    {
        var s = new SrpBattleState
        {
            Width = Width,
            Height = Height,
            Walkable = (bool[])Walkable.Clone(),
            PlayerOrder = (int[])PlayerOrder.Clone(),
            CurrentPlayerIndex = CurrentPlayerIndex,
            RoundNumber = RoundNumber,
            CurrentUnitId = CurrentUnitId,
            RoundQueue = new List<int>(RoundQueue),
            Engagements = CloneEngagements(Engagements),
            _nextUnitId = _nextUnitId,
            TemplateLookup = new Dictionary<string, SrpUnitTemplateData>(TemplateLookup),
            SkillLookup = new Dictionary<string, SrpSkillData>(SkillLookup),
            InteractionPoints = CloneInteractionPoints(InteractionPoints),
            CoverSegments = CloneCoverSegments(CoverSegments),
        };
        foreach (var u in Units)
            s.Units.Add(u.Clone());
        return s;
    }

    public static SrpBattleState FromMap(SrpMapFileV1 map)
    {
        var st = new SrpBattleState
        {
            Width = map.width,
            Height = map.height,
            Walkable = BuildWalkable(map),
            PlayerOrder = map.playerOrder != null && map.playerOrder.Length > 0
                ? (int[])map.playerOrder.Clone()
                : new[] { 0, 1 },
            CurrentPlayerIndex = 0,
            RoundNumber = 1,
            CurrentUnitId = -1,
        };
        if (map.templates != null)
        {
            foreach (var t in map.templates)
            {
                if (!string.IsNullOrEmpty(t.id))
                    st.TemplateLookup[t.id] = t;
            }
        }
        var skills = SrpDataIO.LoadSkillsOrDefault();
        if (skills != null)
            foreach (var sk in skills)
                if (!string.IsNullOrEmpty(sk.id))
                    st.SkillLookup[sk.id] = sk;
        foreach (var sk in SrpDefaultSkills.Create())
        {
            if (string.IsNullOrEmpty(sk.id))
                continue;
            if (!st.SkillLookup.TryGetValue(sk.id, out var current) || ShouldUseDefaultSkillMetadata(current, sk))
                st.SkillLookup[sk.id] = sk;
        }

        st.SpawnFromPlacements(map);
        st.LoadInteractionPoints(map);
        st.LoadCoverSegments(map);
        st.RebuildEngagements();
        return st;
    }

    void LoadInteractionPoints(SrpMapFileV1 map)
    {
        InteractionPoints.Clear();
        if (map.interactionPoints == null)
            return;

        foreach (var point in map.interactionPoints)
        {
            if (point == null || !InBounds(point.x, point.y))
                continue;
            InteractionPoints.Add(point.Clone());
        }
    }

    void LoadCoverSegments(SrpMapFileV1 map)
    {
        CoverSegments.Clear();
        if (map.coverSegments == null)
            return;

        foreach (var segment in map.coverSegments)
        {
            if (segment == null || !InBounds(segment.x, segment.y))
                continue;
            CoverSegments.Add(segment.Clone());
        }
    }

    static Dictionary<int, List<int>> CloneEngagements(Dictionary<int, List<int>> source)
    {
        var copy = new Dictionary<int, List<int>>();
        if (source == null)
            return copy;

        foreach (var kv in source)
            copy[kv.Key] = new List<int>(kv.Value);
        return copy;
    }

    static List<SrpCoverSegmentData> CloneCoverSegments(List<SrpCoverSegmentData> source)
    {
        var copy = new List<SrpCoverSegmentData>();
        if (source == null)
            return copy;

        foreach (var segment in source)
        {
            if (segment != null)
                copy.Add(segment.Clone());
        }
        return copy;
    }

    static List<SrpInteractionPointData> CloneInteractionPoints(List<SrpInteractionPointData> source)
    {
        var copy = new List<SrpInteractionPointData>();
        if (source == null)
            return copy;

        foreach (var point in source)
        {
            if (point != null)
                copy.Add(point.Clone());
        }
        return copy;
    }

    static bool ShouldUseDefaultSkillMetadata(SrpSkillData current, SrpSkillData defaultSkill)
    {
        if (current == null || defaultSkill == null)
            return true;
        if (defaultSkill.maxCharges > 0 && current.maxCharges <= 0)
            return true;
        if (defaultSkill.cooldown > 0 && current.cooldown <= 0)
            return true;
        if (defaultSkill.overclockFrozenHeartCost > 0 && current.overclockFrozenHeartCost <= 0)
            return true;
        if (defaultSkill.overclockPowerBonus > 0 && current.overclockPowerBonus <= 0)
            return true;
        if ((defaultSkill.isParryable || defaultSkill.requiresParryTelegraph)
            && !(current.isParryable || current.requiresParryTelegraph))
            return true;
        return false;
    }

    static bool[] BuildWalkable(SrpMapFileV1 map)
    {
        int n = map.width * map.height;
        var w = new bool[n];
        if (map.walkable == null || map.walkable.Length != n)
        {
            for (int i = 0; i < n; i++)
                w[i] = true;
        }
        else
        {
            Array.Copy(map.walkable, w, n);
        }
        return w;
    }

    void SpawnFromPlacements(SrpMapFileV1 map)
    {
        var placements = map?.placements;
        if (placements == null)
            return;
        var allowedSkillIds = BuildSkillFilter(map.allowedSkillIds);
        foreach (var p in placements)
        {
            if (p == null || string.IsNullOrEmpty(p.templateId))
                continue;
            if (!TemplateLookup.TryGetValue(p.templateId, out var tmpl))
                continue;
            var u = CreateUnitFromTemplate(
                tmpl,
                p.owner,
                p.x,
                p.y,
                p.footprint ?? System.Array.Empty<SrpOffset>(),
                allowedSkillIds,
                BuildSkillFilter(p.disabledSkillIds));
            Units.Add(u);
        }
    }

    SrpUnitRuntime CreateUnitFromTemplate(
        SrpUnitTemplateData t,
        int owner,
        int ax,
        int ay,
        SrpOffset[] footprint,
        HashSet<string> allowedSkillIds,
        HashSet<string> disabledSkillIds)
    {
        var weaponClass = ResolveWeaponClass(t);
        int maxAmmo = weaponClass == SrpWeaponClass.Firearm
            ? (t.maxAmmo > 0 ? t.maxAmmo : SrpUnitRuntime.DefaultFirearmMaxAmmo)
            : 0;
        var u = new SrpUnitRuntime
        {
            id = _nextUnitId++,
            templateId = t.id,
            displayName = string.IsNullOrEmpty(t.displayName) ? t.id : t.displayName,
            owner = owner,
            anchorX = ax,
            anchorY = ay,
            maxHp = t.maxHp,
            hp = t.maxHp,
            maxPg = t.maxPg > 0 ? t.maxPg : 18,
            pg = t.maxPg > 0 ? t.maxPg : 18,
            maxActionPoints = t.maxActionPoints > 0 ? t.maxActionPoints : 2,
            actionPoints = t.maxActionPoints > 0 ? t.maxActionPoints : 2,
            maxReactionPoints = t.maxReactionPoints > 0 ? t.maxReactionPoints : 1,
            reactionPoints = t.maxReactionPoints > 0 ? t.maxReactionPoints : 1,
            speed = t.speed > 0 ? t.speed : 10,
            weaponClass = weaponClass,
            stance = t.stance,
            facing = t.facing,
            moveRange = t.moveRange,
            attackRange = t.attackRange,
            attackPower = t.attackPower,
            maxAmmo = maxAmmo,
            ammo = maxAmmo,
            frozenHeart = t.frozenHeart,
            tags = t.tags,
            groggy = false,
            eliminated = false,
        };
        u.footprintOffsets.Clear();
        if (footprint != null && footprint.Length > 0)
        {
            foreach (var o in footprint)
                u.footprintOffsets.Add(new Vector2Int(o.dx, o.dy));
        }
        else if (t.footprintWidth > 1 || t.footprintHeight > 1)
        {
            for (int fy = 0; fy < t.footprintHeight; fy++)
            for (int fx = 0; fx < t.footprintWidth; fx++)
                u.footprintOffsets.Add(new Vector2Int(fx, fy));
        }
        else
        {
            u.footprintOffsets.Add(Vector2Int.zero);
        }
        if (t.skillIds != null)
        {
            int maxSkills = t.maxSkills > 0 ? t.maxSkills : int.MaxValue;
            foreach (var s in t.skillIds)
            {
                if (CanAssignSkill(s, allowedSkillIds, disabledSkillIds))
                {
                    u.skillIds.Add(s);
                    u.skillRuntimes.Add(new SrpSkillRuntime(s));
                    if (u.skillIds.Count >= maxSkills)
                        break;
                }
            }
        }
        return u;
    }

    static HashSet<string> BuildSkillFilter(string[] skillIds)
    {
        if (skillIds == null || skillIds.Length == 0)
            return null;

        var filter = new HashSet<string>();
        foreach (var id in skillIds)
        {
            if (!string.IsNullOrEmpty(id))
                filter.Add(id);
        }
        return filter.Count > 0 ? filter : null;
    }

    static bool CanAssignSkill(string skillId, HashSet<string> allowedSkillIds, HashSet<string> disabledSkillIds)
    {
        if (string.IsNullOrEmpty(skillId))
            return false;
        if (allowedSkillIds != null && !allowedSkillIds.Contains(skillId))
            return false;
        return disabledSkillIds == null || !disabledSkillIds.Contains(skillId);
    }

    static SrpWeaponClass ResolveWeaponClass(SrpUnitTemplateData t)
    {
        if (Enum.IsDefined(typeof(SrpWeaponClass), t.weaponClass))
            return t.weaponClass;

        // legacy 템플릿은 사거리 기반으로 1차 분류한다.
        return t.attackRange > 1 ? SrpWeaponClass.Firearm : SrpWeaponClass.Melee;
    }

    public bool InBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    public int Index(int x, int y)
    {
        return y * Width + x;
    }

    public bool IsWalkableTile(int x, int y)
    {
        if (!InBounds(x, y))
            return false;
        return Walkable[Index(x, y)];
    }

    public bool IsCoverTile(int x, int y)
    {
        return InBounds(x, y) && !IsWalkableTile(x, y);
    }

    public bool HasAdjacentCover(SrpUnitRuntime unit)
    {
        return TryGetAdjacentCover(unit, out _, out _);
    }

    public bool HasAdjacentCoverSource(SrpUnitRuntime unit, int sourceX, int sourceY)
    {
        if (unit == null || unit.eliminated)
            return false;
        if (!IsCoverTile(sourceX, sourceY) && !HasCoverSegmentAt(sourceX, sourceY))
            return false;

        foreach (var off in unit.footprintOffsets)
        {
            int x = unit.anchorX + off.x;
            int y = unit.anchorY + off.y;
            if ((sourceX == x && sourceY == y) || Mathf.Abs(sourceX - x) + Mathf.Abs(sourceY - y) == 1)
                return true;
        }
        return false;
    }

    public bool TryGetAdjacentCover(SrpUnitRuntime unit, out int coverX, out int coverY)
    {
        coverX = 0;
        coverY = 0;
        if (unit == null || unit.eliminated)
            return false;

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };
        foreach (var off in unit.footprintOffsets)
        {
            int x = unit.anchorX + off.x;
            int y = unit.anchorY + off.y;
            if (TryGetCoverSegmentAt(x, y, out _))
            {
                coverX = x;
                coverY = y;
                return true;
            }

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];
                if (!IsCoverTile(nx, ny))
                    continue;
                coverX = nx;
                coverY = ny;
                return true;
            }
        }
        return false;
    }

    public bool TryGetAdjacentCoverSegment(SrpUnitRuntime unit, out SrpCoverSegmentData segment)
    {
        segment = null;
        if (unit == null || unit.eliminated || CoverSegments == null)
            return false;

        foreach (var off in unit.footprintOffsets)
        {
            int x = unit.anchorX + off.x;
            int y = unit.anchorY + off.y;
            if (TryGetCoverSegmentAt(x, y, out segment))
                return true;
        }

        return false;
    }

    public bool HasCoverBetween(SrpUnitRuntime attacker, SrpUnitRuntime defender, out SrpCoverSegmentData segment)
    {
        segment = null;
        if (attacker == null || defender == null || CoverSegments == null)
            return false;
        if (!TryGetIncomingCoverEdge(attacker, defender, out var edge))
            return false;

        foreach (var off in defender.footprintOffsets)
        {
            int x = defender.anchorX + off.x;
            int y = defender.anchorY + off.y;
            for (int i = 0; i < CoverSegments.Count; i++)
            {
                var candidate = CoverSegments[i];
                if (candidate == null || candidate.x != x || candidate.y != y || candidate.edge != edge)
                    continue;
                segment = candidate;
                return true;
            }
        }
        return false;
    }

    public bool HasLineBlockingCoverSegmentBetween(int fromX, int fromY, int toX, int toY)
    {
        int dx = toX - fromX;
        int dy = toY - fromY;
        if (dx == 0 && dy == 0)
            return false;
        if (Mathf.Abs(dx) > 1 || Mathf.Abs(dy) > 1)
            return false;

        if (dx > 0 && (HasBlockingCoverSegmentAt(fromX, fromY, SrpCoverEdge.East)
            || HasBlockingCoverSegmentAt(toX, toY, SrpCoverEdge.West)))
            return true;
        if (dx < 0 && (HasBlockingCoverSegmentAt(fromX, fromY, SrpCoverEdge.West)
            || HasBlockingCoverSegmentAt(toX, toY, SrpCoverEdge.East)))
            return true;
        if (dy > 0 && (HasBlockingCoverSegmentAt(fromX, fromY, SrpCoverEdge.North)
            || HasBlockingCoverSegmentAt(toX, toY, SrpCoverEdge.South)))
            return true;
        if (dy < 0 && (HasBlockingCoverSegmentAt(fromX, fromY, SrpCoverEdge.South)
            || HasBlockingCoverSegmentAt(toX, toY, SrpCoverEdge.North)))
            return true;

        return false;
    }

    bool HasCoverSegmentAt(int x, int y)
    {
        return TryGetCoverSegmentAt(x, y, out _);
    }

    bool HasBlockingCoverSegmentAt(int x, int y, SrpCoverEdge edge)
    {
        if (CoverSegments == null)
            return false;
        for (int i = 0; i < CoverSegments.Count; i++)
        {
            var candidate = CoverSegments[i];
            if (candidate == null || !candidate.blocksLineOfSight)
                continue;
            if (candidate.x == x && candidate.y == y && candidate.edge == edge)
                return true;
        }
        return false;
    }

    bool TryGetCoverSegmentAt(int x, int y, out SrpCoverSegmentData segment)
    {
        segment = null;
        if (CoverSegments == null)
            return false;
        for (int i = 0; i < CoverSegments.Count; i++)
        {
            var candidate = CoverSegments[i];
            if (candidate == null || candidate.x != x || candidate.y != y)
                continue;
            segment = candidate;
            return true;
        }
        return false;
    }

    static bool TryGetIncomingCoverEdge(SrpUnitRuntime attacker, SrpUnitRuntime defender, out SrpCoverEdge edge)
    {
        edge = SrpCoverEdge.North;
        int dx = attacker.anchorX - defender.anchorX;
        int dy = attacker.anchorY - defender.anchorY;
        int absX = Mathf.Abs(dx);
        int absY = Mathf.Abs(dy);
        if (absX == absY)
            return false;
        if (absX > absY)
        {
            edge = dx > 0 ? SrpCoverEdge.East : SrpCoverEdge.West;
            return true;
        }

        edge = dy > 0 ? SrpCoverEdge.North : SrpCoverEdge.South;
        return true;
    }

    public bool TryGetAdjacentInteraction(SrpUnitRuntime unit, out SrpInteractionPointData point)
    {
        point = null;
        if (unit == null || unit.eliminated || InteractionPoints == null)
            return false;

        foreach (var candidate in InteractionPoints)
        {
            if (!CanUnitInteractWith(unit, candidate))
                continue;
            point = candidate;
            return true;
        }
        return false;
    }

    public bool CanUnitInteractWith(SrpUnitRuntime unit, SrpInteractionPointData point)
    {
        if (unit == null || point == null || unit.eliminated)
            return false;
        if (!InBounds(point.x, point.y))
            return false;
        if (point.singleUse && point.activated)
            return false;
        if (point.requiredOwner >= 0 && point.requiredOwner != unit.owner)
            return false;

        foreach (var off in unit.footprintOffsets)
        {
            int x = unit.anchorX + off.x;
            int y = unit.anchorY + off.y;
            if (Mathf.Abs(point.x - x) + Mathf.Abs(point.y - y) == 1)
                return true;
        }
        return false;
    }

    public bool TryActivateInteraction(SrpUnitRuntime unit, out SrpInteractionPointData point)
    {
        if (!TryGetAdjacentInteraction(unit, out point))
            return false;

        point.activated = true;
        point.owner = unit.owner;
        return true;
    }

    public bool TryResolveInteractionAction(SrpUnitRuntime unit, out SrpInteractionPointData point)
    {
        point = null;
        if (unit == null || unit.actionPoints <= 0)
            return false;
        if (!TryActivateInteraction(unit, out point))
            return false;

        unit.actionPoints = Mathf.Max(0, unit.actionPoints - 1);
        return true;
    }

    /// <summary>해당 칸을 점유한 유닛(앵커 또는 풋프린트). 없으면 null.</summary>
    public SrpUnitRuntime GetOccupant(int x, int y)
    {
        foreach (var u in Units)
        {
            if (u.eliminated)
                continue;
            foreach (var off in u.footprintOffsets)
            {
                int ox = u.anchorX + off.x;
                int oy = u.anchorY + off.y;
                if (ox == x && oy == y)
                    return u;
            }
        }
        return null;
    }

    public bool CanStandAt(SrpUnitRuntime mover, int anchorX, int anchorY, int exceptUnitId)
    {
        foreach (var off in mover.footprintOffsets)
        {
            int tx = anchorX + off.x;
            int ty = anchorY + off.y;
            if (!IsWalkableTile(tx, ty))
                return false;
            var occ = GetOccupant(tx, ty);
            if (occ != null && occ.id != exceptUnitId)
                return false;
        }
        return true;
    }

    public bool IsEnemyAdjacentToTile(int tileX, int tileY, int moverOwner)
    {
        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };
        for (int i = 0; i < 4; i++)
        {
            int nx = tileX + dx[i];
            int ny = tileY + dy[i];
            var u = GetOccupant(nx, ny);
            if (u != null && !u.eliminated && u.owner != moverOwner)
                return true;
        }
        return false;
    }

    public void RebuildEngagements()
    {
        Engagements.Clear();
        for (int i = 0; i < Units.Count; i++)
        {
            var a = Units[i];
            if (a == null || a.eliminated)
                continue;

            for (int j = i + 1; j < Units.Count; j++)
            {
                var b = Units[j];
                if (b == null || b.eliminated || a.owner == b.owner)
                    continue;

                if (!AreOrthogonallyAdjacent(a, b))
                    continue;

                AddEngagement(a.id, b.id);
                AddEngagement(b.id, a.id);
            }
        }
    }

    public bool IsUnitEngaged(int unitId)
    {
        return Engagements.TryGetValue(unitId, out var enemies) && enemies.Count > 0;
    }

    public List<int> GetEngagedEnemyIds(int unitId)
    {
        if (!Engagements.TryGetValue(unitId, out var enemies))
            return new List<int>();
        return new List<int>(enemies);
    }

    public int CountEngagingEnemies(SrpUnitRuntime unit)
    {
        if (unit == null || !Engagements.TryGetValue(unit.id, out var enemies))
            return 0;
        return enemies.Count;
    }

    void AddEngagement(int unitId, int enemyId)
    {
        if (!Engagements.TryGetValue(unitId, out var list))
        {
            list = new List<int>();
            Engagements[unitId] = list;
        }
        if (!list.Contains(enemyId))
            list.Add(enemyId);
    }

    static bool AreOrthogonallyAdjacent(SrpUnitRuntime a, SrpUnitRuntime b)
    {
        foreach (var ao in a.footprintOffsets)
        {
            int ax = a.anchorX + ao.x;
            int ay = a.anchorY + ao.y;
            foreach (var bo in b.footprintOffsets)
            {
                int bx = b.anchorX + bo.x;
                int by = b.anchorY + bo.y;
                if (Mathf.Abs(ax - bx) + Mathf.Abs(ay - by) == 1)
                    return true;
            }
        }
        return false;
    }

    public List<SrpUnitRuntime> GetAliveUnitsForOwner(int owner)
    {
        var list = new List<SrpUnitRuntime>();
        foreach (var u in Units)
        {
            if (!u.eliminated && u.owner == owner)
                list.Add(u);
        }
        return list;
    }

    public bool OwnerHasAliveUnits(int owner)
    {
        foreach (var u in Units)
        {
            if (!u.eliminated && u.owner == owner)
                return true;
        }
        return false;
    }

    public int ManhattanAnchor(SrpUnitRuntime a, SrpUnitRuntime b)
    {
        return Mathf.Abs(a.anchorX - b.anchorX) + Mathf.Abs(a.anchorY - b.anchorY);
    }

    public int ChebyshevAnchor(SrpUnitRuntime a, SrpUnitRuntime b)
    {
        return Mathf.Max(
            Mathf.Abs(a.anchorX - b.anchorX),
            Mathf.Abs(a.anchorY - b.anchorY));
    }

    public void RemoveUnit(SrpUnitRuntime u)
    {
        u.eliminated = true;
        u.hp = 0;
    }

    public SrpUnitRuntime FindUnitById(int id)
    {
        foreach (var u in Units)
        {
            if (u.id == id)
                return u;
        }
        return null;
    }

}

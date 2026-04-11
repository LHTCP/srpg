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
    public Dictionary<string, SrpUnitTemplateData> TemplateLookup { get; private set; } = new Dictionary<string, SrpUnitTemplateData>();
    public Dictionary<string, SrpSkillData> SkillLookup { get; private set; } = new Dictionary<string, SrpSkillData>();

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
            _nextUnitId = _nextUnitId,
            TemplateLookup = new Dictionary<string, SrpUnitTemplateData>(TemplateLookup),
            SkillLookup = new Dictionary<string, SrpSkillData>(SkillLookup),
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

        st.SpawnFromPlacements(map.placements);
        return st;
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

    void SpawnFromPlacements(SrpPlacementData[] placements)
    {
        if (placements == null)
            return;
        foreach (var p in placements)
        {
            if (p == null || string.IsNullOrEmpty(p.templateId))
                continue;
            if (!TemplateLookup.TryGetValue(p.templateId, out var tmpl))
                continue;
            var u = CreateUnitFromTemplate(tmpl, p.owner, p.x, p.y, p.footprint ?? System.Array.Empty<SrpOffset>());
            Units.Add(u);
        }
    }

    SrpUnitRuntime CreateUnitFromTemplate(SrpUnitTemplateData t, int owner, int ax, int ay, SrpOffset[] footprint)
    {
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
            maxAp = t.maxAp,
            ap = t.maxAp,
            maxPosture = t.maxPosture,
            posture = 0,
            moveRange = t.moveRange,
            attackRange = t.attackRange,
            attackPower = t.attackPower,
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
            foreach (var s in t.skillIds)
            {
                if (!string.IsNullOrEmpty(s))
                {
                    u.skillIds.Add(s);
                    u.skillRuntimes.Add(new SrpSkillRuntime(s));
                }
            }
        }
        return u;
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

    public int GetCurrentPlayerId()
    {
        if (PlayerOrder == null || PlayerOrder.Length == 0)
            return 0;
        int idx = Mathf.Clamp(CurrentPlayerIndex, 0, PlayerOrder.Length - 1);
        return PlayerOrder[idx];
    }

    public void AdvanceToNextLivingPlayer()
    {
        if (PlayerOrder == null || PlayerOrder.Length == 0)
            return;
        for (int i = 0; i < PlayerOrder.Length; i++)
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % PlayerOrder.Length;
            int pid = GetCurrentPlayerId();
            if (OwnerHasAliveUnits(pid))
                return;
        }
    }

    public void ResetActivationFlagsForCurrentPlayer()
    {
        int pid = GetCurrentPlayerId();
        foreach (var u in Units)
        {
            if (!u.eliminated && u.owner == pid)
            {
                u.hasMovedThisActivation = false;
                u.hasAttackedThisActivation = false;
            }
        }
    }
}

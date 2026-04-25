using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SrpGameController — 그리드 생성, 타일 색상, 유닛 뷰 갱신.
/// </summary>
public partial class SrpGameController
{
    // ── 렌더링 전용 필드 ─────────────────────────────────────────────────────

    const int OverlayMove = 10;
    const int OverlayAttack = 20;
    const int OverlayOverwatch = 25;
    const int OverlaySkill = 30;
    const int OverlayParryTelegraph = 35;
    const int OverlayDangerAttack = 40;
    const int OverlayDangerZoc = 50;
    const int OverlayDangerBlocked = 60;
    const int OverlayUnitHoverRange = 70;
    const int OverlayUnitHoverZoc = 80;
    const int OverlayIntentPath = 90;
    const int OverlayIntentTarget = 100;
    const int OverlayHover = 110;
    static readonly int[] OverlayComposeOrder =
    {
        OverlayMove,
        OverlayAttack,
        OverlayOverwatch,
        OverlaySkill,
        OverlayParryTelegraph,
        OverlayDangerAttack,
        OverlayDangerZoc,
        OverlayDangerBlocked,
        OverlayUnitHoverRange,
        OverlayUnitHoverZoc,
        OverlayIntentPath,
        OverlayIntentTarget,
        OverlayHover,
    };

    GameObject[,] _tiles;
    readonly Dictionary<int, GameObject> _unitObjs = new Dictionary<int, GameObject>();
    Renderer[,] _tileRenderers;
    Color[,] _baseTileColors;
    readonly Dictionary<int, Dictionary<int, Color>> _tileOverlayLayers = new Dictionary<int, Dictionary<int, Color>>();
    static Mesh s_unitFacingWedgeMesh;

    // ── 그리드 ───────────────────────────────────────────────────────────────

    void BuildGrid()
    {
        var parent = new GameObject("SrpGrid").transform;
        parent.SetParent(transform, false);
        int w = _state.Width, h = _state.Height;
        _tiles = new GameObject[w, h];
        _tileRenderers = new Renderer[w, h];
        _baseTileColors = new Color[w, h];

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"t_{x}_{y}";
            cube.transform.SetParent(parent, false);
            cube.transform.localScale = new Vector3(cellSize * 0.98f, 0.15f, cellSize * 0.98f);
            cube.transform.position = new Vector3(x * cellSize, 0f, y * cellSize);

            bool walk = _state.IsWalkableTile(x, y);
            Color c = walk ? new Color(0.55f, 0.65f, 0.45f) : new Color(0.35f, 0.3f, 0.28f);
            var r = cube.GetComponent<Renderer>();
            ApplyColor(r, c);
            _baseTileColors[x, y] = c;
            _tileRenderers[x, y] = r;
            _tiles[x, y] = cube;

            var tv = cube.AddComponent<SrpTileView>();
            tv.x = x; tv.y = y; tv.game = this;
        }
        ClearAllOverlayLayers();
        RebuildAllTileColors();
    }

    // ── 타일 색상 ────────────────────────────────────────────────────────────

    void TintTile(int x, int y, Color tint)
    {
        SetOverlayTile(OverlayMove, x, y, tint);
    }

    void ResetTileColors()
    {
        ClearOverlayLayer(OverlayMove);
        ClearOverlayLayer(OverlayAttack);
        ClearOverlayLayer(OverlayOverwatch);
        ClearOverlayLayer(OverlaySkill);
        ClearOverlayLayer(OverlayParryTelegraph);
        ClearOverlayLayer(OverlayHover);
        ClearOverlayLayer(OverlayUnitHoverRange);
        ClearOverlayLayer(OverlayUnitHoverZoc);
        ClearOverlayLayer(OverlayDangerBlocked);
        ClearOverlayLayer(OverlayIntentPath);
        ClearOverlayLayer(OverlayIntentTarget);
        RebuildDangerAndIntentOverlays();
    }

    void RebuildAllTileColors()
    {
        for (int y = 0; y < _state.Height; y++)
        for (int x = 0; x < _state.Width; x++)
            RebuildTileColor(x, y);
    }

    void HighlightAttackTiles()
    {
        ClearOverlayLayer(OverlayAttack);
        foreach (var id in _attackIds)
        {
            var d = GetUnit(id);
            if (d == null) continue;
            foreach (var off in d.footprintOffsets)
                SetOverlayTile(OverlayAttack, d.anchorX + off.x, d.anchorY + off.y, new Color(0.95f, 0.35f, 0.25f));
        }
    }

    void HighlightOverwatchTiles(SrpUnitRuntime unit)
    {
        ClearOverlayLayer(OverlayOverwatch);
        if (!SrpOverwatch.CanArm(unit))
            return;

        int range = Mathf.Max(1, unit.attackRange);
        for (int y = 0; y < _state.Height; y++)
        for (int x = 0; x < _state.Width; x++)
        {
            int dist = Mathf.Max(Mathf.Abs(x - unit.anchorX), Mathf.Abs(y - unit.anchorY));
            if (dist == 0 || dist > range)
                continue;
            if (!SrpOverwatch.IsTileInLineOfSight(_state, unit, x, y, range))
                continue;
            SetOverlayTile(OverlayOverwatch, x, y, new Color(0.2f, 0.35f, 1f));
        }
    }

    void HighlightParryTelegraphForAttackTargets(SrpUnitRuntime attacker)
    {
        ClearOverlayLayer(OverlayParryTelegraph);
        foreach (var id in _attackIds)
        {
            var defender = GetUnit(id);
            if (!SrpCombatResolver.CanDefenderParry(_state, attacker, defender))
                continue;
            SetParryTelegraphForUnit(defender);
        }
    }

    void HighlightParryTelegraphForSkillTargets(SrpUnitRuntime caster, SrpSkillData skill)
    {
        ClearOverlayLayer(OverlayParryTelegraph);
        if (skill == null || !skill.requiresParryTelegraph)
            return;

        foreach (var tile in _skillTargetTiles)
        {
            var defender = _state.GetOccupant(tile.x, tile.y);
            if (!SrpCombatResolver.CanDefenderParry(_state, caster, defender, skill))
                continue;
            SetParryTelegraphForUnit(defender);
        }
    }

    void SetParryTelegraphForUnit(SrpUnitRuntime unit)
    {
        if (unit == null)
            return;
        foreach (var off in unit.footprintOffsets)
            SetOverlayTile(OverlayParryTelegraph, unit.anchorX + off.x, unit.anchorY + off.y, new Color(0.15f, 0.95f, 1f));
    }

    void ClearAllOverlayLayers()
    {
        _tileOverlayLayers.Clear();
    }

    void ClearOverlayLayer(int layer)
    {
        if (_tileOverlayLayers.TryGetValue(layer, out var map) && map.Count > 0)
        {
            map.Clear();
            RebuildAllTileColors();
        }
    }

    void SetOverlayTile(int layer, int x, int y, Color tint)
    {
        if (x < 0 || y < 0 || x >= _state.Width || y >= _state.Height)
            return;
        if (!_tileOverlayLayers.TryGetValue(layer, out var map))
        {
            map = new Dictionary<int, Color>();
            _tileOverlayLayers[layer] = map;
        }
        int idx = _state.Index(x, y);
        map[idx] = tint;
        RebuildTileColor(x, y);
    }

    void RebuildTileColor(int x, int y)
    {
        if (x < 0 || y < 0 || x >= _state.Width || y >= _state.Height)
            return;

        var final = _baseTileColors[x, y];
        int idx = _state.Index(x, y);
        for (int i = 0; i < OverlayComposeOrder.Length; i++)
        {
            int layer = OverlayComposeOrder[i];
            if (_tileOverlayLayers.TryGetValue(layer, out var map) && map.TryGetValue(idx, out var tint))
                final = Color.Lerp(final, tint, 0.62f);
        }
        ApplyColor(_tileRenderers[x, y], final);
    }

    static void ApplyColor(Renderer r, Color c)
    {
        if (r == null) return;
        if (r.material.HasProperty("_BaseColor"))
            r.material.SetColor("_BaseColor", c);
        else
            r.material.color = c;
    }

    // ── 유닛 뷰 ─────────────────────────────────────────────────────────────

    void RefreshUnitViews()
    {
        var keep = new HashSet<int>();
        foreach (var u in _state.Units)
        {
            if (u.eliminated) continue;
            keep.Add(u.id);

            if (!_unitObjs.TryGetValue(u.id, out var go))
            {
                go = CreateUnitViewObject();
                go.name = $"unit_{u.id}";
                _unitObjs[u.id] = go;
            }

            go.transform.position = GetUnitWorldCenter(u) + Vector3.up * 0.08f;
            go.transform.rotation = GetFacingRotation(u.facing);
            float sc = u.HasTag(SrpUnitTags.Large) ? 0.88f : 0.72f;
            go.transform.localScale = new Vector3(sc, 0.28f, sc);
            var collider = go.GetComponent<Collider>();
            if (collider != null)
                collider.isTrigger = true;
            var unitView = go.GetComponent<SrpUnitView>();
            if (unitView == null)
                unitView = go.AddComponent<SrpUnitView>();
            unitView.game = this;
            unitView.unitId = u.id;

            Color unitColor = u.owner == 0
                ? new Color(0.25f, 0.6f, 1f)
                : new Color(1f, 0.3f, 0.25f);
            if (u.HasTag(SrpUnitTags.Boss))
                unitColor = Color.Lerp(unitColor, Color.yellow, 0.45f);
            ApplyColor(go.GetComponent<Renderer>(), unitColor);
        }

        var remove = new List<int>();
        foreach (var kv in _unitObjs)
            if (!keep.Contains(kv.Key)) remove.Add(kv.Key);
        foreach (var id in remove)
        {
            Destroy(_unitObjs[id]);
            _unitObjs.Remove(id);
        }
    }

    Vector3 GetUnitWorldCenter(SrpUnitRuntime u)
    {
        float sx = 0, sy = 0;
        int n = u.footprintOffsets.Count;
        foreach (var off in u.footprintOffsets)
        {
            sx += u.anchorX + off.x;
            sy += u.anchorY + off.y;
        }
        float cx = n > 0 ? sx / n : u.anchorX;
        float cy = n > 0 ? sy / n : u.anchorY;
        return new Vector3(cx * cellSize, 0f, cy * cellSize);
    }

    static GameObject CreateUnitViewObject()
    {
        var go = new GameObject("unit_view", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
        var mesh = GetUnitFacingWedgeMesh();
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var collider = go.GetComponent<MeshCollider>();
        collider.sharedMesh = mesh;
        collider.convex = true;
        collider.isTrigger = true;
        return go;
    }

    static Mesh GetUnitFacingWedgeMesh()
    {
        if (s_unitFacingWedgeMesh != null)
            return s_unitFacingWedgeMesh;

        // Local +Z is the front tip. Rotation maps this tip to SrpFacing.
        var vertices = new[]
        {
            new Vector3(0f, 0f, 0.55f),
            new Vector3(-0.48f, 0f, -0.38f),
            new Vector3(0.48f, 0f, -0.38f),
            new Vector3(0f, 1f, 0.55f),
            new Vector3(-0.48f, 1f, -0.38f),
            new Vector3(0.48f, 1f, -0.38f),
        };
        var triangles = new[]
        {
            0, 1, 2,
            3, 5, 4,
            0, 3, 4, 0, 4, 1,
            1, 4, 5, 1, 5, 2,
            2, 5, 3, 2, 3, 0,
        };

        s_unitFacingWedgeMesh = new Mesh
        {
            name = "SrpUnitFacingWedge",
            vertices = vertices,
            triangles = triangles,
        };
        s_unitFacingWedgeMesh.RecalculateNormals();
        s_unitFacingWedgeMesh.RecalculateBounds();
        return s_unitFacingWedgeMesh;
    }

    public static Quaternion GetFacingRotation(SrpFacing facing)
    {
        switch (facing)
        {
            case SrpFacing.East:
                return Quaternion.Euler(0f, 90f, 0f);
            case SrpFacing.South:
                return Quaternion.Euler(0f, 180f, 0f);
            case SrpFacing.West:
                return Quaternion.Euler(0f, 270f, 0f);
            case SrpFacing.North:
            default:
                return Quaternion.identity;
        }
    }
}

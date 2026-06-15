using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// SrpGameController — 그리드 생성, 타일 색상, 유닛 뷰 갱신.
/// </summary>
public partial class SrpGameController
{
    // ── 렌더링 전용 필드 ─────────────────────────────────────────────────────

    const int OverlayMove = 10;
    const int OverlayAttack = 20;
    const int OverlayOverwatch = 25;
    const int OverlayCover = 27;
    const int OverlayInteraction = 28;
    const int OverlayMovePreviewCover = 29;
    const int OverlaySkill = 30;
    const int OverlayParryTelegraph = 35;
    const int OverlayDangerAttack = 40;
    const int OverlayDangerZoc = 50;
    const int OverlayDangerBlocked = 60;
    const int OverlayUnitHoverRange = 70;
    const int OverlayUnitHoverZoc = 80;
    const int OverlayAimLine = 85;
    const int OverlayThreatLine = 86;
    const int OverlayOverwatchThreatLine = 87;
    const int OverlayOverwatchThreatEndpoint = 88;
    const int OverlayIntentPath = 90;
    const int OverlayIntentTarget = 100;
    const int OverlayHover = 110;
    const float TileSurfaceY = 0.075f;
    const float CurrentRingY = TileSurfaceY + 0.035f;
    const float SelectedRingY = TileSurfaceY + 0.048f;
    const float HoverRingY = TileSurfaceY + 0.061f;
    const float TileOverlayBaseY = TileSurfaceY + 0.008f;
    const float TileOverlayTopY = TileSurfaceY + 0.031f;
    const float WorldFeedbackDuration = 2.15f;
    const float WorldFeedbackHoldDuration = 1.25f;
    const float WorldFeedbackBoardRise = 0.72f;
    const float WorldFeedbackLaneGap = 0.22f;
    static readonly int[] OverlayComposeOrder =
    {
        OverlayMove,
        OverlayAttack,
        OverlayOverwatch,
        OverlayCover,
        OverlayInteraction,
        OverlayMovePreviewCover,
        OverlaySkill,
        OverlayParryTelegraph,
        OverlayDangerAttack,
        OverlayDangerZoc,
        OverlayDangerBlocked,
        OverlayUnitHoverRange,
        OverlayUnitHoverZoc,
        OverlayAimLine,
        OverlayThreatLine,
        OverlayOverwatchThreatLine,
        OverlayOverwatchThreatEndpoint,
        OverlayIntentPath,
        OverlayIntentTarget,
        OverlayHover,
    };

    GameObject[,] _tiles;
    readonly Dictionary<int, GameObject> _unitObjs = new Dictionary<int, GameObject>();
    readonly Dictionary<int, GameObject> _unitStatusBadges = new Dictionary<int, GameObject>();
    readonly Dictionary<int, Coroutine> _unitFlashCoroutines = new Dictionary<int, Coroutine>();
    readonly HashSet<int> _flashingUnitIds = new HashSet<int>();
    readonly List<GameObject> _floatingFeedbackTexts = new List<GameObject>();
    readonly List<string> _feedbackTextHistory = new List<string>();
    Renderer[,] _tileRenderers;
    Color[,] _baseTileColors;
    readonly Dictionary<int, Dictionary<int, Color>> _tileOverlayLayers = new Dictionary<int, Dictionary<int, Color>>();
    readonly Dictionary<int, GameObject> _tileOverlayVisuals = new Dictionary<int, GameObject>();
    static Mesh s_unitFacingWedgeMesh;
    static Mesh s_unitRingMesh;
    static Mesh s_tileCenterDiscMesh;
    static Mesh s_tileRingMesh;
    static Mesh s_tileBorderMesh;
    static Mesh s_tileDiamondMesh;
    static Mesh s_tileCrossMesh;
    static Mesh s_unitFacingArrowMesh;
    GameObject _unitFeedbackRoot;
    GameObject _tileOverlayRoot;
    GameObject _coverObjectRoot;
    GameObject _movePreviewThreatRoot;
    GameObject _currentUnitRing;
    GameObject _selectedUnitRing;
    GameObject _hoverUnitRing;
    GameObject _moveGhostUnit;
    readonly Dictionary<int, GameObject> _unitFacingArrows = new Dictionary<int, GameObject>();
    readonly List<GameObject> _coverObjects = new List<GameObject>();
    readonly List<GameObject> _movePreviewThreatObjects = new List<GameObject>();
    int _feedbackTextSpawnCount;
    string _lastFeedbackText = string.Empty;
    readonly Dictionary<int, int> _activeFeedbackByUnit = new Dictionary<int, int>();
    Vector3 _previousFeedbackStartPosition;
    Vector3 _lastFeedbackStartPosition;
    bool _hasPreviousFeedbackStartPosition;

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
        RenderCoverObjects();
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
        ClearOverlayLayer(OverlayCover);
        ClearOverlayLayer(OverlayInteraction);
        ClearOverlayLayer(OverlayMovePreviewCover);
        ClearOverlayLayer(OverlaySkill);
        ClearOverlayLayer(OverlayParryTelegraph);
        ClearOverlayLayer(OverlayHover);
        ClearOverlayLayer(OverlayUnitHoverRange);
        ClearOverlayLayer(OverlayUnitHoverZoc);
        ClearOverlayLayer(OverlayAimLine);
        ClearOverlayLayer(OverlayThreatLine);
        ClearOverlayLayer(OverlayOverwatchThreatLine);
        ClearOverlayLayer(OverlayOverwatchThreatEndpoint);
        ClearOverlayLayer(OverlayDangerBlocked);
        ClearOverlayLayer(OverlayIntentPath);
        ClearOverlayLayer(OverlayIntentTarget);
        RebuildDangerAndIntentOverlays();
        ClearMovePreviewVisuals();
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
                SetOverlayTile(OverlayAttack, d.anchorX + off.x, d.anchorY + off.y, new Color(0.70f, 0.24f, 0.20f));
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

    void HighlightCoverTiles(SrpUnitRuntime unit)
    {
        ClearOverlayLayer(OverlayCover);
        if (_state == null || unit == null || unit.eliminated)
            return;

        if (unit.coverActive && _state.HasAdjacentCoverSource(unit, unit.coverSourceX, unit.coverSourceY))
        {
            SetOverlayTile(OverlayCover, unit.coverSourceX, unit.coverSourceY, new Color(0.45f, 0.95f, 0.25f));
            foreach (var off in unit.footprintOffsets)
                SetOverlayTile(OverlayCover, unit.anchorX + off.x, unit.anchorY + off.y, new Color(0.38f, 0.75f, 0.24f));
            return;
        }

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };
        foreach (var off in unit.footprintOffsets)
        {
            int x = unit.anchorX + off.x;
            int y = unit.anchorY + off.y;
            HighlightCoverSegmentsAt(x, y);
            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];
                if (_state.IsCoverTile(nx, ny))
                    SetOverlayTile(OverlayCover, nx, ny, new Color(0.45f, 0.95f, 0.25f));
            }
        }
    }

    void HighlightCoverSegmentsAt(int x, int y)
    {
        if (_state?.CoverSegments == null)
            return;
        foreach (var segment in _state.CoverSegments)
        {
            if (segment == null || segment.x != x || segment.y != y)
                continue;
            SetOverlayTile(OverlayCover, x, y, GetCoverSegmentTint(segment));
        }
    }

    static Color GetCoverSegmentTint(SrpCoverSegmentData segment)
    {
        switch (segment.edge)
        {
            case SrpCoverEdge.East:
                return new Color(0.55f, 1.0f, 0.25f);
            case SrpCoverEdge.South:
                return new Color(0.35f, 0.9f, 0.22f);
            case SrpCoverEdge.West:
                return new Color(0.65f, 0.9f, 0.25f);
            case SrpCoverEdge.North:
            default:
                return new Color(0.45f, 0.95f, 0.25f);
        }
    }

    void HighlightInteractionTiles(SrpUnitRuntime unit)
    {
        ClearOverlayLayer(OverlayInteraction);
        if (_state == null || _state.InteractionPoints == null)
            return;

        foreach (var point in _state.InteractionPoints)
        {
            if (point == null || !_state.InBounds(point.x, point.y))
                continue;
            bool canInteract = unit != null && _state.CanUnitInteractWith(unit, point);
            Color tint = point.activated
                ? new Color(0.45f, 0.38f, 0.18f)
                : canInteract
                    ? new Color(1.0f, 0.9f, 0.20f)
                    : new Color(0.85f, 0.65f, 0.15f);
            SetOverlayTile(OverlayInteraction, point.x, point.y, tint);
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

    void HighlightFirearmAimLine(SrpUnitRuntime attacker, SrpUnitRuntime target)
    {
        ClearOverlayLayer(OverlayAimLine);
        if (attacker == null || target == null)
            return;
        if (SrpCombatResolver.ResolveBasicAttackKind(_state, attacker, target) != SrpBasicAttackKind.Firearm)
            return;
        if (!SrpFirearmAim.CanBasicAttack(_state, attacker, target, out var line))
            return;
        foreach (var tile in line.tiles)
        {
            var occupant = _state.GetOccupant(tile.x, tile.y);
            if (occupant != null && occupant.id == attacker.id)
                continue;
            SetOverlayTile(OverlayAimLine, tile.x, tile.y, new Color(1f, 0.78f, 0.18f));
        }
    }

    void ClearAllOverlayLayers()
    {
        _tileOverlayLayers.Clear();
        ClearAllTileOverlayVisuals();
    }

    void ClearOverlayLayer(int layer)
    {
        bool hadTintOverlay = false;
        if (_tileOverlayLayers.TryGetValue(layer, out var map) && map.Count > 0)
        {
            map.Clear();
            hadTintOverlay = true;
        }
        ClearTileOverlayVisualsForLayer(layer);
        if (hadTintOverlay)
            RebuildAllTileColors();
    }

    void SetOverlayTile(int layer, int x, int y, Color tint)
    {
        if (x < 0 || y < 0 || x >= _state.Width || y >= _state.Height)
            return;
        if (ShouldTintTileLayer(layer))
        {
            if (!_tileOverlayLayers.TryGetValue(layer, out var map))
            {
                map = new Dictionary<int, Color>();
                _tileOverlayLayers[layer] = map;
            }
            int idx = _state.Index(x, y);
            map[idx] = tint;
        }
        UpdateTileOverlayVisual(layer, x, y, tint);
        if (ShouldTintTileLayer(layer))
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
            if (ShouldTintTileLayer(layer) && _tileOverlayLayers.TryGetValue(layer, out var map) && map.TryGetValue(idx, out var tint))
                final = Color.Lerp(final, tint, 0.45f);
        }
        ApplyColor(_tileRenderers[x, y], final);
    }

    static bool ShouldTintTileLayer(int layer)
    {
        return false;
    }

    static bool ShouldUseTileOverlayVisual(int layer)
    {
        return !ShouldTintTileLayer(layer);
    }

    void UpdateTileOverlayVisual(int layer, int x, int y, Color tint)
    {
        if (_state == null)
            return;
        if (!ShouldUseTileOverlayVisual(layer))
            return;

        var spec = GetTileOverlayVisualSpec(layer);
        int visualKey = GetTileOverlayVisualKey(layer, _state.Index(x, y));
        if (!_tileOverlayVisuals.TryGetValue(visualKey, out var go) || go == null)
        {
            EnsureTileOverlayRoot();
            go = new GameObject($"TileOverlay_{layer}_{x}_{y}", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(_tileOverlayRoot.transform, false);
            _tileOverlayVisuals[visualKey] = go;
        }

        go.GetComponent<MeshFilter>().sharedMesh = GetTileOverlayMesh(spec.style);
        go.transform.position = new Vector3(x * cellSize, spec.worldY, y * cellSize);
        go.transform.rotation = spec.rotation;
        float scale = cellSize * spec.scale;
        go.transform.localScale = new Vector3(scale, 1f, scale);

        var renderer = go.GetComponent<Renderer>();
        if (renderer.sharedMaterial == null)
            renderer.sharedMaterial = CreateFeedbackMaterial(tint);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = spec.sortingOrder;
        ApplyFeedbackColor(renderer, tint);
        go.SetActive(true);
    }

    static TileOverlayVisualSpec GetTileOverlayVisualSpec(int layer)
    {
        if (layer == OverlayMove)
            return new TileOverlayVisualSpec(TileOverlayStyle.CenterDisc, TileOverlayBaseY, 0.28f, Quaternion.identity, 1);
        if (layer == OverlayAttack)
            return new TileOverlayVisualSpec(TileOverlayStyle.CenterDisc, TileOverlayBaseY + 0.006f, 0.34f, Quaternion.identity, 2);
        if (layer == OverlayDangerAttack)
            return new TileOverlayVisualSpec(TileOverlayStyle.CenterDisc, TileOverlayBaseY + 0.006f, 0.30f, Quaternion.identity, 2);
        if (layer == OverlayUnitHoverRange)
            return new TileOverlayVisualSpec(TileOverlayStyle.CenterDisc, TileOverlayBaseY + 0.006f, 0.26f, Quaternion.identity, 2);
        if (layer == OverlayDangerZoc || layer == OverlayUnitHoverZoc)
            return new TileOverlayVisualSpec(TileOverlayStyle.WarningRing, TileOverlayBaseY + 0.010f, 0.68f, Quaternion.identity, 3);
        if (layer == OverlayOverwatch)
            return new TileOverlayVisualSpec(TileOverlayStyle.CenterDisc, TileOverlayBaseY + 0.012f, 0.32f, Quaternion.identity, 4);
        if (layer == OverlayCover)
            return new TileOverlayVisualSpec(TileOverlayStyle.Border, TileOverlayBaseY + 0.014f, 0.56f, Quaternion.identity, 5);
        if (layer == OverlayInteraction)
            return new TileOverlayVisualSpec(TileOverlayStyle.ObjectiveDiamond, TileOverlayTopY, 0.62f, Quaternion.identity, 8);
        if (layer == OverlayMovePreviewCover)
            return new TileOverlayVisualSpec(TileOverlayStyle.Border, TileOverlayBaseY + 0.026f, 0.46f, Quaternion.identity, 9);
        if (layer == OverlaySkill || layer == OverlayIntentTarget)
            return new TileOverlayVisualSpec(TileOverlayStyle.ObjectiveDiamond, TileOverlayBaseY + 0.020f, 0.50f, Quaternion.identity, 7);
        if (layer == OverlayParryTelegraph)
            return new TileOverlayVisualSpec(TileOverlayStyle.WarningRing, TileOverlayBaseY + 0.022f, 0.58f, Quaternion.identity, 7);
        if (layer == OverlayDangerBlocked)
            return new TileOverlayVisualSpec(TileOverlayStyle.BlockedCross, TileOverlayBaseY + 0.018f, 0.62f, Quaternion.identity, 6);
        if (layer == OverlayIntentPath)
            return new TileOverlayVisualSpec(TileOverlayStyle.CenterDisc, TileOverlayBaseY + 0.016f, 0.20f, Quaternion.identity, 6);
        if (layer == OverlayHover)
            return new TileOverlayVisualSpec(TileOverlayStyle.Border, TileOverlayBaseY + 0.024f, 0.78f, Quaternion.identity, 9);
        if (layer == OverlayThreatLine)
            return new TileOverlayVisualSpec(TileOverlayStyle.CenterDisc, TileOverlayTopY + 0.020f, 0.18f, Quaternion.identity, 10);
        if (layer == OverlayOverwatchThreatLine)
            return new TileOverlayVisualSpec(TileOverlayStyle.CenterDisc, TileOverlayTopY + 0.030f, 0.24f, Quaternion.identity, 11);
        if (layer == OverlayOverwatchThreatEndpoint)
            return new TileOverlayVisualSpec(TileOverlayStyle.WarningRing, TileOverlayTopY + 0.040f, 0.76f, Quaternion.identity, 12);
        return new TileOverlayVisualSpec(TileOverlayStyle.CenterDisc, TileOverlayBaseY, 0.24f, Quaternion.identity, 1);
    }

    static Mesh GetTileOverlayMesh(TileOverlayStyle style)
    {
        switch (style)
        {
            case TileOverlayStyle.WarningRing:
                return GetTileRingMesh();
            case TileOverlayStyle.Border:
                return GetTileBorderMesh();
            case TileOverlayStyle.ObjectiveDiamond:
                return GetTileDiamondMesh();
            case TileOverlayStyle.BlockedCross:
                return GetTileCrossMesh();
            case TileOverlayStyle.CenterDisc:
            default:
                return GetTileCenterDiscMesh();
        }
    }

    static Mesh GetTileCenterDiscMesh()
    {
        if (s_tileCenterDiscMesh != null)
            return s_tileCenterDiscMesh;

        const int segments = 32;
        var vertices = new Vector3[segments + 1];
        var triangles = new int[segments * 3];
        vertices[0] = Vector3.zero;
        for (int i = 0; i < segments; i++)
        {
            float angle = (Mathf.PI * 2f * i) / segments;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * 0.5f, 0f, Mathf.Sin(angle) * 0.5f);
            int tri = i * 3;
            triangles[tri] = 0;
            triangles[tri + 1] = (i + 1) % segments + 1;
            triangles[tri + 2] = i + 1;
        }

        s_tileCenterDiscMesh = CreateTileOverlayMesh("SrpTileOverlayCenterDisc", vertices, triangles);
        return s_tileCenterDiscMesh;
    }

    static Mesh GetTileRingMesh()
    {
        if (s_tileRingMesh != null)
            return s_tileRingMesh;

        const int segments = 48;
        const float outerRadius = 0.5f;
        const float innerRadius = 0.42f;
        var vertices = new Vector3[segments * 2];
        var triangles = new int[segments * 6];
        for (int i = 0; i < segments; i++)
        {
            float angle = (Mathf.PI * 2f * i) / segments;
            float sin = Mathf.Sin(angle);
            float cos = Mathf.Cos(angle);
            vertices[i * 2] = new Vector3(cos * outerRadius, 0f, sin * outerRadius);
            vertices[i * 2 + 1] = new Vector3(cos * innerRadius, 0f, sin * innerRadius);

            int next = (i + 1) % segments;
            int tri = i * 6;
            triangles[tri] = i * 2;
            triangles[tri + 1] = i * 2 + 1;
            triangles[tri + 2] = next * 2;
            triangles[tri + 3] = next * 2;
            triangles[tri + 4] = i * 2 + 1;
            triangles[tri + 5] = next * 2 + 1;
        }

        s_tileRingMesh = CreateTileOverlayMesh("SrpTileOverlayWarningRing", vertices, triangles);
        return s_tileRingMesh;
    }

    static Mesh GetTileBorderMesh()
    {
        if (s_tileBorderMesh != null)
            return s_tileBorderMesh;

        const float outer = 0.5f;
        const float inner = 0.42f;
        var vertices = new[]
        {
            new Vector3(-outer, 0f, outer), new Vector3(outer, 0f, outer), new Vector3(outer, 0f, inner), new Vector3(-outer, 0f, inner),
            new Vector3(outer, 0f, outer), new Vector3(outer, 0f, -outer), new Vector3(inner, 0f, -outer), new Vector3(inner, 0f, outer),
            new Vector3(outer, 0f, -outer), new Vector3(-outer, 0f, -outer), new Vector3(-outer, 0f, -inner), new Vector3(outer, 0f, -inner),
            new Vector3(-outer, 0f, -outer), new Vector3(-outer, 0f, outer), new Vector3(-inner, 0f, outer), new Vector3(-inner, 0f, -outer),
        };
        var triangles = QuadStripTriangles(4);
        s_tileBorderMesh = CreateTileOverlayMesh("SrpTileOverlayDangerBorder", vertices, triangles);
        return s_tileBorderMesh;
    }

    static Mesh GetTileDiamondMesh()
    {
        if (s_tileDiamondMesh != null)
            return s_tileDiamondMesh;

        var vertices = new[]
        {
            Vector3.zero,
            new Vector3(0f, 0f, 0.5f),
            new Vector3(0.5f, 0f, 0f),
            new Vector3(0f, 0f, -0.5f),
            new Vector3(-0.5f, 0f, 0f),
        };
        var triangles = new[] { 0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 1 };
        s_tileDiamondMesh = CreateTileOverlayMesh("SrpTileOverlayObjectiveDiamond", vertices, triangles);
        return s_tileDiamondMesh;
    }

    static Mesh GetTileCrossMesh()
    {
        if (s_tileCrossMesh != null)
            return s_tileCrossMesh;

        const float halfLength = 0.5f;
        const float halfWidth = 0.08f;
        var vertices = new[]
        {
            new Vector3(-halfLength, 0f, halfWidth), new Vector3(halfLength, 0f, halfWidth), new Vector3(halfLength, 0f, -halfWidth), new Vector3(-halfLength, 0f, -halfWidth),
            new Vector3(-halfWidth, 0f, halfLength), new Vector3(halfWidth, 0f, halfLength), new Vector3(halfWidth, 0f, -halfLength), new Vector3(-halfWidth, 0f, -halfLength),
        };
        var triangles = QuadStripTriangles(2);
        s_tileCrossMesh = CreateTileOverlayMesh("SrpTileOverlayBlockedCross", vertices, triangles);
        return s_tileCrossMesh;
    }

    static int[] QuadStripTriangles(int quadCount)
    {
        var triangles = new int[quadCount * 6];
        for (int i = 0; i < quadCount; i++)
        {
            int vertex = i * 4;
            int tri = i * 6;
            triangles[tri] = vertex;
            triangles[tri + 1] = vertex + 1;
            triangles[tri + 2] = vertex + 2;
            triangles[tri + 3] = vertex;
            triangles[tri + 4] = vertex + 2;
            triangles[tri + 5] = vertex + 3;
        }
        return triangles;
    }

    static Mesh CreateTileOverlayMesh(string name, Vector3[] vertices, int[] triangles)
    {
        var mesh = new Mesh
        {
            name = name,
            vertices = vertices,
            triangles = triangles,
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static int GetTileOverlayVisualKey(int layer, int tileIndex)
    {
        return unchecked((layer * 73856093) ^ tileIndex);
    }

    void EnsureTileOverlayRoot()
    {
        if (_tileOverlayRoot != null)
            return;
        _tileOverlayRoot = new GameObject("SrpTileOverlayGrammarLayer");
        _tileOverlayRoot.transform.SetParent(transform, false);
    }

    void ClearTileOverlayVisualsForLayer(int layer)
    {
        var remove = new List<int>();
        foreach (var kv in _tileOverlayVisuals)
        {
            if (kv.Value == null)
            {
                remove.Add(kv.Key);
                continue;
            }
            if (kv.Value.name.StartsWith($"TileOverlay_{layer}_"))
            {
                Destroy(kv.Value);
                remove.Add(kv.Key);
            }
        }
        foreach (int key in remove)
            _tileOverlayVisuals.Remove(key);
    }

    void ClearAllTileOverlayVisuals()
    {
        foreach (var go in _tileOverlayVisuals.Values)
            if (go != null)
                Destroy(go);
        _tileOverlayVisuals.Clear();
    }

    enum TileOverlayStyle
    {
        CenterDisc,
        Border,
        WarningRing,
        ObjectiveDiamond,
        BlockedCross,
    }

    readonly struct TileOverlayVisualSpec
    {
        public readonly TileOverlayStyle style;
        public readonly float worldY;
        public readonly float scale;
        public readonly Quaternion rotation;
        public readonly int sortingOrder;

        public TileOverlayVisualSpec(TileOverlayStyle style, float worldY, float scale, Quaternion rotation, int sortingOrder)
        {
            this.style = style;
            this.worldY = worldY;
            this.scale = scale;
            this.rotation = rotation;
            this.sortingOrder = sortingOrder;
        }
    }

    static void ApplyColor(Renderer r, Color c)
    {
        if (r == null) return;
        if (r.material.HasProperty("_BaseColor"))
            r.material.SetColor("_BaseColor", c);
        else
            r.material.color = c;
    }

    static Material CreateFeedbackMaterial(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Standard");
        var material = new Material(shader);
        material.name = "SrpFeedbackUnlit";
        material.renderQueue = 3100;
        SetMaterialColor(material, color);
        return material;
    }

    static void ApplyFeedbackColor(Renderer r, Color color)
    {
        if (r == null)
            return;
        if (r.sharedMaterial == null)
            r.sharedMaterial = CreateFeedbackMaterial(color);
        SetMaterialColor(r.material, color);
    }

    static void SetMaterialColor(Material material, Color color)
    {
        if (material == null)
            return;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
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
            if (!_flashingUnitIds.Contains(u.id))
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

        UpdateUnitFeedbackVisuals();
    }

    Color GetUnitBaseColor(SrpUnitRuntime u)
    {
        if (u == null)
            return Color.white;
        Color unitColor = u.owner == 0
            ? new Color(0.25f, 0.6f, 1f)
            : new Color(1f, 0.3f, 0.25f);
        if (u.HasTag(SrpUnitTags.Boss))
            unitColor = Color.Lerp(unitColor, Color.yellow, 0.45f);
        return unitColor;
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

    void UpdateUnitFeedbackVisuals()
    {
        if (_state == null)
            return;

        EnsureUnitFeedbackRoot();
        UpdatePriorityRing(ref _currentUnitRing, "CurrentActionRing", GetUnit(_state.CurrentUnitId),
            new Color(1f, 0.66f, 0.22f), CurrentRingY, 1.08f);
        UpdatePriorityRing(ref _selectedUnitRing, "SelectedUnitRing", _selectedId.HasValue ? GetUnit(_selectedId.Value) : null,
            new Color(0.22f, 0.82f, 0.84f), SelectedRingY, 0.94f);
        UpdatePriorityRing(ref _hoverUnitRing, "HoverUnitRing", _hoverUnitId > 0 ? GetUnit(_hoverUnitId) : null,
            new Color(0.92f, 0.94f, 0.88f), HoverRingY, 0.78f);
        UpdateUnitFacingArrows();
        UpdateUnitStatusBadges();
    }

    void UpdateUnitFacingArrows()
    {
        var keep = new HashSet<int>();
        foreach (var unit in _state.Units)
        {
            if (unit == null || unit.eliminated)
                continue;
            keep.Add(unit.id);
            if (!_unitFacingArrows.TryGetValue(unit.id, out var arrow) || arrow == null)
            {
                arrow = new GameObject($"UnitFacingArrow_{unit.id}", typeof(MeshFilter), typeof(MeshRenderer));
                arrow.transform.SetParent(_unitFeedbackRoot.transform, false);
                arrow.GetComponent<MeshFilter>().sharedMesh = GetUnitFacingArrowMesh();
                var renderer = arrow.GetComponent<Renderer>();
                renderer.sharedMaterial = CreateFeedbackMaterial(new Color(1f, 1f, 1f, 0.72f));
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sortingOrder = 24;
                _unitFacingArrows[unit.id] = arrow;
            }

            var center = GetUnitWorldCenter(unit);
            arrow.SetActive(true);
            arrow.transform.position = new Vector3(center.x, TileOverlayTopY + 0.15f, center.z);
            arrow.transform.rotation = GetFacingRotation(unit.facing);
            float scale = unit.HasTag(SrpUnitTags.Large) ? 0.86f : 0.68f;
            arrow.transform.localScale = new Vector3(scale, 1f, scale);
            var color = unit.owner == 0
                ? new Color(0.75f, 0.96f, 1f, 0.78f)
                : new Color(1f, 0.72f, 0.60f, 0.78f);
            ApplyFeedbackColor(arrow.GetComponent<Renderer>(), color);
        }

        var remove = new List<int>();
        foreach (var kv in _unitFacingArrows)
            if (!keep.Contains(kv.Key))
                remove.Add(kv.Key);
        foreach (int id in remove)
        {
            if (_unitFacingArrows[id] != null)
                Destroy(_unitFacingArrows[id]);
            _unitFacingArrows.Remove(id);
        }
    }

    void UpdatePriorityRing(ref GameObject ring, string name, SrpUnitRuntime unit, Color color, float worldY, float radiusScale)
    {
        if (ring == null)
            ring = CreateUnitRing(name, color);
        if (unit == null || unit.eliminated)
        {
            ring.SetActive(false);
            return;
        }

        ring.SetActive(true);
        var center = GetUnitWorldCenter(unit);
        ring.transform.position = new Vector3(center.x, worldY, center.z);
        float radius = GetUnitFeedbackRadius(unit) * radiusScale;
        ring.transform.localScale = new Vector3(radius, 1f, radius);
        ApplyFeedbackColor(ring.GetComponent<Renderer>(), color);
    }

    GameObject CreateUnitRing(string name, Color color)
    {
        EnsureUnitFeedbackRoot();
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(_unitFeedbackRoot.transform, false);
        go.GetComponent<MeshFilter>().sharedMesh = GetUnitRingMesh();
        var renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateFeedbackMaterial(color);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = 20;
        ApplyFeedbackColor(renderer, color);
        return go;
    }

    static Mesh GetUnitRingMesh()
    {
        if (s_unitRingMesh != null)
            return s_unitRingMesh;

        const int segments = 64;
        const float outerRadius = 0.5f;
        const float innerRadius = 0.4f;
        var vertices = new Vector3[segments * 2];
        var triangles = new int[segments * 6];
        for (int i = 0; i < segments; i++)
        {
            float angle = (Mathf.PI * 2f * i) / segments;
            float sin = Mathf.Sin(angle);
            float cos = Mathf.Cos(angle);
            vertices[i * 2] = new Vector3(cos * outerRadius, 0f, sin * outerRadius);
            vertices[i * 2 + 1] = new Vector3(cos * innerRadius, 0f, sin * innerRadius);

            int next = (i + 1) % segments;
            int tri = i * 6;
            triangles[tri] = i * 2;
            triangles[tri + 1] = i * 2 + 1;
            triangles[tri + 2] = next * 2;
            triangles[tri + 3] = next * 2;
            triangles[tri + 4] = i * 2 + 1;
            triangles[tri + 5] = next * 2 + 1;
        }

        s_unitRingMesh = new Mesh
        {
            name = "SrpUnitFeedbackRing",
            vertices = vertices,
            triangles = triangles,
        };
        s_unitRingMesh.RecalculateNormals();
        s_unitRingMesh.RecalculateBounds();
        return s_unitRingMesh;
    }

    static Mesh GetUnitFacingArrowMesh()
    {
        if (s_unitFacingArrowMesh != null)
            return s_unitFacingArrowMesh;

        var vertices = new[]
        {
            new Vector3(0f, 0f, 0.52f),
            new Vector3(-0.28f, 0f, 0.12f),
            new Vector3(-0.10f, 0f, 0.12f),
            new Vector3(-0.10f, 0f, -0.46f),
            new Vector3(0.10f, 0f, -0.46f),
            new Vector3(0.10f, 0f, 0.12f),
            new Vector3(0.28f, 0f, 0.12f),
        };
        var triangles = new[]
        {
            0, 1, 2,
            0, 2, 5,
            0, 5, 6,
            2, 3, 4,
            2, 4, 5,
        };
        s_unitFacingArrowMesh = new Mesh
        {
            name = "SrpUnitFacingArrow",
            vertices = vertices,
            triangles = triangles,
        };
        s_unitFacingArrowMesh.RecalculateNormals();
        s_unitFacingArrowMesh.RecalculateBounds();
        return s_unitFacingArrowMesh;
    }

    float GetUnitFeedbackRadius(SrpUnitRuntime unit)
    {
        if (unit == null || unit.footprintOffsets == null || unit.footprintOffsets.Count == 0)
            return cellSize;

        int minX = 0, maxX = 0, minY = 0, maxY = 0;
        foreach (var off in unit.footprintOffsets)
        {
            minX = Mathf.Min(minX, off.x);
            maxX = Mathf.Max(maxX, off.x);
            minY = Mathf.Min(minY, off.y);
            maxY = Mathf.Max(maxY, off.y);
        }
        float span = Mathf.Max(maxX - minX + 1, maxY - minY + 1);
        return Mathf.Max(cellSize * 0.95f, span * cellSize);
    }

    void UpdateUnitStatusBadges()
    {
        var keep = new HashSet<int>();
        foreach (var unit in _state.Units)
        {
            if (unit == null || unit.eliminated)
                continue;
            keep.Add(unit.id);

            if (!_unitStatusBadges.TryGetValue(unit.id, out var badge))
            {
                badge = CreateUnitStatusBadge(unit.id);
                _unitStatusBadges[unit.id] = badge;
            }

            bool engaged = _state.IsUnitEngaged(unit.id);
            bool inZoc = !engaged && _state.IsEnemyAdjacentToTile(unit.anchorX, unit.anchorY, unit.owner);
            badge.SetActive(engaged || inZoc);
            if (!badge.activeSelf)
                continue;

            badge.transform.position = GetUnitWorldCenter(unit) + new Vector3(0f, 0.42f, -0.42f * cellSize);
            badge.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var text = badge.GetComponent<TextMeshPro>();
            text.text = engaged ? "\uAD50\uC804" : "ZOC";
            text.color = engaged ? new Color(1f, 0.25f, 0.65f) : new Color(1f, 0.92f, 0.25f);
        }

        var remove = new List<int>();
        foreach (var kv in _unitStatusBadges)
            if (!keep.Contains(kv.Key)) remove.Add(kv.Key);
        foreach (int id in remove)
        {
            Destroy(_unitStatusBadges[id]);
            _unitStatusBadges.Remove(id);
        }
    }

    GameObject CreateUnitStatusBadge(int unitId)
    {
        EnsureUnitFeedbackRoot();
        var go = new GameObject($"UnitStatusBadge_{unitId}");
        go.transform.SetParent(_unitFeedbackRoot.transform, false);
        var text = go.AddComponent<TextMeshPro>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 2.8f;
        text.fontStyle = FontStyles.Bold;
        text.enableWordWrapping = false;
        text.text = string.Empty;
        go.SetActive(false);
        return go;
    }

    void SpawnWorldFeedback(SrpUnitRuntime unit, string text, Color color)
    {
        if (unit == null || unit.eliminated || string.IsNullOrEmpty(text))
            return;

        EnsureUnitFeedbackRoot();
        var go = new GameObject("FloatingFeedback_" + text);
        go.transform.SetParent(_unitFeedbackRoot.transform, false);
        go.transform.position = GetFeedbackStartPosition(unit);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        var shadowGo = new GameObject("Shadow");
        shadowGo.transform.SetParent(go.transform, false);
        shadowGo.transform.localPosition = new Vector3(0.045f, -0.045f, 0.012f);
        var shadow = shadowGo.AddComponent<TextMeshPro>();
        ConfigureFeedbackText(shadow, text, new Color(0f, 0f, 0f, 0.86f), 3.35f);

        var label = go.AddComponent<TextMeshPro>();
        ConfigureFeedbackText(label, text, color, 3.35f);
        label.outlineWidth = 0.14f;
        label.outlineColor = Color.black;

        _floatingFeedbackTexts.Add(go);
        _feedbackTextSpawnCount++;
        _lastFeedbackText = text;
        _feedbackTextHistory.Add(text);
        if (_feedbackTextHistory.Count > 24)
            _feedbackTextHistory.RemoveAt(0);
        StartCoroutine(AnimateWorldFeedback(unit.id, go, label, shadow, color));
    }

    Vector3 GetFeedbackStartPosition(SrpUnitRuntime unit)
    {
        int activeCount = 0;
        _activeFeedbackByUnit.TryGetValue(unit.id, out activeCount);
        _activeFeedbackByUnit[unit.id] = activeCount + 1;

        Vector3 center = GetUnitWorldCenter(unit);
        Vector3 screenUp = GetBoardScreenUp();
        Vector3 screenRight = GetBoardScreenRight();
        int lane = activeCount;
        float sideStep = lane == 0 ? 0f : ((lane % 2 == 0) ? -1f : 1f) * 0.08f * Mathf.Ceil(lane * 0.5f);
        var start = center
            + Vector3.up * 0.64f
            + screenUp * (WorldFeedbackLaneGap * lane)
            + screenRight * sideStep;

        _hasPreviousFeedbackStartPosition = _feedbackTextSpawnCount > 0;
        _previousFeedbackStartPosition = _lastFeedbackStartPosition;
        _lastFeedbackStartPosition = start;
        return start;
    }

    Vector3 GetBoardScreenUp()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            var projected = Vector3.ProjectOnPlane(cam.transform.up, Vector3.up);
            if (projected.sqrMagnitude > 0.0001f)
                return projected.normalized;
        }
        return Vector3.forward;
    }

    Vector3 GetBoardScreenRight()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            var projected = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up);
            if (projected.sqrMagnitude > 0.0001f)
                return projected.normalized;
        }
        return Vector3.right;
    }

    static void ConfigureFeedbackText(TextMeshPro label, string text, Color color, float fontSize)
    {
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.enableWordWrapping = false;
        label.text = text;
        label.color = color;
        label.richText = false;
        label.rectTransform.sizeDelta = new Vector2(8f, 1.4f);
        var renderer = label.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 30;
        }
    }

    IEnumerator AnimateWorldFeedback(int unitId, GameObject go, TextMeshPro label, TextMeshPro shadow, Color color)
    {
        Vector3 start = go.transform.position;
        Vector3 screenUp = GetBoardScreenUp();
        float elapsed = 0f;
        while (elapsed < WorldFeedbackDuration && go != null && label != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / WorldFeedbackDuration);
            go.transform.position = start + screenUp * (WorldFeedbackBoardRise * t) + Vector3.up * (0.08f * t);
            float fadeT = Mathf.Clamp01((elapsed - WorldFeedbackHoldDuration) / (WorldFeedbackDuration - WorldFeedbackHoldDuration));
            SetFeedbackTextAlpha(label, color, 1f - fadeT);
            if (shadow != null)
                SetFeedbackTextAlpha(shadow, Color.black, 0.9f * (1f - fadeT));
            yield return null;
        }
        if (_activeFeedbackByUnit.TryGetValue(unitId, out var count))
        {
            count = Mathf.Max(0, count - 1);
            if (count == 0)
                _activeFeedbackByUnit.Remove(unitId);
            else
                _activeFeedbackByUnit[unitId] = count;
        }
        if (go != null)
        {
            _floatingFeedbackTexts.Remove(go);
            Destroy(go);
        }
    }

    static void SetFeedbackTextAlpha(TextMeshPro label, Color baseColor, float alpha)
    {
        if (label == null)
            return;
        var color = baseColor;
        color.a = Mathf.Clamp01(alpha);
        label.color = color;
    }

    void FlashUnit(SrpUnitRuntime unit, Color color)
    {
        if (unit == null || !_unitObjs.TryGetValue(unit.id, out var go) || go == null)
            return;
        var renderer = go.GetComponent<Renderer>();
        if (renderer == null)
            return;

        if (_unitFlashCoroutines.TryGetValue(unit.id, out var running) && running != null)
            StopCoroutine(running);
        _unitFlashCoroutines[unit.id] = StartCoroutine(FlashUnitRoutine(unit.id, renderer, color));
    }

    IEnumerator FlashUnitRoutine(int unitId, Renderer renderer, Color flashColor)
    {
        const float duration = 0.36f;
        _flashingUnitIds.Add(unitId);
        float elapsed = 0f;
        while (elapsed < duration && renderer != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            var unit = GetUnit(unitId);
            Color baseColor = GetUnitBaseColor(unit);
            ApplyColor(renderer, Color.Lerp(flashColor, baseColor, t));
            yield return null;
        }

        _flashingUnitIds.Remove(unitId);
        _unitFlashCoroutines.Remove(unitId);
        var finalUnit = GetUnit(unitId);
        if (renderer != null && finalUnit != null)
            ApplyColor(renderer, GetUnitBaseColor(finalUnit));
    }

    struct UnitVitals
    {
        public int hp;
        public int pg;
    }

    Dictionary<int, UnitVitals> CaptureUnitVitals()
    {
        var result = new Dictionary<int, UnitVitals>();
        if (_state == null)
            return result;
        foreach (var unit in _state.Units)
        {
            if (unit == null || unit.eliminated)
                continue;
            result[unit.id] = new UnitVitals { hp = unit.hp, pg = unit.pg };
        }
        return result;
    }

    void FlashChangedUnits(Dictionary<int, UnitVitals> before)
    {
        if (before == null || _state == null)
            return;
        foreach (var unit in _state.Units)
        {
            if (unit == null || unit.eliminated || !before.TryGetValue(unit.id, out var old))
                continue;
            bool damaged = unit.hp < old.hp || unit.pg < old.pg;
            bool restored = unit.hp > old.hp || unit.pg > old.pg;
            if (damaged)
                FlashUnit(unit, new Color(1f, 0.15f, 0.12f));
            else if (restored)
                FlashUnit(unit, new Color(0.1f, 1f, 0.65f));
        }
    }

    void EnsureUnitFeedbackRoot()
    {
        if (_unitFeedbackRoot != null)
            return;
        _unitFeedbackRoot = new GameObject("SrpUnitFeedbackLayer");
        _unitFeedbackRoot.transform.SetParent(transform, false);
    }

    void ClearUnitFeedbackObjects()
    {
        foreach (var running in _unitFlashCoroutines.Values)
        {
            if (running != null)
                StopCoroutine(running);
        }
        if (_unitFeedbackRoot != null)
            Destroy(_unitFeedbackRoot);
        _unitFeedbackRoot = null;
        _currentUnitRing = null;
        _selectedUnitRing = null;
        _hoverUnitRing = null;
        if (_moveGhostUnit != null)
            Destroy(_moveGhostUnit);
        _moveGhostUnit = null;
        foreach (var arrow in _unitFacingArrows.Values)
            if (arrow != null)
                Destroy(arrow);
        _unitFacingArrows.Clear();
        ClearMovePreviewThreatObjects();
        _unitStatusBadges.Clear();
        _unitFlashCoroutines.Clear();
        _flashingUnitIds.Clear();
        _floatingFeedbackTexts.Clear();
        _feedbackTextHistory.Clear();
        _activeFeedbackByUnit.Clear();
        _feedbackTextSpawnCount = 0;
        _lastFeedbackText = string.Empty;
        _previousFeedbackStartPosition = Vector3.zero;
        _lastFeedbackStartPosition = Vector3.zero;
        _hasPreviousFeedbackStartPosition = false;
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

    void RenderMovePreview(SrpUnitRuntime unit, SrpMovePreviewEvaluation preview)
    {
        ClearOverlayLayer(OverlayMovePreviewCover);
        ClearOverlayLayer(OverlayThreatLine);
        ClearOverlayLayer(OverlayOverwatchThreatLine);
        ClearOverlayLayer(OverlayOverwatchThreatEndpoint);
        ClearMovePreviewThreatObjects();
        RenderMoveGhostUnit(unit, preview);

        if (preview == null || !preview.valid)
            return;

        if (preview.hasCover)
            SetOverlayTile(OverlayMovePreviewCover, preview.coverX, preview.coverY, new Color(0.45f, 1f, 0.62f, 0.72f));

        foreach (var threat in preview.threats)
            RenderMovePreviewThreatLine(preview, threat);
    }

    void RenderMovePreviewThreatLine(SrpMovePreviewEvaluation preview, SrpMovePreviewThreat threat)
    {
        if (preview == null || threat == null)
            return;
        var attacker = GetUnit(threat.attackerId);
        if (attacker == null)
            return;

        EnsureMovePreviewThreatRoot();
        bool overwatch = threat.isOverwatch;
        Color color = overwatch
            ? new Color(1f, 0.08f, 0.06f, 0.96f)
            : new Color(0.95f, 0.58f, 0.26f, 0.62f);
        var go = new GameObject(overwatch ? "MovePreviewThreatLine_Overwatch" : "MovePreviewThreatLine_Basic");
        go.transform.SetParent(_movePreviewThreatRoot.transform, false);
        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.sharedMaterial = CreateFeedbackMaterial(color);
        line.startColor = color;
        line.endColor = color;
        line.widthMultiplier = overwatch ? 0.075f : 0.036f;
        line.numCapVertices = overwatch ? 6 : 3;
        line.numCornerVertices = 3;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;

        var start = GetUnitWorldCenter(attacker) + Vector3.up * (overwatch ? 0.92f : 0.78f);
        var end = new Vector3(preview.destinationX * cellSize, overwatch ? 0.46f : 0.38f, preview.destinationY * cellSize);
        var points = BuildArcPoints(start, end, overwatch ? 0.82f : 0.50f, threat.lineTiles);
        line.positionCount = points.Length;
        line.SetPositions(points);
        _movePreviewThreatObjects.Add(go);

        if (overwatch)
            RenderMovePreviewOverwatchEndpoint(end, color);
    }

    Vector3[] BuildArcPoints(Vector3 start, Vector3 end, float arcHeight, List<Vector2Int> lineTiles)
    {
        int count = Mathf.Clamp((lineTiles != null ? lineTiles.Count : 0) + 3, 6, 18);
        var points = new Vector3[count];
        Vector3 delta = end - start;
        for (int i = 0; i < count; i++)
        {
            float t = count <= 1 ? 1f : i / (float)(count - 1);
            var point = start + delta * t;
            point.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
            points[i] = point;
        }
        return points;
    }

    void RenderMovePreviewOverwatchEndpoint(Vector3 end, Color color)
    {
        var marker = new GameObject("MovePreviewThreatEndpoint_OverwatchPulse", typeof(MeshFilter), typeof(MeshRenderer));
        marker.transform.SetParent(_movePreviewThreatRoot.transform, false);
        marker.GetComponent<MeshFilter>().sharedMesh = GetTileRingMesh();
        marker.transform.position = new Vector3(end.x, TileOverlayTopY + 0.11f, end.z);
        marker.transform.localScale = new Vector3(cellSize * 0.62f, 1f, cellSize * 0.62f);
        var renderer = marker.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateFeedbackMaterial(color);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = 28;
        ApplyFeedbackColor(renderer, color);
        _movePreviewThreatObjects.Add(marker);
    }

    void RenderMoveGhostUnit(SrpUnitRuntime unit, SrpMovePreviewEvaluation preview)
    {
        if (_moveGhostUnit == null)
        {
            _moveGhostUnit = new GameObject("MovePreviewGhostUnit", typeof(MeshFilter), typeof(MeshRenderer));
            _moveGhostUnit.transform.SetParent(transform, false);
            _moveGhostUnit.GetComponent<MeshFilter>().sharedMesh = GetUnitFacingWedgeMesh();
            var renderer = _moveGhostUnit.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateFeedbackMaterial(new Color(0.72f, 0.95f, 1f, 0.38f));
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        if (unit == null || preview == null || !preview.valid)
        {
            _moveGhostUnit.SetActive(false);
            return;
        }

        _moveGhostUnit.SetActive(true);
        _moveGhostUnit.transform.position = new Vector3(preview.destinationX * cellSize, 0.18f, preview.destinationY * cellSize);
        _moveGhostUnit.transform.rotation = GetFacingRotation(unit.facing);
        float scale = unit.HasTag(SrpUnitTags.Large) ? 0.88f : 0.72f;
        _moveGhostUnit.transform.localScale = new Vector3(scale, 0.24f, scale);
        ApplyFeedbackColor(_moveGhostUnit.GetComponent<Renderer>(), new Color(0.72f, 0.95f, 1f, 0.38f));
    }

    void ClearMovePreviewVisuals()
    {
        ClearOverlayLayer(OverlayMovePreviewCover);
        ClearOverlayLayer(OverlayThreatLine);
        ClearOverlayLayer(OverlayOverwatchThreatLine);
        ClearOverlayLayer(OverlayOverwatchThreatEndpoint);
        ClearMovePreviewThreatObjects();
        if (_moveGhostUnit != null)
            _moveGhostUnit.SetActive(false);
        _currentMovePreview = null;
    }

    void EnsureMovePreviewThreatRoot()
    {
        if (_movePreviewThreatRoot != null)
            return;
        _movePreviewThreatRoot = new GameObject("SrpMovePreviewThreatLines");
        _movePreviewThreatRoot.transform.SetParent(transform, false);
    }

    void ClearMovePreviewThreatObjects()
    {
        foreach (var go in _movePreviewThreatObjects)
            if (go != null)
                Destroy(go);
        _movePreviewThreatObjects.Clear();
    }

    void RenderCoverObjects()
    {
        ClearCoverObjects();
        if (_state == null)
            return;

        _coverObjectRoot = new GameObject("SrpCoverObjects");
        _coverObjectRoot.transform.SetParent(transform, false);

        if (_state.CoverObjects != null)
        {
            foreach (var coverObject in _state.CoverObjects)
                RenderOccupyingCoverObject(coverObject);
        }

        if (_state.CoverSegments != null)
        {
            foreach (var segment in _state.CoverSegments)
                RenderEdgeCoverSegment(segment);
        }
    }

    void RenderOccupyingCoverObject(SrpCoverObjectData coverObject)
    {
        if (coverObject == null || !_state.InBounds(coverObject.x, coverObject.y))
            return;

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = coverObject.blocksLineOfSight
            ? $"CoverObject_LOS_{coverObject.x}_{coverObject.y}"
            : $"CoverObject_{coverObject.x}_{coverObject.y}";
        cube.transform.SetParent(_coverObjectRoot.transform, false);
        float height = coverObject.blocksLineOfSight ? 0.82f : 0.46f;
        cube.transform.position = new Vector3(coverObject.x * cellSize, height * 0.5f, coverObject.y * cellSize);
        cube.transform.localScale = new Vector3(cellSize * 0.70f, height, cellSize * 0.70f);
        var renderer = cube.GetComponent<Renderer>();
        var color = coverObject.blocksLineOfSight
            ? new Color(0.34f, 0.43f, 0.50f, 1f)
            : new Color(0.48f, 0.62f, 0.48f, 1f);
        ApplyColor(renderer, color);
        _coverObjects.Add(cube);
    }

    void RenderEdgeCoverSegment(SrpCoverSegmentData segment)
    {
        if (segment == null || !_state.InBounds(segment.x, segment.y))
            return;

        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = segment.blocksLineOfSight
            ? $"CoverEdgeSegment_LOS_{segment.x}_{segment.y}_{segment.edge}"
            : $"CoverEdgeSegment_{segment.x}_{segment.y}_{segment.edge}";
        wall.transform.SetParent(_coverObjectRoot.transform, false);

        const float edgeOffset = 0.46f;
        const float thickness = 0.12f;
        float height = segment.blocksLineOfSight ? 0.52f : 0.34f;
        var pos = new Vector3(segment.x * cellSize, height * 0.5f, segment.y * cellSize);
        var scale = new Vector3(cellSize * 0.74f, height, cellSize * thickness);
        if (segment.edge == SrpCoverEdge.North)
            pos.z += cellSize * edgeOffset;
        else if (segment.edge == SrpCoverEdge.South)
            pos.z -= cellSize * edgeOffset;
        else if (segment.edge == SrpCoverEdge.East)
        {
            pos.x += cellSize * edgeOffset;
            scale = new Vector3(cellSize * thickness, height, cellSize * 0.74f);
        }
        else
        {
            pos.x -= cellSize * edgeOffset;
            scale = new Vector3(cellSize * thickness, height, cellSize * 0.74f);
        }

        wall.transform.position = pos;
        wall.transform.localScale = scale;
        var renderer = wall.GetComponent<Renderer>();
        var color = segment.blocksLineOfSight
            ? new Color(0.42f, 0.45f, 0.48f, 1f)
            : new Color(0.42f, 0.58f, 0.42f, 1f);
        ApplyColor(renderer, color);
        _coverObjects.Add(wall);
    }

    void ClearCoverObjects()
    {
        foreach (var go in _coverObjects)
            if (go != null)
                Destroy(go);
        _coverObjects.Clear();
        if (_coverObjectRoot != null)
            Destroy(_coverObjectRoot);
        _coverObjectRoot = null;
    }

#if UNITY_INCLUDE_TESTS
    public bool TestHasCurrentActionRing => _currentUnitRing != null && _currentUnitRing.activeInHierarchy;
    public bool TestHasSelectedUnitRing => _selectedUnitRing != null && _selectedUnitRing.activeInHierarchy;
    public bool TestHasHoverUnitRing => _hoverUnitRing != null && _hoverUnitRing.activeInHierarchy;
    public float TestTileSurfaceY => TileSurfaceY;
    public float TestCurrentActionRingWorldY => _currentUnitRing != null ? _currentUnitRing.transform.position.y : -1f;
    public float TestSelectedUnitRingWorldY => _selectedUnitRing != null ? _selectedUnitRing.transform.position.y : -1f;
    public float TestHoverUnitRingWorldY => _hoverUnitRing != null ? _hoverUnitRing.transform.position.y : -1f;
    public float TestCurrentActionRingRadiusScale => _currentUnitRing != null ? _currentUnitRing.transform.localScale.x : 0f;
    public float TestSelectedUnitRingRadiusScale => _selectedUnitRing != null ? _selectedUnitRing.transform.localScale.x : 0f;
    public float TestHoverUnitRingRadiusScale => _hoverUnitRing != null ? _hoverUnitRing.transform.localScale.x : 0f;
    public int TestVisibleUnitStatusBadgeCount
    {
        get
        {
            int count = 0;
            foreach (var badge in _unitStatusBadges.Values)
                if (badge != null && badge.activeInHierarchy)
                    count++;
            return count;
        }
    }
    public int TestFloatingFeedbackSpawnCount => _feedbackTextSpawnCount;
    public string TestLastFloatingFeedbackText => _lastFeedbackText;
    public string TestFloatingFeedbackHistory => string.Join("\n", _feedbackTextHistory);
    public float TestWorldFeedbackDuration => WorldFeedbackDuration;
    public float TestWorldFeedbackHoldDuration => WorldFeedbackHoldDuration;
    public bool TestHasStackedFeedbackStartPositions => _hasPreviousFeedbackStartPosition
        && Vector3.Distance(_previousFeedbackStartPosition, _lastFeedbackStartPosition) > 0.01f;
    public int TestTileOverlayVisualCount => CountActiveTileOverlayVisuals();
    public int TestMoveOverlayMarkerCount => CountTileOverlayVisualsForLayer(OverlayMove);
    public int TestDangerAttackTintTileCount => CountTintOverlayTilesForLayer(OverlayDangerAttack);
    public int TestOverwatchTintTileCount => CountTintOverlayTilesForLayer(OverlayOverwatch);
    public int TestDangerAttackMeshVisualCount => CountTileOverlayVisualsForLayer(OverlayDangerAttack);
    public int TestOverwatchMeshVisualCount => CountTileOverlayVisualsForLayer(OverlayOverwatch);
    public int TestAttackPreviewMarkerCount => CountTileOverlayVisualsForLayer(OverlayAttack);
    public int TestCoverPreviewMarkerCount => CountTileOverlayVisualsForLayer(OverlayCover);
    public int TestSkillPreviewMarkerCount => CountTileOverlayVisualsForLayer(OverlaySkill);
    public bool TestDangerAttackUsesMarker => TestDangerAttackTintTileCount == 0 && TestDangerAttackMeshVisualCount > 0;
    public bool TestOverwatchUsesMarker => TestOverwatchTintTileCount == 0 && TestOverwatchMeshVisualCount > 0;
    public int TestDangerZocWarningRingCount => CountTileOverlayVisualsForLayer(OverlayDangerZoc);
    public int TestInteractionObjectiveMarkerCount => CountTileOverlayVisualsForLayer(OverlayInteraction);
    public int TestMovePreviewCoverMarkerCount => CountTileOverlayVisualsForLayer(OverlayMovePreviewCover);
    public int TestMovePreviewThreatLineCount => CountActiveMovePreviewThreatObjects("MovePreviewThreatLine_Basic");
    public int TestMovePreviewOverwatchThreatLineCount => CountActiveMovePreviewThreatObjects("MovePreviewThreatLine_Overwatch");
    public int TestMovePreviewOverwatchEndpointCount => CountActiveMovePreviewThreatObjects("MovePreviewThreatEndpoint_OverwatchPulse");
    public int TestMovePreviewThreatTileMarkerCount => CountTileOverlayVisualsForLayer(OverlayThreatLine) + CountTileOverlayVisualsForLayer(OverlayOverwatchThreatLine);
    public bool TestMovePreviewThreatLinesAreWorldSpace => ActiveThreatLinesUseWorldSpace();
    public float TestMovePreviewBasicThreatLineWidth => GetFirstThreatLineWidth("MovePreviewThreatLine_Basic");
    public float TestMovePreviewOverwatchThreatLineWidth => GetFirstThreatLineWidth("MovePreviewThreatLine_Overwatch");
    public bool TestHasMovePreviewGhost => _moveGhostUnit != null && _moveGhostUnit.activeInHierarchy;
    public bool TestMovePreviewGhostAvoidsCoverObjects => MovePreviewGhostAvoidsCoverObjects();
    public int TestUnitFacingArrowCount => CountActiveUnitFacingArrows();
    public int TestCoverObjectCount => CountActiveCoverObjects(false);
    public int TestLineOfSightCoverObjectCount => CountActiveCoverObjects(true);
    public int TestOccupyingCoverObjectCount => CountActiveCoverVisuals("CoverObject");
    public int TestEdgeCoverSegmentVisualCount => CountActiveCoverVisuals("CoverEdgeSegment");
    public bool TestEdgeCoverSegmentsRenderOnEdges => EdgeCoverSegmentsRenderOnEdges();
    public bool TestOccupyingCoverObjectsAvoidUnits => OccupyingCoverObjectsAvoidUnits();
    public bool TestHasTacticalCameraController => _tacticalCamera != null;
    public bool TestTacticalCameraToggleKeyIsC => _tacticalCamera != null
        && _tacticalCamera.TestToggleViewKey == KeyCode.C;
    public bool TestTacticalCameraPanZoomFocusStable => _tacticalCamera != null
        && _tacticalCamera.TestPanZoomFocusReturnsToBoardCenter();
    public bool TestPerspectiveZoomChangesFocusDistance => _tacticalCamera != null
        && _tacticalCamera.TestPerspectiveZoomChangesFocusDistance();
    public bool TestPanThenZoomKeepsFocusPoint => _tacticalCamera != null
        && _tacticalCamera.TestPanThenZoomKeepsFocusPoint();
    public bool TestTacticalCameraToggleMode()
    {
        if (_tacticalCamera == null)
            return false;
        bool before = _tacticalCamera.IsTopOrthographic;
        _tacticalCamera.ToggleViewMode();
        return _tacticalCamera.IsTopOrthographic != before;
    }
    public bool TestHasAimLineOverlay => CountTileOverlayVisualsForLayer(OverlayAimLine) > 0;
    public int TestAimLineOverlayCount => CountTileOverlayVisualsForLayer(OverlayAimLine);
    public float TestTileOverlayMaxWorldY => GetMaxTileOverlayWorldY();
    public float TestMoveOverlayMarkerScale => GetFirstTileOverlayScaleForLayer(OverlayMove);
    public float TestInteractionObjectiveMarkerScale => GetFirstTileOverlayScaleForLayer(OverlayInteraction);

    public bool TestShowCurrentOverwatchRange()
    {
        if (_state == null || _state.CurrentUnitId <= 0)
            return false;
        var unit = GetUnit(_state.CurrentUnitId);
        if (unit == null)
            return false;
        unit.actionPoints = Mathf.Max(unit.actionPoints, 1);
        unit.reactionPoints = Mathf.Max(unit.reactionPoints, 1);
        if (unit.UsesAmmo)
            unit.ammo = Mathf.Max(unit.ammo, 1);
        HighlightOverwatchTiles(unit);
        return TestOverwatchMeshVisualCount > 0;
    }

    public bool TestSpawnTwoFeedbackOnCurrentUnit()
    {
        if (_state == null)
            return false;
        var unit = GetUnit(_state.CurrentUnitId);
        if (unit == null)
            return false;
        SpawnWorldFeedback(unit, "TEST A", Color.white);
        SpawnWorldFeedback(unit, "TEST B", Color.yellow);
        return TestHasStackedFeedbackStartPositions;
    }

    int CountActiveTileOverlayVisuals()
    {
        int count = 0;
        foreach (var go in _tileOverlayVisuals.Values)
            if (go != null && go.activeInHierarchy)
                count++;
        return count;
    }

    int CountTileOverlayVisualsForLayer(int layer)
    {
        int count = 0;
        string prefix = $"TileOverlay_{layer}_";
        foreach (var go in _tileOverlayVisuals.Values)
        {
            if (go != null && go.activeInHierarchy && go.name.StartsWith(prefix))
                count++;
        }
        return count;
    }

    int CountTintOverlayTilesForLayer(int layer)
    {
        if (!_tileOverlayLayers.TryGetValue(layer, out var map))
            return 0;
        int count = 0;
        foreach (var kv in map)
            count++;
        return count;
    }

    float GetMaxTileOverlayWorldY()
    {
        float maxY = -1f;
        foreach (var go in _tileOverlayVisuals.Values)
        {
            if (go != null && go.activeInHierarchy)
                maxY = Mathf.Max(maxY, go.transform.position.y);
        }
        return maxY;
    }

    float GetFirstTileOverlayScaleForLayer(int layer)
    {
        string prefix = $"TileOverlay_{layer}_";
        foreach (var go in _tileOverlayVisuals.Values)
        {
            if (go != null && go.activeInHierarchy && go.name.StartsWith(prefix))
                return go.transform.localScale.x;
        }
        return 0f;
    }

    bool LayerUsesMesh(int layer, string meshName)
    {
        int count = 0;
        string prefix = $"TileOverlay_{layer}_";
        foreach (var go in _tileOverlayVisuals.Values)
        {
            if (go == null || !go.activeInHierarchy || !go.name.StartsWith(prefix))
                continue;
            count++;
            var filter = go.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || filter.sharedMesh.name != meshName)
                return false;
        }
        return count > 0;
    }

    int CountActiveMovePreviewThreatObjects(string prefix)
    {
        int count = 0;
        foreach (var go in _movePreviewThreatObjects)
            if (go != null && go.activeInHierarchy && go.name.StartsWith(prefix))
                count++;
        return count;
    }

    bool ActiveThreatLinesUseWorldSpace()
    {
        bool found = false;
        foreach (var go in _movePreviewThreatObjects)
        {
            if (go == null || !go.activeInHierarchy)
                continue;
            var line = go.GetComponent<LineRenderer>();
            if (line == null)
                continue;
            found = true;
            if (!line.useWorldSpace)
                return false;
            if (line.positionCount < 2)
                return false;
            if (line.GetPosition(0).y <= TileOverlayTopY)
                return false;
        }
        return found;
    }

    float GetFirstThreatLineWidth(string prefix)
    {
        foreach (var go in _movePreviewThreatObjects)
        {
            if (go == null || !go.activeInHierarchy || !go.name.StartsWith(prefix))
                continue;
            var line = go.GetComponent<LineRenderer>();
            if (line != null)
                return line.widthMultiplier;
        }
        return 0f;
    }

    int CountActiveUnitFacingArrows()
    {
        int count = 0;
        foreach (var go in _unitFacingArrows.Values)
            if (go != null && go.activeInHierarchy)
                count++;
        return count;
    }

    int CountActiveCoverObjects(bool lineOfSightOnly)
    {
        int count = 0;
        foreach (var go in _coverObjects)
        {
            if (go == null || !go.activeInHierarchy)
                continue;
            bool los = go.name.StartsWith("CoverObject_LOS_") || go.name.StartsWith("CoverEdgeSegment_LOS_");
            if (!lineOfSightOnly || los)
                count++;
        }
        return count;
    }

    int CountActiveCoverVisuals(string prefix)
    {
        int count = 0;
        foreach (var go in _coverObjects)
            if (go != null && go.activeInHierarchy && go.name.StartsWith(prefix))
                count++;
        return count;
    }

    bool EdgeCoverSegmentsRenderOnEdges()
    {
        bool found = false;
        foreach (var go in _coverObjects)
        {
            if (go == null || !go.activeInHierarchy || !go.name.StartsWith("CoverEdgeSegment"))
                continue;
            found = true;
            var pos = go.transform.position;
            float nearestTileX = Mathf.Round(pos.x / cellSize) * cellSize;
            float nearestTileZ = Mathf.Round(pos.z / cellSize) * cellSize;
            float dx = Mathf.Abs(pos.x - nearestTileX);
            float dz = Mathf.Abs(pos.z - nearestTileZ);
            if (Mathf.Max(dx, dz) < cellSize * 0.35f)
                return false;
            if (go.transform.localScale.x > cellSize * 0.35f && go.transform.localScale.z > cellSize * 0.35f)
                return false;
        }
        return found;
    }

    bool OccupyingCoverObjectsAvoidUnits()
    {
        if (_state == null)
            return true;
        foreach (var go in _coverObjects)
        {
            if (go == null || !go.activeInHierarchy || !go.name.StartsWith("CoverObject"))
                continue;
            int x = Mathf.RoundToInt(go.transform.position.x / cellSize);
            int y = Mathf.RoundToInt(go.transform.position.z / cellSize);
            if (_state.GetOccupant(x, y) != null)
                return false;
        }
        return true;
    }

    bool MovePreviewGhostAvoidsCoverObjects()
    {
        if (_state == null || _moveGhostUnit == null || !_moveGhostUnit.activeInHierarchy)
            return true;
        int x = Mathf.RoundToInt(_moveGhostUnit.transform.position.x / cellSize);
        int y = Mathf.RoundToInt(_moveGhostUnit.transform.position.z / cellSize);
        return !_state.TryGetCoverObjectAt(x, y, out _);
    }
#endif
}

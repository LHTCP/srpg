using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// SRPG 런타임 — 핵심 필드, 생명주기, 입력, 전투, 게임 흐름.
/// 렌더링은 SrpGameController.Rendering.cs, HUD는 SrpGameController.Hud.cs 참조.
/// </summary>
public partial class SrpGameController : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────

    public float cellSize = 1f;

    [Tooltip("비어 있으면 startPreset 맵을 사용합니다.")]
    public SrpMapFileV1 initialMap;

    [Tooltip("initialMap이 비어 있을 때 로드할 내장 프리셋.")]
    public SrpMapPreset startPreset = SrpMapPreset.M1QaIntegrated;

    [Header("Camera")]
    public bool frameCameraOnStart = true;

    [Tooltip("직교 뷰에서 보드 여유(월드 단위).")]
    public float orthoViewPadding = 0.75f;

    [Header("HUD")]
    [Tooltip("왼쪽 컨트롤 패널 폭(캔버스 단위).")]
    public float leftPanelWidth = 630f;

    [Tooltip("오른쪽 로그 패널 폭(캔버스 단위).")]
    public float rightPanelWidth = 630f;

    [Tooltip("시작 시 오른쪽 로그 패널 표시 여부.")]
    public bool startWithLogVisible = true;

    // ── 시뮬레이션 상태 ──────────────────────────────────────────────────────

    const float DefaultHudPanelWidth = 370f;
    SrpBattleState _state;
    readonly Stack<SrpBattleState> _undo = new Stack<SrpBattleState>();

    // ── 입력·페이즈 상태 ─────────────────────────────────────────────────────

    enum Phase { Idle, UnitActive, SelectingSkillTarget }

    Phase _phase;
    int? _selectedId;
    int _remainingMove;
    bool _hasAttackedThisTurn;
    readonly Dictionary<Vector2Int, int> _moveCostMap = new Dictionary<Vector2Int, int>();
    readonly List<int> _attackIds = new List<int>();

    SrpSkillData _pendingSkillData;
    SrpSkillRuntime _pendingSkillRuntime;
    readonly List<Vector2Int> _skillTargetTiles = new List<Vector2Int>();
    bool _dangerAreaVisible;
    string _hoverStatusHint = string.Empty;
    int _hoverUnitId = -1;

    bool _gameOver;

    // ── 생명주기 ─────────────────────────────────────────────────────────────

    void Awake()
    {
        EnsureEventSystem();
        SrpFontWarmup.Warmup();
        if (leftPanelWidth <= 0f)
            leftPanelWidth = DefaultHudPanelWidth;
        if (rightPanelWidth <= 0f)
            rightPanelWidth = DefaultHudPanelWidth;

        // 로비에서 전달한 맵 또는 프리셋 우선 적용
        if (SrpGameSettings.CustomMap != null)
        {
            initialMap            = SrpGameSettings.CustomMap;
            SrpGameSettings.CustomMap = null;
        }
        else
        {
            startPreset = SrpGameSettings.SelectedPreset;
        }

        bool mapEmpty = initialMap == null
            || initialMap.placements == null
            || initialMap.placements.Length == 0;
        if (mapEmpty)
            initialMap = SrpDefaultMaps.GetPreset(startPreset);

        _state = SrpBattleState.FromMap(initialMap);
        _undo.Clear();

        BuildGrid();
        if (frameCameraOnStart) FrameBoardCamera();
        BuildHud();
        RefreshUnitViews();
        LogLine("SRPG 프로토타입 — 속도 기반 라운드 턴 시작.");
        StartRound();
        UpdateHud();
    }

    // ── 카메라 ───────────────────────────────────────────────────────────────

    public void FrameBoardCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        float w = _state.Width;
        float h = _state.Height;
        float cx = (w - 1f) * 0.5f * cellSize;
        float cz = (h - 1f) * 0.5f * cellSize;
        cam.orthographic = true;
        float aspect = Mathf.Max(cam.aspect, 0.01f);
        float halfNeededV = h * cellSize * 0.5f;
        float halfNeededH = w * cellSize / (2f * aspect);
        cam.orthographicSize = Mathf.Max(halfNeededV, halfNeededH) + orthoViewPadding;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 50f;
        cam.transform.position = new Vector3(cx, 12f, cz);
        cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    // ── 초기화 헬퍼 ──────────────────────────────────────────────────────────

    void ResetRoundFlagsAndResources()
    {
        SrpTurnOrder.ResetRoundResources(_state);
        _state.RebuildEngagements();
    }

    static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }

    // ── 입력 ─────────────────────────────────────────────────────────────────

    bool EnsureApAvailable(SrpUnitRuntime unit, string actionLabel)
    {
        if (unit == null)
            return false;
        if (unit.actionPoints > 0)
            return true;
        LogLine($"{unit.displayName} AP 부족: {actionLabel} 불가");
        return false;
    }

    void LogInvalidMoveReason(SrpUnitRuntime unit, int x, int y, SrpUnitRuntime occupant)
    {
        if (unit == null || _state == null)
            return;

        if (!_state.InBounds(x, y))
        {
            LogLine($"이동 불가: ({x},{y})는 전장 범위를 벗어났습니다.");
            return;
        }

        if (!EnsureApAvailable(unit, "이동"))
            return;

        if (!_state.IsWalkableTile(x, y))
        {
            LogLine($"이동 불가: ({x},{y})는 지형 장애물입니다.");
            return;
        }

        if (occupant != null && !occupant.eliminated && occupant.id != unit.id)
        {
            string side = occupant.owner == unit.owner ? "아군" : "적";
            LogLine($"이동 불가: ({x},{y})는 {side} 유닛 {occupant.displayName}({occupant.id})이 점유 중입니다.");
            return;
        }

        if (!_state.CanStandAt(unit, x, y, unit.id))
        {
            LogLine($"이동 불가: ({x},{y})에 풋프린트를 온전히 둘 수 없습니다.");
            return;
        }

        int manhattan = Mathf.Abs(unit.anchorX - x) + Mathf.Abs(unit.anchorY - y);
        if (manhattan > _remainingMove)
        {
            LogLine($"이동 불가: 필요 이동력 {manhattan}, 현재 잔여 {_remainingMove}.");
            return;
        }

        LogLine($"이동 불가: ({x},{y})까지 경로를 찾지 못했습니다. (ZOC/우회 경로 비용 초과 가능)");
    }

    public void OnTileClicked(int x, int y)
    {
        if (_gameOver) return;

        var occ = _state.GetOccupant(x, y);

        if (_phase == Phase.SelectingSkillTarget && _selectedId.HasValue)
        {
            var cell = new Vector2Int(x, y);
            if (_skillTargetTiles.Contains(cell))
            {
                var u = GetUnit(_selectedId.Value);
                if (u == null || !EnsureApAvailable(u, "스킬 사용"))
                {
                    CancelSkillTargeting();
                    return;
                }
                PushUndo();
                SrpSkills.ResolveActiveSkill(_pendingSkillData, _pendingSkillRuntime,
                    u, x, y, _state, LogLine);
                u.hasUsedSkillThisActivation = true;
                u.actionPoints = Mathf.Max(0, u.actionPoints - 1);
                RefreshUnitViews();
                if (TryResolveOverwatchAgainst(u))
                {
                    FinishActivation();
                    return;
                }

                if (_pendingSkillData.endsActivation)
                {
                    FinishActivation();
                }
                else
                {
                    _phase = Phase.UnitActive;
                    _pendingSkillData = null;
                    _pendingSkillRuntime = null;
                    _skillTargetTiles.Clear();
                    RefreshActiveHighlights(u);
                    UpdateHud();
                }
            }
            else
            {
                CancelSkillTargeting();
            }
            return;
        }

        if (_phase == Phase.Idle)
        {
            if (occ != null && !occ.eliminated && _state.CurrentUnitId == occ.id)
                BeginSelectUnit(occ);
            return;
        }

        if (_phase == Phase.UnitActive && _selectedId.HasValue)
        {
            var u = GetUnit(_selectedId.Value);
            if (u == null) return;

            if (occ != null && !occ.eliminated && occ.owner == u.owner && occ.id != u.id)
            {
                LogLine($"{u.displayName} 행동을 먼저 완료하세요.");
                return;
            }

            var cell = new Vector2Int(x, y);
            if (_moveCostMap.TryGetValue(cell, out int moveCost))
            {
                if (!EnsureApAvailable(u, "이동"))
                    return;
                bool wasEngaged = _state.IsUnitEngaged(u.id);
                var previousEngagers = _state.GetEngagedEnemyIds(u.id);
                PushUndo();
                u.anchorX = x;
                u.anchorY = y;
                _state.RebuildEngagements();
                bool disengaged = wasEngaged && !_state.IsUnitEngaged(u.id);
                _remainingMove -= moveCost;
                u.actionPoints = Mathf.Max(0, u.actionPoints - 1);
                u.hasMovedThisActivation = true;
                string disengageHint = disengaged ? " (교전 이탈)" : string.Empty;
                LogLine($"이동: {u.displayName}({u.id}) → ({x},{y}), 잔여 이동력 {_remainingMove}{disengageHint}");
                bool opportunityKilledMover = disengaged && TryResolveOpportunityAttack(u, previousEngagers);
                RefreshUnitViews();
                if (opportunityKilledMover)
                {
                    FinishActivation();
                    return;
                }
                if (TryResolveOverwatchAgainst(u))
                {
                    FinishActivation();
                    return;
                }
                RefreshActiveHighlights(u);
                UpdateHud();
                return;
            }

            if (!_hasAttackedThisTurn && occ != null && !occ.eliminated
                && occ.owner != u.owner && _attackIds.Contains(occ.id))
            {
                if (!EnsureApAvailable(u, "공격"))
                    return;
                DoAttack(u, occ);
                return;
            }

            if (occ != null && !occ.eliminated && occ.owner != u.owner)
            {
                int dist = _state.ChebyshevAnchor(u, occ);
                if (_hasAttackedThisTurn)
                    LogLine($"공격 불가: {u.displayName}은 이번 활성화에서 이미 공격을 사용했습니다.");
                else if (dist > u.attackRange)
                    LogLine($"공격 불가: 사거리 밖 대상입니다. (거리 {dist}, 사거리 {u.attackRange})");
                else if (!EnsureApAvailable(u, "공격"))
                    return;
                else
                    LogLine("공격 불가: 현재 상태에서 해당 대상을 타격할 수 없습니다.");
                return;
            }

            LogInvalidMoveReason(u, x, y, occ);
        }
    }

    public void OnTileHoverEnter(int x, int y)
    {
        if (_gameOver)
            return;
        if (_phase != Phase.UnitActive || !_selectedId.HasValue)
            return;

        ClearOverlayLayer(OverlayHover);
        ClearOverlayLayer(OverlayDangerBlocked);

        var u = GetUnit(_selectedId.Value);
        if (u == null)
            return;

        var cell = new Vector2Int(x, y);
        if (_moveCostMap.TryGetValue(cell, out int moveCost))
        {
            int threatCount = CountEnemyAttackersForTile(x, y, u.owner);
            bool inZoc = _state.IsEnemyAdjacentToTile(x, y, u.owner);
            if (threatCount > 0)
            {
                SetOverlayTile(OverlayHover, x, y, new Color(0.95f, 0.2f, 0.2f));
                _hoverStatusHint = $"위험도 높음: 해당 칸은 {threatCount}명에게 공격 노출";
            }
            else if (inZoc)
            {
                SetOverlayTile(OverlayHover, x, y, new Color(1.0f, 0.6f, 0.25f));
                _hoverStatusHint = "주의: 해당 칸은 ZOC 인접(다음 이동 부담 증가)";
            }
            else
            {
                SetOverlayTile(OverlayHover, x, y, new Color(0.25f, 0.95f, 0.95f));
                _hoverStatusHint = $"안전 칸: 이동 비용 {moveCost}, 직접 위협 없음";
            }
        }
        else
        {
            if (!_state.CanStandAt(u, x, y, u.id))
            {
                SetOverlayTile(OverlayDangerBlocked, x, y, new Color(0.25f, 0.25f, 0.25f));
                _hoverStatusHint = "진입 불가: 장애물 또는 점유 중";
            }
            else
            {
                _hoverStatusHint = "이동 가능 범위 밖";
            }
        }
        UpdateHud();
    }

    public void OnTileHoverExit(int x, int y)
    {
        if (_gameOver)
            return;
        ClearOverlayLayer(OverlayHover);
        ClearOverlayLayer(OverlayDangerBlocked);
        _hoverStatusHint = string.Empty;
        UpdateHud();
    }

    public void OnUnitHoverEnter(int unitId)
    {
        if (_gameOver)
            return;
        var unit = GetUnit(unitId);
        if (unit == null)
            return;

        _hoverUnitId = unitId;
        RenderUnitHoverOverlays(unit);
        _hoverStatusHint = $"유닛 미리보기: {unit.displayName} 공격범위/ZOC 표시";
        UpdateHud();
    }

    public void OnUnitHoverExit(int unitId)
    {
        if (_hoverUnitId != unitId)
            return;
        _hoverUnitId = -1;
        ClearOverlayLayer(OverlayUnitHoverRange);
        ClearOverlayLayer(OverlayUnitHoverZoc);
        _hoverStatusHint = string.Empty;
        UpdateHud();
    }

    public void OnUnitClicked(int unitId)
    {
        var unit = GetUnit(unitId);
        if (unit == null)
            return;
        OnTileClicked(unit.anchorX, unit.anchorY);
    }

    void BeginSelectUnit(SrpUnitRuntime u)
    {
        _postUndoHint = false;
        if (!u.passiveAppliedThisTurn)
        {
            SrpSkills.TryApplyPassiveTurnStart(u, _state, LogLine);
            u.passiveAppliedThisTurn = true;
        }
        _selectedId = u.id;
        _remainingMove = u.moveRange;
        _hasAttackedThisTurn = false;
        u.hasMovedThisActivation = false;
        u.hasAttackedThisActivation = false;
        u.hasUsedSkillThisActivation = false;
        _phase = Phase.UnitActive;
        RefreshActiveHighlights(u);
        UpdateHud();
    }

    void RefreshActiveHighlights(SrpUnitRuntime u)
    {
        ResetTileColors();
        _moveCostMap.Clear();
        if (_remainingMove > 0 && u.actionPoints > 0)
        {
            var costs = SrpPathfinder.GetReachableWithCosts(_state, u, _remainingMove);
            foreach (var kv in costs)
            {
                _moveCostMap[kv.Key] = kv.Value;
                SetOverlayTile(OverlayMove, kv.Key.x, kv.Key.y, new Color(0.3f, 0.9f, 0.4f));
            }
        }
        RefreshAttackTargets(u);
        if (!_hasAttackedThisTurn)
        {
            HighlightAttackTiles();
            HighlightOverwatchTiles(u);
            HighlightParryTelegraphForAttackTargets(u);
        }
        RebuildDangerAndIntentOverlays();
    }

    // ── 전투 ─────────────────────────────────────────────────────────────────

    void RefreshAttackTargets(SrpUnitRuntime atk)
    {
        _attackIds.Clear();
        if (atk.actionPoints <= 0)
            return;
        foreach (var o in _state.Units)
        {
            if (o.eliminated || o.owner == atk.owner) continue;
            if (SrpCombatResolver.CanAttack(_state, atk, o))
                _attackIds.Add(o.id);
        }
    }

    // ── 버튼 핸들러 ──────────────────────────────────────────────────────────

    void OnSkipAttack()
    {
        if (_gameOver || _phase != Phase.UnitActive || !_selectedId.HasValue) return;
        LogLine($"유닛 완료 — {GetUnit(_selectedId.Value)?.displayName}({_selectedId})");
        FinishActivation();
    }

    void BeginSkillTargeting(SrpSkillData data, SrpSkillRuntime runtime)
    {
        if (_selectedId == null) return;
        var u = GetUnit(_selectedId.Value);
        if (u == null) return;

        if (!EnsureApAvailable(u, "스킬 준비"))
        {
            _pendingSkillData = null;
            _pendingSkillRuntime = null;
            _skillTargetTiles.Clear();
            if (_phase == Phase.SelectingSkillTarget)
                CancelSkillTargeting();
            else
                UpdateHud();
            return;
        }

        _pendingSkillData = data;
        _pendingSkillRuntime = runtime;
        _skillTargetTiles.Clear();

        if (data.targetType == SrpTargetType.Self)
        {
            PushUndo();
            SrpSkills.ResolveActiveSkill(data, runtime, u, u.anchorX, u.anchorY, _state, LogLine);
            u.hasUsedSkillThisActivation = true;
            u.actionPoints = Mathf.Max(0, u.actionPoints - 1);
            RefreshUnitViews();
            if (data.endsActivation)
            {
                FinishActivation();
            }
            else
            {
                _pendingSkillData = null;
                _pendingSkillRuntime = null;
                RefreshActiveHighlights(u);
                UpdateHud();
            }
            return;
        }

        _skillTargetTiles.AddRange(SrpSkills.GetSkillTargetTiles(data, u, _state));
        if (_skillTargetTiles.Count == 0)
        {
            LogLine("선택 가능한 스킬 대상이 없습니다.");
            _pendingSkillData = null;
            _pendingSkillRuntime = null;
            _phase = Phase.UnitActive;
            RefreshActiveHighlights(u);
            UpdateHud();
            return;
        }
        _phase = Phase.SelectingSkillTarget;
        ResetTileColors();
        foreach (var tile in _skillTargetTiles)
            SetOverlayTile(OverlaySkill, tile.x, tile.y, new Color(0.7f, 0.3f, 0.9f));
        HighlightParryTelegraphForSkillTargets(u, data);
        UpdateHud();
    }

    void CancelSkillTargeting()
    {
        if (_phase != Phase.SelectingSkillTarget) return;
        _phase = Phase.UnitActive;
        _pendingSkillData = null;
        _pendingSkillRuntime = null;
        _skillTargetTiles.Clear();
        var u = _selectedId.HasValue ? GetUnit(_selectedId.Value) : null;
        if (u != null) RefreshActiveHighlights(u);
        UpdateHud();
    }

    void OnEndTurnSoft()
    {
        if (_gameOver || !_selectedId.HasValue)
            return;
        if (_phase == Phase.SelectingSkillTarget)
        {
            LogLine("스킬 선택을 취소하고 현재 유닛 턴을 강제 종료합니다.");
            CancelSkillTargeting();
            FinishActivation();
            return;
        }
        if (_phase != Phase.UnitActive)
            return;

        LogLine("현재 유닛 턴 강제 종료");
        FinishActivation();
    }

    void OnUndo()
    {
        if (_undo.Count == 0) return;
        _state = _undo.Pop();
        _selectedId = null;
        _phase = Phase.Idle;
        _moveCostMap.Clear();
        _attackIds.Clear();
        _pendingSkillData = null;
        _pendingSkillRuntime = null;
        _skillTargetTiles.Clear();
        ResetTileColors();
        RefreshUnitViews();
        LogLine("— 되감기 —");
        _postUndoHint = true;
        _gameOver = false;
        UpdateHud();
    }

    // ── 게임 흐름 ────────────────────────────────────────────────────────────

    void PushUndo() => _undo.Push(_state.Clone());

    bool TryResolveOpportunityAttack(SrpUnitRuntime mover, List<int> previousEngagers)
    {
        if (mover == null || mover.eliminated || previousEngagers == null)
            return false;

        var attacker = FindOpportunityAttacker(mover, previousEngagers);
        if (attacker == null)
            return false;

        if (!SrpCombatResolver.TryApplyOpportunityAttack(_state, attacker, mover, out var outcome))
            return false;
        SrpSkills.OnAttackResolved(attacker, mover, outcome, _state, LogLine);
        if (outcome.damageToHp > 0 || outcome.damageToPg > 0)
            SrpSkills.OnTakeDamage(mover, _state, LogLine);

        LogLine(
            $"기회공격: {attacker.displayName}({attacker.id}) → {mover.displayName}({mover.id}) | " +
            $"피해 PG-{outcome.damageToPg} HP-{outcome.damageToHp} | 반응 사격 소모");
        LogDefenseBuffers(mover, outcome);
        LogReactionOutcome(mover, outcome);
        if (outcome.becameGroggy)
            LogLine($"PG 붕괴: {mover.displayName}({mover.id}) 처단 위험 상태");
        if (outcome.defenderDied)
        {
            _state.RemoveUnit(mover);
            _state.RebuildEngagements();
            LogLine($"사망: {mover.displayName}({mover.id})");
            CheckWin();
        }

        return outcome.defenderDied;
    }

    bool TryResolveOverwatchAgainst(SrpUnitRuntime target)
    {
        if (target == null || target.eliminated)
            return false;

        foreach (var watcher in _state.Units)
        {
            if (!SrpOverwatch.CanTrigger(_state, watcher, target))
                continue;

            if (!SrpOverwatch.TryTrigger(_state, watcher, target, out var outcome))
                continue;
            SrpSkills.OnAttackResolved(watcher, target, outcome, _state, LogLine);
            if (outcome.damageToHp > 0 || outcome.damageToPg > 0)
                SrpSkills.OnTakeDamage(target, _state, LogLine);

            LogLine(
                $"오버워치 사격: {watcher.displayName}({watcher.id}) → {target.displayName}({target.id}) | " +
                $"피해 PG-{outcome.damageToPg} HP-{outcome.damageToHp} | 반응 사격 소모");
            LogDefenseBuffers(target, outcome);
            LogReactionOutcome(target, outcome);
            if (outcome.becameGroggy)
                LogLine($"PG 붕괴: {target.displayName}({target.id}) 처단 위험 상태");
            if (outcome.defenderDied)
            {
                _state.RemoveUnit(target);
                _state.RebuildEngagements();
                LogLine($"사망: {target.displayName}({target.id})");
                CheckWin();
            }

            return outcome.defenderDied;
        }

        return false;
    }

    SrpUnitRuntime FindOpportunityAttacker(SrpUnitRuntime mover, List<int> previousEngagers)
    {
        foreach (int enemyId in previousEngagers)
        {
            var enemy = _state.FindUnitById(enemyId);
            if (enemy == null || enemy.eliminated || enemy.owner == mover.owner)
                continue;
            if (enemy.reactionPoints <= 0)
                continue;
            if (_state.Engagements.TryGetValue(mover.id, out var current) && current.Contains(enemy.id))
                continue;
            return enemy;
        }

        return null;
    }

    void DoAttack(SrpUnitRuntime atk, SrpUnitRuntime def)
    {
        PushUndo();
        var outcome = SrpCombatResolver.ApplyAttack(_state, atk, def);
        SrpSkills.OnAttackResolved(atk, def, outcome, _state, LogLine);
        if (outcome.damageToHp > 0 || outcome.damageToPg > 0)
            SrpSkills.OnTakeDamage(def, _state, LogLine);
        atk.hasAttackedThisActivation = true;
        _hasAttackedThisTurn = true;
        atk.actionPoints = Mathf.Max(0, atk.actionPoints - 1);
        LogLine(
            $"공격: {atk.displayName}({atk.id}) → {def.displayName}({def.id}) | " +
            $"피해 PG-{outcome.damageToPg} HP-{outcome.damageToHp} | " +
            $"처단:{outcome.wasExecution} 그로기:{outcome.becameGroggy}");
        LogDefenseBuffers(def, outcome);
        LogReactionOutcome(def, outcome);
        if (outcome.becameGroggy)
            LogLine($"PG 붕괴: {def.displayName}({def.id}) 처단 위험 상태");
        if (outcome.wasExecution)
            LogLine($"처단 타격: {def.displayName}({def.id})");
        if (outcome.defenderDied)
        {
            _state.RemoveUnit(def);
            _state.RebuildEngagements();
            LogLine($"사망: {def.displayName}({def.id})");
        }
        if (TryResolveOverwatchAgainst(atk))
        {
            RefreshUnitViews();
            FinishActivation();
            return;
        }
        RefreshUnitViews();
        FinishActivation();
    }

    void LogDefenseBuffers(SrpUnitRuntime defender, SrpCombatResolver.AttackOutcome outcome)
    {
        if (defender == null)
            return;
        if (outcome.sustainedDefenseBufferApplied)
            LogLine($"수비 완충: {defender.displayName} 후속 공격 감쇠 HP-{outcome.reducedHpBySustainedDefense} PG-{outcome.reducedPgBySustainedDefense}");
        if (outcome.tankMultiEngagementBufferApplied)
            LogLine($"탱커 대응: {defender.displayName} 다중 교전 감쇠 HP-{outcome.reducedHpByTank} PG-{outcome.reducedPgByTank}");
    }

    void LogReactionOutcome(SrpUnitRuntime defender, SrpCombatResolver.AttackOutcome outcome)
    {
        if (defender == null || !outcome.reactionSpentRp)
            return;

        string unit = $"{defender.displayName}({defender.id})";
        switch (outcome.reactionKind)
        {
            case SrpReactionKind.Parry:
                LogLine($"방어 반응: {unit} 패링 성공 | 피해 무효");
                break;
            case SrpReactionKind.Dodge:
                if (outcome.wasDodged)
                    LogLine($"방어 반응: {unit} 회피 성공 | 피해 무효");
                else if (outcome.dodgeFailed)
                    LogLine($"방어 반응: {unit} 회피 실패 | 기본 피해 적용");
                else
                    LogLine($"방어 반응: {unit} 회피 발동");
                break;
            case SrpReactionKind.Guard:
                LogLine($"방어 반응: {unit} 가드 발동 | 추가 감쇠 적용");
                break;
            default:
                LogLine($"방어 반응: {unit} {outcome.reactionKind} 발동");
                break;
        }
    }

    void FinishActivation()
    {
        ResetTileColors();
        _selectedId = null;
        _phase = Phase.Idle;
        _moveCostMap.Clear();
        _attackIds.Clear();
        _pendingSkillData = null;
        _pendingSkillRuntime = null;
        _skillTargetTiles.Clear();
        AdvanceToNextActivation();
    }

    void StartRound()
    {
        _state.RoundQueue.Clear();
        _state.RebuildEngagements();
        _state.RoundQueue.AddRange(SrpTurnOrder.BuildRoundQueue(_state));
        foreach (var u in _state.Units)
        {
            if (u.eliminated)
                continue;
            SrpSkills.TickSkillResourcesForUnit(u, _state);
        }
        ResetRoundFlagsAndResources();
        LogLine($"라운드 {_state.RoundNumber} 시작 (유닛 {_state.RoundQueue.Count}명)");
        AdvanceToNextActivation();
    }

    void AdvanceToNextActivation()
    {
        if (_gameOver)
            return;

        CheckWin();
        if (_gameOver)
        {
            UpdateHud();
            return;
        }

        if (!SrpTurnOrder.HasRemainingUnitInRound(_state))
        {
            _state.RoundNumber++;
            StartRound();
            return;
        }

        int nextId = SrpTurnOrder.AdvanceToNextUnit(_state);
        var next = GetUnit(nextId);
        if (next == null || next.eliminated)
        {
            AdvanceToNextActivation();
            return;
        }

        BeginSelectUnit(next);
        LogLine($"행동 시작: {next.displayName}({next.id}) [SPD {next.speed}]");
        CheckWin();
        UpdateHud();
    }

    public void ApplyMap(SrpMapFileV1 map)
    {
        if (map == null) return;
        initialMap = map;

        var grid = transform.Find("SrpGrid");
        if (grid != null) Destroy(grid.gameObject);
        foreach (var kv in _unitObjs)
            if (kv.Value != null) Destroy(kv.Value);
        _unitObjs.Clear();

        _undo.Clear();
        _log.Clear();
        _selectedId = null;
        _phase = Phase.Idle;
        _gameOver = false;

        _state = SrpBattleState.FromMap(map);
        BuildGrid();
        if (frameCameraOnStart) FrameBoardCamera();
        RefreshUnitViews();
        StartRound();
        LogLine("맵 적용: " + map.name);
        UpdateHud();
    }

    void CheckWin()
    {
        var alive = new HashSet<int>();
        foreach (var u in _state.Units)
            if (!u.eliminated) alive.Add(u.owner);

        if (alive.Count > 1) return;
        _gameOver = true;
        if (alive.Count == 1)
        {
            foreach (var a in alive)
                LogLine($"게임 종료: 플레이어 {a} 승리.");
        }
        else
            LogLine("게임 종료: 무승부(전멸).");
    }

    SrpUnitRuntime GetUnit(int id)
    {
        if (_state == null || _state.Units == null)
            return null;
        foreach (var u in _state.Units)
            if (u.id == id) return u;
        return null;
    }

    public void ToggleDangerArea()
    {
        _dangerAreaVisible = !_dangerAreaVisible;
        RebuildDangerAndIntentOverlays();
        UpdateHud();
    }

    public bool IsDangerAreaVisible => _dangerAreaVisible;

    int GetFocusedOwner()
    {
        if (_selectedId.HasValue)
        {
            var selected = GetUnit(_selectedId.Value);
            if (selected != null)
                return selected.owner;
        }
        if (_state != null && _state.CurrentUnitId > 0)
        {
            var current = GetUnit(_state.CurrentUnitId);
            if (current != null)
                return current.owner;
        }
        return 0;
    }

    int CountEnemyAttackersForTile(int x, int y, int friendlyOwner)
    {
        int count = 0;
        foreach (var enemy in _state.Units)
        {
            if (enemy.eliminated || enemy.owner == friendlyOwner)
                continue;
            int dist = Mathf.Max(Mathf.Abs(enemy.anchorX - x), Mathf.Abs(enemy.anchorY - y));
            if (dist <= enemy.attackRange)
                count++;
        }
        return count;
    }

    void RenderUnitHoverOverlays(SrpUnitRuntime unit)
    {
        ClearOverlayLayer(OverlayUnitHoverRange);
        ClearOverlayLayer(OverlayUnitHoverZoc);

        for (int y = 0; y < _state.Height; y++)
        {
            for (int x = 0; x < _state.Width; x++)
            {
                int dist = Mathf.Max(Mathf.Abs(unit.anchorX - x), Mathf.Abs(unit.anchorY - y));
                if (dist > 0 && dist <= unit.attackRange)
                    SetOverlayTile(OverlayUnitHoverRange, x, y, new Color(0.35f, 0.55f, 1f));
            }
        }

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };
        for (int i = 0; i < 4; i++)
            SetOverlayTile(OverlayUnitHoverZoc, unit.anchorX + dx[i], unit.anchorY + dy[i], new Color(1f, 0.85f, 0.2f));
    }

    void RebuildDangerAndIntentOverlays()
    {
        ClearOverlayLayer(OverlayDangerAttack);
        ClearOverlayLayer(OverlayDangerZoc);
        ClearOverlayLayer(OverlayIntentPath);
        ClearOverlayLayer(OverlayIntentTarget);

        if (!_dangerAreaVisible || _state == null)
            return;

        int focusedOwner = GetFocusedOwner();
        foreach (var enemy in _state.Units)
        {
            if (enemy.eliminated || enemy.owner == focusedOwner)
                continue;

            for (int y = 0; y < _state.Height; y++)
            {
                for (int x = 0; x < _state.Width; x++)
                {
                    int dist = Mathf.Max(Mathf.Abs(enemy.anchorX - x), Mathf.Abs(enemy.anchorY - y));
                    if (dist > 0 && dist <= enemy.attackRange)
                        SetOverlayTile(OverlayDangerAttack, x, y, new Color(0.95f, 0.22f, 0.22f));
                }
            }

            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };
            for (int i = 0; i < 4; i++)
                SetOverlayTile(OverlayDangerZoc, enemy.anchorX + dx[i], enemy.anchorY + dy[i], new Color(1f, 0.7f, 0.2f));

            var target = FindNearestEnemyTarget(enemy, focusedOwner);
            if (target == null)
                continue;

            int px = enemy.anchorX;
            int py = enemy.anchorY;
            int maxStep = Mathf.Max(1, enemy.moveRange);
            for (int step = 0; step < maxStep; step++)
            {
                if (px == target.anchorX && py == target.anchorY)
                    break;
                px += target.anchorX > px ? 1 : (target.anchorX < px ? -1 : 0);
                py += target.anchorY > py ? 1 : (target.anchorY < py ? -1 : 0);
                SetOverlayTile(OverlayIntentPath, px, py, new Color(0.25f, 0.55f, 1f));
            }
            SetOverlayTile(OverlayIntentTarget, target.anchorX, target.anchorY, new Color(0.95f, 0.1f, 0.95f));
        }
    }

    SrpUnitRuntime FindNearestEnemyTarget(SrpUnitRuntime source, int focusedOwner)
    {
        SrpUnitRuntime best = null;
        int bestDist = int.MaxValue;
        foreach (var unit in _state.Units)
        {
            if (unit.eliminated || unit.owner != focusedOwner)
                continue;
            int dist = Mathf.Abs(source.anchorX - unit.anchorX) + Mathf.Abs(source.anchorY - unit.anchorY);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = unit;
            }
        }
        return best;
    }

#if UNITY_INCLUDE_TESTS
    public int TestRoundNumber => _state != null ? _state.RoundNumber : -1;
    public int TestCurrentUnitId => _state != null ? _state.CurrentUnitId : -1;
    public int TestRoundQueueCount => _state != null && _state.RoundQueue != null ? _state.RoundQueue.Count : -1;
    public bool TestDangerAreaVisible => _dangerAreaVisible;
    public int TestHoveredUnitId => _hoverUnitId;

    public int TestAliveUnitCount()
    {
        if (_state == null || _state.Units == null)
            return 0;
        int count = 0;
        foreach (var u in _state.Units)
            if (!u.eliminated)
                count++;
        return count;
    }

    public bool TestTryHoverFirstMoveTile()
    {
        foreach (var kv in _moveCostMap)
        {
            OnTileHoverEnter(kv.Key.x, kv.Key.y);
            return true;
        }
        return false;
    }
#endif
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// SRPG 런타임 — 핵심 필드, 생명주기, 입력, 전투, 게임 흐름.
/// 렌더링은 SrpGameController.Rendering.cs, HUD는 SrpGameController.Hud.cs 참조.
/// </summary>
public partial class SrpGameController : MonoBehaviour, ISrpBattleFlowObserver
{
    // ── Inspector ────────────────────────────────────────────────────────────

    public float cellSize = 1f;

    [Tooltip("비어 있으면 startPreset 맵을 사용합니다.")]
    public SrpMapFileV1 initialMap;

    [Tooltip("initialMap이 비어 있을 때 로드할 내장 프리셋.")]
    public SrpMapPreset startPreset = SrpMapPreset.M1OpeningPrototype;

    [Header("Camera")]
    public bool frameCameraOnStart = true;

    [Tooltip("직교 뷰에서 보드 여유(월드 단위).")]
    public float orthoViewPadding = 0.75f;

    [Header("HUD")]
    [Tooltip("왼쪽 컨트롤 패널 폭(캔버스 단위).")]
    public float leftPanelWidth = 170f;

    [Tooltip("오른쪽 로그 패널 폭(캔버스 단위).")]
    public float rightPanelWidth = 640f;

    [Tooltip("시작 시 오른쪽 로그 패널 표시 여부.")]
    public bool startWithLogVisible = false;

    [Header("Enemy AI")]
    [Tooltip("적 AI가 행동 하나를 한 뒤 다음 행동까지 쉬는 시간(초).")]
    public float enemyAiStepDelay = 0.55f;

    // ── 시뮬레이션 상태 ──────────────────────────────────────────────────────

    const float DefaultHudPanelWidth = 370f;
    SrpBattleState _state;
    readonly Stack<UndoSnapshot> _undo = new Stack<UndoSnapshot>();

    /// <summary>되감기 지점. 전투 상태에 들어 있지 않은 활성화 진행 값(남은 이동력)을 함께 보관한다.</summary>
    struct UndoSnapshot
    {
        public SrpBattleState state;
        public int remainingMove;
        /// <summary>이 행동을 한 진영. 핫시트 진영별 되감기 횟수에 쓴다.</summary>
        public int owner;
    }

    int _undoResumeRemainingMove;

    /// <summary>되감기 횟수 한도. 스냅숏(_state) 밖에 두어 되감기로 사용 횟수가 돌아가지 않는다. 판 시작 때 채운다.</summary>
    readonly SrpRewindBudget _rewindBudget = new SrpRewindBudget();

    // ── 입력·페이즈 상태 ─────────────────────────────────────────────────────

    enum Phase { Idle, UnitActive, SelectingSkillTarget, EnemyActing }

    const int PlayerOwner = 0;
    bool _enemyAiEnabled;
    int _enemyAiToken;
    bool _enemyAiLastStepInstant;

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
    int _hoverTileX = -1;
    int _hoverTileY = -1;
    SrpMovePreviewEvaluation _currentMovePreview;
    SrpTacticalCameraController _tacticalCamera;

    bool _gameOver;
    SrpVictoryResult _victoryResult = SrpVictoryResult.Undecided();

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
        else if (SrpGameSettings.HasSelectedPreset)
        {
            startPreset = SrpGameSettings.SelectedPreset;
            SrpGameSettings.HasSelectedPreset = false;
        }

        bool mapEmpty = initialMap == null
            || initialMap.placements == null
            || initialMap.placements.Length == 0;
        if (mapEmpty)
            initialMap = SrpDefaultMaps.GetPreset(startPreset);

        _state = SrpBattleState.FromMap(initialMap);
        SeedBattleRandom();
        _undo.Clear();
        _enemyAiEnabled = SrpGameSettings.EnemyAiEnabled;
        ResetRewindBudget();

        BuildGrid();
        if (frameCameraOnStart) FrameBoardCamera();
        BuildHud();
        RefreshUnitViews();
        LogLine("SRPG 프로토타입 — 속도 기반 라운드 턴 시작.");
        LogVictoryRule();
        StartRound();
        UpdateHud();
    }

    // ── 카메라 ───────────────────────────────────────────────────────────────

    public void FrameBoardCamera()
    {
        var cam = EnsureTacticalCamera();
        float w = _state.Width;
        float h = _state.Height;
        _tacticalCamera = SrpTacticalCameraController.Ensure(cam);
        _tacticalCamera.ConfigureBoard(w, h, cellSize, orthoViewPadding);
        _tacticalCamera.FocusBoard();
    }

    static Camera EnsureTacticalCamera()
    {
        var cam = Camera.main;
        if (cam != null)
            return cam;

        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.depth = -1f;
        return cam;
    }

    // ── 초기화 헬퍼 ──────────────────────────────────────────────────────────

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
                if (PerformSkill(u, _pendingSkillData, _pendingSkillRuntime, x, y))
                    return;

                _phase = Phase.UnitActive;
                _pendingSkillData = null;
                _pendingSkillRuntime = null;
                _skillTargetTiles.Clear();
                RefreshActiveHighlights(u);
                UpdateHud();
            }
            else
            {
                CancelSkillTargeting();
            }
            return;
        }

        if (_phase == Phase.Idle)
        {
            if (occ != null && !occ.eliminated && _state.CurrentUnitId == occ.id && !IsEnemyAiUnit(occ))
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
                PerformMove(u, x, y, moveCost);
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
                else if (!SrpCombatResolver.HasAmmoForBasicAttack(_state, u, occ))
                    LogLine($"공격 불가: {u.displayName} 탄약 없음. 재장전 필요");
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
        _hoverTileX = x;
        _hoverTileY = y;
        if (_phase != Phase.UnitActive || !_selectedId.HasValue)
        {
            ShowSkillAreaHover(x, y);
            UpdateHud();
            return;
        }

        ClearOverlayLayer(OverlayHover);
        ClearOverlayLayer(OverlayDangerBlocked);
        ClearOverlayLayer(OverlayAimLine);
        ClearMovePreviewVisuals();

        var u = GetUnit(_selectedId.Value);
        if (u == null)
            return;

        var cell = new Vector2Int(x, y);
        if (_moveCostMap.TryGetValue(cell, out int moveCost))
        {
            _currentMovePreview = SrpPreviewEvaluator.EvaluateMove(_state, u, x, y);
            RenderMovePreview(u, _currentMovePreview);
            int threatCount = _currentMovePreview != null ? _currentMovePreview.threats.Count : CountEnemyAttackersForTile(x, y, u.owner);
            int overwatchThreatCount = CountOverwatchThreats(_currentMovePreview);
            bool inZoc = _state.IsEnemyAdjacentToTile(x, y, u.owner);
            if (overwatchThreatCount > 0)
            {
                SetOverlayTile(OverlayHover, x, y, new Color(1.0f, 0.1f, 0.1f));
                _hoverStatusHint = $"경계사격 위험: {overwatchThreatCount}명 | 일반 위협 {threatCount}명 | 이동 비용 {moveCost}";
            }
            else if (threatCount > 0)
            {
                SetOverlayTile(OverlayHover, x, y, new Color(0.95f, 0.2f, 0.2f));
                _hoverStatusHint = $"위협 높음: 해당 칸은 {threatCount}명에게 공격 노출 | 이동 비용 {moveCost}";
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

    /// <summary>범위 스킬 대상을 고르는 중이면 마우스를 올린 칸 기준 효과 범위를 표시한다.</summary>
    void ShowSkillAreaHover(int x, int y)
    {
        if (_phase != Phase.SelectingSkillTarget || !SrpSkills.IsAreaSkill(_pendingSkillData))
            return;

        ClearOverlayLayer(OverlayHover);
        if (!_skillTargetTiles.Contains(new Vector2Int(x, y)))
            return;
        foreach (var tile in SrpSkills.GetSkillAreaTiles(_pendingSkillData, x, y, _state))
            SetOverlayTile(OverlayHover, tile.x, tile.y, new Color(1f, 0.55f, 0.2f));
    }

    public void OnTileHoverExit(int x, int y)
    {
        if (_gameOver)
            return;
        if (_hoverTileX == x && _hoverTileY == y)
        {
            _hoverTileX = -1;
            _hoverTileY = -1;
        }
        ClearOverlayLayer(OverlayHover);
        ClearOverlayLayer(OverlayDangerBlocked);
        ClearOverlayLayer(OverlayAimLine);
        ClearMovePreviewVisuals();
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
        _hoverTileX = unit.anchorX;
        _hoverTileY = unit.anchorY;
        ClearOverlayLayer(OverlayAimLine);
        RenderUnitHoverOverlays(unit);
        var active = _selectedId.HasValue ? GetUnit(_selectedId.Value) : null;
        if (_phase == Phase.UnitActive
            && active != null
            && unit.owner != active.owner
            && _attackIds.Contains(unit.id))
        {
            var attackKind = SrpCombatResolver.ResolveBasicAttackKind(_state, active, unit);
            if (attackKind == SrpBasicAttackKind.Firearm)
                HighlightFirearmAimLine(active, unit);
            bool willExecute = attackKind == SrpBasicAttackKind.Melee && (unit.pg <= 0 || unit.groggy);
            _hoverStatusHint = attackKind == SrpBasicAttackKind.Firearm
                ? $"총기 조준: {active.displayName} -> {unit.displayName} | 실제 대상 벡터"
                : willExecute
                    ? $"근접 처단 예상: {active.displayName} -> {unit.displayName}"
                    : $"근접 공격 미리보기: {active.displayName} -> {unit.displayName}";
            UpdateUnitFeedbackVisuals();
            UpdateHud();
            return;
        }
        _hoverStatusHint = $"유닛 미리보기: {unit.displayName} 공격범위/ZOC 표시";
        UpdateUnitFeedbackVisuals();
        UpdateHud();
    }

    public void OnUnitHoverExit(int unitId)
    {
        if (_hoverUnitId != unitId)
            return;
        _hoverUnitId = -1;
        _hoverTileX = -1;
        _hoverTileY = -1;
        ClearOverlayLayer(OverlayUnitHoverRange);
        ClearOverlayLayer(OverlayUnitHoverZoc);
        ClearOverlayLayer(OverlayAimLine);
        _hoverStatusHint = string.Empty;
        UpdateUnitFeedbackVisuals();
        UpdateHud();
    }

    public void OnUnitClicked(int unitId)
    {
        var unit = GetUnit(unitId);
        if (unit == null)
            return;
        OnTileClicked(unit.anchorX, unit.anchorY);
    }

    // 이동/공격/스킬/재장전의 규칙 처리는 SrpBattleFlow가 한다(시뮬레이션과 공유).
    // 여기서는 되감기 기록, 화면 갱신, 활성화 종료 처리만 한다.

    /// <summary>이동 1회를 실행한다. 이 이동으로 활성화가 끝났으면 true.</summary>
    bool PerformMove(SrpUnitRuntime u, int x, int y, int moveCost)
    {
        PushUndo();
        _remainingMove -= moveCost;
        var result = SrpBattleFlow.Move(_state, u, x, y, moveCost, this);
        return AfterMove(u, result);
    }

    bool AfterMove(SrpUnitRuntime u, SrpMoveResult result)
    {
        RefreshUnitViews();
        if (result.moverDied)
        {
            FinishActivation();
            return true;
        }
        if (TryEndActivationIfPgBroken(u))
            return true;
        RefreshAfterAction(u);
        return false;
    }

    /// <summary>대상 지정 스킬 1회를 실행한다. 이 스킬로 활성화가 끝났으면 true.</summary>
    bool PerformSkill(SrpUnitRuntime u, SrpSkillData data, SrpSkillRuntime runtime, int x, int y)
    {
        PushUndo();
        var beforeVitals = CaptureUnitVitals();
        SpawnWorldFeedback(u, $"{data.displayName}!", new Color(0.82f, 0.55f, 1f));
        var result = SrpBattleFlow.UseSkill(_state, u, data, runtime, x, y, this);
        FlashChangedUnits(beforeVitals);
        return AfterSkill(u, data, result);
    }

    bool AfterSkill(SrpUnitRuntime u, SrpSkillData data, SrpSkillUseResult result)
    {
        RefreshUnitViews();
        // 스킬로 승패가 났으면 남은 AP와 무관하게 바로 종료 흐름으로 넘어간다.
        if (result.casterDied || data.endsActivation || IsBattleDecided())
        {
            FinishActivation();
            return true;
        }
        return TryEndActivationIfPgBroken(u);
    }

    void PerformReload(SrpUnitRuntime unit)
    {
        PushUndo();
        SrpBattleFlow.Reload(unit);
        AfterReload(unit);
    }

    void AfterReload(SrpUnitRuntime unit)
    {
        SpawnWorldFeedback(unit, "재장전", new Color(0.25f, 1f, 0.65f));
        FlashUnit(unit, new Color(0.25f, 1f, 0.65f));
        LogLine($"재장전: {unit.displayName} | 탄약 {unit.ammo}/{unit.maxAmmo} | AP-1");
        RefreshAfterAction(unit);
    }

    /// <summary>경계태세 예약 직후의 연출과 활성화 종료. 예약 자체는 SrpOverwatch.Arm이 한다.</summary>
    void AfterOverwatchArm(SrpUnitRuntime unit)
    {
        SpawnWorldFeedback(unit, "경계태세 준비", new Color(0.35f, 0.55f, 1f));
        FlashUnit(unit, new Color(0.35f, 0.55f, 1f));
        LogLine($"경계태세 준비: {unit.displayName}({unit.id}) | 사거리 {unit.overwatchRange} | 행동 소모");
        RefreshUnitViews();
        FinishActivation();
    }

    /// <summary>행동 하나가 끝난 뒤 화면을 갱신한다. 적 AI 차례에는 플레이어용 이동/공격 표시를 띄우지 않는다.</summary>
    void RefreshAfterAction(SrpUnitRuntime u)
    {
        if (_phase == Phase.EnemyActing)
        {
            ResetTileColors();
            _moveCostMap.Clear();
            _attackIds.Clear();
            UpdateUnitFeedbackVisuals();
        }
        else
        {
            RefreshActiveHighlights(u);
        }
        UpdateHud();
    }

    /// <summary>행동 도중 PG가 붕괴(그로기)한 유닛은 남은 AP와 무관하게 활성화를 끝낸다.</summary>
    bool TryEndActivationIfPgBroken(SrpUnitRuntime unit)
    {
        if (unit == null || unit.eliminated || !unit.IsPgBroken)
            return false;
        LogLine($"행동 중단: {unit.displayName}({unit.id}) PG 붕괴로 남은 행동 불가");
        FinishActivation();
        return true;
    }

    bool IsBattleDecided()
    {
        return SrpVictory.Evaluate(_state).decided;
    }

    // ── 적 AI ────────────────────────────────────────────────────────────────

    bool IsEnemyAiUnit(SrpUnitRuntime unit)
    {
        return _enemyAiEnabled && unit != null && unit.owner != PlayerOwner;
    }

    void BeginEnemyAiActivation(SrpUnitRuntime unit)
    {
        // 적 차례를 넘어서 되감지 않도록 되감기 기록을 비운다.
        _undo.Clear();
        _phase = Phase.EnemyActing;
        ResetTileColors();
        _moveCostMap.Clear();
        _attackIds.Clear();
        UpdateUnitFeedbackVisuals();
        _enemyAiToken++;
        StartCoroutine(RunEnemyAiActivation(unit.id, _enemyAiToken));
    }

    IEnumerator RunEnemyAiActivation(int unitId, int token)
    {
        float delay = Mathf.Max(0f, enemyAiStepDelay);
        yield return new WaitForSeconds(delay);

        for (int step = 0; step < SrpBattleFlow.MaxAiActionsPerActivation; step++)
        {
            if (!IsEnemyAiActivationCurrent(unitId, token))
                yield break;

            if (!TryRunEnemyAiStep(unitId))
                break;
            if (!IsEnemyAiActivationCurrent(unitId, token))
                yield break;

            // 태세와 방향 변경은 다음 행동과 이어서 보여 준다(기다리지 않는다).
            if (!_enemyAiLastStepInstant)
                yield return new WaitForSeconds(delay);
        }

        if (!IsEnemyAiActivationCurrent(unitId, token))
            yield break;
        LogLine($"유닛 완료 — {GetUnit(unitId)?.displayName}({unitId})");
        FinishActivation();
    }

    /// <summary>
    /// 적 AI 행동 하나를 고르고 실행한다. 더 할 행동이 없으면 false.
    /// 판단/실행 중 예외가 나면 적 차례가 "적 행동 중"에 멈춰 플레이어가 아무것도 못 하게 되므로,
    /// 오류를 남기고 false를 돌려 그 유닛의 활성화를 끝낸다.
    /// </summary>
    bool TryRunEnemyAiStep(int unitId)
    {
        try
        {
            var unit = GetUnit(unitId);
            var action = SrpEnemyAi.DecideStep(_state, unit, _remainingMove, _hasAttackedThisTurn);
            return ExecuteEnemyAiAction(unit, action);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            LogLine($"적 AI 오류: 유닛 {unitId}의 남은 행동을 건너뜁니다. ({e.GetType().Name})");
            return false;
        }
    }

    bool IsEnemyAiActivationCurrent(int unitId, int token)
    {
        if (_gameOver || token != _enemyAiToken || _phase != Phase.EnemyActing)
            return false;
        if (!_selectedId.HasValue || _selectedId.Value != unitId)
            return false;
        var unit = GetUnit(unitId);
        return unit != null && !unit.eliminated;
    }

    /// <summary>
    /// AI가 고른 행동 하나를 실행한다. 실행할 수 없는 행동이면 false.
    /// 검증과 규칙 처리는 시뮬레이션과 같은 SrpBattleFlow.ExecuteAiAction이 한다.
    /// </summary>
    bool ExecuteEnemyAiAction(SrpUnitRuntime unit, SrpEnemyAiAction action)
    {
        var beforeVitals = action.type == SrpEnemyAiActionType.UseSkill ? CaptureUnitVitals() : null;
        var step = SrpBattleFlow.ExecuteAiAction(_state, unit, action, ref _remainingMove, this);
        if (!step.executed)
            return false;
        _enemyAiLastStepInstant = step.type == SrpEnemyAiActionType.SetStance
            || step.type == SrpEnemyAiActionType.SetFacing;

        switch (step.type)
        {
            case SrpEnemyAiActionType.Move:
                AfterMove(unit, step.move);
                break;
            case SrpEnemyAiActionType.Attack:
                AfterBasicAttack(unit, step.attack);
                break;
            case SrpEnemyAiActionType.Reload:
                AfterReload(unit);
                break;
            case SrpEnemyAiActionType.UseSkill:
            {
                var data = _state.SkillLookup[action.skillId];
                SpawnWorldFeedback(unit, $"{data.displayName}!", new Color(0.82f, 0.55f, 1f));
                FlashChangedUnits(beforeVitals);
                if (!AfterSkill(unit, data, step.skill))
                    RefreshAfterAction(unit);
                break;
            }
            case SrpEnemyAiActionType.Overwatch:
                AfterOverwatchArm(unit);
                break;
            case SrpEnemyAiActionType.TakeCover:
                AfterTakeCover(unit, step.coverX, step.coverY);
                break;
            case SrpEnemyAiActionType.Interact:
                AfterInteraction(unit, step.interaction);
                break;
            case SrpEnemyAiActionType.Overclock:
                // 로그는 SrpSkills.TryOverclockSkill이 남긴다.
                SpawnWorldFeedback(unit, "\uC624\uBC84\uD074\uB7ED", new Color(1f, 0.78f, 0.25f));
                FlashUnit(unit, new Color(1f, 0.78f, 0.25f));
                UpdateHud();
                break;
            case SrpEnemyAiActionType.SetStance:
                LogLine($"태세 변경: {unit.displayName} → {DescribeStance(unit.stance)}");
                RefreshUnitViews();
                UpdateHud();
                break;
            case SrpEnemyAiActionType.SetFacing:
                LogLine($"방향 변경: {unit.displayName} → {DescribeFacing(unit.facing)}");
                RefreshUnitViews();
                UpdateHud();
                break;
        }
        return true;
    }

    void BeginSelectUnit(SrpUnitRuntime u)
    {
        // 되감기 뒤 다시 선택한 경우는 새 활성화가 아니다. 이미 쓴 기본 공격과 이동력을 되살려야
        // 공격 → 다른 행동 → 되감기 → 재공격 같은 우회가 생기지 않는다.
        bool resumingAfterUndo = _postUndoHint;
        _postUndoHint = false;
        _selectedId = u.id;
        if (resumingAfterUndo)
        {
            if (!u.passiveAppliedThisTurn)
            {
                SrpSkills.TryApplyPassiveTurnStart(u, _state, LogLine);
                u.passiveAppliedThisTurn = true;
            }
            _remainingMove = Mathf.Clamp(_undoResumeRemainingMove, 0, u.moveRange);
            _hasAttackedThisTurn = u.hasAttackedThisActivation;
        }
        else
        {
            _remainingMove = SrpBattleFlow.BeginActivation(_state, u, LogLine);
            _hasAttackedThisTurn = false;
        }
        _phase = Phase.UnitActive;
        RefreshActiveHighlights(u);
        FlashUnit(u, new Color(1f, 0.92f, 0.35f));
        UpdateUnitFeedbackVisuals();
        UpdateHud();
    }

    void RefreshActiveHighlights(SrpUnitRuntime u)
    {
        ResetTileColors();
        _moveCostMap.Clear();
        _currentMovePreview = null;
        _actionPreviewKind = SrpActionPreviewKind.None;
        _hoverPreviewSkillData = null;
        _hoverPreviewSkillRuntime = null;
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
        RebuildDangerAndIntentOverlays();
        UpdateUnitFeedbackVisuals();
    }

    // ── 전투 ─────────────────────────────────────────────────────────────────

    void RefreshAttackTargets(SrpUnitRuntime atk)
    {
        _attackIds.Clear();
        if (atk.actionPoints <= 0 || _hasAttackedThisTurn)
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
        if (_selectedId == null || _phase == Phase.EnemyActing) return;
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
            SpawnWorldFeedback(u, $"{data.displayName}!", new Color(0.82f, 0.55f, 1f));
            var beforeVitals = CaptureUnitVitals();
            SrpSkills.ResolveActiveSkill(data, runtime, u, u.anchorX, u.anchorY, _state, LogLine);
            FlashChangedUnits(beforeVitals);
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
        SpawnWorldFeedback(u, $"{data.displayName} \uC900\uBE44", new Color(0.82f, 0.55f, 1f));
        FlashUnit(u, new Color(0.9f, 0.82f, 1f));
        UpdateUnitFeedbackVisuals();
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
        if (!CanUndo()) return;
        // 사용 횟수는 스냅숏 밖에 있으므로 아래 복원으로 되돌아가지 않는다.
        // 공유 모드(기본)에서는 진영과 무관하게 공유 횟수를 쓴다.
        _rewindBudget.TryConsumeFor(_undo.Peek().owner);
        var snapshot = _undo.Pop();
        _state = snapshot.state;
        _undoResumeRemainingMove = snapshot.remainingMove;
        _selectedId = null;
        _phase = Phase.Idle;
        _moveCostMap.Clear();
        _attackIds.Clear();
        _pendingSkillData = null;
        _pendingSkillRuntime = null;
        _skillTargetTiles.Clear();
        _hoverUnitId = -1;
        _hoverTileX = -1;
        _hoverTileY = -1;
        _hoverStatusHint = string.Empty;
        _currentMovePreview = null;
        _actionPreviewKind = SrpActionPreviewKind.None;
        _hoverPreviewSkillData = null;
        _hoverPreviewSkillRuntime = null;
        ResetTileColors();
        RefreshUnitViews();
        if (_rewindBudget.IsUnlimited)
            LogLine("— 되감기 —");
        else if (_rewindBudget.PerOwner)
            LogLine($"— 되감기 — (P{snapshot.owner} 남은 횟수 {_rewindBudget.RemainingFor(snapshot.owner)}/{_rewindBudget.Limit})");
        else
            LogLine($"— 되감기 — (남은 횟수 {_rewindBudget.Remaining}/{_rewindBudget.Limit})");
        _postUndoHint = true;
        _gameOver = false;
        _victoryResult = SrpVictoryResult.Undecided();
        UpdateHud();
    }

    /// <summary>판 시작 때 되감기 횟수를 채운다. 진영별 모드는 핫시트(적 AI 끔)에서만 켠다.</summary>
    void ResetRewindBudget()
    {
        _rewindBudget.Reset(SrpGameSettings.RewindLimit,
            SrpGameSettings.RewindPerSideInHotseat && !_enemyAiEnabled);
    }

    /// <summary>지금 행동 중인 진영. 진영별 되감기 횟수의 표시와 판정에 쓴다.</summary>
    int CurrentRewindOwner()
    {
        var current = _state != null ? GetUnit(_state.CurrentUnitId) : null;
        return current != null ? current.owner : PlayerOwner;
    }

    /// <summary>진영별 모드에서 마지막 기록이 상대 진영의 행동이면 true. 상대 행동은 되감을 수 없다.</summary>
    bool IsLastUndoOtherSide()
    {
        return _rewindBudget.PerOwner && _undo.Count > 0 && _undo.Peek().owner != CurrentRewindOwner();
    }

    /// <summary>
    /// 지금 되감기를 할 수 있는가: 되돌릴 행동이 있고, 적 AI 차례가 아니고, 그 행동을 한 진영(공유 모드는 공유)의
    /// 남은 횟수가 있다. 진영별 모드에서는 지금 행동 중인 진영의 행동만 되감는다.
    /// </summary>
    bool CanUndo()
    {
        if (_undo.Count == 0 || _phase == Phase.EnemyActing || IsLastUndoOtherSide())
            return false;
        return _rewindBudget.CanRewindFor(_undo.Peek().owner);
    }

    // ── 게임 흐름 ────────────────────────────────────────────────────────────

    void SeedBattleRandom()
    {
        _state.SeedRandom(SrpGameSettings.BattleRandomSeed ?? System.Environment.TickCount);
    }

    void PushUndo()
    {
        // 적 AI 행동은 되감기 대상이 아니다.
        if (_phase == Phase.EnemyActing)
            return;
        _undo.Push(new UndoSnapshot
        {
            state = _state.Clone(),
            remainingMove = _remainingMove,
            owner = CurrentRewindOwner(),
        });
    }

    bool TryResolveOverwatchAgainst(SrpUnitRuntime target)
    {
        return SrpBattleFlow.ResolveOverwatchAgainst(_state, target, this).targetDied;
    }

    void DoAttack(SrpUnitRuntime atk, SrpUnitRuntime def)
    {
        var attackKind = SrpCombatResolver.ResolveBasicAttackKind(_state, atk, def);
        if (!SrpCombatResolver.HasAmmoForBasicAttack(attackKind, atk))
        {
            LogLine($"공격 불가: {atk.displayName} 탄약 없음. 재장전 필요");
            UpdateHud();
            return;
        }
        PushUndo();
        var result = SrpBattleFlow.BasicAttack(_state, atk, def, this);
        if (!result.executed)
        {
            if (_phase != Phase.EnemyActing && _undo.Count > 0)
                _undo.Pop();
            LogLine($"공격 불가: {atk.displayName} 탄약 없음. 재장전 필요");
            UpdateHud();
            return;
        }
        AfterBasicAttack(atk, result);
    }

    void AfterBasicAttack(SrpUnitRuntime atk, SrpBasicAttackResult result)
    {
        _hasAttackedThisTurn = true;
        RefreshUnitViews();
        // 기본 공격은 활성화당 1회지만, AP가 남으면 이동/스킬/재장전 등을 이어서 할 수 있다.
        if (result.activationEnded || IsBattleDecided())
        {
            FinishActivation();
            return;
        }
        RefreshAfterAction(atk);
    }

    // ── SrpBattleFlow 관찰(로그/연출) ────────────────────────────────────────

    void ISrpBattleFlowObserver.OnRuleLog(string line)
    {
        LogLine(line);
    }

    void ISrpBattleFlowObserver.OnMoved(SrpUnitRuntime mover, int cost, bool disengaged)
    {
        string disengageHint = disengaged ? " (교전 이탈)" : string.Empty;
        LogLine($"이동: {mover.displayName}({mover.id}) → ({mover.anchorX},{mover.anchorY}), 잔여 이동력 {_remainingMove}{disengageHint}");
    }

    void ISrpBattleFlowObserver.OnAttackResolved(
        SrpAttackSource source,
        SrpUnitRuntime atk,
        SrpUnitRuntime def,
        SrpCombatResolver.AttackOutcome outcome)
    {
        if (outcome.damageToHp > 0 || outcome.damageToPg > 0)
            FlashUnit(def, new Color(1f, 0.15f, 0.12f));

        switch (source)
        {
            case SrpAttackSource.Opportunity:
                SpawnWorldFeedback(atk, "기회공격!", new Color(1f, 0.42f, 0.35f));
                LogLine(
                    $"기회공격: {atk.displayName}({atk.id}) → {def.displayName}({def.id}) | " +
                    $"피해 PG-{outcome.damageToPg} HP-{outcome.damageToHp} | 반응 사격 소모");
                break;
            case SrpAttackSource.Overwatch:
                SpawnWorldFeedback(atk, "경계사격!", new Color(0.35f, 0.55f, 1f));
                LogLine(
                    $"경계사격: {atk.displayName}({atk.id}) → {def.displayName}({def.id}) | " +
                    $"피해 PG-{outcome.damageToPg} HP-{outcome.damageToHp} | 반응 사격 소모 | 탄약 {atk.ammo}/{atk.maxAmmo}");
                break;
            default:
            {
                SpawnWorldFeedback(atk, "공격!", new Color(1f, 0.42f, 0.35f));
                string attackKindText = outcome.basicAttackKind == SrpBasicAttackKind.Firearm ? "총기" : "근접";
                string ammoText = outcome.basicAttackKind == SrpBasicAttackKind.Firearm && atk.UsesAmmo ? $" | 탄약 {atk.ammo}/{atk.maxAmmo}" : string.Empty;
                LogLine(
                    $"공격({attackKindText}): {atk.displayName}({atk.id}) → {def.displayName}({def.id}) | " +
                    $"피해 PG-{outcome.damageToPg} HP-{outcome.damageToHp} | " +
                    $"처단:{outcome.wasExecution} 그로기:{outcome.becameGroggy}{ammoText}");
                break;
            }
        }

        LogDefenseBuffers(def, outcome);
        LogReactionOutcome(def, outcome);
        if (outcome.becameGroggy)
            LogLine($"PG 붕괴: {def.displayName}({def.id}) 처단 위험 상태");
        if (source == SrpAttackSource.Basic && outcome.wasExecution)
            LogLine($"처단 타격: {def.displayName}({def.id})");
    }

    void ISrpBattleFlowObserver.OnUnitDied(SrpAttackSource source, SrpUnitRuntime unit)
    {
        // 경계사격은 행동 중인 유닛을 쓰러뜨리므로 선택/hover 표시를 먼저 걷어 낸다.
        if (source == SrpAttackSource.Overwatch)
        {
            if (_selectedId == unit.id)
                _selectedId = null;
            if (_hoverUnitId == unit.id)
                _hoverUnitId = -1;
            if (_hoverTileX == unit.anchorX && _hoverTileY == unit.anchorY)
            {
                _hoverTileX = -1;
                _hoverTileY = -1;
            }
            ClearOverlayLayer(OverlayHover);
            ClearOverlayLayer(OverlayDangerBlocked);
            ClearOverlayLayer(OverlayAimLine);
        }

        LogLine($"사망: {unit.displayName}({unit.id})");
        if (source == SrpAttackSource.Overwatch)
        {
            RefreshUnitViews();
            UpdateHud();
        }
    }

    void LogDefenseBuffers(SrpUnitRuntime defender, SrpCombatResolver.AttackOutcome outcome)
    {
        if (defender == null)
            return;
        if (outcome.coverBufferApplied)
            LogLine($"엄폐 완충: {defender.displayName} 원거리 사격 감쇠 HP-{outcome.reducedHpByCover} PG-{outcome.reducedPgByCover}");
        if (outcome.perfectDefenseApplied)
            LogLine($"완벽한 수비: {defender.displayName} 경미 HP 피해 무효 HP-{outcome.reducedHpByPerfectDefense}");
        if (outcome.sustainedDefenseBufferApplied)
            LogLine($"수비 완충: {defender.displayName} 후속 공격 감쇠 PG-{outcome.reducedPgBySustainedDefense}");
        if (outcome.tankMultiEngagementBufferApplied)
            LogLine($"탱커 대응: {defender.displayName} 다중 교전 감쇠 HP-{outcome.reducedHpByTank} PG-{outcome.reducedPgByTank}");
        if (outcome.combatTagBonusApplied)
            LogLine($"전투 태그 소모: {defender.displayName} {SrpCombatTagUtility.BuildSummary(outcome.consumedCombatTags)} | HP+{outcome.bonusHpFromCombatTags} PG+{outcome.bonusPgFromCombatTags}");
        if (outcome.firearmPgSpillover > 0)
            LogLine($"총기 충격: {defender.displayName} 최종 HP 피해 기반 PG 파급 +{outcome.firearmPgSpillover}");
    }

    void LogReactionOutcome(SrpUnitRuntime defender, SrpCombatResolver.AttackOutcome outcome)
    {
        if (defender == null || !outcome.reactionSpentRp)
            return;

        string unit = $"{defender.displayName}({defender.id})";
        switch (outcome.reactionKind)
        {
            case SrpReactionKind.Parry:
                string parryReward = outcome.parryAppliedBalanceBreak
                    ? $" | 반격 PG-{outcome.parryCounterDamageToPg}, 균형 붕괴"
                    : string.Empty;
                LogLine($"방어 반응: {unit} 패링 성공 | 피해 무효{parryReward}");
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
        var ended = _selectedId.HasValue ? GetUnit(_selectedId.Value) : null;
        if (ended != null)
        {
            SpawnWorldFeedback(ended, "\uD589\uB3D9 \uC885\uB8CC", new Color(0.9f, 0.9f, 0.9f));
            FlashUnit(ended, new Color(1f, 1f, 1f));
        }
        ResetTileColors();
        _selectedId = null;
        _phase = Phase.Idle;
        _moveCostMap.Clear();
        _attackIds.Clear();
        _pendingSkillData = null;
        _pendingSkillRuntime = null;
        _skillTargetTiles.Clear();
        UpdateUnitFeedbackVisuals();
        AdvanceToNextActivation();
    }

    void StartRound()
    {
        SrpBattleFlow.StartRound(_state);
        LogLine($"라운드 {_state.RoundNumber} 시작 (유닛 {_state.RoundQueue.Count}명)");
        AdvanceToNextActivation();
    }

    void AdvanceToNextActivation()
    {
        // 반응 사격 등으로 이미 승패가 난 채 들어와도 종료 상태를 화면에 반영한다.
        CheckWin();
        if (_gameOver)
        {
            UpdateHud();
            return;
        }

        if (!SrpTurnOrder.HasRemainingUnitInRound(_state))
        {
            // 라운드가 끝나는 순간 성립하는 조건(버티기, 거점 유지)을 판정한 뒤 다음 라운드로 넘어간다.
            SrpBattleFlow.EndRound(_state);
            CheckWin();
            if (_gameOver)
            {
                UpdateHud();
                return;
            }
            string progress = SrpVictory.DescribeProgress(_state);
            if (!string.IsNullOrEmpty(progress))
                LogLine($"승리 조건 진행: {progress}");
            _state.RoundNumber++;
            StartRound();
            return;
        }

        int nextId = SrpTurnOrder.AdvanceToNextUnit(_state);
        var next = GetUnit(nextId);
        // 진영별 되감기: 상대 진영의 활성화가 시작되면(행동 없이 끝나거나 그로기 회복으로 넘어가도) 그 전 기록을 버린다.
        if (next != null && _undo.Count > 0 && _rewindBudget.CutsHistoryAt(_undo.Peek().owner, next.owner))
            _undo.Clear();
        if (next == null || next.eliminated)
        {
            AdvanceToNextActivation();
            return;
        }

        if (SrpTurnOrder.TryConsumeActivationForGroggyRecovery(next, out int recoveredPg))
        {
            SpawnWorldFeedback(next, "그로기 회복", new Color(0.72f, 0.86f, 1f));
            LogLine($"그로기 회복: {next.displayName}({next.id}) 이번 행동 불가 | PG +{recoveredPg} → {next.pg}/{next.maxPg} | 다음 행동까지 재붕괴 보호");
            RefreshUnitViews();
            AdvanceToNextActivation();
            return;
        }

        BeginSelectUnit(next);
        SpawnWorldFeedback(next, "\uD134 \uC2DC\uC791", new Color(1f, 0.92f, 0.35f));
        LogLine($"행동 시작: {next.displayName}({next.id}) [SPD {next.speed}]");
        CheckWin();
        if (!_gameOver && IsEnemyAiUnit(next))
            BeginEnemyAiActivation(next);
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
        ClearUnitFeedbackObjects();

        _undo.Clear();
        ResetRewindBudget();
        _log.Clear();
        _selectedId = null;
        _phase = Phase.Idle;
        _gameOver = false;
        _victoryResult = SrpVictoryResult.Undecided();
        _enemyAiToken++;

        _state = SrpBattleState.FromMap(map);
        SeedBattleRandom();
        BuildGrid();
        if (frameCameraOnStart) FrameBoardCamera();
        RefreshUnitViews();
        StartRound();
        LogLine("맵 적용: " + map.name);
        LogVictoryRule();
        UpdateHud();
    }

    /// <summary>승패를 판정한다(SrpVictory). 결판이 났으면 _gameOver를 세우고 결과를 한 번만 기록한다.</summary>
    void CheckWin()
    {
        if (_gameOver)
            return;
        var result = SrpVictory.Evaluate(_state);
        if (!result.decided)
            return;

        _gameOver = true;
        _victoryResult = result;
        if (result.winnerOwner >= 0)
            LogLine($"게임 종료: 플레이어 {result.winnerOwner} 승리. ({SrpVictory.DescribeResult(_state, result)})");
        else
            LogLine("게임 종료: 무승부(전멸).");
    }

    void LogVictoryRule()
    {
        LogLine($"승리 조건: {SrpVictory.DescribeRule(_state)}");
        string defeatRule = SrpVictory.DescribeDefeatRule(_state);
        if (!string.IsNullOrEmpty(defeatRule))
            LogLine($"패배 조건: {defeatRule}");
        var rule = _state.VictoryRule;
        if (rule == null || !rule.commanderKill)
            return;
        foreach (var unit in _state.Units)
        {
            if (unit != null && unit.isCommander && !unit.eliminated)
                LogLine($"지휘관: P{unit.owner} {unit.displayName}({unit.id})");
        }
    }

    string BuildGameOverText()
    {
        if (!_victoryResult.decided)
            return "게임 종료";
        return _victoryResult.winnerOwner >= 0
            ? $"게임 종료 | 플레이어 {_victoryResult.winnerOwner} 승리 ({SrpVictory.DescribeResult(_state, _victoryResult)})"
            : "게임 종료 | 무승부";
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

    bool CanSetSelectedStance(SrpUnitRuntime unit)
    {
        // 조건은 적 AI와 같이 쓰는 SrpBattleFlow.CanChangeStance가 정한다(활성화의 첫 행동 전까지).
        return !_gameOver
            && _phase == Phase.UnitActive
            && SrpBattleFlow.CanChangeStance(unit);
    }

    bool TrySetSelectedStance(SrpStance stance, bool logFailure)
    {
        if (!_selectedId.HasValue)
            return false;
        var unit = GetUnit(_selectedId.Value);
        if (!CanSetSelectedStance(unit))
        {
            if (logFailure && unit != null)
                LogLine($"태세 변경 불가: {unit.displayName} | 행동 전까지만 변경 가능");
            UpdateHud();
            return false;
        }
        if (unit.stance == stance)
            return true;

        PushUndo();
        unit.stance = stance;
        LogLine($"태세 변경: {unit.displayName} → {DescribeStance(stance)}");
        RefreshActiveHighlights(unit);
        RefreshUnitViews();
        UpdateHud();
        return true;
    }

    bool TrySetSelectedFacing(SrpFacing facing, bool logFailure)
    {
        if (!_selectedId.HasValue)
            return false;
        var unit = GetUnit(_selectedId.Value);
        if (_gameOver || _phase != Phase.UnitActive || unit == null || unit.eliminated)
        {
            if (logFailure)
                LogLine("방향 변경 불가: 현재 행동 유닛이 없습니다.");
            UpdateHud();
            return false;
        }
        if (unit.facing == facing)
            return true;

        PushUndo();
        unit.facing = facing;
        LogLine($"방향 변경: {unit.displayName} → {DescribeFacing(facing)}");
        RefreshActiveHighlights(unit);
        RefreshUnitViews();
        UpdateHud();
        return true;
    }

    bool CanReloadSelectedUnit(SrpUnitRuntime unit)
    {
        return !_gameOver
            && _phase == Phase.UnitActive
            && unit != null
            && !unit.eliminated
            && unit.actionPoints > 0
            && unit.CanReload();
    }

    bool TryReloadSelectedUnit(bool logFailure)
    {
        if (!_selectedId.HasValue)
            return false;
        var unit = GetUnit(_selectedId.Value);
        if (!CanReloadSelectedUnit(unit))
        {
            if (logFailure && unit != null)
            {
                string reason = !unit.UsesAmmo
                    ? "재장전 가능한 총기가 없음"
                    : unit.actionPoints <= 0
                        ? "AP 부족"
                        : "탄약이 이미 가득 참";
                LogLine($"재장전 불가: {unit.displayName} | {reason}");
            }
            UpdateHud();
            return false;
        }

        PerformReload(unit);
        return true;
    }

    bool CanTakeCoverSelectedUnit(SrpUnitRuntime unit)
    {
        return !_gameOver
            && _phase == Phase.UnitActive
            && SrpBattleFlow.CanTakeCover(_state, unit);
    }

    bool TryTakeCoverSelectedUnit(bool logFailure)
    {
        if (!_selectedId.HasValue)
            return false;
        var unit = GetUnit(_selectedId.Value);
        if (!CanTakeCoverSelectedUnit(unit))
        {
            if (logFailure && unit != null)
            {
                string reason = unit.coverActive
                    ? "이미 엄폐 중"
                    : unit.actionPoints <= 0
                        ? "AP 부족"
                        : "인접 엄폐물 없음";
                LogLine($"엄폐 불가: {unit.displayName} | {reason}");
            }
            UpdateHud();
            return false;
        }

        PushUndo();
        if (!SrpBattleFlow.TakeCover(_state, unit, out int coverX, out int coverY))
        {
            _undo.Pop();
            UpdateHud();
            return false;
        }

        AfterTakeCover(unit, coverX, coverY);
        return true;
    }

    /// <summary>엄폐 직후의 연출과 화면 갱신. 엄폐 자체는 SrpBattleFlow.TakeCover가 한다.</summary>
    void AfterTakeCover(SrpUnitRuntime unit, int coverX, int coverY)
    {
        SpawnWorldFeedback(unit, "\uC5C4\uD3D0", new Color(0.55f, 1f, 0.35f));
        FlashUnit(unit, new Color(0.55f, 1f, 0.35f));
        LogLine($"엄폐: {unit.displayName} | 엄폐물 ({coverX},{coverY}) | AP-1");
        RefreshAfterAction(unit);
    }

    bool CanInteractSelectedUnit(SrpUnitRuntime unit)
    {
        return !_gameOver
            && _phase == Phase.UnitActive
            && unit != null
            && !unit.eliminated
            && unit.actionPoints > 0
            && _state != null
            && _state.TryGetAdjacentInteraction(unit, out _);
    }

    bool TryInteractSelectedUnit(bool logFailure)
    {
        if (!_selectedId.HasValue)
            return false;
        var unit = GetUnit(_selectedId.Value);
        if (!CanInteractSelectedUnit(unit))
        {
            if (logFailure && unit != null)
            {
                string reason = unit.actionPoints <= 0
                    ? "AP 부족"
                    : "인접 활성화 가능 지점 없음";
                LogLine($"상호작용 불가: {unit.displayName} | {reason}");
            }
            UpdateHud();
            return false;
        }

        PushUndo();
        if (!_state.TryResolveInteractionAction(unit, out var point))
        {
            _undo.Pop();
            UpdateHud();
            return false;
        }
        AfterInteraction(unit, point);
        return true;
    }

    /// <summary>상호작용 직후의 연출과 승패 확인. 활성화 자체는 SrpBattleState.TryResolveInteractionAction이 한다.</summary>
    void AfterInteraction(SrpUnitRuntime unit, SrpInteractionPointData point)
    {
        string label = string.IsNullOrEmpty(point.displayName) ? point.id : point.displayName;
        SpawnWorldFeedback(unit, "\uC0C1\uD638\uC791\uC6A9", new Color(1f, 0.86f, 0.25f));
        FlashUnit(unit, new Color(1f, 0.86f, 0.25f));
        LogLine($"상호작용: {unit.displayName} → {label}({point.x},{point.y}) | AP-1");
        if (point.effect == SrpInteractionEffect.VictoryObjective)
            LogLine($"승리 목표 확보: P{unit.owner} → {label}");
        // 목표 확보로 승패가 났으면 활성화를 끝내고 종료 상태로 넘어간다.
        if (IsBattleDecided())
        {
            FinishActivation();
            return;
        }
        RefreshAfterAction(unit);
    }

    bool TryOverclockSelectedSkill(bool logFailure)
    {
        if (!_selectedId.HasValue)
            return false;
        var unit = GetUnit(_selectedId.Value);
        if (_gameOver || _phase != Phase.UnitActive || unit == null || unit.eliminated)
        {
            if (logFailure)
                LogLine("오버클럭 불가: 현재 행동 유닛이 없습니다.");
            UpdateHud();
            return false;
        }
        if (!FindFirstOverclockableSkill(unit, out var data, out var runtime))
        {
            if (logFailure)
                LogLine($"오버클럭 불가: {unit.displayName} | 회복 가능한 쿨다운/충전 없음");
            UpdateHud();
            return false;
        }

        PushUndo();
        bool applied = SrpSkills.TryOverclockSkill(unit, data, runtime, LogLine);
        if (!applied)
        {
            _undo.Pop();
            UpdateHud();
            return false;
        }

        SpawnWorldFeedback(unit, "\uC624\uBC84\uD074\uB7ED", new Color(1f, 0.78f, 0.25f));
        FlashUnit(unit, new Color(1f, 0.78f, 0.25f));
        RefreshActiveHighlights(unit);
        UpdateHud();
        return true;
    }

    bool FindFirstOverclockableSkill(SrpUnitRuntime unit, out SrpSkillData data, out SrpSkillRuntime runtime)
    {
        data = null;
        runtime = null;
        if (unit == null || _state == null)
            return false;

        foreach (var sr in unit.skillRuntimes)
        {
            if (sr == null || !_state.SkillLookup.TryGetValue(sr.skillId, out var skill))
                continue;
            if (!SrpSkills.CanOverclockSkill(unit, skill, sr))
                continue;
            data = skill;
            runtime = sr;
            return true;
        }
        return false;
    }

    static string DescribeStance(SrpStance stance)
    {
        return stance == SrpStance.Defensive ? "수비" : "공격";
    }

    static string DescribeFacing(SrpFacing facing)
    {
        switch (facing)
        {
            case SrpFacing.North:
                return "북";
            case SrpFacing.East:
                return "동";
            case SrpFacing.South:
                return "남";
            case SrpFacing.West:
                return "서";
            default:
                return "?";
        }
    }

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

    static int CountOverwatchThreats(SrpMovePreviewEvaluation preview)
    {
        if (preview == null)
            return 0;
        int count = 0;
        foreach (var threat in preview.threats)
            if (threat != null && threat.isOverwatch)
                count++;
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
                    SetOverlayTile(OverlayUnitHoverRange, x, y, new Color(0.42f, 0.48f, 0.70f));
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
                        SetOverlayTile(OverlayDangerAttack, x, y, new Color(0.64f, 0.18f, 0.18f));
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
    public int TestCurrentMovePreviewThreatCount => _currentMovePreview != null ? _currentMovePreview.threats.Count : 0;
    public int TestCurrentMovePreviewOverwatchThreatCount => CountOverwatchThreats(_currentMovePreview);
    public bool TestCurrentMovePreviewHasCover => _currentMovePreview != null && _currentMovePreview.hasCover;

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
            if (TestIsInteractionPointTile(kv.Key.x, kv.Key.y))
                continue;
            OnTileHoverEnter(kv.Key.x, kv.Key.y);
            return true;
        }
        return false;
    }

    public bool TestTryHoverFirstThreatenedMoveTile()
    {
        foreach (var kv in _moveCostMap)
        {
            var unit = _selectedId.HasValue ? GetUnit(_selectedId.Value) : null;
            var preview = SrpPreviewEvaluator.EvaluateMove(_state, unit, kv.Key.x, kv.Key.y);
            if (preview == null || preview.threats.Count == 0)
                continue;
            OnTileHoverEnter(kv.Key.x, kv.Key.y);
            return true;
        }
        return false;
    }

    bool TestIsInteractionPointTile(int x, int y)
    {
        if (_state == null || _state.InteractionPoints == null)
            return false;
        foreach (var point in _state.InteractionPoints)
        {
            if (point != null && point.x == x && point.y == y)
                return true;
        }
        return false;
    }

    public bool TestTryHoverFirstAttackTarget()
    {
        foreach (int id in _attackIds)
        {
            var unit = GetUnit(id);
            if (unit == null)
                continue;
            OnUnitHoverEnter(unit.id);
            return true;
        }
        return false;
    }

    public bool TestTryHoverFirstAttackTargetOfKind(SrpBasicAttackKind kind)
    {
        foreach (int id in _attackIds)
        {
            var unit = GetUnit(id);
            var active = _selectedId.HasValue ? GetUnit(_selectedId.Value) : null;
            if (unit == null || active == null)
                continue;
            if (SrpCombatResolver.ResolveBasicAttackKind(_state, active, unit) != kind)
                continue;
            OnUnitHoverEnter(unit.id);
            return true;
        }
        return false;
    }

    public bool TestSetFirstAttackTargetGuardState(SrpBasicAttackKind kind, int pg, bool groggy)
    {
        foreach (int id in _attackIds)
        {
            var unit = GetUnit(id);
            var active = _selectedId.HasValue ? GetUnit(_selectedId.Value) : null;
            if (unit == null || active == null)
                continue;
            if (SrpCombatResolver.ResolveBasicAttackKind(_state, active, unit) != kind)
                continue;
            unit.pg = Mathf.Max(0, pg);
            unit.groggy = groggy;
            UpdateHud();
            return true;
        }
        return false;
    }

    public bool TestClickFirstAttackTargetOfKind(SrpBasicAttackKind kind)
    {
        foreach (int id in _attackIds)
        {
            var unit = GetUnit(id);
            var active = _selectedId.HasValue ? GetUnit(_selectedId.Value) : null;
            if (unit == null || active == null)
                continue;
            if (SrpCombatResolver.ResolveBasicAttackKind(_state, active, unit) != kind)
                continue;
            OnTileClicked(unit.anchorX, unit.anchorY);
            return true;
        }
        return false;
    }

    public bool TestForceLethalOverwatchAgainstCurrentUnit()
    {
        if (_state == null || _state.CurrentUnitId <= 0)
            return false;

        var target = GetUnit(_state.CurrentUnitId);
        if (target == null || target.eliminated)
            return false;

        SrpUnitRuntime watcher = null;
        foreach (var candidate in _state.Units)
        {
            if (candidate != null && !candidate.eliminated && candidate.owner != target.owner)
            {
                watcher = candidate;
                break;
            }
        }
        if (watcher == null || !_state.InBounds(0, 1) || !_state.InBounds(2, 1))
            return false;

        int targetId = target.id;
        target.anchorX = 2;
        target.anchorY = 1;
        target.hp = 1;
        target.pg = 0;
        target.groggy = true;
        target.reactionPoints = 0;
        target.eliminated = false;

        watcher.anchorX = 0;
        watcher.anchorY = 1;
        watcher.weaponClass = SrpWeaponClass.Firearm;
        watcher.attackRange = 4;
        watcher.attackPower = Mathf.Max(watcher.attackPower, 8);
        watcher.maxAmmo = Mathf.Max(watcher.maxAmmo, 1);
        watcher.ammo = watcher.maxAmmo;
        watcher.maxReactionPoints = Mathf.Max(watcher.maxReactionPoints, 1);
        watcher.reactionPoints = watcher.maxReactionPoints;
        watcher.overwatchArmed = true;
        watcher.overwatchRange = watcher.attackRange;
        watcher.overwatchRound = _state.RoundNumber;

        _selectedId = target.id;
        _state.CurrentUnitId = target.id;
        _phase = Phase.UnitActive;
        _state.RebuildEngagements();
        RefreshUnitViews();
        UpdateHud();

        bool died = TryResolveOverwatchAgainst(target);
        if (died)
            FinishActivation();

        bool viewRemoved = !_unitObjs.TryGetValue(targetId, out var go) || go == null || !go.activeInHierarchy;
        bool hudCleared = !TestTurnOrderTrackerText.Contains($"({targetId})");
        return died && target.eliminated && viewRemoved && hudCleared;
    }

    public bool TestTryHoverFirstInteractionPoint()
    {
        if (_state == null || _state.InteractionPoints == null)
            return false;
        foreach (var point in _state.InteractionPoints)
        {
            if (point == null)
                continue;
            OnTileHoverEnter(point.x, point.y);
            return true;
        }
        return false;
    }
#endif
}

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
    public SrpMapPreset startPreset = SrpMapPreset.Skirmish;

    [Header("Camera")]
    public bool frameCameraOnStart = true;

    [Tooltip("직교 뷰에서 보드 여유(월드 단위).")]
    public float orthoViewPadding = 0.75f;

    [Header("HUD")]
    [Tooltip("왼쪽 컨트롤 패널 폭(캔버스 단위).")]
    public float leftPanelWidth = 420f;

    [Tooltip("오른쪽 로그 패널 폭(캔버스 단위).")]
    public float rightPanelWidth = 420f;

    [Tooltip("시작 시 오른쪽 로그 패널 표시 여부.")]
    public bool startWithLogVisible = true;

    // ── 시뮬레이션 상태 ──────────────────────────────────────────────────────

    SrpBattleState _state;
    readonly Stack<SrpBattleState> _undo = new Stack<SrpBattleState>();

    // ── 입력·페이즈 상태 ─────────────────────────────────────────────────────

    enum Phase { Idle, UnitActive }

    Phase _phase;
    int? _selectedId;
    int _remainingMove;
    bool _hasAttackedThisTurn;
    readonly Dictionary<Vector2Int, int> _moveCostMap = new Dictionary<Vector2Int, int>();
    readonly List<int> _attackIds = new List<int>();
    readonly HashSet<int> _actedUnitsThisTurn = new HashSet<int>();

    bool _gameOver;

    // ── 생명주기 ─────────────────────────────────────────────────────────────

    void Awake()
    {
        EnsureEventSystem();

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
        LogLine("SRPG 프로토타입 — 아군 유닛을 클릭해 이동/공격하세요.");
        ResetPassivesForCurrentPlayer();
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

    void ResetPassivesForCurrentPlayer()
    {
        int pid = _state.GetCurrentPlayerId();
        foreach (var u in _state.Units)
            if (u.owner == pid) u.passiveAppliedThisTurn = false;
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

    public void OnTileClicked(int x, int y)
    {
        if (_gameOver) return;

        var occ = _state.GetOccupant(x, y);
        int pid = _state.GetCurrentPlayerId();

        if (_phase == Phase.Idle)
        {
            if (occ != null && !occ.eliminated && occ.owner == pid)
                BeginSelectUnit(occ);
            return;
        }

        if (_phase == Phase.UnitActive && _selectedId.HasValue)
        {
            var u = GetUnit(_selectedId.Value);
            if (u == null) return;

            // 다른 아군 클릭 시 현재 유닛 완료 후 전환 불가 — 먼저 완료 버튼 필요
            if (occ != null && !occ.eliminated && occ.owner == pid && occ.id != u.id)
            {
                LogLine($"{u.displayName} 행동을 먼저 완료하세요.");
                return;
            }

            // 이동 타일
            var cell = new Vector2Int(x, y);
            if (_moveCostMap.TryGetValue(cell, out int moveCost))
            {
                PushUndo();
                u.anchorX = x;
                u.anchorY = y;
                _remainingMove -= moveCost;
                LogLine($"이동: {u.displayName}({u.id}) → ({x},{y}), 잔여 이동력 {_remainingMove}");
                RefreshUnitViews();
                RefreshActiveHighlights(u);
                UpdateHud();
                return;
            }

            // 공격 타일
            if (!_hasAttackedThisTurn && occ != null && !occ.eliminated
                && occ.owner != u.owner && _attackIds.Contains(occ.id))
            {
                DoAttack(u, occ);
            }
        }
    }

    void BeginSelectUnit(SrpUnitRuntime u)
    {
        if (_actedUnitsThisTurn.Contains(u.id))
        {
            LogLine($"{u.displayName}({u.id}) 은(는) 이번 턴에 이미 행동 완료.");
            return;
        }
        if (!u.passiveAppliedThisTurn)
        {
            SrpSkills.TryApplyPassiveTurnStart(u, _state, LogLine);
            u.passiveAppliedThisTurn = true;
        }
        _selectedId = u.id;
        _remainingMove = u.moveRange;
        _hasAttackedThisTurn = false;
        _phase = Phase.UnitActive;
        RefreshActiveHighlights(u);
        UpdateHud();
    }

    void RefreshActiveHighlights(SrpUnitRuntime u)
    {
        ResetTileColors();
        _moveCostMap.Clear();
        if (_remainingMove > 0)
        {
            var costs = SrpPathfinder.GetReachableWithCosts(_state, u, _remainingMove);
            foreach (var kv in costs)
            {
                _moveCostMap[kv.Key] = kv.Value;
                TintTile(kv.Key.x, kv.Key.y, new Color(0.3f, 0.9f, 0.4f));
            }
        }
        RefreshAttackTargets(u);
        if (!_hasAttackedThisTurn) HighlightAttackTiles();
    }

    // ── 전투 ─────────────────────────────────────────────────────────────────

    void RefreshAttackTargets(SrpUnitRuntime atk)
    {
        _attackIds.Clear();
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
        if (_gameOver || _phase != Phase.UnitActive) return;
        LogLine($"유닛 완료 — {GetUnit(_selectedId.Value)?.displayName}({_selectedId})");
        FinishActivation();
    }

    void OnEndTurnSoft()
    {
        if (_gameOver || _phase != Phase.Idle) return;
        LogLine($"플레이어 {_state.GetCurrentPlayerId()} 턴 종료");
        AdvancePlayerTurn();
    }

    void OnUndo()
    {
        if (_undo.Count == 0) return;
        _state = _undo.Pop();
        _selectedId = null;
        _phase = Phase.Idle;
        _moveCostMap.Clear();
        _attackIds.Clear();
        _actedUnitsThisTurn.Clear();
        ResetTileColors();
        RefreshUnitViews();
        LogLine("— 되감기 —");
        _gameOver = false;
        UpdateHud();
    }

    // ── 게임 흐름 ────────────────────────────────────────────────────────────

    void PushUndo() => _undo.Push(_state.Clone());

    void DoAttack(SrpUnitRuntime atk, SrpUnitRuntime def)
    {
        PushUndo();
        var outcome = SrpCombatResolver.ApplyAttack(atk, def);
        SrpSkills.OnAttackResolved(atk, def, outcome, LogLine);
        atk.hasAttackedThisActivation = true;
        _hasAttackedThisTurn = true;
        LogLine(
            $"공격: {atk.displayName}({atk.id}) → {def.displayName}({def.id}) | " +
            $"AP-{outcome.damageToAp} HP-{outcome.damageToHp} " +
            $"처단:{outcome.wasExecution} 그로기:{outcome.becameGroggy}");
        if (outcome.defenderDied)
        {
            _state.RemoveUnit(def);
            LogLine($"사망: {def.displayName}({def.id})");
        }
        RefreshUnitViews();
        FinishActivation();
    }

    void FinishActivation()
    {
        if (_selectedId.HasValue)
            _actedUnitsThisTurn.Add(_selectedId.Value);
        ResetTileColors();
        _selectedId = null;
        _phase = Phase.Idle;
        _moveCostMap.Clear();
        _attackIds.Clear();
        UpdateHud();
    }

    void AdvancePlayerTurn()
    {
        _actedUnitsThisTurn.Clear();
        _state.AdvanceToNextLivingPlayer();
        ResetPassivesForCurrentPlayer();
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
        _actedUnitsThisTurn.Clear();

        _state = SrpBattleState.FromMap(map);
        BuildGrid();
        if (frameCameraOnStart) FrameBoardCamera();
        RefreshUnitViews();
        ResetPassivesForCurrentPlayer();
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
        foreach (var u in _state.Units)
            if (u.id == id) return u;
        return null;
    }
}

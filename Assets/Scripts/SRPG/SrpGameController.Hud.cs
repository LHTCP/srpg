using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SrpGameController — HUD 생성(좌/우 사이드 패널), 로그, 상태 갱신.
/// </summary>
public partial class SrpGameController
{
    // ── HUD 전용 필드 ────────────────────────────────────────────────────────

    const int MaxLogLines = 80;
    readonly List<string> _log = new List<string>();

    Text _txtTurn;
    Text _txtStatus;
    Text _txtUnit;
    Text _txtLog;
    Button _btnSkipAttack;
    Button _btnEndTurn;
    Button _btnUndo;
    Button _btnLobby;
    Button _btnToggleLog;
    Text _txtLogToggleLabel;
    GameObject _logBody;
    bool _logVisible = true;

    // ── HUD 생성 ─────────────────────────────────────────────────────────────

    void BuildHud()
    {
        var canvasGo = new GameObject("SrpCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        BuildLeftPanel(canvasGo.transform);
        BuildRightPanel(canvasGo.transform);
        _logVisible = startWithLogVisible;
        ApplyLogVisibility();
    }

    void BuildLeftPanel(Transform canvasRoot)
    {
        var panel = new GameObject("LeftPanel", typeof(RectTransform));
        panel.transform.SetParent(canvasRoot, false);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(leftPanelWidth, 0f);
        panel.AddComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.86f);

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 14, 14);
        vlg.spacing = 8f;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        _txtTurn = MakeLabel(panel.transform, "TurnInfo", 28, new Color(1f, 0.9f, 0.5f), 40);
        MakeSeparator(panel.transform);
        _txtStatus = MakeLabel(panel.transform, "Status", 22, new Color(0.75f, 0.9f, 1f), 64);
        MakeSeparator(panel.transform);
        _txtUnit = MakeLabel(panel.transform, "UnitInfo", 20, Color.white, 120);
        MakeSeparator(panel.transform);
        _btnSkipAttack = MakeButton(panel.transform, "유닛 완료", OnSkipAttack, 60);
        _btnEndTurn = MakeButton(panel.transform, "플레이어 턴 종료", OnEndTurnSoft, 60);
        _btnUndo = MakeButton(panel.transform, "되감기", OnUndo, 60);
        MakeSeparator(panel.transform);
        _btnLobby = MakeButton(panel.transform, "◀ 로비로 돌아가기",
            SrpGameSettings.ReturnToLobby, 52, 22);
    }

    void BuildRightPanel(Transform canvasRoot)
    {
        var panel = new GameObject("RightPanel", typeof(RectTransform));
        panel.transform.SetParent(canvasRoot, false);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(rightPanelWidth, 0f);
        panel.AddComponent<Image>().color = new Color(0.06f, 0.07f, 0.10f, 0.82f);

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(8, 8, 10, 10);
        vlg.spacing = 6f;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        _btnToggleLog = MakeButton(panel.transform, "로그 숨기기", OnToggleLog, 52, 22);
        _txtLogToggleLabel = _btnToggleLog.GetComponentInChildren<Text>();

        _logBody = new GameObject("LogBody", typeof(RectTransform));
        _logBody.transform.SetParent(panel.transform, false);
        _logBody.AddComponent<LayoutElement>().flexibleHeight = 1f;
        _logBody.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.07f, 0.4f);

        var logTextGo = new GameObject("LogText", typeof(RectTransform));
        logTextGo.transform.SetParent(_logBody.transform, false);
        var ltr = logTextGo.GetComponent<RectTransform>();
        ltr.anchorMin = Vector2.zero;
        ltr.anchorMax = Vector2.one;
        ltr.offsetMin = new Vector2(8, 8);
        ltr.offsetMax = new Vector2(-8, -8);
        _txtLog = logTextGo.AddComponent<Text>();
        SafeFont(_txtLog);
        _txtLog.fontSize = 20;
        _txtLog.color = new Color(0.88f, 0.92f, 0.95f);
        _txtLog.alignment = TextAnchor.UpperLeft;
        _txtLog.horizontalOverflow = HorizontalWrapMode.Wrap;
        _txtLog.verticalOverflow = VerticalWrapMode.Truncate;
        _txtLog.supportRichText = false;
    }

    // ── HUD 헬퍼 ─────────────────────────────────────────────────────────────

    static Text MakeLabel(Transform parent, string name, int fontSize, Color color, float minH)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().minHeight = minH;
        var t = go.AddComponent<Text>();
        SafeFont(t);
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = TextAnchor.UpperLeft;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    static Button MakeButton(Transform parent, string label,
        UnityEngine.Events.UnityAction onClick, float height, int fontSize = 24)
    {
        var go = new GameObject("Btn_" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
        le.flexibleWidth = 1;
        go.AddComponent<Image>().color = new Color(0.22f, 0.33f, 0.5f, 0.9f);
        var b = go.AddComponent<Button>();
        b.onClick.AddListener(onClick);

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var tx = textGo.AddComponent<Text>();
        SafeFont(tx);
        tx.fontSize = fontSize;
        tx.color = Color.white;
        tx.alignment = TextAnchor.MiddleCenter;
        tx.text = label;
        return b;
    }

    static void MakeSeparator(Transform parent)
    {
        var go = new GameObject("Sep", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().minHeight = 2;
        go.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.14f);
    }

    static void SafeFont(Text t)
    {
        if (t == null) return;
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.font = f;
    }

    void OnToggleLog()
    {
        _logVisible = !_logVisible;
        ApplyLogVisibility();
    }

    void ApplyLogVisibility()
    {
        if (_logBody != null) _logBody.SetActive(_logVisible);
        if (_txtLogToggleLabel != null)
            _txtLogToggleLabel.text = _logVisible ? "로그 숨기기" : "로그 보기";
    }

    // ── 로그 · HUD 갱신 ──────────────────────────────────────────────────────

    void LogLine(string msg)
    {
        _log.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
        while (_log.Count > MaxLogLines) _log.RemoveAt(0);

        if (_txtLog != null)
        {
            var sb = new StringBuilder();
            for (int i = Mathf.Max(0, _log.Count - 22); i < _log.Count; i++)
                sb.AppendLine(_log[i]);
            _txtLog.text = sb.ToString();
        }
        Debug.Log("[SRPG] " + msg);
    }

    void UpdateHud()
    {
        if (_txtTurn == null) return;
        int pid = _state.GetCurrentPlayerId();
        string mapName = initialMap != null ? initialMap.name : "?";

        _txtTurn.text = _gameOver
            ? "게임 종료"
            : $"플레이어 {pid} 차례\n맵: {mapName}";

        string st;
        if (_phase == Phase.Idle)
        {
            st = "아군 유닛을 클릭해 선택";
        }
        else
        {
            string moveInfo = _remainingMove > 0 ? $"이동력: {_remainingMove}" : "이동 불가";
            string atkInfo = _hasAttackedThisTurn ? "공격 완료" : "공격 가능";
            st = $"{moveInfo} | {atkInfo}\n이동(녹) 또는 공격(적) 선택";
        }
        _txtStatus.text = st;

        if (_selectedId.HasValue)
        {
            var u = GetUnit(_selectedId.Value);
            if (u != null)
                _txtUnit.text =
                    $"{u.displayName} (P{u.owner})\n" +
                    $"HP {u.hp}/{u.maxHp}  AP {u.ap}/{u.maxAp}\n" +
                    $"PG {u.posture}/{u.maxPosture}\n" +
                    $"그로기:{u.groggy}  FH:{u.frozenHeart}";
        }
        else
            _txtUnit.text = "— 미선택 —";

        if (_btnSkipAttack != null)
            _btnSkipAttack.interactable = !_gameOver && _phase == Phase.UnitActive;
        if (_btnEndTurn != null)
            _btnEndTurn.interactable = !_gameOver && _phase == Phase.Idle;
        if (_btnUndo != null)
            _btnUndo.interactable = _undo.Count > 0 && !_gameOver;
    }
}

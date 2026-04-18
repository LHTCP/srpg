using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// SrpGameController — HUD 생성(좌/우 사이드 패널), 로그, 상태 갱신.
/// </summary>
public partial class SrpGameController
{
    // ── HUD 전용 필드 ────────────────────────────────────────────────────────

    const int MaxLogLines = 80;
    const int QueuePreviewCount = 5;
    readonly List<string> _log = new List<string>();

    TextMeshProUGUI _txtTurn;
    TextMeshProUGUI _txtStatus;
    TextMeshProUGUI _txtUnit;
    TextMeshProUGUI _txtLog;
    Button _btnSkipAttack;
    Button _btnEndTurn;
    Button _btnUndo;
    Button _btnLobby;
    Button _btnToggleLog;
    Button _btnUseSkill;
    Button _btnCancelSkill;
    Button _btnDangerArea;
    GameObject _skillListPanel;
    readonly List<SkillListEntry> _skillListButtons = new List<SkillListEntry>();
    TextMeshProUGUI _txtLogToggleLabel;
    GameObject _logBody;
    ScrollRect _logScrollRect;
    RectTransform _logContent;
    bool _logVisible = true;
    bool _logScrollPending;
    GameObject _tooltipGo;
    TextMeshProUGUI _tooltipText;
    Canvas _hudCanvas;
    bool _postUndoHint;

    class SkillListEntry
    {
        public GameObject root;
        public Button button;
        public TextMeshProUGUI label;
        public EventTrigger trigger;
    }

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

        _hudCanvas = canvas;
        BuildLeftPanel(canvasGo.transform);
        BuildRightPanel(canvasGo.transform);
        BuildTooltip(canvasGo.transform);
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

        _txtTurn = MakeLabel(panel.transform, "TurnInfo", 22, new Color(1f, 0.9f, 0.5f), 40);
        MakeSeparator(panel.transform);
        _txtStatus = MakeLabel(panel.transform, "Status", 18, new Color(0.75f, 0.9f, 1f), 64);
        MakeSeparator(panel.transform);
        _txtUnit = MakeLabel(panel.transform, "UnitInfo", 16, Color.white, 120);
        MakeSeparator(panel.transform);
        _btnUseSkill = MakeButton(panel.transform, "스킬 사용", OnShowSkillList, 60);
        _btnUseSkill.GetComponent<Image>().color = new Color(0.40f, 0.22f, 0.55f, 0.9f);
        _btnCancelSkill = MakeButton(panel.transform, "스킬 취소", OnCancelSkillUi, 48, 20);
        _btnCancelSkill.GetComponent<Image>().color = new Color(0.55f, 0.25f, 0.20f, 0.9f);
        _btnDangerArea = MakeButton(panel.transform, "위험영역 보기", OnToggleDangerAreaUi, 48, 20);
        _btnDangerArea.GetComponent<Image>().color = new Color(0.25f, 0.22f, 0.15f, 0.9f);

        // 스킬 목록 패널 (숨김 시작)
        _skillListPanel = new GameObject("SkillListPanel", typeof(RectTransform));
        _skillListPanel.transform.SetParent(panel.transform, false);
        _skillListPanel.AddComponent<LayoutElement>().flexibleHeight = 0.3f;
        _skillListPanel.AddComponent<Image>().color = new Color(0.10f, 0.08f, 0.16f, 0.9f);
        var sklVlg = _skillListPanel.AddComponent<VerticalLayoutGroup>();
        sklVlg.padding = new RectOffset(6, 6, 4, 4);
        sklVlg.spacing = 4f;
        sklVlg.childControlHeight = true;
        sklVlg.childControlWidth = true;
        sklVlg.childForceExpandHeight = false;
        sklVlg.childForceExpandWidth = true;
        _skillListPanel.SetActive(false);

        MakeSeparator(panel.transform);
        _btnSkipAttack = MakeButton(panel.transform, "행동 종료", OnSkipAttack, 60);
        _btnEndTurn = MakeButton(panel.transform, "강제 턴 종료", OnEndTurnSoft, 60);
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
        _txtLogToggleLabel = _btnToggleLog.GetComponentInChildren<TextMeshProUGUI>();

        // ScrollRect 컨테이너
        _logBody = new GameObject("LogScrollRect", typeof(RectTransform));
        _logBody.transform.SetParent(panel.transform, false);
        _logBody.AddComponent<LayoutElement>().flexibleHeight = 1f;
        _logBody.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.07f, 0.4f);
        _logScrollRect = _logBody.AddComponent<ScrollRect>();
        _logScrollRect.horizontal = false;
        _logScrollRect.vertical = true;
        _logScrollRect.scrollSensitivity = 30f;
        _logScrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Viewport (Mask)
        var viewportGo = new GameObject("Viewport", typeof(RectTransform));
        viewportGo.transform.SetParent(_logBody.transform, false);
        var vrt = viewportGo.GetComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero;
        vrt.anchorMax = Vector2.one;
        vrt.offsetMin = Vector2.zero;
        vrt.offsetMax = new Vector2(-14f, 0f); // 스크롤바 폭만큼 여백
        // RectMask2D: Image 없이도 동작하며 스텐실 머티리얼 교체 문제가 없음
        viewportGo.AddComponent<RectMask2D>();
        _logScrollRect.viewport = vrt;

        // Content — anchorMin/Max.x 스트레치로 width는 뷰포트에 맞추고,
        // height는 LogLine()에서 preferredHeight로 직접 설정한다.
        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(viewportGo.transform, false);
        _logContent = contentGo.GetComponent<RectTransform>();
        _logContent.anchorMin = new Vector2(0f, 1f);
        _logContent.anchorMax = new Vector2(1f, 1f);
        _logContent.pivot = new Vector2(0f, 1f);
        _logContent.offsetMin = Vector2.zero;
        _logContent.offsetMax = Vector2.zero;
        _logScrollRect.content = _logContent;

        // LogText — Content와 같은 GameObject에 추가
        _txtLog = contentGo.AddComponent<TextMeshProUGUI>();
        _txtLog.fontSize = 20;
        _txtLog.color = new Color(0.88f, 0.92f, 0.95f);
        _txtLog.alignment = TextAlignmentOptions.TopLeft;
        _txtLog.overflowMode = TextOverflowModes.Overflow;
        _txtLog.richText = false;

        // 세로 스크롤바
        var sbGo = new GameObject("LogScrollbar", typeof(RectTransform));
        sbGo.transform.SetParent(_logBody.transform, false);
        var sbrt = sbGo.GetComponent<RectTransform>();
        sbrt.anchorMin = new Vector2(1f, 0f);
        sbrt.anchorMax = new Vector2(1f, 1f);
        sbrt.pivot = new Vector2(1f, 0.5f);
        sbrt.sizeDelta = new Vector2(12f, 0f);
        sbGo.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 0.8f);
        var sb = sbGo.AddComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;

        var sbHandleGo = new GameObject("Handle", typeof(RectTransform));
        sbHandleGo.transform.SetParent(sbGo.transform, false);
        var sbhrt = sbHandleGo.GetComponent<RectTransform>();
        sbhrt.anchorMin = Vector2.zero;
        sbhrt.anchorMax = Vector2.one;
        sbhrt.offsetMin = sbhrt.offsetMax = Vector2.zero;
        sbHandleGo.AddComponent<Image>().color = new Color(0.5f, 0.6f, 0.75f, 0.9f);
        sb.handleRect = sbhrt;
        sb.targetGraphic = sbHandleGo.GetComponent<Image>();

        _logScrollRect.verticalScrollbar = sb;
        _logScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
    }

    // ── HUD 헬퍼 ─────────────────────────────────────────────────────────────

    static TextMeshProUGUI MakeLabel(Transform parent, string name, int fontSize, Color color, float minH)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().minHeight = minH;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = TextAlignmentOptions.TopLeft;
        t.overflowMode = TextOverflowModes.Overflow;
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
        var tx = textGo.AddComponent<TextMeshProUGUI>();
        tx.fontSize = fontSize;
        tx.color = Color.white;
        tx.alignment = TextAlignmentOptions.Center;
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

    void OnShowSkillList()
    {
        if (_phase != Phase.UnitActive || !_selectedId.HasValue) return;
        var u = GetUnit(_selectedId.Value);
        if (u == null) return;

        HideAllSkillEntries();

        bool anySkill = false;
        int visibleIndex = 0;
        for (int i = 0; i < u.skillRuntimes.Count; i++)
        {
            var sr = u.skillRuntimes[i];
            if (!_state.SkillLookup.TryGetValue(sr.skillId, out var data)) continue;
            if (data.skillType != SrpSkillType.Active) continue;

            anySkill = true;
            bool usable = u.actionPoints > 0
                && SrpSkills.CanUseActiveSkill(data, sr)
                && !u.hasUsedSkillThisActivation;
            var capturedData = data;
            var capturedRuntime = sr;

            var entry = GetOrCreateSkillEntry(visibleIndex++);
            entry.root.name = "SkillBtn_" + data.id;
            entry.root.GetComponent<Image>().color = usable
                ? new Color(0.30f, 0.18f, 0.50f, 0.9f)
                : new Color(0.20f, 0.20f, 0.25f, 0.6f);
            entry.button.interactable = usable;
            entry.button.onClick.RemoveAllListeners();
            entry.button.onClick.AddListener(() =>
            {
                HideTooltip();
                _skillListPanel.SetActive(false);
                BeginSkillTargeting(capturedData, capturedRuntime);
            });

            entry.label.fontSize = 18;
            entry.label.color = usable ? Color.white : new Color(0.6f, 0.6f, 0.6f);
            string cdText = sr.cooldownRemaining > 0 ? $" (CD:{sr.cooldownRemaining})" : "";
            entry.label.text = $"{data.displayName}{cdText}";
            entry.label.alignment = TextAlignmentOptions.MidlineLeft;
            entry.label.overflowMode = TextOverflowModes.Ellipsis;
            entry.label.textWrappingMode = TextWrappingModes.Normal;

            string fullTooltip = $"<b>{data.displayName}</b>{cdText}\n{data.description}";
            if (data.endsActivation) fullTooltip += "\n(사용 후 활성화 종료)";
            SetTooltipTrigger(entry.trigger, fullTooltip);
            entry.root.SetActive(true);
        }

        if (!anySkill)
        {
            var entry = GetOrCreateSkillEntry(0);
            entry.root.name = "NoSkill";
            entry.root.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.15f, 0.7f);
            entry.button.interactable = false;
            entry.button.onClick.RemoveAllListeners();
            entry.label.text = "사용 가능한 액티브 스킬 없음";
            entry.label.fontSize = 18;
            entry.label.color = new Color(0.6f, 0.6f, 0.6f);
            entry.label.alignment = TextAlignmentOptions.Center;
            entry.label.textWrappingMode = TextWrappingModes.Normal;
            SetTooltipTrigger(entry.trigger, string.Empty);
            entry.root.SetActive(true);
        }

        _skillListPanel.SetActive(true);
        UpdateHud();
    }

    void OnCancelSkillUi()
    {
        if (_skillListPanel != null && _skillListPanel.activeSelf && _phase == Phase.UnitActive)
        {
            HideTooltip();
            _skillListPanel.SetActive(false);
            UpdateHud();
            return;
        }
        CancelSkillTargeting();
    }

    void OnToggleDangerAreaUi()
    {
        ToggleDangerArea();
    }

    SkillListEntry GetOrCreateSkillEntry(int index)
    {
        while (_skillListButtons.Count <= index)
        {
            var btnGo = new GameObject("SkillBtn", typeof(RectTransform));
            btnGo.transform.SetParent(_skillListPanel.transform, false);
            btnGo.AddComponent<LayoutElement>().minHeight = 48f;
            btnGo.AddComponent<Image>().color = new Color(0.30f, 0.18f, 0.50f, 0.9f);
            var btn = btnGo.AddComponent<Button>();
            var trigger = btnGo.AddComponent<EventTrigger>();

            var lblGo = new GameObject("Lbl", typeof(RectTransform));
            lblGo.transform.SetParent(btnGo.transform, false);
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(8, 0);
            lrt.offsetMax = new Vector2(-8, 0);
            var tx = lblGo.AddComponent<TextMeshProUGUI>();
            tx.fontSize = 18;
            tx.color = Color.white;
            tx.alignment = TextAlignmentOptions.MidlineLeft;
            tx.overflowMode = TextOverflowModes.Ellipsis;
            tx.textWrappingMode = TextWrappingModes.Normal;

            _skillListButtons.Add(new SkillListEntry
            {
                root = btnGo,
                button = btn,
                label = tx,
                trigger = trigger,
            });
        }
        return _skillListButtons[index];
    }

    void HideAllSkillEntries()
    {
        foreach (var entry in _skillListButtons)
        {
            if (entry == null || entry.root == null)
                continue;
            entry.root.SetActive(false);
            entry.button.onClick.RemoveAllListeners();
            SetTooltipTrigger(entry.trigger, string.Empty);
        }
    }

    void BuildTooltip(Transform canvasRoot)
    {
        _tooltipGo = new GameObject("Tooltip", typeof(RectTransform));
        _tooltipGo.transform.SetParent(canvasRoot, false);
        var rt = _tooltipGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(380f, 0f);
        rt.pivot = new Vector2(0f, 1f);
        _tooltipGo.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.95f);
        _tooltipGo.AddComponent<VerticalLayoutGroup>().padding = new RectOffset(12, 12, 8, 8);

        var csf = _tooltipGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var txtGo = new GameObject("Text", typeof(RectTransform));
        txtGo.transform.SetParent(_tooltipGo.transform, false);
        _tooltipText = txtGo.AddComponent<TextMeshProUGUI>();
        _tooltipText.fontSize = 18;
        _tooltipText.color = new Color(0.92f, 0.95f, 1f);
        _tooltipText.alignment = TextAlignmentOptions.TopLeft;
        _tooltipText.textWrappingMode = TextWrappingModes.Normal;
        _tooltipText.richText = true;
        _tooltipText.overflowMode = TextOverflowModes.Overflow;

        _tooltipGo.AddComponent<CanvasGroup>().blocksRaycasts = false;
        _tooltipGo.SetActive(false);
    }

    void AddTooltipTrigger(GameObject target, string text)
    {
        var et = target.AddComponent<EventTrigger>();
        SetTooltipTrigger(et, text);
    }

    void SetTooltipTrigger(EventTrigger et, string text)
    {
        if (et == null)
            return;
        et.triggers.Clear();
        if (string.IsNullOrEmpty(text))
            return;

        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        string capturedText = text;
        enterEntry.callback.AddListener(_ => ShowTooltip(capturedText));
        et.triggers.Add(enterEntry);

        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ => HideTooltip());
        et.triggers.Add(exitEntry);
    }

    void ShowTooltip(string text)
    {
        if (_tooltipGo == null) return;
        _tooltipText.text = text;
        _tooltipGo.SetActive(true);
        StartCoroutine(PositionTooltipNextFrame());
    }

    IEnumerator PositionTooltipNextFrame()
    {
        yield return null;
        if (_tooltipGo == null || !_tooltipGo.activeSelf) yield break;
        var rt = _tooltipGo.GetComponent<RectTransform>();
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _hudCanvas.GetComponent<RectTransform>(), Input.mousePosition, null, out pos);
        pos.x += 16f;
        pos.y -= 8f;
        rt.anchoredPosition = pos;
    }

    void HideTooltip()
    {
        if (_tooltipGo != null) _tooltipGo.SetActive(false);
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
        string formatted = $"[{DateTime.Now:HH:mm:ss}] {msg}";
        _log.Add(formatted);
        bool trimmed = false;
        while (_log.Count > MaxLogLines)
        {
            _log.RemoveAt(0);
            trimmed = true;
        }

        if (_txtLog != null)
        {
            if (trimmed || string.IsNullOrEmpty(_txtLog.text))
            {
                var sb = new StringBuilder();
                foreach (var line in _log)
                    sb.AppendLine(line);
                _txtLog.text = sb.ToString();
            }
            else
            {
                _txtLog.text += formatted + "\n";
            }
        }
        Debug.Log("[SRPG] " + msg);

        if (_logScrollRect != null && _logContent != null && !_logScrollPending)
        {
            _logScrollPending = true;
            StartCoroutine(ApplyLogScroll());
        }
    }

    // Awake에서 곧바로 Canvas 레이아웃을 강제하면 순서 문제가 생기므로
    // 1프레임 기다려 레이아웃이 확정된 뒤 Content 크기·스크롤 위치를 갱신한다.
    IEnumerator ApplyLogScroll()
    {
        yield return null; // 이전 프레임 레이아웃 완료 대기
        _logScrollPending = false;
        if (_logContent == null || _txtLog == null || _logScrollRect == null) yield break;
        _logContent.sizeDelta = new Vector2(0f, _txtLog.preferredHeight);
        yield return null; // sizeDelta 반영 레이아웃 완료 대기
        _logScrollRect.verticalNormalizedPosition = 0f;
    }

    void UpdateHud()
    {
        if (_txtTurn == null) return;
        _txtTurn.text = BuildTurnHudText();
        _txtStatus.text = BuildStatusHudText();
        _txtUnit.text = BuildUnitHudText();

        bool unitActive = !_gameOver && _phase == Phase.UnitActive;
        if (_btnSkipAttack != null)
            _btnSkipAttack.interactable = unitActive;
        if (_btnEndTurn != null)
            _btnEndTurn.interactable = !_gameOver
                && (_phase == Phase.UnitActive || (_phase == Phase.SelectingSkillTarget && _selectedId.HasValue));
        if (_btnUndo != null)
            _btnUndo.interactable = _undo.Count > 0;
        if (_btnUseSkill != null)
            _btnUseSkill.interactable = unitActive;
        if (_btnCancelSkill != null)
            _btnCancelSkill.interactable = !_gameOver
                && (_phase == Phase.SelectingSkillTarget
                    || (_phase == Phase.UnitActive && _skillListPanel != null && _skillListPanel.activeSelf));
        if (_btnDangerArea != null)
        {
            _btnDangerArea.interactable = !_gameOver;
            var label = _btnDangerArea.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = IsDangerAreaVisible ? "위험영역 숨기기" : "위험영역 보기";
        }

        if (_skillListPanel != null && _phase != Phase.UnitActive && _phase != Phase.SelectingSkillTarget)
            _skillListPanel.SetActive(false);
    }

    string BuildTurnHudText()
    {
        string mapName = initialMap != null ? initialMap.name : "?";
        var current = _state.CurrentUnitId > 0 ? GetUnit(_state.CurrentUnitId) : null;
        string currentText = current != null
            ? $"{current.displayName}({current.id}) SPD:{current.speed}"
            : "없음";
        string queueText = BuildQueuePreviewText();
        return _gameOver
            ? "게임 종료"
            : $"라운드 {_state.RoundNumber}\n현재: {currentText}\n대기: {queueText}\n맵: {mapName}";
    }

    string BuildQueuePreviewText()
    {
        if (_state.RoundQueue == null || _state.RoundQueue.Count == 0)
            return "-";

        var queueSb = new StringBuilder();
        int shown = Mathf.Min(QueuePreviewCount, _state.RoundQueue.Count);
        for (int i = 0; i < shown; i++)
        {
            var qUnit = GetUnit(_state.RoundQueue[i]);
            if (qUnit == null) continue;
            if (queueSb.Length > 0)
                queueSb.Append(" > ");
            queueSb.Append($"{qUnit.displayName}({qUnit.id})");
        }
        if (_state.RoundQueue.Count > shown)
            queueSb.Append($" ...+{_state.RoundQueue.Count - shown}");
        return queueSb.ToString();
    }

    string BuildStatusHudText()
    {
        if (_phase == Phase.Idle)
        {
            if (_postUndoHint)
                return "되감기 후 상태\n현재 행동 유닛 타일을 다시 클릭하세요";
            return "다음 행동 유닛 자동 선택 대기";
        }

        if (_phase == Phase.SelectingSkillTarget)
        {
            string skillName = _pendingSkillData != null ? _pendingSkillData.displayName : "?";
            return $"스킬 대상 선택: {skillName}\n보라색 타일 클릭 / 잘못 선택 시 스킬 취소";
        }

        if (!string.IsNullOrEmpty(_hoverStatusHint))
            return $"행동 단계\n{_hoverStatusHint}";

        string moveInfo = _remainingMove > 0 ? $"이동력 {_remainingMove}" : "이동력 없음";
        string atkInfo = _hasAttackedThisTurn ? "공격 완료" : "공격 가능 (공격 후 턴 종료)";
        string dangerInfo = IsDangerAreaVisible ? "위험영역 ON" : "위험영역 OFF";
        string undoInfo = _undo.Count > 0 ? "되감기 가능" : "되감기 없음(행동 확정 후 생성)";
        return $"행동 단계\n{moveInfo} | {atkInfo}\n{dangerInfo} | {undoInfo}\n행동 종료=정상 종료 / 강제 턴 종료=선택 중단 후 종료";
    }

    string BuildUnitHudText()
    {
        SrpUnitRuntime unit = null;
        if (_selectedId.HasValue)
            unit = GetUnit(_selectedId.Value);
        if (unit == null && _state.CurrentUnitId > 0)
            unit = GetUnit(_state.CurrentUnitId);
        if (unit == null)
            return "— 유닛 정보 없음 —";

        var sb = new StringBuilder();
        bool isCurrent = _state.CurrentUnitId == unit.id;
        sb.AppendLine($"{(isCurrent ? "▶ " : string.Empty)}{unit.displayName} (P{unit.owner}) [{unit.weaponClass}]");
        sb.AppendLine($"HP {unit.hp}/{unit.maxHp}  PG {unit.pg}/{unit.maxPg}");
        sb.AppendLine($"AP {unit.actionPoints}/{unit.maxActionPoints}  RP {unit.reactionPoints}/{unit.maxReactionPoints}");
        sb.AppendLine($"태세: {unit.stance}  방향: {unit.facing}  그로기: {unit.groggy}");
        if (unit.skillIds.Count > 0)
        {
            sb.Append("스킬:");
            foreach (var sid in unit.skillIds)
            {
                if (_state.SkillLookup.TryGetValue(sid, out var sd))
                    sb.Append($" {sd.displayName}");
                else
                    sb.Append($" {sid}");
            }
        }
        return sb.ToString();
    }

#if UNITY_INCLUDE_TESTS
    public bool TestHudReady => _txtTurn != null && _txtStatus != null && _txtUnit != null;
    public string TestTurnHudText => _txtTurn != null ? _txtTurn.text : string.Empty;
    public string TestStatusHudText => _txtStatus != null ? _txtStatus.text : string.Empty;
    public string TestUnitHudText => _txtUnit != null ? _txtUnit.text : string.Empty;
#endif
}

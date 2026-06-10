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
    const float TopHudHeight = 170f;
    const float TurnOrderStripWidth = 560f;
    const float TurnOrderStripHeight = 82f;
    const float TurnOrderCurrentTokenSize = 66f;
    const float TurnOrderNextTokenSize = 48f;
    const string OverlayLegendText = "범례: 초록=이동 중심점 | 주황=ZOC/주의 ring | 빨강=공격/위험 테두리 | 보라=스킬 marker | 청록=패링 가능 스킬 ring | 파랑=오버워치 테두리 | 연두=엄폐/방향엄폐 테두리 | 노랑=상호작용 목표 marker";
    readonly List<string> _log = new List<string>();

    TextMeshProUGUI _txtTurn;
    TextMeshProUGUI _txtStatus;
    TextMeshProUGUI _txtUnit;
    TextMeshProUGUI _txtLog;
    TextMeshProUGUI _txtActiveCardTitle;
    TextMeshProUGUI _txtActiveCardMeta;
    TextMeshProUGUI _txtActiveCardState;
    TextMeshProUGUI _txtPreviewTitle;
    TextMeshProUGUI _txtPreviewBody;
    HudGauge _gaugeActiveHp;
    HudGauge _gaugeActivePg;
    HudGauge _gaugeActiveAp;
    HudGauge _gaugeActiveAmmo;
    HudGauge _gaugePreviewHp;
    HudGauge _gaugePreviewPg;
    Button _btnSkipAttack;
    Button _btnEndTurn;
    Button _btnUndo;
    Button _btnLobby;
    Button _btnToggleLog;
    Button _btnUseSkill;
    Button _btnCancelSkill;
    Button _btnDangerArea;
    Button _btnOverwatch;
    Button _btnReload;
    Button _btnCover;
    Button _btnInteract;
    Button _btnStanceAggressive;
    Button _btnStanceDefensive;
    Button _btnFacingNorth;
    Button _btnFacingEast;
    Button _btnFacingSouth;
    Button _btnFacingWest;
    Button _btnOverclock;
    GameObject _skillListPanel;
    GameObject _topStatusPanel;
    GameObject _leftConsolePanel;
    GameObject _turnOrderTrackerPanel;
    GameObject _activeUnitCardPanel;
    GameObject _actionPreviewPanel;
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
    readonly List<TurnOrderIconEntry> _turnOrderIcons = new List<TurnOrderIconEntry>();
    readonly Dictionary<string, Sprite> _turnOrderSpriteCache = new Dictionary<string, Sprite>();
    Sprite _turnOrderPipSprite;

    class SkillListEntry
    {
        public GameObject root;
        public Button button;
        public TextMeshProUGUI label;
        public EventTrigger trigger;
    }

    class HudGauge
    {
        public TextMeshProUGUI label;
        public TextMeshProUGUI value;
        public Image fill;
    }

    class TurnOrderIconEntry
    {
        public GameObject root;
        public LayoutElement layout;
        public Image frame;
        public Image portrait;
        public Image ownerPip;
        public GameObject currentPointer;
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
        BuildTopStatusArea(canvasGo.transform);
        BuildTurnOrderTracker(canvasGo.transform);
        BuildLeftPanel(canvasGo.transform);
        BuildRightPanel(canvasGo.transform);
        BuildBottomTacticalCards(canvasGo.transform);
        BuildTooltip(canvasGo.transform);
        _logVisible = startWithLogVisible;
        ApplyLogVisibility();
    }

    void BuildTopStatusArea(Transform canvasRoot)
    {
        var panel = new GameObject("TopStatusPanel", typeof(RectTransform));
        _topStatusPanel = panel;
        panel.transform.SetParent(canvasRoot, false);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(leftPanelWidth + 10f, -TopHudHeight);
        rt.offsetMax = new Vector2(-(rightPanelWidth + TurnOrderStripWidth + 26f), 0f);
        panel.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.82f);

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 10, 10);
        vlg.spacing = 6f;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        _txtTurn = MakeLabel(panel.transform, "BattleHeader", 20, new Color(1f, 0.9f, 0.5f), 38);
        _txtTurn.alignment = TextAlignmentOptions.MidlineLeft;

        var infoRow = new GameObject("InfoRow", typeof(RectTransform));
        infoRow.transform.SetParent(panel.transform, false);
        infoRow.AddComponent<LayoutElement>().flexibleHeight = 1f;
        var hlg = infoRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childControlHeight = true;
        hlg.childControlWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth = true;

        var statusBox = MakeInfoBox(infoRow.transform, "BattleInfo", new Color(0.07f, 0.10f, 0.14f, 0.74f));
        _txtStatus = MakeLabel(statusBox.transform, "Status", 15, new Color(0.75f, 0.9f, 1f), 0);
        _txtStatus.GetComponent<LayoutElement>().flexibleHeight = 1f;

        var unitBox = MakeInfoBox(infoRow.transform, "UnitInfoBox", new Color(0.09f, 0.08f, 0.12f, 0.74f));
        _txtUnit = MakeLabel(unitBox.transform, "UnitInfo", 15, Color.white, 0);
        _txtUnit.GetComponent<LayoutElement>().flexibleHeight = 1f;
    }

    void BuildLeftPanel(Transform canvasRoot)
    {
        var panel = new GameObject("LeftConsolePanel", typeof(RectTransform));
        _leftConsolePanel = panel;
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

        var title = MakeLabel(panel.transform, "ConsoleTitle", 20, new Color(1f, 0.9f, 0.5f), 34);
        title.text = "전술 콘솔";
        title.alignment = TextAlignmentOptions.MidlineLeft;
        MakeSeparator(panel.transform);
        var stanceTitle = MakeLabel(panel.transform, "StanceTitle", 15, new Color(0.75f, 0.9f, 1f), 22);
        stanceTitle.text = "태세 선택";
        var stanceRow = MakeButtonRow(panel.transform, 42f);
        _btnStanceAggressive = MakeButton(stanceRow.transform, "공격", () => OnSetStanceUi(SrpStance.Aggressive), 42, 18);
        _btnStanceAggressive.GetComponent<Image>().color = new Color(0.42f, 0.18f, 0.16f, 0.9f);
        _btnStanceDefensive = MakeButton(stanceRow.transform, "수비", () => OnSetStanceUi(SrpStance.Defensive), 42, 18);
        _btnStanceDefensive.GetComponent<Image>().color = new Color(0.16f, 0.28f, 0.44f, 0.9f);

        var facingTitle = MakeLabel(panel.transform, "FacingTitle", 15, new Color(0.75f, 0.9f, 1f), 22);
        facingTitle.text = "최종 방향";
        var facingRow = MakeButtonRow(panel.transform, 40f);
        _btnFacingNorth = MakeButton(facingRow.transform, "북", () => OnSetFacingUi(SrpFacing.North), 40, 18);
        _btnFacingEast = MakeButton(facingRow.transform, "동", () => OnSetFacingUi(SrpFacing.East), 40, 18);
        _btnFacingSouth = MakeButton(facingRow.transform, "남", () => OnSetFacingUi(SrpFacing.South), 40, 18);
        _btnFacingWest = MakeButton(facingRow.transform, "서", () => OnSetFacingUi(SrpFacing.West), 40, 18);

        MakeSeparator(panel.transform);
        _btnUseSkill = MakeButton(panel.transform, "스킬 사용", OnShowSkillList, 60);
        _btnUseSkill.GetComponent<Image>().color = new Color(0.40f, 0.22f, 0.55f, 0.9f);
        _btnCancelSkill = MakeButton(panel.transform, "스킬 취소", OnCancelSkillUi, 48, 20);
        _btnCancelSkill.GetComponent<Image>().color = new Color(0.55f, 0.25f, 0.20f, 0.9f);
        _btnOverclock = MakeButton(panel.transform, "오버클럭", OnOverclockUi, 48, 20);
        _btnOverclock.GetComponent<Image>().color = new Color(0.38f, 0.26f, 0.12f, 0.9f);
        _btnReload = MakeButton(panel.transform, "재장전", OnReloadUi, 48, 20);
        _btnReload.GetComponent<Image>().color = new Color(0.18f, 0.38f, 0.28f, 0.9f);
        _btnCover = MakeButton(panel.transform, "엄폐", OnCoverUi, 48, 20);
        _btnCover.GetComponent<Image>().color = new Color(0.24f, 0.42f, 0.18f, 0.9f);
        _btnInteract = MakeButton(panel.transform, "상호작용", OnInteractUi, 48, 20);
        _btnInteract.GetComponent<Image>().color = new Color(0.45f, 0.36f, 0.12f, 0.9f);
        _btnOverwatch = MakeButton(panel.transform, "오버워치", OnOverwatchUi, 48, 20);
        _btnOverwatch.GetComponent<Image>().color = new Color(0.15f, 0.24f, 0.55f, 0.9f);
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
        _btnSkipAttack = MakeButton(panel.transform, "행동 종료", OnSkipAttack, 54, 22);
        _btnEndTurn = MakeButton(panel.transform, "턴 종료", OnEndTurnSoft, 54, 22);
        _btnUndo = MakeButton(panel.transform, "되감기", OnUndo, 54, 22);
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

    void BuildTurnOrderTracker(Transform parent)
    {
        var panel = new GameObject("TurnOrderTrackerPanel", typeof(RectTransform));
        _turnOrderTrackerPanel = panel;
        panel.transform.SetParent(parent, false);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-(rightPanelWidth + 16f), -14f);
        rt.sizeDelta = new Vector2(TurnOrderStripWidth, TurnOrderStripHeight);
        panel.AddComponent<Image>().color = new Color(0.045f, 0.055f, 0.075f, 0.92f);

        var hlg = panel.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 12, 8, 8);
        hlg.spacing = 8f;
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childControlHeight = true;
        hlg.childControlWidth = true;
        hlg.childForceExpandHeight = false;
        hlg.childForceExpandWidth = false;

        _turnOrderIcons.Clear();
        for (int i = 0; i < QueuePreviewCount + 1; i++)
            _turnOrderIcons.Add(MakeTurnOrderIconEntry(panel.transform, i));
    }

    TurnOrderIconEntry MakeTurnOrderIconEntry(Transform parent, int index)
    {
        var root = new GameObject(index == 0 ? "TurnOrderIcon_Current" : $"TurnOrderIcon_Next{index}", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        var layout = root.AddComponent<LayoutElement>();
        layout.minWidth = TurnOrderNextTokenSize;
        layout.preferredWidth = TurnOrderNextTokenSize;
        layout.minHeight = TurnOrderNextTokenSize + 10f;
        layout.preferredHeight = TurnOrderNextTokenSize + 10f;

        var frame = root.AddComponent<Image>();
        frame.color = new Color(0.16f, 0.18f, 0.22f, 0.98f);

        var portraitGo = new GameObject("Portrait", typeof(RectTransform));
        portraitGo.transform.SetParent(root.transform, false);
        var portraitRt = portraitGo.GetComponent<RectTransform>();
        portraitRt.anchorMin = Vector2.zero;
        portraitRt.anchorMax = Vector2.one;
        portraitRt.offsetMin = new Vector2(4f, 8f);
        portraitRt.offsetMax = new Vector2(-4f, -4f);
        var portrait = portraitGo.AddComponent<Image>();
        portrait.preserveAspect = true;

        var ownerGo = new GameObject("OwnerPip", typeof(RectTransform));
        ownerGo.transform.SetParent(root.transform, false);
        var ownerRt = ownerGo.GetComponent<RectTransform>();
        ownerRt.anchorMin = new Vector2(1f, 0f);
        ownerRt.anchorMax = new Vector2(1f, 0f);
        ownerRt.pivot = new Vector2(1f, 0f);
        ownerRt.anchoredPosition = new Vector2(-4f, 4f);
        ownerRt.sizeDelta = new Vector2(11f, 11f);
        var ownerPip = ownerGo.AddComponent<Image>();
        ownerPip.sprite = GetTurnOrderPipSprite();

        var pointer = new GameObject("CurrentPointer", typeof(RectTransform));
        pointer.transform.SetParent(root.transform, false);
        var pointerRt = pointer.GetComponent<RectTransform>();
        pointerRt.anchorMin = new Vector2(0.5f, 0f);
        pointerRt.anchorMax = new Vector2(0.5f, 0f);
        pointerRt.pivot = new Vector2(0.5f, 0.5f);
        pointerRt.anchoredPosition = new Vector2(0f, -3f);
        pointerRt.sizeDelta = new Vector2(12f, 12f);
        pointerRt.localRotation = Quaternion.Euler(0f, 0f, 45f);
        pointer.AddComponent<Image>().color = new Color(1f, 0.86f, 0.34f, 1f);
        pointer.SetActive(false);

        return new TurnOrderIconEntry
        {
            root = root,
            layout = layout,
            frame = frame,
            portrait = portrait,
            ownerPip = ownerPip,
            currentPointer = pointer,
        };
    }

    void BuildBottomTacticalCards(Transform canvasRoot)
    {
        var activePanel = MakeFloatingPanel(canvasRoot, "ActiveUnitCardPanel",
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(leftPanelWidth + 12f, 12f), new Vector2(455f, 220f),
            new Color(0.05f, 0.07f, 0.10f, 0.86f));
        _activeUnitCardPanel = activePanel;
        _txtActiveCardTitle = MakeLabel(activePanel.transform, "ActiveCardTitle", 18, new Color(1f, 0.9f, 0.5f), 28);
        _txtActiveCardMeta = MakeLabel(activePanel.transform, "ActiveCardMeta", 14, new Color(0.78f, 0.9f, 1f), 42);
        _gaugeActiveHp = MakeGauge(activePanel.transform, "HP", new Color(0.85f, 0.22f, 0.18f));
        _gaugeActivePg = MakeGauge(activePanel.transform, "PG", new Color(0.85f, 0.62f, 0.20f));
        _gaugeActiveAp = MakeGauge(activePanel.transform, "AP", new Color(0.25f, 0.75f, 1f));
        _gaugeActiveAmmo = MakeGauge(activePanel.transform, "탄약", new Color(0.45f, 0.95f, 0.45f));
        _txtActiveCardState = MakeLabel(activePanel.transform, "ActiveCardState", 13, new Color(0.82f, 0.86f, 0.9f), 42);

        var previewPanel = MakeFloatingPanel(canvasRoot, "ActionPreviewPanel",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-(rightPanelWidth + 12f), 12f), new Vector2(500f, 230f),
            new Color(0.07f, 0.06f, 0.10f, 0.88f));
        _actionPreviewPanel = previewPanel;
        _txtPreviewTitle = MakeLabel(previewPanel.transform, "PreviewTitle", 18, new Color(1f, 0.86f, 0.45f), 30);
        _gaugePreviewHp = MakeGauge(previewPanel.transform, "대상 HP", new Color(0.85f, 0.22f, 0.18f));
        _gaugePreviewPg = MakeGauge(previewPanel.transform, "대상 PG", new Color(0.85f, 0.62f, 0.20f));
        _txtPreviewBody = MakeLabel(previewPanel.transform, "PreviewBody", 14, new Color(0.85f, 0.9f, 0.96f), 120);
        _txtPreviewBody.GetComponent<LayoutElement>().flexibleHeight = 1f;
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
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.textWrappingMode = TextWrappingModes.Normal;
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
        tx.overflowMode = TextOverflowModes.Ellipsis;
        tx.textWrappingMode = TextWrappingModes.Normal;
        tx.text = label;
        return b;
    }

    static GameObject MakeInfoBox(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;
        le.flexibleHeight = 1f;
        go.AddComponent<Image>().color = color;
        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 6, 6);
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = true;
        vlg.childForceExpandWidth = true;
        return go;
    }

    static GameObject MakeFloatingPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;
        go.AddComponent<Image>().color = color;
        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 10, 10);
        vlg.spacing = 5f;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        return go;
    }

    static HudGauge MakeGauge(Transform parent, string label, Color fillColor)
    {
        var row = new GameObject("Gauge_" + label, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        var le = row.AddComponent<LayoutElement>();
        le.minHeight = 22f;
        le.preferredHeight = 22f;
        le.flexibleWidth = 1f;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childControlHeight = true;
        hlg.childControlWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth = false;

        var labelText = MakeGaugeText(row.transform, "Label", label, 13, new Color(0.78f, 0.88f, 0.95f), 56f);
        var bar = new GameObject("Bar", typeof(RectTransform));
        bar.transform.SetParent(row.transform, false);
        var barLe = bar.AddComponent<LayoutElement>();
        barLe.flexibleWidth = 1f;
        barLe.minWidth = 80f;
        bar.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.08f, 0.95f);

        var fillGo = new GameObject("Fill", typeof(RectTransform));
        fillGo.transform.SetParent(bar.transform, false);
        var fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        var fill = fillGo.AddComponent<Image>();
        fill.color = fillColor;

        var valueText = MakeGaugeText(row.transform, "Value", "-", 13, Color.white, 72f);
        valueText.alignment = TextAlignmentOptions.MidlineRight;
        return new HudGauge { label = labelText, value = valueText, fill = fill };
    }

    static TextMeshProUGUI MakeGaugeText(Transform parent, string name, string text, int size, Color color, float width)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        return t;
    }

    static GameObject MakeButtonRow(Transform parent, float height)
    {
        var row = new GameObject("ButtonRow", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        var le = row.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
        le.flexibleWidth = 1f;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childControlHeight = true;
        hlg.childControlWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth = true;
        return row;
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
            string resourceText = BuildSkillResourceText(data, sr);
            string tagText = BuildSkillTagText(data, sr);
            entry.label.text = $"{data.displayName}{resourceText}{tagText}";
            entry.label.alignment = TextAlignmentOptions.MidlineLeft;
            entry.label.overflowMode = TextOverflowModes.Ellipsis;
            entry.label.textWrappingMode = TextWrappingModes.Normal;

            string fullTooltip = $"<b>{data.displayName}</b>{resourceText}{tagText}\n{data.description}";
            if (SrpSkills.UsesCharges(data))
                fullTooltip += $"\n충전 회복: {Mathf.Max(1, data.chargeRecoveryTurns)} 라운드";
            if (data.overclockFrozenHeartCost > 0)
                fullTooltip += $"\n오버클럭: 안정도(FH) {data.overclockFrozenHeartCost} 소모";
            if (data.overclockPowerBonus > 0)
                fullTooltip += $"\n오버클럭 증폭: 다음 사용 피해/회복 +{data.overclockPowerBonus}";
            if (sr.overclockedUsesRemaining > 0)
                fullTooltip += "\n오버클럭 강화 대기: 다음 사용 1회 강화";
            if (data.requiresParryTelegraph)
                fullTooltip += "\n패링 가능 스킬: 대상에게 청록색 텔레그래프 표시";
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

    void OnSetStanceUi(SrpStance stance)
    {
        TrySetSelectedStance(stance, true);
    }

    void OnSetFacingUi(SrpFacing facing)
    {
        TrySetSelectedFacing(facing, true);
    }

    void OnOverclockUi()
    {
        TryOverclockSelectedSkill(true);
    }

    void OnReloadUi()
    {
        TryReloadSelectedUnit(true);
    }

    void OnCoverUi()
    {
        TryTakeCoverSelectedUnit(true);
    }

    void OnInteractUi()
    {
        TryInteractSelectedUnit(true);
    }

    void OnOverwatchUi()
    {
        if (_gameOver || _phase != Phase.UnitActive || !_selectedId.HasValue)
            return;
        var unit = GetUnit(_selectedId.Value);
        if (unit == null)
            return;
        var status = SrpOverwatch.GetArmStatus(unit);
        if (status != SrpOverwatchArmStatus.Ready)
        {
            LogLine($"오버워치 불가: {unit.displayName}({unit.id}) | {DescribeOverwatchArmStatus(status)}");
            UpdateHud();
            return;
        }

        PushUndo();
        if (!SrpOverwatch.Arm(_state, unit))
        {
            UpdateHud();
            return;
        }
        SpawnWorldFeedback(unit, "\uC624\uBC84\uC6CC\uCE58", new Color(0.35f, 0.55f, 1f));
        FlashUnit(unit, new Color(0.35f, 0.55f, 1f));
        LogLine($"오버워치 예약: {unit.displayName}({unit.id}) | 사거리 {unit.overwatchRange} | 행동 소모");
        RefreshUnitViews();
        FinishActivation();
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
        if (_state == null)
        {
            _txtTurn.text = "초기화 중...";
            _txtStatus.text = "전투 상태를 준비 중입니다.";
            _txtUnit.text = "— 유닛 정보 없음 —";
            return;
        }
        _txtTurn.text = BuildTurnHudText();
        _txtStatus.text = BuildStatusHudText();
        _txtUnit.text = BuildUnitHudText();
        UpdateTurnOrderTracker();
        UpdateBottomTacticalCards();

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
        if (_btnOverwatch != null)
        {
            var active = unitActive && _selectedId.HasValue ? GetUnit(_selectedId.Value) : null;
            var status = SrpOverwatch.GetArmStatus(active);
            _btnOverwatch.interactable = unitActive && status == SrpOverwatchArmStatus.Ready;
            var label = _btnOverwatch.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = BuildOverwatchButtonLabel(active, status);
        }
        UpdateDirectControlButtons(unitActive);

        if (_skillListPanel != null && _phase != Phase.UnitActive && _phase != Phase.SelectingSkillTarget)
            _skillListPanel.SetActive(false);
    }

    void UpdateBottomTacticalCards()
    {
        var active = GetDisplayedUnit();
        UpdateActiveUnitCard(active);
        UpdateActionPreviewCard(active);
    }

    void UpdateTurnOrderTracker()
    {
        if (_turnOrderIcons.Count == 0)
            return;

        int entryIndex = 0;
        var current = _state != null && _state.CurrentUnitId > 0 ? GetUnit(_state.CurrentUnitId) : null;
        if (current != null && !_gameOver)
            ApplyTurnOrderIcon(_turnOrderIcons[entryIndex++], current, true);

        if (_state != null && _state.RoundQueue != null && !_gameOver)
        {
            for (int i = 0; i < _state.RoundQueue.Count && entryIndex < _turnOrderIcons.Count; i++)
            {
                var unit = GetUnit(_state.RoundQueue[i]);
                if (unit == null || unit.eliminated)
                    continue;
                ApplyTurnOrderIcon(_turnOrderIcons[entryIndex++], unit, false);
            }
        }

        for (int i = entryIndex; i < _turnOrderIcons.Count; i++)
            _turnOrderIcons[i].root.SetActive(false);
    }

    void ApplyTurnOrderIcon(TurnOrderIconEntry entry, SrpUnitRuntime unit, bool isCurrent)
    {
        if (entry == null || unit == null)
            return;

        float size = isCurrent ? TurnOrderCurrentTokenSize : TurnOrderNextTokenSize;
        entry.root.SetActive(true);
        entry.layout.minWidth = size;
        entry.layout.preferredWidth = size;
        entry.layout.minHeight = size + (isCurrent ? 12f : 8f);
        entry.layout.preferredHeight = size + (isCurrent ? 12f : 8f);
        entry.frame.color = isCurrent
            ? new Color(1f, 0.84f, 0.30f, 0.96f)
            : new Color(0.13f, 0.15f, 0.20f, 0.94f);
        entry.portrait.sprite = GetTurnOrderPortraitSprite(unit);
        entry.ownerPip.sprite = GetTurnOrderPipSprite();
        entry.ownerPip.color = GetTurnOrderOwnerColor(unit.owner);
        entry.currentPointer.SetActive(isCurrent);
        entry.root.transform.SetAsLastSibling();
    }

    Sprite GetTurnOrderPipSprite()
    {
        if (_turnOrderPipSprite != null)
            return _turnOrderPipSprite;

        var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        FillTexture(tex, Color.clear);
        DrawDisc(tex, 8, 8, 7, Color.white);
        DrawCircle(tex, 8, 8, 7, new Color(0.04f, 0.05f, 0.07f, 1f));
        tex.Apply();
        _turnOrderPipSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        return _turnOrderPipSprite;
    }

    Sprite GetTurnOrderPortraitSprite(SrpUnitRuntime unit)
    {
        string key = unit == null ? "none" : $"{unit.owner}:{unit.weaponClass}";
        if (_turnOrderSpriteCache.TryGetValue(key, out var cached) && cached != null)
            return cached;

        var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        FillTexture(tex, Color.clear);

        Color ownerColor = GetTurnOrderOwnerColor(unit != null ? unit.owner : 0);
        Color faceColor = Color.Lerp(ownerColor, Color.white, 0.58f);
        Color ink = new Color(0.035f, 0.04f, 0.055f, 1f);
        Color detail = Color.Lerp(ownerColor, ink, 0.35f);

        DrawTriangle(tex, new Vector2Int(15, 42), new Vector2Int(24, 58), new Vector2Int(29, 43), faceColor);
        DrawTriangle(tex, new Vector2Int(35, 43), new Vector2Int(42, 58), new Vector2Int(50, 42), faceColor);
        DrawTriangle(tex, new Vector2Int(15, 42), new Vector2Int(24, 58), new Vector2Int(29, 43), ink, true);
        DrawTriangle(tex, new Vector2Int(35, 43), new Vector2Int(42, 58), new Vector2Int(50, 42), ink, true);
        DrawDisc(tex, 32, 30, 23, faceColor);
        DrawCircle(tex, 32, 30, 23, ink);

        DrawDisc(tex, 24, 34, 3, ink);
        DrawDisc(tex, 40, 34, 3, ink);
        DrawLine(tex, 28, 23, 32, 19, ink, 2);
        DrawLine(tex, 32, 19, 36, 23, ink, 2);

        if (unit != null)
        {
            switch (unit.weaponClass)
            {
                case SrpWeaponClass.Firearm:
                    DrawRect(tex, 16, 45, 48, 51, detail);
                    DrawRect(tex, 20, 47, 44, 49, ink);
                    break;
                case SrpWeaponClass.Magic:
                    DrawDisc(tex, 32, 46, 5, detail);
                    DrawLine(tex, 32, 39, 32, 53, ink, 1);
                    DrawLine(tex, 25, 46, 39, 46, ink, 1);
                    break;
                default:
                    DrawTriangle(tex, new Vector2Int(32, 51), new Vector2Int(24, 42), new Vector2Int(40, 42), detail);
                    DrawTriangle(tex, new Vector2Int(32, 51), new Vector2Int(24, 42), new Vector2Int(40, 42), ink, true);
                    break;
            }
        }

        tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        _turnOrderSpriteCache[key] = sprite;
        return sprite;
    }

    static Color GetTurnOrderOwnerColor(int owner)
    {
        return owner == 0
            ? new Color(0.66f, 0.86f, 1f, 1f)
            : new Color(0.95f, 0.60f, 0.45f, 1f);
    }

    static void FillTexture(Texture2D tex, Color color)
    {
        for (int y = 0; y < tex.height; y++)
        for (int x = 0; x < tex.width; x++)
            tex.SetPixel(x, y, color);
    }

    static void DrawDisc(Texture2D tex, int cx, int cy, int radius, Color color)
    {
        int r2 = radius * radius;
        for (int y = cy - radius; y <= cy + radius; y++)
        for (int x = cx - radius; x <= cx + radius; x++)
        {
            int dx = x - cx;
            int dy = y - cy;
            if (dx * dx + dy * dy <= r2)
                SetPixelSafe(tex, x, y, color);
        }
    }

    static void DrawCircle(Texture2D tex, int cx, int cy, int radius, Color color)
    {
        for (int a = 0; a < 360; a++)
        {
            float rad = a * Mathf.Deg2Rad;
            int x = Mathf.RoundToInt(cx + Mathf.Cos(rad) * radius);
            int y = Mathf.RoundToInt(cy + Mathf.Sin(rad) * radius);
            SetPixelSafe(tex, x, y, color);
            SetPixelSafe(tex, x + 1, y, color);
            SetPixelSafe(tex, x, y + 1, color);
        }
    }

    static void DrawRect(Texture2D tex, int x0, int y0, int x1, int y1, Color color)
    {
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
            SetPixelSafe(tex, x, y, color);
    }

    static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color color, int thickness = 1)
    {
        int dx = Mathf.Abs(x1 - x0);
        int sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0);
        int sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            for (int oy = -thickness; oy <= thickness; oy++)
            for (int ox = -thickness; ox <= thickness; ox++)
                SetPixelSafe(tex, x0 + ox, y0 + oy, color);
            if (x0 == x1 && y0 == y1)
                break;
            int e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x0 += sx;
            }
            if (e2 <= dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    static void DrawTriangle(Texture2D tex, Vector2Int a, Vector2Int b, Vector2Int c, Color color, bool outlineOnly = false)
    {
        if (outlineOnly)
        {
            DrawLine(tex, a.x, a.y, b.x, b.y, color, 1);
            DrawLine(tex, b.x, b.y, c.x, c.y, color, 1);
            DrawLine(tex, c.x, c.y, a.x, a.y, color, 1);
            return;
        }

        int minX = Mathf.Min(a.x, Mathf.Min(b.x, c.x));
        int maxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
        int minY = Mathf.Min(a.y, Mathf.Min(b.y, c.y));
        int maxY = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
        {
            var p = new Vector2Int(x, y);
            if (SameSide(p, a, b, c) && SameSide(p, b, a, c) && SameSide(p, c, a, b))
                SetPixelSafe(tex, x, y, color);
        }
    }

    static bool SameSide(Vector2Int p1, Vector2Int p2, Vector2Int a, Vector2Int b)
    {
        int cp1 = Cross(b - a, p1 - a);
        int cp2 = Cross(b - a, p2 - a);
        return cp1 * cp2 >= 0;
    }

    static int Cross(Vector2Int a, Vector2Int b)
    {
        return a.x * b.y - a.y * b.x;
    }

    static void SetPixelSafe(Texture2D tex, int x, int y, Color color)
    {
        if (x < 0 || y < 0 || x >= tex.width || y >= tex.height)
            return;
        tex.SetPixel(x, y, color);
    }

    SrpUnitRuntime GetDisplayedUnit()
    {
        SrpUnitRuntime unit = null;
        if (_selectedId.HasValue)
            unit = GetUnit(_selectedId.Value);
        if (unit == null && _state != null && _state.CurrentUnitId > 0)
            unit = GetUnit(_state.CurrentUnitId);
        return unit;
    }

    void UpdateActiveUnitCard(SrpUnitRuntime unit)
    {
        if (_txtActiveCardTitle == null)
            return;
        if (unit == null)
        {
            _txtActiveCardTitle.text = "현재 행동 유닛";
            _txtActiveCardMeta.text = "선택된 유닛 없음";
            _txtActiveCardState.text = "현재 행동 유닛을 선택하면 상세 정보가 표시됩니다.";
            SetGauge(_gaugeActiveHp, 0, 1);
            SetGauge(_gaugeActivePg, 0, 1);
            SetGauge(_gaugeActiveAp, 0, 1);
            SetGauge(_gaugeActiveAmmo, 0, 1, "탄약", "-");
            return;
        }

        bool isCurrent = _state != null && _state.CurrentUnitId == unit.id;
        _txtActiveCardTitle.text = $"{(isCurrent ? "▶ " : string.Empty)}{unit.displayName}({unit.id})";
        _txtActiveCardMeta.text = $"P{unit.owner} | {unit.weaponClass} | 태세 {unit.stance} | 방향 {unit.facing}";
        SetGauge(_gaugeActiveHp, unit.hp, unit.maxHp);
        SetGauge(_gaugeActivePg, unit.pg, unit.maxPg);
        SetGauge(_gaugeActiveAp, unit.actionPoints, unit.maxActionPoints);
        if (unit.UsesAmmo)
            SetGauge(_gaugeActiveAmmo, unit.ammo, unit.maxAmmo, "탄약");
        else
            SetGauge(_gaugeActiveAmmo, 0, 1, "탄약", "비총기");

        var parts = new List<string>();
        parts.Add($"반응: {BuildReactionReadinessText(unit)}");
        if (unit.frozenHeart > 0)
            parts.Add($"안정도(FH) {unit.frozenHeart}");
        if (unit.groggy)
            parts.Add("그로기");
        if (unit.coverActive)
            parts.Add($"엄폐 ({unit.coverSourceX},{unit.coverSourceY})");
        if (unit.overwatchArmed)
            parts.Add($"오버워치 사거리 {unit.overwatchRange}");
        if (_state != null && _state.TryGetAdjacentInteraction(unit, out var point))
            parts.Add($"상호작용: {GetInteractionLabel(point)}");
        _txtActiveCardState.text = string.Join(" | ", parts);
    }

    void UpdateActionPreviewCard(SrpUnitRuntime active)
    {
        if (_txtPreviewTitle == null)
            return;

        var target = GetPreviewTargetUnit();
        if (target != null)
        {
            SetGauge(_gaugePreviewHp, target.hp, target.maxHp);
            SetGauge(_gaugePreviewPg, target.pg, target.maxPg);
        }
        else
        {
            SetGauge(_gaugePreviewHp, 0, 1, "대상 HP", "-");
            SetGauge(_gaugePreviewPg, 0, 1, "대상 PG", "-");
        }

        var preview = BuildActionPreviewText(active, target);
        _txtPreviewTitle.text = preview.title;
        _txtPreviewBody.text = preview.body;
    }

    static void SetGauge(HudGauge gauge, int current, int max, string label = null, string valueOverride = null)
    {
        if (gauge == null)
            return;
        if (label != null && gauge.label != null)
            gauge.label.text = label;
        int safeMax = Mathf.Max(1, max);
        float ratio = Mathf.Clamp01((float)Mathf.Max(0, current) / safeMax);
        if (gauge.fill != null)
        {
            var rt = gauge.fill.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(ratio, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            gauge.fill.enabled = ratio > 0f;
        }
        if (gauge.value != null)
            gauge.value.text = valueOverride ?? $"{Mathf.Max(0, current)}/{safeMax}";
    }

    void UpdateDirectControlButtons(bool unitActive)
    {
        var active = unitActive && _selectedId.HasValue ? GetUnit(_selectedId.Value) : null;
        bool canSetStance = CanSetSelectedStance(active);
        if (_btnStanceAggressive != null)
        {
            _btnStanceAggressive.interactable = canSetStance && active.stance != SrpStance.Aggressive;
            SetButtonLabel(_btnStanceAggressive, active != null && active.stance == SrpStance.Aggressive ? "공격*" : "공격");
        }
        if (_btnStanceDefensive != null)
        {
            _btnStanceDefensive.interactable = canSetStance && active.stance != SrpStance.Defensive;
            SetButtonLabel(_btnStanceDefensive, active != null && active.stance == SrpStance.Defensive ? "수비*" : "수비");
        }

        bool canSetFacing = active != null && unitActive;
        SetFacingButtonState(_btnFacingNorth, active, SrpFacing.North, canSetFacing);
        SetFacingButtonState(_btnFacingEast, active, SrpFacing.East, canSetFacing);
        SetFacingButtonState(_btnFacingSouth, active, SrpFacing.South, canSetFacing);
        SetFacingButtonState(_btnFacingWest, active, SrpFacing.West, canSetFacing);

        if (_btnOverclock != null)
        {
            bool canOverclock = active != null && unitActive && FindFirstOverclockableSkill(active, out _, out _);
            _btnOverclock.interactable = canOverclock;
            SetButtonLabel(_btnOverclock, canOverclock ? "오버클럭" : "오버클럭 불가");
        }
        if (_btnReload != null)
        {
            bool canReload = active != null && unitActive && CanReloadSelectedUnit(active);
            _btnReload.interactable = canReload;
            SetButtonLabel(_btnReload, BuildReloadButtonLabel(active, canReload));
        }
        if (_btnCover != null)
        {
            bool canCover = active != null && unitActive && CanTakeCoverSelectedUnit(active);
            _btnCover.interactable = canCover;
            SetButtonLabel(_btnCover, BuildCoverButtonLabel(active, canCover));
        }
        if (_btnInteract != null)
        {
            bool canInteract = active != null && unitActive && CanInteractSelectedUnit(active);
            _btnInteract.interactable = canInteract;
            SetButtonLabel(_btnInteract, BuildInteractButtonLabel(active, canInteract));
        }
    }

    static string BuildReloadButtonLabel(SrpUnitRuntime unit, bool canReload)
    {
        if (unit == null || unit.weaponClass != SrpWeaponClass.Firearm)
            return "재장전 불가";
        if (!unit.UsesAmmo)
            return "재장전 불가";
        if (unit.ammo >= unit.maxAmmo)
            return "재장전 완료";
        return canReload ? $"재장전 {unit.ammo}/{unit.maxAmmo}" : "재장전 불가";
    }

    static string BuildCoverButtonLabel(SrpUnitRuntime unit, bool canCover)
    {
        if (unit == null)
            return "엄폐 불가";
        if (unit.coverActive)
            return "엄폐 중";
        return canCover ? "엄폐" : "엄폐 불가";
    }

    static string BuildInteractButtonLabel(SrpUnitRuntime unit, bool canInteract)
    {
        if (unit == null)
            return "상호작용 불가";
        return canInteract ? "상호작용" : "상호작용 불가";
    }

    static void SetFacingButtonState(Button button, SrpUnitRuntime active, SrpFacing facing, bool canSetFacing)
    {
        if (button == null)
            return;
        button.interactable = canSetFacing && active.facing != facing;
        SetButtonLabel(button, active != null && active.facing == facing ? $"{FacingShortName(facing)}*" : FacingShortName(facing));
    }

    static void SetButtonLabel(Button button, string text)
    {
        if (button == null)
            return;
        var label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.text = text;
    }

    static string FacingShortName(SrpFacing facing)
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

    string BuildTurnHudText()
    {
        if (_state == null)
            return "초기화 중...";
        string mapName = initialMap != null ? initialMap.name : "?";
        return _gameOver
            ? "게임 종료"
            : $"라운드 {_state.RoundNumber} | 상태: {BuildHudPhaseLabel()} | 위험영역 {(IsDangerAreaVisible ? "ON" : "OFF")} | 맵: {mapName}";
    }

    string BuildQueuePreviewText()
    {
        return BuildTurnOrderPreviewText().Replace("\n", " > ");
    }

    string BuildHudPhaseLabel()
    {
        if (_gameOver)
            return "종료";
        switch (_phase)
        {
            case Phase.UnitActive:
                return "행동 중";
            case Phase.SelectingSkillTarget:
                return "스킬 대상 선택";
            default:
                return "대기";
        }
    }

    string BuildTurnOrderCurrentText()
    {
        if (_state == null)
            return "NOW > -";
        if (_gameOver)
            return "NOW > 전투 종료";

        var current = _state.CurrentUnitId > 0 ? GetUnit(_state.CurrentUnitId) : null;
        if (current == null)
            return "NOW > -";

        return $"NOW > {BuildTurnOrderUnitLine(current)}";
    }

    string BuildTurnOrderPreviewText()
    {
        if (_state == null)
            return "-";
        if (_state.RoundQueue == null || _state.RoundQueue.Count == 0)
            return "NEXT -";

        var queueSb = new StringBuilder();
        int shown = Mathf.Min(QueuePreviewCount, _state.RoundQueue.Count);
        for (int i = 0; i < shown; i++)
        {
            var qUnit = GetUnit(_state.RoundQueue[i]);
            if (qUnit == null) continue;
            if (queueSb.Length > 0)
                queueSb.AppendLine();
            queueSb.Append($"NEXT {i + 1}. {BuildTurnOrderUnitLine(qUnit)}");
        }
        if (_state.RoundQueue.Count > shown)
            queueSb.AppendLine().Append($"+{_state.RoundQueue.Count - shown} more");
        return queueSb.ToString();
    }

    string BuildTurnOrderUnitLine(SrpUnitRuntime unit)
    {
        if (unit == null)
            return "-";
        return $"{unit.displayName}({unit.id}) | P{unit.owner} | {unit.weaponClass} | SPD {unit.speed}";
    }

    string BuildStatusHudText()
    {
        if (_state == null)
            return "전투 상태를 준비 중입니다.";
        if (_phase == Phase.Idle)
        {
            if (_postUndoHint)
                return $"되감기 후 상태: 현재 행동 유닛 타일을 다시 클릭하세요\n{OverlayLegendText}";
            return $"다음 행동 유닛 자동 선택 대기\n{OverlayLegendText}";
        }

        if (_phase == Phase.SelectingSkillTarget)
        {
            string skillName = _pendingSkillData != null ? _pendingSkillData.displayName : "?";
            return $"스킬 대상 선택: {skillName} | 보라=대상 | 청록=패링 가능 스킬 | 취소 가능\n{OverlayLegendText}";
        }

        if (!string.IsNullOrEmpty(_hoverStatusHint))
            return $"행동 단계: {_hoverStatusHint}\n{OverlayLegendText}";

        if (_selectedId.HasValue)
        {
            var selected = GetUnit(_selectedId.Value);
            if (selected != null && selected.actionPoints <= 0)
                return $"행동 단계: AP 0, 이동/공격/스킬 불가. 행동 종료를 선택하세요\n{OverlayLegendText}";
        }

        string moveInfo = _remainingMove > 0 ? $"이동력 {_remainingMove}" : "이동력 없음";
        string atkInfo = _hasAttackedThisTurn ? "공격 완료" : "공격 가능 (공격 후 턴 종료)";
        string dangerInfo = IsDangerAreaVisible ? "위험영역 ON" : "위험영역 OFF";
        string undoInfo = _undo.Count > 0 ? "되감기 가능" : "되감기 없음(행동 확정 후 생성)";
        return $"행동 단계: {moveInfo} | {atkInfo} | {dangerInfo} | {undoInfo}\n{OverlayLegendText}";
    }

    string BuildUnitHudText()
    {
        if (_state == null)
            return "— 유닛 정보 없음 —";
        SrpUnitRuntime unit = null;
        if (_selectedId.HasValue)
            unit = GetUnit(_selectedId.Value);
        if (unit == null && _state.CurrentUnitId > 0)
            unit = GetUnit(_state.CurrentUnitId);
        if (unit == null)
            return "— 유닛 정보 없음 —";

        var sb = new StringBuilder();
        bool isCurrent = _state.CurrentUnitId == unit.id;
        sb.AppendLine($"{(isCurrent ? "▶ " : string.Empty)}{unit.displayName} (P{unit.owner}) [{unit.weaponClass}] | HP {unit.hp}/{unit.maxHp} PG {unit.pg}/{unit.maxPg}");
        sb.AppendLine($"AP {unit.actionPoints}/{unit.maxActionPoints} | 반응: {BuildReactionReadinessText(unit)} | 태세: {unit.stance} | 방향: {unit.facing} | 그로기: {unit.groggy}");
        if (isCurrent && unit.actionPoints <= 0)
            sb.AppendLine("상태: AP 소진, 행동 종료 필요");
        var stateParts = new List<string>();
        if (unit.HasTag(SrpUnitTags.Tank))
            stateParts.Add($"탱커 / 교전 수 {_state.CountEngagingEnemies(unit)}");
        var combatTags = (SrpCombatTag)unit.combatTags;
        if (combatTags != SrpCombatTag.None)
            stateParts.Add($"전투 태그: {SrpCombatTagUtility.BuildSummary(combatTags)}");
        if (unit.stance == SrpStance.Defensive && unit.defensiveHitsRound == _state.RoundNumber)
            stateParts.Add($"수비 압박 {unit.defensiveHitsTakenThisRound}");
        if (unit.overwatchArmed)
            stateParts.Add($"오버워치 사거리 {unit.overwatchRange}");
        if (unit.UsesAmmo)
            stateParts.Add($"탄약 {unit.ammo}/{unit.maxAmmo}");
        if (unit.coverActive)
            stateParts.Add($"엄폐 중 ({unit.coverSourceX},{unit.coverSourceY})");
        if (_state.TryGetAdjacentInteraction(unit, out var point))
            stateParts.Add($"상호작용 가능: {(string.IsNullOrEmpty(point.displayName) ? point.id : point.displayName)}");
        if (stateParts.Count > 0)
            sb.AppendLine(string.Join(" | ", stateParts));
        if (unit.skillIds.Count > 0)
        {
            sb.Append("스킬:");
            for (int i = 0; i < unit.skillIds.Count; i++)
            {
                var sid = unit.skillIds[i];
                if (_state.SkillLookup.TryGetValue(sid, out var sd))
                {
                    var runtime = i < unit.skillRuntimes.Count ? unit.skillRuntimes[i] : null;
                    sb.Append($" {sd.displayName}{BuildSkillResourceText(sd, runtime)}{BuildSkillTagText(sd, runtime)}");
                }
                else
                {
                    sb.Append($" {sid}");
                }
            }
        }
        return sb.ToString();
    }

    SrpUnitRuntime GetPreviewTargetUnit()
    {
        if (_state == null)
            return null;
        if (_hoverUnitId > 0)
            return GetUnit(_hoverUnitId);
        if (_hoverTileX >= 0 && _hoverTileY >= 0)
            return _state.GetOccupant(_hoverTileX, _hoverTileY);
        return null;
    }

    (string title, string body) BuildActionPreviewText(SrpUnitRuntime active, SrpUnitRuntime target)
    {
        if (_state == null)
            return ("행동 Preview", "전투 상태를 준비 중입니다.");

        if (_hoverTileX >= 0 && _hoverTileY >= 0)
        {
            if (_phase == Phase.SelectingSkillTarget && _pendingSkillData != null)
                return BuildSkillPreview(active, target);

            if (TryGetInteractionAt(_hoverTileX, _hoverTileY, out var point))
                return BuildInteractionPreview(active, point);

            if (active != null && _moveCostMap.TryGetValue(new Vector2Int(_hoverTileX, _hoverTileY), out int moveCost))
            {
                int threatCount = CountEnemyAttackersForTile(_hoverTileX, _hoverTileY, active.owner);
                string risk = threatCount > 0 ? $"예상 위협 {threatCount}명" : "직접 위협 없음";
                return ("이동 Preview", $"위치 ({_hoverTileX},{_hoverTileY})\n이동 비용 {moveCost} | AP-1\n{risk}");
            }
        }

        if (active != null && target != null && target.owner != active.owner && _attackIds.Contains(target.id))
            return BuildAttackPreview(active, target);

        if (target != null)
            return ($"대상 정보: {target.displayName}", BuildTargetInfoText(target));

        if (active != null)
            return ("행동 Preview", "이동/공격/스킬/상호작용 대상 위에 마우스를 올리면 예상 결과가 표시됩니다.");

        return ("행동 Preview", "현재 행동 유닛을 선택하면 preview가 활성화됩니다.");
    }

    (string title, string body) BuildAttackPreview(SrpUnitRuntime active, SrpUnitRuntime target)
    {
        if (!active.HasAmmoForAttack())
            return ($"공격 Preview: {target.displayName}", "탄약 없음. 재장전 후 공격할 수 있습니다.");

        var clone = _state.Clone();
        var cloneActive = clone.FindUnitById(active.id);
        var cloneTarget = clone.FindUnitById(target.id);
        if (cloneActive == null || cloneTarget == null)
            return ($"공격 Preview: {target.displayName}", "예상 피해를 계산할 수 없습니다.");

        var outcome = SrpCombatResolver.ApplyAttack(clone, cloneActive, cloneTarget);
        var parts = new List<string>
        {
            $"예상 피해: HP-{outcome.damageToHp} / PG-{outcome.damageToPg}",
            $"결과 예상: HP {Mathf.Max(0, target.hp - outcome.damageToHp)}/{target.maxHp}, PG {Mathf.Max(0, target.pg - outcome.damageToPg)}/{target.maxPg}",
            active.UsesAmmo ? "소모: AP-1, 탄약-1" : "소모: AP-1",
        };
        if (active.weaponClass == SrpWeaponClass.Firearm
            && SrpFirearmAim.CanBasicAttack(_state, active, target, out var aim))
        {
            parts.Add($"총기 기본 조준: 벡터 조준, 거리 {aim.distance}, 방향 {aim.facing}, sector {aim.sector8}");
        }
        if (outcome.wasExecution)
            parts.Add("처단 공격 예상");
        if (outcome.becameGroggy)
            parts.Add("PG 붕괴 예상");
        if (outcome.wasDodged)
            parts.Add("대상 회피 성공 가능성 반영");
        if (outcome.wasParried)
            parts.Add("대상 패링 가능성 반영");
        if (outcome.coverBufferApplied)
            parts.Add($"엄폐 완충 HP-{outcome.reducedHpByCover} PG-{outcome.reducedPgByCover}");
        return ($"공격 Preview: {target.displayName}", string.Join("\n", parts));
    }

    (string title, string body) BuildSkillPreview(SrpUnitRuntime active, SrpUnitRuntime target)
    {
        string skillName = _pendingSkillData.displayName;
        var parts = new List<string>
        {
            $"스킬: {skillName}",
            $"대상: {(target != null ? target.displayName : $"({_hoverTileX},{_hoverTileY})")}",
            $"소모: AP-1 | {BuildSkillResourceText(_pendingSkillData, _pendingSkillRuntime)}",
            BuildSkillEffectSummary(_pendingSkillData, _pendingSkillRuntime),
        };

        string delta = TryBuildSkillDeltaPreview(active, target);
        if (!string.IsNullOrEmpty(delta))
            parts.Add(delta);
        return ($"스킬 Preview: {skillName}", string.Join("\n", parts));
    }

    string TryBuildSkillDeltaPreview(SrpUnitRuntime active, SrpUnitRuntime target)
    {
        if (active == null || _pendingSkillData == null || _pendingSkillRuntime == null)
            return string.Empty;
        if (_hoverTileX < 0 || _hoverTileY < 0 || !_skillTargetTiles.Contains(new Vector2Int(_hoverTileX, _hoverTileY)))
            return "현재 타일은 선택 가능한 스킬 대상이 아닙니다.";

        var clone = _state.Clone();
        var cloneCaster = clone.FindUnitById(active.id);
        if (cloneCaster == null)
            return string.Empty;
        SrpSkillRuntime cloneRuntime = null;
        foreach (var runtime in cloneCaster.skillRuntimes)
        {
            if (runtime != null && runtime.skillId == _pendingSkillRuntime.skillId)
            {
                cloneRuntime = runtime;
                break;
            }
        }
        if (cloneRuntime == null)
            cloneRuntime = new SrpSkillRuntime(_pendingSkillRuntime.skillId);

        int targetId = target != null ? target.id : cloneCaster.id;
        var beforeTarget = target != null ? target : active;
        int beforeHp = beforeTarget.hp;
        int beforePg = beforeTarget.pg;
        SrpSkills.ResolveActiveSkill(_pendingSkillData, cloneRuntime, cloneCaster, _hoverTileX, _hoverTileY, clone, null);
        var afterTarget = clone.FindUnitById(targetId) ?? cloneCaster;
        int hpDelta = afterTarget.hp - beforeHp;
        int pgDelta = afterTarget.pg - beforePg;
        if (hpDelta == 0 && pgDelta == 0)
            return "예상 수치 변화: 직접 HP/PG 변화 없음";
        return $"예상 수치 변화: {FormatSignedDelta("HP", hpDelta)} / {FormatSignedDelta("PG", pgDelta)}";
    }

    (string title, string body) BuildInteractionPreview(SrpUnitRuntime active, SrpInteractionPointData point)
    {
        bool canInteract = active != null && _state.CanUnitInteractWith(active, point) && active.actionPoints > 0;
        string ownerText = point.requiredOwner < 0 ? "누구나 가능" : $"P{point.requiredOwner} 전용";
        string stateText = point.activated ? "활성화됨" : "미활성";
        string result = canInteract ? "실행 가능: AP-1, 포인트 활성화" : "현재 유닛으로 실행 불가";
        return ($"상호작용 Preview: {GetInteractionLabel(point)}",
            $"위치 ({point.x},{point.y}) | {stateText}\n조건: {ownerText} | singleUse:{point.singleUse}\n{result}");
    }

    string BuildTargetInfoText(SrpUnitRuntime target)
    {
        var parts = new List<string>
        {
            $"P{target.owner} | {target.weaponClass} | 태세 {target.stance} | 방향 {target.facing}",
            $"HP {target.hp}/{target.maxHp} | PG {target.pg}/{target.maxPg}",
            $"반응: {BuildReactionReadinessText(target)}",
        };
        if (target.UsesAmmo)
            parts.Add($"탄약 {target.ammo}/{target.maxAmmo}");
        if (target.coverActive)
            parts.Add($"엄폐 중 ({target.coverSourceX},{target.coverSourceY})");
        if (target.overwatchArmed)
            parts.Add($"오버워치 예약 사거리 {target.overwatchRange}");
        return string.Join("\n", parts);
    }

    string BuildSkillEffectSummary(SrpSkillData data, SrpSkillRuntime runtime)
    {
        if (data == null)
            return "효과: -";
        var parts = new List<string>();
        if (data.effects != null)
        {
            foreach (var effect in data.effects)
                parts.Add($"{effect.type} {effect.stat} {effect.value}");
        }
        if (data.cooldown > 0)
            parts.Add($"쿨다운 {data.cooldown}");
        if (SrpSkills.UsesCharges(data))
            parts.Add($"충전 {runtime?.chargesRemaining ?? 0}/{data.maxCharges}");
        if (data.overclockPowerBonus > 0 && runtime != null && runtime.overclockedUsesRemaining > 0)
            parts.Add($"강화 대기 +{data.overclockPowerBonus}");
        return parts.Count > 0 ? "효과: " + string.Join(", ", parts) : "효과: 직접 수치 효과 없음";
    }

    bool TryGetInteractionAt(int x, int y, out SrpInteractionPointData point)
    {
        point = null;
        if (_state == null || _state.InteractionPoints == null)
            return false;
        foreach (var candidate in _state.InteractionPoints)
        {
            if (candidate != null && candidate.x == x && candidate.y == y)
            {
                point = candidate;
                return true;
            }
        }
        return false;
    }

    static string GetInteractionLabel(SrpInteractionPointData point)
    {
        if (point == null)
            return "-";
        return string.IsNullOrEmpty(point.displayName) ? point.id : point.displayName;
    }

    static string FormatSignedDelta(string label, int delta)
    {
        if (delta > 0)
            return $"{label}+{delta}";
        if (delta < 0)
            return $"{label}{delta}";
        return $"{label}±0";
    }

    static string BuildSkillResourceText(SrpSkillData data, SrpSkillRuntime runtime)
    {
        if (data == null || runtime == null)
            return string.Empty;

        var parts = new List<string>();
        if (runtime.cooldownRemaining > 0)
            parts.Add($"쿨다운:{runtime.cooldownRemaining}");
        if (SrpSkills.UsesCharges(data))
            parts.Add($"충전:{runtime.chargesRemaining}/{data.maxCharges}");
        return parts.Count > 0 ? $" ({string.Join(", ", parts)})" : string.Empty;
    }

    static string BuildSkillTagText(SrpSkillData data, SrpSkillRuntime runtime = null)
    {
        if (data == null)
            return string.Empty;

        var tags = new List<string>();
        if (data.isParryable || data.requiresParryTelegraph)
            tags.Add("패링 가능");
        if (data.overclockFrozenHeartCost > 0)
            tags.Add("오버클럭");
        if (data.overclockPowerBonus > 0)
            tags.Add($"증폭+{data.overclockPowerBonus}");
        string combatTagText = BuildSkillCombatTagText(data);
        if (!string.IsNullOrEmpty(combatTagText))
            tags.Add(combatTagText);
        if (runtime != null && runtime.overclockedUsesRemaining > 0)
            tags.Add("강화 대기");
        return tags.Count > 0 ? $" [{string.Join("/", tags)}]" : string.Empty;
    }

    static string BuildSkillCombatTagText(SrpSkillData data)
    {
        if (data?.effects == null)
            return string.Empty;

        var parts = new List<string>();
        foreach (var effect in data.effects)
        {
            if (effect == null || effect.type != SrpEffectType.ApplyCombatTag)
                continue;
            if (SrpCombatTagUtility.TryParse(effect.stat, out var tag))
                parts.Add(SrpCombatTagUtility.GetDisplayName(tag));
        }
        return parts.Count > 0 ? $"태그:{string.Join(",", parts)}" : string.Empty;
    }

    static string BuildReactionReadinessText(SrpUnitRuntime unit)
    {
        if (unit == null || unit.eliminated)
            return "불가";
        if (unit.reactionPoints > 0)
            return unit.overwatchArmed ? "예약 중" : "준비";
        return "소모됨";
    }

    static string BuildOverwatchButtonLabel(SrpUnitRuntime unit, SrpOverwatchArmStatus status)
    {
        if (unit != null && unit.overwatchArmed)
            return "오버워치 예약 중";
        return status == SrpOverwatchArmStatus.Ready ? "오버워치 예약" : "오버워치 불가";
    }

    static string DescribeOverwatchArmStatus(SrpOverwatchArmStatus status)
    {
        switch (status)
        {
            case SrpOverwatchArmStatus.Ready:
                return "예약 가능";
            case SrpOverwatchArmStatus.AlreadyArmed:
                return "이미 예약 중";
            case SrpOverwatchArmStatus.NoUnit:
                return "선택 유닛 없음";
            case SrpOverwatchArmStatus.Eliminated:
                return "전투 불능";
            case SrpOverwatchArmStatus.NoAction:
                return "행동 소모";
            case SrpOverwatchArmStatus.NoReaction:
                return "반응 소모";
            case SrpOverwatchArmStatus.NotFirearm:
                return "총기 유닛만 예약 가능";
            case SrpOverwatchArmStatus.RangeTooShort:
                return "원거리 사거리 필요";
            case SrpOverwatchArmStatus.NoAmmo:
                return "탄약 없음";
            default:
                return "조건 불충족";
        }
    }

#if UNITY_INCLUDE_TESTS
    public bool TestHudReady => _txtTurn != null && _txtStatus != null && _txtUnit != null
        && _txtActiveCardTitle != null && _txtPreviewTitle != null
        && _turnOrderTrackerPanel != null && _turnOrderIcons.Count == QueuePreviewCount + 1;
    public bool TestHasTopStatusPanel => _topStatusPanel != null && _topStatusPanel.activeInHierarchy;
    public bool TestHasLeftConsolePanel => _leftConsolePanel != null && _leftConsolePanel.activeInHierarchy;
    public bool TestHasTurnOrderTrackerPanel => _turnOrderTrackerPanel != null && _turnOrderTrackerPanel.activeInHierarchy;
    public bool TestTurnOrderTrackerIsLogChild => _turnOrderTrackerPanel != null
        && _turnOrderTrackerPanel.transform.parent != null
        && _turnOrderTrackerPanel.transform.parent.name == "RightPanel";
    public int TestTurnOrderVisibleIconCount
    {
        get
        {
            int count = 0;
            foreach (var entry in _turnOrderIcons)
            {
                if (entry != null && entry.root != null && entry.root.activeSelf)
                    count++;
            }
            return count;
        }
    }
    public bool TestTurnOrderCurrentIconHighlighted
    {
        get
        {
            if (_turnOrderIcons.Count == 0)
                return false;
            var entry = _turnOrderIcons[0];
            return entry != null
                && entry.root != null
                && entry.root.activeSelf
                && entry.currentPointer != null
                && entry.currentPointer.activeSelf;
        }
    }
    public bool TestHasActiveUnitCardPanel => _activeUnitCardPanel != null && _activeUnitCardPanel.activeInHierarchy;
    public bool TestHasActionPreviewPanel => _actionPreviewPanel != null && _actionPreviewPanel.activeInHierarchy;
    public string TestTurnHudText => _txtTurn != null ? _txtTurn.text : string.Empty;
    public string TestStatusHudText => _txtStatus != null ? _txtStatus.text : string.Empty;
    public string TestUnitHudText => _txtUnit != null ? _txtUnit.text : string.Empty;
    public string TestTurnOrderCurrentText => BuildTurnOrderCurrentText();
    public string TestTurnOrderPreviewText => BuildTurnOrderPreviewText();
    public string TestTurnOrderTrackerText => $"{TestTurnOrderCurrentText}\n{TestTurnOrderPreviewText}";
    public int TestTurnOrderPreviewLineCount
    {
        get
        {
            string text = TestTurnOrderPreviewText;
            if (string.IsNullOrEmpty(text))
                return 0;
            int count = 0;
            string[] lines = text.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("NEXT ", StringComparison.Ordinal))
                    count++;
            }
            return count;
        }
    }
    public string TestActiveUnitCardText
    {
        get
        {
            var sb = new StringBuilder();
            if (_txtActiveCardTitle != null) sb.AppendLine(_txtActiveCardTitle.text);
            if (_txtActiveCardMeta != null) sb.AppendLine(_txtActiveCardMeta.text);
            if (_gaugeActiveHp?.value != null) sb.AppendLine($"HP {_gaugeActiveHp.value.text}");
            if (_gaugeActivePg?.value != null) sb.AppendLine($"PG {_gaugeActivePg.value.text}");
            if (_gaugeActiveAp?.value != null) sb.AppendLine($"AP {_gaugeActiveAp.value.text}");
            if (_gaugeActiveAmmo?.value != null) sb.AppendLine($"탄약 {_gaugeActiveAmmo.value.text}");
            if (_txtActiveCardState != null) sb.AppendLine(_txtActiveCardState.text);
            return sb.ToString();
        }
    }
    public string TestActionPreviewText
    {
        get
        {
            var sb = new StringBuilder();
            if (_txtPreviewTitle != null) sb.AppendLine(_txtPreviewTitle.text);
            if (_gaugePreviewHp?.value != null) sb.AppendLine($"대상 HP {_gaugePreviewHp.value.text}");
            if (_gaugePreviewPg?.value != null) sb.AppendLine($"대상 PG {_gaugePreviewPg.value.text}");
            if (_txtPreviewBody != null) sb.AppendLine(_txtPreviewBody.text);
            return sb.ToString();
        }
    }
    public string TestLogText => _txtLog != null ? _txtLog.text : string.Empty;
    public string TestSkillListText
    {
        get
        {
            var sb = new StringBuilder();
            foreach (var entry in _skillListButtons)
            {
                if (entry == null || entry.root == null || !entry.root.activeSelf || entry.label == null)
                    continue;
                if (sb.Length > 0)
                    sb.Append("\n");
                sb.Append(entry.label.text);
            }
            return sb.ToString();
        }
    }
    public string TestOverwatchButtonText
    {
        get
        {
            if (_btnOverwatch == null)
                return string.Empty;
            var label = _btnOverwatch.GetComponentInChildren<TextMeshProUGUI>();
            return label != null ? label.text : string.Empty;
        }
    }
    public string TestOverclockButtonText => GetButtonText(_btnOverclock);
    public string TestReloadButtonText => GetButtonText(_btnReload);
    public string TestCoverButtonText => GetButtonText(_btnCover);
    public string TestInteractButtonText => GetButtonText(_btnInteract);
    public string TestStanceAggressiveButtonText => GetButtonText(_btnStanceAggressive);
    public string TestStanceDefensiveButtonText => GetButtonText(_btnStanceDefensive);
    public string TestFacingNorthButtonText => GetButtonText(_btnFacingNorth);
    public string TestFacingEastButtonText => GetButtonText(_btnFacingEast);
    public string TestFacingSouthButtonText => GetButtonText(_btnFacingSouth);
    public string TestFacingWestButtonText => GetButtonText(_btnFacingWest);

    static string GetButtonText(Button button)
    {
        if (button == null)
            return string.Empty;
        var label = button.GetComponentInChildren<TextMeshProUGUI>();
        return label != null ? label.text : string.Empty;
    }

    public bool TestShowSkillList()
    {
        OnShowSkillList();
        return _skillListPanel != null && _skillListPanel.activeSelf;
    }

    public bool TestEndTurnSelectedUnit()
    {
        if (!_selectedId.HasValue)
            return false;
        OnEndTurnSoft();
        return true;
    }

    public bool TestBeginFirstTargetedSkill()
    {
        var unit = _selectedId.HasValue ? GetUnit(_selectedId.Value) : null;
        if (_state == null)
            return false;
        if (TryBeginFirstTargetedSkillForUnit(unit))
            return true;

        foreach (var candidate in _state.Units)
        {
            if (candidate == null || candidate.eliminated)
                continue;
            if (TryBeginFirstTargetedSkillForUnit(candidate))
                return true;
        }
        return false;
    }

    bool TryBeginFirstTargetedSkillForUnit(SrpUnitRuntime unit)
    {
        if (unit == null || _state == null)
            return false;
        foreach (var runtime in unit.skillRuntimes)
        {
            if (runtime == null || !_state.SkillLookup.TryGetValue(runtime.skillId, out var data))
                continue;
            if (data.skillType != SrpSkillType.Active || data.targetType == SrpTargetType.Self)
                continue;
            SrpSkills.EnsureRuntimeInitialized(data, runtime);
            if (!SrpSkills.CanUseActiveSkill(data, runtime))
                continue;
            unit.actionPoints = Mathf.Max(1, unit.actionPoints);
            _selectedId = unit.id;
            _state.CurrentUnitId = unit.id;
            _phase = Phase.UnitActive;
            _remainingMove = unit.moveRange;
            var targets = SrpSkills.GetSkillTargetTiles(data, unit, _state);
            if (targets == null || targets.Count == 0)
            {
                if (!TryPrepareFeedbackSkillTarget(unit, data))
                    continue;
                targets = SrpSkills.GetSkillTargetTiles(data, unit, _state);
                if (targets == null || targets.Count == 0)
                    continue;
            }
            RefreshActiveHighlights(unit);
            BeginSkillTargeting(data, runtime);
            return _phase == Phase.SelectingSkillTarget && _skillTargetTiles.Count > 0;
        }
        return false;
    }

    bool TryPrepareFeedbackSkillTarget(SrpUnitRuntime caster, SrpSkillData data)
    {
        if (caster == null || data == null || _state == null)
            return false;
        if (data.targetType == SrpTargetType.AreaEnemy || data.targetType == SrpTargetType.AreaAlly)
            return true;

        bool wantsEnemy = data.targetType == SrpTargetType.SingleEnemy;
        SrpUnitRuntime target = null;
        foreach (var candidate in _state.Units)
        {
            if (candidate == null || candidate.id == caster.id)
                continue;
            bool ownerMatches = wantsEnemy
                ? candidate.owner != caster.owner
                : candidate.owner == caster.owner;
            if (!ownerMatches)
                continue;
            target = candidate;
            break;
        }
        if (target == null)
            return false;

        int range = Mathf.Max(1, data.range);
        bool wasEliminated = target.eliminated;
        target.eliminated = true;
        for (int y = Mathf.Max(0, caster.anchorY - range); y <= Mathf.Min(_state.Height - 1, caster.anchorY + range); y++)
        for (int x = Mathf.Max(0, caster.anchorX - range); x <= Mathf.Min(_state.Width - 1, caster.anchorX + range); x++)
        {
            if (x == caster.anchorX && y == caster.anchorY)
                continue;
            if (!_state.CanStandAt(target, x, y, target.id))
                continue;
            target.anchorX = x;
            target.anchorY = y;
            target.eliminated = false;
            target.hp = Mathf.Max(1, target.hp > 0 ? target.hp : target.maxHp);
            target.pg = Mathf.Max(1, target.pg > 0 ? target.pg : target.maxPg);
            _state.RebuildEngagements();
            RefreshUnitViews();
            return true;
        }
        target.eliminated = wasEliminated;
        return false;
    }

    public bool TestUsePendingSkillOnFirstTarget()
    {
        if (_phase != Phase.SelectingSkillTarget || _skillTargetTiles.Count == 0)
            return false;
        var tile = _skillTargetTiles[0];
        OnTileClicked(tile.x, tile.y);
        return true;
    }

    public bool TestForceCurrentUnitIntoEnemyZoc()
    {
        if (_state == null || _state.CurrentUnitId <= 0)
            return false;
        var unit = GetUnit(_state.CurrentUnitId);
        if (unit == null)
            return false;
        SrpUnitRuntime enemy = null;
        foreach (var candidate in _state.Units)
        {
            if (candidate != null && !candidate.eliminated && candidate.owner != unit.owner)
            {
                enemy = candidate;
                break;
            }
        }
        if (enemy == null)
            return false;

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };
        for (int i = 0; i < dx.Length; i++)
        {
            int nx = unit.anchorX + dx[i];
            int ny = unit.anchorY + dy[i];
            if (!_state.CanStandAt(enemy, nx, ny, enemy.id))
                continue;
            enemy.anchorX = nx;
            enemy.anchorY = ny;
            _state.RebuildEngagements();
            RefreshUnitViews();
            RefreshActiveHighlights(unit);
            UpdateUnitFeedbackVisuals();
            UpdateHud();
            return true;
        }
        return false;
    }

    public bool TestSetSelectedStance(SrpStance stance)
    {
        return TrySetSelectedStance(stance, false);
    }

    public bool TestSetSelectedFacing(SrpFacing facing)
    {
        return TrySetSelectedFacing(facing, false);
    }

    public bool TestPrepareFirstOverclockableSkill()
    {
        var unit = _selectedId.HasValue ? GetUnit(_selectedId.Value) : null;
        if (unit == null)
            return false;

        foreach (var runtime in unit.skillRuntimes)
        {
            if (runtime == null || !_state.SkillLookup.TryGetValue(runtime.skillId, out var data))
                continue;
            if (data.overclockFrozenHeartCost <= 0)
                continue;
            SrpSkills.EnsureRuntimeInitialized(data, runtime);
            unit.frozenHeart = Mathf.Max(unit.frozenHeart, data.overclockFrozenHeartCost);
            if (SrpSkills.UsesCharges(data) && data.overclockChargeRestore > 0)
            {
                runtime.chargesRemaining = Mathf.Max(0, data.maxCharges - 1);
                runtime.chargeRecoveryRemaining = Mathf.Max(1, data.chargeRecoveryTurns);
                UpdateHud();
                return true;
            }
            if (data.overclockCooldownReduction > 0)
            {
                runtime.cooldownRemaining = Mathf.Max(1, data.overclockCooldownReduction);
                UpdateHud();
                return true;
            }
        }
        return false;
    }

    public bool TestOverclockSelectedSkill()
    {
        return TryOverclockSelectedSkill(false);
    }

    public bool TestPrepareSelectedUnitForReload()
    {
        var unit = _selectedId.HasValue ? GetUnit(_selectedId.Value) : null;
        if (unit == null || !unit.UsesAmmo)
            return false;
        unit.ammo = Mathf.Max(0, unit.maxAmmo - 1);
        unit.actionPoints = Mathf.Max(1, unit.actionPoints);
        UpdateHud();
        return true;
    }

    public bool TestReloadSelectedUnit()
    {
        return TryReloadSelectedUnit(false);
    }

    public bool TestPrepareSelectedUnitForCover()
    {
        var unit = _selectedId.HasValue ? GetUnit(_selectedId.Value) : null;
        if (unit == null || _state == null)
            return false;
        unit.actionPoints = Mathf.Max(1, unit.actionPoints);
        unit.ClearCover();
        if (_state.HasAdjacentCover(unit))
        {
            UpdateHud();
            return true;
        }

        foreach (var other in _state.Units)
        {
            if (other == null || other.eliminated || other.id == unit.id)
                continue;
            other.eliminated = true;
        }
        for (int y = 0; y < _state.Height; y++)
        for (int x = 0; x < _state.Width; x++)
        {
            if (!_state.CanStandAt(unit, x, y, unit.id))
                continue;
            unit.anchorX = x;
            unit.anchorY = y;
            if (_state.HasAdjacentCover(unit))
            {
                _state.RebuildEngagements();
                RefreshActiveHighlights(unit);
                UpdateHud();
                return true;
            }
        }
        return false;
    }

    public bool TestTakeCoverSelectedUnit()
    {
        return TryTakeCoverSelectedUnit(false);
    }

    public bool TestPrepareSelectedUnitForInteraction()
    {
        var unit = _selectedId.HasValue ? GetUnit(_selectedId.Value) : null;
        if (unit == null || _state == null)
            return false;
        unit.actionPoints = Mathf.Max(1, unit.actionPoints);
        if (_state.TryGetAdjacentInteraction(unit, out _))
        {
            UpdateHud();
            return true;
        }

        foreach (var point in _state.InteractionPoints)
            point.activated = false;
        foreach (var other in _state.Units)
        {
            if (other == null || other.eliminated || other.id == unit.id)
                continue;
            other.eliminated = true;
        }
        for (int y = 0; y < _state.Height; y++)
        for (int x = 0; x < _state.Width; x++)
        {
            if (!_state.CanStandAt(unit, x, y, unit.id))
                continue;
            unit.anchorX = x;
            unit.anchorY = y;
            if (_state.TryGetAdjacentInteraction(unit, out _))
            {
                _state.RebuildEngagements();
                RefreshActiveHighlights(unit);
                UpdateHud();
                return true;
            }
        }
        return false;
    }

    public bool TestInteractSelectedUnit()
    {
        return TryInteractSelectedUnit(false);
    }
#endif
}

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
    GameObject _skillListPanel;
    readonly List<GameObject> _skillListButtons = new List<GameObject>();
    TextMeshProUGUI _txtLogToggleLabel;
    GameObject _logBody;
    ScrollRect _logScrollRect;
    RectTransform _logContent;
    bool _logVisible = true;
    bool _logScrollPending;
    GameObject _tooltipGo;
    TextMeshProUGUI _tooltipText;
    Canvas _hudCanvas;

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

        _txtTurn = MakeLabel(panel.transform, "TurnInfo", 28, new Color(1f, 0.9f, 0.5f), 40);
        MakeSeparator(panel.transform);
        _txtStatus = MakeLabel(panel.transform, "Status", 22, new Color(0.75f, 0.9f, 1f), 64);
        MakeSeparator(panel.transform);
        _txtUnit = MakeLabel(panel.transform, "UnitInfo", 20, Color.white, 120);
        MakeSeparator(panel.transform);
        _btnUseSkill = MakeButton(panel.transform, "스킬 사용", OnShowSkillList, 60);
        _btnUseSkill.GetComponent<Image>().color = new Color(0.40f, 0.22f, 0.55f, 0.9f);
        _btnCancelSkill = MakeButton(panel.transform, "스킬 취소", () => CancelSkillTargeting(), 48, 20);
        _btnCancelSkill.GetComponent<Image>().color = new Color(0.55f, 0.25f, 0.20f, 0.9f);

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

        foreach (var go in _skillListButtons)
            if (go != null) Destroy(go);
        _skillListButtons.Clear();

        bool anySkill = false;
        for (int i = 0; i < u.skillRuntimes.Count; i++)
        {
            var sr = u.skillRuntimes[i];
            if (!_state.SkillLookup.TryGetValue(sr.skillId, out var data)) continue;
            if (data.skillType != SrpSkillType.Active) continue;

            anySkill = true;
            bool usable = SrpSkills.CanUseActiveSkill(data, sr) && !u.hasUsedSkillThisActivation;
            var capturedData = data;
            var capturedRuntime = sr;

            var btnGo = new GameObject("SkillBtn_" + data.id, typeof(RectTransform));
            btnGo.transform.SetParent(_skillListPanel.transform, false);
            btnGo.AddComponent<LayoutElement>().minHeight = 48f;
            btnGo.AddComponent<Image>().color = usable
                ? new Color(0.30f, 0.18f, 0.50f, 0.9f)
                : new Color(0.20f, 0.20f, 0.25f, 0.6f);
            var btn = btnGo.AddComponent<Button>();
            btn.interactable = usable;
            btn.onClick.AddListener(() =>
            {
                HideTooltip();
                _skillListPanel.SetActive(false);
                BeginSkillTargeting(capturedData, capturedRuntime);
            });

            var lblGo = new GameObject("Lbl", typeof(RectTransform));
            lblGo.transform.SetParent(btnGo.transform, false);
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(8, 0);
            lrt.offsetMax = new Vector2(-8, 0);
            var tx = lblGo.AddComponent<TextMeshProUGUI>();
            tx.fontSize = 18;
            tx.color = usable ? Color.white : new Color(0.6f, 0.6f, 0.6f);
            string cdText = sr.cooldownRemaining > 0 ? $" (CD:{sr.cooldownRemaining})" : "";
            tx.text = $"{data.displayName}{cdText}";
            tx.alignment = TextAlignmentOptions.MidlineLeft;
            tx.overflowMode = TextOverflowModes.Ellipsis;
            tx.enableWordWrapping = true;

            string fullTooltip = $"<b>{data.displayName}</b>{cdText}\n{data.description}";
            if (data.endsActivation) fullTooltip += "\n(사용 후 활성화 종료)";
            AddTooltipTrigger(btnGo, fullTooltip);

            _skillListButtons.Add(btnGo);
        }

        if (!anySkill)
        {
            var noSkill = new GameObject("NoSkill", typeof(RectTransform));
            noSkill.transform.SetParent(_skillListPanel.transform, false);
            noSkill.AddComponent<LayoutElement>().minHeight = 36f;
            var nst = noSkill.AddComponent<TextMeshProUGUI>();
            nst.text = "사용 가능한 액티브 스킬 없음";
            nst.fontSize = 18;
            nst.color = new Color(0.6f, 0.6f, 0.6f);
            nst.alignment = TextAlignmentOptions.Center;
            _skillListButtons.Add(noSkill);
        }

        _skillListPanel.SetActive(true);
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
        _tooltipText.enableWordWrapping = true;
        _tooltipText.richText = true;
        _tooltipText.overflowMode = TextOverflowModes.Overflow;

        _tooltipGo.AddComponent<CanvasGroup>().blocksRaycasts = false;
        _tooltipGo.SetActive(false);
    }

    void AddTooltipTrigger(GameObject target, string text)
    {
        var et = target.AddComponent<EventTrigger>();

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
        _log.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
        while (_log.Count > MaxLogLines) _log.RemoveAt(0);

        if (_txtLog != null)
        {
            var sb = new StringBuilder();
            foreach (var line in _log)
                sb.AppendLine(line);
            _txtLog.text = sb.ToString();
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
        else if (_phase == Phase.SelectingSkillTarget)
        {
            string skillName = _pendingSkillData != null ? _pendingSkillData.displayName : "?";
            st = $"스킬 대상 선택: {skillName}\n보라색 타일을 클릭 (다른 곳 = 취소)";
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
            {
                var sb2 = new System.Text.StringBuilder();
                sb2.AppendLine($"{u.displayName} (P{u.owner})");
                sb2.AppendLine($"HP {u.hp}/{u.maxHp}  AP {u.ap}/{u.maxAp}");
                sb2.AppendLine($"PG {u.posture}/{u.maxPosture}");
                sb2.Append($"그로기:{u.groggy}  FH:{u.frozenHeart}");
                if (u.skillIds.Count > 0)
                {
                    sb2.Append("\n스킬:");
                    foreach (var sid in u.skillIds)
                    {
                        if (_state.SkillLookup.TryGetValue(sid, out var sd))
                            sb2.Append($" {sd.displayName}");
                        else
                            sb2.Append($" {sid}");
                    }
                }
                _txtUnit.text = sb2.ToString();
            }
        }
        else
            _txtUnit.text = "— 미선택 —";

        bool unitActive = !_gameOver && _phase == Phase.UnitActive;
        if (_btnSkipAttack != null)
            _btnSkipAttack.interactable = unitActive;
        if (_btnEndTurn != null)
            _btnEndTurn.interactable = !_gameOver && _phase == Phase.Idle;
        if (_btnUndo != null)
            _btnUndo.interactable = _undo.Count > 0 && !_gameOver;
        if (_btnUseSkill != null)
            _btnUseSkill.interactable = unitActive;
        if (_btnCancelSkill != null)
            _btnCancelSkill.interactable = !_gameOver && _phase == Phase.SelectingSkillTarget;

        if (_skillListPanel != null && _phase != Phase.UnitActive && _phase != Phase.SelectingSkillTarget)
            _skillListPanel.SetActive(false);
    }
}

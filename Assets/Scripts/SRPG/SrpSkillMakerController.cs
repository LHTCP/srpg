using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SrpSkillMakerController : MonoBehaviour
{
    // ── 상태 ──────────────────────────────────────────────────────────────────

    List<SrpSkillData> _skills = new List<SrpSkillData>();
    int _selectedIndex = -1;

    // ── UI 참조 ───────────────────────────────────────────────────────────────

    RectTransform _listContent;
    readonly List<GameObject> _listItems = new List<GameObject>();

    TMP_InputField _fldId;
    TMP_InputField _fldName;
    TMP_InputField _fldDesc;
    TMP_InputField _fldRange;
    TMP_InputField _fldArea;
    TMP_InputField _fldCooldown;
    TMP_InputField _fldMaxCharges;
    TMP_InputField _fldChargeRecoveryTurns;
    TMP_InputField _fldOverclockFrozenHeartCost;
    TMP_InputField _fldOverclockCooldownReduction;
    TMP_InputField _fldOverclockChargeRestore;
    TMP_InputField _fldOverclockPowerBonus;
    Toggle _togEndsActivation;
    Toggle _togIsParryable;
    Toggle _togRequiresParryTelegraph;
    TMP_Dropdown _ddType;
    TMP_Dropdown _ddTrigger;
    TMP_Dropdown _ddTarget;

    RectTransform _effectListContent;
    readonly List<GameObject> _effectItems = new List<GameObject>();

    TextMeshProUGUI _txtStatus;

    // ── 색상 ──────────────────────────────────────────────────────────────────

    static readonly Color PanelBg      = new Color(0.08f, 0.10f, 0.14f, 0.92f);
    static readonly Color DarkBg       = new Color(0.05f, 0.06f, 0.09f, 1f);
    static readonly Color FieldBg      = new Color(0.12f, 0.14f, 0.18f, 0.95f);
    static readonly Color BtnNormal    = new Color(0.22f, 0.33f, 0.50f, 0.90f);
    static readonly Color BtnDanger    = new Color(0.60f, 0.22f, 0.18f, 0.90f);
    static readonly Color BtnGreen     = new Color(0.18f, 0.50f, 0.22f, 0.95f);
    static readonly Color SelColor     = new Color(0.25f, 0.60f, 0.30f, 0.95f);
    static readonly Color UnselColor   = new Color(0.16f, 0.20f, 0.28f, 0.85f);
    static readonly Color AccentYellow = new Color(1f, 0.92f, 0.45f);

    static readonly string[] StatOptions = { "self", "hp", "ap", "attackPower", "moveRange", "attackRange", "posture" };

    // ── 생명주기 ──────────────────────────────────────────────────────────────

    void Awake()
    {
        EnsureEventSystem();
        SrpFontWarmup.Warmup();
        LoadData();
        BuildUi();
        RefreshList();
        if (_skills.Count > 0) SelectSkill(0);
    }

    void LoadData()
    {
        var arr = SrpDataIO.LoadSkillsOrDefault();
        _skills = new List<SrpSkillData>(arr);
    }

    // ── UI 생성 ───────────────────────────────────────────────────────────────

    void BuildUi()
    {
        var canvasGo = new GameObject("SkillMakerCanvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        MakeFullBg(canvasGo.transform);

        var title = MakeLabel(canvasGo.transform, "스킬 메이커", 36, AccentYellow);
        SetRect(title, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0f, -50f));

        BuildLeftPanel(canvasGo.transform);
        BuildRightPanel(canvasGo.transform);
        BuildBottomBar(canvasGo.transform);
    }

    void BuildLeftPanel(Transform root)
    {
        var panel = MakePanel(root, "LeftPanel", 360f);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 0.5f);
        rt.offsetMin = new Vector2(16f, 70f);
        rt.offsetMax = new Vector2(376f, -60f);

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 8f;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        MakeLabelInLayout(panel.transform, "스킬 목록", 24, Color.white, 32);

        var btnRow = MakeHRow(panel.transform, 44);
        MakeButtonInParent(btnRow.transform, "추가", OnAddSkill, BtnGreen, 0.5f);
        MakeButtonInParent(btnRow.transform, "삭제", OnDeleteSkill, BtnDanger, 0.5f);

        // ScrollRect
        var scrollGo = new GameObject("SkillListScroll", typeof(RectTransform));
        scrollGo.transform.SetParent(panel.transform, false);
        scrollGo.AddComponent<LayoutElement>().flexibleHeight = 1f;
        scrollGo.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.07f, 0.5f);
        var sr = scrollGo.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30f;

        var vpGo = new GameObject("Viewport", typeof(RectTransform));
        vpGo.transform.SetParent(scrollGo.transform, false);
        FillRect(vpGo.GetComponent<RectTransform>(), 0);
        vpGo.AddComponent<RectMask2D>();
        sr.viewport = vpGo.GetComponent<RectTransform>();

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(vpGo.transform, false);
        _listContent = contentGo.GetComponent<RectTransform>();
        _listContent.anchorMin = new Vector2(0f, 1f);
        _listContent.anchorMax = new Vector2(1f, 1f);
        _listContent.pivot     = new Vector2(0f, 1f);
        _listContent.offsetMin = Vector2.zero;
        _listContent.offsetMax = Vector2.zero;
        var cvlg = contentGo.AddComponent<VerticalLayoutGroup>();
        cvlg.spacing = 4f;
        cvlg.childControlHeight = true;
        cvlg.childControlWidth = true;
        cvlg.childForceExpandWidth = true;
        cvlg.childForceExpandHeight = false;
        var csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = _listContent;
    }

    void BuildRightPanel(Transform root)
    {
        var panel = MakePanel(root, "RightPanel", 0f);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(392f, 70f);
        rt.offsetMax = new Vector2(-16f, -60f);

        var scrollGo = new GameObject("EditScroll", typeof(RectTransform));
        scrollGo.transform.SetParent(panel.transform, false);
        FillRect(scrollGo.GetComponent<RectTransform>(), 0);
        var sr = scrollGo.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30f;

        var vpGo = new GameObject("Viewport", typeof(RectTransform));
        vpGo.transform.SetParent(scrollGo.transform, false);
        FillRect(vpGo.GetComponent<RectTransform>(), 0);
        vpGo.AddComponent<RectMask2D>();
        sr.viewport = vpGo.GetComponent<RectTransform>();

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(vpGo.transform, false);
        var crt = contentGo.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot     = new Vector2(0f, 1f);
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = Vector2.zero;
        var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 14, 14);
        vlg.spacing = 10f;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        var csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = crt;

        var form = contentGo.transform;
        MakeLabelInLayout(form, "스킬 편집", 26, AccentYellow, 36);
        MakeSep(form);

        _fldId   = MakeFieldRow(form, "ID", "skill_id");
        _fldName = MakeFieldRow(form, "표시 이름", "기사의 일격");
        _fldDesc = MakeFieldRow(form, "설명", "스킬 설명...");

        MakeSep(form);
        _ddType    = MakeDropdown(form, "스킬 유형", new[] { "Active", "Passive" });
        _ddTrigger = MakeDropdown(form, "트리거", new[] { "OnActivate", "OnTurnStart", "OnAttackHit", "OnTakeDamage" });
        _ddTarget  = MakeDropdown(form, "대상 유형", new[] { "None", "Self", "SingleEnemy", "SingleAlly", "AreaEnemy", "AreaAlly" });

        MakeSep(form);
        _fldRange    = MakeFieldRow(form, "사거리", "1", TMP_InputField.ContentType.IntegerNumber);
        _fldArea     = MakeFieldRow(form, "범위 크기", "0", TMP_InputField.ContentType.IntegerNumber);
        _fldCooldown = MakeFieldRow(form, "쿨다운(턴)", "0", TMP_InputField.ContentType.IntegerNumber);
        _fldMaxCharges = MakeFieldRow(form, "최대 충전", "0", TMP_InputField.ContentType.IntegerNumber);
        _fldChargeRecoveryTurns = MakeFieldRow(form, "충전 회복(라운드)", "1", TMP_InputField.ContentType.IntegerNumber);
        _fldOverclockFrozenHeartCost = MakeFieldRow(form, "오버클럭 FH 비용", "0", TMP_InputField.ContentType.IntegerNumber);
        _fldOverclockCooldownReduction = MakeFieldRow(form, "오버클럭 CD 단축", "0", TMP_InputField.ContentType.IntegerNumber);
        _fldOverclockChargeRestore = MakeFieldRow(form, "오버클럭 충전 복구", "0", TMP_InputField.ContentType.IntegerNumber);
        _fldOverclockPowerBonus = MakeFieldRow(form, "오버클럭 위력 보너스", "0", TMP_InputField.ContentType.IntegerNumber);
        _togEndsActivation = MakeToggleRow(form, "사용 후 활성화 종료 (공격 대체)");
        _togIsParryable = MakeToggleRow(form, "패링 가능 공격");
        _togRequiresParryTelegraph = MakeToggleRow(form, "패링 텔레그래프 필요");

        MakeSep(form);
        MakeLabelInLayout(form, "효과 목록", 22, new Color(0.8f, 0.9f, 1f), 30);

        var effBtnRow = MakeHRow(form, 40);
        MakeButtonInParent(effBtnRow.transform, "효과 추가", OnAddEffect, BtnGreen, 0.5f);
        MakeButtonInParent(effBtnRow.transform, "마지막 삭제", OnRemoveEffect, BtnDanger, 0.5f);

        var effScrollGo = new GameObject("EffectListScroll", typeof(RectTransform));
        effScrollGo.transform.SetParent(form, false);
        effScrollGo.AddComponent<LayoutElement>().minHeight = 200f;
        effScrollGo.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.07f, 0.4f);
        var effSr = effScrollGo.AddComponent<ScrollRect>();
        effSr.horizontal = false;
        effSr.vertical = true;
        effSr.movementType = ScrollRect.MovementType.Clamped;
        effSr.scrollSensitivity = 30f;

        var effVp = new GameObject("Viewport", typeof(RectTransform));
        effVp.transform.SetParent(effScrollGo.transform, false);
        FillRect(effVp.GetComponent<RectTransform>(), 0);
        effVp.AddComponent<RectMask2D>();
        effSr.viewport = effVp.GetComponent<RectTransform>();

        var effContent = new GameObject("Content", typeof(RectTransform));
        effContent.transform.SetParent(effVp.transform, false);
        _effectListContent = effContent.GetComponent<RectTransform>();
        _effectListContent.anchorMin = new Vector2(0f, 1f);
        _effectListContent.anchorMax = new Vector2(1f, 1f);
        _effectListContent.pivot     = new Vector2(0f, 1f);
        _effectListContent.offsetMin = Vector2.zero;
        _effectListContent.offsetMax = Vector2.zero;
        var evlg = effContent.AddComponent<VerticalLayoutGroup>();
        evlg.spacing = 6f;
        evlg.childControlHeight = true;
        evlg.childControlWidth = true;
        evlg.childForceExpandWidth = true;
        evlg.childForceExpandHeight = false;
        evlg.padding = new RectOffset(6, 6, 6, 6);
        var ecsf = effContent.AddComponent<ContentSizeFitter>();
        ecsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        effSr.content = _effectListContent;
    }

    void BuildBottomBar(Transform root)
    {
        var bar = new GameObject("BottomBar", typeof(RectTransform));
        bar.transform.SetParent(root, false);
        var rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.offsetMin = new Vector2(16f, 10f);
        rt.offsetMax = new Vector2(-16f, 60f);

        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        MakeButtonInParent(bar.transform, "저장", OnSave, BtnGreen, 1f);

        _txtStatus = MakeLabelInLayout(bar.transform, "", 20, new Color(0.7f, 1f, 0.7f), 40);
        _txtStatus.alignment = TextAlignmentOptions.Center;

        MakeButtonInParent(bar.transform, "로비로 돌아가기", OnReturnToLobby, BtnNormal, 1f);
    }

    // ── 데이터 ↔ UI ──────────────────────────────────────────────────────────

    void SelectSkill(int index)
    {
        ApplyFromUi();
        _selectedIndex = index;
        RefreshListHighlight();
        LoadToUi();
    }

    void LoadToUi()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _skills.Count) return;
        var s = _skills[_selectedIndex];
        _fldId.text       = s.id ?? "";
        _fldName.text     = s.displayName ?? "";
        _fldDesc.text     = s.description ?? "";
        _fldRange.text    = s.range.ToString();
        _fldArea.text     = s.areaSize.ToString();
        _fldCooldown.text = s.cooldown.ToString();
        _fldMaxCharges.text = s.maxCharges.ToString();
        _fldChargeRecoveryTurns.text = s.chargeRecoveryTurns.ToString();
        _fldOverclockFrozenHeartCost.text = s.overclockFrozenHeartCost.ToString();
        _fldOverclockCooldownReduction.text = s.overclockCooldownReduction.ToString();
        _fldOverclockChargeRestore.text = s.overclockChargeRestore.ToString();
        _fldOverclockPowerBonus.text = s.overclockPowerBonus.ToString();
        _togEndsActivation.isOn = s.endsActivation;
        _togIsParryable.isOn = s.isParryable;
        _togRequiresParryTelegraph.isOn = s.requiresParryTelegraph;
        _ddType.value    = (int)s.skillType;
        _ddTrigger.value = (int)s.trigger;
        _ddTarget.value  = (int)s.targetType;
        RefreshEffectList(s);
    }

    void ApplyFromUi()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _skills.Count) return;
        var s = _skills[_selectedIndex];
        s.id              = _fldId.text.Trim();
        s.displayName     = _fldName.text.Trim();
        s.description     = _fldDesc.text.Trim();
        int.TryParse(_fldRange.text, out s.range);
        int.TryParse(_fldArea.text, out s.areaSize);
        int.TryParse(_fldCooldown.text, out s.cooldown);
        int.TryParse(_fldMaxCharges.text, out s.maxCharges);
        int.TryParse(_fldChargeRecoveryTurns.text, out s.chargeRecoveryTurns);
        int.TryParse(_fldOverclockFrozenHeartCost.text, out s.overclockFrozenHeartCost);
        int.TryParse(_fldOverclockCooldownReduction.text, out s.overclockCooldownReduction);
        int.TryParse(_fldOverclockChargeRestore.text, out s.overclockChargeRestore);
        int.TryParse(_fldOverclockPowerBonus.text, out s.overclockPowerBonus);
        s.endsActivation = _togEndsActivation.isOn;
        s.isParryable = _togIsParryable.isOn;
        s.requiresParryTelegraph = _togRequiresParryTelegraph.isOn;
        s.skillType  = (SrpSkillType)_ddType.value;
        s.trigger    = (SrpSkillTrigger)_ddTrigger.value;
        s.targetType = (SrpTargetType)_ddTarget.value;
        ApplyEffectsFromUi(s);
    }

    // ── 효과 목록 ─────────────────────────────────────────────────────────────

    void RefreshEffectList(SrpSkillData s)
    {
        foreach (var go in _effectItems)
            if (go != null) Destroy(go);
        _effectItems.Clear();

        if (s.effects == null) s.effects = Array.Empty<SrpSkillEffect>();
        foreach (var eff in s.effects)
            _effectItems.Add(CreateEffectRow(eff));
    }

    GameObject CreateEffectRow(SrpSkillEffect eff)
    {
        var row = new GameObject("EffRow", typeof(RectTransform));
        row.transform.SetParent(_effectListContent, false);
        row.AddComponent<LayoutElement>().minHeight = 200f;
        row.AddComponent<Image>().color = new Color(0.10f, 0.12f, 0.16f, 0.8f);
        var vlg = row.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(8, 8, 6, 6);
        vlg.spacing = 6f;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        var typeRow = MakeHRow(row.transform, 40);
        MakeLabelInLayout(typeRow.transform, "효과 유형", 18, new Color(0.75f, 0.85f, 0.95f), 36);
        var typeNames = Enum.GetNames(typeof(SrpEffectType));
        var ddType = MakeDropdownInParent(typeRow.transform, "", typeNames, (int)eff.type);
        ddType.gameObject.name = "effType";

        var statRow = MakeHRow(row.transform, 40);
        MakeLabelInLayout(statRow.transform, "대상 스탯", 18, new Color(0.75f, 0.85f, 0.95f), 36);
        int statIdx = Mathf.Max(0, Array.IndexOf(StatOptions, eff.stat ?? "self"));
        var ddStat = MakeDropdownInParent(statRow.transform, "", StatOptions, statIdx);
        ddStat.gameObject.name = "effStat";

        var valRow = MakeHRow(row.transform, 40);
        MakeLabelInLayout(valRow.transform, "값", 18, new Color(0.75f, 0.85f, 0.95f), 36);
        var valFld = MakeInputFieldInParent(valRow.transform, "0", TMP_InputField.ContentType.IntegerNumber);
        valFld.text = eff.value.ToString();
        valFld.gameObject.name = "effVal";

        var durRow = MakeHRow(row.transform, 40);
        MakeLabelInLayout(durRow.transform, "지속(턴)", 18, new Color(0.75f, 0.85f, 0.95f), 36);
        var durFld = MakeInputFieldInParent(durRow.transform, "0", TMP_InputField.ContentType.IntegerNumber);
        durFld.text = eff.duration.ToString();
        durFld.gameObject.name = "effDur";

        return row;
    }

    void ApplyEffectsFromUi(SrpSkillData s)
    {
        var effects = new List<SrpSkillEffect>();
        foreach (var row in _effectItems)
        {
            if (row == null) continue;
            var eff = new SrpSkillEffect();
            var ddType = FindInDescendants<TMP_Dropdown>(row.transform, "effType");
            if (ddType != null) eff.type = (SrpEffectType)ddType.value;
            var ddStat = FindInDescendants<TMP_Dropdown>(row.transform, "effStat");
            if (ddStat != null && ddStat.value >= 0 && ddStat.value < StatOptions.Length)
                eff.stat = StatOptions[ddStat.value];
            var valFld = FindInDescendants<TMP_InputField>(row.transform, "effVal");
            if (valFld != null) int.TryParse(valFld.text, out eff.value);
            var durFld = FindInDescendants<TMP_InputField>(row.transform, "effDur");
            if (durFld != null) int.TryParse(durFld.text, out eff.duration);
            effects.Add(eff);
        }
        s.effects = effects.ToArray();
    }

    static T FindInDescendants<T>(Transform root, string name) where T : Component
    {
        foreach (var t in root.GetComponentsInChildren<T>(true))
            if (t.gameObject.name == name) return t;
        return null;
    }

    // ── 목록 ──────────────────────────────────────────────────────────────────

    void RefreshList()
    {
        foreach (var go in _listItems)
            if (go != null) Destroy(go);
        _listItems.Clear();

        for (int i = 0; i < _skills.Count; i++)
        {
            int idx = i;
            var s = _skills[i];
            var item = new GameObject("Item_" + i, typeof(RectTransform));
            item.transform.SetParent(_listContent, false);
            item.AddComponent<LayoutElement>().minHeight = 48f;
            item.AddComponent<Image>().color = UnselColor;
            var btn = item.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectSkill(idx));

            var txtGo = new GameObject("Lbl", typeof(RectTransform));
            txtGo.transform.SetParent(item.transform, false);
            FillRect(txtGo.GetComponent<RectTransform>(), 8);
            var tx = txtGo.AddComponent<TextMeshProUGUI>();
            tx.fontSize = 20;
            tx.color = Color.white;
            string label = string.IsNullOrEmpty(s.displayName) ? s.id : s.displayName;
            string typeTag = s.skillType == SrpSkillType.Active ? "[A]" : "[P]";
            string chargeTag = s.maxCharges > 0 ? $" CH:{s.maxCharges}" : string.Empty;
            string parryTag = s.isParryable ? " Parry" : string.Empty;
            string overclockTag = s.overclockFrozenHeartCost > 0
                ? (s.overclockPowerBonus > 0 ? $" OC+{s.overclockPowerBonus}" : " OC")
                : string.Empty;
            tx.text = $"{typeTag} {label}{chargeTag}{parryTag}{overclockTag}";
            tx.alignment = TextAlignmentOptions.MidlineLeft;

            _listItems.Add(item);
        }
        RefreshListHighlight();
    }

    void RefreshListHighlight()
    {
        for (int i = 0; i < _listItems.Count; i++)
        {
            var img = _listItems[i].GetComponent<Image>();
            if (img != null)
                img.color = i == _selectedIndex ? SelColor : UnselColor;
        }
    }

    // ── 핸들러 ────────────────────────────────────────────────────────────────

    void OnAddSkill()
    {
        ApplyFromUi();
        var s = new SrpSkillData
        {
            id = "new_skill_" + _skills.Count,
            displayName = "새 스킬",
            description = "",
            skillType = SrpSkillType.Active,
            trigger = SrpSkillTrigger.OnActivate,
            targetType = SrpTargetType.SingleEnemy,
            range = 1,
            endsActivation = true,
            effects = Array.Empty<SrpSkillEffect>(),
        };
        _skills.Add(s);
        RefreshList();
        SelectSkill(_skills.Count - 1);
    }

    void OnDeleteSkill()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _skills.Count) return;
        _skills.RemoveAt(_selectedIndex);
        if (_selectedIndex >= _skills.Count) _selectedIndex = _skills.Count - 1;
        RefreshList();
        if (_selectedIndex >= 0) LoadToUi();
    }

    void OnAddEffect()
    {
        if (_selectedIndex < 0) return;
        var eff = new SrpSkillEffect { type = SrpEffectType.Damage, value = 10 };
        _effectItems.Add(CreateEffectRow(eff));
    }

    void OnRemoveEffect()
    {
        if (_effectItems.Count == 0) return;
        var last = _effectItems[_effectItems.Count - 1];
        _effectItems.RemoveAt(_effectItems.Count - 1);
        if (last != null) Destroy(last);
    }

    void OnSave()
    {
        ApplyFromUi();
        SrpDataIO.SaveSkills(_skills.ToArray());
        _txtStatus.text  = "저장 완료!";
        _txtStatus.color = new Color(0.5f, 1f, 0.5f);
    }

    void OnReturnToLobby()
    {
        ApplyFromUi();
        SrpDataIO.SaveSkills(_skills.ToArray());
        SrpGameSettings.ReturnToLobby();
    }

    // ── UI 헬퍼 ───────────────────────────────────────────────────────────────

    static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }

    static void MakeFullBg(Transform parent)
    {
        var go = new GameObject("Bg", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        FillRect(go.GetComponent<RectTransform>(), 0);
        go.AddComponent<Image>().color = DarkBg;
    }

    static GameObject MakePanel(Transform parent, string name, float width)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = PanelBg;
        return go;
    }

    static TextMeshProUGUI MakeLabel(Transform parent, string text, int size, Color color)
    {
        var go = new GameObject("Lbl", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    static TextMeshProUGUI MakeLabelInLayout(Transform parent, string text, int size, Color color, float minH)
    {
        var go = new GameObject("Lbl", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().minHeight = minH;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.TopLeft;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    static void MakeSep(Transform parent)
    {
        var go = new GameObject("Sep", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().minHeight = 2;
        go.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.14f);
    }

    static GameObject MakeHRow(Transform parent, float height)
    {
        var go = new GameObject("Row", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
        le.flexibleHeight = 0f;
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        return go;
    }

    static Button MakeButtonInParent(Transform parent, string label,
        UnityEngine.Events.UnityAction onClick, Color bgColor, float flex)
    {
        var go = new GameObject("Btn_" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = flex;
        go.AddComponent<Image>().color = bgColor;
        var b = go.AddComponent<Button>();
        b.onClick.AddListener(onClick);

        var txtGo = new GameObject("Label", typeof(RectTransform));
        txtGo.transform.SetParent(go.transform, false);
        FillRect(txtGo.GetComponent<RectTransform>(), 0);
        var tx = txtGo.AddComponent<TextMeshProUGUI>();
        tx.fontSize = 22;
        tx.color = Color.white;
        tx.alignment = TextAlignmentOptions.Center;
        tx.text = label;
        return b;
    }

    TMP_InputField MakeFieldRow(Transform parent, string label, string placeholder,
        TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard)
    {
        var row = MakeHRow(parent, 44);
        MakeLabelInLayout(row.transform, label, 20, new Color(0.75f, 0.85f, 0.95f), 40);
        return MakeInputFieldInParent(row.transform, placeholder, contentType);
    }

    static TMP_InputField MakeSmallField(Transform parent, string placeholder, string value)
    {
        var fld = MakeInputFieldInParent(parent, placeholder, TMP_InputField.ContentType.Standard);
        fld.text = value;
        return fld;
    }

    static TMP_InputField MakeInputFieldInParent(Transform parent, string placeholder,
        TMP_InputField.ContentType contentType)
    {
        var go = new GameObject("Field", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().flexibleWidth = 1f;
        go.AddComponent<Image>().color = FieldBg;
        var field = go.AddComponent<TMP_InputField>();
        field.contentType = contentType;

        var areaGo = new GameObject("Text Area", typeof(RectTransform));
        areaGo.transform.SetParent(go.transform, false);
        FillRect(areaGo.GetComponent<RectTransform>(), 6);
        areaGo.AddComponent<RectMask2D>();
        field.textViewport = areaGo.GetComponent<RectTransform>();

        var phGo = new GameObject("Placeholder", typeof(RectTransform));
        phGo.transform.SetParent(areaGo.transform, false);
        FillRect(phGo.GetComponent<RectTransform>(), 0);
        var phTx = phGo.AddComponent<TextMeshProUGUI>();
        phTx.fontSize = 18;
        phTx.color = new Color(0.5f, 0.5f, 0.55f);
        phTx.fontStyle = FontStyles.Italic;
        phTx.text = placeholder;
        field.placeholder = phTx;

        var txGo = new GameObject("Text", typeof(RectTransform));
        txGo.transform.SetParent(areaGo.transform, false);
        FillRect(txGo.GetComponent<RectTransform>(), 0);
        var inputTx = txGo.AddComponent<TextMeshProUGUI>();
        inputTx.fontSize = 18;
        inputTx.color = Color.white;
        field.textComponent = inputTx;
        field.caretWidth = 2;
        field.customCaretColor = true;
        field.caretColor = Color.white;
        field.selectionColor = new Color(0.3f, 0.5f, 0.9f, 0.5f);
        field.enabled = false;
        field.enabled = true;
        return field;
    }

    TMP_Dropdown MakeDropdown(Transform parent, string label, string[] options)
    {
        var row = MakeHRow(parent, 44);
        MakeLabelInLayout(row.transform, label, 20, new Color(0.75f, 0.85f, 0.95f), 40);
        return MakeDropdownInParent(row.transform, "", options, 0);
    }

    static TMP_Dropdown MakeDropdownInParent(Transform parent, string label, string[] options, int value)
    {
        var go = new GameObject("Dropdown", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().flexibleWidth = 1f;
        go.AddComponent<Image>().color = FieldBg;

        var dd = go.AddComponent<TMP_Dropdown>();
        dd.ClearOptions();
        var opts = new List<TMP_Dropdown.OptionData>();
        foreach (var o in options)
            opts.Add(new TMP_Dropdown.OptionData(o));
        dd.AddOptions(opts);

        var captionGo = new GameObject("Label", typeof(RectTransform));
        captionGo.transform.SetParent(go.transform, false);
        FillRect(captionGo.GetComponent<RectTransform>(), 8);
        var captionTx = captionGo.AddComponent<TextMeshProUGUI>();
        captionTx.fontSize = 18;
        captionTx.color = Color.white;
        dd.captionText = captionTx;

        // Template (최소 구조)
        var templateGo = new GameObject("Template", typeof(RectTransform));
        templateGo.transform.SetParent(go.transform, false);
        var trt = templateGo.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0f);
        trt.anchorMax = new Vector2(1f, 0f);
        trt.pivot     = new Vector2(0.5f, 1f);
        trt.sizeDelta = new Vector2(0f, 200f);
        templateGo.AddComponent<Image>().color = new Color(0.14f, 0.16f, 0.20f, 0.98f);
        var tsr = templateGo.AddComponent<ScrollRect>();
        tsr.horizontal = false;
        tsr.vertical = true;
        tsr.movementType = ScrollRect.MovementType.Clamped;

        var tVp = new GameObject("Viewport", typeof(RectTransform));
        tVp.transform.SetParent(templateGo.transform, false);
        FillRect(tVp.GetComponent<RectTransform>(), 0);
        tVp.AddComponent<RectMask2D>();
        tsr.viewport = tVp.GetComponent<RectTransform>();

        var tContent = new GameObject("Content", typeof(RectTransform));
        tContent.transform.SetParent(tVp.transform, false);
        var tcrt = tContent.GetComponent<RectTransform>();
        tcrt.anchorMin = new Vector2(0f, 1f);
        tcrt.anchorMax = new Vector2(1f, 1f);
        tcrt.pivot     = new Vector2(0f, 1f);
        tcrt.sizeDelta = Vector2.zero;
        var tVlg = tContent.AddComponent<VerticalLayoutGroup>();
        tVlg.childControlWidth = true;
        tVlg.childControlHeight = true;
        tVlg.childForceExpandWidth = true;
        tVlg.childForceExpandHeight = false;
        var tCsf = tContent.AddComponent<ContentSizeFitter>();
        tCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        tsr.content = tcrt;

        var itemGo = new GameObject("Item", typeof(RectTransform));
        itemGo.transform.SetParent(tContent.transform, false);
        itemGo.AddComponent<LayoutElement>().minHeight = 36f;
        itemGo.AddComponent<Image>().color = new Color(0.18f, 0.20f, 0.26f, 0.95f);
        var toggle = itemGo.AddComponent<Toggle>();
        toggle.isOn = true;

        var itemLbl = new GameObject("Item Label", typeof(RectTransform));
        itemLbl.transform.SetParent(itemGo.transform, false);
        FillRect(itemLbl.GetComponent<RectTransform>(), 6);
        var itemTx = itemLbl.AddComponent<TextMeshProUGUI>();
        itemTx.fontSize = 18;
        itemTx.color = Color.white;
        dd.itemText = itemTx;
        toggle.targetGraphic = itemGo.GetComponent<Image>();

        dd.template = trt;
        templateGo.SetActive(false);

        dd.value = value;
        dd.RefreshShownValue();
        return dd;
    }

    Toggle MakeToggleRow(Transform parent, string label)
    {
        var row = MakeHRow(parent, 40);
        row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;

        var togGo = new GameObject("Toggle", typeof(RectTransform));
        togGo.transform.SetParent(row.transform, false);
        var tle = togGo.AddComponent<LayoutElement>();
        tle.minWidth = 36f;
        tle.preferredWidth = 36f;
        tle.flexibleWidth = 0f;

        var bgImg = togGo.AddComponent<Image>();
        bgImg.color = FieldBg;
        var toggle = togGo.AddComponent<Toggle>();
        toggle.targetGraphic = bgImg;

        var checkGo = new GameObject("Check", typeof(RectTransform));
        checkGo.transform.SetParent(togGo.transform, false);
        var chkRt = checkGo.GetComponent<RectTransform>();
        chkRt.anchorMin = new Vector2(0.15f, 0.15f);
        chkRt.anchorMax = new Vector2(0.85f, 0.85f);
        chkRt.offsetMin = Vector2.zero;
        chkRt.offsetMax = Vector2.zero;
        var checkImg = checkGo.AddComponent<Image>();
        checkImg.color = new Color(0.4f, 0.9f, 0.5f);
        toggle.graphic = checkImg;

        var lblGo = new GameObject("Lbl", typeof(RectTransform));
        lblGo.transform.SetParent(row.transform, false);
        var lle = lblGo.AddComponent<LayoutElement>();
        lle.flexibleWidth = 1f;
        lle.minHeight = 36f;
        var lbl = lblGo.AddComponent<TextMeshProUGUI>();
        lbl.text = label;
        lbl.fontSize = 20;
        lbl.color = new Color(0.75f, 0.85f, 0.95f);
        lbl.alignment = TextAlignmentOptions.MidlineLeft;
        lbl.overflowMode = TextOverflowModes.Overflow;

        return toggle;
    }

    static void SetRect(Component c, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax)
    {
        var rt = c.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    static void FillRect(RectTransform rt, float padding)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
    }
}

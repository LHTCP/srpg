using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SrpUnitMakerController : MonoBehaviour
{
    // ── 상태 ──────────────────────────────────────────────────────────────────

    List<SrpUnitTemplateData> _units = new List<SrpUnitTemplateData>();
    SrpSkillData[] _allSkills = Array.Empty<SrpSkillData>();
    int _selectedIndex = -1;

    // ── UI 참조 ───────────────────────────────────────────────────────────────

    RectTransform _listContent;
    readonly List<GameObject> _listItems = new List<GameObject>();

    TMP_InputField _fldId;
    TMP_InputField _fldName;
    TMP_InputField _fldMoveRange;
    TMP_InputField _fldAtkRange;
    TMP_InputField _fldAtkPower;
    TMP_InputField _fldMaxAmmo;
    TMP_InputField _fldMaxHp;
    TMP_InputField _fldMaxActionPoints;
    TMP_InputField _fldMaxReactionPoints;
    TMP_InputField _fldMaxPg;
    TMP_InputField _fldSpeed;
    TMP_InputField _fldFrozenHeart;
    TMP_InputField _fldMaxSkills;
    Toggle _togBoss;
    Toggle _togLarge;
    Toggle _togParryUser;
    Toggle _togTank;
    TMP_Dropdown _ddWeaponClass;
    TMP_Dropdown _ddStance;
    TMP_Dropdown _ddFacing;
    TMP_InputField _fldFpW;
    TMP_InputField _fldFpH;
    GameObject _fpRow;

    RectTransform _skillListContent;
    readonly List<(GameObject go, Toggle toggle, string skillId)> _skillToggles
        = new List<(GameObject, Toggle, string)>();

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

    // ── 생명주기 ──────────────────────────────────────────────────────────────

    void Awake()
    {
        EnsureEventSystem();
        SrpFontWarmup.Warmup();
        LoadData();
        BuildUi();
        RefreshList();
        if (_units.Count > 0) SelectUnit(0);
    }

    void LoadData()
    {
        var arr = SrpDataIO.LoadUnitsOrDefault();
        _units = new List<SrpUnitTemplateData>(arr);
        _allSkills = SrpDataIO.LoadSkillsOrDefault();
    }

    // ── UI 생성 ───────────────────────────────────────────────────────────────

    void BuildUi()
    {
        var canvasGo = new GameObject("UnitMakerCanvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        MakeFullBg(canvasGo.transform);

        var title = MakeLabel(canvasGo.transform, "유닛 메이커", 36, AccentYellow);
        SetRect(title, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0f, -50f));

        BuildLeftPanel(canvasGo.transform);
        BuildRightPanel(canvasGo.transform);
        BuildBottomBar(canvasGo.transform);
    }

    void BuildLeftPanel(Transform root)
    {
        var panel = MakePanel(root, "LeftPanel");
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 0.5f);
        rt.offsetMin = new Vector2(16f, 70f);
        rt.offsetMax = new Vector2(360f, -60f);

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 8f;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        MakeLabelInLayout(panel.transform, "유닛 목록", 24, Color.white, 32);

        var btnRow = MakeHRow(panel.transform, 44);
        MakeButtonInParent(btnRow.transform, "추가", OnAddUnit, BtnGreen, 0.5f);
        MakeButtonInParent(btnRow.transform, "삭제", OnDeleteUnit, BtnDanger, 0.5f);

        var scrollGo = MakeScrollRect(panel.transform);
        _listContent = MakeScrollContent(scrollGo);
    }

    void BuildRightPanel(Transform root)
    {
        var panel = MakePanel(root, "RightPanel");
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(376f, 70f);
        rt.offsetMax = new Vector2(-16f, -60f);

        var outerScroll = MakeScrollRectFill(panel.transform);
        var content = MakeScrollContent(outerScroll);
        var vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 14, 14);

        var form = content.transform;
        MakeLabelInLayout(form, "유닛 편집", 26, AccentYellow, 36);
        MakeSep(form);

        _fldId   = MakeFieldRow(form, "ID", "unit_id");
        _fldName = MakeFieldRow(form, "표시 이름", "기사");

        MakeSep(form);
        MakeLabelInLayout(form, "스탯", 22, new Color(0.8f, 0.9f, 1f), 28);

        _fldMoveRange   = MakeFieldRow(form, "이동력", "5", TMP_InputField.ContentType.IntegerNumber);
        _fldAtkRange    = MakeFieldRow(form, "공격 사거리", "1", TMP_InputField.ContentType.IntegerNumber);
        _fldAtkPower    = MakeFieldRow(form, "공격력", "10", TMP_InputField.ContentType.IntegerNumber);
        _fldMaxAmmo     = MakeFieldRow(form, "최대 탄약(총기)", "0", TMP_InputField.ContentType.IntegerNumber);
        _fldMaxHp       = MakeFieldRow(form, "최대 HP", "40", TMP_InputField.ContentType.IntegerNumber);
        _fldMaxActionPoints = MakeFieldRow(form, "최대 AP(v2)", "2", TMP_InputField.ContentType.IntegerNumber);
        _fldMaxReactionPoints = MakeFieldRow(form, "최대 RP(v2)", "1", TMP_InputField.ContentType.IntegerNumber);
        _fldMaxPg = MakeFieldRow(form, "최대 PG(v2)", "18", TMP_InputField.ContentType.IntegerNumber);
        _fldSpeed = MakeFieldRow(form, "속도", "10", TMP_InputField.ContentType.IntegerNumber);
        _fldFrozenHeart = MakeFieldRow(form, "빙결된 심장(FH)", "0", TMP_InputField.ContentType.IntegerNumber);
        _fldMaxSkills   = MakeFieldRow(form, "최대 스킬 수", "4", TMP_InputField.ContentType.IntegerNumber);

        _ddWeaponClass = MakeDropdown(form, "무기 분류", Enum.GetNames(typeof(SrpWeaponClass)));
        _ddStance = MakeDropdown(form, "기본 태세", Enum.GetNames(typeof(SrpStance)));
        _ddFacing = MakeDropdown(form, "기본 방향", Enum.GetNames(typeof(SrpFacing)));

        MakeSep(form);
        MakeLabelInLayout(form, "태그", 22, new Color(0.8f, 0.9f, 1f), 28);
        var tagRow = MakeHRow(form, 40);
        _togBoss  = MakeToggleInRow(tagRow.transform, "Boss");
        _togLarge = MakeToggleInRow(tagRow.transform, "Large");
        var combatTagRow = MakeHRow(form, 40);
        _togParryUser = MakeToggleInRow(combatTagRow.transform, "ParryUser");
        _togTank = MakeToggleInRow(combatTagRow.transform, "Tank");

        _fpRow = MakeHRow(form, 44).gameObject;
        MakeLabelInLayout(_fpRow.transform, "풋프린트", 20, new Color(0.75f, 0.85f, 0.95f), 40);
        _fldFpW = MakeInputFieldInParent(_fpRow.transform, "가로", TMP_InputField.ContentType.IntegerNumber);
        _fldFpW.text = "1";
        MakeLabelInLayout(_fpRow.transform, "x", 20, new Color(0.75f, 0.85f, 0.95f), 40).alignment =
            TextAlignmentOptions.Center;
        _fldFpH = MakeInputFieldInParent(_fpRow.transform, "세로", TMP_InputField.ContentType.IntegerNumber);
        _fldFpH.text = "1";
        _fpRow.SetActive(false);
        _togLarge.onValueChanged.AddListener(on => {
            _fpRow.SetActive(on);
            if (!on) { _fldFpW.text = "1"; _fldFpH.text = "1"; }
        });

        MakeSep(form);
        MakeLabelInLayout(form, "스킬 할당", 22, new Color(0.8f, 0.9f, 1f), 28);

        var skillScrollGo = new GameObject("SkillListScroll", typeof(RectTransform));
        skillScrollGo.transform.SetParent(form, false);
        var sle = skillScrollGo.AddComponent<LayoutElement>();
        sle.minHeight = 180f;
        sle.flexibleHeight = 0.5f;
        skillScrollGo.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.07f, 0.4f);
        var ssr = skillScrollGo.AddComponent<ScrollRect>();
        ssr.horizontal = false;
        ssr.vertical = true;
        ssr.movementType = ScrollRect.MovementType.Clamped;
        ssr.scrollSensitivity = 30f;

        var svp = new GameObject("Viewport", typeof(RectTransform));
        svp.transform.SetParent(skillScrollGo.transform, false);
        FillRect(svp.GetComponent<RectTransform>(), 0);
        svp.AddComponent<RectMask2D>();
        ssr.viewport = svp.GetComponent<RectTransform>();

        var sc = new GameObject("Content", typeof(RectTransform));
        sc.transform.SetParent(svp.transform, false);
        _skillListContent = sc.GetComponent<RectTransform>();
        _skillListContent.anchorMin = new Vector2(0f, 1f);
        _skillListContent.anchorMax = new Vector2(1f, 1f);
        _skillListContent.pivot     = new Vector2(0f, 1f);
        _skillListContent.offsetMin = Vector2.zero;
        _skillListContent.offsetMax = Vector2.zero;
        var svlg = sc.AddComponent<VerticalLayoutGroup>();
        svlg.spacing = 4f;
        svlg.padding = new RectOffset(6, 6, 6, 6);
        svlg.childControlHeight = true;
        svlg.childControlWidth = true;
        svlg.childForceExpandWidth = true;
        svlg.childForceExpandHeight = false;
        var scsf = sc.AddComponent<ContentSizeFitter>();
        scsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ssr.content = _skillListContent;

        BuildSkillToggles();
    }

    void BuildSkillToggles()
    {
        foreach (var (go, _, _) in _skillToggles)
            if (go != null) Destroy(go);
        _skillToggles.Clear();

        foreach (var skill in _allSkills)
        {
            var row = new GameObject("SkillTog_" + skill.id, typeof(RectTransform));
            row.transform.SetParent(_skillListContent, false);
            row.AddComponent<LayoutElement>().minHeight = 36f;

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.padding = new RectOffset(6, 6, 0, 0);

            var togGo = new GameObject("Tog", typeof(RectTransform));
            togGo.transform.SetParent(row.transform, false);
            var tle = togGo.AddComponent<LayoutElement>();
            tle.minWidth = 30f;
            tle.preferredWidth = 30f;
            tle.flexibleWidth = 0f;
            var bgImg = togGo.AddComponent<Image>();
            bgImg.color = FieldBg;
            var toggle = togGo.AddComponent<Toggle>();
            toggle.targetGraphic = bgImg;

            var chkGo = new GameObject("Check", typeof(RectTransform));
            chkGo.transform.SetParent(togGo.transform, false);
            var chkRt = chkGo.GetComponent<RectTransform>();
            chkRt.anchorMin = new Vector2(0.15f, 0.15f);
            chkRt.anchorMax = new Vector2(0.85f, 0.85f);
            chkRt.offsetMin = chkRt.offsetMax = Vector2.zero;
            var chkImg = chkGo.AddComponent<Image>();
            chkImg.color = new Color(0.4f, 0.9f, 0.5f);
            toggle.graphic = chkImg;

            var lblGo = new GameObject("Lbl", typeof(RectTransform));
            lblGo.transform.SetParent(row.transform, false);
            lblGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var tx = lblGo.AddComponent<TextMeshProUGUI>();
            string typeTag = skill.skillType == SrpSkillType.Active ? "[A]" : "[P]";
            tx.text = $"{typeTag} {skill.displayName} ({skill.id})";
            tx.fontSize = 18;
            tx.color = Color.white;
            tx.alignment = TextAlignmentOptions.MidlineLeft;

            _skillToggles.Add((row, toggle, skill.id));
        }
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

    void SelectUnit(int index)
    {
        ApplyFromUi();
        _selectedIndex = index;
        RefreshListHighlight();
        LoadToUi();
    }

    void LoadToUi()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _units.Count) return;
        var u = _units[_selectedIndex];
        _fldId.text          = u.id ?? "";
        _fldName.text        = u.displayName ?? "";
        _fldMoveRange.text   = u.moveRange.ToString();
        _fldAtkRange.text    = u.attackRange.ToString();
        _fldAtkPower.text    = u.attackPower.ToString();
        _fldMaxAmmo.text     = u.maxAmmo.ToString();
        _fldMaxHp.text       = u.maxHp.ToString();
        _fldMaxActionPoints.text = u.maxActionPoints.ToString();
        _fldMaxReactionPoints.text = u.maxReactionPoints.ToString();
        _fldMaxPg.text = u.maxPg.ToString();
        _fldSpeed.text = u.speed.ToString();
        _fldFrozenHeart.text = u.frozenHeart.ToString();
        _fldMaxSkills.text   = u.maxSkills.ToString();
        _ddWeaponClass.value = (int)u.weaponClass;
        _ddStance.value = (int)u.stance;
        _ddFacing.value = (int)u.facing;
        _ddWeaponClass.RefreshShownValue();
        _ddStance.RefreshShownValue();
        _ddFacing.RefreshShownValue();
        _togBoss.isOn  = (u.tags & (int)SrpUnitTags.Boss) != 0;
        bool isLarge = (u.tags & (int)SrpUnitTags.Large) != 0;
        _togLarge.isOn = isLarge;
        _togParryUser.isOn = (u.tags & (int)SrpUnitTags.ParryUser) != 0;
        _togTank.isOn = (u.tags & (int)SrpUnitTags.Tank) != 0;
        _fldFpW.text = Mathf.Max(1, u.footprintWidth).ToString();
        _fldFpH.text = Mathf.Max(1, u.footprintHeight).ToString();
        _fpRow.SetActive(isLarge);

        var skillSet = new HashSet<string>(u.skillIds ?? Array.Empty<string>());
        foreach (var (_, toggle, skillId) in _skillToggles)
            toggle.isOn = skillSet.Contains(skillId);
    }

    void ApplyFromUi()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _units.Count) return;
        var u = _units[_selectedIndex];
        u.id          = _fldId.text.Trim();
        u.displayName = _fldName.text.Trim();
        int.TryParse(_fldMoveRange.text, out u.moveRange);
        int.TryParse(_fldAtkRange.text, out u.attackRange);
        int.TryParse(_fldAtkPower.text, out u.attackPower);
        int.TryParse(_fldMaxAmmo.text, out u.maxAmmo);
        int.TryParse(_fldMaxHp.text, out u.maxHp);
        int.TryParse(_fldMaxActionPoints.text, out u.maxActionPoints);
        int.TryParse(_fldMaxReactionPoints.text, out u.maxReactionPoints);
        int.TryParse(_fldMaxPg.text, out u.maxPg);
        int.TryParse(_fldSpeed.text, out u.speed);
        int.TryParse(_fldFrozenHeart.text, out u.frozenHeart);
        int.TryParse(_fldMaxSkills.text, out u.maxSkills);
        u.weaponClass = (SrpWeaponClass)_ddWeaponClass.value;
        u.stance = (SrpStance)_ddStance.value;
        u.facing = (SrpFacing)_ddFacing.value;
        SyncV2LegacyStats(u);

        u.tags = 0;
        if (_togBoss.isOn)  u.tags |= (int)SrpUnitTags.Boss;
        if (_togLarge.isOn) u.tags |= (int)SrpUnitTags.Large;
        if (_togParryUser.isOn) u.tags |= (int)SrpUnitTags.ParryUser;
        if (_togTank.isOn) u.tags |= (int)SrpUnitTags.Tank;
        int.TryParse(_fldFpW.text, out u.footprintWidth);
        int.TryParse(_fldFpH.text, out u.footprintHeight);
        u.footprintWidth  = Mathf.Max(1, u.footprintWidth);
        u.footprintHeight = Mathf.Max(1, u.footprintHeight);
        if (!_togLarge.isOn) { u.footprintWidth = 1; u.footprintHeight = 1; }

        var selected = new List<string>();
        foreach (var (_, toggle, skillId) in _skillToggles)
            if (toggle.isOn) selected.Add(skillId);
        if (u.maxSkills > 0 && selected.Count > u.maxSkills)
            selected.RemoveRange(u.maxSkills, selected.Count - u.maxSkills);
        u.skillIds = selected.ToArray();
    }

    // ── 목록 ──────────────────────────────────────────────────────────────────

    void RefreshList()
    {
        foreach (var go in _listItems)
            if (go != null) Destroy(go);
        _listItems.Clear();

        for (int i = 0; i < _units.Count; i++)
        {
            int idx = i;
            var u = _units[i];
            var item = new GameObject("Item_" + i, typeof(RectTransform));
            item.transform.SetParent(_listContent, false);
            item.AddComponent<LayoutElement>().minHeight = 48f;
            item.AddComponent<Image>().color = UnselColor;
            var btn = item.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectUnit(idx));

            var txtGo = new GameObject("Lbl", typeof(RectTransform));
            txtGo.transform.SetParent(item.transform, false);
            FillRect(txtGo.GetComponent<RectTransform>(), 8);
            var tx = txtGo.AddComponent<TextMeshProUGUI>();
            tx.fontSize = 20;
            tx.color = Color.white;
            string label = string.IsNullOrEmpty(u.displayName) ? u.id : u.displayName;
            tx.text = $"{label} HP:{u.maxHp} AP:{u.maxActionPoints} RP:{u.maxReactionPoints} {BuildTagSummary(u.tags)}";
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

    void OnAddUnit()
    {
        ApplyFromUi();
        var u = new SrpUnitTemplateData
        {
            id = "new_unit_" + _units.Count,
            displayName = "새 유닛",
            moveRange = 4,
            attackRange = 1,
            attackPower = 10,
            maxHp = 30,
            maxActionPoints = 2,
            maxReactionPoints = 1,
            maxPg = 18,
            maxAp = 2,
            maxPosture = 18,
            speed = 10,
            weaponClass = SrpWeaponClass.Melee,
            stance = SrpStance.Aggressive,
            facing = SrpFacing.South,
            maxSkills = 4,
            skillIds = Array.Empty<string>(),
        };
        _units.Add(u);
        RefreshList();
        SelectUnit(_units.Count - 1);
    }

    void OnDeleteUnit()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _units.Count) return;
        _units.RemoveAt(_selectedIndex);
        if (_selectedIndex >= _units.Count) _selectedIndex = _units.Count - 1;
        RefreshList();
        if (_selectedIndex >= 0) LoadToUi();
    }

    void OnSave()
    {
        ApplyFromUi();
        SrpDataIO.SaveUnits(_units.ToArray());
        _txtStatus.text  = "저장 완료!";
        _txtStatus.color = new Color(0.5f, 1f, 0.5f);
    }

    void OnReturnToLobby()
    {
        ApplyFromUi();
        SrpDataIO.SaveUnits(_units.ToArray());
        SrpGameSettings.ReturnToLobby();
    }

    public static void SyncV2LegacyStats(SrpUnitTemplateData unit)
    {
        if (unit == null)
            return;
        unit.maxActionPoints = Mathf.Max(0, unit.maxActionPoints);
        unit.maxReactionPoints = Mathf.Max(0, unit.maxReactionPoints);
        unit.maxPg = Mathf.Max(1, unit.maxPg);
        unit.speed = Mathf.Max(0, unit.speed);
        unit.maxAmmo = Mathf.Max(0, unit.maxAmmo);
        unit.maxAp = unit.maxActionPoints;
        unit.maxPosture = unit.maxPg;
    }

    static string BuildTagSummary(int tags)
    {
        var parts = new List<string>();
        if ((tags & (int)SrpUnitTags.Boss) != 0) parts.Add("Boss");
        if ((tags & (int)SrpUnitTags.Large) != 0) parts.Add("Large");
        if ((tags & (int)SrpUnitTags.ParryUser) != 0) parts.Add("Parry");
        if ((tags & (int)SrpUnitTags.Tank) != 0) parts.Add("Tank");
        return parts.Count > 0 ? "[" + string.Join(",", parts) + "]" : string.Empty;
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

    static GameObject MakePanel(Transform parent, string name)
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
        go.AddComponent<LayoutElement>().flexibleWidth = flex;
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

    TMP_Dropdown MakeDropdown(Transform parent, string label, string[] options)
    {
        var row = MakeHRow(parent, 44);
        MakeLabelInLayout(row.transform, label, 20, new Color(0.75f, 0.85f, 0.95f), 40);
        return MakeDropdownInParent(row.transform, options, 0);
    }

    static TMP_Dropdown MakeDropdownInParent(Transform parent, string[] options, int value)
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

        var templateGo = new GameObject("Template", typeof(RectTransform));
        templateGo.transform.SetParent(go.transform, false);
        var trt = templateGo.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0f);
        trt.anchorMax = new Vector2(1f, 0f);
        trt.pivot = new Vector2(0.5f, 1f);
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
        tcrt.pivot = new Vector2(0f, 1f);
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

    static Toggle MakeToggleInRow(Transform parent, string label)
    {
        var togGo = new GameObject("Tog_" + label, typeof(RectTransform));
        togGo.transform.SetParent(parent, false);
        togGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var hlg = togGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        var box = new GameObject("Box", typeof(RectTransform));
        box.transform.SetParent(togGo.transform, false);
        var ble = box.AddComponent<LayoutElement>();
        ble.minWidth = 30f;
        ble.preferredWidth = 30f;
        ble.flexibleWidth = 0f;
        var bgImg = box.AddComponent<Image>();
        bgImg.color = FieldBg;
        var toggle = box.AddComponent<Toggle>();
        toggle.targetGraphic = bgImg;

        var chk = new GameObject("Check", typeof(RectTransform));
        chk.transform.SetParent(box.transform, false);
        var crt = chk.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.15f, 0.15f);
        crt.anchorMax = new Vector2(0.85f, 0.85f);
        crt.offsetMin = crt.offsetMax = Vector2.zero;
        var chkImg = chk.AddComponent<Image>();
        chkImg.color = new Color(0.4f, 0.9f, 0.5f);
        toggle.graphic = chkImg;

        var lbl = new GameObject("Lbl", typeof(RectTransform));
        lbl.transform.SetParent(togGo.transform, false);
        lbl.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var tx = lbl.AddComponent<TextMeshProUGUI>();
        tx.text = label;
        tx.fontSize = 20;
        tx.color = Color.white;
        tx.alignment = TextAlignmentOptions.MidlineLeft;

        return toggle;
    }

    static GameObject MakeScrollRect(Transform parent)
    {
        var go = new GameObject("Scroll", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().flexibleHeight = 1f;
        go.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.07f, 0.5f);
        var sr = go.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30f;

        var vp = new GameObject("Viewport", typeof(RectTransform));
        vp.transform.SetParent(go.transform, false);
        FillRect(vp.GetComponent<RectTransform>(), 0);
        vp.AddComponent<RectMask2D>();
        sr.viewport = vp.GetComponent<RectTransform>();
        return go;
    }

    static GameObject MakeScrollRectFill(Transform parent)
    {
        var go = new GameObject("Scroll", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        FillRect(go.GetComponent<RectTransform>(), 0);
        var sr = go.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30f;

        var vp = new GameObject("Viewport", typeof(RectTransform));
        vp.transform.SetParent(go.transform, false);
        FillRect(vp.GetComponent<RectTransform>(), 0);
        vp.AddComponent<RectMask2D>();
        sr.viewport = vp.GetComponent<RectTransform>();
        return go;
    }

    static RectTransform MakeScrollContent(GameObject scrollGo)
    {
        var sr = scrollGo.GetComponent<ScrollRect>();
        var vp = sr.viewport;
        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(vp.transform, false);
        var crt = contentGo.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot     = new Vector2(0f, 1f);
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = Vector2.zero;
        var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        var csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = crt;
        return crt;
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

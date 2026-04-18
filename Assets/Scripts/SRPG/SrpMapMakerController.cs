using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SrpMapMakerController : MonoBehaviour
{
    // ── 상태 ──────────────────────────────────────────────────────────────────

    int _mapWidth = 10;
    int _mapHeight = 8;
    string _mapName = "new_map";
    bool[] _walkable;
    List<PlacedUnit> _placements = new List<PlacedUnit>();
    SrpUnitTemplateData[] _allUnits = Array.Empty<SrpUnitTemplateData>();
    SrpSkillData[] _allSkills = Array.Empty<SrpSkillData>();

    enum EditMode { Terrain, PlaceUnit, RemoveUnit }
    EditMode _editMode = EditMode.Terrain;
    int _selectedUnitIndex = -1;
    int _placementOwner;

    struct PlacedUnit
    {
        public string templateId;
        public int owner;
        public int x, y;
        public int footprintW, footprintH;
        public List<string> disabledSkillIds;
    }

    // ── 그리드 뷰 ─────────────────────────────────────────────────────────────

    GameObject[,] _tiles;
    Renderer[,] _tileRenderers;
    readonly Dictionary<int, GameObject> _unitMarkers = new Dictionary<int, GameObject>();
    float _cellSize = 1f;

    // ── UI 참조 ───────────────────────────────────────────────────────────────

    TMP_InputField _fldMapName;
    TMP_InputField _fldWidth;
    TMP_InputField _fldHeight;
    TMP_Dropdown _ddLoadMap;
    TMP_Dropdown _ddUnitSelect;
    TMP_Dropdown _ddOwner;
    TextMeshProUGUI _txtStatus;
    TextMeshProUGUI _txtPlacementList;
    Button _btnModeTerrain;
    Button _btnModePlace;
    Button _btnModeRemove;
    int _selectedPlacementIndex = -1;
    GameObject _unitSkillPanel;
    readonly List<(GameObject go, Toggle toggle, string skillId)> _unitSkillToggles
        = new List<(GameObject, Toggle, string)>();
    Vector3 _lastMousePos;

    // ── 색상 ──────────────────────────────────────────────────────────────────

    static readonly Color PanelBg    = new Color(0.08f, 0.10f, 0.14f, 0.92f);
    static readonly Color DarkBg     = new Color(0.05f, 0.06f, 0.09f, 1f);
    static readonly Color FieldBg    = new Color(0.12f, 0.14f, 0.18f, 0.95f);
    static readonly Color BtnNormal  = new Color(0.22f, 0.33f, 0.50f, 0.90f);
    static readonly Color BtnGreen   = new Color(0.18f, 0.50f, 0.22f, 0.95f);
    static readonly Color BtnActive  = new Color(0.25f, 0.60f, 0.30f, 0.95f);
    static readonly Color BtnDanger  = new Color(0.60f, 0.22f, 0.18f, 0.90f);
    static readonly Color AccentYellow = new Color(1f, 0.92f, 0.45f);
    static readonly Color WalkColor  = new Color(0.55f, 0.65f, 0.45f);
    static readonly Color BlockColor = new Color(0.35f, 0.3f, 0.28f);

    // ── 생명주기 ──────────────────────────────────────────────────────────────

    void Awake()
    {
        EnsureEventSystem();
        SrpFontWarmup.Warmup();
        _allUnits = SrpDataIO.LoadUnitsOrDefault();
        _allSkills = SrpDataIO.LoadSkillsOrDefault();
        InitMap(_mapWidth, _mapHeight);
        BuildUi();
        RefreshMapDropdown();
        BuildGrid();
        FrameCamera();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            HandleGridClick();

        HandleCameraControls();
    }

    void HandleCameraControls()
    {
        bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        var cam = Camera.main;
        if (cam == null) return;

        if (!overUi)
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * 0.8f, 1f, 40f);
        }

        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            _lastMousePos = Input.mousePosition;

        if (!overUi && (Input.GetMouseButton(1) || Input.GetMouseButton(2)))
        {
            Vector3 delta = Input.mousePosition - _lastMousePos;
            float scale = cam.orthographicSize * 2f / Screen.height;
            cam.transform.position -= new Vector3(delta.x * scale, 0f, delta.y * scale);
            _lastMousePos = Input.mousePosition;
        }
    }

    void InitMap(int w, int h)
    {
        _mapWidth = w;
        _mapHeight = h;
        _walkable = new bool[w * h];
        for (int i = 0; i < _walkable.Length; i++)
            _walkable[i] = true;
        _placements.Clear();
    }

    // ── 그리드 ────────────────────────────────────────────────────────────────

    void BuildGrid()
    {
        ClearGrid();
        var parent = new GameObject("MapGrid").transform;
        parent.SetParent(transform, false);
        _tiles = new GameObject[_mapWidth, _mapHeight];
        _tileRenderers = new Renderer[_mapWidth, _mapHeight];

        for (int y = 0; y < _mapHeight; y++)
        for (int x = 0; x < _mapWidth; x++)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"t_{x}_{y}";
            cube.transform.SetParent(parent, false);
            cube.transform.localScale = new Vector3(_cellSize * 0.96f, 0.15f, _cellSize * 0.96f);
            cube.transform.position = new Vector3(x * _cellSize, 0f, y * _cellSize);
            _tiles[x, y] = cube;
            _tileRenderers[x, y] = cube.GetComponent<Renderer>();
        }
        RefreshAllTileColors();
        RefreshUnitMarkers();
    }

    void ClearGrid()
    {
        var old = transform.Find("MapGrid");
        if (old != null) Destroy(old.gameObject);
        foreach (var kv in _unitMarkers)
            if (kv.Value != null) Destroy(kv.Value);
        _unitMarkers.Clear();
    }

    void RefreshAllTileColors()
    {
        for (int y = 0; y < _mapHeight; y++)
        for (int x = 0; x < _mapWidth; x++)
        {
            bool walk = _walkable[y * _mapWidth + x];
            ApplyColor(_tileRenderers[x, y], walk ? WalkColor : BlockColor);
        }
    }

    void RefreshUnitMarkers()
    {
        foreach (var kv in _unitMarkers)
            if (kv.Value != null) Destroy(kv.Value);
        _unitMarkers.Clear();

        for (int i = 0; i < _placements.Count; i++)
        {
            var p = _placements[i];
            int fw = Mathf.Max(1, p.footprintW);
            int fh = Mathf.Max(1, p.footprintH);
            float cx = (p.x + (fw - 1) * 0.5f) * _cellSize;
            float cz = (p.y + (fh - 1) * 0.5f) * _cellSize;
            var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyl.name = $"placed_{i}";
            Destroy(cyl.GetComponent<Collider>());
            cyl.transform.position = new Vector3(cx, 0.13f, cz);
            float scX = _cellSize * fw * 0.75f;
            float scZ = _cellSize * fh * 0.75f;
            cyl.transform.localScale = new Vector3(scX, 0.12f, scZ);
            Color col = p.owner == 0
                ? new Color(0.25f, 0.6f, 1f)
                : new Color(1f, 0.3f, 0.25f);
            ApplyColor(cyl.GetComponent<Renderer>(), col);
            _unitMarkers[i] = cyl;
        }
    }

    void FrameCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.orthographic = true;
        float cx = (_mapWidth - 1f) * 0.5f * _cellSize;
        float cz = (_mapHeight - 1f) * 0.5f * _cellSize;
        float aspect = Mathf.Max(cam.aspect, 0.01f);
        float halfV = _mapHeight * _cellSize * 0.5f;
        float halfH = _mapWidth * _cellSize / (2f * aspect);
        cam.orthographicSize = Mathf.Max(halfV, halfH) + 1f;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 50f;
        cam.transform.position = new Vector3(cx, 12f, cz);
        cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    // ── 입력 ──────────────────────────────────────────────────────────────────

    void HandleGridClick()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        var cam = Camera.main;
        if (cam == null) return;
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.up, Vector3.zero);
        if (!plane.Raycast(ray, out float enter)) return;

        var hit = ray.GetPoint(enter);
        int x = Mathf.RoundToInt(hit.x / _cellSize);
        int y = Mathf.RoundToInt(hit.z / _cellSize);
        if (x < 0 || x >= _mapWidth || y < 0 || y >= _mapHeight) return;

        switch (_editMode)
        {
            case EditMode.Terrain:
                int foundIdx = -1;
                for (int i = 0; i < _placements.Count; i++)
                    if (IsFootprintOverlap(_placements[i], x, y)) { foundIdx = i; break; }
                if (foundIdx >= 0)
                {
                    OnSelectPlacement(foundIdx);
                    break;
                }
                int idx = y * _mapWidth + x;
                _walkable[idx] = !_walkable[idx];
                ApplyColor(_tileRenderers[x, y], _walkable[idx] ? WalkColor : BlockColor);
                break;

            case EditMode.PlaceUnit:
                if (_selectedUnitIndex < 0 || _selectedUnitIndex >= _allUnits.Length) break;
                var tmpl = _allUnits[_selectedUnitIndex];
                int fpW = Mathf.Max(1, tmpl.footprintWidth);
                int fpH = Mathf.Max(1, tmpl.footprintHeight);
                if (x + fpW > _mapWidth || y + fpH > _mapHeight) break;
                for (int fy = 0; fy < fpH; fy++)
                for (int fx = 0; fx < fpW; fx++)
                    for (int ri = _placements.Count - 1; ri >= 0; ri--)
                        if (IsFootprintOverlap(_placements[ri], x + fx, y + fy))
                            _placements.RemoveAt(ri);
                _placements.Add(new PlacedUnit
                {
                    templateId = tmpl.id,
                    owner = _placementOwner,
                    x = x, y = y,
                    footprintW = fpW, footprintH = fpH,
                    disabledSkillIds = new List<string>(),
                });
                RefreshUnitMarkers();
                RefreshPlacementList();
                break;

            case EditMode.RemoveUnit:
                for (int i = _placements.Count - 1; i >= 0; i--)
                    if (IsFootprintOverlap(_placements[i], x, y))
                        _placements.RemoveAt(i);
                _selectedPlacementIndex = -1;
                RefreshUnitMarkers();
                RefreshPlacementList();
                if (_unitSkillPanel != null) _unitSkillPanel.SetActive(false);
                break;
        }
    }

    // ── UI 생성 ───────────────────────────────────────────────────────────────

    void BuildUi()
    {
        var canvasGo = new GameObject("MapMakerCanvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        BuildRightPanel(canvasGo.transform);
        BuildBottomBar(canvasGo.transform);
    }

    void BuildRightPanel(Transform root)
    {
        var panel = new GameObject("RightPanel", typeof(RectTransform));
        panel.transform.SetParent(root, false);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(420f, 0f);
        panel.AddComponent<Image>().color = PanelBg;

        var outerScroll = new GameObject("Scroll", typeof(RectTransform));
        outerScroll.transform.SetParent(panel.transform, false);
        FillRect(outerScroll.GetComponent<RectTransform>(), 0);
        var sr = outerScroll.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30f;

        var vp = new GameObject("Viewport", typeof(RectTransform));
        vp.transform.SetParent(outerScroll.transform, false);
        FillRect(vp.GetComponent<RectTransform>(), 0);
        vp.AddComponent<RectMask2D>();
        sr.viewport = vp.GetComponent<RectTransform>();

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(vp.transform, false);
        var crt = contentGo.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot     = new Vector2(0f, 1f);
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = Vector2.zero;
        var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 12, 12);
        vlg.spacing = 8f;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        var csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = crt;

        var form = contentGo.transform;

        MakeLabelInLayout(form, "맵 메이커", 28, AccentYellow, 38);
        MakeSep(form);

        // 맵 속성
        MakeLabelInLayout(form, "맵 속성", 22, new Color(0.8f, 0.9f, 1f), 28);
        _fldMapName = MakeFieldRow(form, "맵 이름", _mapName);
        _fldWidth   = MakeFieldRow(form, "너비", _mapWidth.ToString(), TMP_InputField.ContentType.IntegerNumber);
        _fldHeight  = MakeFieldRow(form, "높이", _mapHeight.ToString(), TMP_InputField.ContentType.IntegerNumber);
        _fldWidth.text = _mapWidth.ToString();
        _fldHeight.text = _mapHeight.ToString();

        MakeButton(form, "크기 적용 (그리드 재생성)", OnApplySize, BtnNormal, 48);
        MakeSep(form);

        // 편집 모드
        MakeLabelInLayout(form, "편집 모드", 22, new Color(0.8f, 0.9f, 1f), 28);
        var modeRow = MakeHRow(form, 48);
        _btnModeTerrain = MakeButtonInParent(modeRow.transform, "지형", () => SetMode(EditMode.Terrain), BtnActive, 1f);
        _btnModePlace   = MakeButtonInParent(modeRow.transform, "배치", () => SetMode(EditMode.PlaceUnit), BtnNormal, 1f);
        _btnModeRemove  = MakeButtonInParent(modeRow.transform, "제거", () => SetMode(EditMode.RemoveUnit), BtnNormal, 1f);

        MakeSep(form);

        // 유닛 선택
        MakeLabelInLayout(form, "배치할 유닛", 22, new Color(0.8f, 0.9f, 1f), 28);
        var unitNames = new List<string>();
        foreach (var u in _allUnits)
            unitNames.Add($"{u.displayName} ({u.id})");
        if (unitNames.Count == 0) unitNames.Add("(없음)");
        _ddUnitSelect = MakeDropdown(form, unitNames.ToArray(), 0);
        _ddUnitSelect.onValueChanged.AddListener(v => _selectedUnitIndex = v);
        if (_allUnits.Length > 0) _selectedUnitIndex = 0;

        var ownerRow = MakeHRow(form, 44);
        MakeLabelInLayout(ownerRow.transform, "소유자", 20, new Color(0.75f, 0.85f, 0.95f), 40);
        _ddOwner = MakeDropdown(ownerRow.transform, new[] { "플레이어 0 (파랑)", "플레이어 1 (빨강)" }, 0);
        _ddOwner.onValueChanged.AddListener(v => _placementOwner = v);

        MakeSep(form);

        // 배치 목록 (클릭 가능)
        MakeLabelInLayout(form, "배치 유닛 (클릭=스킬 설정)", 22, new Color(0.8f, 0.9f, 1f), 28);
        _txtPlacementList = MakeLabelInLayout(form, "(없음)", 18, new Color(0.7f, 0.8f, 0.9f), 60);

        // 유닛별 스킬 비활성화 패널
        _unitSkillPanel = new GameObject("UnitSkillPanel", typeof(RectTransform));
        _unitSkillPanel.transform.SetParent(form, false);
        var uspLe = _unitSkillPanel.AddComponent<LayoutElement>();
        uspLe.minHeight = 40f;
        _unitSkillPanel.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.7f);
        var uspVlg = _unitSkillPanel.AddComponent<VerticalLayoutGroup>();
        uspVlg.padding = new RectOffset(6, 6, 6, 6);
        uspVlg.spacing = 4f;
        uspVlg.childControlHeight = true;
        uspVlg.childControlWidth = true;
        uspVlg.childForceExpandHeight = false;
        uspVlg.childForceExpandWidth = true;
        _unitSkillPanel.SetActive(false);

        MakeSep(form);

        // 저장/로드
        MakeLabelInLayout(form, "저장 / 불러오기", 22, new Color(0.8f, 0.9f, 1f), 28);
        MakeButton(form, "JSON 저장", OnSave, BtnGreen, 48);
        _ddLoadMap = MakeDropdown(form, new string[]{ "(맵 없음)" }, 0);
        MakeButton(form, "JSON 불러오기", OnLoad, BtnNormal, 48);

        _txtStatus = MakeLabelInLayout(form, "", 18, new Color(0.5f, 1f, 0.5f), 24);
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
        rt.offsetMax = new Vector2(-436f, 56f);

        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        MakeButtonInParent(bar.transform, "로비로 돌아가기", OnReturnToLobby, BtnNormal, 1f);
    }

    void RefreshPlacementList()
    {
        if (_txtPlacementList == null) return;
        if (_placements.Count == 0)
        {
            _txtPlacementList.text = "(없음)";
            _selectedPlacementIndex = -1;
            if (_unitSkillPanel != null) _unitSkillPanel.SetActive(false);
            return;
        }
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < _placements.Count; i++)
        {
            var p = _placements[i];
            string marker = i == _selectedPlacementIndex ? ">" : " ";
            int disCount = p.disabledSkillIds != null ? p.disabledSkillIds.Count : 0;
            string disTag = disCount > 0 ? $" [-{disCount}스킬]" : "";
            sb.AppendLine($"{marker} P{p.owner} {p.templateId} ({p.x},{p.y}){disTag}");
        }
        _txtPlacementList.text = sb.ToString();
    }

    void OnSelectPlacement(int index)
    {
        if (index < 0 || index >= _placements.Count) return;
        _selectedPlacementIndex = index;
        RefreshPlacementList();
        ShowUnitSkillPanel(index);
    }

    void ShowUnitSkillPanel(int placementIndex)
    {
        foreach (var (go, _, _) in _unitSkillToggles)
            if (go != null) Destroy(go);
        _unitSkillToggles.Clear();

        if (placementIndex < 0 || placementIndex >= _placements.Count)
        {
            _unitSkillPanel.SetActive(false);
            return;
        }

        var p = _placements[placementIndex];
        SrpUnitTemplateData tmpl = null;
        foreach (var u in _allUnits)
            if (u.id == p.templateId) { tmpl = u; break; }

        if (tmpl == null || tmpl.skillIds == null || tmpl.skillIds.Length == 0)
        {
            var noLbl = new GameObject("Lbl", typeof(RectTransform));
            noLbl.transform.SetParent(_unitSkillPanel.transform, false);
            noLbl.AddComponent<LayoutElement>().minHeight = 30f;
            var nt = noLbl.AddComponent<TextMeshProUGUI>();
            nt.text = "이 유닛에 할당된 스킬 없음";
            nt.fontSize = 16;
            nt.color = new Color(0.6f, 0.6f, 0.6f);
            nt.alignment = TextAlignmentOptions.Center;
            _unitSkillToggles.Add((noLbl, null, ""));
            _unitSkillPanel.SetActive(true);
            return;
        }

        var disSet = new HashSet<string>(p.disabledSkillIds ?? new List<string>());

        var headerLbl = new GameObject("Lbl", typeof(RectTransform));
        headerLbl.transform.SetParent(_unitSkillPanel.transform, false);
        headerLbl.AddComponent<LayoutElement>().minHeight = 26f;
        var ht = headerLbl.AddComponent<TextMeshProUGUI>();
        ht.text = $"{tmpl.displayName} - 비활성화할 스킬 선택";
        ht.fontSize = 17;
        ht.color = new Color(0.9f, 0.7f, 0.4f);
        ht.alignment = TextAlignmentOptions.TopLeft;
        _unitSkillToggles.Add((headerLbl, null, ""));

        foreach (var sid in tmpl.skillIds)
        {
            string skillName = sid;
            foreach (var sk in _allSkills)
                if (sk.id == sid) { skillName = sk.displayName; break; }

            var row = MakeHRow(_unitSkillPanel.transform, 32);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            row.GetComponent<LayoutElement>().flexibleHeight = 0f;

            var togGo = new GameObject("Tog", typeof(RectTransform));
            togGo.transform.SetParent(row.transform, false);
            var tle = togGo.AddComponent<LayoutElement>();
            tle.minWidth = 26f; tle.preferredWidth = 26f; tle.flexibleWidth = 0f;
            var bgImg = togGo.AddComponent<Image>();
            bgImg.color = FieldBg;
            var toggle = togGo.AddComponent<Toggle>();
            toggle.targetGraphic = bgImg;
            toggle.isOn = disSet.Contains(sid);

            var chk = new GameObject("Check", typeof(RectTransform));
            chk.transform.SetParent(togGo.transform, false);
            var crt2 = chk.GetComponent<RectTransform>();
            crt2.anchorMin = new Vector2(0.15f, 0.15f);
            crt2.anchorMax = new Vector2(0.85f, 0.85f);
            crt2.offsetMin = crt2.offsetMax = Vector2.zero;
            var chkImg = chk.AddComponent<Image>();
            chkImg.color = new Color(0.9f, 0.35f, 0.3f);
            toggle.graphic = chkImg;

            var lbl = new GameObject("Lbl", typeof(RectTransform));
            lbl.transform.SetParent(row.transform, false);
            lbl.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var tx = lbl.AddComponent<TextMeshProUGUI>();
            tx.text = $"{skillName} ({sid})";
            tx.fontSize = 16;
            tx.color = Color.white;
            tx.alignment = TextAlignmentOptions.MidlineLeft;

            string capturedSid = sid;
            int capturedIdx = placementIndex;
            toggle.onValueChanged.AddListener(isOn =>
            {
                if (capturedIdx < 0 || capturedIdx >= _placements.Count) return;
                var pu = _placements[capturedIdx];
                if (pu.disabledSkillIds == null) pu.disabledSkillIds = new List<string>();
                if (isOn && !pu.disabledSkillIds.Contains(capturedSid))
                    pu.disabledSkillIds.Add(capturedSid);
                else if (!isOn)
                    pu.disabledSkillIds.Remove(capturedSid);
                _placements[capturedIdx] = pu;
                RefreshPlacementList();
            });

            _unitSkillToggles.Add((row, toggle, sid));
        }

        _unitSkillPanel.SetActive(true);
    }

    static bool IsFootprintOverlap(PlacedUnit p, int tx, int ty)
    {
        int fw = Mathf.Max(1, p.footprintW);
        int fh = Mathf.Max(1, p.footprintH);
        return tx >= p.x && tx < p.x + fw && ty >= p.y && ty < p.y + fh;
    }

    // ── 핸들러 ────────────────────────────────────────────────────────────────

    void SetMode(EditMode mode)
    {
        _editMode = mode;
        RefreshModeButtons();
    }

    void RefreshModeButtons()
    {
        SetBtnColor(_btnModeTerrain, _editMode == EditMode.Terrain ? BtnActive : BtnNormal);
        SetBtnColor(_btnModePlace,   _editMode == EditMode.PlaceUnit ? BtnActive : BtnNormal);
        SetBtnColor(_btnModeRemove,  _editMode == EditMode.RemoveUnit ? BtnActive : BtnNormal);
    }

    static void SetBtnColor(Button btn, Color c)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = c;
    }

    void OnApplySize()
    {
        int.TryParse(_fldWidth.text, out int w);
        int.TryParse(_fldHeight.text, out int h);
        w = Mathf.Clamp(w, 2, 30);
        h = Mathf.Clamp(h, 2, 30);
        InitMap(w, h);
        BuildGrid();
        FrameCamera();
        RefreshPlacementList();
        SetStatus("그리드 재생성 완료");
    }

    void OnSave()
    {
        var map = BuildMapFile();
        string name = _fldMapName != null ? _fldMapName.text.Trim() : "map";
        if (string.IsNullOrEmpty(name)) name = "map";
        map.name = name;
        string path = SrpMapIO.Save(map, name);
        SetStatus($"저장: {path}");
        RefreshMapDropdown();
    }

    void OnLoad()
    {
        if (_ddLoadMap == null || _ddLoadMap.options.Count == 0)
        {
            SetStatus("불러올 맵이 없습니다.", false);
            return;
        }
        string fileName = _ddLoadMap.options[_ddLoadMap.value].text;
        if (fileName == "(맵 없음)" || string.IsNullOrEmpty(fileName))
        {
            SetStatus("파일명을 선택하세요.", false);
            return;
        }
        if (!SrpMapIO.TryLoad(fileName, out var map))
        {
            SetStatus($"'{fileName}.json' 를 찾을 수 없습니다.", false);
            return;
        }
        ApplyLoadedMap(map);
        SetStatus($"로드 완료: {map.name} ({map.width}x{map.height})");
    }

    void ApplyLoadedMap(SrpMapFileV1 map)
    {
        _mapWidth = map.width;
        _mapHeight = map.height;
        _mapName = map.name ?? "loaded";
        _walkable = new bool[_mapWidth * _mapHeight];
        if (map.walkable != null && map.walkable.Length == _walkable.Length)
            Array.Copy(map.walkable, _walkable, _walkable.Length);
        else
            for (int i = 0; i < _walkable.Length; i++) _walkable[i] = true;

        _placements.Clear();
        if (map.placements != null)
        {
            foreach (var p in map.placements)
            {
                int fw = 1, fh = 1;
                if (p.footprint != null && p.footprint.Length > 1)
                {
                    foreach (var o in p.footprint) { fw = Mathf.Max(fw, o.dx + 1); fh = Mathf.Max(fh, o.dy + 1); }
                }
                else
                {
                    foreach (var t in _allUnits)
                        if (t.id == p.templateId) { fw = Mathf.Max(1, t.footprintWidth); fh = Mathf.Max(1, t.footprintHeight); break; }
                }
                _placements.Add(new PlacedUnit
                {
                    templateId = p.templateId,
                    owner = p.owner,
                    x = p.x, y = p.y,
                    footprintW = fw, footprintH = fh,
                    disabledSkillIds = p.disabledSkillIds != null
                        ? new List<string>(p.disabledSkillIds)
                        : new List<string>(),
                });
            }
        }

        if (_fldMapName != null) _fldMapName.text = _mapName;
        if (_fldWidth != null) _fldWidth.text = _mapWidth.ToString();
        if (_fldHeight != null) _fldHeight.text = _mapHeight.ToString();

        BuildGrid();
        FrameCamera();
        RefreshPlacementList();
    }

    SrpMapFileV1 BuildMapFile()
    {
        var templates = new Dictionary<string, SrpUnitTemplateData>();
        foreach (var u in _allUnits)
            templates[u.id] = u;

        var usedTemplates = new List<SrpUnitTemplateData>();
        var usedIds = new HashSet<string>();
        var placements = new List<SrpPlacementData>();

        foreach (var p in _placements)
        {
            if (!usedIds.Contains(p.templateId) && templates.ContainsKey(p.templateId))
            {
                usedTemplates.Add(templates[p.templateId]);
                usedIds.Add(p.templateId);
            }
            int fw = Mathf.Max(1, p.footprintW);
            int fh = Mathf.Max(1, p.footprintH);
            var offsets = new List<SrpOffset>();
            if (fw > 1 || fh > 1)
                for (int fy = 0; fy < fh; fy++)
                for (int fx = 0; fx < fw; fx++)
                    offsets.Add(new SrpOffset { dx = fx, dy = fy });
            placements.Add(new SrpPlacementData
            {
                templateId = p.templateId,
                owner = p.owner,
                x = p.x,
                y = p.y,
                footprint = offsets.ToArray(),
                disabledSkillIds = p.disabledSkillIds != null
                    ? p.disabledSkillIds.ToArray()
                    : Array.Empty<string>(),
            });
        }

        return new SrpMapFileV1
        {
            version = 2,
            name = _mapName,
            width = _mapWidth,
            height = _mapHeight,
            walkable = (bool[])_walkable.Clone(),
            playerOrder = new[] { 0, 1 },
            templates = usedTemplates.ToArray(),
            placements = placements.ToArray(),
        };
    }

    void OnReturnToLobby()
    {
        SrpGameSettings.ReturnToLobby();
    }

    void SetStatus(string msg, bool success = true)
    {
        if (_txtStatus == null) return;
        _txtStatus.text = msg;
        _txtStatus.color = success
            ? new Color(0.5f, 1f, 0.5f)
            : new Color(1f, 0.5f, 0.4f);
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

    static Button MakeButton(Transform parent, string label,
        UnityEngine.Events.UnityAction onClick, Color bgColor, float height)
    {
        var go = new GameObject("Btn_" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
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
        tx.fontSize = 20;
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

    static void SetOptions(TMP_Dropdown dd, string[] options)
    {
        dd.ClearOptions();
        var list = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
        foreach (var o in options)
            list.Add(new TMP_Dropdown.OptionData(o));
        dd.AddOptions(list);
    }

    void RefreshMapDropdown()
    {
        if (_ddLoadMap == null) return;
        string[] maps = SrpMapIO.ListMaps();
        string[] options = maps.Length > 0 ? maps : new[] { "(맵 없음)" };
        SetOptions(_ddLoadMap, options);
        _ddLoadMap.value = 0;
        _ddLoadMap.RefreshShownValue();
    }

    static TMP_Dropdown MakeDropdown(Transform parent, string[] options, int value)
    {
        var go = new GameObject("Dropdown", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().minHeight = 40f;
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

    static void ApplyColor(Renderer r, Color c)
    {
        if (r == null) return;
        if (r.material.HasProperty("_BaseColor"))
            r.material.SetColor("_BaseColor", c);
        else
            r.material.color = c;
    }

    static void FillRect(RectTransform rt, float padding)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
    }
}

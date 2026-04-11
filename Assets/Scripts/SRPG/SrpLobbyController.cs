using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// SRPG 로비 씬 — 맵 선택 후 전투 씬을 로드한다.
/// 빈 GameObject에 이 컴포넌트를 추가하면 UI를 코드로 생성한다.
/// </summary>
public class SrpLobbyController : MonoBehaviour
{
    // ── 인스펙터 ───────────────────────────────────────────────────────────────

    [Tooltip("선택됐을 때 프리셋 버튼 강조 색상.")]
    public Color selectedColor   = new Color(0.25f, 0.60f, 0.30f, 0.95f);

    [Tooltip("미선택 프리셋 버튼 기본 색상.")]
    public Color unselectedColor = new Color(0.22f, 0.33f, 0.50f, 0.90f);

    // ── 상태 ──────────────────────────────────────────────────────────────────

    SrpMapPreset   _selectedPreset = SrpMapPreset.Skirmish;
    SrpMapFileV1   _loadedMap;

    Button[]  _presetButtons;
    Image[]   _presetImages;
    TextMeshProUGUI _txtLoadStatus;
    TMP_InputField  _inputFileName;

    // ── 생명주기 ──────────────────────────────────────────────────────────────

    void Awake()
    {
        EnsureEventSystem();
        SrpFontWarmup.Warmup();
        BuildUi();
        RefreshPresetButtons();
    }

    static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }

    // ── UI 생성 ───────────────────────────────────────────────────────────────

    void BuildUi()
    {
        // Canvas
        var canvasGo = new GameObject("LobbyCanvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode    = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // 전체 배경
        var bg = MakePanel(canvasGo.transform, "Background", Vector2.zero, Vector2.one);
        bg.color = new Color(0.05f, 0.06f, 0.09f, 1f);

        // 중앙 콘텐츠 패널
        var centerGo = new GameObject("CenterPanel", typeof(RectTransform));
        centerGo.transform.SetParent(canvasGo.transform, false);
        var crt = centerGo.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot     = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(640f, 820f);

        centerGo.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.14f, 0.92f);

        var vlg = centerGo.AddComponent<VerticalLayoutGroup>();
        vlg.padding             = new RectOffset(36, 36, 36, 36);
        vlg.spacing             = 16f;
        vlg.childControlHeight  = true;
        vlg.childControlWidth   = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth  = true;

        // 타이틀
        MakeLabel(centerGo.transform, "SRPG 프로토타입", 40, new Color(1f, 0.92f, 0.45f), 60);
        MakeLabel(centerGo.transform, "전투 시작 전 맵을 선택하세요", 22, new Color(0.7f, 0.8f, 0.9f), 36);
        MakeSeparator(centerGo.transform);

        // 맵 선택 헤더
        MakeLabel(centerGo.transform, "맵 선택", 24, new Color(0.85f, 0.95f, 1f), 32);

        // 프리셋 버튼 행
        var presetRow = MakeHorizontalRow(centerGo.transform, 72);
        var presets = new[]
        {
            (SrpMapPreset.TinyDuel, "TinyDuel\n6×4 1vs1"),
            (SrpMapPreset.Corridor, "Corridor\n8×10 통로"),
            (SrpMapPreset.Skirmish, "Skirmish\n10×8 혼전"),
        };

        _presetButtons = new Button[presets.Length];
        _presetImages  = new Image[presets.Length];

        for (int i = 0; i < presets.Length; i++)
        {
            var (preset, label) = presets[i];
            int captured = i;
            var btn = MakeButtonInRow(presetRow.transform, label, () => OnSelectPreset(captured), preset);
            _presetButtons[i] = btn;
            _presetImages[i]  = btn.GetComponent<Image>();
        }

        MakeSeparator(centerGo.transform);

        // JSON 로드 섹션
        MakeLabel(centerGo.transform, "JSON 맵 로드 (선택사항)", 22, new Color(0.75f, 0.85f, 0.95f), 30);

        var jsonRow = MakeHorizontalRow(centerGo.transform, 56);
        _inputFileName = MakeInputField(jsonRow.transform, "파일명 (확장자 제외)");
        MakeSmallButton(jsonRow.transform, "불러오기", OnLoadJson, 140f);

        _txtLoadStatus = MakeLabel(centerGo.transform, "", 20, new Color(0.7f, 1f, 0.7f), 28);

        MakeSeparator(centerGo.transform);

        // 전투 시작 버튼
        MakeButton(centerGo.transform, "전투 시작", OnStartBattle, 72, 30);

        MakeSeparator(centerGo.transform);

        // 메이커 섹션
        MakeLabel(centerGo.transform, "데이터 관리", 24, new Color(0.85f, 0.95f, 1f), 32);
        var makerRow = MakeHorizontalRow(centerGo.transform, 60);
        MakeMakerButton(makerRow.transform, "스킬 메이커",
            () => UnityEngine.SceneManagement.SceneManager.LoadScene(SrpGameSettings.SkillMakerScene));
        MakeMakerButton(makerRow.transform, "유닛 메이커",
            () => UnityEngine.SceneManagement.SceneManager.LoadScene(SrpGameSettings.UnitMakerScene));
        MakeMakerButton(makerRow.transform, "맵 메이커",
            () => UnityEngine.SceneManagement.SceneManager.LoadScene(SrpGameSettings.MapMakerScene));
    }

    // ── 버튼 핸들러 ───────────────────────────────────────────────────────────

    void OnSelectPreset(int index)
    {
        var presets = new[] { SrpMapPreset.TinyDuel, SrpMapPreset.Corridor, SrpMapPreset.Skirmish };
        _selectedPreset = presets[index];
        _loadedMap      = null;
        _txtLoadStatus.text = "";
        RefreshPresetButtons();
    }

    void OnLoadJson()
    {
        string fileName = _inputFileName != null ? _inputFileName.text.Trim() : "";
        if (string.IsNullOrEmpty(fileName))
        {
            _txtLoadStatus.text = "파일명을 입력하세요.";
            _txtLoadStatus.color = new Color(1f, 0.6f, 0.4f);
            return;
        }
        if (SrpMapIO.TryLoad(fileName, out var map))
        {
            _loadedMap = map;
            _txtLoadStatus.text  = $"로드 완료: {map.name} ({map.width}×{map.height})";
            _txtLoadStatus.color = new Color(0.5f, 1f, 0.5f);
            foreach (var img in _presetImages)
                img.color = unselectedColor;
        }
        else
        {
            _loadedMap = null;
            _txtLoadStatus.text  = $"'{fileName}.json' 파일을 찾을 수 없습니다.";
            _txtLoadStatus.color = new Color(1f, 0.5f, 0.4f);
        }
    }

    void OnStartBattle()
    {
        if (_loadedMap != null)
            SrpGameSettings.StartBattleWithMap(_loadedMap);
        else
            SrpGameSettings.StartBattle(_selectedPreset);
    }

    void RefreshPresetButtons()
    {
        var presets = new[] { SrpMapPreset.TinyDuel, SrpMapPreset.Corridor, SrpMapPreset.Skirmish };
        for (int i = 0; i < _presetImages.Length; i++)
            _presetImages[i].color = (presets[i] == _selectedPreset && _loadedMap == null)
                ? selectedColor
                : unselectedColor;
    }

    // ── UI 헬퍼 ───────────────────────────────────────────────────────────────

    static Image MakePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin  = anchorMin;
        rt.anchorMax  = anchorMax;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
        return go.AddComponent<Image>();
    }

    static TextMeshProUGUI MakeLabel(Transform parent, string text, int fontSize, Color color, float minH)
    {
        var go = new GameObject("Lbl", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().minHeight = minH;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize  = fontSize;
        t.color     = color;
        t.alignment = TextAlignmentOptions.Center;
        t.overflowMode = TextOverflowModes.Overflow;
        t.text      = text;
        return t;
    }

    static void MakeSeparator(Transform parent)
    {
        var go = new GameObject("Sep", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().minHeight = 2;
        go.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.14f);
    }

    static GameObject MakeHorizontalRow(Transform parent, float height)
    {
        var go = new GameObject("Row", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight       = height;
        le.preferredHeight = height;
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing             = 12f;
        hlg.childControlHeight  = true;
        hlg.childControlWidth   = true;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth  = true;
        return go;
    }

    Button MakeButtonInRow(Transform parent, string label, UnityEngine.Events.UnityAction onClick,
        SrpMapPreset preset)
    {
        var go  = new GameObject("BtnPreset_" + preset, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = unselectedColor;
        var b   = go.AddComponent<Button>();
        b.onClick.AddListener(onClick);

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var tx = textGo.AddComponent<TextMeshProUGUI>();
        tx.fontSize  = 22;
        tx.color     = Color.white;
        tx.alignment = TextAlignmentOptions.Center;
        tx.text      = label;
        return b;
    }

    static Button MakeButton(Transform parent, string label,
        UnityEngine.Events.UnityAction onClick, float height, int fontSize = 24)
    {
        var go = new GameObject("Btn_" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight       = height;
        le.preferredHeight = height;
        le.flexibleWidth   = 1;
        go.AddComponent<Image>().color = new Color(0.18f, 0.50f, 0.22f, 0.95f);
        var b = go.AddComponent<Button>();
        b.onClick.AddListener(onClick);

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var tx = textGo.AddComponent<TextMeshProUGUI>();
        tx.fontSize  = fontSize;
        tx.color     = Color.white;
        tx.alignment = TextAlignmentOptions.Center;
        tx.text      = label;
        return b;
    }

    static void MakeSmallButton(Transform parent, string label,
        UnityEngine.Events.UnityAction onClick, float width)
    {
        var go = new GameObject("Btn_" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minWidth       = width;
        le.preferredWidth = width;
        le.flexibleWidth  = 0;
        go.AddComponent<Image>().color = new Color(0.28f, 0.38f, 0.55f, 0.9f);
        var b = go.AddComponent<Button>();
        b.onClick.AddListener(onClick);

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var tx = textGo.AddComponent<TextMeshProUGUI>();
        tx.fontSize  = 22;
        tx.color     = Color.white;
        tx.alignment = TextAlignmentOptions.Center;
        tx.text      = label;
    }

    static TMP_InputField MakeInputField(Transform parent, string placeholder)
    {
        var go = new GameObject("InputField", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
        var field = go.AddComponent<TMP_InputField>();

        // Text Area (viewport — TMP_InputField 필수 구조)
        var areaGo = new GameObject("Text Area", typeof(RectTransform));
        areaGo.transform.SetParent(go.transform, false);
        FillRect(areaGo.GetComponent<RectTransform>(), 6);
        areaGo.AddComponent<RectMask2D>();
        field.textViewport = areaGo.GetComponent<RectTransform>();

        // Placeholder
        var phGo = new GameObject("Placeholder", typeof(RectTransform));
        phGo.transform.SetParent(areaGo.transform, false);
        FillRect(phGo.GetComponent<RectTransform>(), 0);
        var phTx = phGo.AddComponent<TextMeshProUGUI>();
        phTx.fontSize  = 20;
        phTx.color     = new Color(0.5f, 0.5f, 0.55f);
        phTx.fontStyle = FontStyles.Italic;
        phTx.text      = placeholder;
        field.placeholder = phTx;

        // Text
        var txGo = new GameObject("Text", typeof(RectTransform));
        txGo.transform.SetParent(areaGo.transform, false);
        FillRect(txGo.GetComponent<RectTransform>(), 0);
        var inputTx = txGo.AddComponent<TextMeshProUGUI>();
        inputTx.fontSize = 20;
        inputTx.color    = Color.white;
        field.textComponent = inputTx;

        return field;
    }

    static void MakeMakerButton(Transform parent, string label,
        UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(0.28f, 0.38f, 0.55f, 0.92f);
        var b = go.AddComponent<Button>();
        b.onClick.AddListener(onClick);

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var tx = textGo.AddComponent<TextMeshProUGUI>();
        tx.fontSize = 22;
        tx.color = Color.white;
        tx.alignment = TextAlignmentOptions.Center;
        tx.text = label;
    }

    static void FillRect(RectTransform rt, float padding)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
    }

}

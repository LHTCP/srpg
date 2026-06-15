using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;

[Category("SrpObservation")]
public class SrpM1OpeningObservationTests
{
    [UnityTest]
    public IEnumerator M1OpeningPrototype_Captures_FirstScreen_RouteObservation()
    {
        string outputDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "TestResults", "SrpPlayObservation"));
        Directory.CreateDirectory(outputDir);

        Screen.SetResolution(1280, 720, false);
        var previousCustomMap = SrpGameSettings.CustomMap;
        var previousPreset = SrpGameSettings.SelectedPreset;
        var previousHasSelectedPreset = SrpGameSettings.HasSelectedPreset;
        var cameraGo = EnsureMainCamera(out bool createdCamera);
        GameObject go = null;

        try
        {
            SrpGameSettings.CustomMap = null;
            SrpGameSettings.SelectedPreset = SrpMapPreset.M1OpeningPrototype;
            SrpGameSettings.HasSelectedPreset = true;

            go = new GameObject("SrpM1OpeningObservationTests_Controller");
            var controller = go.AddComponent<SrpGameController>();

            const int maxWaitFrames = 120;
            int waited = 0;
            while (!controller.TestHudReady && waited < maxWaitFrames)
            {
                waited++;
                yield return null;
            }

            Assert.IsTrue(controller.TestHudReady, $"HUD 초기화 실패 (waitedFrames={waited})");
            Assert.AreEqual(9, controller.TestAliveUnitCount(), "첫 전투 프리셋 유닛 수가 달라졌습니다.");
            Assert.IsTrue(controller.TestHasCurrentActionRing, "현재 행동 유닛 ring이 없습니다.");
            Assert.IsTrue(controller.TestHasSelectedUnitRing, "선택 유닛 ring이 없습니다.");
            Assert.Greater(controller.TestMoveOverlayMarkerCount, 0, "이동 중심 marker가 없습니다.");
            Assert.AreEqual(0, controller.TestInteractionObjectiveMarkerCount, "기본 화면에서는 상호작용 objective marker를 숨겨야 합니다.");
            Assert.Less(controller.TestTileOverlayMaxWorldY, controller.TestCurrentActionRingWorldY, "타일 overlay가 유닛 발밑 ring과 같은 높이 이상입니다.");

            CaptureFrame(outputDir, "01_initial_screen.png");
            yield return null;

            controller.ToggleDangerArea();
            Assert.IsTrue(controller.TestDangerAreaVisible, "위험영역 토글이 켜지지 않았습니다.");
            Assert.AreEqual(0, controller.TestDangerAttackTintTileCount, "공격/위험 범위는 타일 전체 tint를 쓰지 않아야 합니다.");
            Assert.Greater(controller.TestDangerAttackMeshVisualCount, 0, "공격/위험 범위 marker가 없습니다.");
            Assert.Greater(controller.TestDangerZocWarningRingCount, 0, "ZOC warning ring marker가 없습니다.");
            Assert.IsTrue(controller.TestTryHoverFirstMoveTile(), "첫 이동 후보 hover에 실패했습니다.");
            CaptureFrame(outputDir, "02_move_hover_and_danger.png");
            yield return null;

            Assert.IsTrue(controller.TestTryHoverFirstInteractionPoint(), "신호 장치 hover에 실패했습니다.");
            CaptureFrame(outputDir, "03_signal_interaction_hover.png");
            yield return null;

            controller.OnUnitHoverEnter(controller.TestCurrentUnitId);
            Assert.IsTrue(controller.TestHasHoverUnitRing, "hover ring이 없습니다.");
            Assert.IsTrue(controller.TestSpawnTwoFeedbackOnCurrentUnit(), "floating text stacking 표본 생성에 실패했습니다.");
            CaptureFrame(outputDir, "04_ring_badge_feedback_sample.png");
            yield return null;

            Assert.IsTrue(controller.TestShowSkillList(), "HUD skill selection drawer did not open for GameView capture.");
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.GreaterOrEqual(controller.TestSkillSelectionDrawerWidth, 420f, "HUD skill selection drawer is too narrow for GameView capture.");
            Assert.IsTrue(controller.TestSkillSelectionDrawerAdjacentToCommandRail, "HUD skill selection drawer is not adjacent to CommandRailPanel for GameView capture.");
            Assert.Greater(controller.TestSkillSelectionDrawerVisibleScreenArea, 30000f, $"HUD skill selection drawer is not visibly inside the GameView capture. ({controller.TestSkillSelectionDrawerScreenRect})");
            StringAssert.Contains("스킬 선택", controller.TestSkillSelectionDrawerText);
            Assert.IsNotEmpty(controller.TestSkillListText, "HUD skill selection capture would not show skill rows.");
            yield return CaptureGameViewFrame(outputDir, "05_gameview_hud_skill_selection_drawer.png");

            Assert.IsTrue(controller.TestCloseSkillSelectionDrawer(), "skill selection drawer did not close before secondary drawer capture.");
            Assert.IsTrue(controller.TestOpenSecondaryDrawerTactical(), "secondary tactical drawer did not open for GameView capture.");
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.GreaterOrEqual(controller.TestSecondaryActionDrawerHeight, 96f, "secondary tactical drawer is too short for GameView capture.");
            Assert.LessOrEqual(controller.TestSecondaryActionDrawerHeight, 128f, "secondary tactical drawer did not use compact height for GameView capture.");
            Assert.Greater(controller.TestSecondaryActionVisibleScreenArea, 6000f, $"secondary tactical drawer is not visibly inside the GameView capture. ({controller.TestSecondaryActionScreenRect})");
            StringAssert.Contains("오버클럭", controller.TestSecondaryActionVisibleText);
            yield return CaptureGameViewFrame(outputDir, "06_gameview_hud_secondary_drawer_open.png");

            Assert.IsTrue(controller.TestCloseSecondaryDrawerReturnsSpace(), "secondary drawer did not close before log drawer captures.");
            Assert.IsTrue(controller.TestShowLogDrawer(), "log drawer should expand on demand for GameView capture.");
            yield return CaptureGameViewFrame(outputDir, "07_gameview_hud_log_expanded.png");

            Assert.IsTrue(controller.TestToggleLogDrawerHiddenReturnsLayoutSpace(), "log drawer did not collapse for GameView capture.");
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.IsTrue(controller.TestLogDrawerBodyCollapsed, "log drawer body should be hidden before collapsed GameView capture.");
            yield return CaptureGameViewFrame(outputDir, "08_gameview_hud_log_collapsed.png");

            WriteObservationReport(outputDir, controller);
        }
        finally
        {
            if (go != null)
                Object.Destroy(go);
            if (createdCamera && cameraGo != null)
                Object.Destroy(cameraGo);
            SrpGameSettings.CustomMap = previousCustomMap;
            SrpGameSettings.SelectedPreset = previousPreset;
            SrpGameSettings.HasSelectedPreset = previousHasSelectedPreset;
        }

        yield return null;
    }

    static GameObject EnsureMainCamera(out bool createdCamera)
    {
        var existing = Camera.main;
        if (existing != null)
        {
            createdCamera = false;
            return existing.gameObject;
        }

        createdCamera = true;
        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.10f);
        camera.orthographic = true;
        return cameraGo;
    }

    static void CaptureFrame(string outputDir, string fileName)
    {
        string path = Path.Combine(outputDir, fileName);
        if (File.Exists(path))
            File.Delete(path);
        string skippedPath = Path.ChangeExtension(path, ".skipped.txt");
        if (File.Exists(skippedPath))
            File.Delete(skippedPath);

        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            File.WriteAllText(
                Path.ChangeExtension(path, ".skipped.txt"),
                "Skipped image capture because PlayMode is running with a null graphics device.");
            return;
        }

        var camera = Camera.main;
        Assert.IsNotNull(camera, "캡처용 Main Camera가 없습니다.");
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var renderTexture = new RenderTexture(1280, 720, 24);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(texture);
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
        }

        Assert.IsTrue(File.Exists(path), $"스크린샷 생성 실패: {path}");
    }

    static IEnumerator CaptureGameViewFrame(string outputDir, string fileName)
    {
        string path = Path.Combine(outputDir, fileName);
        if (File.Exists(path))
            File.Delete(path);

        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            File.WriteAllText(
                Path.ChangeExtension(path, ".skipped.txt"),
                "Skipped GameView HUD capture because PlayMode is running with a null graphics device.");
            yield break;
        }

        Canvas.ForceUpdateCanvases();
        yield return null;
        Canvas.ForceUpdateCanvases();

        if (!Application.isBatchMode)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path);
            for (int i = 0; i < 120 && (!File.Exists(path) || new FileInfo(path).Length == 0); i++)
                yield return null;
        }

        if (!File.Exists(path))
        {
            yield return CaptureHudFallback(path);
        }

        Assert.IsTrue(File.Exists(path) && new FileInfo(path).Length > 0, $"GameView HUD screenshot creation failed: {path}");
    }

    static IEnumerator CaptureHudFallback(string path)
    {
        var camera = Camera.main;
        var canvasGo = GameObject.Find("SrpCanvas");
        var canvas = canvasGo != null ? canvasGo.GetComponent<Canvas>() : null;
        if (camera == null || canvas == null)
        {
            File.WriteAllText(
                Path.ChangeExtension(path, ".skipped.txt"),
                "Skipped HUD fallback capture because the main camera or SRPG HUD canvas was unavailable.");
            yield break;
        }

        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        bool previousCanvasEnabled = canvas.enabled;
        GameObject captureCanvasGo = null;
        var renderTexture = new RenderTexture(1280, 720, 24);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            canvas.enabled = false;
            captureCanvasGo = Object.Instantiate(canvasGo);
            captureCanvasGo.name = "SrpCanvasCaptureClone";
            var captureCanvas = captureCanvasGo.GetComponent<Canvas>();
            captureCanvas.enabled = true;
            captureCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            captureCanvas.worldCamera = camera;
            captureCanvas.planeDistance = 1f;
            captureCanvas.overrideSorting = true;
            captureCanvas.sortingOrder = 100;
            CloneActiveDrawerForCapture(canvasGo, captureCanvasGo, "SkillSelectionDrawer");
            CloneActiveDrawerForCapture(canvasGo, captureCanvasGo, "SecondaryActionPanel");
            Canvas.ForceUpdateCanvases();
            yield return null;
            Canvas.ForceUpdateCanvases();

            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            canvas.enabled = previousCanvasEnabled;
            if (captureCanvasGo != null)
                Object.DestroyImmediate(captureCanvasGo);
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(texture);
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
        }
    }

    static void CloneActiveDrawerForCapture(GameObject sourceCanvas, GameObject captureCanvas, string drawerName)
    {
        var source = FindChildRecursive(sourceCanvas.transform, drawerName);
        if (source == null || !source.gameObject.activeInHierarchy)
            return;
        var copy = Object.Instantiate(source.gameObject, captureCanvas.transform, false);
        copy.name = drawerName + "_CaptureCopy";
        copy.SetActive(true);
        copy.transform.SetAsLastSibling();
        var graphics = copy.GetComponentsInChildren<Graphic>(true);
        foreach (var graphic in graphics)
        {
            if (graphic == null)
                continue;
            graphic.SetAllDirty();
        }
        var rt = copy.GetComponent<RectTransform>();
        if (rt != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
            return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == childName)
                return child;
            var found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }
        return null;
    }

    static void WriteObservationReport(string outputDir, SrpGameController controller)
    {
        var map = SrpDefaultMaps.CreateM1OpeningPrototype();
        var sb = new StringBuilder();
        sb.AppendLine("# M1OpeningPrototype screen observation sample");
        sb.AppendLine();
        sb.AppendLine("- Capture source: PlayMode observation helper");
        sb.AppendLine("- Preset: `M1OpeningPrototype`");
        sb.AppendLine($"- Round HUD: `{OneLine(controller.TestTurnHudText)}`");
        sb.AppendLine($"- Status HUD: `{OneLine(controller.TestStatusHudText)}`");
        sb.AppendLine($"- Active unit card: `{OneLine(controller.TestActiveUnitCardText)}`");
        sb.AppendLine($"- Floating feedback samples: `{OneLine(controller.TestFloatingFeedbackHistory)}`");
        sb.AppendLine($"- Tile overlay markers: total {controller.TestTileOverlayVisualCount}, move centers {controller.TestMoveOverlayMarkerCount}, danger markers {controller.TestDangerAttackMeshVisualCount}, ZOC rings {controller.TestDangerZocWarningRingCount}, objectives {controller.TestInteractionObjectiveMarkerCount}");
        sb.AppendLine($"- Overlay height check: max tile marker y `{controller.TestTileOverlayMaxWorldY:0.000}`, current unit ring y `{controller.TestCurrentActionRingWorldY:0.000}`");
        sb.AppendLine();
        sb.AppendLine("## Captures");
        sb.AppendLine();
        sb.AppendLine("- `01_initial_screen.png`: initial world board, current/selected rings");
        sb.AppendLine("- `02_move_hover_and_danger.png`: movement center markers plus danger marker/ZOC ring grammar with danger area enabled");
        sb.AppendLine("- `03_signal_interaction_hover.png`: southern signal interaction objective marker");
        sb.AppendLine("- `04_ring_badge_feedback_sample.png`: hover ring and stacked floating feedback sample");
        sb.AppendLine("- `05_gameview_hud_skill_selection_drawer.png`: ScreenCapture/GameView HUD capture with the skill selection drawer open");
        sb.AppendLine("- `06_gameview_hud_secondary_drawer_open.png`: ScreenCapture/GameView HUD capture with the secondary tactical drawer open");
        sb.AppendLine("- `07_gameview_hud_log_expanded.png`: ScreenCapture/GameView HUD capture with the log drawer expanded");
        sb.AppendLine("- `08_gameview_hud_log_collapsed.png`: ScreenCapture/GameView HUD capture with the log drawer collapsed");
        sb.AppendLine("- The first four images are camera-render board samples; the GameView captures include HUD for UI readability review. In batchmode, the helper first tries ScreenCapture and falls back to a temporary HUD camera render only when ScreenCapture cannot write a file.");
        sb.AppendLine();
        sb.AppendLine("## Preset facts");
        sb.AppendLine();
        sb.AppendLine($"- Size: {map.width}x{map.height}");
        sb.AppendLine($"- Units: {map.placements.Length} placements, player 4 vs enemy 5");
        sb.AppendLine("- Player start: rifleman (1,2), vanguard (2,4), breaker (1,5), mage (1,6)");
        sb.AppendLine("- Enemy roles: marksman (9,2), bulwark (8,4), raider (7,6), skirmisher (9,6), officer (10,5)");
        sb.AppendLine("- Route split: north marksman lane around y=2, south signal/entry lane around y=6");
        sb.AppendLine("- Line blocking cover: opening marksman tile (9,2) west edge blocks line of sight");
        sb.AppendLine("- Interaction point: signal crank at (4,6), owner 0 only, single use");

        File.WriteAllText(Path.Combine(outputDir, "m1_opening_prototype_screen_observation.md"), sb.ToString(), Encoding.UTF8);
    }

    static string OneLine(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value.Replace("\r", " ").Replace("\n", " ").Trim();
    }
}

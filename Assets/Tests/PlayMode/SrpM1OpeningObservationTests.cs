using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

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
            Assert.Greater(controller.TestInteractionObjectiveMarkerCount, 0, "신호 장치 objective marker가 없습니다.");
            Assert.Greater(controller.TestInteractionObjectiveMarkerScale, controller.TestMoveOverlayMarkerScale, "신호 장치 marker가 이동 marker보다 작거나 같습니다.");
            Assert.Less(controller.TestTileOverlayMaxWorldY, controller.TestCurrentActionRingWorldY, "타일 overlay가 유닛 발밑 ring과 같은 높이 이상입니다.");

            CaptureFrame(outputDir, "01_initial_screen.png");
            yield return null;

            controller.ToggleDangerArea();
            Assert.IsTrue(controller.TestDangerAreaVisible, "위험영역 토글이 켜지지 않았습니다.");
            Assert.Greater(controller.TestDangerAttackTintTileCount, 0, "공격/위험 타일막이 없습니다.");
            Assert.IsTrue(controller.TestDangerAttackUsesFullTileTint, "공격/위험 범위가 반투명 타일막 계약을 사용하지 않습니다.");
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
        sb.AppendLine($"- Tile overlay markers: total {controller.TestTileOverlayVisualCount}, move centers {controller.TestMoveOverlayMarkerCount}, danger tint tiles {controller.TestDangerAttackTintTileCount}, ZOC rings {controller.TestDangerZocWarningRingCount}, objectives {controller.TestInteractionObjectiveMarkerCount}");
        sb.AppendLine($"- Overlay height check: max tile marker y `{controller.TestTileOverlayMaxWorldY:0.000}`, current unit ring y `{controller.TestCurrentActionRingWorldY:0.000}`");
        sb.AppendLine();
        sb.AppendLine("## Captures");
        sb.AppendLine();
        sb.AppendLine("- `01_initial_screen.png`: initial world board, current/selected rings");
        sb.AppendLine("- `02_move_hover_and_danger.png`: movement center markers plus translucent danger tile/ZOC ring grammar with danger area enabled");
        sb.AppendLine("- `03_signal_interaction_hover.png`: southern signal interaction objective marker");
        sb.AppendLine("- `04_ring_badge_feedback_sample.png`: hover ring and stacked floating feedback sample");
        sb.AppendLine("- HUD and log readability are recorded as text fields above because the batchmode capture uses camera rendering.");
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

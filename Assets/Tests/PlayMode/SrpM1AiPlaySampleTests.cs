using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

[Category("SrpM1All")]
[Category("SrpAiSim")]
public class SrpM1AiPlaySampleTests
{
    [UnityTest]
    public IEnumerator PlayMode_Runtime_Revalidation_For_AiQa()
    {
        var go = new GameObject("SrpM1AiPlaySampleTests_Controller");
        var controller = go.AddComponent<SrpGameController>();

        const int maxWaitFrames = 120;
        int waited = 0;
        while (!controller.TestHudReady && waited < maxWaitFrames)
        {
            waited++;
            yield return null;
        }

        Assert.IsTrue(controller.TestHudReady, $"HUD 초기화 실패 (waitedFrames={waited})");
        Assert.GreaterOrEqual(controller.TestRoundNumber, 1, "라운드 번호 초기화 실패");
        Assert.Greater(controller.TestCurrentUnitId, 0, "현재 행동 유닛 미설정");

        // 몇 프레임 경과 후에도 HUD 핵심 정보가 유지되는지 확인한다.
        for (int i = 0; i < 5; i++)
            yield return null;

        StringAssert.Contains("라운드", controller.TestTurnHudText);
        StringAssert.Contains("상태:", controller.TestTurnHudText);
        Assert.IsFalse(controller.TestTurnHudText.Contains("현재:"), "상단 HUD에 현재 유닛 정보가 다시 섞였습니다.");
        Assert.IsTrue(controller.TestHasTurnOrderTrackerPanel, "행동 순서 패널이 생성되지 않았습니다.");
        Assert.IsFalse(controller.TestTurnOrderTrackerIsLogChild, "행동 순서 패널이 로그 패널 위/안에 배치되었습니다.");
        Assert.IsTrue(controller.TestTurnOrderCurrentIconHighlighted, "현재 행동 유닛 아이콘 강조가 없습니다.");
        Assert.GreaterOrEqual(controller.TestTurnOrderVisibleIconCount, 4, "행동 순서 아이콘 preview가 부족합니다.");
        Assert.LessOrEqual(controller.TestTurnOrderVisibleIconCount, 6, "행동 순서 아이콘이 너무 많이 노출됩니다.");
        StringAssert.Contains($"({controller.TestCurrentUnitId})", controller.TestTurnOrderCurrentText);
        StringAssert.Contains("NOW >", controller.TestTurnOrderCurrentText);
        StringAssert.Contains("SPD", controller.TestTurnOrderTrackerText);
        StringAssert.Contains("P", controller.TestTurnOrderTrackerText);
        Assert.GreaterOrEqual(controller.TestTurnOrderPreviewLineCount, 3, "다음 행동 순서 preview가 부족합니다.");
        Assert.LessOrEqual(controller.TestTurnOrderPreviewLineCount, 5, "다음 행동 순서 preview가 너무 깁니다.");
        Assert.IsTrue(controller.TestHasTopStatusPanel, "상단 전투 상태 헤더가 생성되지 않았습니다.");
        Assert.IsTrue(controller.TestHasLeftConsolePanel, "좌측 조작 콘솔이 생성되지 않았습니다.");
        StringAssert.Contains("범례:", controller.TestStatusHudText);
        StringAssert.Contains("청록=패링 가능 스킬", controller.TestStatusHudText);
        StringAssert.Contains("AP", controller.TestUnitHudText);
        StringAssert.Contains("반응:", controller.TestUnitHudText);
        StringAssert.Contains("PG", controller.TestUnitHudText);
        StringAssert.Contains("오버워치", controller.TestOverwatchButtonText);
        StringAssert.Contains("행동 시작", controller.TestLogText);

        Object.Destroy(go);
        yield return null;
    }
}

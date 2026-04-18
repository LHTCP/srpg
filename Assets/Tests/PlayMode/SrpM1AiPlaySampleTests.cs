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
        StringAssert.Contains("현재:", controller.TestTurnHudText);
        StringAssert.Contains("AP", controller.TestUnitHudText);
        StringAssert.Contains("RP", controller.TestUnitHudText);
        StringAssert.Contains("PG", controller.TestUnitHudText);

        Object.Destroy(go);
        yield return null;
    }
}

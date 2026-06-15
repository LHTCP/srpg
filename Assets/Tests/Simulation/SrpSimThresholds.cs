using UnityEngine;

public struct SrpSimThresholdConfig
{
    public float minFirearmHpShare;
    public float minMeleePgShare;
    public float maxDrawRate;
    public float maxAverageRounds;

    public static SrpSimThresholdConfig Default()
    {
        return new SrpSimThresholdConfig
        {
            minFirearmHpShare = 0.45f,
            minMeleePgShare = 0.44f,
            maxDrawRate = 0.20f,
            maxAverageRounds = 16f,
        };
    }
}

public static class SrpSimThresholds
{
    public static SrpSimThresholdResult Evaluate(SrpSimReport report, SrpSimThresholdConfig config)
    {
        var result = new SrpSimThresholdResult { pass = true };

        if (report.combat.firearmHpShare < config.minFirearmHpShare)
        {
            result.pass = false;
            result.warnings.Add(
                $"총기 HP 비중 저하: {report.combat.firearmHpShare:F3} < {config.minFirearmHpShare:F3}");
        }

        if (report.combat.meleePgShare < config.minMeleePgShare)
        {
            result.pass = false;
            result.warnings.Add(
                $"근접 PG 비중 저하: {report.combat.meleePgShare:F3} < {config.minMeleePgShare:F3}");
        }

        if (report.outcome.drawRate > config.maxDrawRate)
        {
            result.pass = false;
            result.warnings.Add(
                $"무승부 비율 과다: {report.outcome.drawRate:F3} > {config.maxDrawRate:F3}");
        }

        if (report.outcome.averageRounds > config.maxAverageRounds)
        {
            result.pass = false;
            result.warnings.Add(
                $"평균 라운드 과다: {report.outcome.averageRounds:F2} > {config.maxAverageRounds:F2}");
        }

        if (result.warnings.Count == 0)
            result.warnings.Add("임계치 판정 통과");
        return result;
    }

    public static float SafeDiv(int numerator, int denominator)
    {
        if (denominator <= 0) return 0f;
        return numerator / (float)denominator;
    }
}

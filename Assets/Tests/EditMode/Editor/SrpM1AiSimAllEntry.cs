using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[Category("SrpM1All")]
[Category("SrpAiSim")]
public class SrpM1AiSimAllEntry
{
    struct PolicyCase
    {
        public string name;
        public Func<ISrpAiPolicy> owner0Factory;
        public Func<ISrpAiPolicy> owner1Factory;
        public bool enforceThreshold;
    }

    [Test]
    public void Run_M1_Hybrid_Ai_Simulation_And_Validate_Thresholds()
    {
        var config = new SrpBattleSimConfig
        {
            mapPreset = SrpMapPreset.M1QaIntegrated,
            trials = 500,
            maxRounds = 24,
            baseSeed = 20260418,
            sampleSeedCount = 5,
            owner0Policy = new SrpHeuristicAiPolicy(),
            owner1Policy = new SrpRandomAiPolicy(),
            thresholds = SrpSimThresholdConfig.Default(),
        };

        var output = SrpBattleSimRunner.RunBatch(config, writeReport: true);
        Assert.IsNotNull(output.report, "AI 시뮬레이션 리포트 생성 실패");
        Assert.IsFalse(string.IsNullOrEmpty(output.reportPath), "JSON 리포트 경로 생성 실패");
        Assert.IsTrue(
            output.report.threshold.pass,
            "[AI 시뮬레이션 임계치 실패]\n" + string.Join("\n", output.report.threshold.warnings));

        Debug.Log(
            $"[SRPG][AI-Sim] trials={output.report.runMeta.trials}, " +
            $"avgRounds={output.report.outcome.averageRounds:F2}, " +
            $"drawRate={output.report.outcome.drawRate:F3}, report={output.reportPath}");
    }

    [Test]
    public void Run_M1_Ai_Policy_Matrix_Comparison()
    {
        const int trialsPerCase = 300;
        const int maxRounds = 24;
        const int baseSeed = 20260418;
        var thresholds = SrpSimThresholdConfig.Default();

        var cases = new List<PolicyCase>
        {
            new PolicyCase
            {
                name = "Heuristic_vs_Random",
                owner0Factory = () => new SrpHeuristicAiPolicy(),
                owner1Factory = () => new SrpRandomAiPolicy(),
                enforceThreshold = true,
            },
            new PolicyCase
            {
                name = "Random_vs_Heuristic",
                owner0Factory = () => new SrpRandomAiPolicy(),
                owner1Factory = () => new SrpHeuristicAiPolicy(),
                enforceThreshold = true,
            },
            new PolicyCase
            {
                name = "Heuristic_vs_Heuristic",
                owner0Factory = () => new SrpHeuristicAiPolicy(),
                owner1Factory = () => new SrpHeuristicAiPolicy(),
                enforceThreshold = true,
            },
            new PolicyCase
            {
                name = "Random_vs_Random",
                owner0Factory = () => new SrpRandomAiPolicy(),
                owner1Factory = () => new SrpRandomAiPolicy(),
                // 완전 랜덤 미러전은 장기전/무승부 편향이 자연스럽게 발생하므로
                // 회귀 게이트가 아니라 경향 관찰 케이스로만 사용한다.
                enforceThreshold = false,
            },
        };

        var matrix = new SrpSimPolicyMatrixReport
        {
            timestampUtc = DateTime.UtcNow.ToString("o"),
            mapPreset = SrpMapPreset.M1QaIntegrated.ToString(),
            trialsPerCase = trialsPerCase,
            maxRounds = maxRounds,
        };

        for (int i = 0; i < cases.Count; i++)
        {
            var c = cases[i];
            var config = new SrpBattleSimConfig
            {
                mapPreset = SrpMapPreset.M1QaIntegrated,
                trials = trialsPerCase,
                maxRounds = maxRounds,
                baseSeed = baseSeed + (i * 100000),
                sampleSeedCount = 5,
                owner0Policy = c.owner0Factory(),
                owner1Policy = c.owner1Factory(),
                thresholds = thresholds,
            };

            var output = SrpBattleSimRunner.RunBatch(config, writeReport: true);
            Assert.IsNotNull(output.report, $"[{c.name}] 리포트 생성 실패");
            Assert.IsFalse(string.IsNullOrEmpty(output.reportPath), $"[{c.name}] JSON 경로 누락");

            matrix.cases.Add(new SrpSimPolicyMatrixCaseResult
            {
                caseName = c.name,
                owner0Policy = output.report.runMeta.owner0Policy,
                owner1Policy = output.report.runMeta.owner1Policy,
                trials = output.report.runMeta.trials,
                owner0WinRate = output.report.outcome.owner0WinRate,
                owner1WinRate = output.report.outcome.owner1WinRate,
                drawRate = output.report.outcome.drawRate,
                averageRounds = output.report.outcome.averageRounds,
                firearmHpShare = output.report.combat.firearmHpShare,
                meleePgShare = output.report.combat.meleePgShare,
                zocPenaltyAverage = output.report.control.zocPenaltyAverage,
                thresholdPass = output.report.threshold.pass,
                warnings = new List<string>(output.report.threshold.warnings),
                reportPath = output.reportPath,
            });

            Debug.Log(
                $"[SRPG][AI-Sim][Matrix] {c.name} | " +
                $"w0={output.report.outcome.owner0WinRate:F3}, " +
                $"w1={output.report.outcome.owner1WinRate:F3}, " +
                $"draw={output.report.outcome.drawRate:F3}, " +
                $"avgR={output.report.outcome.averageRounds:F2}");
        }

        string matrixPath = SrpSimReportWriter.WriteMatrix(matrix);
        Assert.IsFalse(string.IsNullOrEmpty(matrixPath), "정책 매트릭스 요약 JSON 생성 실패");

        for (int i = 0; i < matrix.cases.Count; i++)
        {
            var row = matrix.cases[i];
            if (cases[i].enforceThreshold)
            {
                Assert.IsTrue(row.thresholdPass,
                    $"[AI 정책 매트릭스 임계치 실패] {row.caseName}\n{string.Join("\n", row.warnings)}");
            }
            else if (!row.thresholdPass)
            {
                Debug.Log(
                    $"[SRPG][AI-Sim][Matrix][WarnOnly] {row.caseName}\n" +
                    string.Join("\n", row.warnings));
            }
        }

        Debug.Log($"[SRPG][AI-Sim][Matrix] summary={matrixPath}");
    }

    [Test]
    public void Run_M1OpeningPrototype_Ai_Policy_Matrix_For_BalanceObservation()
    {
        const int trialsPerCase = 300;
        const int maxRounds = 16;
        const int baseSeed = 20260608;
        const float minAverageRounds = 6f;
        const float maxAverageRounds = 13f;
        var thresholds = SrpSimThresholdConfig.Default();

        var cases = new List<PolicyCase>
        {
            new PolicyCase
            {
                name = "Opening_Heuristic_vs_Random",
                owner0Factory = () => new SrpHeuristicAiPolicy(),
                owner1Factory = () => new SrpRandomAiPolicy(),
                enforceThreshold = true,
            },
            new PolicyCase
            {
                name = "Opening_Random_vs_Heuristic",
                owner0Factory = () => new SrpRandomAiPolicy(),
                owner1Factory = () => new SrpHeuristicAiPolicy(),
                enforceThreshold = true,
            },
            new PolicyCase
            {
                name = "Opening_Heuristic_vs_Heuristic",
                owner0Factory = () => new SrpHeuristicAiPolicy(),
                owner1Factory = () => new SrpHeuristicAiPolicy(),
                enforceThreshold = false,
            },
            new PolicyCase
            {
                name = "Opening_Random_vs_Random",
                owner0Factory = () => new SrpRandomAiPolicy(),
                owner1Factory = () => new SrpRandomAiPolicy(),
                enforceThreshold = false,
            },
        };

        var matrix = new SrpSimPolicyMatrixReport
        {
            timestampUtc = DateTime.UtcNow.ToString("o"),
            mapPreset = SrpMapPreset.M1OpeningPrototype.ToString(),
            trialsPerCase = trialsPerCase,
            maxRounds = maxRounds,
        };

        for (int i = 0; i < cases.Count; i++)
        {
            var c = cases[i];
            var config = new SrpBattleSimConfig
            {
                mapPreset = SrpMapPreset.M1OpeningPrototype,
                trials = trialsPerCase,
                maxRounds = maxRounds,
                baseSeed = baseSeed + (i * 100000),
                sampleSeedCount = 5,
                owner0Policy = c.owner0Factory(),
                owner1Policy = c.owner1Factory(),
                thresholds = thresholds,
            };

            var output = SrpBattleSimRunner.RunBatch(config, writeReport: true);
            Assert.IsNotNull(output.report, $"[{c.name}] 리포트 생성 실패");
            Assert.IsFalse(string.IsNullOrEmpty(output.reportPath), $"[{c.name}] JSON 경로 누락");

            matrix.cases.Add(new SrpSimPolicyMatrixCaseResult
            {
                caseName = c.name,
                owner0Policy = output.report.runMeta.owner0Policy,
                owner1Policy = output.report.runMeta.owner1Policy,
                trials = output.report.runMeta.trials,
                owner0WinRate = output.report.outcome.owner0WinRate,
                owner1WinRate = output.report.outcome.owner1WinRate,
                drawRate = output.report.outcome.drawRate,
                averageRounds = output.report.outcome.averageRounds,
                firearmHpShare = output.report.combat.firearmHpShare,
                meleePgShare = output.report.combat.meleePgShare,
                zocPenaltyAverage = output.report.control.zocPenaltyAverage,
                thresholdPass = output.report.threshold.pass,
                warnings = new List<string>(output.report.threshold.warnings),
                reportPath = output.reportPath,
            });

            Debug.Log(
                $"[SRPG][AI-Sim][OpeningMatrix] {c.name} | " +
                $"w0={output.report.outcome.owner0WinRate:F3}, " +
                $"w1={output.report.outcome.owner1WinRate:F3}, " +
                $"draw={output.report.outcome.drawRate:F3}, " +
                $"avgR={output.report.outcome.averageRounds:F2}");
        }

        string matrixPath = SrpSimReportWriter.WriteMatrix(matrix);
        Assert.IsFalse(string.IsNullOrEmpty(matrixPath), "첫 전투 정책 매트릭스 요약 JSON 생성 실패");

        for (int i = 0; i < matrix.cases.Count; i++)
        {
            var row = matrix.cases[i];
            if (cases[i].enforceThreshold)
            {
                Assert.GreaterOrEqual(row.averageRounds, minAverageRounds,
                    $"[첫 전투 밸런스 관찰 실패] {row.caseName} 평균 라운드가 너무 짧습니다.");
                Assert.LessOrEqual(row.averageRounds, maxAverageRounds,
                    $"[첫 전투 밸런스 관찰 실패] {row.caseName} 평균 라운드가 너무 깁니다.");
            }
        }

        Debug.Log($"[SRPG][AI-Sim][OpeningMatrix] summary={matrixPath}");
    }
}

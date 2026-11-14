using System;
using System.Collections.Generic;

[Serializable]
public class SrpSimMetrics
{
    public int totalTrials;
    public int completedTrials;
    public int totalRounds;
    public int drawCount;
    public int owner0Wins;
    public int owner1Wins;

    public int totalExecutionCount;
    public float totalZocPenalty;
    public int zocSampleCount;

    public int firearmHpDamage;
    public int firearmPgDamage;
    public int meleeHpDamage;
    public int meleePgDamage;
    public int magicHpDamage;
    public int magicPgDamage;

    public int aggressiveAttackCount;
    public int defensiveAttackCount;

    public int reloadCount;
    public int skillUseCount;
    public int overwatchArmCount;
    public int overwatchShotCount;
    public int opportunityAttackCount;
    public int groggySkipCount;
    public int coverCount;
    public int interactionCount;

    public int annihilationCount;
    public int commanderKillCount;
    public int objectiveCount;

    public List<int> sampledSeeds = new List<int>();
}

[Serializable]
public class SrpSimRunMeta
{
    public string timestampUtc;
    public int baseSeed;
    public int trials;
    public string mapPreset;
    public string owner0Policy;
    public string owner1Policy;
}

[Serializable]
public class SrpSimOutcomeSummary
{
    public float owner0WinRate;
    public float owner1WinRate;
    public float drawRate;
    public float averageRounds;
    /// <summary>전체 판 중 각 방식으로 끝난 비율. 무승부와 라운드 상한은 어디에도 들지 않는다.</summary>
    public float annihilationRate;
    public float commanderKillRate;
    public float objectiveRate;
}

[Serializable]
public class SrpSimCombatSummary
{
    public float firearmHpShare;
    public float meleePgShare;
    public float executionRate;
}

[Serializable]
public class SrpSimControlSummary
{
    public float zocPenaltyAverage;
    public int aggressiveAttackCount;
    public int defensiveAttackCount;
}

/// <summary>판당 평균 행동/반응 횟수. 시뮬레이션이 런타임 AI와 같은 수단을 쓰는지 확인하는 용도다.</summary>
[Serializable]
public class SrpSimActionSummary
{
    public float reloadsPerBattle;
    public float skillUsesPerBattle;
    public float overwatchArmsPerBattle;
    public float overwatchShotsPerBattle;
    public float opportunityAttacksPerBattle;
    public float groggySkipsPerBattle;
    public float coversPerBattle;
    public float interactionsPerBattle;
}

[Serializable]
public class SrpSimThresholdResult
{
    public bool pass;
    public List<string> warnings = new List<string>();
}

[Serializable]
public class SrpSimReport
{
    public SrpSimRunMeta runMeta = new SrpSimRunMeta();
    public SrpSimOutcomeSummary outcome = new SrpSimOutcomeSummary();
    public SrpSimCombatSummary combat = new SrpSimCombatSummary();
    public SrpSimControlSummary control = new SrpSimControlSummary();
    public SrpSimActionSummary actions = new SrpSimActionSummary();
    public SrpSimThresholdResult threshold = new SrpSimThresholdResult();
    public List<int> sampleSeeds = new List<int>();
}

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
    public SrpSimThresholdResult threshold = new SrpSimThresholdResult();
    public List<int> sampleSeeds = new List<int>();
}

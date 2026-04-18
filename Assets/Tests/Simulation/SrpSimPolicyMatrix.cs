using System;
using System.Collections.Generic;

[Serializable]
public class SrpSimPolicyMatrixCaseResult
{
    public string caseName;
    public string owner0Policy;
    public string owner1Policy;
    public int trials;
    public float owner0WinRate;
    public float owner1WinRate;
    public float drawRate;
    public float averageRounds;
    public float firearmHpShare;
    public float meleePgShare;
    public float zocPenaltyAverage;
    public bool thresholdPass;
    public List<string> warnings = new List<string>();
    public string reportPath;
}

[Serializable]
public class SrpSimPolicyMatrixReport
{
    public string timestampUtc;
    public string mapPreset;
    public int trialsPerCase;
    public int maxRounds;
    public List<SrpSimPolicyMatrixCaseResult> cases = new List<SrpSimPolicyMatrixCaseResult>();
}

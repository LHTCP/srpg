using System;
using System.IO;
using UnityEngine;

public static class SrpSimReportWriter
{
    static string _lastReportPath;

    public static string Write(SrpSimReport report)
    {
        string fileName = $"srpg_ai_sim_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        return WriteJson(fileName, JsonUtility.ToJson(report, true));
    }

    public static string WriteMatrix(SrpSimPolicyMatrixReport matrixReport)
    {
        string fileName = $"srpg_ai_sim_matrix_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        return WriteJson(fileName, JsonUtility.ToJson(matrixReport, true));
    }

    static string WriteJson(string fileName, string json)
    {
        string dir = GetReportDirectory();
        Directory.CreateDirectory(dir);

        string fullPath = Path.Combine(dir, fileName);
        File.WriteAllText(fullPath, json);
        _lastReportPath = fullPath;
        return fullPath;
    }

    public static string GetReportDirectory()
    {
        return Path.Combine(Directory.GetCurrentDirectory(), "TestResults", "SrpSim");
    }

    public static string GetLastReportPath()
    {
        if (!string.IsNullOrEmpty(_lastReportPath) && File.Exists(_lastReportPath))
            return _lastReportPath;
        return GetLatestReportPath();
    }

    public static string GetLatestReportPath()
    {
        string dir = GetReportDirectory();
        if (!Directory.Exists(dir))
            return null;

        var files = Directory.GetFiles(dir, "*.json");
        if (files.Length == 0)
            return null;

        Array.Sort(files, StringComparer.Ordinal);
        return files[files.Length - 1];
    }

    public static SrpSimReport Read(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return null;
        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<SrpSimReport>(json);
    }
}

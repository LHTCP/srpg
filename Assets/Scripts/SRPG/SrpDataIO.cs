using System.IO;
using UnityEngine;

public static class SrpDataIO
{
    static string DataDirectory
    {
        get
        {
            string dir = Path.Combine(Application.persistentDataPath, "SrpData");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }
    }

    static string SkillsPath => Path.Combine(DataDirectory, "skills.json");
    static string UnitsPath  => Path.Combine(DataDirectory, "units.json");

    // ── Skills ───────────────────────────────────────────────────────────────

    public static SrpSkillData[] LoadSkills()
    {
        if (!File.Exists(SkillsPath))
            return null;
        string json = File.ReadAllText(SkillsPath);
        var db = JsonUtility.FromJson<SrpSkillDatabase>(json);
        return db?.skills;
    }

    public static SrpSkillData[] LoadSkillsOrDefault()
    {
        var skills = LoadSkills();
        if (skills == null || skills.Length == 0)
        {
            skills = SrpDefaultSkills.Create();
            SaveSkills(skills);
        }
        return skills;
    }

    public static void SaveSkills(SrpSkillData[] skills)
    {
        var db = new SrpSkillDatabase { skills = skills ?? new SrpSkillData[0] };
        string json = JsonUtility.ToJson(db, true);
        File.WriteAllText(SkillsPath, json);
    }

    // ── Units ────────────────────────────────────────────────────────────────

    public static SrpUnitTemplateData[] LoadUnits()
    {
        if (!File.Exists(UnitsPath))
            return null;
        string json = File.ReadAllText(UnitsPath);
        var db = JsonUtility.FromJson<SrpUnitDatabase>(json);
        return db?.units;
    }

    public static SrpUnitTemplateData[] LoadUnitsOrDefault()
    {
        var units = LoadUnits();
        if (units == null || units.Length == 0)
        {
            units = SrpDefaultUnits.Create();
            SaveUnits(units);
        }
        return units;
    }

    public static void SaveUnits(SrpUnitTemplateData[] units)
    {
        var db = new SrpUnitDatabase { units = units ?? new SrpUnitTemplateData[0] };
        string json = JsonUtility.ToJson(db, true);
        File.WriteAllText(UnitsPath, json);
    }
}

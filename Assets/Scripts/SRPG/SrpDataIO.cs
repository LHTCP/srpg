using System.Collections.Generic;
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

    public static string SkillsPath => Path.Combine(DataDirectory, "skills.json");
    public static string UnitsPath  => Path.Combine(DataDirectory, "units.json");

    // ── Skills ───────────────────────────────────────────────────────────────

    public static SrpSkillData[] LoadSkills()
    {
        return LoadSkillDatabase()?.skills;
    }

    static SrpSkillDatabase LoadSkillDatabase()
    {
        if (!File.Exists(SkillsPath))
            return null;
        string json = File.ReadAllText(SkillsPath);
        return JsonUtility.FromJson<SrpSkillDatabase>(json);
    }

    /// <summary>
    /// 로컬 스킬 DB를 읽는다. 파일이 없으면 기본 스킬로 만든다.
    /// 파일이 예전 기본 데이터 버전으로 저장됐으면 기본 스킬(내장 id)을 최신 값으로 갱신하고,
    /// 사용자가 만든 스킬(내장 id가 아닌 것)은 그대로 둔다. 갱신 전 파일은 백업한다.
    /// </summary>
    public static SrpSkillData[] LoadSkillsOrDefault()
    {
        var db = LoadSkillDatabase();
        if (db == null || db.skills == null || db.skills.Length == 0)
        {
            var defaults = SrpDefaultSkills.Create();
            SaveSkills(defaults);
            return defaults;
        }

        if (db.defaultsVersion < SrpDefaultSkills.Version)
        {
            BackupBeforeDefaultsUpdate(SkillsPath, db.defaultsVersion);
            var merged = SrpDefaultDataMerge.MergeSkills(db.skills, SrpDefaultSkills.Create());
            SaveSkills(merged);
            Debug.Log($"[SrpDataIO] 기본 스킬 데이터를 v{db.defaultsVersion} → v{SrpDefaultSkills.Version}로 갱신했습니다. 이전 파일은 백업했습니다.");
            return merged;
        }
        return db.skills;
    }

    public static void SaveSkills(SrpSkillData[] skills)
    {
        var db = new SrpSkillDatabase
        {
            defaultsVersion = SrpDefaultSkills.Version,
            skills = skills ?? new SrpSkillData[0],
        };
        string json = JsonUtility.ToJson(db, true);
        File.WriteAllText(SkillsPath, json);
    }

    // ── Units ────────────────────────────────────────────────────────────────

    public static SrpUnitTemplateData[] LoadUnits()
    {
        return LoadUnitDatabase()?.units;
    }

    static SrpUnitDatabase LoadUnitDatabase()
    {
        if (!File.Exists(UnitsPath))
            return null;
        string json = File.ReadAllText(UnitsPath);
        return JsonUtility.FromJson<SrpUnitDatabase>(json);
    }

    /// <summary>로컬 유닛 DB를 읽는다. 기본 데이터 버전 갱신 규칙은 스킬과 같다.</summary>
    public static SrpUnitTemplateData[] LoadUnitsOrDefault()
    {
        var db = LoadUnitDatabase();
        if (db == null || db.units == null || db.units.Length == 0)
        {
            var defaults = SrpDefaultUnits.Create();
            SaveUnits(defaults);
            return defaults;
        }

        if (db.defaultsVersion < SrpDefaultUnits.Version)
        {
            BackupBeforeDefaultsUpdate(UnitsPath, db.defaultsVersion);
            var merged = SrpDefaultDataMerge.MergeUnits(db.units, SrpDefaultUnits.Create());
            SaveUnits(merged);
            Debug.Log($"[SrpDataIO] 기본 유닛 데이터를 v{db.defaultsVersion} → v{SrpDefaultUnits.Version}로 갱신했습니다. 이전 파일은 백업했습니다.");
            return merged;
        }
        return db.units;
    }

    public static void SaveUnits(SrpUnitTemplateData[] units)
    {
        var db = new SrpUnitDatabase
        {
            defaultsVersion = SrpDefaultUnits.Version,
            units = units ?? new SrpUnitTemplateData[0],
        };
        string json = JsonUtility.ToJson(db, true);
        File.WriteAllText(UnitsPath, json);
    }

    /// <summary>기본 데이터 갱신 전 파일을 같은 폴더에 복사해 둔다. 예: skills.backup-v0.json</summary>
    static void BackupBeforeDefaultsUpdate(string path, int oldVersion)
    {
        try
        {
            string dir = Path.GetDirectoryName(path);
            string name = Path.GetFileNameWithoutExtension(path);
            string backup = Path.Combine(dir, $"{name}.backup-v{oldVersion}.json");
            File.Copy(path, backup, true);
        }
        catch (IOException e)
        {
            // 백업 실패가 전투 시작을 막지는 않게 한다.
            Debug.LogWarning($"[SrpDataIO] 백업 실패: {e.Message}");
        }
    }
}

/// <summary>
/// 로컬에 저장된 DB와 내장 기본 데이터를 합치는 규칙(순수 로직).
/// 내장 id와 같은 항목은 기본 데이터의 최신 값으로 바꾸고, 그 밖의 항목(사용자가 만든 것)은 순서대로 뒤에 둔다.
/// </summary>
public static class SrpDefaultDataMerge
{
    public static SrpSkillData[] MergeSkills(SrpSkillData[] saved, SrpSkillData[] defaults)
    {
        var result = new List<SrpSkillData>();
        var defaultIds = new HashSet<string>();
        if (defaults != null)
        {
            foreach (var skill in defaults)
            {
                if (skill == null || string.IsNullOrEmpty(skill.id) || !defaultIds.Add(skill.id))
                    continue;
                result.Add(skill);
            }
        }
        if (saved != null)
        {
            foreach (var skill in saved)
            {
                if (skill == null || string.IsNullOrEmpty(skill.id) || defaultIds.Contains(skill.id))
                    continue;
                result.Add(skill);
            }
        }
        return result.ToArray();
    }

    public static SrpUnitTemplateData[] MergeUnits(SrpUnitTemplateData[] saved, SrpUnitTemplateData[] defaults)
    {
        var result = new List<SrpUnitTemplateData>();
        var defaultIds = new HashSet<string>();
        if (defaults != null)
        {
            foreach (var unit in defaults)
            {
                if (unit == null || string.IsNullOrEmpty(unit.id) || !defaultIds.Add(unit.id))
                    continue;
                result.Add(unit);
            }
        }
        if (saved != null)
        {
            foreach (var unit in saved)
            {
                if (unit == null || string.IsNullOrEmpty(unit.id) || defaultIds.Contains(unit.id))
                    continue;
                result.Add(unit);
            }
        }
        return result.ToArray();
    }
}

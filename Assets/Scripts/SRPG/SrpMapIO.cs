using System.IO;
using UnityEngine;

public static class SrpMapIO
{
    public static string MapsDirectory
    {
        get
        {
            string dir = Path.Combine(Application.persistentDataPath, "SrpMaps");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string Save(SrpMapFileV1 map, string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            fileName = "map";
        if (!fileName.EndsWith(".json"))
            fileName += ".json";
        string path = Path.Combine(MapsDirectory, fileName);
        string json = JsonUtility.ToJson(map, true);
        File.WriteAllText(path, json);
        return path;
    }

    public static bool TryLoad(string fileName, out SrpMapFileV1 map)
    {
        map = null;
        if (string.IsNullOrEmpty(fileName))
            return false;
        if (!fileName.EndsWith(".json"))
            fileName += ".json";
        string path = Path.Combine(MapsDirectory, fileName);
        if (!File.Exists(path))
            return false;
        string json = File.ReadAllText(path);
        map = JsonUtility.FromJson<SrpMapFileV1>(json);
        return map != null && map.version >= 1;
    }
}

using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static string Path => Application.persistentDataPath + "/save.json";

    public static void Save(PlayerRuntimeData data)
    {
        Debug.Log("Saving to: " + Path);
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(Path, json);
    }

    public static PlayerRuntimeData Load()
    {
        if (!File.Exists(Path))
            return null;

        string json = File.ReadAllText(Path);
        return JsonUtility.FromJson<PlayerRuntimeData>(json);
    }
}
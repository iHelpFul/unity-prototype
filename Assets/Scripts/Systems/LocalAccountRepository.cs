using System.IO;
using UnityEngine;

public class LocalAccountRepository
{
    private string AccountPath => Application.persistentDataPath + "/account_profile.json";

    public AccountProfileData Load()
    {
        if (!File.Exists(AccountPath))
            return null;

        string json = File.ReadAllText(AccountPath);
        return JsonUtility.FromJson<AccountProfileData>(json);
    }

    public void Save(AccountProfileData data)
    {
        if (data == null)
            return;

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(AccountPath, json);
    }
}

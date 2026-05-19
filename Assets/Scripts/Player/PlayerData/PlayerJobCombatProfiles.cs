using System.Collections.Generic;
using UnityEngine;

public static class PlayerJobCombatProfiles
{
    private const string ResourcePath = "GameData/PlayerJobDatabase";

    private static Dictionary<PlayerJobType, PlayerJobDefinition> definitions;
    private static PlayerJobDatabaseAsset asset;

    public static PlayerBasicAttackProfile GetBasicAttackProfile(PlayerJobType jobType)
    {
        PlayerJobDefinition definition = GetJobDefinition(jobType);
        return definition != null ? definition.BasicAttackProfile : null;
    }

    public static IReadOnlyList<PlayerSkillDefinition> GetDefaultSkillsForJob(PlayerJobType jobType)
    {
        PlayerJobDefinition definition = GetJobDefinition(jobType);
        if (definition != null && definition.DefaultSkills != null && definition.DefaultSkills.Count > 0)
        {
            List<PlayerSkillDefinition> sortedSkills = new List<PlayerSkillDefinition>();

            for (int index = 0; index < definition.DefaultSkills.Count; index++)
            {
                PlayerSkillDefinition skill = definition.DefaultSkills[index];
                if (skill != null)
                    sortedSkills.Add(skill);
            }

            sortedSkills.Sort((left, right) => left.DefaultSlotIndex.CompareTo(right.DefaultSlotIndex));
            return sortedSkills;
        }

        return System.Array.Empty<PlayerSkillDefinition>();
    }

    public static PlayerJobDefinition GetJobDefinition(PlayerJobType jobType)
    {
        EnsureLoaded();

        if (definitions != null && definitions.TryGetValue(jobType, out PlayerJobDefinition definition) && definition != null)
            return definition;

        return null;
    }

    public static string GetDisplayName(PlayerJobType jobType)
    {
        PlayerJobDefinition definition = GetJobDefinition(jobType);
        return definition != null && !string.IsNullOrWhiteSpace(definition.DisplayName)
            ? definition.DisplayName
            : jobType.ToString();
    }

    public static bool IsJobAdvancementAvailable(PlayerRuntimeData data)
    {
        if (data == null || data.CurrentJob != PlayerJobType.Novice)
            return false;

        PlayerJobDefinition noviceDefinition = GetJobDefinition(PlayerJobType.Novice);
        if (noviceDefinition == null)
            return false;

        return data.Level >= Mathf.Max(1, noviceDefinition.AdvancementLevelRequirement);
    }

    public static void ResetCache()
    {
        definitions = null;
        asset = null;
    }

    private static void EnsureLoaded()
    {
        if (definitions != null)
            return;

        asset = Resources.Load<PlayerJobDatabaseAsset>(ResourcePath);
        definitions = new Dictionary<PlayerJobType, PlayerJobDefinition>();

        if (asset == null)
        {
            Debug.LogError($"PlayerJobDatabaseAsset was not found at Resources/{ResourcePath}.");
            return;
        }

        if (asset.Definitions == null || asset.Definitions.Count == 0)
        {
            Debug.LogError("PlayerJobDatabaseAsset is loaded but does not contain any job definitions.");
            return;
        }

        for (int index = 0; index < asset.Definitions.Count; index++)
        {
            PlayerJobDefinition definition = asset.Definitions[index];
            if (definition == null)
                continue;

            definitions[definition.JobType] = definition;
        }
    }
}

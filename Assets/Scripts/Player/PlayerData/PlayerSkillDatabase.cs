using System.Collections.Generic;
using UnityEngine;

public static class PlayerSkillDatabase
{
    public const string VanguardComboMasteryId = "vanguard_combo_mastery";
    public const string VanguardRageId = "vanguard_rage";
    public const string VanguardPowerStrikeId = "vanguard_power_strike";
    public const string ShadeLuckySevenId = "shade_lucky_seven";
    public const string ShadeHasteId = "shade_haste";
    public const string ShadeNimbleBodyId = "shade_nimble_body";
    public const string ArcanistMagicClawId = "arcanist_magic_claw";
    public const string ArcanistMagicGuardId = "arcanist_magic_guard";
    public const string ArcanistMpBoostId = "arcanist_mp_boost";

    private const string ResourcePath = "GameData/PlayerSkillDatabase";

    private static Dictionary<string, PlayerSkillDefinition> definitions;
    private static PlayerSkillDatabaseAsset asset;

    public static bool TryGetDefinition(string skillId, out PlayerSkillDefinition definition)
    {
        EnsureLoaded();

        if (string.IsNullOrWhiteSpace(skillId))
        {
            definition = null;
            return false;
        }

        return definitions.TryGetValue(skillId.Trim(), out definition);
    }

    public static PlayerSkillDefinition GetDefinition(string skillId)
    {
        TryGetDefinition(skillId, out PlayerSkillDefinition definition);
        return definition;
    }

    public static IReadOnlyList<PlayerSkillDefinition> GetDefaultSkillsForJob(PlayerJobType jobType)
    {
        EnsureLoaded();

        List<PlayerSkillDefinition> defaultSkills = new List<PlayerSkillDefinition>();

        foreach (PlayerSkillDefinition definition in definitions.Values)
        {
            if (definition != null && definition.JobType == jobType)
                defaultSkills.Add(definition);
        }

        defaultSkills.Sort((left, right) => left.DefaultSlotIndex.CompareTo(right.DefaultSlotIndex));
        return defaultSkills;
    }

    public static IReadOnlyList<PlayerSkillDefinition> GetAllDefinitions()
    {
        EnsureLoaded();
        return new List<PlayerSkillDefinition>(definitions.Values);
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

        asset = Resources.Load<PlayerSkillDatabaseAsset>(ResourcePath);
        definitions = BuildLookup(asset != null ? asset.Definitions : null);

        if (asset == null)
        {
            Debug.LogError($"PlayerSkillDatabaseAsset was not found at Resources/{ResourcePath}.");
            return;
        }

        if (asset.Definitions == null || asset.Definitions.Count == 0)
            Debug.LogError("PlayerSkillDatabaseAsset is loaded but does not contain any skill definitions.");
    }

    private static Dictionary<string, PlayerSkillDefinition> BuildLookup(IReadOnlyList<PlayerSkillDefinition> skillDefinitions)
    {
        Dictionary<string, PlayerSkillDefinition> lookup = new Dictionary<string, PlayerSkillDefinition>();

        if (skillDefinitions == null)
            return lookup;

        for (int index = 0; index < skillDefinitions.Count; index++)
        {
            PlayerSkillDefinition definition = skillDefinitions[index];
            if (definition == null || string.IsNullOrWhiteSpace(definition.SkillId))
                continue;

            lookup[definition.SkillId] = definition;
        }

        return lookup;
    }
}


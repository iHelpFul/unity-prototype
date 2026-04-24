using System.Collections.Generic;
using UnityEngine;

public static class PlayerPassiveRuntimeUtility
{
    public static IReadOnlyList<PlayerPassiveEntry> GetUnlockedPassives(PlayerRuntimeData data)
    {
        EnsureCollections(data);

        return data != null && data.UnlockedPassives != null
            ? (IReadOnlyList<PlayerPassiveEntry>)data.UnlockedPassives
            : System.Array.Empty<PlayerPassiveEntry>();
    }

    public static void EnsureDefaultPassivesForJob(PlayerRuntimeData data, PlayerJobType jobType)
    {
        if (data == null)
            return;

        EnsureCollections(data);
        PlayerJobDefinition jobDefinition = PlayerJobCombatProfiles.GetJobDefinition(jobType);
        if (jobDefinition == null || jobDefinition.DefaultPassives == null)
            return;

        for (int index = 0; index < jobDefinition.DefaultPassives.Count; index++)
        {
            PassiveDefinition passiveDefinition = jobDefinition.DefaultPassives[index];
            if (passiveDefinition == null
                || string.IsNullOrWhiteSpace(passiveDefinition.PassiveId)
                || !passiveDefinition.SupportsJob(jobType))
            {
                continue;
            }

            UnlockOrSetPassiveLevel(data, passiveDefinition, 1, onlyUpgrade: true);
        }
    }

    public static int ResolvePassiveLevel(
        PlayerRuntimeData data,
        PassiveDefinition passiveDefinition,
        int fallbackLevel = 1)
    {
        if (passiveDefinition == null || string.IsNullOrWhiteSpace(passiveDefinition.PassiveId))
            return 0;

        PlayerPassiveEntry entry = FindPassiveEntry(data, passiveDefinition.PassiveId);
        if (entry != null)
            return Mathf.Clamp(entry.PassiveLevel, 1, ResolveMaxLevel(passiveDefinition));

        return Mathf.Clamp(fallbackLevel, 0, ResolveMaxLevel(passiveDefinition));
    }

    public static IReadOnlyList<PassiveDefinition> ResolveActivePassiveDefinitions(
        PlayerRuntimeData data,
        PlayerJobDefinition jobDefinition,
        PlayerJobType jobType)
    {
        EnsureCollections(data);

        List<PassiveDefinition> resolvedDefinitions = new List<PassiveDefinition>();
        HashSet<string> addedPassiveIds = new HashSet<string>();
        Dictionary<string, PassiveDefinition> defaultDefinitionsById = BuildDefaultPassiveLookup(jobDefinition, jobType);

        if (data?.UnlockedPassives != null)
        {
            for (int index = 0; index < data.UnlockedPassives.Count; index++)
            {
                PlayerPassiveEntry entry = data.UnlockedPassives[index];
                if (entry == null || string.IsNullOrWhiteSpace(entry.PassiveId))
                    continue;

                PassiveDefinition definition = ResolveDefinition(entry.PassiveId, defaultDefinitionsById);
                AddDefinitionIfValid(definition, jobType, resolvedDefinitions, addedPassiveIds);
            }
        }

        foreach (PassiveDefinition defaultDefinition in defaultDefinitionsById.Values)
        {
            AddDefinitionIfValid(defaultDefinition, jobType, resolvedDefinitions, addedPassiveIds);
        }

        return resolvedDefinitions;
    }

    public static bool TryUnlockOrSetPassiveLevel(
        PlayerRuntimeData data,
        PassiveDefinition passiveDefinition,
        int passiveLevel)
    {
        return UnlockOrSetPassiveLevel(data, passiveDefinition, passiveLevel, onlyUpgrade: false);
    }

    private static bool UnlockOrSetPassiveLevel(
        PlayerRuntimeData data,
        PassiveDefinition passiveDefinition,
        int passiveLevel,
        bool onlyUpgrade)
    {
        if (data == null || passiveDefinition == null || string.IsNullOrWhiteSpace(passiveDefinition.PassiveId))
            return false;

        EnsureCollections(data);
        int resolvedLevel = Mathf.Clamp(passiveLevel, 1, ResolveMaxLevel(passiveDefinition));
        PlayerPassiveEntry existingEntry = FindPassiveEntry(data, passiveDefinition.PassiveId);

        if (existingEntry != null)
        {
            existingEntry.PassiveLevel = onlyUpgrade
                ? Mathf.Max(existingEntry.PassiveLevel, resolvedLevel)
                : resolvedLevel;
            return true;
        }

        data.UnlockedPassives.Add(new PlayerPassiveEntry
        {
            PassiveId = passiveDefinition.PassiveId,
            PassiveLevel = resolvedLevel
        });

        return true;
    }

    private static PlayerPassiveEntry FindPassiveEntry(PlayerRuntimeData data, string passiveId)
    {
        if (data?.UnlockedPassives == null || string.IsNullOrWhiteSpace(passiveId))
            return null;

        string normalizedPassiveId = passiveId.Trim();
        for (int index = 0; index < data.UnlockedPassives.Count; index++)
        {
            PlayerPassiveEntry entry = data.UnlockedPassives[index];
            if (entry != null && entry.PassiveId == normalizedPassiveId)
                return entry;
        }

        return null;
    }

    private static int ResolveMaxLevel(PassiveDefinition passiveDefinition)
    {
        if (passiveDefinition == null || passiveDefinition.Levels == null || passiveDefinition.Levels.Count == 0)
            return 1;

        int maxLevel = 1;
        for (int index = 0; index < passiveDefinition.Levels.Count; index++)
        {
            PassiveLevelDefinition levelDefinition = passiveDefinition.Levels[index];
            if (levelDefinition != null)
                maxLevel = Mathf.Max(maxLevel, levelDefinition.Level);
        }

        return maxLevel;
    }

    public static void EnsureCollections(PlayerRuntimeData data)
    {
        if (data == null)
            return;

        data.UnlockedPassives ??= new List<PlayerPassiveEntry>();
        HashSet<string> uniquePassiveIds = new HashSet<string>();

        for (int index = data.UnlockedPassives.Count - 1; index >= 0; index--)
        {
            PlayerPassiveEntry entry = data.UnlockedPassives[index];
            if (entry == null || string.IsNullOrWhiteSpace(entry.PassiveId))
            {
                data.UnlockedPassives.RemoveAt(index);
                continue;
            }

            entry.PassiveId = entry.PassiveId.Trim();
            entry.PassiveLevel = Mathf.Max(1, entry.PassiveLevel);

            if (!uniquePassiveIds.Add(entry.PassiveId))
                data.UnlockedPassives.RemoveAt(index);
        }
    }

    private static Dictionary<string, PassiveDefinition> BuildDefaultPassiveLookup(
        PlayerJobDefinition jobDefinition,
        PlayerJobType jobType)
    {
        Dictionary<string, PassiveDefinition> definitionsById = new Dictionary<string, PassiveDefinition>();

        if (jobDefinition == null || jobDefinition.DefaultPassives == null)
            return definitionsById;

        for (int index = 0; index < jobDefinition.DefaultPassives.Count; index++)
        {
            PassiveDefinition definition = jobDefinition.DefaultPassives[index];
            if (definition == null
                || string.IsNullOrWhiteSpace(definition.PassiveId)
                || !definition.SupportsJob(jobType))
            {
                continue;
            }

            string passiveId = definition.PassiveId.Trim();
            if (!definitionsById.ContainsKey(passiveId))
                definitionsById.Add(passiveId, definition);
        }

        return definitionsById;
    }

    private static PassiveDefinition ResolveDefinition(
        string passiveId,
        Dictionary<string, PassiveDefinition> defaultDefinitionsById)
    {
        if (string.IsNullOrWhiteSpace(passiveId))
            return null;

        string normalizedPassiveId = passiveId.Trim();
        if (defaultDefinitionsById != null
            && defaultDefinitionsById.TryGetValue(normalizedPassiveId, out PassiveDefinition defaultDefinition)
            && defaultDefinition != null)
        {
            return defaultDefinition;
        }

        return PlayerPassiveDefinitionRegistry.GetDefinition(normalizedPassiveId);
    }

    private static void AddDefinitionIfValid(
        PassiveDefinition definition,
        PlayerJobType jobType,
        List<PassiveDefinition> resolvedDefinitions,
        HashSet<string> addedPassiveIds)
    {
        if (definition == null
            || string.IsNullOrWhiteSpace(definition.PassiveId)
            || !definition.SupportsJob(jobType))
        {
            return;
        }

        string passiveId = definition.PassiveId.Trim();
        if (!addedPassiveIds.Add(passiveId))
            return;

        resolvedDefinitions.Add(definition);
    }
}

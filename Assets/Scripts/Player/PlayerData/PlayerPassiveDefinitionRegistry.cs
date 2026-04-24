using System.Collections.Generic;
using UnityEngine;

public static class PlayerPassiveDefinitionRegistry
{
    private const string ResourcePath = "GameData";

    private static Dictionary<string, PassiveDefinition> definitionsById;

    public static bool TryGetDefinition(string passiveId, out PassiveDefinition definition)
    {
        definition = null;

        if (string.IsNullOrWhiteSpace(passiveId))
            return false;

        EnsureLoaded();
        return definitionsById.TryGetValue(passiveId.Trim(), out definition) && definition != null;
    }

    public static PassiveDefinition GetDefinition(string passiveId)
    {
        return TryGetDefinition(passiveId, out PassiveDefinition definition)
            ? definition
            : null;
    }

    public static void ResetCache()
    {
        definitionsById = null;
    }

    private static void EnsureLoaded()
    {
        if (definitionsById != null)
            return;

        definitionsById = new Dictionary<string, PassiveDefinition>();
        PassiveDefinition[] definitions = Resources.LoadAll<PassiveDefinition>(ResourcePath);

        for (int index = 0; index < definitions.Length; index++)
        {
            PassiveDefinition definition = definitions[index];
            if (definition == null || string.IsNullOrWhiteSpace(definition.PassiveId))
                continue;

            string passiveId = definition.PassiveId.Trim();
            if (definitionsById.ContainsKey(passiveId))
            {
                Debug.LogWarning($"Duplicate PassiveDefinition id '{passiveId}' found in Resources/{ResourcePath}. Keeping the first definition.");
                continue;
            }

            definitionsById.Add(passiveId, definition);
        }
    }
}

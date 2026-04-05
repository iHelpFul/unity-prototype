using System.Collections.Generic;
using UnityEngine;

public static class EnemyDefinitionDatabase
{
    private const string ResourcePath = "GameData/EnemyDefinitionDatabase";

    private static Dictionary<EnemyType, EnemyDefinition> definitions;
    private static EnemyDefinitionDatabaseAsset asset;
    private static Dictionary<EnemyType, EnemyDefinition> fallbackDefinitions;

    public static EnemyDefinition GetDefinition(EnemyType enemyType)
    {
        EnsureLoaded();

        if (definitions != null && definitions.TryGetValue(enemyType, out EnemyDefinition definition))
            return definition;

        fallbackDefinitions ??= CreateFallbackDefinitions();
        return fallbackDefinitions.TryGetValue(enemyType, out EnemyDefinition fallbackDefinition)
            ? fallbackDefinition
            : null;
    }

    public static void ResetCache()
    {
        definitions = null;
        asset = null;
        fallbackDefinitions = null;
    }

    private static void EnsureLoaded()
    {
        if (definitions != null)
            return;

        asset = Resources.Load<EnemyDefinitionDatabaseAsset>(ResourcePath);
        definitions = new Dictionary<EnemyType, EnemyDefinition>();

        if (asset?.Definitions == null)
            return;

        for (int index = 0; index < asset.Definitions.Count; index++)
        {
            EnemyDefinition definition = asset.Definitions[index];
            if (definition == null)
                continue;

            definitions[definition.EnemyType] = definition;
        }
    }

    private static Dictionary<EnemyType, EnemyDefinition> CreateFallbackDefinitions()
    {
        return new Dictionary<EnemyType, EnemyDefinition>
        {
            [EnemyType.RedSlime] = EnemyDefinition.CreateTransient(
                EnemyType.RedSlime,
                30,
                1,
                8,
                4,
                8,
                4,
                10,
                0.8f,
                1,
                0.25f,
                1,
                0.08f,
                ItemDatabase.GetDefinition(ItemDatabase.SlimeGelId),
                1,
                0.55f,
                1.25f,
                5.5f),
            [EnemyType.BlueTurtle] = EnemyDefinition.CreateTransient(
                EnemyType.BlueTurtle,
                30,
                1,
                14,
                7,
                10,
                9,
                18,
                0.9f,
                1,
                0.18f,
                1,
                0.16f,
                ItemDatabase.GetDefinition(ItemDatabase.TurtleShellId),
                1,
                0.45f,
                1.25f,
                8f),
            [EnemyType.GreenMushroom] = EnemyDefinition.CreateTransient(
                EnemyType.GreenMushroom,
                30,
                1,
                11,
                5,
                9,
                6,
                14,
                0.85f,
                1,
                0.22f,
                1,
                0.2f,
                null,
                1,
                0.4f,
                1.25f,
                6.5f)
        };
    }
}

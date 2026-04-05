using System.Collections.Generic;
using UnityEngine;

public static class CharacterAppearanceCatalogDatabase
{
    private const string ResourcePath = "GameData/Characters/AppearanceCatalog";

    private static CharacterAppearanceCatalogAsset catalog;
    private static CharacterAppearanceCatalogAsset fallbackCatalog;

    public static CharacterAppearanceCatalogAsset GetCatalog()
    {
        EnsureLoaded();
        return catalog != null ? catalog : fallbackCatalog;
    }

    public static void ResetCache()
    {
        catalog = null;
        fallbackCatalog = null;
    }

    private static void EnsureLoaded()
    {
        if (catalog != null || fallbackCatalog != null)
            return;

        catalog = Resources.Load<CharacterAppearanceCatalogAsset>(ResourcePath);
        if (catalog == null)
            fallbackCatalog = CreateFallbackCatalog();
    }

    private static CharacterAppearanceCatalogAsset CreateFallbackCatalog()
    {
        CharacterAppearanceCatalogAsset transientCatalog = ScriptableObject.CreateInstance<CharacterAppearanceCatalogAsset>();
        transientCatalog.hideFlags = HideFlags.HideAndDontSave;
        transientCatalog.Initialize(
            new[]
            {
                new CharacterAppearanceOption { Id = "Head01_Male", DisplayName = "Head 01 Male" },
                new CharacterAppearanceOption { Id = "Head02_Female", DisplayName = "Head 02 Female" },
                new CharacterAppearanceOption { Id = "Head03_Blue", DisplayName = "Head 03 Blue" }
            },
            new[]
            {
                new CharacterAppearanceOption { Id = "Hair01", DisplayName = "Hair 01" },
                new CharacterAppearanceOption { Id = "Hair02", DisplayName = "Hair 02" },
                new CharacterAppearanceOption { Id = "Hair03", DisplayName = "Hair 03" }
            },
            new[]
            {
                new CharacterAppearanceOption { Id = "Eye01", DisplayName = "Eye 01" },
                new CharacterAppearanceOption { Id = "Eye02", DisplayName = "Eye 02" },
                new CharacterAppearanceOption { Id = "Eye03", DisplayName = "Eye 03" }
            },
            new[]
            {
                new CharacterAppearanceOption { Id = "Mouth01", DisplayName = "Mouth 01" },
                new CharacterAppearanceOption { Id = "Mouth02", DisplayName = "Mouth 02" },
                new CharacterAppearanceOption { Id = "Mouth03", DisplayName = "Mouth 03" }
            },
            new[]
            {
                new CharacterAppearanceOption { Id = "Body01", DisplayName = "Body 01" },
                new CharacterAppearanceOption { Id = "Body02", DisplayName = "Body 02" },
                new CharacterAppearanceOption { Id = "Body03", DisplayName = "Body 03" }
            },
            new[]
            {
                new CharacterAppearanceOption { Id = "None", DisplayName = "None" },
                new CharacterAppearanceOption { Id = "OHS01_Stick", DisplayName = "Stick" },
                new CharacterAppearanceOption { Id = "OHS03_Sword", DisplayName = "Sword 03" },
                new CharacterAppearanceOption { Id = "Wand01", DisplayName = "Wand 01" }
            },
            new[]
            {
                new CharacterAppearanceOption { Id = "None", DisplayName = "None" },
                new CharacterAppearanceOption { Id = "Hat01", DisplayName = "Hat 01" },
                new CharacterAppearanceOption { Id = "Hat02", DisplayName = "Hat 02" }
            },
            new[]
            {
                new CharacterAppearanceOption { Id = "None", DisplayName = "None" }
            },
            new[]
            {
                new CharacterAppearanceOption { Id = "None", DisplayName = "None" },
                new CharacterAppearanceOption { Id = "AC05_Horn01", DisplayName = "Horn 01" },
                new CharacterAppearanceOption { Id = "AC05_Horn02", DisplayName = "Horn 02" }
            },
            new[]
            {
                new CharacterAppearanceOption { Id = "None", DisplayName = "None" }
            },
            new[]
            {
                new CharacterAppearanceOption { Id = "None", DisplayName = "None" },
                new CharacterAppearanceOption { Id = "AC08_NinjaMask01", DisplayName = "Ninja Mask 01" },
                new CharacterAppearanceOption { Id = "AC08_NinjaMask02", DisplayName = "Ninja Mask 02" }
            },
            new[]
            {
                new CharacterAppearanceOption { Id = "None", DisplayName = "None" },
                new CharacterAppearanceOption { Id = "AC10_Mustache01", DisplayName = "Mustache 01" },
                new CharacterAppearanceOption { Id = "AC10_Mustache02", DisplayName = "Mustache 02" }
            },
            new List<Color>
            {
                Color.white,
                new Color(0.92f, 0.80f, 0.67f),
                new Color(0.72f, 0.56f, 0.42f),
                new Color(0.38f, 0.24f, 0.16f),
                new Color(0.95f, 0.75f, 0.20f),
                new Color(0.92f, 0.38f, 0.34f),
                new Color(0.92f, 0.52f, 0.82f),
                new Color(0.57f, 0.42f, 0.89f),
                new Color(0.22f, 0.56f, 0.96f),
                new Color(0.18f, 0.82f, 0.86f),
                new Color(0.44f, 0.74f, 0.38f),
                new Color(0.12f, 0.12f, 0.14f)
            });
        return transientCatalog;
    }
}

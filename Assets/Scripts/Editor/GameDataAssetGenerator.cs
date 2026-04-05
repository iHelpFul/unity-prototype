using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GameDataAssetGenerator
{
    private const string ResourcesRoot = "Assets/Resources";
    private const string GameDataRoot = "Assets/Resources/GameData";

    [MenuItem("Tools/Game Data/Generate Default Gameplay Data")]
    public static void GenerateDefaultGameplayData()
    {
        EnsureFolderPath(ResourcesRoot);
        EnsureFolderPath(GameDataRoot);
        EnsureFolderPath($"{GameDataRoot}/Items");
        EnsureFolderPath($"{GameDataRoot}/Items/Equipment");
        EnsureFolderPath($"{GameDataRoot}/Items/Equipment/Overalls");
        EnsureFolderPath($"{GameDataRoot}/Items/Equipment/Weapons");
        EnsureFolderPath($"{GameDataRoot}/Skills");
        EnsureFolderPath($"{GameDataRoot}/Skills/Warrior");
        EnsureFolderPath($"{GameDataRoot}/Skills/Thief");
        EnsureFolderPath($"{GameDataRoot}/Skills/Mage");
        EnsureFolderPath($"{GameDataRoot}/Jobs");
        EnsureFolderPath($"{GameDataRoot}/Jobs/Profiles");
        EnsureFolderPath($"{GameDataRoot}/Jobs/Definitions");
        EnsureFolderPath($"{GameDataRoot}/Enemies");
        EnsureFolderPath($"{GameDataRoot}/Vendors");
        EnsureFolderPath($"{GameDataRoot}/Characters");

        IReadOnlyList<CharacterAppearanceOption> overallAppearanceOptions = new[]
        {
            new CharacterAppearanceOption { Id = "Body01", DisplayName = "Body 01" },
            new CharacterAppearanceOption { Id = "Body02", DisplayName = "Body 02" },
            new CharacterAppearanceOption { Id = "Body03", DisplayName = "Body 03" }
        };

        IReadOnlyList<CharacterAppearanceOption> weaponAppearanceOptions = BuildAppearanceOptions(
            "Assets/RPGTinyHeroWavePBR/Prefab/Weapons",
            "OHS",
            true,
            12,
            extraPrefixes: new[] { "Wand" });

        ItemDefinition redPotion = EnsureItem(
            $"{GameDataRoot}/Items/RedPotion.asset",
            ItemDatabase.RedPotionId,
            "Red Potion",
            ItemCategory.Consumable,
            100,
            50,
            25,
            50,
            0,
            new Color(0.94f, 0.32f, 0.36f),
            new Color(1f, 0.77f, 0.8f));

        ItemDefinition bluePotion = EnsureItem(
            $"{GameDataRoot}/Items/BluePotion.asset",
            ItemDatabase.BluePotionId,
            "Blue Potion",
            ItemCategory.Consumable,
            100,
            60,
            30,
            0,
            30,
            new Color(0.22f, 0.56f, 0.96f),
            new Color(0.72f, 0.9f, 1f));

        ItemDefinition slimeGel = EnsureItem(
            $"{GameDataRoot}/Items/SlimeGel.asset",
            ItemDatabase.SlimeGelId,
            "Slime Gel",
            ItemCategory.Etc,
            200,
            0,
            6,
            0,
            0,
            new Color(0.49f, 0.83f, 0.39f),
            new Color(0.84f, 1f, 0.74f));

        ItemDefinition turtleShell = EnsureItem(
            $"{GameDataRoot}/Items/TurtleShell.asset",
            ItemDatabase.TurtleShellId,
            "Turtle Shell",
            ItemCategory.Etc,
            200,
            0,
            10,
            0,
            0,
            new Color(0.46f, 0.57f, 0.7f),
            new Color(0.84f, 0.91f, 1f));

        List<ItemDefinition> generatedItems = new List<ItemDefinition> { redPotion, bluePotion, slimeGel, turtleShell };

        for (int index = 0; index < overallAppearanceOptions.Count; index++)
        {
            CharacterAppearanceOption option = overallAppearanceOptions[index];
            if (option == null || string.IsNullOrWhiteSpace(option.Id))
                continue;

            generatedItems.Add(EnsureEquipmentItem(
                $"{GameDataRoot}/Items/Equipment/Overalls/{option.Id}.asset",
                $"eq_overall_{ToItemIdSegment(option.Id)}",
                $"{option.DisplayName} Overall",
                EquipmentSlotType.Overall,
                0,
                0,
                new ItemStatModifierData(),
                new ItemEquipmentAppearanceData
                {
                    OutfitId = option.Id
                },
                Color.white,
                new Color(0.9f, 0.9f, 0.9f)));
        }

        for (int index = 0; index < weaponAppearanceOptions.Count; index++)
        {
            CharacterAppearanceOption option = weaponAppearanceOptions[index];
            if (option == null || string.IsNullOrWhiteSpace(option.Id) || option.Id == "None")
                continue;

            generatedItems.Add(EnsureEquipmentItem(
                $"{GameDataRoot}/Items/Equipment/Weapons/{option.Id}.asset",
                $"eq_weapon_right_{ToItemIdSegment(option.Id)}",
                option.DisplayName,
                EquipmentSlotType.WeaponRight,
                0,
                0,
                new ItemStatModifierData(),
                new ItemEquipmentAppearanceData
                {
                    WeaponRightId = option.Id,
                    ApplyWeaponRightColor = true,
                    WeaponRightColor = Color.white
                },
                Color.white,
                new Color(0.9f, 0.9f, 0.9f)));
        }

        ItemDatabaseAsset itemDatabase = EnsureAsset<ItemDatabaseAsset>($"{GameDataRoot}/ItemDatabase.asset");
        itemDatabase.SetItems(generatedItems);
        EditorUtility.SetDirty(itemDatabase);

        PlayerSkillDefinition warriorComboMastery = EnsureSkill(
            $"{GameDataRoot}/Skills/Warrior/ComboMastery.asset",
            PlayerSkillDatabase.WarriorComboMasteryId,
            "Combo Mastery",
            PlayerJobType.Warrior,
            PlayerSkillType.Passive,
            20,
            -1);

        PlayerSkillDefinition warriorRage = EnsureSkill(
            $"{GameDataRoot}/Skills/Warrior/Rage.asset",
            PlayerSkillDatabase.WarriorRageId,
            "Rage",
            PlayerJobType.Warrior,
            PlayerSkillType.ActiveBuff,
            20,
            2);

        PlayerSkillDefinition warriorPowerStrike = EnsureSkill(
            $"{GameDataRoot}/Skills/Warrior/PowerStrike.asset",
            PlayerSkillDatabase.WarriorPowerStrikeId,
            "Power Strike",
            PlayerJobType.Warrior,
            PlayerSkillType.ActiveAttack,
            20,
            1,
            8,
            0f,
            1.9f,
            1.85f,
            1,
            0f,
            4,
            "Combo04_InPlace_SingleSword",
            1.08f,
            0.92f,
            PlayerSkillTargetingMode.MeleeArea,
            3);

        PlayerSkillDefinition thiefLuckySeven = EnsureSkill(
            $"{GameDataRoot}/Skills/Thief/LuckySeven.asset",
            PlayerSkillDatabase.ThiefLuckySevenId,
            "Lucky Seven",
            PlayerJobType.Thief,
            PlayerSkillType.ActiveAttack,
            20,
            1,
            6,
            0f,
            6.25f,
            1.02f,
            1,
            0.08f,
            2,
            "Combo02_InPlace_SingleSword 0",
            0.96f,
            0.82f,
            PlayerSkillTargetingMode.ForwardProjectile,
            1,
            2,
            15f,
            0.18f,
            0.65f,
            7f,
            0.95f,
            1.05f,
            0.16f);

        PlayerSkillDefinition thiefHaste = EnsureSkill(
            $"{GameDataRoot}/Skills/Thief/Haste.asset",
            PlayerSkillDatabase.ThiefHasteId,
            "Haste",
            PlayerJobType.Thief,
            PlayerSkillType.ActiveBuff,
            20,
            2);

        PlayerSkillDefinition thiefNimbleBody = EnsureSkill(
            $"{GameDataRoot}/Skills/Thief/NimbleBody.asset",
            PlayerSkillDatabase.ThiefNimbleBodyId,
            "Nimble Body",
            PlayerJobType.Thief,
            PlayerSkillType.Passive,
            20,
            -1);

        PlayerSkillDefinition mageMagicClaw = EnsureSkill(
            $"{GameDataRoot}/Skills/Mage/MagicClaw.asset",
            PlayerSkillDatabase.MageMagicClawId,
            "Magic Claw",
            PlayerJobType.Mage,
            PlayerSkillType.ActiveAttack,
            20,
            1,
            10,
            0f,
            6.5f,
            0.92f,
            2,
            0.14f,
            1,
            "Combo03_InPlace_SingleSword 0",
            0.92f,
            0.9f,
            PlayerSkillTargetingMode.FrontSingleTarget,
            1);

        PlayerSkillDefinition mageMagicGuard = EnsureSkill(
            $"{GameDataRoot}/Skills/Mage/MagicGuard.asset",
            PlayerSkillDatabase.MageMagicGuardId,
            "Magic Guard",
            PlayerJobType.Mage,
            PlayerSkillType.ActiveBuff,
            20,
            2);

        PlayerSkillDefinition mageMpBoost = EnsureSkill(
            $"{GameDataRoot}/Skills/Mage/MpBoost.asset",
            PlayerSkillDatabase.MageMpBoostId,
            "MP Boost",
            PlayerJobType.Mage,
            PlayerSkillType.Passive,
            20,
            -1);

        PlayerSkillDatabaseAsset skillDatabase = EnsureAsset<PlayerSkillDatabaseAsset>($"{GameDataRoot}/PlayerSkillDatabase.asset");
        skillDatabase.SetDefinitions(new[]
        {
            warriorComboMastery,
            warriorRage,
            warriorPowerStrike,
            thiefLuckySeven,
            thiefHaste,
            thiefNimbleBody,
            mageMagicClaw,
            mageMagicGuard,
            mageMpBoost
        });
        EditorUtility.SetDirty(skillDatabase);

        PlayerBasicAttackProfile noviceProfile = EnsureBasicAttackProfile(
            $"{GameDataRoot}/Jobs/Profiles/NoviceBasicAttack.asset",
            PlayerJobType.Novice,
            2,
            2,
            PlayerBasicAttackSelectionMode.Random,
            0.72f,
            1.1f,
            0.8f,
            1,
            1f,
            0,
            0f,
            0f,
            false);

        PlayerBasicAttackProfile warriorProfile = EnsureBasicAttackProfile(
            $"{GameDataRoot}/Jobs/Profiles/WarriorBasicAttack.asset",
            PlayerJobType.Warrior,
            5,
            5,
            PlayerBasicAttackSelectionMode.Sequential,
            0.4f,
            1.2f,
            1f,
            1,
            1.18f,
            10,
            4f,
            0.05f,
            true);

        PlayerBasicAttackProfile thiefProfile = EnsureBasicAttackProfile(
            $"{GameDataRoot}/Jobs/Profiles/ThiefBasicAttack.asset",
            PlayerJobType.Thief,
            2,
            2,
            PlayerBasicAttackSelectionMode.Random,
            0.6f,
            1f,
            0.86f,
            1,
            1f,
            0,
            0f,
            0f,
            false);

        PlayerBasicAttackProfile mageProfile = EnsureBasicAttackProfile(
            $"{GameDataRoot}/Jobs/Profiles/MageBasicAttack.asset",
            PlayerJobType.Mage,
            2,
            2,
            PlayerBasicAttackSelectionMode.Random,
            0.76f,
            1.12f,
            0.78f,
            1,
            1f,
            0,
            0f,
            0f,
            false);

        PlayerJobDefinition noviceJob = EnsureJobDefinition(
            $"{GameDataRoot}/Jobs/Definitions/Novice.asset",
            PlayerJobType.Novice,
            "Novice",
            10,
            noviceProfile,
            new PlayerSkillDefinition[0]);

        PlayerJobDefinition warriorJob = EnsureJobDefinition(
            $"{GameDataRoot}/Jobs/Definitions/Warrior.asset",
            PlayerJobType.Warrior,
            "Warrior",
            10,
            warriorProfile,
            new[] { warriorPowerStrike, warriorRage, warriorComboMastery });

        PlayerJobDefinition thiefJob = EnsureJobDefinition(
            $"{GameDataRoot}/Jobs/Definitions/Thief.asset",
            PlayerJobType.Thief,
            "Thief",
            10,
            thiefProfile,
            new[] { thiefLuckySeven, thiefHaste, thiefNimbleBody });

        PlayerJobDefinition mageJob = EnsureJobDefinition(
            $"{GameDataRoot}/Jobs/Definitions/Mage.asset",
            PlayerJobType.Mage,
            "Mage",
            10,
            mageProfile,
            new[] { mageMagicClaw, mageMagicGuard, mageMpBoost });

        PlayerJobDatabaseAsset jobDatabase = EnsureAsset<PlayerJobDatabaseAsset>($"{GameDataRoot}/PlayerJobDatabase.asset");
        jobDatabase.SetDefinitions(new[] { noviceJob, warriorJob, thiefJob, mageJob });
        EditorUtility.SetDirty(jobDatabase);

        EnemyDefinition redSlime = EnsureEnemyDefinition(
            $"{GameDataRoot}/Enemies/RedSlime.asset",
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
            slimeGel,
            1,
            0.55f,
            1.25f,
            5.5f);

        EnemyDefinition blueTurtle = EnsureEnemyDefinition(
            $"{GameDataRoot}/Enemies/BlueTurtle.asset",
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
            turtleShell,
            1,
            0.45f,
            1.25f,
            8f);

        EnemyDefinition greenMushroom = EnsureEnemyDefinition(
            $"{GameDataRoot}/Enemies/GreenMushroom.asset",
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
            6.5f);

        EnemyDefinitionDatabaseAsset enemyDatabase = EnsureAsset<EnemyDefinitionDatabaseAsset>($"{GameDataRoot}/EnemyDefinitionDatabase.asset");
        enemyDatabase.SetDefinitions(new[] { redSlime, blueTurtle, greenMushroom });
        EditorUtility.SetDirty(enemyDatabase);

        NpcVendorDefinition potionShop = EnsureVendorDefinition(
            $"{GameDataRoot}/Vendors/PotionShop.asset",
            true,
            new[] { redPotion, bluePotion });

        CharacterAppearanceCatalogAsset appearanceCatalog = EnsureAppearanceCatalog(
            $"{GameDataRoot}/Characters/AppearanceCatalog.asset",
            BuildAppearanceOptions("Assets/RPGTinyHeroWavePBR/Prefab/HeadParts", "Head", false),
            BuildAppearanceOptions("Assets/RPGTinyHeroWavePBR/Prefab/HeadParts", "Hair", false),
            BuildAppearanceOptions("Assets/RPGTinyHeroWavePBR/Prefab/HeadParts", "Eye", false),
            BuildAppearanceOptions("Assets/RPGTinyHeroWavePBR/Prefab/HeadParts", "Mouth", false),
            overallAppearanceOptions,
            weaponAppearanceOptions,
            BuildAppearanceOptions("Assets/RPGTinyHeroWavePBR/Prefab/HeadParts", "Hat", true),
            new[]
            {
                new CharacterAppearanceOption { Id = "None", DisplayName = "None" }
            },
            BuildAppearanceOptions("Assets/RPGTinyHeroWavePBR/Prefab/HeadParts", "AC05_Horn", true),
            new[]
            {
                new CharacterAppearanceOption { Id = "None", DisplayName = "None" }
            },
            BuildAppearanceOptions("Assets/RPGTinyHeroWavePBR/Prefab/HeadParts", "AC08_NinjaMask", true),
            BuildAppearanceOptions("Assets/RPGTinyHeroWavePBR/Prefab/HeadParts", "AC10_Mustache", true, 6, extraPrefixes: new[] { "AC11_Mustache" }),
            new[]
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

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ItemDatabase.ResetCache();
        PlayerSkillDatabase.ResetCache();
        PlayerJobCombatProfiles.ResetCache();
        EnemyDefinitionDatabase.ResetCache();
        CharacterAppearanceCatalogDatabase.ResetCache();

        Debug.Log("Default gameplay ScriptableObject data generated under Assets/Resources/GameData.");
    }

    private static ItemDefinition EnsureItem(
        string assetPath,
        string itemId,
        string displayName,
        ItemCategory category,
        int maxStack,
        int buyPrice,
        int sellPrice,
        int restoreHP,
        int restoreMP,
        Color primaryColor,
        Color accentColor)
    {
        ItemDefinition definition = EnsureAsset<ItemDefinition>(assetPath);
        definition.Initialize(
            itemId,
            displayName,
            category,
            maxStack,
            buyPrice,
            sellPrice,
            restoreHP,
            restoreMP,
            primaryColor,
            accentColor);
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static ItemDefinition EnsureEquipmentItem(
        string assetPath,
        string itemId,
        string displayName,
        EquipmentSlotType slot,
        int buyPrice,
        int sellPrice,
        ItemStatModifierData statBonuses,
        ItemEquipmentAppearanceData equipmentAppearance,
        Color primaryColor,
        Color accentColor)
    {
        ItemDefinition definition = EnsureAsset<ItemDefinition>(assetPath);
        definition.Initialize(
            itemId,
            displayName,
            ItemCategory.Equipment,
            1,
            buyPrice,
            sellPrice,
            0,
            0,
            primaryColor,
            accentColor);
        definition.ConfigureEquipment(slot, statBonuses, equipmentAppearance);
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static PlayerSkillDefinition EnsureSkill(
        string assetPath,
        string skillId,
        string displayName,
        PlayerJobType jobType,
        PlayerSkillType skillType,
        int maxLevel,
        int defaultSlotIndex,
        int manaCost = 0,
        float cooldown = 0f,
        float range = 0f,
        float damageMultiplier = 1f,
        int hitCount = 1,
        float hitInterval = 0f,
        int animationVariantIndex = 1,
        string animatorStateName = "",
        float animationSpeed = 1f,
        float attackDuration = 0.9f,
        PlayerSkillTargetingMode targetingMode = PlayerSkillTargetingMode.FrontSingleTarget,
        int maxTargets = 1,
        int projectileCount = 1,
        float projectileSpeed = 0f,
        float projectileRadius = 0.2f,
        float projectileLifetime = 0.5f,
        float projectileSpreadAngle = 0f,
        float projectileSpawnForwardOffset = 0.8f,
        float projectileSpawnUpOffset = 1f,
        float projectileVisualScale = 0.2f)
    {
        PlayerSkillDefinition definition = EnsureAsset<PlayerSkillDefinition>(assetPath);
        definition.Initialize(
            skillId,
            displayName,
            jobType,
            skillType,
            maxLevel,
            defaultSlotIndex,
            manaCost,
            cooldown,
            range,
            damageMultiplier,
            hitCount,
            hitInterval,
            animationVariantIndex,
            animatorStateName,
            animationSpeed,
            attackDuration,
            targetingMode,
            maxTargets,
            projectileCount,
            projectileSpeed,
            projectileRadius,
            projectileLifetime,
            projectileSpreadAngle,
            projectileSpawnForwardOffset,
            projectileSpawnUpOffset,
            projectileVisualScale);
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static PlayerBasicAttackProfile EnsureBasicAttackProfile(
        string assetPath,
        PlayerJobType jobType,
        int animationVariantCount,
        int maxChainCount,
        PlayerBasicAttackSelectionMode selectionMode,
        float attackCooldown,
        float maxAttackDuration,
        float attackAnimationSpeed,
        int maxTargets,
        float basicDamageMultiplier,
        int maxComboCounter,
        float comboResetDelay,
        float comboDamageBonusPerStack,
        bool supportsComboCounter)
    {
        PlayerBasicAttackProfile profile = EnsureAsset<PlayerBasicAttackProfile>(assetPath);
        profile.Initialize(
            jobType,
            animationVariantCount,
            maxChainCount,
            selectionMode,
            attackCooldown,
            maxAttackDuration,
            attackAnimationSpeed,
            maxTargets,
            basicDamageMultiplier,
            maxComboCounter,
            comboResetDelay,
            comboDamageBonusPerStack,
            supportsComboCounter);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static PlayerJobDefinition EnsureJobDefinition(
        string assetPath,
        PlayerJobType jobType,
        string displayName,
        int advancementLevelRequirement,
        PlayerBasicAttackProfile basicAttackProfile,
        IReadOnlyList<PlayerSkillDefinition> defaultSkills)
    {
        PlayerJobDefinition definition = EnsureAsset<PlayerJobDefinition>(assetPath);
        definition.Initialize(
            jobType,
            displayName,
            advancementLevelRequirement,
            basicAttackProfile,
            defaultSkills);
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static EnemyDefinition EnsureEnemyDefinition(
        string assetPath,
        EnemyType enemyType,
        int maxHP,
        int defense,
        int expReward,
        int contactDamage,
        int animatedAttackDamage,
        int minMesoDrop,
        int maxMesoDrop,
        float mesoDropChance,
        int redPotionDropCount,
        float redPotionDropChance,
        int bluePotionDropCount,
        float bluePotionDropChance,
        ItemDefinition commonEtcItem,
        int commonEtcDropCount,
        float commonEtcDropChance,
        float corpseDuration,
        float respawnDelay)
    {
        EnemyDefinition definition = EnsureAsset<EnemyDefinition>(assetPath);
        definition.Initialize(
            enemyType,
            maxHP,
            defense,
            expReward,
            contactDamage,
            animatedAttackDamage,
            minMesoDrop,
            maxMesoDrop,
            mesoDropChance,
            redPotionDropCount,
            redPotionDropChance,
            bluePotionDropCount,
            bluePotionDropChance,
            commonEtcItem,
            commonEtcDropCount,
            commonEtcDropChance,
            corpseDuration,
            respawnDelay);
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static NpcVendorDefinition EnsureVendorDefinition(
        string assetPath,
        bool buysPlayerItems,
        IReadOnlyList<ItemDefinition> stockedItems)
    {
        NpcVendorDefinition definition = EnsureAsset<NpcVendorDefinition>(assetPath);
        definition.Initialize(buysPlayerItems, stockedItems);
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static CharacterAppearanceCatalogAsset EnsureAppearanceCatalog(
        string assetPath,
        IReadOnlyList<CharacterAppearanceOption> headOptions,
        IReadOnlyList<CharacterAppearanceOption> hairOptions,
        IReadOnlyList<CharacterAppearanceOption> eyeOptions,
        IReadOnlyList<CharacterAppearanceOption> mouthOptions,
        IReadOnlyList<CharacterAppearanceOption> outfitOptions,
        IReadOnlyList<CharacterAppearanceOption> weaponRightOptions,
        IReadOnlyList<CharacterAppearanceOption> hatOptions,
        IReadOnlyList<CharacterAppearanceOption> capeOptions,
        IReadOnlyList<CharacterAppearanceOption> hornsOptions,
        IReadOnlyList<CharacterAppearanceOption> accessoryOptions,
        IReadOnlyList<CharacterAppearanceOption> ninjaMaskOptions,
        IReadOnlyList<CharacterAppearanceOption> mustacheOptions,
        IReadOnlyList<Color> paletteOptions)
    {
        CharacterAppearanceCatalogAsset catalog = EnsureAsset<CharacterAppearanceCatalogAsset>(assetPath);
        catalog.Initialize(
            headOptions,
            hairOptions,
            eyeOptions,
            mouthOptions,
            outfitOptions,
            weaponRightOptions,
            hatOptions,
            capeOptions,
            hornsOptions,
            accessoryOptions,
            ninjaMaskOptions,
            mustacheOptions,
            paletteOptions);
        EditorUtility.SetDirty(catalog);
        return catalog;
    }

    private static IReadOnlyList<CharacterAppearanceOption> BuildAppearanceOptions(
        string folderPath,
        string prefix,
        bool includeNone,
        int maxCount = 0,
        IReadOnlyList<string> extraPrefixes = null)
    {
        List<CharacterAppearanceOption> options = new List<CharacterAppearanceOption>();
        if (includeNone)
        {
            options.Add(new CharacterAppearanceOption
            {
                Id = "None",
                DisplayName = "None"
            });
        }

        HashSet<string> prefixes = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(prefix))
            prefixes.Add(prefix.Trim());

        if (extraPrefixes != null)
        {
            for (int index = 0; index < extraPrefixes.Count; index++)
            {
                string extraPrefix = extraPrefixes[index];
                if (!string.IsNullOrWhiteSpace(extraPrefix))
                    prefixes.Add(extraPrefix.Trim());
            }
        }

        HashSet<string> uniqueIds = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        List<string> prefabNames = new List<string>(guids.Length);
        for (int index = 0; index < guids.Length; index++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[index]);
            string name = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrWhiteSpace(name))
                continue;

            foreach (string optionPrefix in prefixes)
            {
                if (name.StartsWith(optionPrefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    prefabNames.Add(name);
                    break;
                }
            }
        }

        prefabNames.Sort(System.StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < prefabNames.Count; index++)
        {
            if (maxCount > 0 && options.Count >= maxCount + (includeNone ? 1 : 0))
                break;

            string id = prefabNames[index];
            if (!uniqueIds.Add(id))
                continue;

            options.Add(new CharacterAppearanceOption
            {
                Id = id,
                DisplayName = BeautifyAppearanceName(id)
            });
        }

        return options;
    }

    private static string BeautifyAppearanceName(string rawId)
    {
        if (string.IsNullOrWhiteSpace(rawId))
            return string.Empty;

        string value = rawId.Replace('_', ' ').Trim();
        return value;
    }

    private static string ToItemIdSegment(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return string.Empty;

        string normalized = rawValue.Trim().ToLowerInvariant();
        normalized = normalized.Replace(" ", "_").Replace("-", "_");
        return normalized;
    }

    private static T EnsureAsset<T>(string assetPath) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset != null)
            return asset;

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, assetPath);
        return asset;
    }

    private static void EnsureFolderPath(string assetPath)
    {
        string[] segments = assetPath.Split('/');
        string currentPath = segments[0];

        for (int index = 1; index < segments.Length; index++)
        {
            string nextPath = $"{currentPath}/{segments[index]}";
            if (!AssetDatabase.IsValidFolder(nextPath))
                AssetDatabase.CreateFolder(currentPath, segments[index]);

            currentPath = nextPath;
        }
    }
}

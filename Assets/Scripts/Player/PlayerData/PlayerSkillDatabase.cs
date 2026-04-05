using System.Collections.Generic;
using UnityEngine;

public static class PlayerSkillDatabase
{
    public const string WarriorComboMasteryId = "warrior_combo_mastery";
    public const string WarriorRageId = "warrior_rage";
    public const string WarriorPowerStrikeId = "warrior_power_strike";
    public const string ThiefLuckySevenId = "thief_lucky_seven";
    public const string ThiefHasteId = "thief_haste";
    public const string ThiefNimbleBodyId = "thief_nimble_body";
    public const string MageMagicClawId = "mage_magic_claw";
    public const string MageMagicGuardId = "mage_magic_guard";
    public const string MageMpBoostId = "mage_mp_boost";

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
        definitions = BuildLookup(asset != null && asset.Definitions != null && asset.Definitions.Count > 0
            ? asset.Definitions
            : CreateFallbackDefinitions());
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

    private static IReadOnlyList<PlayerSkillDefinition> CreateFallbackDefinitions()
    {
        return new[]
        {
            PlayerSkillDefinition.CreateTransient(
                WarriorComboMasteryId,
                "Combo Mastery",
                PlayerJobType.Warrior,
                PlayerSkillType.Passive,
                20,
                -1),
            PlayerSkillDefinition.CreateTransient(
                WarriorRageId,
                "Rage",
                PlayerJobType.Warrior,
                PlayerSkillType.ActiveBuff,
                20,
                2),
            PlayerSkillDefinition.CreateTransient(
                WarriorPowerStrikeId,
                "Power Strike",
                PlayerJobType.Warrior,
                PlayerSkillType.ActiveAttack,
                20,
                1,
                newManaCost: 8,
                newCooldown: 0f,
                newRange: 1.9f,
                newDamageMultiplier: 1.85f,
                newHitCount: 1,
                newHitInterval: 0f,
                newAnimationVariantIndex: 4,
                newAnimatorStateName: "Combo04_InPlace_SingleSword",
                newAnimationSpeed: 1.08f,
                newAttackDuration: 0.92f,
                newTargetingMode: PlayerSkillTargetingMode.MeleeArea,
                newMaxTargets: 3),
            PlayerSkillDefinition.CreateTransient(
                ThiefLuckySevenId,
                "Lucky Seven",
                PlayerJobType.Thief,
                PlayerSkillType.ActiveAttack,
                20,
                1,
                newManaCost: 6,
                newCooldown: 0f,
                newRange: 6.25f,
                newDamageMultiplier: 1.02f,
                newHitCount: 1,
                newHitInterval: 0.08f,
                newAnimationVariantIndex: 2,
                newAnimatorStateName: "Combo02_InPlace_SingleSword 0",
                newAnimationSpeed: 0.96f,
                newAttackDuration: 0.82f,
                newTargetingMode: PlayerSkillTargetingMode.ForwardProjectile,
                newMaxTargets: 1,
                newProjectileCount: 2,
                newProjectileSpeed: 15f,
                newProjectileRadius: 0.18f,
                newProjectileLifetime: 0.65f,
                newProjectileSpreadAngle: 7f,
                newProjectileSpawnForwardOffset: 0.95f,
                newProjectileSpawnUpOffset: 1.05f,
                newProjectileVisualScale: 0.16f),
            PlayerSkillDefinition.CreateTransient(
                ThiefHasteId,
                "Haste",
                PlayerJobType.Thief,
                PlayerSkillType.ActiveBuff,
                20,
                2),
            PlayerSkillDefinition.CreateTransient(
                ThiefNimbleBodyId,
                "Nimble Body",
                PlayerJobType.Thief,
                PlayerSkillType.Passive,
                20,
                -1),
            PlayerSkillDefinition.CreateTransient(
                MageMagicClawId,
                "Magic Claw",
                PlayerJobType.Mage,
                PlayerSkillType.ActiveAttack,
                20,
                1,
                newManaCost: 10,
                newCooldown: 0f,
                newRange: 6.5f,
                newDamageMultiplier: 0.92f,
                newHitCount: 2,
                newHitInterval: 0.14f,
                newAnimationVariantIndex: 1,
                newAnimatorStateName: "Combo03_InPlace_SingleSword 0",
                newAnimationSpeed: 0.92f,
                newAttackDuration: 0.9f,
                newTargetingMode: PlayerSkillTargetingMode.FrontSingleTarget,
                newMaxTargets: 1),
            PlayerSkillDefinition.CreateTransient(
                MageMagicGuardId,
                "Magic Guard",
                PlayerJobType.Mage,
                PlayerSkillType.ActiveBuff,
                20,
                2),
            PlayerSkillDefinition.CreateTransient(
                MageMpBoostId,
                "MP Boost",
                PlayerJobType.Mage,
                PlayerSkillType.Passive,
                20,
                -1)
        };
    }
}

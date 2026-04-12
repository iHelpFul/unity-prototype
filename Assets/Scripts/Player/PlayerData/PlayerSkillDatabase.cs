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
                VanguardComboMasteryId,
                "Combo Mastery",
                PlayerJobType.Vanguard,
                PlayerSkillType.Passive,
                20,
                -1),
            PlayerSkillDefinition.CreateTransient(
                VanguardRageId,
                "Rage",
                PlayerJobType.Vanguard,
                PlayerSkillType.ActiveBuff,
                20,
                2),
            PlayerSkillDefinition.CreateTransient(
                VanguardPowerStrikeId,
                "Power Strike",
                PlayerJobType.Vanguard,
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
                ShadeLuckySevenId,
                "Lucky Seven",
                PlayerJobType.Shade,
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
                ShadeHasteId,
                "Haste",
                PlayerJobType.Shade,
                PlayerSkillType.ActiveBuff,
                20,
                2),
            PlayerSkillDefinition.CreateTransient(
                ShadeNimbleBodyId,
                "Nimble Body",
                PlayerJobType.Shade,
                PlayerSkillType.Passive,
                20,
                -1),
            PlayerSkillDefinition.CreateTransient(
                ArcanistMagicClawId,
                "Magic Claw",
                PlayerJobType.Arcanist,
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
                ArcanistMagicGuardId,
                "Magic Guard",
                PlayerJobType.Arcanist,
                PlayerSkillType.ActiveBuff,
                20,
                2),
            PlayerSkillDefinition.CreateTransient(
                ArcanistMpBoostId,
                "MP Boost",
                PlayerJobType.Arcanist,
                PlayerSkillType.Passive,
                20,
                -1)
        };
    }
}


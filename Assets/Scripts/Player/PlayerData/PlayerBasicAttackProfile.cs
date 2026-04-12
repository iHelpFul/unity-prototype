using System.Collections.Generic;
using UnityEngine;

public enum PlayerBasicAttackSelectionMode
{
    Sequential = 0,
    Random = 1
}

[CreateAssetMenu(menuName = "Game Data/Jobs/Basic Attack Profile")]
public class PlayerBasicAttackProfile : ScriptableObject
{
    [SerializeField] private PlayerJobType jobType = PlayerJobType.Drifter;
    [SerializeField] private int animationVariantCount = 1;
    [SerializeField] private int maxChainCount = 1;
    [SerializeField] private PlayerBasicAttackSelectionMode selectionMode = PlayerBasicAttackSelectionMode.Sequential;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float maxAttackDuration = 1f;
    [SerializeField] private float attackAnimationSpeed = 1f;
    [SerializeField] private int maxTargets = 1;
    [SerializeField] private float basicDamageMultiplier = 1f;
    [SerializeField] private int maxComboCounter;
    [SerializeField] private float comboResetDelay;
    [SerializeField] private float comboDamageBonusPerStack;
    [SerializeField] private bool supportsComboCounter;

    public PlayerJobType JobType => jobType;
    public int AnimationVariantCount => animationVariantCount;
    public int MaxChainCount => maxChainCount;
    public PlayerBasicAttackSelectionMode SelectionMode => selectionMode;
    public float AttackCooldown => attackCooldown;
    public float MaxAttackDuration => maxAttackDuration;
    public float AttackAnimationSpeed => attackAnimationSpeed;
    public int MaxTargets => maxTargets;
    public float BasicDamageMultiplier => basicDamageMultiplier;
    public int MaxComboCounter => maxComboCounter;
    public float ComboResetDelay => comboResetDelay;
    public float ComboDamageBonusPerStack => comboDamageBonusPerStack;
    public bool SupportsComboCounter => supportsComboCounter;

    public void Initialize(
        PlayerJobType newJobType,
        int newAnimationVariantCount,
        int newMaxChainCount,
        PlayerBasicAttackSelectionMode newSelectionMode,
        float newAttackCooldown,
        float newMaxAttackDuration,
        float newAttackAnimationSpeed,
        int newMaxTargets,
        float newBasicDamageMultiplier,
        int newMaxComboCounter,
        float newComboResetDelay,
        float newComboDamageBonusPerStack,
        bool newSupportsComboCounter)
    {
        jobType = newJobType;
        animationVariantCount = newAnimationVariantCount;
        maxChainCount = newMaxChainCount;
        selectionMode = newSelectionMode;
        attackCooldown = newAttackCooldown;
        maxAttackDuration = newMaxAttackDuration;
        attackAnimationSpeed = newAttackAnimationSpeed;
        maxTargets = newMaxTargets;
        basicDamageMultiplier = newBasicDamageMultiplier;
        maxComboCounter = newMaxComboCounter;
        comboResetDelay = newComboResetDelay;
        comboDamageBonusPerStack = newComboDamageBonusPerStack;
        supportsComboCounter = newSupportsComboCounter;
        Sanitize();
    }

    public static PlayerBasicAttackProfile CreateTransient(
        PlayerJobType newJobType,
        int newAnimationVariantCount,
        int newMaxChainCount,
        PlayerBasicAttackSelectionMode newSelectionMode,
        float newAttackCooldown,
        float newMaxAttackDuration,
        float newAttackAnimationSpeed,
        int newMaxTargets,
        float newBasicDamageMultiplier,
        int newMaxComboCounter,
        float newComboResetDelay,
        float newComboDamageBonusPerStack,
        bool newSupportsComboCounter)
    {
        PlayerBasicAttackProfile profile = CreateInstance<PlayerBasicAttackProfile>();
        profile.hideFlags = HideFlags.HideAndDontSave;
        profile.Initialize(
            newJobType,
            newAnimationVariantCount,
            newMaxChainCount,
            newSelectionMode,
            newAttackCooldown,
            newMaxAttackDuration,
            newAttackAnimationSpeed,
            newMaxTargets,
            newBasicDamageMultiplier,
            newMaxComboCounter,
            newComboResetDelay,
            newComboDamageBonusPerStack,
            newSupportsComboCounter);
        return profile;
    }

    private void OnValidate()
    {
        Sanitize();
    }

    private void Sanitize()
    {
        animationVariantCount = Mathf.Max(1, animationVariantCount);
        maxChainCount = Mathf.Max(1, maxChainCount);
        attackCooldown = Mathf.Max(0f, attackCooldown);
        maxAttackDuration = Mathf.Max(0.05f, maxAttackDuration);
        attackAnimationSpeed = Mathf.Max(0.05f, attackAnimationSpeed);
        maxTargets = Mathf.Max(1, maxTargets);
        basicDamageMultiplier = Mathf.Max(0.05f, basicDamageMultiplier);
        maxComboCounter = Mathf.Max(0, maxComboCounter);
        comboResetDelay = Mathf.Max(0f, comboResetDelay);
        comboDamageBonusPerStack = Mathf.Max(0f, comboDamageBonusPerStack);

        if (!supportsComboCounter)
        {
            maxComboCounter = 0;
            comboResetDelay = 0f;
            comboDamageBonusPerStack = 0f;
        }
    }
}

public static class PlayerJobCombatProfiles
{
    private const string ResourcePath = "GameData/PlayerJobDatabase";

    private static Dictionary<PlayerJobType, PlayerJobDefinition> definitions;
    private static Dictionary<PlayerJobType, PlayerJobDefinition> fallbackDefinitions;
    private static PlayerJobDatabaseAsset asset;

    public static PlayerBasicAttackProfile GetBasicAttackProfile(PlayerJobType jobType)
    {
        PlayerJobDefinition definition = GetJobDefinition(jobType);
        if (definition != null && definition.BasicAttackProfile != null)
            return definition.BasicAttackProfile;

        return GetFallbackJobDefinition(jobType)?.BasicAttackProfile;
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

        return PlayerSkillDatabase.GetDefaultSkillsForJob(jobType);
    }

    public static PlayerJobDefinition GetJobDefinition(PlayerJobType jobType)
    {
        EnsureLoaded();

        if (definitions != null && definitions.TryGetValue(jobType, out PlayerJobDefinition definition) && definition != null)
            return definition;

        return GetFallbackJobDefinition(jobType);
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
        if (data == null || data.CurrentJob != PlayerJobType.Drifter)
            return false;

        PlayerJobDefinition noviceDefinition = GetJobDefinition(PlayerJobType.Drifter);
        int requiredLevel = noviceDefinition != null
            ? Mathf.Max(1, noviceDefinition.AdvancementLevelRequirement)
            : 10;

        return data.Level >= requiredLevel;
    }

    public static void ResetCache()
    {
        definitions = null;
        fallbackDefinitions = null;
        asset = null;
    }

    private static void EnsureLoaded()
    {
        if (definitions != null)
            return;

        asset = Resources.Load<PlayerJobDatabaseAsset>(ResourcePath);
        definitions = new Dictionary<PlayerJobType, PlayerJobDefinition>();

        if (asset?.Definitions == null)
            return;

        for (int index = 0; index < asset.Definitions.Count; index++)
        {
            PlayerJobDefinition definition = asset.Definitions[index];
            if (definition == null)
                continue;

            definitions[definition.JobType] = definition;
        }
    }

    private static PlayerJobDefinition GetFallbackJobDefinition(PlayerJobType jobType)
    {
        fallbackDefinitions ??= CreateFallbackDefinitions();

        if (fallbackDefinitions.TryGetValue(jobType, out PlayerJobDefinition definition))
            return definition;

        return fallbackDefinitions[PlayerJobType.Drifter];
    }

    private static Dictionary<PlayerJobType, PlayerJobDefinition> CreateFallbackDefinitions()
    {
        PlayerBasicAttackProfile noviceProfile = PlayerBasicAttackProfile.CreateTransient(
            PlayerJobType.Drifter,
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

        PlayerBasicAttackProfile warriorProfile = PlayerBasicAttackProfile.CreateTransient(
            PlayerJobType.Vanguard,
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

        PlayerBasicAttackProfile thiefProfile = PlayerBasicAttackProfile.CreateTransient(
            PlayerJobType.Shade,
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

        PlayerBasicAttackProfile mageProfile = PlayerBasicAttackProfile.CreateTransient(
            PlayerJobType.Arcanist,
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

        return new Dictionary<PlayerJobType, PlayerJobDefinition>
        {
            [PlayerJobType.Drifter] = PlayerJobDefinition.CreateTransient(
                PlayerJobType.Drifter,
                "Drifter",
                10,
                noviceProfile,
                new PlayerSkillDefinition[0]),
            [PlayerJobType.Vanguard] = PlayerJobDefinition.CreateTransient(
                PlayerJobType.Vanguard,
                "Vanguard",
                10,
                warriorProfile,
                ResolveDefaultSkills(
                    PlayerSkillDatabase.VanguardPowerStrikeId,
                    PlayerSkillDatabase.VanguardRageId,
                    PlayerSkillDatabase.VanguardComboMasteryId)),
            [PlayerJobType.Shade] = PlayerJobDefinition.CreateTransient(
                PlayerJobType.Shade,
                "Shade",
                10,
                thiefProfile,
                ResolveDefaultSkills(
                    PlayerSkillDatabase.ShadeLuckySevenId,
                    PlayerSkillDatabase.ShadeHasteId,
                    PlayerSkillDatabase.ShadeNimbleBodyId)),
            [PlayerJobType.Arcanist] = PlayerJobDefinition.CreateTransient(
                PlayerJobType.Arcanist,
                "Arcanist",
                10,
                mageProfile,
                ResolveDefaultSkills(
                    PlayerSkillDatabase.ArcanistMagicClawId,
                    PlayerSkillDatabase.ArcanistMagicGuardId,
                    PlayerSkillDatabase.ArcanistMpBoostId))
        };
    }

    private static IReadOnlyList<PlayerSkillDefinition> ResolveDefaultSkills(params string[] skillIds)
    {
        List<PlayerSkillDefinition> defaultSkills = new List<PlayerSkillDefinition>();

        for (int index = 0; index < skillIds.Length; index++)
        {
            PlayerSkillDefinition definition = PlayerSkillDatabase.GetDefinition(skillIds[index]);
            if (definition != null)
                defaultSkills.Add(definition);
        }

        return defaultSkills;
    }
}


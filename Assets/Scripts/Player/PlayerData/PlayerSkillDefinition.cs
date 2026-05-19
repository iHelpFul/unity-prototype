using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

[System.Serializable]
public enum PlayerSkillType
{
    Passive = 0,
    Utility = 1,
    Attack = 2
}

[System.Serializable]
public class PlayerSkillLevelDefinition
{
    [SerializeField] private int level = 1;
    [SerializeField] private int manaCost;
    [SerializeField] private float cooldown;
    [SerializeField] private float recoveryTime = 0.2f;
    [SerializeField] private float range = 1f;
    [SerializeField] private int hitCount = 1;
    [SerializeField] private float damageCoefficient = 1f;
    [SerializeField] private float extraSurgeChance;
    [SerializeField] private float extraSurgePower;
    [SerializeField] private int momentumGain;
    [SerializeField] private int momentumCost;
    [SerializeField] private ProjectileProfile projectileProfile;

    public int Level => level;
    public int ManaCost => manaCost;
    public float Cooldown => cooldown;
    public float RecoveryTime => recoveryTime;
    public float Range => range;
    public int HitCount => hitCount;
    public float DamageCoefficient => damageCoefficient;
    public float ExtraSurgeChance => extraSurgeChance;
    public float ExtraSurgePower => extraSurgePower;
    public int MomentumGain => momentumGain;
    public int MomentumCost => momentumCost;
    public ProjectileProfile ProjectileProfile => projectileProfile;

    public void Sanitize()
    {
        level = Mathf.Max(1, level);
        manaCost = Mathf.Max(0, manaCost);
        cooldown = Mathf.Max(0f, cooldown);
        recoveryTime = Mathf.Max(0f, recoveryTime);
        range = Mathf.Max(0f, range);
        hitCount = Mathf.Max(1, hitCount);
        damageCoefficient = Mathf.Max(0f, damageCoefficient);
        extraSurgeChance = Mathf.Max(0f, extraSurgeChance);
        extraSurgePower = Mathf.Max(0f, extraSurgePower);
        momentumGain = Mathf.Max(0, momentumGain);
        momentumCost = Mathf.Max(0, momentumCost);
    }
}

[CreateAssetMenu(menuName = "Game Data/Skills/Player Skill Definition")]
public class PlayerSkillDefinition : ScriptableObject
{
    [SerializeField] private string skillId = string.Empty;
    [SerializeField] private string displayName = "New Skill";
    [SerializeField] private string description = string.Empty;
    [SerializeField] private Sprite icon;
    [SerializeField] private PlayerJobType jobType = PlayerJobType.Novice;
    [SerializeField] private PlayerSkillType skillType = PlayerSkillType.Attack;
    [SerializeField] private CombatAttackFamily attackFamily = CombatAttackFamily.None;
    [SerializeField] private CombatExecutionKind executionKind = CombatExecutionKind.Direct;
    [SerializeField] private CombatTargetingKind combatTargetingKind = CombatTargetingKind.SingleTarget;
    [SerializeField] private CombatHitBoxDefinition hitBox;
    [FormerlySerializedAs("enemyCombatStateInteraction")]
    [SerializeField] private ReadyStateEmpowerDefinition readyStateEmpower;
    [SerializeField] private UtilitySkillProfile utilitySkillProfile;
    [SerializeField] private BurstLinkedSkillProfile burstLinkedSkillProfile;
    [SerializeField] private PresentationCueSet presentationCueSet;
    [SerializeField] private PresentationCueSet burstPresentationCueSet;
    [SerializeField] private ProjectileProfile defaultProjectileProfile;
    [SerializeField] private ProjectileLaunchMode projectileLaunchMode = ProjectileLaunchMode.Locked;
    [SerializeField] private int baseMomentumGain;
    [SerializeField] private int baseMomentumCost;
    [SerializeField] private float baseBreakPower;
    [SerializeField] private float baseSurgeChanceBonus;
    [SerializeField] private float baseSurgePowerBonus;
    [SerializeField] private CombatElementType defaultElement = CombatElementType.None;
    [SerializeField] private List<PlayerSkillLevelDefinition> levels = new List<PlayerSkillLevelDefinition>();
    [SerializeField] private int maxLevel = 1;
    [SerializeField] private int defaultSlotIndex = -1;
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private float hitInterval;
    [SerializeField] private int animationVariantIndex = 1;
    [SerializeField] private string animatorStateName = string.Empty;
    [SerializeField] private float animationSpeed = 1f;
    [SerializeField] private float attackDuration = 0.9f;
    [SerializeField] private int maxTargets = 1;
    [SerializeField] private int projectileCount = 1;
    [SerializeField] private float projectileSpreadAngle;

    public string SkillId => skillId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public PlayerJobType JobType => jobType;
    public PlayerSkillType SkillType => skillType;
    public CombatAttackFamily AttackFamily => attackFamily;
    public CombatExecutionKind ExecutionKind => executionKind;
    public CombatTargetingKind CombatTargetingKind => combatTargetingKind;
    public CombatHitBoxDefinition HitBox => hitBox.GetSanitized();
    public ReadyStateEmpowerDefinition ReadyStateEmpower => readyStateEmpower.GetSanitized();
    public UtilitySkillProfile UtilitySkillProfile => utilitySkillProfile;
    public BurstLinkedSkillProfile BurstLinkedSkillProfile => burstLinkedSkillProfile;
    public PresentationCueSet PresentationCueSet => presentationCueSet;
    public PresentationCueSet BurstPresentationCueSet => burstPresentationCueSet != null
        ? burstPresentationCueSet
        : presentationCueSet;
    public ProjectileProfile DefaultProjectileProfile => defaultProjectileProfile;
    public ProjectileLaunchMode ProjectileLaunchMode => projectileLaunchMode;
    public int BaseMomentumGain => baseMomentumGain;
    public int BaseMomentumCost => baseMomentumCost;
    public float BaseBreakPower => baseBreakPower;
    public float BaseSurgeChanceBonus => baseSurgeChanceBonus;
    public float BaseSurgePowerBonus => baseSurgePowerBonus;
    public CombatElementType DefaultElement => defaultElement;
    public IReadOnlyList<PlayerSkillLevelDefinition> Levels => levels;
    public int MaxLevel => maxLevel;
    public int DefaultSlotIndex => defaultSlotIndex;
    public int ManaCost => GetResolvedManaCost(1);
    public float Cooldown => GetResolvedCooldown(1);
    public float Range => GetResolvedRange(1);
    public float DamageMultiplier => damageMultiplier;
    public int HitCount => GetResolvedHitCount(1);
    public float HitInterval => hitInterval;
    public int AnimationVariantIndex => animationVariantIndex;
    public string AnimatorStateName => animatorStateName;
    public float AnimationSpeed => animationSpeed;
    public float AttackDuration => attackDuration;
    public int MaxTargets => maxTargets;
    public int ProjectileCount => projectileCount;
    public float ProjectileSpeed => GetResolvedProjectileSpeed(1);
    public float ProjectileHitBoxRange => GetResolvedProjectileHitBoxRange(1);
    public float ProjectileLifetime => GetResolvedProjectileLifetime(1);
    public float ProjectileSpreadAngle => projectileSpreadAngle;
    public float ProjectileSpawnForwardOffset => GetResolvedProjectileSpawnForwardOffset(1);
    public float ProjectileSpawnUpOffset => GetResolvedProjectileSpawnUpOffset(1);
    public float ProjectileVisualScale => GetResolvedProjectileVisualScale(1);
    public bool HasUtilitySkillProfile => utilitySkillProfile != null;
    public bool HasBurstLinkedSkillProfile => burstLinkedSkillProfile != null;

    public PresentationCueSet ResolvePresentationCueSet(AttackPayloadActionKind actionKind)
    {
        return actionKind == AttackPayloadActionKind.BurstLinkedSkill
            ? BurstPresentationCueSet
            : PresentationCueSet;
    }

    public PlayerSkillLevelDefinition GetLevelDefinition(int skillLevel)
    {
        if (levels == null || levels.Count == 0)
            return null;

        int clampedLevel = Mathf.Max(1, skillLevel);
        PlayerSkillLevelDefinition bestMatch = null;

        for (int index = 0; index < levels.Count; index++)
        {
            PlayerSkillLevelDefinition candidate = levels[index];
            if (candidate == null)
                continue;

            if (candidate.Level == clampedLevel)
                return candidate;

            if (candidate.Level <= clampedLevel)
            {
                if (bestMatch == null || candidate.Level > bestMatch.Level)
                    bestMatch = candidate;
            }
        }

        return bestMatch;
    }

    public int GetResolvedManaCost(int skillLevel)
    {
        PlayerSkillLevelDefinition levelDefinition = GetLevelDefinition(skillLevel);
        if (levelDefinition != null)
            return Mathf.Max(0, levelDefinition.ManaCost);

        return 0;
    }

    public float GetResolvedCooldown(int skillLevel)
    {
        PlayerSkillLevelDefinition levelDefinition = GetLevelDefinition(skillLevel);
        if (levelDefinition != null)
            return Mathf.Max(0f, levelDefinition.Cooldown);

        return 0f;
    }

    public float GetResolvedRecoveryTime(int skillLevel)
    {
        PlayerSkillLevelDefinition levelDefinition = GetLevelDefinition(skillLevel);
        if (levelDefinition != null && levelDefinition.RecoveryTime > 0f)
            return Mathf.Max(0f, levelDefinition.RecoveryTime);

        return 0f;
    }

    public float GetResolvedRange(int skillLevel)
    {
        PlayerSkillLevelDefinition levelDefinition = GetLevelDefinition(skillLevel);
        if (levelDefinition != null)
            return Mathf.Max(0f, levelDefinition.Range);

        return 0f;
    }

    public int GetResolvedHitCount(int skillLevel)
    {
        PlayerSkillLevelDefinition levelDefinition = GetLevelDefinition(skillLevel);
        if (levelDefinition != null)
            return Mathf.Max(1, levelDefinition.HitCount);

        return 1;
    }

    public int GetResolvedMomentumGain(int skillLevel)
    {
        PlayerSkillLevelDefinition levelDefinition = GetLevelDefinition(skillLevel);
        int resolvedGain = baseMomentumGain + (levelDefinition != null ? levelDefinition.MomentumGain : 0);
        return Mathf.Max(0, resolvedGain);
    }

    public int GetResolvedMomentumCost(int skillLevel)
    {
        PlayerSkillLevelDefinition levelDefinition = GetLevelDefinition(skillLevel);
        int resolvedCost = baseMomentumCost + (levelDefinition != null ? levelDefinition.MomentumCost : 0);
        return Mathf.Max(0, resolvedCost);
    }

    public float GetResolvedHitInterval(int skillLevel)
    {
        return Mathf.Max(0f, hitInterval);
    }

    public int GetResolvedMaxTargets(int skillLevel)
    {
        ProjectileProfile profile = GetResolvedProjectileProfile(skillLevel);
        if (profile != null && profile.MaxTargets > 1)
            return Mathf.Max(1, profile.MaxTargets);

        return Mathf.Max(1, maxTargets);
    }

    public int GetResolvedProjectileCount(int skillLevel)
    {
        return Mathf.Max(1, projectileCount);
    }

    public ProjectileProfile GetResolvedProjectileProfile(int skillLevel)
    {
        PlayerSkillLevelDefinition levelDefinition = GetLevelDefinition(skillLevel);
        if (levelDefinition != null && levelDefinition.ProjectileProfile != null)
            return levelDefinition.ProjectileProfile;

        return defaultProjectileProfile;
    }

    public float GetResolvedProjectileSpeed(int skillLevel)
    {
        ProjectileProfile profile = GetResolvedProjectileProfile(skillLevel);
        if (profile != null)
            return Mathf.Max(0f, profile.Speed);

        return 0f;
    }

    public float GetResolvedProjectileHitBoxRange(int skillLevel)
    {
        ProjectileProfile profile = GetResolvedProjectileProfile(skillLevel);
        if (profile != null)
            return Mathf.Max(0.05f, profile.CollisionRange);

        return ProjectileProfileUtility.DefaultCollisionRange;
    }

    public CombatHitBoxDefinition GetResolvedProjectileHitBox(int skillLevel)
    {
        ProjectileProfile profile = GetResolvedProjectileProfile(skillLevel);
        return ProjectileProfileUtility.ResolveCollisionHitBox(profile);
    }

    public float GetResolvedProjectileLifetime(int skillLevel)
    {
        ProjectileProfile profile = GetResolvedProjectileProfile(skillLevel);
        if (profile != null)
            return Mathf.Max(0.05f, profile.Lifetime);

        return 0.5f;
    }

    public float GetResolvedProjectileSpawnForwardOffset(int skillLevel)
    {
        ProjectileProfile profile = GetResolvedProjectileProfile(skillLevel);
        if (profile != null)
            return Mathf.Max(0f, profile.SpawnForwardOffset);

        return 0.8f;
    }

    public float GetResolvedProjectileSpawnUpOffset(int skillLevel)
    {
        ProjectileProfile profile = GetResolvedProjectileProfile(skillLevel);
        if (profile != null)
            return profile.SpawnUpOffset;

        return 1f;
    }

    public float GetResolvedProjectileVisualScale(int skillLevel)
    {
        ProjectileProfile profile = GetResolvedProjectileProfile(skillLevel);
        if (profile != null)
            return Mathf.Max(0.01f, profile.VisualScale);

        return 0.2f;
    }

    private void OnValidate()
    {
        Sanitize();
    }

    private void Sanitize()
    {
        skillId = string.IsNullOrWhiteSpace(skillId) ? string.Empty : skillId.Trim();
        displayName = string.IsNullOrWhiteSpace(displayName) ? "Unnamed Skill" : displayName.Trim();
        description = string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim();
        baseMomentumGain = Mathf.Max(0, baseMomentumGain);
        baseMomentumCost = Mathf.Max(0, baseMomentumCost);
        baseBreakPower = Mathf.Max(0f, baseBreakPower);
        baseSurgeChanceBonus = Mathf.Max(0f, baseSurgeChanceBonus);
        baseSurgePowerBonus = Mathf.Max(0f, baseSurgePowerBonus);
        hitBox = hitBox.GetSanitized();
        readyStateEmpower = readyStateEmpower.GetSanitized();
        maxLevel = Mathf.Max(1, maxLevel);
        damageMultiplier = Mathf.Max(0f, damageMultiplier);
        hitInterval = Mathf.Max(0f, hitInterval);
        animationVariantIndex = Mathf.Max(1, animationVariantIndex);
        animatorStateName = string.IsNullOrWhiteSpace(animatorStateName) ? string.Empty : animatorStateName.Trim();
        animationSpeed = Mathf.Max(0.05f, animationSpeed);
        attackDuration = Mathf.Max(0.05f, attackDuration);
        maxTargets = Mathf.Max(1, maxTargets);
        projectileCount = Mathf.Max(1, projectileCount);
        levels ??= new List<PlayerSkillLevelDefinition>();

        int highestLevel = 0;
        for (int index = 0; index < levels.Count; index++)
        {
            PlayerSkillLevelDefinition levelDefinition = levels[index];
            if (levelDefinition == null)
                continue;

            levelDefinition.Sanitize();
            highestLevel = Mathf.Max(highestLevel, levelDefinition.Level);
        }

        if (highestLevel > 0)
            maxLevel = Mathf.Max(maxLevel, highestLevel);
    }
}


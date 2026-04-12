using UnityEngine;

[System.Serializable]
public enum PlayerSkillType
{
    Passive = 0,
    ActiveBuff = 1,
    ActiveAttack = 2
}

public enum PlayerSkillTargetingMode
{
    MeleeArea = 0,
    FrontSingleTarget = 1,
    ForwardProjectile = 2
}

[CreateAssetMenu(menuName = "Game Data/Skills/Player Skill Definition")]
public class PlayerSkillDefinition : ScriptableObject
{
    [SerializeField] private string skillId = string.Empty;
    [SerializeField] private string displayName = "New Skill";
    [SerializeField] private PlayerJobType jobType = PlayerJobType.Drifter;
    [SerializeField] private PlayerSkillType skillType = PlayerSkillType.ActiveAttack;
    [SerializeField] private int maxLevel = 1;
    [SerializeField] private int defaultSlotIndex = -1;
    [SerializeField] private int manaCost;
    [SerializeField] private float cooldown;
    [SerializeField] private float range = 1f;
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private int hitCount = 1;
    [SerializeField] private float hitInterval;
    [SerializeField] private int animationVariantIndex = 1;
    [SerializeField] private string animatorStateName = string.Empty;
    [SerializeField] private float animationSpeed = 1f;
    [SerializeField] private float attackDuration = 0.9f;
    [SerializeField] private PlayerSkillTargetingMode targetingMode = PlayerSkillTargetingMode.FrontSingleTarget;
    [SerializeField] private int maxTargets = 1;
    [SerializeField] private int projectileCount = 1;
    [SerializeField] private float projectileSpeed;
    [SerializeField] private float projectileRadius = 0.2f;
    [SerializeField] private float projectileLifetime = 0.5f;
    [SerializeField] private float projectileSpreadAngle;
    [SerializeField] private float projectileSpawnForwardOffset = 0.8f;
    [SerializeField] private float projectileSpawnUpOffset = 1f;
    [SerializeField] private float projectileVisualScale = 0.2f;

    public string SkillId => skillId;
    public string DisplayName => displayName;
    public PlayerJobType JobType => jobType;
    public PlayerSkillType SkillType => skillType;
    public int MaxLevel => maxLevel;
    public int DefaultSlotIndex => defaultSlotIndex;
    public int ManaCost => manaCost;
    public float Cooldown => cooldown;
    public float Range => range;
    public float DamageMultiplier => damageMultiplier;
    public int HitCount => hitCount;
    public float HitInterval => hitInterval;
    public int AnimationVariantIndex => animationVariantIndex;
    public string AnimatorStateName => animatorStateName;
    public float AnimationSpeed => animationSpeed;
    public float AttackDuration => attackDuration;
    public PlayerSkillTargetingMode TargetingMode => targetingMode;
    public int MaxTargets => maxTargets;
    public int ProjectileCount => projectileCount;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileRadius => projectileRadius;
    public float ProjectileLifetime => projectileLifetime;
    public float ProjectileSpreadAngle => projectileSpreadAngle;
    public float ProjectileSpawnForwardOffset => projectileSpawnForwardOffset;
    public float ProjectileSpawnUpOffset => projectileSpawnUpOffset;
    public float ProjectileVisualScale => projectileVisualScale;

    public void Initialize(
        string newSkillId,
        string newDisplayName,
        PlayerJobType newJobType,
        PlayerSkillType newSkillType,
        int newMaxLevel,
        int newDefaultSlotIndex,
        int newManaCost = 0,
        float newCooldown = 0f,
        float newRange = 0f,
        float newDamageMultiplier = 1f,
        int newHitCount = 1,
        float newHitInterval = 0f,
        int newAnimationVariantIndex = 1,
        string newAnimatorStateName = "",
        float newAnimationSpeed = 1f,
        float newAttackDuration = 0.9f,
        PlayerSkillTargetingMode newTargetingMode = PlayerSkillTargetingMode.FrontSingleTarget,
        int newMaxTargets = 1,
        int newProjectileCount = 1,
        float newProjectileSpeed = 0f,
        float newProjectileRadius = 0.2f,
        float newProjectileLifetime = 0.5f,
        float newProjectileSpreadAngle = 0f,
        float newProjectileSpawnForwardOffset = 0.8f,
        float newProjectileSpawnUpOffset = 1f,
        float newProjectileVisualScale = 0.2f)
    {
        skillId = newSkillId;
        displayName = newDisplayName;
        jobType = newJobType;
        skillType = newSkillType;
        maxLevel = newMaxLevel;
        defaultSlotIndex = newDefaultSlotIndex;
        manaCost = newManaCost;
        cooldown = newCooldown;
        range = newRange;
        damageMultiplier = newDamageMultiplier;
        hitCount = newHitCount;
        hitInterval = newHitInterval;
        animationVariantIndex = newAnimationVariantIndex;
        animatorStateName = newAnimatorStateName;
        animationSpeed = newAnimationSpeed;
        attackDuration = newAttackDuration;
        targetingMode = newTargetingMode;
        maxTargets = newMaxTargets;
        projectileCount = newProjectileCount;
        projectileSpeed = newProjectileSpeed;
        projectileRadius = newProjectileRadius;
        projectileLifetime = newProjectileLifetime;
        projectileSpreadAngle = newProjectileSpreadAngle;
        projectileSpawnForwardOffset = newProjectileSpawnForwardOffset;
        projectileSpawnUpOffset = newProjectileSpawnUpOffset;
        projectileVisualScale = newProjectileVisualScale;
        Sanitize();
    }

    public static PlayerSkillDefinition CreateTransient(
        string newSkillId,
        string newDisplayName,
        PlayerJobType newJobType,
        PlayerSkillType newSkillType,
        int newMaxLevel,
        int newDefaultSlotIndex,
        int newManaCost = 0,
        float newCooldown = 0f,
        float newRange = 0f,
        float newDamageMultiplier = 1f,
        int newHitCount = 1,
        float newHitInterval = 0f,
        int newAnimationVariantIndex = 1,
        string newAnimatorStateName = "",
        float newAnimationSpeed = 1f,
        float newAttackDuration = 0.9f,
        PlayerSkillTargetingMode newTargetingMode = PlayerSkillTargetingMode.FrontSingleTarget,
        int newMaxTargets = 1,
        int newProjectileCount = 1,
        float newProjectileSpeed = 0f,
        float newProjectileRadius = 0.2f,
        float newProjectileLifetime = 0.5f,
        float newProjectileSpreadAngle = 0f,
        float newProjectileSpawnForwardOffset = 0.8f,
        float newProjectileSpawnUpOffset = 1f,
        float newProjectileVisualScale = 0.2f)
    {
        PlayerSkillDefinition definition = CreateInstance<PlayerSkillDefinition>();
        definition.hideFlags = HideFlags.HideAndDontSave;
        definition.Initialize(
            newSkillId,
            newDisplayName,
            newJobType,
            newSkillType,
            newMaxLevel,
            newDefaultSlotIndex,
            newManaCost,
            newCooldown,
            newRange,
            newDamageMultiplier,
            newHitCount,
            newHitInterval,
            newAnimationVariantIndex,
            newAnimatorStateName,
            newAnimationSpeed,
            newAttackDuration,
            newTargetingMode,
            newMaxTargets,
            newProjectileCount,
            newProjectileSpeed,
            newProjectileRadius,
            newProjectileLifetime,
            newProjectileSpreadAngle,
            newProjectileSpawnForwardOffset,
            newProjectileSpawnUpOffset,
            newProjectileVisualScale);
        return definition;
    }

    private void OnValidate()
    {
        Sanitize();
    }

    private void Sanitize()
    {
        skillId = string.IsNullOrWhiteSpace(skillId) ? string.Empty : skillId.Trim();
        displayName = string.IsNullOrWhiteSpace(displayName) ? "Unnamed Skill" : displayName.Trim();
        maxLevel = Mathf.Max(1, maxLevel);
        manaCost = Mathf.Max(0, manaCost);
        cooldown = Mathf.Max(0f, cooldown);
        range = Mathf.Max(0f, range);
        damageMultiplier = Mathf.Max(0f, damageMultiplier);
        hitCount = Mathf.Max(1, hitCount);
        hitInterval = Mathf.Max(0f, hitInterval);
        animationVariantIndex = Mathf.Max(1, animationVariantIndex);
        animatorStateName = string.IsNullOrWhiteSpace(animatorStateName) ? string.Empty : animatorStateName.Trim();
        animationSpeed = Mathf.Max(0.05f, animationSpeed);
        attackDuration = Mathf.Max(0.05f, attackDuration);
        maxTargets = Mathf.Max(1, maxTargets);
        projectileCount = Mathf.Max(1, projectileCount);
        projectileSpeed = Mathf.Max(0f, projectileSpeed);
        projectileRadius = Mathf.Max(0.01f, projectileRadius);
        projectileLifetime = Mathf.Max(0.05f, projectileLifetime);
        projectileSpawnForwardOffset = Mathf.Max(0f, projectileSpawnForwardOffset);
        projectileVisualScale = Mathf.Max(0.01f, projectileVisualScale);
    }
}


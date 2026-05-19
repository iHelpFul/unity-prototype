using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/Enemies/Enemy Definition")]
public class EnemyDefinition : ScriptableObject
{
    [SerializeField] private EnemyType enemyType;

    [Header("Identity")]
    [SerializeField] private EnemyRole role = EnemyRole.Skirmisher;
    [SerializeField] private Sprite icon;
    [SerializeField] private CombatElementType elementType = CombatElementType.None;
    [SerializeField] private EnemyEliteProfile eliteProfile;

    [Header("Core Stats")]
    [SerializeField, Min(1)] private int maxHP = 30;
    [SerializeField, Min(0)] private int defense = 1;
    [Tooltip("Reduces incoming hit chance. 0 means this enemy cannot Evade unless the combat formula disables guaranteed hits.")]
    [SerializeField, Min(0)] private int avoidance;
    [SerializeField, Min(0)] private int expReward = 10;
    [SerializeField, Min(0f)] private float gaugeReward = 1f;

    [Header("Damage")]
    [SerializeField, Min(0)] private int contactDamage = 5;
    [SerializeField, Min(0)] private int animatedAttackDamage = 8;
    [SerializeField, Min(0)] private int hitReactionDamageThreshold;

    [Header("Hit Feedback")]
    [SerializeField] private bool enableHitKnockback;
    [SerializeField, Min(0f)] private float hitKnockbackForce = 0.18f;
    [SerializeField, Min(0f)] private float hitKnockbackDuration = 0.08f;
    [SerializeField, Min(0)] private int hitKnockbackDamageThreshold = 999999;
    [SerializeField, Min(0f)] private float hitReactionMovementLockDuration = 0.16f;

    [Header("Cadence")]
    [SerializeField] private EnemyCadenceProfile cadenceProfile;

    [Header("Poise / Break")]
    [SerializeField] private EnemyPoiseProfile poiseProfile;

    [Header("Drops")]
    [SerializeField, Min(0)] private int minMesoDrop = 5;
    [SerializeField, Min(0)] private int maxMesoDrop = 12;
    [SerializeField, Range(0f, 1f)] private float mesoDropChance = 0.8f;
    [SerializeField, Min(0)] private int redPotionDropCount = 1;
    [SerializeField, Range(0f, 1f)] private float redPotionDropChance = 0.2f;
    [SerializeField, Min(0)] private int bluePotionDropCount = 1;
    [SerializeField, Range(0f, 1f)] private float bluePotionDropChance = 0.12f;
    [SerializeField] private ItemDefinition commonEtcItem;
    [SerializeField, Min(0)] private int commonEtcDropCount = 1;
    [SerializeField, Range(0f, 1f)] private float commonEtcDropChance = 0.35f;

    [Header("Lifecycle")]
    [SerializeField, Min(0.05f)] private float corpseDuration = 1.25f;
    [SerializeField, Min(0.1f)] private float respawnDelay = 6f;

    public EnemyType EnemyType => enemyType;
    public EnemyRole Role => role == EnemyRole.None ? EnemyRole.Skirmisher : role;
    public int MaxHP => maxHP;
    public int Defense => defense;
    public int Avoidance => avoidance;
    public int ExpReward => expReward;
    public float GaugeReward => gaugeReward;
    public Sprite Icon => icon;
    public CombatElementType ElementType => elementType;
    public EnemyEliteProfile EliteProfile => eliteProfile.Sanitize();
    public int ContactDamage => contactDamage;
    public int AnimatedAttackDamage => animatedAttackDamage;
    public int HitReactionDamageThreshold => hitReactionDamageThreshold;
    public bool EnableHitKnockback => enableHitKnockback;
    public float HitKnockbackForce => hitKnockbackForce;
    public float HitKnockbackDuration => hitKnockbackDuration;
    public int HitKnockbackDamageThreshold => hitKnockbackDamageThreshold;
    public float HitReactionMovementLockDuration => hitReactionMovementLockDuration;
    public EnemyCadenceProfile CadenceProfile => cadenceProfile.Sanitize();
    public EnemyPoiseProfile PoiseProfile => poiseProfile.Sanitize();
    public int MinMesoDrop => minMesoDrop;
    public int MaxMesoDrop => maxMesoDrop;
    public float MesoDropChance => mesoDropChance;
    public int RedPotionDropCount => redPotionDropCount;
    public float RedPotionDropChance => redPotionDropChance;
    public int BluePotionDropCount => bluePotionDropCount;
    public float BluePotionDropChance => bluePotionDropChance;
    public ItemDefinition CommonEtcItem => commonEtcItem;
    public string CommonEtcItemId => commonEtcItem != null ? commonEtcItem.ItemId : string.Empty;
    public int CommonEtcDropCount => commonEtcDropCount;
    public float CommonEtcDropChance => commonEtcDropChance;
    public float CorpseDuration => corpseDuration;
    public float RespawnDelay => respawnDelay;

    public void Initialize(
        EnemyType newEnemyType,
        int newMaxHP,
        int newDefense,
        int newExpReward,
        float newGaugeReward,
        CombatElementType newElementType,
        int newContactDamage,
        int newAnimatedAttackDamage,
        int newHitReactionDamageThreshold,
        int newMinMesoDrop,
        int newMaxMesoDrop,
        float newMesoDropChance,
        int newRedPotionDropCount,
        float newRedPotionDropChance,
        int newBluePotionDropCount,
        float newBluePotionDropChance,
        ItemDefinition newCommonEtcItem,
        int newCommonEtcDropCount,
        float newCommonEtcDropChance,
        float newCorpseDuration,
        float newRespawnDelay,
        int newAvoidance = 0)
    {
        enemyType = newEnemyType;
        maxHP = newMaxHP;
        defense = newDefense;
        avoidance = newAvoidance;
        expReward = newExpReward;
        gaugeReward = newGaugeReward;
        elementType = newElementType;
        contactDamage = newContactDamage;
        animatedAttackDamage = newAnimatedAttackDamage;
        hitReactionDamageThreshold = newHitReactionDamageThreshold;
        minMesoDrop = newMinMesoDrop;
        maxMesoDrop = newMaxMesoDrop;
        mesoDropChance = newMesoDropChance;
        redPotionDropCount = newRedPotionDropCount;
        redPotionDropChance = newRedPotionDropChance;
        bluePotionDropCount = newBluePotionDropCount;
        bluePotionDropChance = newBluePotionDropChance;
        commonEtcItem = newCommonEtcItem;
        commonEtcDropCount = newCommonEtcDropCount;
        commonEtcDropChance = newCommonEtcDropChance;
        corpseDuration = newCorpseDuration;
        respawnDelay = newRespawnDelay;
        Sanitize();
    }

    public static EnemyDefinition CreateTransient(
        EnemyType newEnemyType,
        int newMaxHP,
        int newDefense,
        int newExpReward,
        float newGaugeReward,
        CombatElementType newElementType,
        int newContactDamage,
        int newAnimatedAttackDamage,
        int newHitReactionDamageThreshold,
        int newMinMesoDrop,
        int newMaxMesoDrop,
        float newMesoDropChance,
        int newRedPotionDropCount,
        float newRedPotionDropChance,
        int newBluePotionDropCount,
        float newBluePotionDropChance,
        ItemDefinition newCommonEtcItem,
        int newCommonEtcDropCount,
        float newCommonEtcDropChance,
        float newCorpseDuration,
        float newRespawnDelay,
        int newAvoidance = 0)
    {
        EnemyDefinition definition = CreateInstance<EnemyDefinition>();
        definition.hideFlags = HideFlags.HideAndDontSave;
        definition.Initialize(
            newEnemyType,
            newMaxHP,
            newDefense,
            newExpReward,
            newGaugeReward,
            newElementType,
            newContactDamage,
            newAnimatedAttackDamage,
            newHitReactionDamageThreshold,
            newMinMesoDrop,
            newMaxMesoDrop,
            newMesoDropChance,
            newRedPotionDropCount,
            newRedPotionDropChance,
            newBluePotionDropCount,
            newBluePotionDropChance,
            newCommonEtcItem,
            newCommonEtcDropCount,
            newCommonEtcDropChance,
            newCorpseDuration,
            newRespawnDelay,
            newAvoidance);
        return definition;
    }

    private void OnValidate()
    {
        Sanitize();
    }

    private void Sanitize()
    {
        maxHP = Mathf.Max(1, maxHP);
        defense = Mathf.Max(0, defense);
        avoidance = Mathf.Max(0, avoidance);
        expReward = Mathf.Max(0, expReward);
        gaugeReward = Mathf.Max(0f, gaugeReward);
        if (!System.Enum.IsDefined(typeof(CombatElementType), elementType))
            elementType = CombatElementType.None;
        eliteProfile = eliteProfile.Sanitize();
        contactDamage = Mathf.Max(0, contactDamage);
        animatedAttackDamage = Mathf.Max(0, animatedAttackDamage);
        hitReactionDamageThreshold = Mathf.Max(0, hitReactionDamageThreshold);
        hitKnockbackForce = Mathf.Max(0f, hitKnockbackForce);
        hitKnockbackDuration = Mathf.Max(0f, hitKnockbackDuration);
        hitKnockbackDamageThreshold = Mathf.Max(0, hitKnockbackDamageThreshold);
        hitReactionMovementLockDuration = Mathf.Max(0f, hitReactionMovementLockDuration);
        cadenceProfile = cadenceProfile.Sanitize();
        poiseProfile = poiseProfile.Sanitize();
        minMesoDrop = Mathf.Max(0, minMesoDrop);
        maxMesoDrop = Mathf.Max(minMesoDrop, maxMesoDrop);
        mesoDropChance = Mathf.Clamp01(mesoDropChance);
        redPotionDropCount = Mathf.Max(0, redPotionDropCount);
        redPotionDropChance = Mathf.Clamp01(redPotionDropChance);
        bluePotionDropCount = Mathf.Max(0, bluePotionDropCount);
        bluePotionDropChance = Mathf.Clamp01(bluePotionDropChance);
        commonEtcDropCount = Mathf.Max(0, commonEtcDropCount);
        commonEtcDropChance = Mathf.Clamp01(commonEtcDropChance);
        corpseDuration = Mathf.Max(0.05f, corpseDuration);
        respawnDelay = Mathf.Max(0.1f, respawnDelay);
    }
}

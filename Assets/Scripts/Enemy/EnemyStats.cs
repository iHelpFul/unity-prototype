using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class EnemyStats
{
    [Header("Identity")]
    [SerializeField] private EnemyType enemyType;
    [SerializeField] private EnemyDefinition definition;
    [SerializeField] private bool preferDefinitionValues = true;

    [Header("Core Stats")]
    [FormerlySerializedAs("MaxHP")]
    [SerializeField, Min(1)] private int maxHP = 30;
    [FormerlySerializedAs("Defense")]
    [SerializeField, Min(0)] private int defense = 1;
    [Tooltip("Reduces incoming hit chance. 0 means this enemy cannot Evade unless the combat formula disables guaranteed hits.")]
    [SerializeField, Min(0)] private int avoidance;
    [SerializeField, Min(0)] private int expReward = 10;

    [Header("Damage")]
    [Tooltip("Touch damage dealt when the player overlaps the enemy's contact hitbox. Set 0 to use the enemy definition/default value.")]
    [SerializeField, Min(0)] private int contactDamage;
    [Tooltip("Damage dealt by the enemy's animated attack hit frame. Set 0 to use the enemy definition/default value.")]
    [SerializeField, Min(0)] private int animatedAttackDamage;

    [Header("Drops")]
    [SerializeField, Min(0)] private int minMesoDrop;
    [SerializeField, Min(0)] private int maxMesoDrop;
    [SerializeField, Range(0f, 1f)] private float mesoDropChance;
    [SerializeField, Min(0)] private int redPotionDropCount;
    [SerializeField, Range(0f, 1f)] private float redPotionDropChance;
    [SerializeField, Min(0)] private int bluePotionDropCount;
    [SerializeField, Range(0f, 1f)] private float bluePotionDropChance;
    [FormerlySerializedAs("commonEtcItemId")]
    [SerializeField] private string legacyCommonEtcItemId;
    [SerializeField] private ItemDefinition commonEtcItem;
    [SerializeField, Min(0)] private int commonEtcDropCount;
    [SerializeField, Range(0f, 1f)] private float commonEtcDropChance;

    [Header("Lifecycle")]
    [SerializeField, Min(0.05f)] private float corpseDuration;
    [SerializeField, Min(0.1f)] private float respawnDelay;

    public EnemyType EnemyType => enemyType;
    public EnemyDefinition Definition => definition != null ? definition : EnemyDefinitionDatabase.GetDefinition(enemyType);
    public int MaxHP => preferDefinitionValues && Definition != null ? Definition.MaxHP : maxHP;
    public int Defense => preferDefinitionValues && Definition != null ? Definition.Defense : defense;
    public int Avoidance => preferDefinitionValues && Definition != null ? Definition.Avoidance : avoidance;
    public int ExpReward => preferDefinitionValues && Definition != null ? Definition.ExpReward : expReward > 0 ? expReward : GetResolvedExpReward();
    public int ContactDamage => preferDefinitionValues && Definition != null ? Definition.ContactDamage : contactDamage > 0 ? contactDamage : GetResolvedContactDamage();
    public int AnimatedAttackDamage => preferDefinitionValues && Definition != null ? Definition.AnimatedAttackDamage : animatedAttackDamage > 0 ? animatedAttackDamage : GetResolvedAnimatedAttackDamage();
    public int MinMesoDrop => preferDefinitionValues && Definition != null ? Definition.MinMesoDrop : Mathf.Max(0, minMesoDrop > 0 ? minMesoDrop : GetResolvedMinMesoDrop());
    public int MaxMesoDrop => preferDefinitionValues && Definition != null ? Definition.MaxMesoDrop : Mathf.Max(MinMesoDrop, maxMesoDrop > 0 ? maxMesoDrop : GetResolvedMaxMesoDrop());
    public float MesoDropChance => preferDefinitionValues && Definition != null ? Definition.MesoDropChance : mesoDropChance > 0f ? mesoDropChance : GetResolvedMesoDropChance();
    public int RedPotionDropCount => preferDefinitionValues && Definition != null ? Definition.RedPotionDropCount : Mathf.Max(0, redPotionDropCount > 0 ? redPotionDropCount : GetResolvedRedPotionDropCount());
    public float RedPotionDropChance => preferDefinitionValues && Definition != null ? Definition.RedPotionDropChance : redPotionDropChance > 0f ? redPotionDropChance : GetResolvedRedPotionDropChance();
    public int BluePotionDropCount => preferDefinitionValues && Definition != null ? Definition.BluePotionDropCount : Mathf.Max(0, bluePotionDropCount > 0 ? bluePotionDropCount : GetResolvedBluePotionDropCount());
    public float BluePotionDropChance => preferDefinitionValues && Definition != null ? Definition.BluePotionDropChance : bluePotionDropChance > 0f ? bluePotionDropChance : GetResolvedBluePotionDropChance();
    public string CommonEtcItemId
    {
        get
        {
            if (!preferDefinitionValues && !string.IsNullOrWhiteSpace(legacyCommonEtcItemId))
                return legacyCommonEtcItemId.Trim();

            if (commonEtcItem != null)
                return commonEtcItem.ItemId;

            EnemyDefinition resolvedDefinition = Definition;
            return resolvedDefinition != null ? resolvedDefinition.CommonEtcItemId : string.Empty;
        }
    }
    public int CommonEtcDropCount => preferDefinitionValues && Definition != null ? Definition.CommonEtcDropCount : Mathf.Max(0, commonEtcDropCount > 0 ? commonEtcDropCount : GetResolvedCommonEtcDropCount());
    public float CommonEtcDropChance => preferDefinitionValues && Definition != null ? Definition.CommonEtcDropChance : commonEtcDropChance > 0f ? commonEtcDropChance : GetResolvedCommonEtcDropChance();
    public float CorpseDuration => preferDefinitionValues && Definition != null ? Definition.CorpseDuration : corpseDuration > 0f ? corpseDuration : GetResolvedCorpseDuration();
    public float RespawnDelay => preferDefinitionValues && Definition != null ? Definition.RespawnDelay : respawnDelay > 0f ? respawnDelay : GetResolvedRespawnDelay();

    public void Sanitize()
    {
        if (definition != null)
            enemyType = definition.EnemyType;

        maxHP = Mathf.Max(1, maxHP);
        defense = Mathf.Max(0, defense);
        avoidance = Mathf.Max(0, avoidance);
        expReward = Mathf.Max(0, expReward);

        contactDamage = Mathf.Max(0, contactDamage);
        animatedAttackDamage = Mathf.Max(0, animatedAttackDamage);

        minMesoDrop = Mathf.Max(0, minMesoDrop);
        maxMesoDrop = Mathf.Max(minMesoDrop, maxMesoDrop);
        mesoDropChance = Mathf.Clamp01(mesoDropChance);

        redPotionDropCount = Mathf.Max(0, redPotionDropCount);
        redPotionDropChance = Mathf.Clamp01(redPotionDropChance);
        bluePotionDropCount = Mathf.Max(0, bluePotionDropCount);
        bluePotionDropChance = Mathf.Clamp01(bluePotionDropChance);

        legacyCommonEtcItemId = string.IsNullOrWhiteSpace(legacyCommonEtcItemId) ? string.Empty : legacyCommonEtcItemId.Trim();
        commonEtcDropCount = Mathf.Max(0, commonEtcDropCount);
        commonEtcDropChance = Mathf.Clamp01(commonEtcDropChance);

        corpseDuration = Mathf.Max(0.05f, corpseDuration);
        respawnDelay = Mathf.Max(0.1f, respawnDelay);
    }

    private int GetResolvedExpReward()
    {
        return Definition != null ? Definition.ExpReward : GetDefaultExpReward();
    }

    private int GetResolvedContactDamage()
    {
        return Definition != null ? Definition.ContactDamage : GetDefaultContactDamage();
    }

    private int GetResolvedAnimatedAttackDamage()
    {
        return Definition != null ? Definition.AnimatedAttackDamage : GetDefaultAnimatedAttackDamage();
    }

    private int GetResolvedMinMesoDrop()
    {
        return Definition != null ? Definition.MinMesoDrop : GetDefaultMinMesoDrop();
    }

    private int GetResolvedMaxMesoDrop()
    {
        return Definition != null ? Definition.MaxMesoDrop : GetDefaultMaxMesoDrop();
    }

    private float GetResolvedMesoDropChance()
    {
        return Definition != null ? Definition.MesoDropChance : GetDefaultMesoDropChance();
    }

    private int GetResolvedRedPotionDropCount()
    {
        return Definition != null ? Definition.RedPotionDropCount : GetDefaultRedPotionDropCount();
    }

    private float GetResolvedRedPotionDropChance()
    {
        return Definition != null ? Definition.RedPotionDropChance : GetDefaultRedPotionDropChance();
    }

    private int GetResolvedBluePotionDropCount()
    {
        return Definition != null ? Definition.BluePotionDropCount : GetDefaultBluePotionDropCount();
    }

    private float GetResolvedBluePotionDropChance()
    {
        return Definition != null ? Definition.BluePotionDropChance : GetDefaultBluePotionDropChance();
    }

    private int GetResolvedCommonEtcDropCount()
    {
        return Definition != null ? Definition.CommonEtcDropCount : 1;
    }

    private float GetResolvedCommonEtcDropChance()
    {
        return Definition != null ? Definition.CommonEtcDropChance : GetDefaultCommonEtcDropChance();
    }

    private float GetResolvedCorpseDuration()
    {
        return Definition != null ? Definition.CorpseDuration : 1.25f;
    }

    private float GetResolvedRespawnDelay()
    {
        return Definition != null ? Definition.RespawnDelay : GetDefaultRespawnDelay();
    }

    private int GetDefaultExpReward()
    {
        return enemyType switch
        {
            EnemyType.RedSlime => 8,
            EnemyType.BlueTurtle => 14,
            EnemyType.GreenMushroom => 11,
            _ => 10
        };
    }

    private int GetDefaultContactDamage()
    {
        return enemyType switch
        {
            EnemyType.RedSlime => 4,
            EnemyType.BlueTurtle => 7,
            EnemyType.GreenMushroom => 5,
            _ => 5
        };
    }

    private int GetDefaultAnimatedAttackDamage()
    {
        return enemyType switch
        {
            EnemyType.RedSlime => 8,
            EnemyType.BlueTurtle => 10,
            EnemyType.GreenMushroom => 9,
            _ => 8
        };
    }

    private int GetDefaultMinMesoDrop()
    {
        return enemyType switch
        {
            EnemyType.RedSlime => 4,
            EnemyType.BlueTurtle => 9,
            EnemyType.GreenMushroom => 6,
            _ => 5
        };
    }

    private int GetDefaultMaxMesoDrop()
    {
        return enemyType switch
        {
            EnemyType.RedSlime => 10,
            EnemyType.BlueTurtle => 18,
            EnemyType.GreenMushroom => 14,
            _ => 12
        };
    }

    private float GetDefaultMesoDropChance()
    {
        return enemyType switch
        {
            EnemyType.RedSlime => 0.8f,
            EnemyType.BlueTurtle => 0.9f,
            EnemyType.GreenMushroom => 0.85f,
            _ => 0.8f
        };
    }

    private int GetDefaultRedPotionDropCount()
    {
        return 1;
    }

    private float GetDefaultRedPotionDropChance()
    {
        return enemyType switch
        {
            EnemyType.RedSlime => 0.25f,
            EnemyType.BlueTurtle => 0.18f,
            EnemyType.GreenMushroom => 0.22f,
            _ => 0.2f
        };
    }

    private int GetDefaultBluePotionDropCount()
    {
        return 1;
    }

    private float GetDefaultBluePotionDropChance()
    {
        return enemyType switch
        {
            EnemyType.RedSlime => 0.08f,
            EnemyType.BlueTurtle => 0.16f,
            EnemyType.GreenMushroom => 0.2f,
            _ => 0.12f
        };
    }

    private float GetDefaultCommonEtcDropChance()
    {
        return enemyType switch
        {
            EnemyType.RedSlime => 0.55f,
            EnemyType.BlueTurtle => 0.45f,
            EnemyType.GreenMushroom => 0.4f,
            _ => 0.35f
        };
    }

    private float GetDefaultRespawnDelay()
    {
        return enemyType switch
        {
            EnemyType.RedSlime => 5.5f,
            EnemyType.BlueTurtle => 8f,
            EnemyType.GreenMushroom => 6.5f,
            _ => 6f
        };
    }
}

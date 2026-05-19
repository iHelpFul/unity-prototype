using UnityEngine;

[System.Serializable]
public class EnemyStats
{
    [Header("Identity")]
    [SerializeField] private EnemyType enemyType;
    [SerializeField] private EnemyDefinition definition;

    public EnemyType EnemyType => enemyType;
    public EnemyDefinition Definition => definition != null ? definition : EnemyDefinitionDatabase.GetDefinition(enemyType);

    public EnemyRole Role
    {
        get
        {
            EnemyDefinition resolvedDefinition = Definition;
            return resolvedDefinition != null ? resolvedDefinition.Role : EnemyRole.Skirmisher;
        }
    }

    public EnemyEliteProfile EliteProfile
    {
        get
        {
            EnemyDefinition resolvedDefinition = Definition;
            return resolvedDefinition != null ? resolvedDefinition.EliteProfile : default;
        }
    }

    public EnemyCadenceProfile CadenceProfile
    {
        get
        {
            EnemyDefinition resolvedDefinition = Definition;
            return resolvedDefinition != null ? resolvedDefinition.CadenceProfile : default;
        }
    }

    public EnemyPoiseProfile PoiseProfile
    {
        get
        {
            EnemyDefinition resolvedDefinition = Definition;
            EnemyPoiseProfile poise = resolvedDefinition != null ? resolvedDefinition.PoiseProfile : default;
            EnemyEliteProfile elite = EliteProfile;
            return elite.IsElite ? poise.WithAdjustedThreshold(elite.PoiseThresholdMultiplier) : poise;
        }
    }

    public int MaxHP => ApplyEliteMaxHp(Definition != null ? Definition.MaxHP : 1);
    public int Defense => ApplyEliteDefense(Definition != null ? Definition.Defense : 0);
    public int Avoidance => Definition != null ? Definition.Avoidance : 0;
    public int ExpReward => ApplyEliteExpReward(Definition != null ? Definition.ExpReward : 0);
    public float GaugeReward => Definition != null ? Definition.GaugeReward : 0f;
    public CombatElementType ElementType => Definition != null ? Definition.ElementType : CombatElementType.None;
    public int ContactDamage => ApplyEliteContactDamage(Definition != null ? Definition.ContactDamage : 0);
    public int AnimatedAttackDamage => ApplyEliteAnimatedAttackDamage(Definition != null ? Definition.AnimatedAttackDamage : 0);
    public int HitReactionDamageThreshold => Definition != null ? Definition.HitReactionDamageThreshold : 0;
    public bool EnableHitKnockback => Definition != null && Definition.EnableHitKnockback;
    public float HitKnockbackForce => Definition != null ? Definition.HitKnockbackForce : 0f;
    public float HitKnockbackDuration => Definition != null ? Definition.HitKnockbackDuration : 0f;
    public int HitKnockbackDamageThreshold => Definition != null ? Definition.HitKnockbackDamageThreshold : 999999;
    public float HitReactionMovementLockDuration => Definition != null ? Definition.HitReactionMovementLockDuration : 0.16f;
    public int MinMesoDrop => Definition != null ? Definition.MinMesoDrop : 0;
    public int MaxMesoDrop => Definition != null ? Definition.MaxMesoDrop : Mathf.Max(0, MinMesoDrop);
    public float MesoDropChance => Definition != null ? Definition.MesoDropChance : 0f;
    public int RedPotionDropCount => Definition != null ? Definition.RedPotionDropCount : 0;
    public float RedPotionDropChance => Definition != null ? Definition.RedPotionDropChance : 0f;
    public int BluePotionDropCount => Definition != null ? Definition.BluePotionDropCount : 0;
    public float BluePotionDropChance => Definition != null ? Definition.BluePotionDropChance : 0f;
    public string CommonEtcItemId => Definition != null ? Definition.CommonEtcItemId : string.Empty;
    public int CommonEtcDropCount => Definition != null ? Definition.CommonEtcDropCount : 0;
    public float CommonEtcDropChance => Definition != null ? Definition.CommonEtcDropChance : 0f;
    public float CorpseDuration => Definition != null ? Definition.CorpseDuration : 1.25f;
    public float RespawnDelay => Definition != null ? Definition.RespawnDelay : 6f;

    public void Sanitize()
    {
        if (definition != null)
            enemyType = definition.EnemyType;
    }

    private int ApplyEliteMaxHp(int baseValue)
    {
        EnemyEliteProfile elite = EliteProfile;
        return elite.IsElite
            ? Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(1, baseValue) * elite.MaxHpMultiplier))
            : Mathf.Max(1, baseValue);
    }

    private int ApplyEliteDefense(int baseValue)
    {
        EnemyEliteProfile elite = EliteProfile;
        return elite.IsElite
            ? Mathf.Max(0, baseValue + elite.DefenseBonus)
            : Mathf.Max(0, baseValue);
    }

    private int ApplyEliteContactDamage(int baseValue)
    {
        EnemyEliteProfile elite = EliteProfile;
        return elite.IsElite
            ? Mathf.Max(0, Mathf.RoundToInt(Mathf.Max(0, baseValue) * elite.ContactDamageMultiplier))
            : Mathf.Max(0, baseValue);
    }

    private int ApplyEliteAnimatedAttackDamage(int baseValue)
    {
        EnemyEliteProfile elite = EliteProfile;
        return elite.IsElite
            ? Mathf.Max(0, Mathf.RoundToInt(Mathf.Max(0, baseValue) * elite.AnimatedAttackDamageMultiplier))
            : Mathf.Max(0, baseValue);
    }

    private int ApplyEliteExpReward(int baseValue)
    {
        EnemyEliteProfile elite = EliteProfile;
        return elite.IsElite
            ? Mathf.Max(0, Mathf.RoundToInt(Mathf.Max(0, baseValue) * elite.ExpRewardMultiplier))
            : Mathf.Max(0, baseValue);
    }
}

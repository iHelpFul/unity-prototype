using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public struct CombatHitBoxWorldQuery
{
    public Vector3 Center;
    public Vector3 HalfExtents;
    public Quaternion Rotation;

    public CombatHitBoxWorldQuery(Vector3 center, Vector3 halfExtents, Quaternion rotation)
    {
        Center = center;
        HalfExtents = halfExtents;
        Rotation = rotation;
    }
}

[System.Serializable]
public struct CombatHitBoxDefinition
{
    private const float DefaultForwardRangeScale = 1f;
    private const float DefaultBackwardRangeScale = 0f;
    private const float DefaultUpExtent = 0.9f;
    private const float DefaultDownExtent = 0.35f;
    private const float DefaultDepthExtent = 0.75f;

    [SerializeField] private Vector3 localOffset;
    [SerializeField] private float forwardRangeScale;
    [SerializeField] private float backwardRangeScale;
    [SerializeField] private float upExtent;
    [SerializeField] private float downExtent;
    [SerializeField] private float depthExtent;

    public Vector3 LocalOffset => localOffset;
    public float ForwardRangeScale => forwardRangeScale;
    public float BackwardRangeScale => backwardRangeScale;
    public float UpExtent => upExtent;
    public float DownExtent => downExtent;
    public float DepthExtent => depthExtent;

    public CombatHitBoxDefinition GetSanitized()
    {
        CombatHitBoxDefinition sanitized = this;
        sanitized.SanitizeInPlace();
        return sanitized;
    }

    public CombatHitBoxDefinition GetRadiusScaled(float radiusScale)
    {
        CombatHitBoxDefinition scaled = GetSanitized();
        float clampedScale = Mathf.Max(0.01f, radiusScale);
        scaled.upExtent *= clampedScale;
        scaled.downExtent *= clampedScale;
        scaled.depthExtent *= clampedScale;
        return scaled;
    }

    public float GetForwardExtent(float resolvedRange)
    {
        CombatHitBoxDefinition sanitized = GetSanitized();
        return Mathf.Max(0f, sanitized.forwardRangeScale) * Mathf.Max(0.05f, resolvedRange);
    }

    public float GetBackwardExtent(float resolvedRange)
    {
        CombatHitBoxDefinition sanitized = GetSanitized();
        return Mathf.Max(0f, sanitized.backwardRangeScale) * Mathf.Max(0.05f, resolvedRange);
    }

    public CombatHitBoxWorldQuery BuildWorldQuery(Transform facingTransform, float resolvedRange)
    {
        Transform resolvedTransform = facingTransform != null ? facingTransform : null;
        if (resolvedTransform == null)
            return new CombatHitBoxWorldQuery(Vector3.zero, Vector3.one * 0.05f, Quaternion.identity);

        return BuildWorldQuery(resolvedTransform.position, resolvedTransform.rotation, resolvedRange);
    }

    public CombatHitBoxWorldQuery BuildWorldQuery(Vector3 worldPosition, Quaternion worldRotation, float resolvedRange)
    {
        CombatHitBoxDefinition sanitized = GetSanitized();
        float clampedRange = Mathf.Max(0.05f, resolvedRange);
        float forwardExtentValue = Mathf.Max(0f, sanitized.forwardRangeScale) * clampedRange;
        float backwardExtentValue = Mathf.Max(0f, sanitized.backwardRangeScale) * clampedRange;
        float upExtentValue = Mathf.Max(0.01f, sanitized.upExtent);
        float downExtentValue = Mathf.Max(0.01f, sanitized.downExtent);
        float depthExtentValue = Mathf.Max(0.01f, sanitized.depthExtent);

        Vector3 center = worldPosition + (worldRotation * sanitized.localOffset);
        center += (worldRotation * Vector3.forward) * ((forwardExtentValue - backwardExtentValue) * 0.5f);
        center += (worldRotation * Vector3.up) * ((upExtentValue - downExtentValue) * 0.5f);

        Vector3 halfExtents = new Vector3(
            depthExtentValue,
            (upExtentValue + downExtentValue) * 0.5f,
            Mathf.Max(0.05f, (forwardExtentValue + backwardExtentValue) * 0.5f));

        return new CombatHitBoxWorldQuery(center, halfExtents, worldRotation);
    }

    private void SanitizeInPlace()
    {
        forwardRangeScale = Mathf.Max(0f, forwardRangeScale);
        backwardRangeScale = Mathf.Max(0f, backwardRangeScale);
        upExtent = Mathf.Max(0f, upExtent);
        downExtent = Mathf.Max(0f, downExtent);
        depthExtent = Mathf.Max(0f, depthExtent);

        if (HasConfiguredVolume())
            return;

        forwardRangeScale = DefaultForwardRangeScale;
        backwardRangeScale = DefaultBackwardRangeScale;
        upExtent = DefaultUpExtent;
        downExtent = DefaultDownExtent;
        depthExtent = DefaultDepthExtent;
    }

    private bool HasConfiguredVolume()
    {
        return forwardRangeScale > 0f
            || backwardRangeScale > 0f
            || upExtent > 0f
            || downExtent > 0f
            || depthExtent > 0f;
    }
}

[System.Serializable]
public struct EmpoweredBasicDefinition
{
    [SerializeField] private float windowDuration;
    [SerializeField] private float animationSpeedMultiplier;
    [SerializeField] private float damageMultiplier;
    [SerializeField] private float surgeChanceBonus;
    [SerializeField] private float surgePowerBonus;
    [FormerlySerializedAs("appliedTargetStateType")]
    [SerializeField] private PlayerReadyStateType grantedReadyStateType;
    [FormerlySerializedAs("appliedTargetStateDuration")]
    [SerializeField] private float grantedReadyStateDuration;

    public float WindowDuration => Mathf.Max(0f, windowDuration);
    public float AnimationSpeedMultiplier => Mathf.Max(0.05f, animationSpeedMultiplier);
    public float DamageMultiplier => Mathf.Max(0.05f, damageMultiplier);
    public float SurgeChanceBonus => Mathf.Max(0f, surgeChanceBonus);
    public float SurgePowerBonus => Mathf.Max(0f, surgePowerBonus);
    public PlayerReadyStateType GrantedReadyStateType => grantedReadyStateType;
    public float GrantedReadyStateDuration => Mathf.Max(0f, grantedReadyStateDuration);
    public bool IsConfigured => WindowDuration > 0f;

    public EmpoweredBasicDefinition GetSanitized()
    {
        EmpoweredBasicDefinition sanitized = this;
        sanitized.windowDuration = Mathf.Max(0f, sanitized.windowDuration);
        sanitized.animationSpeedMultiplier = sanitized.animationSpeedMultiplier > 0f
            ? Mathf.Max(0.05f, sanitized.animationSpeedMultiplier)
            : 1f;
        sanitized.damageMultiplier = sanitized.damageMultiplier > 0f
            ? Mathf.Max(0.05f, sanitized.damageMultiplier)
            : 1f;
        sanitized.surgeChanceBonus = Mathf.Max(0f, sanitized.surgeChanceBonus);
        sanitized.surgePowerBonus = Mathf.Max(0f, sanitized.surgePowerBonus);
        sanitized.grantedReadyStateDuration = Mathf.Max(0f, sanitized.grantedReadyStateDuration);

        if (!System.Enum.IsDefined(typeof(PlayerReadyStateType), sanitized.grantedReadyStateType))
            sanitized.grantedReadyStateType = PlayerReadyStateType.None;

        return sanitized;
    }
}

[System.Serializable]
public struct ReadyStateAreaBonusDefinition
{
    [SerializeField] private float range;
    [SerializeField] private CombatHitBoxDefinition hitBox;
    [SerializeField] private float damageMultiplier;
    [SerializeField] private float breakPowerMultiplier;
    [SerializeField] private int pulseCount;
    [SerializeField] private float pulseInterval;
    [SerializeField] private int maxTargets;
    [SerializeField] private bool includePrimaryTarget;

    public float Range => Mathf.Max(0f, range);
    public CombatHitBoxDefinition HitBox => hitBox.GetSanitized();
    public float DamageMultiplier => Mathf.Max(0f, damageMultiplier);
    public float BreakPowerMultiplier => Mathf.Max(0f, breakPowerMultiplier);
    public int PulseCount => Mathf.Max(1, pulseCount);
    public float PulseInterval => Mathf.Max(0f, pulseInterval);
    public int MaxTargets => Mathf.Max(1, maxTargets);
    public bool IncludePrimaryTarget => includePrimaryTarget;
    public bool IsConfigured => Range > 0f && DamageMultiplier > 0f && MaxTargets > 0;

    public ReadyStateAreaBonusDefinition GetSanitized()
    {
        ReadyStateAreaBonusDefinition sanitized = this;
        sanitized.range = Mathf.Max(0f, sanitized.range);
        sanitized.hitBox = sanitized.hitBox.GetSanitized();
        sanitized.damageMultiplier = Mathf.Max(0f, sanitized.damageMultiplier);
        sanitized.breakPowerMultiplier = Mathf.Max(0f, sanitized.breakPowerMultiplier);
        sanitized.pulseCount = Mathf.Max(1, sanitized.pulseCount);
        sanitized.pulseInterval = Mathf.Max(0f, sanitized.pulseInterval);
        sanitized.maxTargets = Mathf.Max(1, sanitized.maxTargets);
        return sanitized;
    }
}

[System.Serializable]
public struct ReadyStateEmpowerDefinition
{
    [FormerlySerializedAs("requiredStateType")]
    [SerializeField] private PlayerReadyStateType requiredReadyStateType;
    [FormerlySerializedAs("consumeStateOnSuccessfulHit")]
    [SerializeField] private bool consumeReadyStateOnSuccessfulHit;
    [SerializeField] private float damageMultiplier;
    [SerializeField] private float breakPowerMultiplier;
    [SerializeField] private float surgeChanceBonus;
    [SerializeField] private float surgePowerBonus;
    [FormerlySerializedAs("bonusAreaOnTrigger")]
    [SerializeField] private ReadyStateAreaBonusDefinition bonusAreaOnSuccessfulHit;

    public PlayerReadyStateType RequiredReadyStateType => requiredReadyStateType;
    public bool ConsumeReadyStateOnSuccessfulHit => consumeReadyStateOnSuccessfulHit;
    public float DamageMultiplier => damageMultiplier > 0f ? Mathf.Max(0.05f, damageMultiplier) : 1f;
    public float BreakPowerMultiplier => breakPowerMultiplier > 0f ? Mathf.Max(0f, breakPowerMultiplier) : 1f;
    public float SurgeChanceBonus => Mathf.Max(0f, surgeChanceBonus);
    public float SurgePowerBonus => Mathf.Max(0f, surgePowerBonus);
    public ReadyStateAreaBonusDefinition BonusAreaOnSuccessfulHit => bonusAreaOnSuccessfulHit.GetSanitized();
    public bool IsConfigured => requiredReadyStateType != PlayerReadyStateType.None;

    public ReadyStateEmpowerDefinition GetSanitized()
    {
        ReadyStateEmpowerDefinition sanitized = this;
        if (!System.Enum.IsDefined(typeof(PlayerReadyStateType), sanitized.requiredReadyStateType))
            sanitized.requiredReadyStateType = PlayerReadyStateType.None;

        sanitized.damageMultiplier = sanitized.damageMultiplier > 0f
            ? Mathf.Max(0.05f, sanitized.damageMultiplier)
            : 1f;
        sanitized.breakPowerMultiplier = sanitized.breakPowerMultiplier > 0f
            ? Mathf.Max(0f, sanitized.breakPowerMultiplier)
            : 1f;
        sanitized.surgeChanceBonus = Mathf.Max(0f, sanitized.surgeChanceBonus);
        sanitized.surgePowerBonus = Mathf.Max(0f, sanitized.surgePowerBonus);
        sanitized.bonusAreaOnSuccessfulHit = sanitized.bonusAreaOnSuccessfulHit.GetSanitized();
        return sanitized;
    }
}

public enum CombatAttackFamily
{
    None = 0,
    NoviceCore = 1,
    ShadeBasic = 2,
    ShadeShuriken = 3,
    ShadeSignature = 4,
    ArcanistCore = 5,
    VanguardHeavy = 6,
    VanguardBasic = 7,
    VanguardSignature = 8
}

public enum CombatExecutionKind
{
    None = 0,
    Direct = 1,
    Projectile = 2,
    Utility = 3
}

public enum CombatTargetingKind
{
    None = 0,
    Self = 1,
    SingleTarget = 2,
    Area = 3
}

public enum ProjectileLaunchMode
{
    Locked = 0,
    Free = 1
}

public enum CombatControlStateType
{
    None = 0,
    Idle = 1,
    Moving = 2,
    Attacking = 3,
    Casting = 4,
    Charging = 5,
    TalkingToNpc = 6,
    Stunned = 7,
    Dead = 8
}

public enum CombatCuePhase
{
    None = 0,
    CastStart = 1,
    ChargeStart = 2,
    ChargeLoop = 3,
    Release = 4,
    ProjectileSpawn = 5,
    TravelLoop = 6,
    HitImpact = 7,
    MissImpact = 8,
    Finish = 9
}

public enum CombatSpawnTargetKind
{
    None = 0,
    Self = 1,
    Weapon = 2,
    Projectile = 3,
    Target = 4,
    WorldPoint = 5
}

public enum CombatElementType
{
    None = 0,
    Fire = 1,
    Ice = 2,
    Air = 3,
    Poison = 4
}

public enum PlayerReadyStateType
{
    None = 0,
    Breach = 1,
    Mark = 2
}

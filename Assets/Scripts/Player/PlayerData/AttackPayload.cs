using UnityEngine;

public enum AttackPayloadActionKind
{
    None = 0,
    BasicAttack = 1,
    Skill = 2,
    ProjectileImpact = 3,
    PassiveProc = 4,
    BurstLinkedSkill = 5
}

[System.Serializable]
public sealed class AttackPayload
{
    [SerializeField] private string payloadId = string.Empty;
    [SerializeField] private AttackPayloadActionKind actionKind = AttackPayloadActionKind.None;
    [SerializeField] private string actionId = string.Empty;
    [SerializeField] private CombatAttackFamily attackFamily = CombatAttackFamily.None;
    [SerializeField] private CombatExecutionKind executionKind = CombatExecutionKind.None;
    [SerializeField] private PlayerJobType sourceJobType = PlayerJobType.Novice;
    [SerializeField] private int sourceLevel = 1;
    [SerializeField] private Vector3 sourcePositionAtRelease;
    [SerializeField] private int skillLevel = 1;

    [Header("Resolved Combat Values")]
    [SerializeField] private int resolvedMinDamage = 1;
    [SerializeField] private int resolvedMaxDamage = 1;
    [SerializeField] private float resolvedDamageCoefficient = 1f;
    [SerializeField] private int resolvedWeaponPower;
    [SerializeField] private int resolvedHitRate;
    [SerializeField] private int resolvedHitCount = 1;
    [SerializeField] private int resolvedProjectileCount = 1;
    [SerializeField] private float resolvedRange = 1f;
    [SerializeField] private CombatHitBoxDefinition resolvedHitBox;
    [SerializeField] private float resolvedBreakPower;

    [Header("Gauge / Release Context")]
    [SerializeField] private float gaugeSpentAtRelease;
    [SerializeField] private float gaugeSpendNormalized;
    [SerializeField] private int flowStacksAtRelease;
    [SerializeField] private float holdDurationAtRelease;
    [SerializeField] private bool aerialRelease;

    [Header("Momentum / States")]
    [SerializeField] private int momentumBefore;
    [SerializeField] private int momentumToConsume;
    [SerializeField] private PlayerReadyStateType pendingReadyStateType = PlayerReadyStateType.None;
    [SerializeField] private ReadyStateEmpowerDefinition readyStateEmpower;
    [SerializeField] private bool readyStateEmpowerActivated;

    [Header("Surge")]
    [SerializeField] private float baseSurgeChance;
    [SerializeField] private float contextSurgeChanceBonus;
    [SerializeField] private float skillSurgeChanceBonus;
    [SerializeField] private float finalSurgeChance;
    [SerializeField] private float baseSurgePower = 1f;
    [SerializeField] private float contextSurgePowerBonus;
    [SerializeField] private float skillSurgePowerBonus;
    [SerializeField] private float finalSurgePower = 1f;

    [Header("Element")]
    [SerializeField] private bool hasElement;
    [SerializeField] private CombatElementType elementType = CombatElementType.None;
    [SerializeField] private float elementPower;

    [Header("Result Rules")]
    [SerializeField] private bool canMiss = true;
    [SerializeField] private bool canSurge = true;
    [SerializeField] private bool canApplyStates = true;
    [SerializeField] private bool canTriggerOnHitEffects = true;
    [SerializeField] private bool canTriggerOnKillEffects = true;
    [SerializeField] private int maxTargets = 1;
    [SerializeField] private bool stopOnFirstValidHit = true;

    public string PayloadId => payloadId;
    public AttackPayloadActionKind ActionKind => actionKind;
    public string ActionId => actionId;
    public CombatAttackFamily AttackFamily => attackFamily;
    public CombatExecutionKind ExecutionKind => executionKind;
    public PlayerJobType SourceJobType => sourceJobType;
    public int SourceLevel => sourceLevel;
    public Vector3 SourcePositionAtRelease => sourcePositionAtRelease;
    public int SkillLevel => skillLevel;
    public int ResolvedMinDamage => resolvedMinDamage;
    public int ResolvedMaxDamage => resolvedMaxDamage;
    public float ResolvedDamageCoefficient => resolvedDamageCoefficient;
    public int ResolvedWeaponPower => resolvedWeaponPower;
    public int ResolvedHitRate => resolvedHitRate;
    public int ResolvedHitCount => resolvedHitCount;
    public int ResolvedProjectileCount => resolvedProjectileCount;
    public float ResolvedRange => resolvedRange;
    public CombatHitBoxDefinition ResolvedHitBox => resolvedHitBox.GetSanitized();
    public float ResolvedBreakPower => resolvedBreakPower;
    public float GaugeSpentAtRelease => gaugeSpentAtRelease;
    public float GaugeSpendNormalized => gaugeSpendNormalized;
    public int FlowStacksAtRelease => flowStacksAtRelease;
    public float HoldDurationAtRelease => holdDurationAtRelease;
    public bool AerialRelease => aerialRelease;
    public int MomentumBefore => momentumBefore;
    public int MomentumToConsume => momentumToConsume;
    public PlayerReadyStateType PendingReadyStateType => pendingReadyStateType;
    public ReadyStateEmpowerDefinition ReadyStateEmpower => readyStateEmpower.GetSanitized();
    public bool IsReadyStateEmpowerActive => readyStateEmpowerActivated && readyStateEmpower.GetSanitized().IsConfigured;
    public bool CanAttemptReadyStateEmpower => !IsReadyStateEmpowerActive
        && PendingReadyStateType != PlayerReadyStateType.None
        && ReadyStateEmpower.IsConfigured;
    public float BaseSurgeChance => baseSurgeChance;
    public float ContextSurgeChanceBonus => contextSurgeChanceBonus;
    public float SkillSurgeChanceBonus => skillSurgeChanceBonus;
    public float FinalSurgeChance => finalSurgeChance;
    public float BaseSurgePower => baseSurgePower;
    public float ContextSurgePowerBonus => contextSurgePowerBonus;
    public float SkillSurgePowerBonus => skillSurgePowerBonus;
    public float FinalSurgePower => finalSurgePower;
    public bool HasElement => hasElement;
    public CombatElementType ElementType => elementType;
    public float ElementPower => elementPower;
    public bool CanMiss => canMiss;
    public bool CanSurge => canSurge;
    public bool CanApplyStates => canApplyStates;
    public bool CanTriggerOnHitEffects => canTriggerOnHitEffects;
    public bool CanTriggerOnKillEffects => canTriggerOnKillEffects;
    public int MaxTargets => maxTargets;
    public bool StopOnFirstValidHit => stopOnFirstValidHit;

    public void Initialize(
        string newPayloadId,
        AttackPayloadActionKind newActionKind,
        string newActionId,
        CombatAttackFamily newAttackFamily,
        CombatExecutionKind newExecutionKind,
        PlayerJobType newSourceJobType,
        int newSourceLevel,
        Vector3 newSourcePositionAtRelease,
        int newSkillLevel,
        int newResolvedMinDamage,
        int newResolvedMaxDamage,
        float newResolvedDamageCoefficient,
        int newResolvedWeaponPower,
        int newResolvedHitRate,
        int newResolvedHitCount,
        int newResolvedProjectileCount,
        float newResolvedRange,
        CombatHitBoxDefinition newResolvedHitBox = default,
        float newResolvedBreakPower = 0f,
        float newGaugeSpentAtRelease = 0f,
        float newGaugeSpendNormalized = 0f,
        int newFlowStacksAtRelease = 0,
        float newHoldDurationAtRelease = 0f,
        bool newAerialRelease = false,
        int newMomentumBefore = 0,
        int newMomentumToConsume = 0,
        PlayerReadyStateType newPendingReadyStateType = PlayerReadyStateType.None,
        ReadyStateEmpowerDefinition newReadyStateEmpower = default,
        bool newReadyStateEmpowerActivated = false,
        float newBaseSurgeChance = 0f,
        float newContextSurgeChanceBonus = 0f,
        float newSkillSurgeChanceBonus = 0f,
        float newFinalSurgeChance = 0f,
        float newBaseSurgePower = 1f,
        float newContextSurgePowerBonus = 0f,
        float newSkillSurgePowerBonus = 0f,
        float newFinalSurgePower = 1f,
        bool newHasElement = false,
        CombatElementType newElementType = CombatElementType.None,
        float newElementPower = 0f,
        bool newCanMiss = true,
        bool newCanSurge = true,
        bool newCanApplyStates = true,
        bool newCanTriggerOnHitEffects = true,
        bool newCanTriggerOnKillEffects = true,
        int newMaxTargets = 1,
        bool newStopOnFirstValidHit = true)
    {
        payloadId = newPayloadId;
        actionKind = newActionKind;
        actionId = newActionId;
        attackFamily = newAttackFamily;
        executionKind = newExecutionKind;
        sourceJobType = newSourceJobType;
        sourceLevel = newSourceLevel;
        sourcePositionAtRelease = newSourcePositionAtRelease;
        skillLevel = newSkillLevel;
        resolvedMinDamage = newResolvedMinDamage;
        resolvedMaxDamage = newResolvedMaxDamage;
        resolvedDamageCoefficient = newResolvedDamageCoefficient;
        resolvedWeaponPower = newResolvedWeaponPower;
        resolvedHitRate = newResolvedHitRate;
        resolvedHitCount = newResolvedHitCount;
        resolvedProjectileCount = newResolvedProjectileCount;
        resolvedRange = newResolvedRange;
        resolvedHitBox = newResolvedHitBox.GetSanitized();
        resolvedBreakPower = newResolvedBreakPower;
        gaugeSpentAtRelease = newGaugeSpentAtRelease;
        gaugeSpendNormalized = newGaugeSpendNormalized;
        flowStacksAtRelease = newFlowStacksAtRelease;
        holdDurationAtRelease = newHoldDurationAtRelease;
        aerialRelease = newAerialRelease;
        momentumBefore = newMomentumBefore;
        momentumToConsume = newMomentumToConsume;
        pendingReadyStateType = newPendingReadyStateType;
        readyStateEmpower = newReadyStateEmpower.GetSanitized();
        readyStateEmpowerActivated = newReadyStateEmpowerActivated;
        baseSurgeChance = newBaseSurgeChance;
        contextSurgeChanceBonus = newContextSurgeChanceBonus;
        skillSurgeChanceBonus = newSkillSurgeChanceBonus;
        finalSurgeChance = newFinalSurgeChance;
        baseSurgePower = newBaseSurgePower;
        contextSurgePowerBonus = newContextSurgePowerBonus;
        skillSurgePowerBonus = newSkillSurgePowerBonus;
        finalSurgePower = newFinalSurgePower;
        hasElement = newHasElement;
        elementType = newElementType;
        elementPower = newElementPower;
        canMiss = newCanMiss;
        canSurge = newCanSurge;
        canApplyStates = newCanApplyStates;
        canTriggerOnHitEffects = newCanTriggerOnHitEffects;
        canTriggerOnKillEffects = newCanTriggerOnKillEffects;
        maxTargets = newMaxTargets;
        stopOnFirstValidHit = newStopOnFirstValidHit;
        Sanitize();
    }

    public static AttackPayload CreateTransient(
        string newPayloadId,
        AttackPayloadActionKind newActionKind,
        string newActionId,
        CombatAttackFamily newAttackFamily,
        CombatExecutionKind newExecutionKind,
        PlayerJobType newSourceJobType,
        int newSourceLevel,
        Vector3 newSourcePositionAtRelease,
        int newSkillLevel,
        int newResolvedMinDamage,
        int newResolvedMaxDamage,
        float newResolvedDamageCoefficient,
        int newResolvedWeaponPower,
        int newResolvedHitRate,
        int newResolvedHitCount,
        int newResolvedProjectileCount,
        float newResolvedRange,
        CombatHitBoxDefinition newResolvedHitBox = default,
        float newResolvedBreakPower = 0f,
        float newGaugeSpentAtRelease = 0f,
        float newGaugeSpendNormalized = 0f,
        int newFlowStacksAtRelease = 0,
        float newHoldDurationAtRelease = 0f,
        bool newAerialRelease = false,
        int newMomentumBefore = 0,
        int newMomentumToConsume = 0,
        PlayerReadyStateType newPendingReadyStateType = PlayerReadyStateType.None,
        ReadyStateEmpowerDefinition newReadyStateEmpower = default,
        bool newReadyStateEmpowerActivated = false,
        float newBaseSurgeChance = 0f,
        float newContextSurgeChanceBonus = 0f,
        float newSkillSurgeChanceBonus = 0f,
        float newFinalSurgeChance = 0f,
        float newBaseSurgePower = 1f,
        float newContextSurgePowerBonus = 0f,
        float newSkillSurgePowerBonus = 0f,
        float newFinalSurgePower = 1f,
        bool newHasElement = false,
        CombatElementType newElementType = CombatElementType.None,
        float newElementPower = 0f,
        bool newCanMiss = true,
        bool newCanSurge = true,
        bool newCanApplyStates = true,
        bool newCanTriggerOnHitEffects = true,
        bool newCanTriggerOnKillEffects = true,
        int newMaxTargets = 1,
        bool newStopOnFirstValidHit = true)
    {
        AttackPayload payload = new AttackPayload();
        payload.Initialize(
            newPayloadId,
            newActionKind,
            newActionId,
            newAttackFamily,
            newExecutionKind,
            newSourceJobType,
            newSourceLevel,
            newSourcePositionAtRelease,
            newSkillLevel,
            newResolvedMinDamage,
            newResolvedMaxDamage,
            newResolvedDamageCoefficient,
            newResolvedWeaponPower,
            newResolvedHitRate,
            newResolvedHitCount,
            newResolvedProjectileCount,
            newResolvedRange,
            newResolvedHitBox,
            newResolvedBreakPower,
            newGaugeSpentAtRelease,
            newGaugeSpendNormalized,
            newFlowStacksAtRelease,
            newHoldDurationAtRelease,
            newAerialRelease,
            newMomentumBefore,
            newMomentumToConsume,
            newPendingReadyStateType,
            newReadyStateEmpower,
            newReadyStateEmpowerActivated,
            newBaseSurgeChance,
            newContextSurgeChanceBonus,
            newSkillSurgeChanceBonus,
            newFinalSurgeChance,
            newBaseSurgePower,
            newContextSurgePowerBonus,
            newSkillSurgePowerBonus,
            newFinalSurgePower,
            newHasElement,
            newElementType,
            newElementPower,
            newCanMiss,
            newCanSurge,
            newCanApplyStates,
            newCanTriggerOnHitEffects,
            newCanTriggerOnKillEffects,
            newMaxTargets,
            newStopOnFirstValidHit);
        return payload;
    }

    public void Sanitize()
    {
        payloadId = string.IsNullOrWhiteSpace(payloadId) ? string.Empty : payloadId.Trim();
        actionId = string.IsNullOrWhiteSpace(actionId) ? string.Empty : actionId.Trim();
        sourceLevel = Mathf.Max(1, sourceLevel);
        skillLevel = Mathf.Max(1, skillLevel);
        resolvedMinDamage = Mathf.Max(1, resolvedMinDamage);
        resolvedMaxDamage = Mathf.Max(resolvedMinDamage, resolvedMaxDamage);
        resolvedDamageCoefficient = Mathf.Max(0f, resolvedDamageCoefficient);
        resolvedWeaponPower = Mathf.Max(0, resolvedWeaponPower);
        resolvedHitRate = Mathf.Max(0, resolvedHitRate);
        resolvedHitCount = Mathf.Max(1, resolvedHitCount);
        resolvedProjectileCount = Mathf.Max(1, resolvedProjectileCount);
        resolvedRange = Mathf.Max(0f, resolvedRange);
        resolvedHitBox = resolvedHitBox.GetSanitized();
        resolvedBreakPower = Mathf.Max(0f, resolvedBreakPower);
        gaugeSpentAtRelease = Mathf.Max(0f, gaugeSpentAtRelease);
        gaugeSpendNormalized = Mathf.Clamp01(gaugeSpendNormalized);
        flowStacksAtRelease = Mathf.Max(0, flowStacksAtRelease);
        holdDurationAtRelease = Mathf.Max(0f, holdDurationAtRelease);
        momentumBefore = Mathf.Max(0, momentumBefore);
        momentumToConsume = Mathf.Max(0, momentumToConsume);
        if (!System.Enum.IsDefined(typeof(PlayerReadyStateType), pendingReadyStateType))
            pendingReadyStateType = PlayerReadyStateType.None;
        readyStateEmpower = readyStateEmpower.GetSanitized();
        readyStateEmpowerActivated = readyStateEmpowerActivated && readyStateEmpower.IsConfigured;
        baseSurgeChance = Mathf.Max(0f, baseSurgeChance);
        contextSurgeChanceBonus = Mathf.Max(0f, contextSurgeChanceBonus);
        skillSurgeChanceBonus = Mathf.Max(0f, skillSurgeChanceBonus);
        finalSurgeChance = Mathf.Clamp(finalSurgeChance, 0f, 100f);
        baseSurgePower = Mathf.Max(0f, baseSurgePower);
        contextSurgePowerBonus = Mathf.Max(0f, contextSurgePowerBonus);
        skillSurgePowerBonus = Mathf.Max(0f, skillSurgePowerBonus);
        finalSurgePower = Mathf.Max(0f, finalSurgePower);
        elementPower = Mathf.Max(0f, elementPower);
        maxTargets = Mathf.Max(1, maxTargets);
    }

    public bool TryActivateReadyStateEmpower()
    {
        if (!CanAttemptReadyStateEmpower)
            return false;

        readyStateEmpowerActivated = true;
        return true;
    }

    public AttackPayload CreateTriggeredAreaBonusPayload(ReadyStateAreaBonusDefinition areaBonus)
    {
        ReadyStateAreaBonusDefinition sanitizedAreaBonus = areaBonus.GetSanitized();
        if (!sanitizedAreaBonus.IsConfigured)
            return null;

        int scaledMinDamage = Mathf.Max(1, Mathf.RoundToInt(ResolvedMinDamage * sanitizedAreaBonus.DamageMultiplier));
        int scaledMaxDamage = Mathf.Max(scaledMinDamage, Mathf.RoundToInt(ResolvedMaxDamage * sanitizedAreaBonus.DamageMultiplier));
        float scaledDamageCoefficient = Mathf.Max(0.05f, ResolvedDamageCoefficient * sanitizedAreaBonus.DamageMultiplier);
        float scaledBreakPower = Mathf.Max(0f, ResolvedBreakPower * sanitizedAreaBonus.BreakPowerMultiplier);

        return CreateTransient(
            newPayloadId: $"{PayloadId}_state_bonus_{System.Guid.NewGuid():N}",
            newActionKind: AttackPayloadActionKind.PassiveProc,
            newActionId: $"{ActionId}_state_bonus",
            newAttackFamily: AttackFamily,
            newExecutionKind: CombatExecutionKind.Direct,
            newSourceJobType: SourceJobType,
            newSourceLevel: SourceLevel,
            newSourcePositionAtRelease: SourcePositionAtRelease,
            newSkillLevel: SkillLevel,
            newResolvedMinDamage: scaledMinDamage,
            newResolvedMaxDamage: scaledMaxDamage,
            newResolvedDamageCoefficient: scaledDamageCoefficient,
            newResolvedWeaponPower: ResolvedWeaponPower,
            newResolvedHitRate: ResolvedHitRate,
            newResolvedHitCount: 1,
            newResolvedProjectileCount: 1,
            newResolvedRange: sanitizedAreaBonus.Range,
            newResolvedHitBox: sanitizedAreaBonus.HitBox,
            newResolvedBreakPower: scaledBreakPower,
            newGaugeSpentAtRelease: 0f,
            newGaugeSpendNormalized: 0f,
            newFlowStacksAtRelease: FlowStacksAtRelease,
            newHoldDurationAtRelease: 0f,
            newAerialRelease: false,
            newMomentumBefore: MomentumBefore,
            newMomentumToConsume: 0,
            newPendingReadyStateType: PlayerReadyStateType.None,
            newReadyStateEmpower: default,
            newReadyStateEmpowerActivated: false,
            newBaseSurgeChance: BaseSurgeChance,
            newContextSurgeChanceBonus: ContextSurgeChanceBonus,
            newSkillSurgeChanceBonus: SkillSurgeChanceBonus,
            newFinalSurgeChance: FinalSurgeChance,
            newBaseSurgePower: BaseSurgePower,
            newContextSurgePowerBonus: ContextSurgePowerBonus,
            newSkillSurgePowerBonus: SkillSurgePowerBonus,
            newFinalSurgePower: FinalSurgePower,
            newHasElement: HasElement,
            newElementType: ElementType,
            newElementPower: ElementPower,
            newCanMiss: CanMiss,
            newCanSurge: CanSurge,
            newCanApplyStates: false,
            newCanTriggerOnHitEffects: CanTriggerOnHitEffects,
            newCanTriggerOnKillEffects: CanTriggerOnKillEffects,
            newMaxTargets: sanitizedAreaBonus.MaxTargets,
            newStopOnFirstValidHit: false);
    }
}

using System.Collections.Generic;
using UnityEngine;

public enum AttackPayloadActionKind
{
    None = 0,
    BasicAttack = 1,
    Skill = 2,
    ProjectileImpact = 3,
    PassiveProc = 4
}

[System.Serializable]
public sealed class AttackPayload
{
    [SerializeField] private string payloadId = string.Empty;
    [SerializeField] private AttackPayloadActionKind actionKind = AttackPayloadActionKind.None;
    [SerializeField] private string actionId = string.Empty;
    [SerializeField] private CombatAttackFamily attackFamily = CombatAttackFamily.None;
    [SerializeField] private CombatExecutionKind executionKind = CombatExecutionKind.None;
    [SerializeField] private PlayerJobType sourceJobType = PlayerJobType.Drifter;
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
    [SerializeField] private float resolvedRange = 1f;

    [Header("Momentum / States")]
    [SerializeField] private int momentumBefore;
    [SerializeField] private int momentumToConsume;
    [SerializeField] private string[] requiredSelfStateIds = new string[0];
    [SerializeField] private string[] requiredTargetStateIds = new string[0];
    [SerializeField] private string[] consumedSelfStateIds = new string[0];
    [SerializeField] private string[] consumedTargetStateIds = new string[0];
    [SerializeField] private string[] appliedSelfStateIds = new string[0];
    [SerializeField] private string[] appliedTargetStateIds = new string[0];

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
    public float ResolvedRange => resolvedRange;
    public int MomentumBefore => momentumBefore;
    public int MomentumToConsume => momentumToConsume;
    public IReadOnlyList<string> RequiredSelfStateIds => requiredSelfStateIds;
    public IReadOnlyList<string> RequiredTargetStateIds => requiredTargetStateIds;
    public IReadOnlyList<string> ConsumedSelfStateIds => consumedSelfStateIds;
    public IReadOnlyList<string> ConsumedTargetStateIds => consumedTargetStateIds;
    public IReadOnlyList<string> AppliedSelfStateIds => appliedSelfStateIds;
    public IReadOnlyList<string> AppliedTargetStateIds => appliedTargetStateIds;
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
        float newResolvedRange,
        int newMomentumBefore = 0,
        int newMomentumToConsume = 0,
        IEnumerable<string> newRequiredSelfStateIds = null,
        IEnumerable<string> newRequiredTargetStateIds = null,
        IEnumerable<string> newConsumedSelfStateIds = null,
        IEnumerable<string> newConsumedTargetStateIds = null,
        IEnumerable<string> newAppliedSelfStateIds = null,
        IEnumerable<string> newAppliedTargetStateIds = null,
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
        resolvedRange = newResolvedRange;
        momentumBefore = newMomentumBefore;
        momentumToConsume = newMomentumToConsume;
        requiredSelfStateIds = ToStateArray(newRequiredSelfStateIds);
        requiredTargetStateIds = ToStateArray(newRequiredTargetStateIds);
        consumedSelfStateIds = ToStateArray(newConsumedSelfStateIds);
        consumedTargetStateIds = ToStateArray(newConsumedTargetStateIds);
        appliedSelfStateIds = ToStateArray(newAppliedSelfStateIds);
        appliedTargetStateIds = ToStateArray(newAppliedTargetStateIds);
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
        float newResolvedRange,
        int newMomentumBefore = 0,
        int newMomentumToConsume = 0,
        IEnumerable<string> newRequiredSelfStateIds = null,
        IEnumerable<string> newRequiredTargetStateIds = null,
        IEnumerable<string> newConsumedSelfStateIds = null,
        IEnumerable<string> newConsumedTargetStateIds = null,
        IEnumerable<string> newAppliedSelfStateIds = null,
        IEnumerable<string> newAppliedTargetStateIds = null,
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
            newResolvedRange,
            newMomentumBefore,
            newMomentumToConsume,
            newRequiredSelfStateIds,
            newRequiredTargetStateIds,
            newConsumedSelfStateIds,
            newConsumedTargetStateIds,
            newAppliedSelfStateIds,
            newAppliedTargetStateIds,
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
        resolvedRange = Mathf.Max(0f, resolvedRange);
        momentumBefore = Mathf.Max(0, momentumBefore);
        momentumToConsume = Mathf.Max(0, momentumToConsume);
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
        requiredSelfStateIds = NormalizeStateIds(requiredSelfStateIds);
        requiredTargetStateIds = NormalizeStateIds(requiredTargetStateIds);
        consumedSelfStateIds = NormalizeStateIds(consumedSelfStateIds);
        consumedTargetStateIds = NormalizeStateIds(consumedTargetStateIds);
        appliedSelfStateIds = NormalizeStateIds(appliedSelfStateIds);
        appliedTargetStateIds = NormalizeStateIds(appliedTargetStateIds);
    }

    private static string[] NormalizeStateIds(string[] stateIds)
    {
        if (stateIds == null || stateIds.Length == 0)
            return new string[0];

        List<string> normalized = new List<string>();
        HashSet<string> uniqueIds = new HashSet<string>();

        for (int index = 0; index < stateIds.Length; index++)
        {
            string stateId = string.IsNullOrWhiteSpace(stateIds[index]) ? string.Empty : stateIds[index].Trim();
            if (string.IsNullOrWhiteSpace(stateId))
                continue;

            if (!uniqueIds.Add(stateId))
                continue;

            normalized.Add(stateId);
        }

        return normalized.ToArray();
    }

    private static string[] ToStateArray(IEnumerable<string> stateIds)
    {
        if (stateIds == null)
            return new string[0];

        List<string> collectedIds = new List<string>();
        foreach (string stateId in stateIds)
            collectedIds.Add(stateId);

        return collectedIds.ToArray();
    }
}

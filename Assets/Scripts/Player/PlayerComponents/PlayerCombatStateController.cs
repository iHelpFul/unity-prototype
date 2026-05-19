using UnityEngine;

public class PlayerCombatStateController : MonoBehaviour
{
    [Header("Combat Runtime")]
    [SerializeField] private int maxMomentumStacks = 3;
    [SerializeField] private float fallbackMaxGauge = 10f;

    private PlayerCharacter ownerCharacter;
    private int momentumStacks;
    private float maxGauge;
    private float currentGauge;
    private int currentFlowStacks;
    private float currentFlowStackTimer;
    private float currentFlowWindowDuration;
    private float cachedFirstFlowWindowDuration = 3f;
    private float cachedChainedFlowWindowDuration = 2f;
    private bool resetAllFlowStacksOnTimeout = true;
    private float currentHoldChargeTime;
    private float lockedGaugeSpend;
    private EmpoweredBasicDefinition activeEmpoweredBasic;
    private float empoweredBasicTimer;
    private float empoweredBasicWindowDuration;
    private PlayerReadyStateType activeReadyStateType;
    private float readyStateTimer;
    private float readyStateWindowDuration;

    public int CurrentMomentumStacks => Mathf.Clamp(momentumStacks, 0, Mathf.Max(0, maxMomentumStacks));
    public float MaxGauge => Mathf.Max(0f, maxGauge);
    public float CurrentGauge => Mathf.Clamp(currentGauge, 0f, MaxGauge);
    public float GaugeNormalized => MaxGauge > 0f ? Mathf.Clamp01(CurrentGauge / MaxGauge) : 0f;
    public int CurrentFlowStacks => Mathf.Clamp(currentFlowStacks, 0, int.MaxValue);
    public float CurrentFlowStackTimer => Mathf.Max(0f, currentFlowStackTimer);
    public float CurrentFlowWindowDuration => Mathf.Max(0f, currentFlowWindowDuration);
    public bool HasActiveFlowWindow => CurrentFlowStacks > 0 && CurrentFlowStackTimer > 0f;
    public float CurrentHoldChargeTime => Mathf.Max(0f, currentHoldChargeTime);
    public float LockedGaugeSpend => Mathf.Clamp(lockedGaugeSpend, 0f, CurrentGauge);
    public bool HasLockedGaugeSpend => LockedGaugeSpend > 0f;
    public bool HasEmpoweredBasicReady => activeEmpoweredBasic.IsConfigured && empoweredBasicTimer > 0f;
    public float EmpoweredBasicTimer => Mathf.Max(0f, empoweredBasicTimer);
    public float EmpoweredBasicWindowDuration => Mathf.Max(0f, empoweredBasicWindowDuration);
    public EmpoweredBasicDefinition ActiveEmpoweredBasic => HasEmpoweredBasicReady
        ? activeEmpoweredBasic.GetSanitized()
        : default;
    public bool HasReadyStateActive => activeReadyStateType != PlayerReadyStateType.None && readyStateTimer > 0f;
    public PlayerReadyStateType ActiveReadyStateType => HasReadyStateActive ? activeReadyStateType : PlayerReadyStateType.None;
    public float ReadyStateTimer => Mathf.Max(0f, readyStateTimer);
    public float ReadyStateWindowDuration => Mathf.Max(0f, readyStateWindowDuration);

    public void Initialize(PlayerCharacter ownerCharacter)
    {
        this.ownerCharacter = ownerCharacter;
        momentumStacks = 0;
        currentFlowStacks = 0;
        currentFlowStackTimer = 0f;
        currentFlowWindowDuration = 0f;
        currentHoldChargeTime = 0f;
        lockedGaugeSpend = 0f;
        activeEmpoweredBasic = default;
        empoweredBasicTimer = 0f;
        empoweredBasicWindowDuration = 0f;
        activeReadyStateType = PlayerReadyStateType.None;
        readyStateTimer = 0f;
        readyStateWindowDuration = 0f;
        RefreshGaugeCapacity(fillToMax: true);
    }

    public void GainMomentum(int amount)
    {
        int gainAmount = Mathf.Max(0, amount);
        if (gainAmount <= 0)
            return;

        momentumStacks = Mathf.Clamp(momentumStacks + gainAmount, 0, Mathf.Max(0, maxMomentumStacks));
    }

    public void ConsumeMomentum(int amount)
    {
        int consumeAmount = Mathf.Max(0, amount);
        if (consumeAmount <= 0)
            return;

        momentumStacks = Mathf.Clamp(momentumStacks - consumeAmount, 0, Mathf.Max(0, maxMomentumStacks));
    }

    public void ResetMomentum()
    {
        momentumStacks = 0;
    }

    private void Update()
    {
        TickFlowStacks(Time.deltaTime);
        TickEmpoweredBasic(Time.deltaTime);
        TickReadyState(Time.deltaTime);
    }

    public void RefreshGaugeCapacity(bool fillToMax = false)
    {
        float resolvedMaxGauge = ResolveMaxGaugeCapacity();
        bool shouldFillGauge = fillToMax || maxGauge <= 0f;

        maxGauge = resolvedMaxGauge;
        currentGauge = shouldFillGauge
            ? maxGauge
            : Mathf.Clamp(currentGauge, 0f, maxGauge);
        lockedGaugeSpend = Mathf.Clamp(lockedGaugeSpend, 0f, currentGauge);
    }

    public float AddGauge(float amount)
    {
        float gainAmount = Mathf.Max(0f, amount);
        if (gainAmount <= 0f || MaxGauge <= 0f)
            return 0f;

        float previousGauge = currentGauge;
        currentGauge = Mathf.Clamp(currentGauge + gainAmount, 0f, maxGauge);
        return currentGauge - previousGauge;
    }

    public bool HasEnoughGauge(float amount)
    {
        return CurrentGauge >= Mathf.Max(0f, amount);
    }

    public bool TryConsumeGauge(float amount)
    {
        float consumeAmount = Mathf.Max(0f, amount);
        if (consumeAmount <= 0f)
            return true;

        if (CurrentGauge + 0.0001f < consumeAmount)
            return false;

        currentGauge = Mathf.Clamp(currentGauge - consumeAmount, 0f, maxGauge);
        lockedGaugeSpend = Mathf.Clamp(lockedGaugeSpend, 0f, currentGauge);
        return true;
    }

    public float LockGaugeSpend(float requestedAmount)
    {
        lockedGaugeSpend = Mathf.Clamp(requestedAmount, 0f, CurrentGauge);
        return lockedGaugeSpend;
    }

    public float CommitLockedGaugeSpend()
    {
        float committedSpend = LockedGaugeSpend;
        if (committedSpend <= 0f)
            return 0f;

        currentGauge = Mathf.Clamp(currentGauge - committedSpend, 0f, maxGauge);
        lockedGaugeSpend = 0f;
        return committedSpend;
    }

    public void ClearLockedGaugeSpend()
    {
        lockedGaugeSpend = 0f;
    }

    public void SetHoldChargeTime(float holdChargeTime)
    {
        currentHoldChargeTime = Mathf.Max(0f, holdChargeTime);
    }

    public void ClearHoldChargeTime()
    {
        currentHoldChargeTime = 0f;
    }

    public bool TryConsumeEmpoweredBasic(out EmpoweredBasicDefinition empoweredBasicDefinition)
    {
        if (!HasEmpoweredBasicReady)
        {
            empoweredBasicDefinition = default;
            return false;
        }

        empoweredBasicDefinition = activeEmpoweredBasic.GetSanitized();
        ClearEmpoweredBasic(PlayerEmpoweredBasicStateChangeReason.Consumed);
        return true;
    }

    public bool HasMatchingReadyState(PlayerReadyStateType readyStateType)
    {
        return HasReadyStateActive
            && readyStateType != PlayerReadyStateType.None
            && activeReadyStateType == readyStateType;
    }

    public bool TryConsumeReadyState(PlayerReadyStateType readyStateType)
    {
        if (!HasMatchingReadyState(readyStateType))
            return false;

        ClearReadyState(PlayerReadyStateChangeReason.Consumed);
        return true;
    }

    public bool TryActivateReadyState(PlayerReadyStateType readyStateType, float duration)
    {
        if (readyStateType == PlayerReadyStateType.None || duration <= 0f)
            return false;

        ActivateReadyState(readyStateType, duration);
        return true;
    }

    public bool TryActivateGrantedReadyState(EmpoweredBasicDefinition empoweredBasicDefinition)
    {
        EmpoweredBasicDefinition sanitizedDefinition = empoweredBasicDefinition.GetSanitized();
        if (!sanitizedDefinition.IsConfigured
            || sanitizedDefinition.GrantedReadyStateType == PlayerReadyStateType.None
            || sanitizedDefinition.GrantedReadyStateDuration <= 0f)
        {
            return false;
        }

        return TryActivateReadyState(
            sanitizedDefinition.GrantedReadyStateType,
            sanitizedDefinition.GrantedReadyStateDuration);
    }

    public float RegisterBuilderHit(
        PlayerBasicAttackProfile basicAttackProfile,
        bool isChainFinisher,
        bool isAerial,
        bool wasValidHit = true)
    {
        if (basicAttackProfile == null)
            return 0f;

        CacheFlowRules(basicAttackProfile);

        int appliedFlowStackCount = currentFlowStacks;
        bool reachedMaxFlowStackThisHit = false;
        if (wasValidHit || !basicAttackProfile.FlowStacksRequireValidHit)
            appliedFlowStackCount = AdvanceFlowStack(basicAttackProfile, out reachedMaxFlowStackThisHit);

        EmpoweredBasicDefinition empoweredBasicDefinition = basicAttackProfile.EmpoweredBasic;
        bool shouldTriggerFlowCompletion = reachedMaxFlowStackThisHit && empoweredBasicDefinition.IsConfigured;

        if (!basicAttackProfile.BuildsGauge)
        {
            if (shouldTriggerFlowCompletion)
                ActivateEmpoweredBasic(empoweredBasicDefinition, Mathf.Max(1, basicAttackProfile.MaxFlowStacks));

            if (reachedMaxFlowStackThisHit && (shouldTriggerFlowCompletion || basicAttackProfile.ResetFlowStacksOnMaxReached))
                ResetFlowStacks();

            return 0f;
        }

        float baseGaugeGain = wasValidHit ? basicAttackProfile.GaugeGainOnValidHit : 0f;
        if (isChainFinisher)
            baseGaugeGain += basicAttackProfile.GaugeGainOnChainFinisher;

        float gaugeGainMultiplier = EvaluateFlowGaugeMultiplier(basicAttackProfile, appliedFlowStackCount);
        if (isAerial)
            gaugeGainMultiplier *= Mathf.Max(0f, basicAttackProfile.AerialGaugeGainMultiplier);

        float gainedGauge = AddGauge(baseGaugeGain * gaugeGainMultiplier);

        if (shouldTriggerFlowCompletion)
            ActivateEmpoweredBasic(empoweredBasicDefinition, Mathf.Max(1, basicAttackProfile.MaxFlowStacks));

        if (reachedMaxFlowStackThisHit && (shouldTriggerFlowCompletion || basicAttackProfile.ResetFlowStacksOnMaxReached))
            ResetFlowStacks();

        return gainedGauge;
    }

    public void RefreshFlowTimer()
    {
        if (currentFlowStacks <= 0)
            return;

        RefreshFlowTimer(currentFlowStacks > 1
            ? cachedChainedFlowWindowDuration
            : cachedFirstFlowWindowDuration);
    }

    public void ResetFlowStacks()
    {
        currentFlowStacks = 0;
        currentFlowStackTimer = 0f;
        currentFlowWindowDuration = 0f;
    }

    public void ClearEmpoweredBasic(PlayerEmpoweredBasicStateChangeReason reason = PlayerEmpoweredBasicStateChangeReason.Cleared)
    {
        bool hadEmpoweredBasic = activeEmpoweredBasic.IsConfigured || empoweredBasicTimer > 0f || empoweredBasicWindowDuration > 0f;
        EmpoweredBasicDefinition previousDefinition = activeEmpoweredBasic.GetSanitized();
        activeEmpoweredBasic = default;
        empoweredBasicTimer = 0f;
        empoweredBasicWindowDuration = 0f;

        if (hadEmpoweredBasic)
            PublishEmpoweredBasicStateChanged(
                isActive: false,
                reason,
                previousDefinition,
                remainingDuration: 0f,
                totalDuration: 0f);
    }

    public void ClearReadyState(PlayerReadyStateChangeReason reason = PlayerReadyStateChangeReason.Cleared)
    {
        bool hadReadyState = activeReadyStateType != PlayerReadyStateType.None
            || readyStateTimer > 0f
            || readyStateWindowDuration > 0f;
        PlayerReadyStateType previousReadyStateType = activeReadyStateType;
        activeReadyStateType = PlayerReadyStateType.None;
        readyStateTimer = 0f;
        readyStateWindowDuration = 0f;

        if (hadReadyState)
            PublishReadyStateChanged(
                isActive: false,
                reason,
                previousReadyStateType,
                remainingDuration: 0f,
                totalDuration: 0f);
    }

    private float ResolveMaxGaugeCapacity()
    {
        float resolvedGauge = Mathf.Max(0f, fallbackMaxGauge);
        if (ownerCharacter == null)
            return resolvedGauge;

        PlayerJobDefinition jobDefinition = PlayerJobCombatProfiles.GetJobDefinition(ownerCharacter.CurrentJob);
        if (jobDefinition == null)
            return resolvedGauge;

        return Mathf.Max(0f, jobDefinition.BaseMaxGauge);
    }

    private void CacheFlowRules(PlayerBasicAttackProfile basicAttackProfile)
    {
        cachedFirstFlowWindowDuration = Mathf.Max(0.05f, basicAttackProfile.FirstFlowWindowDuration);
        cachedChainedFlowWindowDuration = Mathf.Max(0.05f, basicAttackProfile.ChainedFlowWindowDuration);
        resetAllFlowStacksOnTimeout = basicAttackProfile.ResetAllFlowStacksOnTimeout;
    }

    private int AdvanceFlowStack(PlayerBasicAttackProfile basicAttackProfile, out bool reachedMaxFlowStack)
    {
        int resolvedMaxFlowStacks = Mathf.Max(1, basicAttackProfile.MaxFlowStacks);
        reachedMaxFlowStack = false;

        if (currentFlowStacks <= 0)
        {
            currentFlowStacks = 1;
            RefreshFlowTimer(cachedFirstFlowWindowDuration);
            reachedMaxFlowStack = currentFlowStacks >= resolvedMaxFlowStacks;
            return currentFlowStacks;
        }

        currentFlowStacks = Mathf.Clamp(currentFlowStacks + 1, 1, resolvedMaxFlowStacks);
        reachedMaxFlowStack = currentFlowStacks >= resolvedMaxFlowStacks;
        RefreshFlowTimer(currentFlowStacks > 1
            ? cachedChainedFlowWindowDuration
            : cachedFirstFlowWindowDuration);
        return currentFlowStacks;
    }

    private void RefreshFlowTimer(float duration)
    {
        currentFlowWindowDuration = Mathf.Max(0.05f, duration);
        currentFlowStackTimer = currentFlowWindowDuration;
    }

    private float EvaluateFlowGaugeMultiplier(PlayerBasicAttackProfile basicAttackProfile, int appliedFlowStackCount)
    {
        AnimationCurve multiplierCurve = basicAttackProfile.GaugeGainMultiplierByFlowStack;
        if (multiplierCurve == null)
            return 1f;

        return Mathf.Max(0f, multiplierCurve.Evaluate(Mathf.Max(0, appliedFlowStackCount)));
    }

    private void HandleFlowTimeout()
    {
        if (currentFlowStacks <= 0)
        {
            ResetFlowStacks();
            return;
        }

        if (resetAllFlowStacksOnTimeout || currentFlowStacks <= 1)
        {
            ResetFlowStacks();
            return;
        }

        currentFlowStacks = Mathf.Max(0, currentFlowStacks - 1);
        if (currentFlowStacks <= 0)
        {
            ResetFlowStacks();
            return;
        }

        RefreshFlowTimer(currentFlowStacks > 1
            ? cachedChainedFlowWindowDuration
            : cachedFirstFlowWindowDuration);
    }

    private void ActivateEmpoweredBasic(EmpoweredBasicDefinition empoweredBasicDefinition, int completedStackCount)
    {
        EmpoweredBasicDefinition sanitizedDefinition = empoweredBasicDefinition.GetSanitized();
        if (!sanitizedDefinition.IsConfigured)
            return;

        activeEmpoweredBasic = sanitizedDefinition;
        empoweredBasicWindowDuration = sanitizedDefinition.WindowDuration;
        empoweredBasicTimer = empoweredBasicWindowDuration;
        PublishFlowCompleted(Mathf.Max(1, completedStackCount), sanitizedDefinition);
        PublishEmpoweredBasicStateChanged(
            isActive: true,
            PlayerEmpoweredBasicStateChangeReason.Activated,
            sanitizedDefinition,
            empoweredBasicTimer,
            empoweredBasicWindowDuration);
    }

    private void TickFlowStacks(float deltaTime)
    {
        if (currentFlowStacks <= 0 || currentFlowStackTimer <= 0f)
            return;

        currentFlowStackTimer -= deltaTime;
        if (currentFlowStackTimer > 0f)
            return;

        HandleFlowTimeout();
    }

    private void TickEmpoweredBasic(float deltaTime)
    {
        if (empoweredBasicTimer <= 0f)
            return;

        empoweredBasicTimer = Mathf.Max(0f, empoweredBasicTimer - deltaTime);
        if (empoweredBasicTimer > 0f)
            return;

        ClearEmpoweredBasic(PlayerEmpoweredBasicStateChangeReason.Expired);
    }

    private void ActivateReadyState(PlayerReadyStateType readyStateType, float duration)
    {
        if (readyStateType == PlayerReadyStateType.None || duration <= 0f)
            return;

        activeReadyStateType = readyStateType;
        readyStateWindowDuration = Mathf.Max(0f, duration);
        readyStateTimer = readyStateWindowDuration;
        PublishReadyStateChanged(
            isActive: true,
            PlayerReadyStateChangeReason.Activated,
            readyStateType,
            readyStateTimer,
            readyStateWindowDuration);
    }

    private void TickReadyState(float deltaTime)
    {
        if (readyStateTimer <= 0f)
            return;

        readyStateTimer = Mathf.Max(0f, readyStateTimer - deltaTime);
        if (readyStateTimer > 0f)
            return;

        ClearReadyState(PlayerReadyStateChangeReason.Expired);
    }

    private void PublishFlowCompleted(int completedStackCount, EmpoweredBasicDefinition empoweredBasicDefinition)
    {
        EventBus.Publish(new PlayerFlowCompletedEvent
        {
            Player = ownerCharacter,
            CharacterId = ownerCharacter != null ? ownerCharacter.CharacterId : string.Empty,
            CompletedStackCount = Mathf.Max(1, completedStackCount),
            EmpoweredBasic = empoweredBasicDefinition.GetSanitized()
        });
    }

    private void PublishEmpoweredBasicStateChanged(
        bool isActive,
        PlayerEmpoweredBasicStateChangeReason reason,
        EmpoweredBasicDefinition empoweredBasicDefinition,
        float remainingDuration,
        float totalDuration)
    {
        EventBus.Publish(new PlayerEmpoweredBasicStateChangedEvent
        {
            Player = ownerCharacter,
            CharacterId = ownerCharacter != null ? ownerCharacter.CharacterId : string.Empty,
            IsActive = isActive,
            Reason = reason,
            RemainingDuration = Mathf.Max(0f, remainingDuration),
            TotalDuration = Mathf.Max(0f, totalDuration),
            EmpoweredBasic = empoweredBasicDefinition.GetSanitized()
        });
    }

    private void PublishReadyStateChanged(
        bool isActive,
        PlayerReadyStateChangeReason reason,
        PlayerReadyStateType readyStateType,
        float remainingDuration,
        float totalDuration)
    {
        EventBus.Publish(new PlayerReadyStateChangedEvent
        {
            Player = ownerCharacter,
            CharacterId = ownerCharacter != null ? ownerCharacter.CharacterId : string.Empty,
            IsActive = isActive,
            Reason = reason,
            RemainingDuration = Mathf.Max(0f, remainingDuration),
            TotalDuration = Mathf.Max(0f, totalDuration),
            ReadyStateType = readyStateType
        });
    }
}

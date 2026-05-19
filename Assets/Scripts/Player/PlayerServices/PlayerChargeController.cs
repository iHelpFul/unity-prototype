using UnityEngine;

public sealed class PlayerChargeController
{
    private sealed class ActiveBurstCharge
    {
        public int SlotIndex;
        public PlayerSkillDefinition Definition;
        public BurstLinkedSkillProfile Profile;
        public int ResolvedSkillLevel;
    }

    private readonly struct BurstChargeSpendResolution
    {
        public BurstChargeSpendResolution(float lockedSpend, bool reachedSpendCap)
        {
            LockedSpend = Mathf.Max(0f, lockedSpend);
            ReachedSpendCap = reachedSpendCap;
        }

        public float LockedSpend { get; }
        public bool ReachedSpendCap { get; }
    }

    private PlayerCharacter character;
    private PlayerCombatModule combatModule;
    private PlayerAnimationController animationController;
    private System.Func<bool> resolveIsPlayerAerial;
    private System.Action<PresentationCueSet, CombatCuePhase, Vector3> publishCue;
    private System.Action<PresentationCueSet, CombatCuePhase, Vector3> stopCue;

    private ActiveBurstCharge activeBurstCharge;
    private PendingBurstReleaseRequest pendingBurstRelease;

    public bool HasActiveBurstCharge =>
        activeBurstCharge != null
        && character != null
        && character.ActionStateController != null
        && character.ActionStateController.IsBurstSkillChargeActive;

    public bool ShouldSuppressAttackEnd => HasActiveBurstCharge;

    public void Initialize(
        PlayerCharacter ownerCharacter,
        PlayerCombatModule ownerCombatModule,
        PlayerAnimationController resolvedAnimationController,
        System.Func<bool> playerAerialResolver,
        System.Action<PresentationCueSet, CombatCuePhase, Vector3> cuePublisher,
        System.Action<PresentationCueSet, CombatCuePhase, Vector3> cueStopper)
    {
        character = ownerCharacter;
        combatModule = ownerCombatModule;
        animationController = resolvedAnimationController;
        resolveIsPlayerAerial = playerAerialResolver;
        publishCue = cuePublisher;
        stopCue = cueStopper;
    }

    public void Tick(float deltaTime)
    {
        UpdateBurstSkillCharge(deltaTime);
    }

    public bool CanBeginBurstCharge(BurstLinkedSkillProfile profile)
    {
        return character != null
            && !character.IsDead
            && !character.IsStunned
            && combatModule != null
            && character.ActionStateController != null
            && profile != null
            && character.ActionStateController.CanStartBurstLinkedSkill
            && !combatModule.IsAttacking
            && character.CurrentGauge + 0.0001f >= Mathf.Max(0f, profile.MinimumGaugeToStart);
    }

    public void BeginBurstCharge(
        int slotIndex,
        PlayerSkillDefinition definition,
        BurstLinkedSkillProfile profile,
        int resolvedSkillLevel)
    {
        if (!CanBeginBurstCharge(profile) || definition == null)
            return;

        activeBurstCharge = new ActiveBurstCharge
        {
            SlotIndex = slotIndex,
            Definition = definition,
            Profile = profile,
            ResolvedSkillLevel = resolvedSkillLevel
        };

        character.ActionStateController.BeginBurstSkillCharge();
        character.SetCombatHoldChargeTime(0f);

        bool isAerial = resolveIsPlayerAerial != null && resolveIsPlayerAerial.Invoke();
        BurstChargeSpendResolution initialSpendResolution =
            ResolveBurstSkillChargeSpend(profile, 0f, isAerial);
        character.LockCombatGaugeSpend(initialSpendResolution.LockedSpend);

        animationController?.PlayBurstSkillChargeAnimation(profile);
        publishCue?.Invoke(definition.BurstPresentationCueSet, CombatCuePhase.ChargeStart, character.transform.position);

        if (string.IsNullOrWhiteSpace(profile.ChargeEntryAnimatorStateName))
            OnBurstChargeLoopReady();
    }

    public bool TryReleaseBurstChargeForSlot(int slotIndex)
    {
        if (!HasActiveBurstCharge || activeBurstCharge == null || activeBurstCharge.SlotIndex != slotIndex)
            return false;

        return ReleaseBurstLinkedSkill(activeBurstCharge);
    }

    public void OnBurstChargeLoopReady()
    {
        if (!HasActiveBurstCharge || activeBurstCharge == null)
            return;

        animationController?.PlayBurstSkillChargeLoopAnimation(activeBurstCharge.Profile);
        publishCue?.Invoke(
            activeBurstCharge.Definition != null
                ? activeBurstCharge.Definition.BurstPresentationCueSet
                : null,
            CombatCuePhase.ChargeLoop,
            character.transform.position);
    }

    public void ResumeActiveChargeAnimation()
    {
        if (!HasActiveBurstCharge || activeBurstCharge == null)
            return;

        animationController?.PlayBurstSkillChargeLoopAnimation(activeBurstCharge.Profile);
    }

    public bool TryConsumePendingBurstRelease(out PendingBurstReleaseRequest request)
    {
        request = pendingBurstRelease;
        pendingBurstRelease = null;
        return request != null && request.Definition != null && request.Payload != null;
    }

    public bool HandleInterruptingHit()
    {
        if (character == null || character.ActionStateController == null)
            return true;

        if (!HasActiveBurstCharge || activeBurstCharge == null)
            return true;

        if (activeBurstCharge.Profile != null && !activeBurstCharge.Profile.InterruptedByValidHit)
            return false;

        if (activeBurstCharge.Definition != null)
        {
            stopCue?.Invoke(
                activeBurstCharge.Definition.BurstPresentationCueSet,
                CombatCuePhase.ChargeLoop,
                character.transform.position);
        }

        if (activeBurstCharge.Profile != null && activeBurstCharge.Profile.LoseLockedGaugeOnInterrupt)
            character.CommitLockedCombatGaugeSpend();
        else
            character.ClearLockedCombatGaugeSpend();

        character.ClearCombatHoldChargeTime();
        activeBurstCharge = null;
        pendingBurstRelease = null;
        return true;
    }

    public void CancelAllCharges(bool clearLockedGaugeSpend)
    {
        CancelBurstChargeState(clearLockedGaugeSpend);
        pendingBurstRelease = null;
    }

    private void UpdateBurstSkillCharge(float deltaTime)
    {
        if (!HasActiveBurstCharge || activeBurstCharge == null)
            return;

        BurstLinkedSkillProfile profile = activeBurstCharge.Profile;
        if (profile == null)
        {
            CancelBurstChargeState(clearLockedGaugeSpend: true);
            character.ActionStateController.ResetState();
            return;
        }

        bool isAerial = resolveIsPlayerAerial != null && resolveIsPlayerAerial.Invoke();
        float maxHoldDuration = ResolveEffectiveBurstSkillMaxHoldDuration(profile, isAerial);
        float nextHoldChargeTime = Mathf.Clamp(
            character.CurrentHoldChargeTime + Mathf.Max(0f, deltaTime),
            0f,
            maxHoldDuration);
        character.SetCombatHoldChargeTime(nextHoldChargeTime);

        BurstChargeSpendResolution spendResolution =
            ResolveBurstSkillChargeSpend(profile, nextHoldChargeTime, isAerial);
        character.LockCombatGaugeSpend(spendResolution.LockedSpend);

        if ((profile.AutoReleaseAtMaxHold && nextHoldChargeTime >= maxHoldDuration - 0.0001f)
            || spendResolution.ReachedSpendCap)
        {
            ReleaseBurstLinkedSkill(activeBurstCharge);
        }
    }

    private bool ReleaseBurstLinkedSkill(ActiveBurstCharge burstCharge)
    {
        if (burstCharge == null
            || burstCharge.Definition == null
            || burstCharge.Profile == null
            || character == null
            || character.ActionStateController == null
            || !character.ActionStateController.IsBurstSkillChargeActive)
        {
            return false;
        }

        bool isAerial = resolveIsPlayerAerial != null && resolveIsPlayerAerial.Invoke();
        float holdChargeTime = character.CurrentHoldChargeTime;
        float resolvedGaugeSpend = Mathf.Max(
            Mathf.Max(0f, burstCharge.Profile.MinimumGaugeToStart),
            character.LockedGaugeSpend);
        resolvedGaugeSpend = Mathf.Min(resolvedGaugeSpend, character.CurrentGauge);

        if (resolvedGaugeSpend <= 0f)
        {
            CancelBurstChargeState(clearLockedGaugeSpend: true);
            character.ActionStateController.ResetState();
            return false;
        }

        AttackPayload payload = AttackPayloadBuilder.BuildBurstLinkedSkillPayload(
            character,
            character.GetCombatSnapshot(),
            burstCharge.Definition,
            burstCharge.ResolvedSkillLevel,
            burstCharge.Profile,
            resolvedGaugeSpend,
            character.CurrentFlowStacks,
            holdChargeTime,
            isAerial);

        if (payload == null)
        {
            CancelBurstChargeState(clearLockedGaugeSpend: true);
            character.ActionStateController.ResetState();
            return false;
        }

        if (combatModule == null || !combatModule.TryStartSkillAttack(burstCharge.Definition))
        {
            CancelBurstChargeState(clearLockedGaugeSpend: true);
            character.ActionStateController.ResetState();
            return false;
        }

        if (character.ActionStateController.IsUtilityActive)
            character.ActionStateController.CancelUtility();

        stopCue?.Invoke(
            burstCharge.Definition.BurstPresentationCueSet,
            CombatCuePhase.ChargeLoop,
            character.transform.position);
        character.CommitLockedCombatGaugeSpend();
        character.ClearCombatHoldChargeTime();
        character.ActionStateController.CommitBurstSkillRelease(
            burstCharge.Definition.GetResolvedRecoveryTime(burstCharge.ResolvedSkillLevel));
        animationController?.PlayBurstSkillReleaseAnimation(burstCharge.Profile);
        publishCue?.Invoke(
            burstCharge.Definition.BurstPresentationCueSet,
            CombatCuePhase.Release,
            character.transform.position);
        pendingBurstRelease = new PendingBurstReleaseRequest
        {
            Definition = burstCharge.Definition,
            ResolvedSkillLevel = burstCharge.ResolvedSkillLevel,
            Payload = payload
        };
        activeBurstCharge = null;
        return true;
    }

    private void CancelBurstChargeState(bool clearLockedGaugeSpend)
    {
        if (character == null)
            return;

        if (activeBurstCharge != null && activeBurstCharge.Definition != null)
        {
            stopCue?.Invoke(
                activeBurstCharge.Definition.BurstPresentationCueSet,
                CombatCuePhase.ChargeLoop,
                character.transform.position);
        }

        character.ActionStateController?.CancelBurstSkillCharge();
        character.ClearCombatHoldChargeTime();
        animationController?.ClearActionAnimationOverride();
        activeBurstCharge = null;

        if (clearLockedGaugeSpend)
            character.ClearLockedCombatGaugeSpend();
    }

    private static float ResolveEffectiveBurstSkillMaxHoldDuration(BurstLinkedSkillProfile profile, bool isAerial)
    {
        if (profile == null)
            return 0.05f;

        float maxHoldDuration = Mathf.Max(0.05f, profile.MaxHoldDuration);
        if (isAerial)
            maxHoldDuration *= Mathf.Max(0f, profile.AerialMaxHoldDurationMultiplier);

        return Mathf.Max(0.05f, maxHoldDuration);
    }

    private BurstChargeSpendResolution ResolveBurstSkillChargeSpend(
        BurstLinkedSkillProfile profile,
        float holdChargeTime,
        bool isAerial)
    {
        if (profile == null || character == null)
            return new BurstChargeSpendResolution(0f, false);

        float availableGauge = Mathf.Max(0f, character.CurrentGauge);
        if (availableGauge <= 0f)
            return new BurstChargeSpendResolution(0f, false);

        float minimumGaugeSpend = Mathf.Clamp(
            Mathf.Max(0f, profile.MinimumGaugeToStart),
            0f,
            availableGauge);
        GaugeScalingProfile scalingProfile = profile.GaugeScalingProfile;
        float configuredMaximumGaugeSpend = scalingProfile != null
            ? Mathf.Max(minimumGaugeSpend, scalingProfile.MaximumGaugeSpend)
            : availableGauge;

        if (!profile.AllowPartialGaugeSpend)
        {
            float fullSpend = configuredMaximumGaugeSpend;
            if (isAerial)
                fullSpend *= Mathf.Max(0f, profile.AerialGaugeSpendMultiplier);

            float clampedSpend = Mathf.Clamp(fullSpend, 0f, availableGauge);
            bool reachedSpendCap = fullSpend >= availableGauge - 0.0001f;
            return new BurstChargeSpendResolution(clampedSpend, reachedSpendCap);
        }

        float maxHoldDuration = ResolveEffectiveBurstSkillMaxHoldDuration(profile, isAerial);
        float holdNormalized = maxHoldDuration > 0f
            ? Mathf.Clamp01(holdChargeTime / maxHoldDuration)
            : 1f;
        AnimationCurve spendCurve = scalingProfile != null
            ? scalingProfile.SpendByHoldNormalized
            : null;
        float spendNormalized = spendCurve != null
            ? Mathf.Clamp01(spendCurve.Evaluate(holdNormalized))
            : holdNormalized;
        float desiredSpend = Mathf.Lerp(minimumGaugeSpend, configuredMaximumGaugeSpend, spendNormalized);

        if (isAerial)
            desiredSpend *= Mathf.Max(0f, profile.AerialGaugeSpendMultiplier);

        float lockedSpend = Mathf.Clamp(desiredSpend, minimumGaugeSpend, availableGauge);
        bool reachedSpendCapByGaugeLimit =
            availableGauge > minimumGaugeSpend + 0.0001f
            && desiredSpend >= availableGauge - 0.0001f;

        return new BurstChargeSpendResolution(lockedSpend, reachedSpendCapByGaugeLimit);
    }
}

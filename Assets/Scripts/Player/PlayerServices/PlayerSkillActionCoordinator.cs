using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerSkillActionCoordinator
{
    private sealed class PendingBurstSkillInput
    {
        public PlayerSkillDefinition Definition;
        public BurstLinkedSkillProfile Profile;
        public int ResolvedSkillLevel;
        public int SlotIndex;
        public float HoldElapsed;
    }

    private PlayerCharacter character;
    private PlayerCombatModule combatModule;
    private PlayerAnimationController animationController;
    private PlayerMovementController movementController;
    private Transform visualTransform;
    private PlayerChargeController chargeController;
    private GameBootstrap bootstrap;
    private System.Action<PresentationCueSet, CombatCuePhase, Vector3> publishCue;
    private System.Action<string> notifySystemMessage;
    private Coroutine utilityRepositionRoutine;

    private PendingSkillCastRequest pendingSkillCast;
    private PendingBurstSkillInput pendingBurstSkillInput;

    public bool HasPendingSkillCast => pendingSkillCast != null;
    public bool HasPendingBurstInput => pendingBurstSkillInput != null;

    public void Initialize(
        PlayerCharacter ownerCharacter,
        PlayerCombatModule ownerCombatModule,
        PlayerAnimationController resolvedAnimationController,
        PlayerMovementController resolvedMovementController,
        Transform resolvedVisualTransform,
        PlayerChargeController resolvedChargeController,
        GameBootstrap sessionBootstrap,
        System.Action<PresentationCueSet, CombatCuePhase, Vector3> cuePublisher,
        System.Action<string> systemMessageNotifier)
    {
        character = ownerCharacter;
        combatModule = ownerCombatModule;
        animationController = resolvedAnimationController;
        movementController = resolvedMovementController;
        visualTransform = resolvedVisualTransform != null ? resolvedVisualTransform : ownerCharacter != null ? ownerCharacter.transform : null;
        chargeController = resolvedChargeController;
        bootstrap = sessionBootstrap;
        publishCue = cuePublisher;
        notifySystemMessage = systemMessageNotifier;
    }

    public void BindBootstrap(GameBootstrap sessionBootstrap)
    {
        bootstrap = sessionBootstrap;
    }

    public void Tick(float deltaTime)
    {
        if (pendingBurstSkillInput == null)
            return;

        if (character == null
            || character.ActionStateController == null
            || character.IsDead
            || character.IsStunned)
        {
            pendingBurstSkillInput = null;
            return;
        }

        BurstLinkedSkillProfile profile = pendingBurstSkillInput.Profile;
        if (profile == null || pendingBurstSkillInput.Definition == null)
        {
            pendingBurstSkillInput = null;
            return;
        }

        pendingBurstSkillInput.HoldElapsed = Mathf.Max(0f, pendingBurstSkillInput.HoldElapsed + deltaTime);
        if (pendingBurstSkillInput.HoldElapsed < Mathf.Max(0f, profile.TapReleaseThreshold))
            return;

        if (chargeController == null || !chargeController.CanBeginBurstCharge(profile))
            return;

        if (!TryConsumeSkillActivationResources(
                pendingBurstSkillInput.Definition,
                pendingBurstSkillInput.ResolvedSkillLevel))
        {
            pendingBurstSkillInput = null;
            return;
        }

        chargeController.BeginBurstCharge(
            pendingBurstSkillInput.SlotIndex,
            pendingBurstSkillInput.Definition,
            pendingBurstSkillInput.Profile,
            pendingBurstSkillInput.ResolvedSkillLevel);
        pendingBurstSkillInput = null;
    }

    public void HandleSkillSlotPressed(int slotIndex)
    {
        if (character == null
            || character.IsDead
            || character.IsStunned
            || combatModule == null
            || character.ActionStateController == null)
        {
            return;
        }

        if (pendingBurstSkillInput != null)
            return;

        if (!TryResolveAssignedSkillDefinition(slotIndex, out PlayerSkillDefinition definition))
        {
            notifySystemMessage?.Invoke($"No skill assigned to slot {slotIndex}.");
            return;
        }

        if (definition.JobType != character.CurrentJob)
        {
            notifySystemMessage?.Invoke($"{definition.DisplayName} belongs to {GameBootstrap.FormatJobName(definition.JobType)}.");
            return;
        }

        bool isUtilitySkill = IsUtilitySkillDefinition(definition);
        bool isBurstLinkedSkill = IsBurstLinkedSkillDefinition(definition);
        if (!isUtilitySkill && definition.SkillType != PlayerSkillType.Attack)
        {
            notifySystemMessage?.Invoke($"{definition.DisplayName} is not wired yet.");
            return;
        }

        if (isUtilitySkill)
        {
            if (!character.ActionStateController.CanStartUtilitySkill(definition.UtilitySkillProfile))
                return;
        }
        else if (isBurstLinkedSkill)
        {
            if (!character.ActionStateController.CanStartBurstLinkedSkill)
                return;
        }
        else if (!character.ActionStateController.CanStartSkill)
        {
            return;
        }

        if (combatModule.IsAttacking)
            return;

        int resolvedSkillLevel = ResolveSkillLevel(definition.SkillId);
        if (isBurstLinkedSkill)
        {
            if (!ValidateSkillActivationAvailability(definition, resolvedSkillLevel))
                return;

            pendingBurstSkillInput = new PendingBurstSkillInput
            {
                Definition = definition,
                Profile = definition.BurstLinkedSkillProfile,
                ResolvedSkillLevel = resolvedSkillLevel,
                SlotIndex = slotIndex,
                HoldElapsed = 0f
            };
            return;
        }

        if (!TryConsumeSkillActivationResources(definition, resolvedSkillLevel))
            return;

        if (isUtilitySkill)
        {
            ActivateUtilitySkill(definition, resolvedSkillLevel);
            return;
        }

        if (!combatModule.TryStartSkillAttack(definition))
            return;

        CancelPendingSkillState();
        pendingSkillCast = new PendingSkillCastRequest
        {
            Definition = definition,
            LockedTarget = null,
            ResolvedSkillLevel = resolvedSkillLevel
        };
        character.ActionStateController.BeginSkillPreCommit(definition.GetResolvedRecoveryTime(resolvedSkillLevel));
        animationController?.PlaySkillAnimation(definition);
        publishCue?.Invoke(definition.PresentationCueSet, CombatCuePhase.CastStart, character.transform.position);
    }

    public void HandleSkillSlotReleased(int slotIndex)
    {
        if (pendingBurstSkillInput != null && pendingBurstSkillInput.SlotIndex == slotIndex)
        {
            PendingBurstSkillInput burstInput = pendingBurstSkillInput;
            pendingBurstSkillInput = null;
            ExecuteBurstSkillTap(burstInput);
            return;
        }

        if (chargeController == null || !chargeController.TryReleaseBurstChargeForSlot(slotIndex))
            return;

        SyncCommittedChargeReleases();
    }

    public void SyncCommittedChargeReleases()
    {
        if (chargeController == null)
            return;

        while (chargeController.TryConsumePendingBurstRelease(out PendingBurstReleaseRequest burstRelease))
        {
            pendingSkillCast = new PendingSkillCastRequest
            {
                Definition = burstRelease.Definition,
                LockedTarget = null,
                Payload = burstRelease.Payload,
                ResolvedSkillLevel = burstRelease.ResolvedSkillLevel,
                CommitAsBurstSkill = true
            };
        }
    }

    public bool TryConsumePendingSkillCast(out PendingSkillCastRequest request)
    {
        request = pendingSkillCast;
        pendingSkillCast = null;
        return request != null && request.Definition != null;
    }

    public void ClearPendingSkillCast()
    {
        pendingSkillCast = null;
    }

    public void CancelPendingSkillState()
    {
        pendingSkillCast = null;
        pendingBurstSkillInput = null;
        CancelUtilityReposition();
        animationController?.ClearActionAnimationOverride();
    }

    public void ClearTransientState()
    {
        pendingSkillCast = null;
        pendingBurstSkillInput = null;
        CancelUtilityReposition();
    }

    public bool TryCompleteActiveUtility()
    {
        if (character == null
            || character.ActionStateController == null
            || !character.ActionStateController.IsUtilityActive)
        {
            return false;
        }

        character.ActionStateController.CompleteUtility();
        if (character.ActionStateController.IsAnyChargeActive)
        {
            animationController?.ClearActionAnimationOverride(false);
            chargeController?.ResumeActiveChargeAnimation();
            return true;
        }

        animationController?.ClearActionAnimationOverride();
        return true;
    }

    private void ExecuteBurstSkillTap(PendingBurstSkillInput burstInput)
    {
        if (burstInput == null || burstInput.Definition == null || character == null)
            return;

        if (!TryConsumeSkillActivationResources(burstInput.Definition, burstInput.ResolvedSkillLevel))
            return;

        if (combatModule == null || !combatModule.TryStartSkillAttack(burstInput.Definition))
            return;

        CancelPendingSkillState();
        pendingSkillCast = new PendingSkillCastRequest
        {
            Definition = burstInput.Definition,
            LockedTarget = null,
            ResolvedSkillLevel = burstInput.ResolvedSkillLevel
        };
        character.ActionStateController.BeginSkillPreCommit(
            burstInput.Definition.GetResolvedRecoveryTime(burstInput.ResolvedSkillLevel));
        animationController?.PlaySkillAnimation(burstInput.Definition);
        publishCue?.Invoke(
            burstInput.Definition.PresentationCueSet,
            CombatCuePhase.CastStart,
            character.transform.position);
    }

    private void ActivateUtilitySkill(PlayerSkillDefinition definition, int resolvedSkillLevel)
    {
        if (character == null || definition == null)
            return;

        character.ActionStateController.BeginUtility(
            definition.UtilitySkillProfile,
            definition.GetResolvedRecoveryTime(resolvedSkillLevel));
        ApplyUtilitySkillInstantEffects(definition.UtilitySkillProfile);
        ApplyUtilitySkillReposition(definition.UtilitySkillProfile);
        animationController?.PlaySkillAnimation(definition);
        publishCue?.Invoke(definition.PresentationCueSet, CombatCuePhase.CastStart, character.transform.position);

        if (string.IsNullOrWhiteSpace(definition.AnimatorStateName))
            TryCompleteActiveUtility();
    }

    private void ApplyUtilitySkillInstantEffects(UtilitySkillProfile utilityProfile)
    {
        if (character == null || utilityProfile == null)
            return;

        if (utilityProfile.RefreshFlowTimer)
        {
            bool canRefreshFlow = !utilityProfile.RefreshFlowOnlyWhenStacksActive
                || character.CurrentFlowStacks > 0;
            if (canRefreshFlow)
                character.RefreshCombatFlowTimer();
        }

        if (utilityProfile.GrantsReadyState)
        {
            character.TryActivateReadyState(
                utilityProfile.GrantedReadyStateType,
                utilityProfile.GrantedReadyStateDuration);
        }
    }

    private void ApplyUtilitySkillReposition(UtilitySkillProfile utilityProfile)
    {
        if (character == null || utilityProfile == null || !utilityProfile.HasReposition)
            return;

        Vector3 dashDirection = ResolveUtilityRepositionDirection(utilityProfile);
        if (dashDirection.sqrMagnitude <= 0.0001f)
            return;

        CancelUtilityReposition();
        utilityRepositionRoutine = character.StartCoroutine(PerformUtilityReposition(
            dashDirection.normalized,
            utilityProfile.RepositionDistance,
            utilityProfile.RepositionDuration));
    }

    private Vector3 ResolveUtilityRepositionDirection(UtilitySkillProfile utilityProfile)
    {
        Transform facingTransform = visualTransform != null ? visualTransform : character != null ? character.transform : null;
        Vector3 forward = facingTransform != null && facingTransform.forward.sqrMagnitude > 0.0001f
            ? Vector3.ProjectOnPlane(facingTransform.forward, Vector3.up).normalized
            : Vector3.forward;
        if (forward.sqrMagnitude <= 0.0001f)
            forward = Vector3.forward;

        switch (utilityProfile.RepositionMode)
        {
            case UtilityRepositionMode.BackwardOnly:
                return -forward;

            case UtilityRepositionMode.ForwardOnly:
                return forward;

            case UtilityRepositionMode.InputRelative:
                if (movementController != null)
                {
                    Vector3 moveDirection = movementController.ResolveWorldMoveDirection();
                    if (moveDirection.sqrMagnitude > 0.0001f)
                        return moveDirection;
                }

                return forward;

            case UtilityRepositionMode.None:
            default:
                return Vector3.zero;
        }
    }

    private System.Collections.IEnumerator PerformUtilityReposition(
        Vector3 direction,
        float distance,
        float duration)
    {
        if (character == null)
            yield break;

        PlayerMotor motor = character.GetComponent<PlayerMotor>();
        float resolvedDuration = Mathf.Max(0.01f, duration);
        float resolvedDistance = Mathf.Max(0f, distance);
        float elapsed = 0f;

        while (elapsed < resolvedDuration && character != null && !character.IsDead)
        {
            float deltaTime = Time.deltaTime;
            elapsed = Mathf.Min(resolvedDuration, elapsed + deltaTime);
            Vector3 velocity = direction * (resolvedDistance / resolvedDuration);

            if (motor != null)
                motor.ApplyMovement(velocity);
            else
                character.transform.position += velocity * deltaTime;

            yield return null;
        }

        utilityRepositionRoutine = null;
    }

    private void CancelUtilityReposition()
    {
        if (utilityRepositionRoutine == null || character == null)
            return;

        character.StopCoroutine(utilityRepositionRoutine);
        utilityRepositionRoutine = null;
    }

    private bool TryResolveAssignedSkillDefinition(int slotIndex, out PlayerSkillDefinition definition)
    {
        definition = null;

        if (character == null)
            return false;

        PlayerSessionSkillApplicationService skillSession = bootstrap != null ? bootstrap.SkillSession : null;
        definition = skillSession != null ? skillSession.GetAssignedSkillDefinition(slotIndex) : null;
        if (definition != null)
            return true;

        if (skillSession == null)
            return false;

        IReadOnlyList<PlayerSkillDefinition> defaultSkills =
            PlayerJobCombatProfiles.GetDefaultSkillsForJob(character.CurrentJob);

        for (int index = 0; index < defaultSkills.Count; index++)
        {
            PlayerSkillDefinition fallbackDefinition = defaultSkills[index];
            if (fallbackDefinition == null)
                continue;

            if (fallbackDefinition.DefaultSlotIndex != slotIndex)
                continue;

            if (fallbackDefinition.SkillType == PlayerSkillType.Passive)
                continue;

            skillSession.TryUnlockSkill(fallbackDefinition.SkillId, 1);
            skillSession.TryAssignSkillToSlot(fallbackDefinition.SkillId, slotIndex);
            definition = skillSession.GetAssignedSkillDefinition(slotIndex);

            if (definition != null)
            {
                notifySystemMessage?.Invoke($"{definition.DisplayName} assigned to slot {slotIndex}.");
                return true;
            }
        }

        return false;
    }

    private bool ValidateSkillActivationAvailability(PlayerSkillDefinition definition, int resolvedSkillLevel)
    {
        if (definition == null || character == null)
            return false;

        PlayerSessionSkillApplicationService skillSession = bootstrap != null ? bootstrap.SkillSession : null;
        if (skillSession == null)
            return false;

        float remainingCooldown = skillSession.GetRemainingSkillCooldown(definition.SkillId);
        if (remainingCooldown > 0f)
        {
            notifySystemMessage?.Invoke($"{definition.DisplayName} cooldown {remainingCooldown:0.0}s");
            return false;
        }

        int resolvedManaCost = definition.GetResolvedManaCost(resolvedSkillLevel);
        if (!character.HasEnoughMP(resolvedManaCost))
        {
            notifySystemMessage?.Invoke($"Not enough MP for {definition.DisplayName}.");
            return false;
        }

        return true;
    }

    private bool TryConsumeSkillActivationResources(PlayerSkillDefinition definition, int resolvedSkillLevel)
    {
        if (!ValidateSkillActivationAvailability(definition, resolvedSkillLevel))
            return false;

        PlayerSessionSkillApplicationService skillSession = bootstrap != null ? bootstrap.SkillSession : null;
        if (skillSession == null)
            return false;

        int resolvedManaCost = definition.GetResolvedManaCost(resolvedSkillLevel);
        if (!character.TrySpendMP(resolvedManaCost))
            return false;

        skillSession.StartSkillCooldown(definition.SkillId, definition.GetResolvedCooldown(resolvedSkillLevel));
        return true;
    }

    private int ResolveSkillLevel(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return 1;

        PlayerSessionSkillApplicationService skillSession = bootstrap != null ? bootstrap.SkillSession : null;
        if (skillSession == null)
            return 1;

        IReadOnlyList<PlayerSkillEntry> unlockedSkills = skillSession.GetUnlockedSkills();
        for (int index = 0; index < unlockedSkills.Count; index++)
        {
            PlayerSkillEntry skillEntry = unlockedSkills[index];
            if (skillEntry == null || skillEntry.SkillId != skillId)
                continue;

            return Mathf.Max(1, skillEntry.SkillLevel);
        }

        return 1;
    }

    private static bool IsUtilitySkillDefinition(PlayerSkillDefinition definition)
    {
        return definition != null
            && definition.HasUtilitySkillProfile
            && definition.SkillType != PlayerSkillType.Passive;
    }

    private static bool IsBurstLinkedSkillDefinition(PlayerSkillDefinition definition)
    {
        return definition != null
            && definition.HasBurstLinkedSkillProfile
            && definition.SkillType == PlayerSkillType.Attack;
    }
}

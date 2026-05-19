using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerFacade playerFacade;
    [SerializeField] private PlayerCharacter playerCharacter;

    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int groundedHash = Animator.StringToHash("IsGrounded");
    private readonly int verticalHash = Animator.StringToHash("VerticalVelocity");
    private readonly int jumpHash = Animator.StringToHash("Jump");
    private readonly int landHash = Animator.StringToHash("Land");
    private readonly int attackingHash = Animator.StringToHash("IsAttacking");
    private readonly int comboHash = Animator.StringToHash("ComboIndex");
    private readonly int hitHash = Animator.StringToHash("Hit");
    private readonly int diedHash = Animator.StringToHash("IsDead");
    [SerializeField] private ParticleSystem slashVfx;
    private string activeActionStateName;
    private bool suppressLegacyCombatParameters;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (playerFacade == null)
            playerFacade = GetComponent<PlayerFacade>();

        if (playerCharacter == null)
            playerCharacter = GetComponent<PlayerCharacter>();

        if (animator == null || playerFacade == null || playerCharacter == null)
        {
            Debug.LogError("PlayerAnimationController is missing required component references.");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerHitEvent>(OnPlayerHit);
        EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        EventBus.Subscribe<PlayerRespawnedEvent>(OnPlayerRespawn);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerHitEvent>(OnPlayerHit);
        EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
        EventBus.Unsubscribe<PlayerRespawnedEvent>(OnPlayerRespawn);
    }

    public void UpdateAnimation(
        float horizontalSpeed,
        float verticalVelocity,
        bool isGrounded,
        bool jumpedThisFrame,
        bool landedThisFrame,
        int comboIndex,
        bool isAttacking,
        float attackAnimationSpeed)
    {
        animator.SetFloat(speedHash, horizontalSpeed);
        animator.SetBool(groundedHash, isGrounded);
        animator.SetFloat(verticalHash, verticalVelocity);
        if (!suppressLegacyCombatParameters || string.IsNullOrWhiteSpace(activeActionStateName))
        {
            animator.SetInteger(comboHash, comboIndex);
            animator.SetBool(attackingHash, isAttacking);
        }
        else
        {
            animator.SetInteger(comboHash, 0);
            animator.SetBool(attackingHash, false);
        }

        animator.speed = isAttacking ? Mathf.Max(0.1f, attackAnimationSpeed) : 1f;

        bool suppressInterruptingMovementTriggers =
            playerCharacter != null && playerCharacter.ShouldSuppressInterruptingAnimation;

        if (jumpedThisFrame && !suppressInterruptingMovementTriggers)
            animator.SetTrigger(jumpHash);

        if (landedThisFrame && !suppressInterruptingMovementTriggers)
            animator.SetTrigger(landHash);
    }

    // ===== Animation Events =====

    public void OnHitFrame()
    {
        if (!ShouldProcessGameplayAnimationEvents())
            return;

        playerFacade.OnHitFrame();
    }

    public void OnComboWindow()
    {
        if (!ShouldProcessGameplayAnimationEvents())
            return;

        playerFacade.OnComboWindow();
    }

    public void OnAttackEnd()
    {
        if (!ShouldProcessGameplayAnimationEvents())
            return;

        playerFacade.OnAttackEnd();
    }

    public void OnBurstChargeLoopReady()
    {
        if (!ShouldProcessGameplayAnimationEvents())
            return;

        playerFacade.OnBurstChargeLoopReady();
    }

    public void OnSwingStart()
    {
        if (!ShouldProcessGameplayAnimationEvents())
            return;

        playerFacade.OnSwingStart();
    }

    public void OnLandEffect()
    {
        if (!ShouldProcessGameplayAnimationEvents())
            return;

        playerFacade.OnLandEffect();
        EventBus.Publish(new PlayVfxEvent
        {
            Type = VfxType.PlayerLand,
            Position = transform.position,
            Rotation = Quaternion.identity
        });
    }

    public void OnFootSteps()
    {
        if (!ShouldProcessGameplayAnimationEvents())
            return;

        playerFacade.OnFootSteps();
    }

    public void OnJumpSound()
    {
        if (!ShouldProcessGameplayAnimationEvents())
            return;

        playerFacade.OnJumpSound();
    }

    public void EnableSlashVfx()
    {
        if (slashVfx == null)
            return;

        slashVfx.Clear();
        slashVfx.Play();
    }

    public void PlaySkillAnimation(PlayerSkillDefinition definition)
    {
        if (definition == null)
            return;

        PlayNamedActionAnimation(definition.AnimatorStateName);
    }

    public void PlayBasicAttackAnimation(PlayerBasicAttackProfile profile, int animationVariantIndex)
    {
        if (profile == null)
            return;

        string animatorStateName = profile.GetAnimatorStateName(animationVariantIndex);
        if (string.IsNullOrWhiteSpace(animatorStateName))
        {
            ClearActionAnimationOverride(false);
            return;
        }

        PlayNamedActionAnimation(animatorStateName);
    }

    public void PlayBurstSkillChargeAnimation(BurstLinkedSkillProfile profile)
    {
        if (profile == null)
            return;

        string entryStateName = profile.ChargeEntryAnimatorStateName;
        if (!string.IsNullOrWhiteSpace(entryStateName))
        {
            PlayNamedActionAnimation(entryStateName);
            return;
        }

        PlayNamedActionAnimation(profile.ChargeAnimatorStateName);
    }

    public void PlayBurstSkillChargeLoopAnimation(BurstLinkedSkillProfile profile)
    {
        if (profile == null)
            return;

        PlayNamedActionAnimation(profile.ChargeAnimatorStateName);
    }

    public void PlayBurstSkillReleaseAnimation(BurstLinkedSkillProfile profile)
    {
        if (profile == null)
            return;

        PlayNamedActionAnimation(profile.ReleaseAnimatorStateName);
    }

    public void ClearActionAnimationOverride(bool returnToActionIdle = true)
    {
        if (returnToActionIdle)
            ExitActionLayerToIdle();

        activeActionStateName = string.Empty;
        suppressLegacyCombatParameters = false;
    }

    public void PlayHitReaction()
    {
        if (animator == null)
            return;

        ClearActionAnimationOverride();
        animator.ResetTrigger(hitHash);
        animator.SetTrigger(hitHash);
    }

    private void OnPlayerHit(PlayerHitEvent e)
    {
        if (!MatchesPlayerEvent(e.Target, e.CharacterId))
            return;

        if (playerCharacter != null && playerCharacter.ShouldSuppressHitReactionAnimation)
            return;

        animator.ResetTrigger(hitHash);
        animator.SetTrigger(hitHash);
    }

    private void OnPlayerDied(PlayerDiedEvent e)
    {
        if (!MatchesPlayerEvent(e.Target, e.CharacterId))
            return;

        animator.ResetTrigger(jumpHash);
        animator.ResetTrigger(hitHash);
        animator.ResetTrigger(landHash);
        animator.SetBool(attackingHash, false);
        animator.speed = 1f;
        animator.SetInteger(comboHash, 0);
        animator.SetBool(diedHash, true);
        ClearActionAnimationOverride(false);
        animator.Play(ResolveDeathStateName(), 0, 0f);
    }

    private void OnPlayerRespawn(PlayerRespawnedEvent e)
    {
        if (!MatchesPlayerEvent(e.Target, e.CharacterId))
            return;

        animator.ResetTrigger(jumpHash);
        animator.ResetTrigger(hitHash);
        animator.ResetTrigger(landHash);
        
        animator.SetBool(attackingHash, false);
        animator.speed = 1f;
        animator.SetInteger(comboHash, 0);
        animator.SetBool(diedHash, false);
        ClearActionAnimationOverride(false);

        animator.Play(ResolveRespawnStateName(), 0, 0f);
    }

    private void PlayNamedActionAnimation(string animatorStateName)
    {
        if (animator == null)
            return;

        string resolvedStateName = string.IsNullOrWhiteSpace(animatorStateName)
            ? string.Empty
            : animatorStateName.Trim();
        if (string.IsNullOrWhiteSpace(resolvedStateName))
            return;

        activeActionStateName = resolvedStateName;
        suppressLegacyCombatParameters = true;

        AnimationProfile animationProfile = ResolveAnimationProfile();
        int layerIndex = animationProfile != null ? animationProfile.SkillAnimationLayerIndex : 1;
        float crossFadeDuration = animationProfile != null ? animationProfile.SkillCrossFadeDuration : 0.04f;
        float startNormalizedTime = animationProfile != null ? animationProfile.SkillStartNormalizedTime : 0f;

        animator.CrossFadeInFixedTime(activeActionStateName, crossFadeDuration, layerIndex, startNormalizedTime);
    }

    private AnimationProfile ResolveAnimationProfile()
    {
        if (playerCharacter == null)
            return null;

        PlayerJobDefinition jobDefinition = PlayerJobCombatProfiles.GetJobDefinition(playerCharacter.CurrentJob);
        return jobDefinition != null ? jobDefinition.AnimationProfile : null;
    }

    private string ResolveDeathStateName()
    {
        AnimationProfile animationProfile = ResolveAnimationProfile();
        return animationProfile != null && !string.IsNullOrWhiteSpace(animationProfile.DeathStateName)
            ? animationProfile.DeathStateName
            : AnimationProfile.DefaultDeathStateName;
    }

    private string ResolveRespawnStateName()
    {
        AnimationProfile animationProfile = ResolveAnimationProfile();
        return animationProfile != null && !string.IsNullOrWhiteSpace(animationProfile.RespawnStateName)
            ? animationProfile.RespawnStateName
            : AnimationProfile.DefaultRespawnStateName;
    }

    private bool MatchesPlayerEvent(PlayerCharacter target, string characterId)
    {
        return playerCharacter != null
            && PlayerRuntimeIdentityUtility.MatchesCharacter(
                playerCharacter,
                playerCharacter.CharacterId,
                target,
                characterId);
    }

    private bool ShouldProcessGameplayAnimationEvents()
    {
        return playerCharacter != null && playerCharacter.IsLocalPlayer;
    }

    private void ExitActionLayerToIdle()
    {
        if (animator == null)
            return;

        if (string.IsNullOrWhiteSpace(activeActionStateName))
            return;

        AnimationProfile animationProfile = ResolveAnimationProfile();
        if (animationProfile == null)
            return;

        string idleStateName = animationProfile.ActionIdleStateName;
        if (string.IsNullOrWhiteSpace(idleStateName))
            return;

        animator.CrossFadeInFixedTime(
            idleStateName,
            animationProfile.SkillCrossFadeDuration,
            animationProfile.SkillAnimationLayerIndex,
            0f);
    }
}

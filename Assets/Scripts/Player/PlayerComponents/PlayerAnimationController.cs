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
    private string activeSkillStateName;

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
        if (string.IsNullOrWhiteSpace(activeSkillStateName))
        animator.SetInteger(comboHash, comboIndex);
        animator.SetBool(attackingHash, isAttacking);
        animator.speed = isAttacking ? Mathf.Max(0.1f, attackAnimationSpeed) : 1f;

        bool suppressInterruptingMovementTriggers =
            playerCharacter != null && playerCharacter.ShouldSuppressInterruptingAnimation;

        if (jumpedThisFrame && !suppressInterruptingMovementTriggers)
            animator.SetTrigger(jumpHash);

        if (landedThisFrame && !suppressInterruptingMovementTriggers)
            animator.SetTrigger(landHash);
        if (!isAttacking && !string.IsNullOrWhiteSpace(activeSkillStateName))
            activeSkillStateName = string.Empty;
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
        if (animator == null || definition == null)
            return;

        activeSkillStateName = definition.AnimatorStateName;
        if (string.IsNullOrWhiteSpace(activeSkillStateName))
            return;

        AnimationProfile animationProfile = ResolveAnimationProfile();
        int layerIndex = animationProfile != null ? animationProfile.SkillAnimationLayerIndex : 1;
        float crossFadeDuration = animationProfile != null ? animationProfile.SkillCrossFadeDuration : 0.04f;
        float startNormalizedTime = animationProfile != null ? animationProfile.SkillStartNormalizedTime : 0f;

        animator.CrossFadeInFixedTime(activeSkillStateName, crossFadeDuration, layerIndex, startNormalizedTime);
    }

    public void ClearSkillAnimationOverride()
    {
        activeSkillStateName = string.Empty;
    }

    private void OnPlayerHit(PlayerHitEvent e)
    {
        if (!MatchesPlayerEvent(e.Target, e.CharacterId))
            return;

        if (playerCharacter != null && playerCharacter.ShouldSuppressHitReactionAnimation)
            return;

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
        activeSkillStateName = string.Empty;
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
        activeSkillStateName = string.Empty;

        animator.Play(ResolveRespawnStateName(), 0, 0f);
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
}

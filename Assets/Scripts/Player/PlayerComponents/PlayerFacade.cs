using UnityEngine;

public class PlayerFacade : MonoBehaviour
{
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private Transform visual;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerMovementController movementController;
    [SerializeField] private PlayerCombatController combatController;
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private float footstepMinInterval = 0.22f;
    [SerializeField] private PlayerCharacter character;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameBootstrap bootstrap;

    [Header("Skill Combat")]
    [SerializeField] private float skillFrontDotThreshold = 0.1f;
    [SerializeField] private float skillAreaForwardOffsetFactor = 0.55f;
    [SerializeField] private float skillAreaRadiusFactor = 0.65f;
    [SerializeField] private float skillAreaMinRadius = 0.95f;
    [SerializeField] private float lockedSkillTargetGraceRange = 0.75f;
    [SerializeField] private float attackLowerHeightAllowance = 0.6f;

    private float lastFootstepTime;
    private bool wasGrounded;
    private float presentationHorizontalSpeed;
    private float presentationVerticalVelocity;
    private bool presentationIsGrounded;
    private bool presentationIsAttacking;
    private int presentationComboIndex;
    private float presentationAttackAnimationSpeed = 1f;
    private ushort jumpPresentationSequence;
    private ushort landPresentationSequence;

    public Transform VisualTransform => visual;
    public float PresentationHorizontalSpeed => presentationHorizontalSpeed;
    public float PresentationVerticalVelocity => presentationVerticalVelocity;
    public bool PresentationIsGrounded => presentationIsGrounded;
    public bool PresentationIsAttacking => presentationIsAttacking;
    public int PresentationComboIndex => presentationComboIndex;
    public float PresentationAttackAnimationSpeed => presentationAttackAnimationSpeed;
    public ushort JumpPresentationSequence => jumpPresentationSequence;
    public ushort LandPresentationSequence => landPresentationSequence;
    public int CurrentComboCounter => combatController != null ? combatController.CurrentComboCounter : 0;
    public float SkillFrontDotThreshold => skillFrontDotThreshold;
    public float SkillAreaForwardOffsetFactor => skillAreaForwardOffsetFactor;
    public float SkillAreaRadiusFactor => skillAreaRadiusFactor;
    public float SkillAreaMinRadius => skillAreaMinRadius;
    public float LockedSkillTargetGraceRange => lockedSkillTargetGraceRange;
    public float AttackLowerHeightAllowance => attackLowerHeightAllowance;

    public void BindBootstrap(GameBootstrap sessionBootstrap)
    {
        bootstrap = sessionBootstrap;
        combatController?.BindBootstrap(sessionBootstrap);
    }

    private void Awake()
    {
        if (motor == null)
            motor = GetComponent<PlayerMotor>();

        if (character == null)
            character = GetComponent<PlayerCharacter>();

        if (animationController == null)
            animationController = GetComponent<PlayerAnimationController>();

        if (visual == null)
            visual = transform;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (motor == null || character == null || animationController == null)
        {
            Debug.LogError("PlayerFacade is missing required component references.");
            enabled = false;
            return;
        }

        if (movementController == null)
            movementController = GetComponent<PlayerMovementController>();

        if (movementController == null)
            movementController = gameObject.AddComponent<PlayerMovementController>();

        if (combatController == null)
            combatController = GetComponent<PlayerCombatController>();

        if (combatController == null)
            combatController = gameObject.AddComponent<PlayerCombatController>();

        movementController.Initialize(motor, visual, cameraTransform, character, combatController);
        combatController.Initialize(
            character,
            visual,
            animationController,
            enemyLayer,
            skillFrontDotThreshold,
            skillAreaForwardOffsetFactor,
            skillAreaRadiusFactor,
            skillAreaMinRadius,
            lockedSkillTargetGraceRange,
            attackLowerHeightAllowance);

        if (bootstrap != null)
            BindBootstrap(bootstrap);
        else
            combatController.SyncCombatProfile();

        if (!enabled)
        {
            if (movementController != null)
                movementController.enabled = false;

            if (combatController != null)
                combatController.enabled = false;
        }
    }

    private void Update()
    {
        if (character == null || !character.IsLocalPlayer || character.IsDead)
            return;

        combatController?.SyncCombatProfile();
        movementController?.Tick(Time.deltaTime);
        UpdatePresentation();
        combatController?.Tick(Time.deltaTime);
    }

    public void OnHitFrame()
    {
        combatController?.OnHitFrame();
    }

    public void OnComboWindow()
    {
        combatController?.OnComboWindow();
    }

    public void OnAttackEnd()
    {
        combatController?.OnAttackEnd();
    }

    public void OnBurstChargeLoopReady()
    {
        combatController?.OnBurstChargeLoopReady();
    }

    public void OnSwingStart()
    {
        EventBus.Publish(new PlaySfxEvent
        {
            Type = SfxType.SwordSwing,
            Position = transform.position
        });
    }

    public void OnLandEffect()
    {
        EventBus.Publish(new PlaySfxEvent
        {
            Type = SfxType.Land,
            Position = transform.position
        });
    }

    public void OnFootSteps()
    {
        bool grounded = movementController != null
            ? movementController.IsGrounded
            : motor != null && motor.IsGrounded;

        if (!grounded)
            return;

        float horizontalSpeed = movementController != null ? movementController.HorizontalSpeed : 0f;
        if (horizontalSpeed < 0.1f)
            return;

        if (combatController != null && combatController.IsAttacking)
            return;

        if (Time.time < lastFootstepTime + footstepMinInterval)
            return;

        lastFootstepTime = Time.time;

        EventBus.Publish(new PlaySfxEvent
        {
            Type = SfxType.Footstep,
            Position = transform.position
        });
    }

    public void OnJumpSound()
    {
        EventBus.Publish(new PlaySfxEvent
        {
            Type = SfxType.Jump,
            Position = transform.position
        });
    }

    private void UpdatePresentation()
    {
        bool isGrounded = movementController != null ? movementController.IsGrounded : motor != null && motor.IsGrounded;
        bool jumpedThisFrame = movementController != null && movementController.JumpedThisFrame;
        bool landedThisFrame = !wasGrounded && isGrounded;
        int comboIndex = combatController != null ? combatController.ComboIndex : 0;
        bool isAttacking = combatController != null && combatController.IsAttacking;
        float attackAnimationSpeed = combatController != null ? combatController.AttackAnimationSpeed : 1f;
        float horizontalSpeed = movementController != null ? movementController.HorizontalSpeed : 0f;
        float verticalVelocity = movementController != null ? movementController.VerticalVelocity : 0f;

        animationController.UpdateAnimation(
            horizontalSpeed,
            verticalVelocity,
            isGrounded,
            jumpedThisFrame,
            landedThisFrame,
            comboIndex,
            isAttacking,
            attackAnimationSpeed);

        presentationHorizontalSpeed = horizontalSpeed;
        presentationVerticalVelocity = verticalVelocity;
        presentationIsGrounded = isGrounded;
        presentationComboIndex = comboIndex;
        presentationIsAttacking = isAttacking;
        presentationAttackAnimationSpeed = attackAnimationSpeed;

        if (jumpedThisFrame)
            jumpPresentationSequence++;

        if (landedThisFrame)
            landPresentationSequence++;

        wasGrounded = isGrounded;
    }
}


using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

    [Header("Skill Gizmos")]
    [SerializeField] private bool drawSkillRangeGizmos = true;
    [SerializeField] private PlayerJobType previewJobForSkillGizmos = PlayerJobType.Vanguard;
    [SerializeField] private Color basicAttackGizmoColor = new Color(1f, 0.25f, 0.25f, 0.3f);
    [SerializeField] private Color meleeSkillGizmoColor = new Color(1f, 0.78f, 0.2f, 0.35f);
    [SerializeField] private Color rangedSkillGizmoColor = new Color(0.3f, 0.85f, 1f, 0.35f);

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

    private void OnDrawGizmosSelected()
    {
        if (visual == null)
            visual = transform;

        DrawBasicAttackGizmo();

        if (!drawSkillRangeGizmos)
            return;

        PlayerJobType jobForGizmos = GetSkillGizmoJob();
        IReadOnlyList<PlayerSkillDefinition> skills = PlayerJobCombatProfiles.GetDefaultSkillsForJob(jobForGizmos);

        for (int index = 0; index < skills.Count; index++)
        {
            PlayerSkillDefinition skill = skills[index];
            if (skill == null || skill.SkillType != PlayerSkillType.ActiveAttack)
                continue;

            DrawSkillGizmo(skill);
        }
    }

    private void DrawBasicAttackGizmo()
    {
        PlayerBasicAttackProfile profile = ResolveBasicAttackProfileForGizmos();
        if (profile == null)
            return;

        float resolvedRange = Mathf.Max(0.1f, profile.BaseRange);
        Transform facingTransform = visual != null ? visual : transform;
        bool isProjectileBasic = profile.ExecutionKind == CombatExecutionKind.Projectile
            || profile.ExecutionKind == CombatExecutionKind.MagicProjectile
            || profile.TargetingKind == CombatTargetingKind.ForwardProjectile;

        float forwardOffset = isProjectileBasic
            ? ProjectileProfileUtility.ResolveSpawnForwardOffset(profile.DefaultProjectileProfile)
            : Mathf.Clamp(resolvedRange * 0.5f, 0.35f, 1f);
        float upOffset = isProjectileBasic
            ? ProjectileProfileUtility.ResolveSpawnUpOffset(profile.DefaultProjectileProfile)
            : 0f;
        Vector3 origin = facingTransform.position
            + facingTransform.forward * forwardOffset
            + Vector3.up * upOffset;

        Gizmos.color = isProjectileBasic ? rangedSkillGizmoColor : basicAttackGizmoColor;
        Gizmos.DrawLine(transform.position, origin);

#if UNITY_EDITOR
        Color wireColor = isProjectileBasic
            ? new Color(rangedSkillGizmoColor.r, rangedSkillGizmoColor.g, rangedSkillGizmoColor.b, 1f)
            : new Color(basicAttackGizmoColor.r, basicAttackGizmoColor.g, basicAttackGizmoColor.b, 1f);

        if (isProjectileBasic)
        {
            Handles.color = wireColor;
            float radius = ProjectileProfileUtility.ResolveRadius(profile.DefaultProjectileProfile);
            Handles.DrawWireDisc(origin, Vector3.up, radius);
            Handles.DrawLine(origin, origin + facingTransform.forward * resolvedRange);
        }
        else
        {
            DrawUpperHemisphereGizmo(origin, resolvedRange, wireColor);
        }

        string label = $"{profile.DisplayName} ({resolvedRange:0.0})";
        Handles.Label(origin + Vector3.up * (resolvedRange + 0.12f), label);
#endif
    }

    private void DrawSkillGizmo(PlayerSkillDefinition skill)
    {
        if (skill == null || visual == null)
            return;

        switch (skill.CombatTargetingKind)
        {
            case CombatTargetingKind.Area:
                DrawMeleeSkillGizmo(skill);
                break;

            case CombatTargetingKind.ForwardProjectile:
                DrawProjectileSkillGizmo(skill);
                break;

            default:
                DrawFrontSingleTargetSkillGizmo(skill);
                break;
        }
    }

    private void DrawMeleeSkillGizmo(PlayerSkillDefinition skill)
    {
        Transform facingTransform = visual != null ? visual : transform;
        float resolvedRange = Mathf.Max(0.1f, skill.GetResolvedRange(1));
        float searchRadius = GetSkillAreaRadius(skill);
        Vector3 center = GetSkillAreaCenter(facingTransform, skill, searchRadius);

        Gizmos.color = meleeSkillGizmoColor;
        Gizmos.DrawLine(facingTransform.position, center);

#if UNITY_EDITOR
        DrawUpperHemisphereGizmo(center, searchRadius, new Color(
            meleeSkillGizmoColor.r,
            meleeSkillGizmoColor.g,
            meleeSkillGizmoColor.b,
            1f));
        Handles.Label(center + Vector3.up * (searchRadius + 0.15f), $"{skill.DisplayName} ({resolvedRange:0.0})");
#endif
    }

    private void DrawFrontSingleTargetSkillGizmo(PlayerSkillDefinition skill)
    {
        Transform facingTransform = visual != null ? visual : transform;
        Vector3 origin = facingTransform.position;
        Vector3 forward = facingTransform.forward;
        float range = Mathf.Max(0.1f, skill.GetResolvedRange(1));
        float halfAngle = Mathf.Acos(Mathf.Clamp(skillFrontDotThreshold, -1f, 1f)) * Mathf.Rad2Deg;

        Gizmos.color = rangedSkillGizmoColor;
        Gizmos.DrawLine(origin, origin + forward * range);

#if UNITY_EDITOR
        Handles.color = new Color(rangedSkillGizmoColor.r, rangedSkillGizmoColor.g, rangedSkillGizmoColor.b, 1f);
        DrawUpperHemisphereGizmo(origin, range, Handles.color);
        Vector3 fromDirection = Quaternion.AngleAxis(-halfAngle, Vector3.up) * forward;
        Handles.DrawWireArc(origin, Vector3.up, fromDirection, halfAngle * 2f, range);
        Handles.Label(origin + forward * (range + 0.2f) + Vector3.up * 0.1f, $"{skill.DisplayName} ({range:0.0})");
#endif
    }

    private void DrawProjectileSkillGizmo(PlayerSkillDefinition skill)
    {
        Transform facingTransform = visual != null ? visual : transform;
        Vector3 origin = facingTransform.position;
        Vector3 forward = facingTransform.forward;
        float range = Mathf.Max(0.1f, skill.GetResolvedRange(1));
        int projectileCount = Mathf.Max(1, skill.ProjectileCount);
        float spreadAngle = Mathf.Max(0f, skill.ProjectileSpreadAngle);
        float lateralSpacing = projectileCount > 1 ? 0.16f : 0f;
        Vector3 spawnBasePosition = facingTransform.position
            + forward * Mathf.Max(0f, skill.GetResolvedProjectileSpawnForwardOffset(1))
            + Vector3.up * skill.GetResolvedProjectileSpawnUpOffset(1);

        Gizmos.color = rangedSkillGizmoColor;
        Gizmos.DrawLine(origin, spawnBasePosition);

#if UNITY_EDITOR
        Color projectileColor = new Color(rangedSkillGizmoColor.r, rangedSkillGizmoColor.g, rangedSkillGizmoColor.b, 1f);
        DrawUpperHemisphereGizmo(origin, range, projectileColor);
        Handles.color = projectileColor;

        for (int projectileIndex = 0; projectileIndex < projectileCount; projectileIndex++)
        {
            float normalizedIndex = projectileCount == 1
                ? 0.5f
                : projectileIndex / (float)(projectileCount - 1);

            float yawOffset = projectileCount == 1
                ? 0f
                : Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, normalizedIndex);

            float lateralOffset = projectileCount == 1
                ? 0f
                : Mathf.Lerp(-lateralSpacing * 0.5f, lateralSpacing * 0.5f, normalizedIndex);

            Vector3 spawnPosition = spawnBasePosition + facingTransform.right * lateralOffset;
            Vector3 direction = Quaternion.AngleAxis(yawOffset, Vector3.up) * forward;
            Handles.DrawWireDisc(spawnPosition, Vector3.up, Mathf.Max(0.04f, skill.GetResolvedProjectileRadius(1)));
            Handles.DrawLine(spawnPosition, spawnPosition + direction * range);
        }

        Handles.Label(
            spawnBasePosition + Vector3.up * 0.18f,
            $"{skill.DisplayName} ({range:0.0})");
#endif
    }

    private float GetSkillAreaRadius(PlayerSkillDefinition definition)
    {
        return Mathf.Max(skillAreaMinRadius, definition.GetResolvedRange(1) * skillAreaRadiusFactor);
    }

    private Vector3 GetSkillAreaCenter(Transform facingTransform, PlayerSkillDefinition definition, float radius)
    {
        return facingTransform.position
            + facingTransform.forward * Mathf.Max(radius * 0.25f, definition.GetResolvedRange(1) * skillAreaForwardOffsetFactor);
    }

    private PlayerJobType GetSkillGizmoJob()
    {
        if (Application.isPlaying && character != null)
            return character.CurrentJob;

        return previewJobForSkillGizmos;
    }

    private PlayerBasicAttackProfile ResolveBasicAttackProfileForGizmos()
    {
        if (Application.isPlaying && character != null)
            return character.GetBasicAttackProfile();

        return PlayerJobCombatProfiles.GetBasicAttackProfile(GetSkillGizmoJob());
    }

#if UNITY_EDITOR
    private void DrawUpperHemisphereGizmo(Vector3 center, float radius, Color color)
    {
        Handles.color = color;
        const int ringCount = 4;
        const int meridianCount = 4;
        const int meridianSegments = 14;

        for (int ringIndex = 0; ringIndex <= ringCount; ringIndex++)
        {
            float t = ringIndex / (float)ringCount;
            float angle = Mathf.Lerp(0f, Mathf.PI * 0.5f, t);
            float ringHeight = Mathf.Sin(angle) * radius;
            float ringRadius = Mathf.Cos(angle) * radius;
            Handles.DrawWireDisc(center + Vector3.up * ringHeight, Vector3.up, ringRadius);
        }

        for (int meridianIndex = 0; meridianIndex < meridianCount; meridianIndex++)
        {
            float yaw = 180f / meridianCount * meridianIndex;
            Vector3 horizontalAxis = Quaternion.AngleAxis(yaw, Vector3.up) * Vector3.right;
            Vector3[] points = new Vector3[meridianSegments + 1];

            for (int pointIndex = 0; pointIndex <= meridianSegments; pointIndex++)
            {
                float t = pointIndex / (float)meridianSegments;
                float angle = Mathf.Lerp(0f, Mathf.PI, t);
                Vector3 point = center
                    + horizontalAxis * Mathf.Cos(angle) * radius
                    + Vector3.up * Mathf.Sin(angle) * radius;
                points[pointIndex] = point;
            }

            Handles.DrawAAPolyLine(2f, points);
        }
    }
#endif
}


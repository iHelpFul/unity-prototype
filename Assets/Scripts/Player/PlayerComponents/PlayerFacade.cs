using System.Collections;
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
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private float footstepMinInterval = 0.22f;
    [SerializeField] private PlayerCharacter character;
    [SerializeField] private float attackRange = 1.5f;
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
    [SerializeField] private PlayerJobType previewJobForSkillGizmos = PlayerJobType.Warrior;
    [SerializeField] private Color basicAttackGizmoColor = new Color(1f, 0.25f, 0.25f, 0.3f);
    [SerializeField] private Color meleeSkillGizmoColor = new Color(1f, 0.78f, 0.2f, 0.35f);
    [SerializeField] private Color rangedSkillGizmoColor = new Color(0.3f, 0.85f, 1f, 0.35f);

    private sealed class PendingSkillCast
    {
        public PlayerSkillDefinition Definition;
        public EnemyHealth LockedTarget;
    }

    private float lastFootstepTime;
    private bool wasGrounded;
    private Vector3 cameraForward;
    private Vector3 cameraRight;
    private PlayerMovementModel movementModel;
    private Vector2 moveInput;
    private PlayerCombatModule combatModule;
    private PlayerProgressionModule progression;
    private PendingSkillCast pendingSkillCast;
    private Coroutine queuedSkillHitsRoutine;
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

    private void OnEnable()
    {
        EventBus.Subscribe<MoveInputEvent>(OnMove);
        EventBus.Subscribe<JumpPressedEvent>(OnJumpPressed);
        EventBus.Subscribe<JumpReleasedEvent>(OnJumpReleased);
        EventBus.Subscribe<AttackPressedEvent>(OnAttack);
        EventBus.Subscribe<SkillSlotPressedEvent>(OnSkillSlotPressed);
        EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<MoveInputEvent>(OnMove);
        EventBus.Unsubscribe<JumpPressedEvent>(OnJumpPressed);
        EventBus.Unsubscribe<JumpReleasedEvent>(OnJumpReleased);
        EventBus.Unsubscribe<AttackPressedEvent>(OnAttack);
        EventBus.Unsubscribe<SkillSlotPressedEvent>(OnSkillSlotPressed);
        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        CancelPendingSkillState();
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

        movementModel = new PlayerMovementModel();
        combatModule = new PlayerCombatModule();

        if (motor == null || character == null || animationController == null)
        {
            Debug.LogError("PlayerFacade is missing required component references.");
            enabled = false;
            return;
        }

        ResolveBootstrap();

        if (character.IsLocalPlayer && bootstrap == null)
        {
            Debug.LogError("PlayerFacade requires an active GameBootstrap for the local player.");
            enabled = false;
            return;
        }

        PlayerRuntimeData data = character.IsLocalPlayer && bootstrap != null
            ? bootstrap.PlayerData
            : null;

        progression = new PlayerProgressionModule(data, character, bootstrap);
        combatModule.SetBasicAttackProfile(character.GetBasicAttackProfile());
    }

    private void Update()
    {
        if (!character.IsLocalPlayer)
            return;

        if (character.IsDead)
            return;

        combatModule.SetBasicAttackProfile(character.GetBasicAttackProfile());

        UpdateCameraVectors();

        Vector2 finalInput = character.IsStunned ? Vector2.zero : moveInput;

        movementModel.Tick(
            Time.deltaTime,
            motor.IsGrounded,
            finalInput,
            cameraForward,
            cameraRight
        );

        motor.ApplyMovement(movementModel.Velocity);

        if (!character.IsStunned)
            HandleRotation(Time.deltaTime);

        UpdateVisualsAndCombat();
    }

    private void UpdateCameraVectors()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform == null)
        {
            cameraForward = Vector3.forward;
            cameraRight = Vector3.right;
            return;
        }

        cameraForward = cameraTransform.forward;
        cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();
    }

    private void UpdateVisualsAndCombat()
    {
        bool isGrounded = motor.IsGrounded;
        bool jumpedThisFrame = movementModel.JumpedThisFrame;
        bool landedThisFrame = !wasGrounded && isGrounded;
        int comboIndex = combatModule.ComboIndex;
        bool isAttacking = combatModule.IsAttacking;
        float attackAnimationSpeed = combatModule.AttackAnimationSpeed;
        float horizontalSpeed = movementModel.HorizontalSpeed;
        float verticalVelocity = movementModel.VerticalVelocity;

        animationController.UpdateAnimation(
            horizontalSpeed,
            verticalVelocity,
            isGrounded,
            jumpedThisFrame,
            landedThisFrame,
            comboIndex,
            isAttacking,
            attackAnimationSpeed
        );

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
        if (!character.IsStunned)
            combatModule.Tick(Time.deltaTime);
    }

    private void OnMove(MoveInputEvent e)
    {
        if (!MatchesInputPlayer(e.Player, e.CharacterId))
            return;

        moveInput = e.Direction;
    }

    private void HandleRotation(float deltaTime)
    {
        Vector3 inputDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;
        inputDirection.y = 0f;

        if (inputDirection.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(inputDirection);

        visual.rotation = Quaternion.RotateTowards(
            visual.rotation,
            targetRotation,
            720f * deltaTime
        );
    }

    private void OnJumpPressed(JumpPressedEvent e)
    {
        if (!MatchesInputPlayer(e.Player, e.CharacterId))
            return;

        if (character.IsStunned || character.IsDead)
            return;

        movementModel.PressJump();

        if (combatModule.IsAttacking)
        {
            CancelPendingSkillState();
            combatModule.EndAttack();
        }
    }

    private void OnJumpReleased(JumpReleasedEvent e)
    {
        if (!MatchesInputPlayer(e.Player, e.CharacterId))
            return;

        movementModel.ReleaseJump();
    }

    private void OnAttack(AttackPressedEvent e)
    {
        if (!MatchesInputPlayer(e.Player, e.CharacterId))
            return;

        if (character.IsDead || character.IsStunned)
            return;

        combatModule.RequestAttack();
    }

    private void OnSkillSlotPressed(SkillSlotPressedEvent e)
    {
        if (!MatchesInputPlayer(e.Player, e.CharacterId))
            return;

        if (character.IsDead || character.IsStunned)
            return;

        ResolveBootstrap();
        if (bootstrap == null)
            return;

        if (!TryResolveAssignedSkillDefinition(e.SlotIndex, out PlayerSkillDefinition definition))
        {
            NotifySystemMessage($"No skill assigned to slot {e.SlotIndex}.");
            return;
        }

        if (definition.JobType != character.CurrentJob)
        {
            NotifySystemMessage($"{definition.DisplayName} belongs to {GameBootstrap.FormatJobName(definition.JobType)}.");
            return;
        }

        if (definition.SkillType != PlayerSkillType.ActiveAttack)
        {
            NotifySystemMessage($"{definition.DisplayName} is not wired yet.");
            return;
        }

        if (combatModule.IsAttacking)
            return;

        float remainingCooldown = bootstrap.GetRemainingSkillCooldown(definition.SkillId);
        if (remainingCooldown > 0f)
        {
            NotifySystemMessage($"{definition.DisplayName} cooldown {remainingCooldown:0.0}s");
            return;
        }

        if (!character.HasEnoughMP(definition.ManaCost))
        {
            NotifySystemMessage($"Not enough MP for {definition.DisplayName}.");
            return;
        }

        if (!combatModule.TryStartSkillAttack(definition))
            return;

        if (!character.TrySpendMP(definition.ManaCost))
        {
            animationController.ClearSkillAnimationOverride();
            combatModule.EndAttack();
            return;
        }

        bootstrap.StartSkillCooldown(definition.SkillId, definition.Cooldown);
        CancelPendingSkillState();
        pendingSkillCast = new PendingSkillCast
        {
            Definition = definition,
            LockedTarget = null
        };
        animationController.PlaySkillAnimation(definition);
    }

    private bool TryHitEnemies(int damage)
    {
        Vector3 origin = visual.position + visual.forward * 1f;
        List<EnemyHealth> enemies = GetEnemiesInSphere(origin, attackRange);
        int maxTargets = combatModule.MaxBasicTargets;
        int hitsApplied = 0;

        foreach (EnemyHealth enemy in enemies)
        {
            if (!IsWithinAllowedAttackHeight(enemy, origin.y))
                continue;

            ApplyHitToEnemy(enemy, damage, 0.05f);
            hitsApplied++;

            if (hitsApplied >= maxTargets)
                break;
        }

        return hitsApplied > 0;
    }

    public void OnHitFrame()
    {
        if (!character.IsLocalPlayer)
            return;

        if (pendingSkillCast != null)
        {
            ExecutePendingSkillCast();
            return;
        }

        PlayerCombatSnapshot snapshot = character.GetCombatSnapshot();
        int damage = combatModule.CalculateBasicDamage(snapshot);
        bool landedHit = TryHitEnemies(damage);

        if (landedHit)
            combatModule.RegisterSuccessfulBasicHit();
    }

    public void OnComboWindow()
    {
        if (!character.IsLocalPlayer)
            return;

        combatModule.OnComboWindow();
    }

    public void OnAttackEnd()
    {
        if (!character.IsLocalPlayer)
            return;

        pendingSkillCast = null;
        animationController.ClearSkillAnimationOverride();
        combatModule.EndAttack();
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
        if (!motor.IsGrounded)
            return;

        if (movementModel.HorizontalSpeed < 0.1f)
            return;

        if (combatModule.IsAttacking)
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

    private void ExecutePendingSkillCast()
    {
        PendingSkillCast skillCast = pendingSkillCast;
        pendingSkillCast = null;

        if (skillCast?.Definition == null)
            return;

        switch (skillCast.Definition.TargetingMode)
        {
            case PlayerSkillTargetingMode.MeleeArea:
                ExecuteAreaSkillHit(skillCast.Definition);
                break;

            case PlayerSkillTargetingMode.ForwardProjectile:
                ExecuteProjectileSkillCast(skillCast.Definition);
                break;

            default:
                ExecuteFrontSingleTargetSkillHit(skillCast);
                break;
        }
    }

    private void ExecuteAreaSkillHit(PlayerSkillDefinition definition)
    {
        List<EnemyHealth> targets = FindSkillAreaTargets(definition);
        if (targets.Count == 0)
            return;

        int hitCount = Mathf.Max(1, definition.HitCount);
        bool commitDeath = hitCount <= 1;
        ApplyHitsToTargets(targets, definition, 0.06f, commitDeath);

        if (hitCount > 1)
        {
            CancelQueuedSkillHits(false);
            queuedSkillHitsRoutine = StartCoroutine(PerformRepeatedAreaSkillHits(definition, hitCount - 1));
        }
    }

    private void ExecuteProjectileSkillCast(PlayerSkillDefinition definition)
    {
        if (definition == null)
            return;

        CancelQueuedSkillHits(false);

        EnemyHealth lockedTarget = FindFrontSingleTarget(definition);
        int projectileCount = Mathf.Max(1, definition.ProjectileCount);

        SpawnProjectileShot(definition, lockedTarget, 0, projectileCount);

        if (projectileCount <= 1)
            return;

        queuedSkillHitsRoutine = StartCoroutine(PerformQueuedProjectileShots(
            definition,
            lockedTarget,
            projectileCount));
    }

    private IEnumerator PerformQueuedProjectileShots(
        PlayerSkillDefinition definition,
        EnemyHealth lockedTarget,
        int projectileCount)
    {
        float interval = Mathf.Max(0.04f, definition.HitInterval > 0f ? definition.HitInterval : 0.08f);

        for (int projectileIndex = 1; projectileIndex < projectileCount; projectileIndex++)
        {
            yield return new WaitForSeconds(interval);

            if (character == null || character.IsDead || character.IsStunned)
                break;

            SpawnProjectileShot(definition, lockedTarget, projectileIndex, projectileCount);
        }

        queuedSkillHitsRoutine = null;
    }

    private void SpawnProjectileShot(
        PlayerSkillDefinition definition,
        EnemyHealth lockedTarget,
        int projectileIndex,
        int projectileCount)
    {
        Transform facingTransform = visual != null ? visual : transform;
        float spreadAngle = Mathf.Max(0f, definition.ProjectileSpreadAngle);
        float lateralSpacing = projectileCount > 1 ? 0.16f : 0f;
        Vector3 spawnBasePosition = facingTransform.position
            + facingTransform.forward * Mathf.Max(0f, definition.ProjectileSpawnForwardOffset)
            + Vector3.up * definition.ProjectileSpawnUpOffset;

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
        Vector3 direction = Quaternion.AngleAxis(yawOffset, Vector3.up) * facingTransform.forward;

        if (lockedTarget != null)
        {
            Vector3 targetDirection = GetEnemyTargetPoint(lockedTarget) - spawnPosition;
            if (targetDirection.sqrMagnitude > 0.0001f)
                direction = targetDirection.normalized;
        }

        bool commitDeathOnHit = projectileIndex >= projectileCount - 1;
        SpawnSkillProjectile(definition, spawnPosition, direction, lockedTarget, commitDeathOnHit);
    }

    private void ExecuteFrontSingleTargetSkillHit(PendingSkillCast skillCast)
    {
        EnemyHealth target = ResolveLockedSkillTarget(skillCast.Definition, skillCast.LockedTarget, true);
        if (target == null)
            return;

        int remainingHits = Mathf.Max(0, skillCast.Definition.HitCount - 1);
        bool commitDeath = remainingHits <= 0;
        ApplyHitToEnemy(target, CalculateSkillDamage(skillCast.Definition), 0.06f, commitDeath);
        skillCast.LockedTarget = target;

        if (remainingHits <= 0)
            return;

        CancelQueuedSkillHits(false);
        queuedSkillHitsRoutine = StartCoroutine(PerformRepeatedSingleTargetSkillHits(
            skillCast.Definition,
            target,
            remainingHits));
    }

    private IEnumerator PerformRepeatedSingleTargetSkillHits(
        PlayerSkillDefinition definition,
        EnemyHealth initialTarget,
        int remainingHits)
    {
        for (int hitIndex = 0; hitIndex < remainingHits; hitIndex++)
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, definition.HitInterval));

            if (character == null || character.IsDead || character.IsStunned)
                break;

            EnemyHealth target = ResolveLockedSkillTarget(definition, initialTarget, false);
            if (target == null)
                break;

            bool commitDeath = hitIndex >= remainingHits - 1;
            ApplyHitToEnemy(target, CalculateSkillDamage(definition), 0.045f, commitDeath);
        }

        queuedSkillHitsRoutine = null;
    }

    private IEnumerator PerformRepeatedAreaSkillHits(PlayerSkillDefinition definition, int remainingHits)
    {
        for (int hitIndex = 0; hitIndex < remainingHits; hitIndex++)
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, definition.HitInterval));

            if (character == null || character.IsDead || character.IsStunned)
                break;

            List<EnemyHealth> targets = FindSkillAreaTargets(definition);
            if (targets.Count == 0)
                break;

            bool commitDeath = hitIndex >= remainingHits - 1;
            ApplyHitsToTargets(targets, definition, 0.045f, commitDeath);
        }

        queuedSkillHitsRoutine = null;
    }

    private void ApplyHitsToTargets(
        List<EnemyHealth> targets,
        PlayerSkillDefinition definition,
        float impactDuration = 0.06f,
        bool commitDeath = true)
    {
        int damage = CalculateSkillDamage(definition);

        foreach (EnemyHealth target in targets)
            ApplyHitToEnemy(target, damage, impactDuration, commitDeath);
    }

    private int CalculateSkillDamage(PlayerSkillDefinition definition)
    {
        PlayerCombatSnapshot snapshot = character.GetCombatSnapshot();
        int baseDamage = combatModule.CalculateDamage(snapshot);
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * Mathf.Max(0.1f, definition.DamageMultiplier)));
    }

    private EnemyHealth FindFrontSingleTarget(PlayerSkillDefinition definition)
    {
        List<EnemyHealth> candidates = GetEnemiesInSphere(transform.position, definition.Range);

        EnemyHealth bestTarget = null;
        float bestSqrDistance = float.MaxValue;

        foreach (EnemyHealth candidate in candidates)
        {
            Vector3 targetPoint = GetEnemyTargetPoint(candidate);

            if (!IsInFront(targetPoint))
                continue;

            if (!IsWithinAllowedAttackHeight(candidate, transform.position.y))
                continue;

            float sqrDistance = (targetPoint - transform.position).sqrMagnitude;
            if (sqrDistance >= bestSqrDistance)
                continue;

            bestSqrDistance = sqrDistance;
            bestTarget = candidate;
        }

        return bestTarget;
    }

    private List<EnemyHealth> FindSkillAreaTargets(PlayerSkillDefinition definition)
    {
        float searchRadius = GetSkillAreaRadius(definition);
        Vector3 center = GetSkillAreaCenter(definition, searchRadius);

        List<EnemyHealth> candidates = GetEnemiesInSphere(center, searchRadius);
        List<EnemyHealth> validTargets = new List<EnemyHealth>();

        foreach (EnemyHealth candidate in candidates)
        {
            Vector3 targetPoint = GetEnemyTargetPoint(candidate);

            if (!IsInFront(targetPoint))
                continue;

            if (!IsWithinAllowedAttackHeight(candidate, center.y))
                continue;

            validTargets.Add(candidate);
        }

        validTargets.Sort((left, right) =>
        {
            float leftDistance = (left.transform.position - transform.position).sqrMagnitude;
            float rightDistance = (right.transform.position - transform.position).sqrMagnitude;
            return leftDistance.CompareTo(rightDistance);
        });

        int maxTargets = Mathf.Max(1, definition.MaxTargets);
        if (validTargets.Count > maxTargets)
            validTargets.RemoveRange(maxTargets, validTargets.Count - maxTargets);

        return validTargets;
    }

    private EnemyHealth ResolveLockedSkillTarget(
        PlayerSkillDefinition definition,
        EnemyHealth lockedTarget,
        bool allowReacquire)
    {
        if (lockedTarget != null && !lockedTarget.IsDead)
        {
            Vector3 targetPoint = GetEnemyTargetPoint(lockedTarget);

            if (!allowReacquire)
                return lockedTarget;

            if (!IsInFront(targetPoint))
                return allowReacquire ? FindFrontSingleTarget(definition) : null;

            if (!IsWithinAllowedAttackHeight(lockedTarget, transform.position.y))
                return allowReacquire ? FindFrontSingleTarget(definition) : null;

            float maxDistance = definition.Range + lockedSkillTargetGraceRange;
            float sqrMaxDistance = maxDistance * maxDistance;
            float sqrDistance = (targetPoint - transform.position).sqrMagnitude;

            if (sqrDistance <= sqrMaxDistance)
                return lockedTarget;
        }

        return allowReacquire ? FindFrontSingleTarget(definition) : null;
    }

    private List<EnemyHealth> GetEnemiesInSphere(Vector3 center, float radius)
    {
        Collider[] hits = Physics.OverlapSphere(center, radius, enemyLayer);
        List<EnemyHealth> enemies = new List<EnemyHealth>(hits.Length);
        HashSet<EnemyHealth> seenEnemies = new HashSet<EnemyHealth>();

        foreach (Collider hit in hits)
        {
            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead || !seenEnemies.Add(enemy))
                continue;

            enemies.Add(enemy);
        }

        enemies.Sort((left, right) =>
        {
            float leftDistance = (left.transform.position - center).sqrMagnitude;
            float rightDistance = (right.transform.position - center).sqrMagnitude;
            return leftDistance.CompareTo(rightDistance);
        });

        return enemies;
    }

    private float GetSkillAreaRadius(PlayerSkillDefinition definition)
    {
        return Mathf.Max(skillAreaMinRadius, definition.Range * skillAreaRadiusFactor);
    }

    private Vector3 GetSkillAreaCenter(PlayerSkillDefinition definition, float radius)
    {
        Transform facingTransform = visual != null ? visual : transform;
        return facingTransform.position
            + facingTransform.forward * Mathf.Max(radius * 0.25f, definition.Range * skillAreaForwardOffsetFactor);
    }

    private Vector3 GetEnemyTargetPoint(EnemyHealth enemy)
    {
        if (enemy == null)
            return transform.position;

        Collider enemyCollider = enemy.GetComponentInChildren<Collider>();
        if (enemyCollider != null)
            return enemyCollider.bounds.center;

        return enemy.transform.position + Vector3.up * 0.5f;
    }

    private float GetEnemyTopY(EnemyHealth enemy)
    {
        if (enemy == null)
            return transform.position.y;

        Collider enemyCollider = enemy.GetComponentInChildren<Collider>();
        if (enemyCollider != null)
            return enemyCollider.bounds.max.y;

        return enemy.transform.position.y + 1f;
    }

    private bool IsInFront(Vector3 targetPosition)
    {
        Transform facingTransform = visual != null ? visual : transform;
        Vector3 directionToTarget = targetPosition - facingTransform.position;
        directionToTarget.y = 0f;

        if (directionToTarget.sqrMagnitude <= 0.0001f)
            return true;

        directionToTarget.Normalize();
        return Vector3.Dot(facingTransform.forward, directionToTarget) >= skillFrontDotThreshold;
    }

    private bool IsWithinAllowedAttackHeight(EnemyHealth enemy, float referenceY)
    {
        return GetEnemyTopY(enemy) + attackLowerHeightAllowance >= referenceY;
    }

    private void ApplyHitToEnemy(EnemyHealth enemy, int damage, float impactDuration, bool commitDeath = true)
    {
        if (enemy == null || enemy.IsDead)
            return;

        float direction = Mathf.Sign(enemy.transform.position.x - transform.position.x);

        EventBus.Publish(new HitImpactEvent
        {
            Duration = impactDuration,
            TimeScale = 0.1f,
            Damage = damage
        });

        if (MultiplayerPrototypeEnemyCoordinator.TryRequestDamage(enemy, damage, direction, character, commitDeath))
            return;

        enemy.TakeDamage(damage, direction, character, commitDeath);
    }

    private void SpawnSkillProjectile(
        PlayerSkillDefinition definition,
        Vector3 spawnPosition,
        Vector3 direction,
        EnemyHealth lockedTarget,
        bool commitDeathOnHit)
    {
        if (definition == null)
            return;

        GameObject projectileObject = new GameObject($"{definition.SkillId}_Projectile");
        projectileObject.transform.position = spawnPosition;
        projectileObject.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        PlayerSkillProjectile projectile = projectileObject.AddComponent<PlayerSkillProjectile>();
        projectile.Initialize(
            character,
            lockedTarget,
            enemyLayer,
            CalculateSkillDamage(definition),
            direction,
            definition.ProjectileSpeed,
            definition.ProjectileRadius,
            definition.ProjectileLifetime,
            definition.ProjectileVisualScale,
            commitDeathOnHit);
    }

    private void NotifySystemMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        EventBus.Publish(new GameplayNotificationEvent
        {
            Target = character,
            CharacterId = character != null ? character.CharacterId : string.Empty,
            Category = GameplayNotificationCategory.System,
            Message = message
        });
    }

    private void CancelPendingSkillState(bool cancelQueuedHits = true)
    {
        pendingSkillCast = null;
        animationController?.ClearSkillAnimationOverride();

        if (cancelQueuedHits)
            CancelQueuedSkillHits();
    }

    private void CancelQueuedSkillHits(bool clearReference = true)
    {
        if (queuedSkillHitsRoutine != null)
            StopCoroutine(queuedSkillHitsRoutine);

        if (clearReference)
            queuedSkillHitsRoutine = null;
    }

    private void OnEnemyDied(EnemyDiedEvent e)
    {
        if (e.Killer != character)
            return;

        progression?.AddExp(e.ExpReward);
    }

    private void ResolveBootstrap()
    {
        bootstrap = GameBootstrap.FindReadyBootstrap(bootstrap);
    }

    private bool MatchesInputPlayer(PlayerCharacter player, string characterId)
    {
        return character != null
            && PlayerRuntimeIdentityUtility.MatchesCharacter(
                character,
                character.CharacterId,
                player,
                characterId);
    }

    private bool TryResolveAssignedSkillDefinition(int slotIndex, out PlayerSkillDefinition definition)
    {
        definition = bootstrap != null ? bootstrap.GetAssignedSkillDefinition(slotIndex) : null;
        if (definition != null)
            return true;

        IReadOnlyList<PlayerSkillDefinition> defaultSkills =
            PlayerJobCombatProfiles.GetDefaultSkillsForJob(character.CurrentJob);

        for (int index = 0; index < defaultSkills.Count; index++)
        {
            PlayerSkillDefinition fallbackDefinition = defaultSkills[index];
            if (fallbackDefinition == null)
                continue;

            if (fallbackDefinition.DefaultSlotIndex != slotIndex)
                continue;

            if (fallbackDefinition.SkillType != PlayerSkillType.ActiveAttack)
                continue;

            bootstrap.TryUnlockSkill(character, fallbackDefinition.SkillId);
            bootstrap.TryAssignSkillToSlot(character, fallbackDefinition.SkillId, slotIndex);
            definition = bootstrap.GetAssignedSkillDefinition(slotIndex);

            if (definition != null)
            {
                NotifySystemMessage($"{definition.DisplayName} assigned to slot {slotIndex}.");
                return true;
            }
        }

        return false;
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
        Gizmos.color = basicAttackGizmoColor;

        Vector3 origin = visual.position + visual.forward * 1f;
        Gizmos.DrawLine(transform.position, origin);

#if UNITY_EDITOR
        DrawUpperHemisphereGizmo(origin, attackRange, new Color(
            basicAttackGizmoColor.r,
            basicAttackGizmoColor.g,
            basicAttackGizmoColor.b,
            1f));
#endif
    }

    private void DrawSkillGizmo(PlayerSkillDefinition skill)
    {
        if (skill == null || visual == null)
            return;

        switch (skill.TargetingMode)
        {
            case PlayerSkillTargetingMode.MeleeArea:
                DrawMeleeSkillGizmo(skill);
                break;

            case PlayerSkillTargetingMode.ForwardProjectile:
                DrawProjectileSkillGizmo(skill);
                break;

            default:
                DrawFrontSingleTargetSkillGizmo(skill);
                break;
        }
    }

    private void DrawMeleeSkillGizmo(PlayerSkillDefinition skill)
    {
        float searchRadius = GetSkillAreaRadius(skill);
        Vector3 center = GetSkillAreaCenter(skill, searchRadius);

        Gizmos.color = meleeSkillGizmoColor;
        Gizmos.DrawLine(visual.position, center);

#if UNITY_EDITOR
        DrawUpperHemisphereGizmo(center, searchRadius, new Color(
            meleeSkillGizmoColor.r,
            meleeSkillGizmoColor.g,
            meleeSkillGizmoColor.b,
            1f));
        Handles.Label(center + Vector3.up * (searchRadius + 0.15f), $"{skill.DisplayName} ({skill.Range:0.0})");
#endif
    }

    private void DrawFrontSingleTargetSkillGizmo(PlayerSkillDefinition skill)
    {
        Vector3 origin = visual.position;
        Vector3 forward = visual.forward;
        float range = Mathf.Max(0.1f, skill.Range);
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
        Vector3 origin = visual.position;
        Vector3 forward = visual.forward;
        float range = Mathf.Max(0.1f, skill.Range);
        int projectileCount = Mathf.Max(1, skill.ProjectileCount);
        float spreadAngle = Mathf.Max(0f, skill.ProjectileSpreadAngle);
        float lateralSpacing = projectileCount > 1 ? 0.16f : 0f;
        Vector3 spawnBasePosition = visual.position
            + forward * Mathf.Max(0f, skill.ProjectileSpawnForwardOffset)
            + Vector3.up * skill.ProjectileSpawnUpOffset;

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

            Vector3 spawnPosition = spawnBasePosition + visual.right * lateralOffset;
            Vector3 direction = Quaternion.AngleAxis(yawOffset, Vector3.up) * forward;
            Handles.DrawWireDisc(spawnPosition, Vector3.up, Mathf.Max(0.04f, skill.ProjectileRadius));
            Handles.DrawLine(spawnPosition, spawnPosition + direction * range);
        }

        Handles.Label(
            spawnBasePosition + Vector3.up * 0.18f,
            $"{skill.DisplayName} ({range:0.0})");
#endif
    }

    private PlayerJobType GetSkillGizmoJob()
    {
        if (Application.isPlaying && character != null)
            return character.CurrentJob;

        return previewJobForSkillGizmos;
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

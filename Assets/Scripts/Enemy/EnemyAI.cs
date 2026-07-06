using UnityEngine;

public enum EnemyMovementPlaneMode
{
    LockedZ = 0,
    Full3D = 1
}

[RequireComponent(typeof(CharacterController))]
public class EnemyAI : MonoBehaviour
{
    [Header("Movement Plane")]
    [SerializeField] private EnemyMovementPlaneMode movementPlaneMode = EnemyMovementPlaneMode.LockedZ;
    [SerializeField] private float patrolHalfDepth = 2.5f;

    [Header("Patrol Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Transform leftBoundary;
    [SerializeField] private Transform rightBoundary;
    [SerializeField] private float patrolHalfWidth = 2.5f;
    [SerializeField] private float patrolEdgePadding = 0.1f;
    [SerializeField] private float minPatrolPauseDuration = 0.45f;
    [SerializeField] private float maxPatrolPauseDuration = 0.9f;
    [SerializeField] private float minPatrolMoveDuration = 1.1f;
    [SerializeField] private float maxPatrolMoveDuration = 2.3f;

    [Header("Reactive Aggro Settings")]
    [SerializeField] private EnemyAttackType attackType = EnemyAttackType.Contact;
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float loseAggroDelay = 2.25f;
    [SerializeField] private float chaseBoundsPadding = 1.2f;

    [Header("Animated Attack Settings")]
    [SerializeField] private float attackRangeForward = 1.5f;
    [SerializeField] private float attackRangeUpward = 1.25f;
    [SerializeField] private float attackHitboxForwardOffset = 0.85f;
    [SerializeField] private float attackHitboxWidth = 1.15f;
    [SerializeField] private float attackHitboxHeight = 1.1f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float attackRecoveryDuration = 0.18f;
    [SerializeField] private float attackHitFallbackDelay = 0.28f;
    [SerializeField] private float attackAnimationTimeout = 1.05f;

    [Header("References")]
    [SerializeField] private EnemyAnimationController animationController;
    [SerializeField] private float gravity = -20f;

    private int attackDamage = 15;
    private float verticalVelocity;
    private bool isDead;
    private PlayerCharacter targetPlayer;
    private CharacterController controller;
    private EnemyHealth enemyHealth;
    private float lastAttackTime;
    private float lockedZ;
    private float spawnX;
    private Vector3 spawnPosition;
    private float patrolDirection = 1f;
    private Vector3 patrolPlanarDirection;
    private float patrolPauseTimer;
    private float nextPatrolPauseTime;
    private float loseAggroTimer;
    private bool isAggroActive;
    private bool isAttackInProgress;
    private bool didResolveCurrentAttackHit;
    private float attackRecoveryTimer;
    private float attackFallbackHitTimer;
    private float attackAnimationTimer;
    private float externalMovementLockTimer;
    private EnemyRole resolvedRole;
    private bool isSentinelReturningToAnchor;
    private float resolvedMoveSpeed;
    private EnemyAttackType resolvedAttackType;
    private float resolvedDetectionRange;
    private float resolvedLoseAggroDelay;
    private float resolvedChaseBoundsPadding;
    private float resolvedAttackCooldown;
    private float resolvedAttackRecoveryDuration;

    public bool IsDead => isDead;
    public bool IsAggroActive => isAggroActive;
    public bool IsExternallyLocked => externalMovementLockTimer > 0f;
    public EnemyMovementPlaneMode MovementPlaneMode => movementPlaneMode;

    public void SetMovementPlaneMode(EnemyMovementPlaneMode planeMode, bool recacheMovementAnchor = true)
    {
        movementPlaneMode = planeMode;

        if (!recacheMovementAnchor)
            return;

        lockedZ = transform.position.z;
        spawnX = transform.position.x;
        spawnPosition = transform.position;
        ChooseRandomPatrolPlanarDirection();
        ScheduleNextPatrolPause();
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animationController = GetComponent<EnemyAnimationController>();
        enemyHealth = GetComponent<EnemyHealth>();
        lockedZ = transform.position.z;
        spawnX = transform.position.x;
        spawnPosition = transform.position;
        patrolDirection = Random.value < 0.5f ? -1f : 1f;
        ChooseRandomPatrolPlanarDirection();
        ScheduleNextPatrolPause();
        RefreshConfiguredStats();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    private void Update()
    {
        if (isDead || controller == null || !controller.enabled)
            return;

        if (externalMovementLockTimer > 0f)
        {
            externalMovementLockTimer -= Time.deltaTime;
            if (!IsFull3DMovement())
                LockZAxis();

            animationController?.SetSpeed(0f);
            return;
        }

        if (attackRecoveryTimer > 0f)
            attackRecoveryTimer -= Time.deltaTime;

        if (isAttackInProgress)
        {
            attackAnimationTimer -= Time.deltaTime;

            if (!didResolveCurrentAttackHit)
            {
                attackFallbackHitTimer -= Time.deltaTime;

                if (attackFallbackHitTimer <= 0f)
                    OnAttackHitFrame();
            }

            if (attackAnimationTimer <= 0f)
                EndAnimatedAttack();
        }

        UpdateAggroState();

        Vector3 planarVelocity = Vector3.zero;

        if (!isAttackInProgress)
        {
            planarVelocity = isAggroActive
                ? GetAggroPlanarVelocity()
                : GetPatrolPlanarVelocity();
        }
        else if (IsTargetValid(targetPlayer))
        {
            FaceTargetPosition(targetPlayer.transform.position);
        }

        ApplyMovement(planarVelocity);
    }

    private void BeginAnimatedAttack()
    {
        if (!CanStartAnimatedAttack())
            return;

        lastAttackTime = Time.time;
        isAttackInProgress = true;
        didResolveCurrentAttackHit = false;
        attackFallbackHitTimer = Mathf.Max(0.05f, attackHitFallbackDelay);
        attackAnimationTimer = Mathf.Max(0.15f, attackAnimationTimeout);
        attackRecoveryTimer = 0f;
        patrolPauseTimer = 0f;

        if (IsTargetValid(targetPlayer))
            FaceTargetPosition(targetPlayer.transform.position);

        animationController?.PlayAttack();
        animationController?.SetSpeed(0f);
    }

    public void OnAttackHitFrame()
    {
        if (!isAttackInProgress || didResolveCurrentAttackHit || isDead)
            return;

        didResolveCurrentAttackHit = true;
        TryApplyAnimatedAttackHit();
    }

    public void OnAttackAnimationComplete()
    {
        EndAnimatedAttack();
    }

    private void TryApplyAnimatedAttackHit()
    {
        Collider[] hitColliders = Physics.OverlapBox(
            GetAttackHitboxCenter(),
            GetAttackHitboxHalfExtents(),
            GetAttackHitboxRotation(),
            ~0,
            QueryTriggerInteraction.Collide);

        for (int index = 0; index < hitColliders.Length; index++)
        {
            PlayerCharacter hitTarget = hitColliders[index].GetComponentInParent<PlayerCharacter>();
            if (!IsDamageableAttackTarget(hitTarget))
                continue;

            PublishAnimatedAttackDamage(hitTarget);
            return;
        }

        if (IsDamageableAttackTarget(targetPlayer) && IsInsideAttackHitbox(targetPlayer.transform.position))
            PublishAnimatedAttackDamage(targetPlayer);
    }

    private void PublishAnimatedAttackDamage(PlayerCharacter hitTarget)
    {
        if (MultiplayerPrototypeRuntime.IsEnabled)
            return;

        EventBus.Publish(new PlayerDamagedEvent
        {
            Target = hitTarget,
            CharacterId = hitTarget != null ? hitTarget.CharacterId : string.Empty,
            Source = transform,
            Damage = attackDamage,
            HitDirection = transform.position.x
        });
    }

    private void FaceTarget(float direction)
    {
        if (direction == 0f)
            return;

        transform.rotation = Quaternion.LookRotation(new Vector3(direction, 0f, 0f));
    }

    private void FaceTargetPosition(Vector3 worldPosition)
    {
        Vector3 direction = worldPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        FacePlanarDirection(direction);
    }

    private void FacePlanarDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void ApplyMovement(Vector3 planarVelocity)
    {
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        planarVelocity.y = 0f;
        Vector3 motion = planarVelocity + Vector3.up * verticalVelocity;
        controller.Move(motion * Time.deltaTime);

        if (IsFull3DMovement())
        {
            ClampPlanarPosition();

            if (planarVelocity.sqrMagnitude > 0.0001f)
                FacePlanarDirection(planarVelocity);
        }
        else
        {
            ClampHorizontalPosition();
            LockZAxis();

            if (Mathf.Abs(planarVelocity.x) > 0.001f)
                FaceTarget(Mathf.Sign(planarVelocity.x));
        }

        animationController?.SetSpeed(planarVelocity.magnitude);
    }

    private void OnEnemyDied(EnemyDiedEvent e)
    {
        if (e.Enemy != transform)
            return;

        isDead = true;

        if (animationController != null)
            animationController.SetSpeed(0f);

        if (controller != null && controller.enabled)
            controller.enabled = false;

        isAttackInProgress = false;
        didResolveCurrentAttackHit = false;
        attackRecoveryTimer = 0f;
        attackFallbackHitTimer = 0f;
        attackAnimationTimer = 0f;
        externalMovementLockTimer = 0f;
        isSentinelReturningToAnchor = false;
    }

    public void ResetAfterRespawn()
    {
        isDead = false;
        verticalVelocity = 0f;
        lastAttackTime = Time.time + 0.35f;
        lockedZ = transform.position.z;
        spawnX = transform.position.x;
        spawnPosition = transform.position;
        targetPlayer = null;
        isAggroActive = false;
        loseAggroTimer = 0f;
        patrolPauseTimer = 0f;
        patrolDirection = Random.value < 0.5f ? -1f : 1f;
        ChooseRandomPatrolPlanarDirection();
        isAttackInProgress = false;
        didResolveCurrentAttackHit = false;
        attackRecoveryTimer = 0f;
        attackFallbackHitTimer = 0f;
        attackAnimationTimer = 0f;
        externalMovementLockTimer = 0f;
        isSentinelReturningToAnchor = false;
        ScheduleNextPatrolPause();
        RefreshConfiguredStats();

        if (animationController != null)
            animationController.SetSpeed(0f);
    }

    public void RefreshConfiguredStats()
    {
        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        if (enemyHealth != null && enemyHealth.Stats != null)
        {
            attackDamage = enemyHealth.Stats.AnimatedAttackDamage;
            resolvedRole = enemyHealth.Stats.Role;
            isSentinelReturningToAnchor = false;

            EnemyCadenceProfile cadence = enemyHealth.Stats.CadenceProfile;
            resolvedAttackType = cadence.AttackType;
            resolvedMoveSpeed = cadence.MoveSpeed;
            resolvedDetectionRange = cadence.DetectionRange;
            resolvedLoseAggroDelay = cadence.LoseAggroDelay;
            resolvedChaseBoundsPadding = cadence.ChaseBoundsPadding;
            resolvedAttackCooldown = cadence.AttackCooldown;
            resolvedAttackRecoveryDuration = cadence.AttackRecoveryDuration;
            return;
        }

        resolvedRole = EnemyRole.Skirmisher;
        isSentinelReturningToAnchor = false;
        resolvedAttackType = attackType;
        resolvedMoveSpeed = Mathf.Max(0.05f, moveSpeed);
        resolvedDetectionRange = Mathf.Max(0.1f, detectionRange);
        resolvedLoseAggroDelay = Mathf.Max(0.1f, loseAggroDelay);
        resolvedChaseBoundsPadding = Mathf.Max(0f, chaseBoundsPadding);
        resolvedAttackCooldown = Mathf.Max(0f, attackCooldown);
        resolvedAttackRecoveryDuration = Mathf.Max(0f, attackRecoveryDuration);
    }

    public void NotifyExternalKnockback(float duration)
    {
        ApplyExternalMovementLock(duration);
    }

    public void NotifyHitReactionLock(float duration)
    {
        ApplyExternalMovementLock(duration);
    }

    private void ApplyExternalMovementLock(float duration)
    {
        externalMovementLockTimer = Mathf.Max(externalMovementLockTimer, Mathf.Max(0f, duration));

        if (isAttackInProgress)
            EndAnimatedAttack();
    }

    public float ClampHorizontalToMovementBounds(float worldX)
    {
        float minX = isAggroActive
            ? GetLeftPatrolBoundaryX() - GetEffectiveChaseBoundsPadding()
            : GetLeftPatrolBoundaryX();
        float maxX = isAggroActive
            ? GetRightPatrolBoundaryX() + GetEffectiveChaseBoundsPadding()
            : GetRightPatrolBoundaryX();

        return Mathf.Clamp(worldX, minX, maxX);
    }

    private void OnValidate()
    {
        patrolHalfDepth = Mathf.Max(0.2f, patrolHalfDepth);
        moveSpeed = Mathf.Max(0.05f, moveSpeed);
        patrolHalfWidth = Mathf.Max(0.2f, patrolHalfWidth);
        patrolEdgePadding = Mathf.Max(0f, patrolEdgePadding);
        minPatrolPauseDuration = Mathf.Max(0f, minPatrolPauseDuration);
        maxPatrolPauseDuration = Mathf.Max(minPatrolPauseDuration, maxPatrolPauseDuration);
        minPatrolMoveDuration = Mathf.Max(0.1f, minPatrolMoveDuration);
        maxPatrolMoveDuration = Mathf.Max(minPatrolMoveDuration, maxPatrolMoveDuration);

        detectionRange = Mathf.Max(0.1f, detectionRange);
        loseAggroDelay = Mathf.Max(0.1f, loseAggroDelay);
        chaseBoundsPadding = Mathf.Max(0f, chaseBoundsPadding);

        attackRangeForward = Mathf.Max(0.1f, attackRangeForward);
        attackRangeUpward = Mathf.Max(0.1f, attackRangeUpward);
        attackHitboxForwardOffset = Mathf.Max(0.05f, attackHitboxForwardOffset);
        attackHitboxWidth = Mathf.Max(0.05f, attackHitboxWidth);
        attackHitboxHeight = Mathf.Max(0.05f, attackHitboxHeight);
        attackCooldown = Mathf.Max(0f, attackCooldown);
        attackRecoveryDuration = Mathf.Max(0f, attackRecoveryDuration);
        attackHitFallbackDelay = Mathf.Max(0.01f, attackHitFallbackDelay);
        attackAnimationTimeout = Mathf.Max(0.1f, attackAnimationTimeout);
        gravity = Mathf.Min(-0.1f, gravity);

        RefreshConfiguredStats();
    }

    public void NotifyProvoked(PlayerCharacter attacker)
    {
        if (!IsTargetValid(attacker))
            return;

        targetPlayer = attacker;
        isAggroActive = true;
        loseAggroTimer = Mathf.Max(0.1f, resolvedLoseAggroDelay);
        patrolPauseTimer = 0f;
    }

    private void UpdateAggroState()
    {
        if (!isAggroActive)
        {
            if (!IsTargetValid(targetPlayer))
                targetPlayer = null;

            return;
        }

        if (!IsTargetValid(targetPlayer))
        {
            ClearAggro();
            return;
        }

        float sqrDistance = (targetPlayer.transform.position - transform.position).sqrMagnitude;
        float allowedRange = Mathf.Max(0.5f, resolvedDetectionRange);

        if (sqrDistance <= allowedRange * allowedRange)
        {
            loseAggroTimer = Mathf.Max(0.1f, resolvedLoseAggroDelay);
            return;
        }

        loseAggroTimer -= Time.deltaTime;

        if (loseAggroTimer <= 0f)
            ClearAggro();
    }

    private bool IsTargetValid(PlayerCharacter candidate)
    {
        return candidate != null
            && candidate.IsLocalPlayer
            && candidate.isActiveAndEnabled
            && !candidate.IsDead;
    }

    private bool CanStartAnimatedAttack()
    {
        if (!IsTargetValid(targetPlayer))
            return false;

        if (resolvedAttackType != EnemyAttackType.Animated)
            return false;

        if (isAttackInProgress || attackRecoveryTimer > 0f)
            return false;

        if (Time.time < lastAttackTime + resolvedAttackCooldown)
            return false;

        float planarDistanceToPlayer = IsFull3DMovement()
            ? GetPlanarDistanceTo(targetPlayer.transform.position)
            : Mathf.Abs(targetPlayer.transform.position.x - transform.position.x);

        if (planarDistanceToPlayer > attackRangeForward)
            return false;

        float verticalDistanceToPlayer = Mathf.Abs(targetPlayer.transform.position.y - transform.position.y);
        return verticalDistanceToPlayer <= attackRangeUpward;
    }

    private float GetPatrolHorizontalSpeed()
    {
        if (patrolPauseTimer > 0f)
        {
            patrolPauseTimer -= Time.deltaTime;

            if (patrolPauseTimer <= 0f)
                ScheduleNextPatrolPause();

            return 0f;
        }

        float leftX = GetLeftPatrolBoundaryX();
        float rightX = GetRightPatrolBoundaryX();
        float currentX = transform.position.x;

        if (currentX <= leftX + patrolEdgePadding && patrolDirection < 0f)
        {
            patrolDirection = 1f;
            BeginPatrolPause();
            return 0f;
        }

        if (currentX >= rightX - patrolEdgePadding && patrolDirection > 0f)
        {
            patrolDirection = -1f;
            BeginPatrolPause();
            return 0f;
        }

        if (Time.time >= nextPatrolPauseTime)
        {
            BeginPatrolPause();
            return 0f;
        }

        return patrolDirection * resolvedMoveSpeed;
    }

    private float GetAggroHorizontalSpeed()
    {
        if (!IsTargetValid(targetPlayer))
            return 0f;

        float roleMoveSpeed = ResolveRoleMoveSpeed();
        if (resolvedAttackType == EnemyAttackType.Animated)
        {
            if (CanStartAnimatedAttack())
            {
                BeginAnimatedAttack();
                return 0f;
            }

            float deltaXToPlayer = targetPlayer.transform.position.x - transform.position.x;
            float absoluteDeltaToPlayer = Mathf.Abs(deltaXToPlayer);

            if (resolvedRole == EnemyRole.Skirmisher)
            {
                float retreatThreshold = Mathf.Max(0.35f, attackRangeForward * 0.45f);
                float holdThreshold = Mathf.Max(retreatThreshold + 0.2f, attackRangeForward * 0.9f);

                if (absoluteDeltaToPlayer < retreatThreshold)
                    return -ResolveHorizontalDirection(deltaXToPlayer) * roleMoveSpeed * 0.85f;

                if (absoluteDeltaToPlayer <= holdThreshold)
                    return 0f;
            }
            else if (resolvedRole == EnemyRole.Sentinel)
            {
                float anchorLeash = ResolveSentinelAnchorLeash();
                float anchorReturnThreshold = Mathf.Max(0.1f, anchorLeash * 0.55f);
                float distanceFromAnchor = Mathf.Abs(transform.position.x - spawnX);

                if (isSentinelReturningToAnchor)
                {
                    if (distanceFromAnchor <= anchorReturnThreshold)
                    {
                        isSentinelReturningToAnchor = false;
                    }
                    else
                    {
                        return Mathf.Sign(spawnX - transform.position.x) * roleMoveSpeed * 0.9f;
                    }
                }

                if (distanceFromAnchor > anchorLeash && absoluteDeltaToPlayer > attackRangeForward * 0.85f)
                {
                    isSentinelReturningToAnchor = true;
                    return Mathf.Sign(spawnX - transform.position.x) * roleMoveSpeed * 0.9f;
                }

                if (absoluteDeltaToPlayer <= attackRangeForward * 0.95f)
                    return 0f;
            }

            if (attackRecoveryTimer > 0f)
            {
                if (resolvedRole == EnemyRole.Bruiser && absoluteDeltaToPlayer > Mathf.Max(0.01f, patrolEdgePadding))
                    return ResolveHorizontalDirection(deltaXToPlayer) * roleMoveSpeed * 0.75f;

                return 0f;
            }
        }

        float chasePadding = GetEffectiveChaseBoundsPadding();
        float minChaseX = GetLeftPatrolBoundaryX() - chasePadding;
        float maxChaseX = GetRightPatrolBoundaryX() + chasePadding;
        float desiredTargetX = Mathf.Clamp(targetPlayer.transform.position.x, minChaseX, maxChaseX);
        float deltaX = desiredTargetX - transform.position.x;
        float stopDistance = resolvedRole == EnemyRole.Bruiser
            ? patrolEdgePadding * 0.35f
            : patrolEdgePadding;

        if (Mathf.Abs(deltaX) <= Mathf.Max(0.01f, stopDistance))
            return 0f;

        return ResolveHorizontalDirection(deltaX) * roleMoveSpeed;
    }

    private Vector3 GetPatrolPlanarVelocity()
    {
        if (!IsFull3DMovement())
            return new Vector3(GetPatrolHorizontalSpeed(), 0f, 0f);

        return GetFull3DPatrolVelocity();
    }

    private Vector3 GetAggroPlanarVelocity()
    {
        if (!IsFull3DMovement())
            return new Vector3(GetAggroHorizontalSpeed(), 0f, 0f);

        if (!IsTargetValid(targetPlayer))
            return Vector3.zero;

        float roleMoveSpeed = ResolveRoleMoveSpeed();
        Vector3 deltaToPlayer = GetPlanarDeltaTo(targetPlayer.transform.position);
        float distanceToPlayer = deltaToPlayer.magnitude;

        if (resolvedAttackType == EnemyAttackType.Animated)
        {
            if (CanStartAnimatedAttack())
            {
                BeginAnimatedAttack();
                return Vector3.zero;
            }

            Vector3 directionToPlayer = distanceToPlayer > 0.001f
                ? deltaToPlayer / distanceToPlayer
                : transform.forward;

            if (resolvedRole == EnemyRole.Skirmisher)
            {
                float retreatThreshold = Mathf.Max(0.35f, attackRangeForward * 0.45f);
                float holdThreshold = Mathf.Max(retreatThreshold + 0.2f, attackRangeForward * 0.9f);

                if (distanceToPlayer < retreatThreshold)
                    return -directionToPlayer * roleMoveSpeed * 0.85f;

                if (distanceToPlayer <= holdThreshold)
                    return Vector3.zero;
            }
            else if (resolvedRole == EnemyRole.Sentinel)
            {
                float anchorLeash = ResolveSentinelAnchorLeash();
                float anchorReturnThreshold = Mathf.Max(0.1f, anchorLeash * 0.55f);
                float distanceFromAnchor = GetPlanarDistanceFromAnchor();

                if (isSentinelReturningToAnchor)
                {
                    if (distanceFromAnchor <= anchorReturnThreshold)
                    {
                        isSentinelReturningToAnchor = false;
                    }
                    else
                    {
                        return GetPlanarDirectionTo(spawnPosition) * roleMoveSpeed * 0.9f;
                    }
                }

                if (distanceFromAnchor > anchorLeash && distanceToPlayer > attackRangeForward * 0.85f)
                {
                    isSentinelReturningToAnchor = true;
                    return GetPlanarDirectionTo(spawnPosition) * roleMoveSpeed * 0.9f;
                }

                if (distanceToPlayer <= attackRangeForward * 0.95f)
                    return Vector3.zero;
            }

            if (attackRecoveryTimer > 0f)
            {
                if (resolvedRole == EnemyRole.Bruiser && distanceToPlayer > Mathf.Max(0.01f, patrolEdgePadding))
                    return directionToPlayer * roleMoveSpeed * 0.75f;

                return Vector3.zero;
            }
        }

        if (distanceToPlayer <= Mathf.Max(0.01f, patrolEdgePadding))
            return Vector3.zero;

        return deltaToPlayer.normalized * roleMoveSpeed;
    }

    private void ClampHorizontalPosition()
    {
        float minX = isAggroActive
            ? GetLeftPatrolBoundaryX() - GetEffectiveChaseBoundsPadding()
            : GetLeftPatrolBoundaryX();
        float maxX = isAggroActive
            ? GetRightPatrolBoundaryX() + GetEffectiveChaseBoundsPadding()
            : GetRightPatrolBoundaryX();

        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, minX, maxX);
        transform.position = position;
    }

    private void LockZAxis()
    {
        if (IsFull3DMovement())
            return;

        Vector3 position = transform.position;
        position.z = lockedZ;
        transform.position = position;
    }

    private void ClampPlanarPosition()
    {
        GetPlanarPatrolBounds(out float leftX, out float rightX, out float minZ, out float maxZ);
        float chasePadding = isAggroActive ? GetEffectiveChaseBoundsPadding() : 0f;

        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, leftX - chasePadding, rightX + chasePadding);
        position.z = Mathf.Clamp(position.z, minZ, maxZ);
        transform.position = position;
    }

    private void ClearAggro()
    {
        isAggroActive = false;
        loseAggroTimer = 0f;
        targetPlayer = null;
        isSentinelReturningToAnchor = false;
        BeginPatrolPause();
    }

    private void EndAnimatedAttack()
    {
        if (!isAttackInProgress)
            return;

        isAttackInProgress = false;
        didResolveCurrentAttackHit = false;
        attackFallbackHitTimer = 0f;
        attackAnimationTimer = 0f;
        attackRecoveryTimer = Mathf.Max(0f, resolvedAttackRecoveryDuration);
    }

    private void BeginPatrolPause()
    {
        float minPause = Mathf.Max(0f, minPatrolPauseDuration);
        float maxPause = Mathf.Max(minPause, maxPatrolPauseDuration);
        patrolPauseTimer = Random.Range(minPause, maxPause);
        nextPatrolPauseTime = Time.time + patrolPauseTimer + Random.Range(
            Mathf.Max(0.1f, minPatrolMoveDuration),
            Mathf.Max(Mathf.Max(0.1f, minPatrolMoveDuration), maxPatrolMoveDuration));
    }

    private void ScheduleNextPatrolPause()
    {
        float minMove = Mathf.Max(0.1f, minPatrolMoveDuration);
        float maxMove = Mathf.Max(minMove, maxPatrolMoveDuration);
        nextPatrolPauseTime = Time.time + Random.Range(minMove, maxMove);
    }

    private Vector3 GetFull3DPatrolVelocity()
    {
        if (patrolPauseTimer > 0f)
        {
            patrolPauseTimer -= Time.deltaTime;

            if (patrolPauseTimer <= 0f)
            {
                ChooseRandomPatrolPlanarDirection();
                ScheduleNextPatrolPause();
            }

            return Vector3.zero;
        }

        if (Time.time >= nextPatrolPauseTime)
        {
            BeginPatrolPause();
            return Vector3.zero;
        }

        GetPlanarPatrolBounds(out float leftX, out float rightX, out float minZ, out float maxZ);
        Vector3 position = transform.position;
        bool shouldPause = false;

        if ((position.x <= leftX + patrolEdgePadding && patrolPlanarDirection.x < 0f)
            || (position.x >= rightX - patrolEdgePadding && patrolPlanarDirection.x > 0f))
        {
            patrolPlanarDirection.x *= -1f;
            shouldPause = true;
        }

        if ((position.z <= minZ + patrolEdgePadding && patrolPlanarDirection.z < 0f)
            || (position.z >= maxZ - patrolEdgePadding && patrolPlanarDirection.z > 0f))
        {
            patrolPlanarDirection.z *= -1f;
            shouldPause = true;
        }

        if (patrolPlanarDirection.sqrMagnitude <= 0.0001f)
            ChooseRandomPatrolPlanarDirection();

        if (shouldPause)
        {
            patrolPlanarDirection.Normalize();
            BeginPatrolPause();
            return Vector3.zero;
        }

        return patrolPlanarDirection.normalized * resolvedMoveSpeed;
    }

    private float GetLeftPatrolBoundaryX()
    {
        GetPatrolBounds(out float leftX, out _);
        return leftX;
    }

    private float GetRightPatrolBoundaryX()
    {
        GetPatrolBounds(out _, out float rightX);
        return rightX;
    }

    private void GetPatrolBounds(out float leftX, out float rightX)
    {
        float fallbackCenterX = Application.isPlaying ? spawnX : transform.position.x;
        leftX = leftBoundary != null ? leftBoundary.position.x : fallbackCenterX - Mathf.Max(0.2f, patrolHalfWidth);
        rightX = rightBoundary != null ? rightBoundary.position.x : fallbackCenterX + Mathf.Max(0.2f, patrolHalfWidth);

        if (leftX > rightX)
        {
            float swap = leftX;
            leftX = rightX;
            rightX = swap;
        }
    }

    private void GetPlanarPatrolBounds(out float leftX, out float rightX, out float minZ, out float maxZ)
    {
        GetPatrolBounds(out leftX, out rightX);

        float centerZ = Application.isPlaying ? spawnPosition.z : transform.position.z;
        float halfDepth = Mathf.Max(0.2f, patrolHalfDepth);
        minZ = centerZ - halfDepth;
        maxZ = centerZ + halfDepth;
    }

    private void ChooseRandomPatrolPlanarDirection()
    {
        Vector2 randomDirection = Random.insideUnitCircle;

        if (randomDirection.sqrMagnitude <= 0.0001f)
            randomDirection = new Vector2(patrolDirection, 0f);

        randomDirection.Normalize();
        patrolPlanarDirection = new Vector3(randomDirection.x, 0f, randomDirection.y);

        if (Mathf.Abs(patrolPlanarDirection.x) > 0.001f)
            patrolDirection = Mathf.Sign(patrolPlanarDirection.x);
    }

    private bool IsDamageableAttackTarget(PlayerCharacter candidate)
    {
        return IsTargetValid(candidate);
    }

    private float GetFacingDirectionSign()
    {
        float facingSign = Mathf.Sign(transform.forward.x);

        if (Mathf.Abs(facingSign) < 0.001f)
            facingSign = Mathf.Abs(patrolDirection) > 0.001f ? Mathf.Sign(patrolDirection) : 1f;

        return facingSign;
    }

    private Vector3 GetAttackHitboxCenter()
    {
        float verticalOffset = Mathf.Max(0.05f, attackHitboxHeight) * 0.5f;

        if (IsFull3DMovement())
        {
            return transform.position
                + transform.forward * attackHitboxForwardOffset
                + Vector3.up * verticalOffset;
        }

        float facingSign = GetFacingDirectionSign();

        return transform.position
            + Vector3.right * (facingSign * attackHitboxForwardOffset)
            + Vector3.up * verticalOffset;
    }

    private Vector3 GetAttackHitboxHalfExtents()
    {
        return new Vector3(
            Mathf.Max(0.05f, attackHitboxWidth * 0.5f),
            Mathf.Max(0.05f, attackHitboxHeight * 0.5f),
            0.55f);
    }

    private Quaternion GetAttackHitboxRotation()
    {
        return IsFull3DMovement() ? transform.rotation : Quaternion.identity;
    }

    private bool IsInsideAttackHitbox(Vector3 worldPosition)
    {
        Vector3 center = GetAttackHitboxCenter();
        Vector3 halfExtents = GetAttackHitboxHalfExtents();
        Vector3 delta = IsFull3DMovement()
            ? Quaternion.Inverse(GetAttackHitboxRotation()) * (worldPosition - center)
            : worldPosition - center;

        return Mathf.Abs(delta.x) <= halfExtents.x
            && Mathf.Abs(delta.y) <= halfExtents.y
            && Mathf.Abs(delta.z) <= halfExtents.z;
    }

    private float GetEffectiveChaseBoundsPadding()
    {
        float basePadding = Mathf.Max(0f, resolvedChaseBoundsPadding);
        return resolvedRole switch
        {
            EnemyRole.Bruiser => basePadding + 0.75f,
            EnemyRole.Sentinel => Mathf.Min(basePadding, 0.25f),
            _ => basePadding
        };
    }

    private float ResolveRoleMoveSpeed()
    {
        float baseSpeed = Mathf.Max(0.05f, resolvedMoveSpeed);
        return resolvedRole switch
        {
            EnemyRole.Bruiser => baseSpeed * 1.05f,
            EnemyRole.Sentinel => baseSpeed * 0.82f,
            _ => baseSpeed
        };
    }

    private float ResolveHorizontalDirection(float deltaX)
    {
        if (Mathf.Abs(deltaX) > 0.001f)
            return Mathf.Sign(deltaX);

        return GetFacingDirectionSign();
    }

    private float ResolveSentinelAnchorLeash()
    {
        GetPatrolBounds(out float leftX, out float rightX);
        float patrolWidth = Mathf.Max(0.1f, rightX - leftX);
        return Mathf.Max(0.35f, patrolWidth * 0.2f);
    }

    private bool IsFull3DMovement()
    {
        return movementPlaneMode == EnemyMovementPlaneMode.Full3D;
    }

    private Vector3 GetPlanarDeltaTo(Vector3 worldPosition)
    {
        Vector3 delta = worldPosition - transform.position;
        delta.y = 0f;
        return delta;
    }

    private Vector3 GetPlanarDirectionTo(Vector3 worldPosition)
    {
        Vector3 delta = GetPlanarDeltaTo(worldPosition);
        return delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector3.zero;
    }

    private float GetPlanarDistanceTo(Vector3 worldPosition)
    {
        return GetPlanarDeltaTo(worldPosition).magnitude;
    }

    private float GetPlanarDistanceFromAnchor()
    {
        Vector3 delta = transform.position - spawnPosition;
        delta.y = 0f;
        return delta.magnitude;
    }

    private void OnDrawGizmosSelected()
    {
        GetPatrolBounds(out float leftX, out float rightX);

        Gizmos.color = new Color(1f, 0.9f, 0.25f, 0.9f);
        Vector3 patrolCenter = new Vector3((leftX + rightX) * 0.5f, transform.position.y + 0.05f, transform.position.z);
        Vector3 patrolSize = new Vector3(Mathf.Max(0.1f, rightX - leftX), 0.15f, 0.6f);
        Gizmos.DrawWireCube(patrolCenter, patrolSize);

        Gizmos.color = new Color(0.35f, 0.85f, 1f, 0.65f);
        float gizmoDetectionRange = resolvedDetectionRange > 0f ? resolvedDetectionRange : detectionRange;
        Gizmos.DrawWireSphere(transform.position, gizmoDetectionRange);

        Gizmos.color = new Color(1f, 0.55f, 0.2f, 0.75f);
        float gizmoChasePadding = resolvedChaseBoundsPadding > 0f ? resolvedChaseBoundsPadding : chaseBoundsPadding;
        float chaseLeft = leftX - gizmoChasePadding;
        float chaseRight = rightX + gizmoChasePadding;
        Vector3 chaseCenter = new Vector3((chaseLeft + chaseRight) * 0.5f, transform.position.y + 0.22f, transform.position.z);
        Vector3 chaseSize = new Vector3(Mathf.Max(0.1f, chaseRight - chaseLeft), 0.15f, 0.6f);
        Gizmos.DrawWireCube(chaseCenter, chaseSize);

        float facingSign = GetFacingDirectionSign();

        Gizmos.color = new Color(0.95f, 0.2f, 1f, 0.9f);
        Vector3 triggerCenter = transform.position
            + Vector3.right * (facingSign * attackRangeForward * 0.5f)
            + Vector3.up * (attackRangeUpward * 0.5f);
        Vector3 triggerSize = new Vector3(attackRangeForward, attackRangeUpward, 0.5f);
        Gizmos.DrawWireCube(triggerCenter, triggerSize);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(GetAttackHitboxCenter(), GetAttackHitboxHalfExtents() * 2f);
    }
}

using UnityEngine;

public enum EnemyAttackType
{
    Contact,
    Animated
}

[RequireComponent(typeof(CharacterController))]
public class EnemyAI : MonoBehaviour
{
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
    private float patrolDirection = 1f;
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

    public bool IsDead => isDead;
    public bool IsAggroActive => isAggroActive;
    public bool IsExternallyLocked => externalMovementLockTimer > 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animationController = GetComponent<EnemyAnimationController>();
        enemyHealth = GetComponent<EnemyHealth>();
        lockedZ = transform.position.z;
        spawnX = transform.position.x;
        patrolDirection = Random.value < 0.5f ? -1f : 1f;
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

        float horizontalSpeed = 0f;

        if (!isAttackInProgress)
        {
            horizontalSpeed = isAggroActive
                ? GetAggroHorizontalSpeed()
                : GetPatrolHorizontalSpeed();
        }
        else if (IsTargetValid(targetPlayer))
        {
            FaceTarget(Mathf.Sign(targetPlayer.transform.position.x - transform.position.x));
        }

        ApplyMovement(horizontalSpeed);
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
            FaceTarget(Mathf.Sign(targetPlayer.transform.position.x - transform.position.x));

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
            Quaternion.identity,
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

    private void ApplyMovement(float horizontalSpeed)
    {
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 motion = new Vector3(horizontalSpeed, verticalVelocity, 0f);
        controller.Move(motion * Time.deltaTime);

        ClampHorizontalPosition();
        LockZAxis();

        if (Mathf.Abs(horizontalSpeed) > 0.001f)
            FaceTarget(Mathf.Sign(horizontalSpeed));

        animationController?.SetSpeed(Mathf.Abs(horizontalSpeed));
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
    }

    public void ResetAfterRespawn()
    {
        isDead = false;
        verticalVelocity = 0f;
        lastAttackTime = Time.time + 0.35f;
        lockedZ = transform.position.z;
        spawnX = transform.position.x;
        targetPlayer = null;
        isAggroActive = false;
        loseAggroTimer = 0f;
        patrolPauseTimer = 0f;
        patrolDirection = Random.value < 0.5f ? -1f : 1f;
        isAttackInProgress = false;
        didResolveCurrentAttackHit = false;
        attackRecoveryTimer = 0f;
        attackFallbackHitTimer = 0f;
        attackAnimationTimer = 0f;
        externalMovementLockTimer = 0f;
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
            attackDamage = enemyHealth.Stats.AnimatedAttackDamage;
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
            ? GetLeftPatrolBoundaryX() - chaseBoundsPadding
            : GetLeftPatrolBoundaryX();
        float maxX = isAggroActive
            ? GetRightPatrolBoundaryX() + chaseBoundsPadding
            : GetRightPatrolBoundaryX();

        return Mathf.Clamp(worldX, minX, maxX);
    }

    private void OnValidate()
    {
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
        loseAggroTimer = Mathf.Max(0.1f, loseAggroDelay);
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
        float allowedRange = Mathf.Max(0.5f, detectionRange);

        if (sqrDistance <= allowedRange * allowedRange)
        {
            loseAggroTimer = Mathf.Max(0.1f, loseAggroDelay);
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

        if (attackType != EnemyAttackType.Animated)
            return false;

        if (isAttackInProgress || attackRecoveryTimer > 0f)
            return false;

        if (Time.time < lastAttackTime + attackCooldown)
            return false;

        float distanceXToPlayer = Mathf.Abs(targetPlayer.transform.position.x - transform.position.x);
        if (distanceXToPlayer > attackRangeForward)
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

        return patrolDirection * moveSpeed;
    }

    private float GetAggroHorizontalSpeed()
    {
        if (!IsTargetValid(targetPlayer))
            return 0f;

        if (attackType == EnemyAttackType.Animated)
        {
            if (CanStartAnimatedAttack())
            {
                BeginAnimatedAttack();
                return 0f;
            }

            if (attackRecoveryTimer > 0f)
                return 0f;
        }

        float minChaseX = GetLeftPatrolBoundaryX() - chaseBoundsPadding;
        float maxChaseX = GetRightPatrolBoundaryX() + chaseBoundsPadding;
        float desiredTargetX = Mathf.Clamp(targetPlayer.transform.position.x, minChaseX, maxChaseX);
        float deltaX = desiredTargetX - transform.position.x;

        if (Mathf.Abs(deltaX) <= patrolEdgePadding)
            return 0f;

        return Mathf.Sign(deltaX) * moveSpeed;
    }

    private void ClampHorizontalPosition()
    {
        float minX = isAggroActive
            ? GetLeftPatrolBoundaryX() - chaseBoundsPadding
            : GetLeftPatrolBoundaryX();
        float maxX = isAggroActive
            ? GetRightPatrolBoundaryX() + chaseBoundsPadding
            : GetRightPatrolBoundaryX();

        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, minX, maxX);
        transform.position = position;
    }

    private void LockZAxis()
    {
        Vector3 position = transform.position;
        position.z = lockedZ;
        transform.position = position;
    }

    private void ClearAggro()
    {
        isAggroActive = false;
        loseAggroTimer = 0f;
        targetPlayer = null;
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
        attackRecoveryTimer = Mathf.Max(0f, attackRecoveryDuration);
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
        float facingSign = GetFacingDirectionSign();
        float verticalOffset = Mathf.Max(0.05f, attackHitboxHeight) * 0.5f;

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

    private bool IsInsideAttackHitbox(Vector3 worldPosition)
    {
        Vector3 center = GetAttackHitboxCenter();
        Vector3 halfExtents = GetAttackHitboxHalfExtents();
        Vector3 delta = worldPosition - center;

        return Mathf.Abs(delta.x) <= halfExtents.x
            && Mathf.Abs(delta.y) <= halfExtents.y
            && Mathf.Abs(delta.z) <= halfExtents.z;
    }

    private void OnDrawGizmosSelected()
    {
        GetPatrolBounds(out float leftX, out float rightX);

        Gizmos.color = new Color(1f, 0.9f, 0.25f, 0.9f);
        Vector3 patrolCenter = new Vector3((leftX + rightX) * 0.5f, transform.position.y + 0.05f, transform.position.z);
        Vector3 patrolSize = new Vector3(Mathf.Max(0.1f, rightX - leftX), 0.15f, 0.6f);
        Gizmos.DrawWireCube(patrolCenter, patrolSize);

        Gizmos.color = new Color(0.35f, 0.85f, 1f, 0.65f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = new Color(1f, 0.55f, 0.2f, 0.75f);
        float chaseLeft = leftX - chaseBoundsPadding;
        float chaseRight = rightX + chaseBoundsPadding;
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

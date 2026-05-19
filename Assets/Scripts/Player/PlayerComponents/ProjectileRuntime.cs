using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class ProjectileRuntime : MonoBehaviour
{
    private const float DefaultImpactDuration = 0.04f;

    private readonly PlayerHitApplicationService hitApplicationService = new PlayerHitApplicationService();
    private PlayerCharacter owner;
    private EnemyHealth lockedTarget;
    private LayerMask enemyLayer;
    private int damage;
    private string skillId;
    private AttackPayload attackPayload;
    private float speed;
    private float collisionRange;
    private CombatHitBoxDefinition collisionHitBox;
    private float lifetime;
    private float elapsedTime;
    private float maxTravelDistance;
    private float traveledDistance;
    private Vector3 direction;
    private bool didResolveHit;
    private bool commitDeathOnHit;
    private ProjectileTravelStyle travelStyle = ProjectileTravelStyle.Straight;
    private ProjectileHitMode hitMode = ProjectileHitMode.FirstTarget;
    private int maxTargets = 1;
    private bool stopOnFirstValidHit = true;
    private float arcHeight;
    private float homingRadius;
    private float homingTurnRate;
    private PresentationCueSet presentationCueSet;
    private float impactAreaRadius;
    private int maxImpactAreaTargets = 1;
    private int resolvedHitCount;
    private CommittedEnemyHitPacket? committedHitPacket;
    private System.Action onFirstSuccessfulHit;
    private bool didInvokeFirstSuccessfulHit;
    private bool allowDefaultVisual;
    private Transform visualRoot;
    private Material runtimeMaterial;
    private Material runtimeTrailMaterial;
    private EnemyHealth homingTarget;
    private readonly HashSet<EnemyHealth> hitEnemies = new HashSet<EnemyHealth>();

    public void Initialize(
        PlayerCharacter ownerCharacter,
        EnemyHealth lockedEnemy,
        LayerMask targetEnemyLayer,
        int hitDamage,
        string sourceSkillId,
        Vector3 travelDirection,
        float travelSpeed,
        float projectileCollisionRange,
        CombatHitBoxDefinition projectileCollisionHitBox,
        float maxLifetime,
        float resolvedTravelDistance,
        float visualScale,
        bool shouldCommitDeathOnHit,
        CommittedEnemyHitPacket? packet = null,
        bool shouldSkipDefaultVisual = false,
        AttackPayload payload = null,
        ProjectileTravelStyle projectileTravelStyle = ProjectileTravelStyle.Straight,
        ProjectileHitMode projectileHitMode = ProjectileHitMode.FirstTarget,
        int projectileMaxTargets = 1,
        bool projectileStopOnFirstValidHit = true,
        float projectileArcHeight = 0f,
        float projectileHomingRadius = 0f,
        float projectileHomingTurnRate = 0f,
        float projectileImpactAreaRadius = 0f,
        int projectileMaxImpactAreaTargets = 1,
        PresentationCueSet cueSet = null,
        System.Action firstSuccessfulHitCallback = null)
    {
        owner = ownerCharacter;
        lockedTarget = lockedEnemy;
        enemyLayer = targetEnemyLayer;
        damage = Mathf.Max(0, hitDamage);
        skillId = string.IsNullOrWhiteSpace(sourceSkillId) ? string.Empty : sourceSkillId.Trim();
        attackPayload = payload;
        direction = travelDirection.sqrMagnitude > 0.0001f
            ? travelDirection.normalized
            : Vector3.forward;
        speed = Mathf.Max(0.1f, travelSpeed);
        collisionRange = Mathf.Max(0.05f, projectileCollisionRange);
        collisionHitBox = projectileCollisionHitBox.GetSanitized();
        maxTravelDistance = Mathf.Max(0f, resolvedTravelDistance);
        traveledDistance = 0f;
        didResolveHit = false;
        lifetime = ResolveLifetime(maxLifetime, maxTravelDistance, speed);
        commitDeathOnHit = shouldCommitDeathOnHit;
        travelStyle = projectileTravelStyle;
        hitMode = projectileHitMode;
        maxTargets = Mathf.Max(1, projectileMaxTargets);
        stopOnFirstValidHit = projectileStopOnFirstValidHit;
        arcHeight = Mathf.Max(0f, projectileArcHeight);
        homingRadius = Mathf.Max(0f, projectileHomingRadius);
        homingTurnRate = Mathf.Max(0f, projectileHomingTurnRate);
        presentationCueSet = cueSet;
        impactAreaRadius = Mathf.Max(0f, projectileImpactAreaRadius);
        maxImpactAreaTargets = Mathf.Max(1, projectileMaxImpactAreaTargets);
        resolvedHitCount = 0;
        committedHitPacket = packet;
        onFirstSuccessfulHit = firstSuccessfulHitCallback;
        didInvokeFirstSuccessfulHit = false;
        allowDefaultVisual = !shouldSkipDefaultVisual;
        homingTarget = null;
        hitEnemies.Clear();

        EnsureVisuals(Mathf.Max(0.08f, visualScale));
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        elapsedTime += deltaTime;

        if (elapsedTime >= lifetime)
        {
            TryResolveLockedTargetFallbackHit();
            if (!TryResolveImpactArea(transform.position))
                PublishFinishCue();
            Destroy(gameObject);
            return;
        }

        Vector3 startPosition = transform.position;
        float travelDistance = ResolveTravelDistance(speed * deltaTime);
        if (travelDistance <= 0f)
        {
            TryResolveLockedTargetFallbackHit();
            if (!TryResolveImpactArea(transform.position))
                PublishFinishCue();
            Destroy(gameObject);
            return;
        }

        if (lockedTarget != null && !lockedTarget.IsDead)
        {
            if (TryAdvanceTowardsLockedTarget(startPosition, travelDistance, out EnemyHealth lockedEnemy, out Vector3 impactPosition))
            {
                transform.position = impactPosition;
                ApplyHit(lockedEnemy);
                Destroy(gameObject);
                return;
            }

            RegisterTravelDistance(startPosition, transform.position);

            if (visualRoot != null)
                visualRoot.Rotate(0f, 0f, 1080f * deltaTime, Space.Self);

            return;
        }

        Vector3 nextPosition = ResolveFreeProjectileNextPosition(startPosition, travelDistance, deltaTime);
        if (TryHitEnemy(startPosition, nextPosition, out EnemyHealth enemy, out Vector3 freeImpactPosition))
        {
            Vector3 continuedPosition = nextPosition;
            transform.position = freeImpactPosition;
            if (ShouldResolveImpactArea())
            {
                TryResolveImpactArea(freeImpactPosition);
                Destroy(gameObject);
                return;
            }

            bool didApplyHit = ApplyHit(enemy);
            RegisterTravelDistance(travelDistance);

            if (!didApplyHit || ShouldFinalizeProjectileAfterHit())
            {
                Destroy(gameObject);
                return;
            }

            transform.position = continuedPosition + direction * Mathf.Max(collisionRange * 0.25f, 0.05f);
            homingTarget = null;
            return;
        }

        transform.position = nextPosition;
        RegisterTravelDistance(travelDistance);
        FaceTravelDirection(nextPosition - startPosition);

        if (visualRoot != null)
            visualRoot.Rotate(0f, 0f, 1080f * deltaTime, Space.Self);
    }

    private bool TryAdvanceTowardsLockedTarget(
        Vector3 startPosition,
        float travelDistance,
        out EnemyHealth enemy,
        out Vector3 impactPosition)
    {
        enemy = null;
        impactPosition = startPosition;

        Vector3 targetPoint = GetEnemyTargetPoint(lockedTarget);
        Vector3 desiredDirection = targetPoint - transform.position;
        if (desiredDirection.sqrMagnitude <= 0.0001f)
        {
            enemy = lockedTarget;
            impactPosition = targetPoint;
            return true;
        }

        desiredDirection.Normalize();
        direction = desiredDirection;

        Vector3 nextPosition = Vector3.MoveTowards(startPosition, targetPoint, travelDistance);
        transform.position = nextPosition;

        if (TryHitSpecificEnemy(startPosition, nextPosition, lockedTarget, out impactPosition))
        {
            enemy = lockedTarget;
            return true;
        }

        return false;
    }

    private Vector3 ResolveFreeProjectileNextPosition(Vector3 startPosition, float travelDistance, float deltaTime)
    {
        if (travelStyle == ProjectileTravelStyle.Homing)
            UpdateHomingDirection(deltaTime);

        if (travelStyle != ProjectileTravelStyle.Arc || arcHeight <= 0f)
            return startPosition + direction * travelDistance;

        Vector3 flatDirection = new Vector3(direction.x, 0f, direction.z);
        if (flatDirection.sqrMagnitude <= 0.0001f)
            flatDirection = Vector3.forward;
        else
            flatDirection.Normalize();

        float totalTravelDistance = ResolveTotalArcDistance();
        float progressBefore = totalTravelDistance > 0f
            ? Mathf.Clamp01(traveledDistance / totalTravelDistance)
            : 0f;
        float progressAfter = totalTravelDistance > 0f
            ? Mathf.Clamp01((traveledDistance + travelDistance) / totalTravelDistance)
            : progressBefore;
        float arcBefore = Mathf.Sin(progressBefore * Mathf.PI) * arcHeight;
        float arcAfter = Mathf.Sin(progressAfter * Mathf.PI) * arcHeight;

        return startPosition
            + flatDirection * travelDistance
            + Vector3.up * (arcAfter - arcBefore);
    }

    private void UpdateHomingDirection(float deltaTime)
    {
        if (homingTurnRate <= 0f)
            return;

        EnemyHealth target = ResolveHomingTarget();
        if (target == null)
            return;

        Vector3 desiredDirection = GetEnemyTargetPoint(target) - transform.position;
        if (desiredDirection.sqrMagnitude <= 0.0001f)
            return;

        desiredDirection.Normalize();

        float maxRadiansDelta = Mathf.Deg2Rad * Mathf.Max(0f, homingTurnRate) * Mathf.Max(0f, deltaTime);
        direction = Vector3.RotateTowards(direction, desiredDirection, maxRadiansDelta, 0f).normalized;
    }

    private EnemyHealth ResolveHomingTarget()
    {
        if (homingTarget != null && !homingTarget.IsDead && !hitEnemies.Contains(homingTarget))
            return homingTarget;

        homingTarget = FindNearestHomingTarget();
        return homingTarget;
    }

    private EnemyHealth FindNearestHomingTarget()
    {
        float searchRadius = Mathf.Max(homingRadius, collisionRange);
        if (searchRadius <= 0f)
            return null;

        Collider[] overlaps = Physics.OverlapSphere(transform.position, searchRadius, enemyLayer, QueryTriggerInteraction.Collide);
        EnemyHealth nearestEnemy = null;
        float nearestDistance = float.MaxValue;

        for (int index = 0; index < overlaps.Length; index++)
        {
            EnemyHealth candidate = overlaps[index].GetComponentInParent<EnemyHealth>();
            if (candidate == null || candidate.IsDead || hitEnemies.Contains(candidate))
                continue;

            Vector3 toCandidate = GetEnemyTargetPoint(candidate) - transform.position;
            if (Vector3.Dot(direction, toCandidate.normalized) < -0.2f)
                continue;

            float distance = toCandidate.sqrMagnitude;
            if (distance >= nearestDistance)
                continue;

            nearestDistance = distance;
            nearestEnemy = candidate;
        }

        return nearestEnemy;
    }

    private bool TryHitEnemy(Vector3 startPosition, Vector3 endPosition, out EnemyHealth enemy, out Vector3 impactPosition)
    {
        enemy = null;
        impactPosition = endPosition;

        Vector3 segment = endPosition - startPosition;
        float segmentDistance = segment.magnitude;
        Vector3 castDirection = segmentDistance > 0.0001f
            ? segment / segmentDistance
            : direction;
        CombatHitBoxWorldQuery sweepQuery = BuildSweepHitBoxQuery(startPosition, endPosition);
        if (TryResolveEnemyFromQuery(sweepQuery, startPosition, castDirection, null, out enemy, out impactPosition))
            return true;

        CombatHitBoxWorldQuery startQuery = BuildBodyHitBoxQuery(startPosition);
        if (TryResolveEnemyFromQuery(startQuery, startPosition, castDirection, null, out enemy, out impactPosition))
            return true;

        CombatHitBoxWorldQuery endQuery = BuildBodyHitBoxQuery(endPosition);
        return TryResolveEnemyFromQuery(endQuery, startPosition, castDirection, null, out enemy, out impactPosition);
    }

    private bool ApplyHit(EnemyHealth enemy)
    {
        if (didResolveHit || enemy == null || hitEnemies.Contains(enemy))
            return false;

        bool didApplyHit;
        if (committedHitPacket.HasValue)
        {
            didApplyHit = hitApplicationService.ApplyCommittedHit(
                enemy,
                owner,
                transform.position.x,
                direction.x,
                committedHitPacket.Value,
                attackPayload,
                presentationCueSet,
                transform);
        }
        else
        {
            didApplyHit = hitApplicationService.ApplyHit(
                enemy,
                owner,
                transform.position.x,
                direction.x,
                attackPayload != null ? 0 : ResolveLocalFallbackDamage(),
                DefaultImpactDuration,
                commitDeathOnHit,
                skillId,
                attackPayload,
                presentationCueSet,
                transform);
        }

        if (didApplyHit)
        {
            hitEnemies.Add(enemy);
            resolvedHitCount++;
            NotifyFirstSuccessfulHit();
            if (ShouldFinalizeProjectileAfterHit())
                didResolveHit = true;
        }

        return didApplyHit;
    }

    private void PublishFinishCue()
    {
        if (didResolveHit || presentationCueSet == null)
            return;

        CombatPresentationDispatcher.PublishCuePhase(
            presentationCueSet,
            CombatCuePhase.Finish,
            worldPosition: transform.position,
            source: owner != null ? owner.transform : null,
            target: null,
            projectile: transform,
            didHit: false);
    }

    private bool TryResolveImpactArea(Vector3 impactPosition)
    {
        if (!ShouldResolveImpactArea() || didResolveHit)
            return false;

        float resolvedRadius = Mathf.Max(impactAreaRadius, collisionRange);
        Collider[] overlaps = Physics.OverlapSphere(impactPosition, resolvedRadius, enemyLayer, QueryTriggerInteraction.Collide);
        if (overlaps.Length == 0)
            return false;

        List<EnemyHealth> targets = new List<EnemyHealth>();
        for (int index = 0; index < overlaps.Length; index++)
        {
            EnemyHealth candidate = overlaps[index].GetComponentInParent<EnemyHealth>();
            if (candidate == null || candidate.IsDead || targets.Contains(candidate))
                continue;

            targets.Add(candidate);
        }

        if (targets.Count == 0)
            return false;

        didResolveHit = true;
        targets.Sort((left, right) =>
        {
            float leftDistance = (GetEnemyTargetPoint(left) - impactPosition).sqrMagnitude;
            float rightDistance = (GetEnemyTargetPoint(right) - impactPosition).sqrMagnitude;
            return leftDistance.CompareTo(rightDistance);
        });

        int resolvedTargetCount = 0;
        for (int index = 0; index < targets.Count && resolvedTargetCount < maxImpactAreaTargets; index++)
        {
            EnemyHealth target = targets[index];
            if (target == null || target.IsDead)
                continue;

            hitApplicationService.ApplyHit(
                target,
                owner,
                transform.position.x,
                direction.x,
                attackPayload != null ? 0 : ResolveLocalFallbackDamage(),
                DefaultImpactDuration,
                commitDeathOnHit,
                skillId,
                attackPayload,
                presentationCueSet,
                transform);

            hitEnemies.Add(target);
            resolvedTargetCount++;
        }

        resolvedHitCount += resolvedTargetCount;
        return resolvedTargetCount > 0;
    }

    private int ResolveLocalFallbackDamage()
    {
        if (attackPayload != null)
            return AttackPayloadBuilder.RollResolvedDamage(attackPayload);

        if (damage > 0)
            return damage;

        if (owner == null)
            return 1;

        PlayerCombatSnapshot snapshot = owner.GetCombatSnapshot();
        int baseDamage = DamageCalculator.CalculateDamage(snapshot, isSkillDamage: true);

        if (!string.IsNullOrWhiteSpace(skillId))
        {
            PlayerSkillDefinition definition = PlayerSkillDatabase.GetDefinition(skillId);
            if (definition != null)
                return Mathf.Max(1, Mathf.RoundToInt(baseDamage * Mathf.Max(0.1f, definition.DamageMultiplier)));
        }

        return Mathf.Max(1, baseDamage);
    }

    private void EnsureVisuals(float visualScale)
    {
        if (!allowDefaultVisual)
            return;

        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail == null)
            trail = gameObject.AddComponent<TrailRenderer>();

        Shader trailShader = Shader.Find("Sprites/Default");
        if (trailShader != null)
        {
            runtimeTrailMaterial = new Material(trailShader);
            trail.material = runtimeTrailMaterial;
        }

        trail.time = 0.08f;
        trail.minVertexDistance = 0.02f;
        trail.widthMultiplier = visualScale * 0.5f;
        trail.startColor = new Color(0.95f, 0.95f, 0.98f, 0.9f);
        trail.endColor = new Color(0.4f, 0.7f, 1f, 0f);
        trail.autodestruct = false;
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.receiveShadows = false;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "Visual";
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = new Vector3(visualScale * 0.85f, visualScale * 2.5f, visualScale * 0.85f);

        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
            Destroy(visualCollider);

        MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            runtimeMaterial = new Material(renderer.sharedMaterial);
            runtimeMaterial.color = new Color(0.18f, 0.18f, 0.24f, 1f);
            renderer.material = runtimeMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        visualRoot = visual.transform;
    }

    private float ResolveTravelDistance(float requestedTravelDistance)
    {
        float resolvedTravelDistance = Mathf.Max(0f, requestedTravelDistance);
        if (resolvedTravelDistance <= 0f)
            return 0f;

        if (maxTravelDistance <= 0f)
            return resolvedTravelDistance;

        float remainingDistance = Mathf.Max(0f, maxTravelDistance - traveledDistance);
        return Mathf.Min(resolvedTravelDistance, remainingDistance);
    }

    private void RegisterTravelDistance(Vector3 startPosition, Vector3 endPosition)
    {
        traveledDistance += Vector3.Distance(startPosition, endPosition);
    }

    private void RegisterTravelDistance(float travelDistance)
    {
        traveledDistance += Mathf.Max(0f, travelDistance);
    }

    private float ResolveTotalArcDistance()
    {
        if (maxTravelDistance > 0f)
            return maxTravelDistance;

        return Mathf.Max(0.05f, speed * lifetime);
    }

    private void FaceTravelDirection(Vector3 travelDelta)
    {
        // Projectile travel is resolved from its movement vector and hit queries,
        // not from the root transform rotation. Keep authored prefab rotation intact.
    }

    private static float ResolveLifetime(float configuredLifetime, float resolvedTravelDistance, float travelSpeed)
    {
        if (resolvedTravelDistance > 0f && travelSpeed > 0f)
            return Mathf.Max(0.05f, resolvedTravelDistance / travelSpeed);

        return Mathf.Max(0.05f, configuredLifetime);
    }

    private void TryResolveLockedTargetFallbackHit()
    {
        if (didResolveHit || lockedTarget == null || lockedTarget.IsDead)
            return;

        ApplyHit(lockedTarget);
    }

    private bool ShouldFinalizeProjectileAfterHit()
    {
        if (stopOnFirstValidHit)
            return true;

        if (hitMode == ProjectileHitMode.FirstTarget)
            return true;

        return resolvedHitCount >= maxTargets;
    }

    private bool ShouldResolveImpactArea()
    {
        return impactAreaRadius > 0f && maxImpactAreaTargets > 0;
    }

    private bool TryHitSpecificEnemy(
        Vector3 startPosition,
        Vector3 endPosition,
        EnemyHealth requiredTarget,
        out Vector3 impactPosition)
    {
        impactPosition = endPosition;
        if (requiredTarget == null || requiredTarget.IsDead)
            return false;

        Vector3 segment = endPosition - startPosition;
        Vector3 castDirection = segment.sqrMagnitude > 0.0001f
            ? segment.normalized
            : direction;

        CombatHitBoxWorldQuery sweepQuery = BuildSweepHitBoxQuery(startPosition, endPosition);
        return TryResolveEnemyFromQuery(sweepQuery, startPosition, castDirection, requiredTarget, out _, out impactPosition);
    }

    private CombatHitBoxWorldQuery BuildSweepHitBoxQuery(Vector3 startPosition, Vector3 endPosition)
    {
        Vector3 segment = endPosition - startPosition;
        float segmentDistance = segment.magnitude;
        Vector3 castDirection = segmentDistance > 0.0001f
            ? segment / segmentDistance
            : direction;
        Quaternion rotation = Quaternion.LookRotation(
            castDirection.sqrMagnitude > 0.0001f ? castDirection : Vector3.forward,
            Vector3.up);
        float effectiveRange = Mathf.Max(collisionRange, segmentDistance + collisionRange);
        return collisionHitBox.BuildWorldQuery(startPosition, rotation, effectiveRange);
    }

    private CombatHitBoxWorldQuery BuildBodyHitBoxQuery(Vector3 worldPosition)
    {
        Quaternion rotation = Quaternion.LookRotation(
            direction.sqrMagnitude > 0.0001f ? direction : Vector3.forward,
            Vector3.up);
        return collisionHitBox.BuildWorldQuery(worldPosition, rotation, collisionRange);
    }

    private bool TryResolveEnemyFromQuery(
        CombatHitBoxWorldQuery query,
        Vector3 referencePoint,
        Vector3 travelDirection,
        EnemyHealth requiredTarget,
        out EnemyHealth enemy,
        out Vector3 impactPosition)
    {
        enemy = null;
        impactPosition = query.Center;

        Collider[] overlaps = Physics.OverlapBox(
            query.Center,
            query.HalfExtents,
            query.Rotation,
            enemyLayer,
            QueryTriggerInteraction.Collide);

        if (overlaps == null || overlaps.Length == 0)
            return false;

        Vector3 normalizedDirection = travelDirection.sqrMagnitude > 0.0001f
            ? travelDirection.normalized
            : direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        float bestDistance = float.MaxValue;

        for (int index = 0; index < overlaps.Length; index++)
        {
            Collider overlap = overlaps[index];
            EnemyHealth candidate = overlap != null ? overlap.GetComponentInParent<EnemyHealth>() : null;
            if (candidate == null || candidate.IsDead || hitEnemies.Contains(candidate))
                continue;

            if (requiredTarget != null && candidate != requiredTarget)
                continue;

            Vector3 candidatePoint = GetEnemyTargetPoint(candidate);
            float projectedDistance = Vector3.Dot(candidatePoint - referencePoint, normalizedDirection);
            if (requiredTarget == null && projectedDistance < -0.05f)
                continue;

            float resolvedDistance = requiredTarget != null
                ? (candidatePoint - referencePoint).sqrMagnitude
                : Mathf.Max(0f, projectedDistance);
            if (resolvedDistance >= bestDistance)
                continue;

            bestDistance = resolvedDistance;
            enemy = candidate;
            impactPosition = candidatePoint;
        }

        return enemy != null;
    }

    private static Vector3 GetEnemyTargetPoint(EnemyHealth enemy)
    {
        if (enemy == null)
            return Vector3.zero;

        Collider enemyCollider = enemy.GetComponentInChildren<Collider>();
        if (enemyCollider != null)
            return enemyCollider.bounds.center;

        return enemy.transform.position + Vector3.up * 0.5f;
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);

        if (runtimeTrailMaterial != null)
            Destroy(runtimeTrailMaterial);
    }

    private void NotifyFirstSuccessfulHit()
    {
        if (didInvokeFirstSuccessfulHit || onFirstSuccessfulHit == null)
            return;

        didInvokeFirstSuccessfulHit = true;
        onFirstSuccessfulHit.Invoke();
    }
}

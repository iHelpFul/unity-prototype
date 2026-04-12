using UnityEngine;
using UnityEngine.Rendering;

public class PlayerSkillProjectile : MonoBehaviour
{
    private const float DefaultImpactDuration = 0.04f;

    private readonly PlayerHitApplicationService hitApplicationService = new PlayerHitApplicationService();
    private PlayerCharacter owner;
    private EnemyHealth lockedTarget;
    private LayerMask enemyLayer;
    private int damage;
    private string skillId;
    private float speed;
    private float radius;
    private float lifetime;
    private float elapsedTime;
    private Vector3 direction;
    private bool commitDeathOnHit;
    private bool allowDefaultVisual;
    private Transform visualRoot;
    private Material runtimeMaterial;
    private Material runtimeTrailMaterial;

    public void Initialize(
        PlayerCharacter ownerCharacter,
        EnemyHealth lockedEnemy,
        LayerMask targetEnemyLayer,
        int hitDamage,
        string sourceSkillId,
        Vector3 travelDirection,
        float travelSpeed,
        float hitRadius,
        float maxLifetime,
        float visualScale,
        bool shouldCommitDeathOnHit,
        bool shouldSkipDefaultVisual = false)
    {
        owner = ownerCharacter;
        lockedTarget = lockedEnemy;
        enemyLayer = targetEnemyLayer;
        damage = Mathf.Max(0, hitDamage);
        skillId = string.IsNullOrWhiteSpace(sourceSkillId) ? string.Empty : sourceSkillId.Trim();
        direction = travelDirection.sqrMagnitude > 0.0001f
            ? travelDirection.normalized
            : Vector3.forward;
        speed = Mathf.Max(0.1f, travelSpeed);
        radius = Mathf.Max(0.05f, hitRadius);
        lifetime = Mathf.Max(0.05f, maxLifetime);
        commitDeathOnHit = shouldCommitDeathOnHit;
        allowDefaultVisual = !shouldSkipDefaultVisual;

        EnsureVisuals(Mathf.Max(0.08f, visualScale));
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        elapsedTime += deltaTime;

        if (elapsedTime >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 startPosition = transform.position;
        float travelDistance = speed * deltaTime;

        if (lockedTarget != null && !lockedTarget.IsDead)
        {
            if (TryAdvanceTowardsLockedTarget(startPosition, travelDistance, out EnemyHealth lockedEnemy, out Vector3 impactPosition))
            {
                transform.position = impactPosition;
                ApplyHit(lockedEnemy);
                Destroy(gameObject);
                return;
            }

            if (visualRoot != null)
                visualRoot.Rotate(0f, 0f, 1080f * deltaTime, Space.Self);

            return;
        }

        if (TryHitEnemy(startPosition, travelDistance, out EnemyHealth enemy, out Vector3 freeImpactPosition))
        {
            transform.position = freeImpactPosition;
            ApplyHit(enemy);
            Destroy(gameObject);
            return;
        }

        transform.position = startPosition + direction * travelDistance;

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
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        Vector3 nextPosition = Vector3.MoveTowards(startPosition, targetPoint, travelDistance);
        transform.position = nextPosition;

        if ((targetPoint - nextPosition).sqrMagnitude <= radius * radius * 4f)
        {
            enemy = lockedTarget;
            impactPosition = targetPoint;
            return true;
        }

        return false;
    }

    private bool TryHitEnemy(Vector3 startPosition, float travelDistance, out EnemyHealth enemy, out Vector3 impactPosition)
    {
        enemy = null;
        impactPosition = startPosition + direction * travelDistance;

        float castDistance = Mathf.Max(travelDistance, radius * 0.5f);
        RaycastHit[] hits = Physics.SphereCastAll(
            startPosition,
            radius,
            direction,
            castDistance,
            enemyLayer,
            QueryTriggerInteraction.Collide);

        float bestDistance = float.MaxValue;

        for (int index = 0; index < hits.Length; index++)
        {
            RaycastHit hit = hits[index];
            EnemyHealth candidate = hit.collider.GetComponentInParent<EnemyHealth>();
            if (candidate == null || candidate.IsDead)
                continue;

            if (hit.distance >= bestDistance)
                continue;

            bestDistance = hit.distance;
            enemy = candidate;
            impactPosition = hit.point;
        }

        if (enemy != null)
            return true;

        Collider[] overlaps = Physics.OverlapSphere(startPosition, radius, enemyLayer, QueryTriggerInteraction.Collide);
        for (int index = 0; index < overlaps.Length; index++)
        {
            EnemyHealth candidate = overlaps[index].GetComponentInParent<EnemyHealth>();
            if (candidate == null || candidate.IsDead)
                continue;

            enemy = candidate;
            impactPosition = candidate.transform.position;
            return true;
        }

        return false;
    }

    private void ApplyHit(EnemyHealth enemy)
    {
        hitApplicationService.ApplyHit(
            enemy,
            owner,
            transform.position.x,
            direction.x,
            ResolveLocalFallbackDamage(),
            DefaultImpactDuration,
            commitDeathOnHit,
            skillId);
    }

    private int ResolveLocalFallbackDamage()
    {
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
}

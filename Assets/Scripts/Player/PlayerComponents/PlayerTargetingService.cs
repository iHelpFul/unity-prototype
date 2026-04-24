using System.Collections.Generic;
using UnityEngine;

public class PlayerTargetingService
{
    private readonly Transform ownerTransform;
    private readonly Transform visualTransform;
    private readonly LayerMask enemyLayer;
    private readonly float skillFrontDotThreshold;
    private readonly float skillAreaForwardOffsetFactor;
    private readonly float skillAreaRadiusFactor;
    private readonly float skillAreaMinRadius;
    private readonly float lockedSkillTargetGraceRange;
    private readonly float attackLowerHeightAllowance;

    public PlayerTargetingService(
        Transform ownerTransform,
        Transform visualTransform,
        LayerMask enemyLayer,
        float skillFrontDotThreshold,
        float skillAreaForwardOffsetFactor,
        float skillAreaRadiusFactor,
        float skillAreaMinRadius,
        float lockedSkillTargetGraceRange,
        float attackLowerHeightAllowance)
    {
        this.ownerTransform = ownerTransform;
        this.visualTransform = visualTransform != null ? visualTransform : ownerTransform;
        this.enemyLayer = enemyLayer;
        this.skillFrontDotThreshold = skillFrontDotThreshold;
        this.skillAreaForwardOffsetFactor = skillAreaForwardOffsetFactor;
        this.skillAreaRadiusFactor = skillAreaRadiusFactor;
        this.skillAreaMinRadius = skillAreaMinRadius;
        this.lockedSkillTargetGraceRange = lockedSkillTargetGraceRange;
        this.attackLowerHeightAllowance = attackLowerHeightAllowance;
    }

    public EnemyHealth FindFrontSingleTarget(PlayerSkillDefinition definition, int skillLevel = 1)
    {
        float resolvedRange = definition != null
            ? definition.GetResolvedRange(skillLevel)
            : 0f;
        List<EnemyHealth> candidates = GetEnemiesInSphere(ownerTransform.position, resolvedRange);

        EnemyHealth bestTarget = null;
        float bestSqrDistance = float.MaxValue;

        foreach (EnemyHealth candidate in candidates)
        {
            Vector3 targetPoint = GetEnemyTargetPoint(candidate);

            if (!IsInFront(targetPoint))
                continue;

            if (!IsWithinAllowedAttackHeight(candidate, ownerTransform.position.y))
                continue;

            float sqrDistance = (targetPoint - ownerTransform.position).sqrMagnitude;
            if (sqrDistance >= bestSqrDistance)
                continue;

            bestSqrDistance = sqrDistance;
            bestTarget = candidate;
        }

        return bestTarget;
    }

    public List<EnemyHealth> FindSkillAreaTargets(PlayerSkillDefinition definition, int skillLevel = 1)
    {
        float searchRadius = GetSkillAreaRadius(definition, skillLevel);
        Vector3 center = GetSkillAreaCenter(definition, searchRadius, skillLevel);

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
            float leftDistance = (left.transform.position - ownerTransform.position).sqrMagnitude;
            float rightDistance = (right.transform.position - ownerTransform.position).sqrMagnitude;
            return leftDistance.CompareTo(rightDistance);
        });

        int maxTargets = definition != null
            ? definition.GetResolvedMaxTargets(skillLevel)
            : 1;
        if (validTargets.Count > maxTargets)
            validTargets.RemoveRange(maxTargets, validTargets.Count - maxTargets);

        return validTargets;
    }

    public EnemyHealth ResolveLockedSkillTarget(
        PlayerSkillDefinition definition,
        EnemyHealth lockedTarget,
        bool allowReacquire,
        int skillLevel = 1)
    {
        if (lockedTarget != null && !lockedTarget.IsDead)
        {
            Vector3 targetPoint = GetEnemyTargetPoint(lockedTarget);

            if (!allowReacquire)
                return lockedTarget;

            if (!IsInFront(targetPoint))
                return allowReacquire ? FindFrontSingleTarget(definition, skillLevel) : null;

            if (!IsWithinAllowedAttackHeight(lockedTarget, ownerTransform.position.y))
                return allowReacquire ? FindFrontSingleTarget(definition, skillLevel) : null;

            float maxDistance = (definition != null ? definition.GetResolvedRange(skillLevel) : 0f) + lockedSkillTargetGraceRange;
            float sqrMaxDistance = maxDistance * maxDistance;
            float sqrDistance = (targetPoint - ownerTransform.position).sqrMagnitude;

            if (sqrDistance <= sqrMaxDistance)
                return lockedTarget;
        }

        return allowReacquire ? FindFrontSingleTarget(definition, skillLevel) : null;
    }

    public List<EnemyHealth> GetEnemiesInSphere(Vector3 center, float radius)
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

    public Vector3 GetEnemyTargetPoint(EnemyHealth enemy)
    {
        if (enemy == null)
            return ownerTransform.position;

        Collider enemyCollider = enemy.GetComponentInChildren<Collider>();
        if (enemyCollider != null)
            return enemyCollider.bounds.center;

        return enemy.transform.position + Vector3.up * 0.5f;
    }

    public float GetEnemyTopY(EnemyHealth enemy)
    {
        if (enemy == null)
            return ownerTransform.position.y;

        Collider enemyCollider = enemy.GetComponentInChildren<Collider>();
        if (enemyCollider != null)
            return enemyCollider.bounds.max.y;

        return enemy.transform.position.y + 1f;
    }

    public bool IsInFront(Vector3 targetPosition)
    {
        Transform facingTransform = visualTransform != null ? visualTransform : ownerTransform;
        Vector3 directionToTarget = targetPosition - facingTransform.position;
        directionToTarget.y = 0f;

        if (directionToTarget.sqrMagnitude <= 0.0001f)
            return true;

        directionToTarget.Normalize();
        return Vector3.Dot(facingTransform.forward, directionToTarget) >= skillFrontDotThreshold;
    }

    public bool IsWithinAllowedAttackHeight(EnemyHealth enemy, float referenceY)
    {
        return GetEnemyTopY(enemy) + attackLowerHeightAllowance >= referenceY;
    }

    public float GetSkillAreaRadius(PlayerSkillDefinition definition, int skillLevel = 1)
    {
        float resolvedRange = definition != null
            ? definition.GetResolvedRange(skillLevel)
            : 0f;
        return Mathf.Max(skillAreaMinRadius, resolvedRange * skillAreaRadiusFactor);
    }

    public Vector3 GetSkillAreaCenter(PlayerSkillDefinition definition, float radius, int skillLevel = 1)
    {
        Transform facingTransform = visualTransform != null ? visualTransform : ownerTransform;
        float resolvedRange = definition != null
            ? definition.GetResolvedRange(skillLevel)
            : 0f;
        return facingTransform.position
            + facingTransform.forward * Mathf.Max(radius * 0.25f, resolvedRange * skillAreaForwardOffsetFactor);
    }
}

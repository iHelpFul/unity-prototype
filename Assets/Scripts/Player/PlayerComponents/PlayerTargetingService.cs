using System.Collections.Generic;
using UnityEngine;

public class PlayerTargetingService
{
    private readonly Transform ownerTransform;
    private readonly Transform visualTransform;
    private readonly LayerMask enemyLayer;
    private readonly float lockedSkillTargetGraceRange;

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
        this.lockedSkillTargetGraceRange = Mathf.Max(0f, lockedSkillTargetGraceRange);
    }

    public EnemyHealth FindFrontSingleTarget(PlayerSkillDefinition definition, int skillLevel = 1)
    {
        if (definition == null)
            return null;

        return FindFirstTargetInHitBox(
            definition.HitBox,
            definition.GetResolvedRange(skillLevel));
    }

    public List<EnemyHealth> FindSkillAreaTargets(PlayerSkillDefinition definition, int skillLevel = 1)
    {
        if (definition == null)
            return new List<EnemyHealth>();

        return GetEnemiesInHitBox(
            definition.HitBox,
            definition.GetResolvedRange(skillLevel),
            definition.GetResolvedMaxTargets(skillLevel));
    }

    public EnemyHealth ResolveLockedSkillTarget(
        PlayerSkillDefinition definition,
        EnemyHealth lockedTarget,
        bool allowReacquire,
        int skillLevel = 1)
    {
        if (definition == null)
            return null;

        return ResolveLockedTargetInHitBox(
            definition.HitBox,
            definition.GetResolvedRange(skillLevel),
            lockedTarget,
            allowReacquire);
    }

    public EnemyHealth FindFirstTargetInHitBox(CombatHitBoxDefinition hitBox, float resolvedRange)
    {
        List<EnemyHealth> targets = GetEnemiesInHitBox(hitBox, resolvedRange, 1);
        return targets.Count > 0 ? targets[0] : null;
    }

    public EnemyHealth ResolveLockedTargetInHitBox(
        CombatHitBoxDefinition hitBox,
        float resolvedRange,
        EnemyHealth lockedTarget,
        bool allowReacquire)
    {
        if (lockedTarget != null && !lockedTarget.IsDead)
        {
            float expandedRange = Mathf.Max(0.05f, resolvedRange + lockedSkillTargetGraceRange);
            if (IsTargetInsideHitBox(lockedTarget, hitBox, expandedRange))
                return lockedTarget;
        }

        return allowReacquire ? FindFirstTargetInHitBox(hitBox, resolvedRange) : null;
    }

    public List<EnemyHealth> GetEnemiesInHitBox(
        CombatHitBoxDefinition hitBox,
        float resolvedRange,
        int maxTargets = int.MaxValue)
    {
        CombatHitBoxWorldQuery query = BuildWorldHitBoxQuery(hitBox, resolvedRange);
        Collider[] hits = Physics.OverlapBox(query.Center, query.HalfExtents, query.Rotation, enemyLayer);
        List<EnemyHealth> enemies = new List<EnemyHealth>(hits.Length);
        HashSet<EnemyHealth> seenEnemies = new HashSet<EnemyHealth>();

        for (int index = 0; index < hits.Length; index++)
        {
            Collider hit = hits[index];
            EnemyHealth enemy = hit != null ? hit.GetComponentInParent<EnemyHealth>() : null;
            if (enemy == null || enemy.IsDead || !seenEnemies.Add(enemy))
                continue;

            enemies.Add(enemy);
        }

        Vector3 referencePoint = ownerTransform != null ? ownerTransform.position : query.Center;
        enemies.Sort((left, right) =>
        {
            float leftDistance = (GetEnemyTargetPoint(left) - referencePoint).sqrMagnitude;
            float rightDistance = (GetEnemyTargetPoint(right) - referencePoint).sqrMagnitude;
            return leftDistance.CompareTo(rightDistance);
        });

        int resolvedMaxTargets = maxTargets <= 0 ? int.MaxValue : maxTargets;
        if (resolvedMaxTargets != int.MaxValue && enemies.Count > resolvedMaxTargets)
            enemies.RemoveRange(resolvedMaxTargets, enemies.Count - resolvedMaxTargets);

        return enemies;
    }

    public CombatHitBoxWorldQuery BuildWorldHitBoxQuery(CombatHitBoxDefinition hitBox, float resolvedRange)
    {
        Transform facingTransform = visualTransform != null ? visualTransform : ownerTransform;
        return hitBox.BuildWorldQuery(facingTransform, resolvedRange);
    }

    public bool IsTargetInsideHitBox(EnemyHealth enemy, CombatHitBoxDefinition hitBox, float resolvedRange)
    {
        if (enemy == null)
            return false;

        CombatHitBoxWorldQuery query = BuildWorldHitBoxQuery(hitBox, resolvedRange);
        return IsPointInsideQuery(GetEnemyTargetPoint(enemy), query);
    }

    public Vector3 GetEnemyTargetPoint(EnemyHealth enemy)
    {
        if (enemy == null)
            return ownerTransform != null ? ownerTransform.position : Vector3.zero;

        Collider enemyCollider = enemy.GetComponentInChildren<Collider>();
        if (enemyCollider != null)
            return enemyCollider.bounds.center;

        return enemy.transform.position + Vector3.up * 0.5f;
    }

    private static bool IsPointInsideQuery(Vector3 point, CombatHitBoxWorldQuery query)
    {
        Vector3 localPoint = Quaternion.Inverse(query.Rotation) * (point - query.Center);
        return Mathf.Abs(localPoint.x) <= query.HalfExtents.x
            && Mathf.Abs(localPoint.y) <= query.HalfExtents.y
            && Mathf.Abs(localPoint.z) <= query.HalfExtents.z;
    }
}

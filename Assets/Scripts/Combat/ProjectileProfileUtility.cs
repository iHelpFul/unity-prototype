using UnityEngine;

public static class ProjectileProfileUtility
{
    public const float DefaultSpeed = 12f;
    public const float DefaultCollisionRange = 0.4f;
    public const float DefaultLifetime = 0.5f;
    public const float DefaultSpawnForwardOffset = 0.8f;
    public const float DefaultSpawnUpOffset = 1f;
    public const float DefaultVisualScale = 0.2f;

    public static float ResolveSpeed(ProjectileProfile projectileProfile)
    {
        return projectileProfile != null
            ? Mathf.Max(0.1f, projectileProfile.Speed)
            : DefaultSpeed;
    }

    public static float ResolveCollisionRange(ProjectileProfile projectileProfile)
    {
        return projectileProfile != null
            ? Mathf.Max(0.05f, projectileProfile.CollisionRange)
            : DefaultCollisionRange;
    }

    public static CombatHitBoxDefinition ResolveCollisionHitBox(ProjectileProfile projectileProfile)
    {
        return projectileProfile != null
            ? projectileProfile.CollisionHitBox
            : default;
    }

    public static float ResolveLifetime(ProjectileProfile projectileProfile, AttackPayload payload, float resolvedSpeed)
    {
        float speed = Mathf.Max(0.1f, resolvedSpeed);
        if (payload != null && payload.ResolvedRange > 0f)
            return Mathf.Max(0.05f, payload.ResolvedRange / speed);

        return projectileProfile != null
            ? Mathf.Max(0.05f, projectileProfile.Lifetime)
            : DefaultLifetime;
    }

    public static float ResolveSpawnForwardOffset(ProjectileProfile projectileProfile)
    {
        return projectileProfile != null
            ? Mathf.Max(0f, projectileProfile.SpawnForwardOffset)
            : DefaultSpawnForwardOffset;
    }

    public static float ResolveSpawnUpOffset(ProjectileProfile projectileProfile)
    {
        return projectileProfile != null
            ? projectileProfile.SpawnUpOffset
            : DefaultSpawnUpOffset;
    }

    public static float ResolveVisualScale(ProjectileProfile projectileProfile)
    {
        return projectileProfile != null
            ? Mathf.Max(0.05f, projectileProfile.VisualScale)
            : DefaultVisualScale;
    }

    public static ProjectileVisualRotationMode ResolveVisualRotationMode(ProjectileProfile projectileProfile)
    {
        return projectileProfile != null
            ? projectileProfile.VisualRotationMode
            : ProjectileVisualRotationMode.FlipYOnHorizontalDirection;
    }

    public static bool ResolveInvertVisualRotationOffsetWhenFacingOppositeSide(ProjectileProfile projectileProfile)
    {
        return projectileProfile != null
            && projectileProfile.InvertVisualRotationOffsetWhenFacingOppositeSide;
    }

    public static Vector3 ResolveVisualRotationOffsetEuler(ProjectileProfile projectileProfile)
    {
        return projectileProfile != null
            ? projectileProfile.VisualRotationOffsetEuler
            : Vector3.zero;
    }

    public static ProjectileHitMode ResolveHitMode(ProjectileProfile projectileProfile)
    {
        return projectileProfile != null
            ? projectileProfile.HitMode
            : ProjectileHitMode.FirstTarget;
    }

    public static ProjectileTravelStyle ResolveTravelStyle(ProjectileProfile projectileProfile)
    {
        return projectileProfile != null
            ? projectileProfile.TravelStyle
            : ProjectileTravelStyle.Straight;
    }

    public static int ResolveMaxTargets(ProjectileProfile projectileProfile, AttackPayload payload)
    {
        int profileTargets = projectileProfile != null
            ? Mathf.Max(1, projectileProfile.MaxTargets)
            : 1;
        int payloadTargets = payload != null
            ? Mathf.Max(1, payload.MaxTargets)
            : 1;

        return Mathf.Max(profileTargets, payloadTargets);
    }

    public static bool ResolveStopOnFirstHit(ProjectileProfile projectileProfile, AttackPayload payload)
    {
        if (projectileProfile != null)
            return projectileProfile.StopOnFirstValidHit;

        return payload == null || payload.StopOnFirstValidHit;
    }

    public static float ResolveArcHeight(ProjectileProfile projectileProfile)
    {
        return projectileProfile != null
            ? Mathf.Max(0f, projectileProfile.ArcHeight)
            : 0f;
    }

    public static float ResolveHomingRadius(ProjectileProfile projectileProfile)
    {
        return projectileProfile != null
            ? Mathf.Max(0f, projectileProfile.HomingRadius)
            : 0f;
    }

    public static float ResolveHomingTurnRate(ProjectileProfile projectileProfile)
    {
        return projectileProfile != null
            ? Mathf.Max(0f, projectileProfile.HomingTurnRate)
            : 0f;
    }

    public static float ResolveImpactAreaRadius(ProjectileProfile projectileProfile)
    {
        return projectileProfile != null
            ? Mathf.Max(0f, projectileProfile.ImpactAreaRadius)
            : 0f;
    }

    public static int ResolveMaxImpactAreaTargets(ProjectileProfile projectileProfile, AttackPayload payload)
    {
        int profileTargets = projectileProfile != null
            ? Mathf.Max(1, projectileProfile.MaxImpactAreaTargets)
            : 1;
        int payloadTargets = payload != null
            ? Mathf.Max(1, payload.MaxTargets)
            : 1;

        return Mathf.Max(profileTargets, payloadTargets);
    }
}

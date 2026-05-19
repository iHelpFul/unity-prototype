using UnityEngine;

public class ProjectileSystem : MonoBehaviour
{
    public ProjectileRuntime Launch(ProjectileLaunchRequest request)
    {
        if (request == null || request.Owner == null)
            return null;

        Vector3 direction = request.Direction.sqrMagnitude > 0.0001f
            ? request.Direction.normalized
            : Vector3.forward;

        GameObject projectileObject = CreateProjectileObject(request, direction, out bool skipDefaultVisual);
        ProjectileRuntime projectile = projectileObject.GetComponent<ProjectileRuntime>();
        if (projectile == null)
            projectile = projectileObject.AddComponent<ProjectileRuntime>();

        projectile.Initialize(
            request.Owner,
            request.LockedTarget,
            request.EnemyLayer,
            request.ExplicitDamage,
            request.ActionId,
            direction,
            request.TravelSpeed,
            request.CollisionRange,
            request.CollisionHitBox,
            request.MaxLifetime,
            request.ResolvedTravelDistance,
            request.VisualScale,
            request.CommitDeathOnHit,
            request.CommittedHitPacket,
            skipDefaultVisual,
            request.Payload,
            request.TravelStyle,
            request.HitMode,
            request.MaxTargets,
            request.StopOnFirstValidHit,
            request.ArcHeight,
            request.HomingRadius,
            request.HomingTurnRate,
            request.ImpactAreaRadius,
            request.MaxImpactAreaTargets,
            request.PresentationCueSet,
            request.OnFirstSuccessfulHit);

        PublishProjectileSpawnCue(request, projectile.transform);
        return projectile;
    }

    private static GameObject CreateProjectileObject(
        ProjectileLaunchRequest request,
        Vector3 direction,
        out bool skipDefaultVisual)
    {
        skipDefaultVisual = false;
        Quaternion spawnRotation = ResolveSpawnRotation(request, direction);
        GameObject projectileObject;
        if (request.ProjectilePrefab != null)
        {
            projectileObject = Instantiate(request.ProjectilePrefab, request.SpawnPosition, spawnRotation);
            skipDefaultVisual = HasVisualContent(projectileObject);
        }
        else
        {
            projectileObject = new GameObject();
            projectileObject.transform.SetPositionAndRotation(request.SpawnPosition, spawnRotation);
        }

        projectileObject.name = $"{ResolveActionName(request.ActionId)}_Projectile";
        return projectileObject;
    }

    private static Quaternion ResolveSpawnRotation(ProjectileLaunchRequest request, Vector3 direction)
    {
        Quaternion baseRotation = request != null && request.ProjectilePrefab != null
            ? request.ProjectilePrefab.transform.rotation
            : Quaternion.identity;
        Vector3 visualOffsetEuler = request != null ? request.VisualRotationOffsetEuler : Vector3.zero;
        ProjectileVisualRotationMode rotationMode = request != null
            ? request.VisualRotationMode
            : ProjectileVisualRotationMode.FlipYOnHorizontalDirection;
        Vector3 planarDirection = Vector3.ProjectOnPlane(direction, Vector3.up);
        bool isFacingOppositeHorizontalSide = planarDirection.sqrMagnitude > 0.0001f
            && Vector3.Dot(planarDirection.normalized, Vector3.right) < 0f;
        if (request != null
            && request.InvertVisualRotationOffsetWhenFacingOppositeSide
            && isFacingOppositeHorizontalSide)
        {
            visualOffsetEuler = -visualOffsetEuler;
        }

        Quaternion offsetRotation = Quaternion.Euler(visualOffsetEuler);

        switch (rotationMode)
        {
            case ProjectileVisualRotationMode.KeepPrefabRotation:
                return baseRotation * offsetRotation;

            case ProjectileVisualRotationMode.AlignToTravelDirection:
            {
                if (planarDirection.sqrMagnitude <= 0.0001f)
                    return baseRotation * offsetRotation;

                return Quaternion.LookRotation(planarDirection.normalized, Vector3.up) * offsetRotation;
            }

            default:
            {
                if (planarDirection.sqrMagnitude <= 0.0001f)
                    return baseRotation * offsetRotation;

                Quaternion facingRotation = isFacingOppositeHorizontalSide
                    ? Quaternion.AngleAxis(180f, Vector3.up)
                    : Quaternion.identity;
                return facingRotation * baseRotation * offsetRotation;
            }
        }
    }

    private static void PublishProjectileSpawnCue(ProjectileLaunchRequest request, Transform projectileTransform)
    {
        if (request.PresentationCueSet == null)
            return;

        CombatPresentationDispatcher.PublishCuePhase(
            request.PresentationCueSet,
            CombatCuePhase.ProjectileSpawn,
            request.SpawnPosition,
            request.PresentationSource != null ? request.PresentationSource : request.Owner.transform,
            null,
            projectileTransform);
    }

    private static bool HasVisualContent(GameObject rootObject)
    {
        if (rootObject == null)
            return false;

        return rootObject.GetComponentInChildren<Renderer>(true) != null
            || rootObject.GetComponentInChildren<TrailRenderer>(true) != null
            || rootObject.GetComponentInChildren<ParticleSystem>(true) != null;
    }

    private static string ResolveActionName(string actionId)
    {
        return string.IsNullOrWhiteSpace(actionId)
            ? "Combat"
            : actionId.Trim();
    }
}

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
            request.HitRadius,
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
            request.BehaviorKind,
            request.ImpactAreaRadius,
            request.MaxImpactAreaTargets,
            request.PresentationCueSet);

        PublishProjectileSpawnCue(request, projectile.transform);
        return projectile;
    }

    private static GameObject CreateProjectileObject(
        ProjectileLaunchRequest request,
        Vector3 direction,
        out bool skipDefaultVisual)
    {
        skipDefaultVisual = false;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        GameObject projectileObject;
        if (request.ProjectilePrefab != null)
        {
            projectileObject = Instantiate(request.ProjectilePrefab, request.SpawnPosition, rotation);
            skipDefaultVisual = HasVisualContent(projectileObject);
        }
        else
        {
            projectileObject = new GameObject();
            projectileObject.transform.SetPositionAndRotation(request.SpawnPosition, rotation);
        }

        projectileObject.name = $"{ResolveActionName(request.ActionId)}_Projectile";
        return projectileObject;
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

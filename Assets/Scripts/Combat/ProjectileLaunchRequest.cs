using UnityEngine;

public sealed class ProjectileLaunchRequest
{
    public string ActionId = string.Empty;
    public PlayerCharacter Owner;
    public Transform PresentationSource;
    public GameObject ProjectilePrefab;
    public EnemyHealth LockedTarget;
    public LayerMask EnemyLayer;
    public int ExplicitDamage;
    public Vector3 SpawnPosition;
    public Vector3 Direction = Vector3.forward;
    public float TravelSpeed;
    public float HitRadius;
    public float MaxLifetime;
    public float ResolvedTravelDistance;
    public float VisualScale;
    public bool CommitDeathOnHit;
    public CommittedEnemyHitPacket? CommittedHitPacket;
    public AttackPayload Payload;
    public ProjectileTravelStyle TravelStyle = ProjectileTravelStyle.Straight;
    public ProjectileHitMode HitMode = ProjectileHitMode.FirstTarget;
    public int MaxTargets = 1;
    public bool StopOnFirstValidHit = true;
    public float ArcHeight;
    public float HomingRadius;
    public float HomingTurnRate;
    public ProjectileBehaviorKind BehaviorKind = ProjectileBehaviorKind.Free;
    public float ImpactAreaRadius;
    public int MaxImpactAreaTargets = 1;
    public PresentationCueSet PresentationCueSet;
}

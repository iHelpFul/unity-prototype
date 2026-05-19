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
    public float CollisionRange;
    public CombatHitBoxDefinition CollisionHitBox;
    public float MaxLifetime;
    public float ResolvedTravelDistance;
    public float VisualScale;
    public ProjectileVisualRotationMode VisualRotationMode = ProjectileVisualRotationMode.FlipYOnHorizontalDirection;
    public bool InvertVisualRotationOffsetWhenFacingOppositeSide;
    public Vector3 VisualRotationOffsetEuler;
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
    public float ImpactAreaRadius;
    public int MaxImpactAreaTargets = 1;
    public PresentationCueSet PresentationCueSet;
    public System.Action OnFirstSuccessfulHit;
}

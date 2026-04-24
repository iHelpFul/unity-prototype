using UnityEngine;

public enum ProjectileTravelStyle
{
    Straight = 0,
    Arc = 1,
    Homing = 2
}

public enum ProjectileHitMode
{
    FirstTarget = 0,
    Pierce = 1,
    MultiTarget = 2
}

[CreateAssetMenu(menuName = "Game Data/Combat/Projectile Profile")]
public class ProjectileProfile : ScriptableObject
{
    [SerializeField] private string projectileId = string.Empty;
    [SerializeField] private string displayName = "New Projectile";
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private CombatAttackFamily attackFamily = CombatAttackFamily.None;
    [SerializeField] private PresentationCueSet presentationCueSet;
    [SerializeField] private ProjectileTravelStyle travelStyle = ProjectileTravelStyle.Straight;
    [SerializeField] private ProjectileHitMode hitMode = ProjectileHitMode.FirstTarget;
    [SerializeField] private float speed = 12f;
    [SerializeField] private float maxRange = 8f;
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private int maxTargets = 1;
    [SerializeField] private float collisionRadius = 0.2f;
    [SerializeField] private float spawnForwardOffset = 0.8f;
    [SerializeField] private float spawnUpOffset = 1f;
    [SerializeField] private float visualScale = 0.2f;
    [SerializeField] private bool stopOnFirstValidHit = true;
    [SerializeField] private float arcHeight = 1.25f;
    [SerializeField] private float homingRadius = 5f;
    [SerializeField] private float homingTurnRate = 540f;
    [SerializeField] private float impactAreaRadius = 1.5f;
    [SerializeField] private int maxImpactAreaTargets = 4;

    public string ProjectileId => projectileId;
    public string DisplayName => displayName;
    public GameObject ProjectilePrefab => projectilePrefab;
    public CombatAttackFamily AttackFamily => attackFamily;
    public PresentationCueSet PresentationCueSet => presentationCueSet;
    public ProjectileTravelStyle TravelStyle => travelStyle;
    public ProjectileHitMode HitMode => hitMode;
    public float Speed => speed;
    public float MaxRange => maxRange;
    public float Lifetime => lifetime;
    public int MaxTargets => maxTargets;
    public float CollisionRadius => collisionRadius;
    public float SpawnForwardOffset => spawnForwardOffset;
    public float SpawnUpOffset => spawnUpOffset;
    public float VisualScale => visualScale;
    public bool StopOnFirstValidHit => stopOnFirstValidHit;
    public float ArcHeight => arcHeight;
    public float HomingRadius => homingRadius;
    public float HomingTurnRate => homingTurnRate;
    public float ImpactAreaRadius => impactAreaRadius;
    public int MaxImpactAreaTargets => maxImpactAreaTargets;

    private void OnValidate()
    {
        projectileId = string.IsNullOrWhiteSpace(projectileId) ? string.Empty : projectileId.Trim();
        displayName = string.IsNullOrWhiteSpace(displayName) ? "Unnamed Projectile" : displayName.Trim();
        speed = Mathf.Max(0f, speed);
        maxRange = Mathf.Max(0f, maxRange);
        lifetime = Mathf.Max(0.05f, lifetime);
        maxTargets = Mathf.Max(1, maxTargets);
        collisionRadius = Mathf.Max(0.01f, collisionRadius);
        spawnForwardOffset = Mathf.Max(0f, spawnForwardOffset);
        visualScale = Mathf.Max(0.01f, visualScale);
        arcHeight = Mathf.Max(0f, arcHeight);
        homingRadius = Mathf.Max(0f, homingRadius);
        homingTurnRate = Mathf.Max(0f, homingTurnRate);
        impactAreaRadius = Mathf.Max(0f, impactAreaRadius);
        maxImpactAreaTargets = Mathf.Max(1, maxImpactAreaTargets);
    }
}

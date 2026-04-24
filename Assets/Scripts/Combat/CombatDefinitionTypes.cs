public enum CombatAttackFamily
{
    None = 0,
    NoviceMelee = 1,
    BowBasic = 2,
    BowSkill = 3,
    ArcaneBasic = 4,
    ArcaneBurst = 5,
    HeavyStrike = 6
}

public enum CombatExecutionKind
{
    None = 0,
    Melee = 1,
    Projectile = 2,
    MagicProjectile = 3
}

public enum CombatTargetingKind
{
    None = 0,
    Self = 1,
    SingleTarget = 2,
    Area = 3,
    ForwardProjectile = 4
}

public enum ProjectileBehaviorKind
{
    SequenceLocked = 0,
    Free = 1,
    ImpactAoE = 2
}

public enum CombatControlStateType
{
    None = 0,
    Idle = 1,
    Moving = 2,
    Attacking = 3,
    Casting = 4,
    Charging = 5,
    TalkingToNpc = 6,
    Stunned = 7,
    Dead = 8
}

public enum CombatCuePhase
{
    None = 0,
    CastStart = 1,
    ChargeStart = 2,
    ChargeLoop = 3,
    Release = 4,
    ProjectileSpawn = 5,
    TravelLoop = 6,
    HitImpact = 7,
    MissImpact = 8,
    Finish = 9
}

public enum CombatSpawnTargetKind
{
    None = 0,
    Self = 1,
    Weapon = 2,
    Projectile = 3,
    Target = 4,
    WorldPoint = 5
}

public enum CombatElementType
{
    None = 0,
    Fire = 1,
    Ice = 2,
    Air = 3,
    Poison = 4
}

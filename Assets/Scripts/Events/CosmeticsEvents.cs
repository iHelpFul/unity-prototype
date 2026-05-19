using UnityEngine;
public struct HitImpactEvent
{
    public float Duration;
    public float TimeScale;
    public int Damage;
}

public struct CharacterKnockbackEvent
{
    public Transform Target;
    public float DirectionX; 
    public float Force;
    public float Duration;
}

public enum CombatFloatingTextKind
{
    Damage = 0,
    SurgeDamage = 1,
    Evade = 2
}

public struct DamageNumberEvent
{
    public Vector3 WorldPosition;
    public Transform Target;
    public CombatFloatingTextKind Kind;
    public int Damage;
    public string Text;
    public bool UseCustomColor;
    public Color TextColor;
    public float Scale;
}

public struct PlaySfxEvent
{
    public SfxType Type;
    public Vector3 Position;
    public float Delay;
    public Transform FollowTarget;
    public Vector3 FollowOffset;
    public bool Persistent;
    public Transform TrackingTarget;
}

public struct StopSfxEvent
{
    public SfxType Type;
    public Transform TrackingTarget;
}

public struct PlayVfxEvent
{
    public VfxType Type;
    public Vector3 Position;
    public Quaternion Rotation;
    public float Delay;
    public Transform FollowTarget;
    public Vector3 FollowOffset;
    public bool OverrideLifetime;
    public float Lifetime;
    public bool Persistent;
    public Transform TrackingTarget;
}

public struct StopVfxEvent
{
    public VfxType Type;
    public Transform TrackingTarget;
}



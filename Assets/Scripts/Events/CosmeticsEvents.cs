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
public struct DamageNumberEvent
{
    public Vector3 WorldPosition;
    public int Damage;
}

public struct PlaySfxEvent
{
    public SfxType Type;
    public Vector3 Position;
}

public struct PlayVfxEvent
{
    public VfxType Type;
    public Vector3 Position;
    public Quaternion Rotation;
}



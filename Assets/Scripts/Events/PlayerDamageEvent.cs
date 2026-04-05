using UnityEngine;

public struct PlayerDamagedEvent
{
    public PlayerCharacter Target;
    public string CharacterId;
    public Transform Source;
    public int Damage;
    public float HitDirection;
}

public struct PlayerHitEvent
{
    public PlayerCharacter Target;
    public string CharacterId;
}

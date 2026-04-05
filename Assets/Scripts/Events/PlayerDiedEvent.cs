using UnityEngine;

public struct PlayerDiedEvent
{
    public PlayerCharacter Target;
    public string CharacterId;
}

public struct PlayerRespawnedEvent
{
    public PlayerCharacter Target;
    public string CharacterId;
}

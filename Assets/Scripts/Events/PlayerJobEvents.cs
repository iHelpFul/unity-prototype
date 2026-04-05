public struct PlayerJobStateChangedEvent
{
    public PlayerCharacter Target;
    public string CharacterId;
    public PlayerJobType CurrentJob;
    public bool IsJobAdvancementAvailable;
}

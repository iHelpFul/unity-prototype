public struct MapTransitionRequestEvent
{
    public PlayerCharacter Requester;
    public string CharacterId;
    public string SourcePortalId;
    public string TargetMapId;
    public string TargetSpawnId;
}

public struct MapTransitionStartedEvent
{
    public PlayerCharacter Requester;
    public string CharacterId;
    public string SourcePortalId;
    public string TargetMapId;
    public string TargetSpawnId;
}

public struct MapTransitionCompletedEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
    public string TargetMapId;
    public string TargetSpawnId;
    public string SceneName;
}

public enum PlayerEmpoweredBasicStateChangeReason
{
    Activated = 0,
    Consumed = 1,
    Expired = 2,
    Cleared = 3
}

public enum PlayerReadyStateChangeReason
{
    Activated = 0,
    Consumed = 1,
    Expired = 2,
    Cleared = 3
}

public struct PlayerFlowCompletedEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
    public int CompletedStackCount;
    public EmpoweredBasicDefinition EmpoweredBasic;
}

public struct PlayerEmpoweredBasicStateChangedEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
    public bool IsActive;
    public PlayerEmpoweredBasicStateChangeReason Reason;
    public float RemainingDuration;
    public float TotalDuration;
    public EmpoweredBasicDefinition EmpoweredBasic;
}

public struct PlayerReadyStateChangedEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
    public bool IsActive;
    public PlayerReadyStateChangeReason Reason;
    public float RemainingDuration;
    public float TotalDuration;
    public PlayerReadyStateType ReadyStateType;
}

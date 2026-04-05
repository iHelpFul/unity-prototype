public enum GameplayNotificationCategory
{
    Item,
    Mesos,
    Experience,
    Kill,
    System
}

public struct GameplayNotificationEvent
{
    public PlayerCharacter Target;
    public string CharacterId;
    public GameplayNotificationCategory Category;
    public string Message;
}

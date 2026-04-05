public struct PlayerHealthChangedEvent
{
    public PlayerCharacter Target;
    public string CharacterId;
    public int CurrentHP;
    public int MaxHP;
}

public struct PlayerManaChangedEvent
{
    public PlayerCharacter Target;
    public string CharacterId;
    public int CurrentMP;
    public int MaxMP;
}

public struct PlayerExpChangedEvent
{
    public PlayerCharacter Target;
    public string CharacterId;
    public int CurrentExp;
    public int RequiredExp;
}

public struct PlayerLevelUpEvent
{
    public PlayerCharacter Target;
    public string CharacterId;
    public int NewLevel;
}

public struct PlayerCurrencyChangedEvent
{
    public PlayerCharacter Target;
    public string CharacterId;
    public int Mesos;
}

public struct PlayerConsumablesChangedEvent
{
    public PlayerCharacter Target;
    public string CharacterId;
    public int RedPotions;
    public int BluePotions;
}

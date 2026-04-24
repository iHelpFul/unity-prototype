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
    public int UnspentStatPoints;
    public int UnspentSkillPoints;
}

public struct PlayerLevelUpEvent
{
    public PlayerCharacter Target;
    public string CharacterId;
    public int NewLevel;
    public int UnspentStatPoints;
    public int UnspentSkillPoints;
    public int StatPointsAwarded;
    public int SkillPointsAwarded;
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

public struct PlayerActionBarSlotChangedEvent
{
    public PlayerCharacter Target;
    public string CharacterId;
    public int SlotIndex;
    public PlayerActionBarAssignmentKind AssignmentKind;
    public string AssignedId;
    public PlayerConsumableType ConsumableType;
    public PlayerInputActionId SystemAction;
}

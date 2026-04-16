using UnityEngine;

public struct MoveInputEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
    public Vector2 Direction;
}

public struct JumpPressedEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
}

public struct JumpReleasedEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
}

public struct AttackPressedEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
}

public struct InteractPressedEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
}

public struct UseConsumablePressedEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
    public PlayerConsumableType ConsumableType;
}

public struct SkillSlotPressedEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
    public int SlotIndex;
}

public struct InventoryTogglePressedEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
}

public struct ProgressionTogglePressedEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
}

public struct QuestLogTogglePressedEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
}


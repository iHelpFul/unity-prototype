using System.Collections.Generic;

public enum CharacterFlowAction
{
    Refresh,
    Select,
    Create,
    EnterWorld
}

public struct CharacterSelectionRefreshRequestEvent
{
}

public struct CharacterSlotSelectRequestEvent
{
    public int SlotIndex;
}

public struct CharacterCreateRequestEvent
{
    public int SlotIndex;
    public string Nickname;
    public CharacterAppearanceData Appearance;
}

public struct CharacterEnterWorldRequestEvent
{
}

public struct CharacterSelectionStateChangedEvent
{
    public CharacterSelectionSnapshot Snapshot;
}

public struct CharacterCreationPanelVisibilityChangedEvent
{
    public bool IsVisible;
}

public struct CharacterFlowResultEvent
{
    public CharacterFlowAction Action;
    public bool IsSuccess;
    public string Message;
    public int SlotIndex;
    public string CharacterId;
}

public sealed class CharacterSlotSnapshot
{
    public int SlotIndex { get; }
    public bool IsOccupied { get; }
    public bool IsSelected { get; }
    public string CharacterId { get; }
    public string Nickname { get; }
    public int Level { get; }
    public PlayerJobType JobType { get; }
    public string JobDisplayName { get; }
    public string CurrentMapId { get; }
    public CharacterAppearanceData Appearance { get; }

    public CharacterSlotSnapshot(
        int slotIndex,
        bool isOccupied,
        bool isSelected,
        string characterId,
        string nickname,
        int level,
        PlayerJobType jobType,
        string jobDisplayName,
        string currentMapId,
        CharacterAppearanceData appearance)
    {
        SlotIndex = slotIndex;
        IsOccupied = isOccupied;
        IsSelected = isSelected;
        CharacterId = characterId;
        Nickname = nickname;
        Level = level;
        JobType = jobType;
        JobDisplayName = jobDisplayName;
        CurrentMapId = currentMapId;
        Appearance = appearance;
    }
}

public sealed class CharacterSelectionSnapshot
{
    public string Username { get; }
    public int MaxCharacterSlots { get; }
    public string SelectedCharacterId { get; }
    public int SelectedSlotIndex { get; }
    public bool CanEnterWorld { get; }
    public bool IsEnteringWorld { get; }
    public CharacterAppearanceCatalogAsset AppearanceCatalog { get; }
    public IReadOnlyList<CharacterSlotSnapshot> Slots { get; }

    public CharacterSelectionSnapshot(
        string username,
        int maxCharacterSlots,
        string selectedCharacterId,
        int selectedSlotIndex,
        bool canEnterWorld,
        bool isEnteringWorld,
        CharacterAppearanceCatalogAsset appearanceCatalog,
        IReadOnlyList<CharacterSlotSnapshot> slots)
    {
        Username = username;
        MaxCharacterSlots = maxCharacterSlots;
        SelectedCharacterId = selectedCharacterId;
        SelectedSlotIndex = selectedSlotIndex;
        CanEnterWorld = canEnterWorld;
        IsEnteringWorld = isEnteringWorld;
        AppearanceCatalog = appearanceCatalog;
        Slots = slots;
    }
}

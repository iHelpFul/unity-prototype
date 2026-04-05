[System.Serializable]
public class CharacterSlotData
{
    public int SlotIndex;
    public string CharacterId;

    public bool IsOccupied => !string.IsNullOrWhiteSpace(CharacterId);
}

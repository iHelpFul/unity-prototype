using System.Collections.Generic;

[System.Serializable]
public class AccountProfileData
{
    public int DataVersion = 1;
    public string AccountId;
    public string Username;
    public int MaxCharacterSlots = 3;
    public string SelectedCharacterId;
    public List<CharacterSlotData> CharacterSlots = new List<CharacterSlotData>();
    public List<CharacterSaveData> Characters = new List<CharacterSaveData>();
}

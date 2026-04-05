[System.Serializable]
public class CharacterSaveData
{
    public string CharacterId;
    public string Nickname;
    public CharacterAppearanceData Appearance = new CharacterAppearanceData();
    public PlayerRuntimeData RuntimeData = new PlayerRuntimeData();
}

using System;
using System.Collections.Generic;

public class PlayerSessionPersistenceService
{
    private const int CurrentDataVersion = 1;
    private const int DefaultCharacterSlots = 3;

    private readonly LocalAccountRepository accountRepository = new LocalAccountRepository();

    public AccountProfileData LoadOrCreateAccountProfile(string startMapId, string startSpawnId)
    {
        AccountProfileData accountProfile = accountRepository.Load();

        if (accountProfile == null || accountProfile.DataVersion != CurrentDataVersion)
            accountProfile = CreateEmptyAccountProfile();

        NormalizeAccountProfile(accountProfile, startMapId, startSpawnId);
        accountRepository.Save(accountProfile);
        return accountProfile;
    }

    public void Save(AccountProfileData accountProfile)
    {
        accountRepository.Save(accountProfile);
    }

    public CharacterSaveData ResolveSelectedCharacter(AccountProfileData accountProfile, string startMapId, string startSpawnId)
    {
        if (accountProfile == null)
            return null;

        NormalizeAccountProfile(accountProfile, startMapId, startSpawnId);

        CharacterSaveData selectedCharacter = FindCharacter(accountProfile, accountProfile.SelectedCharacterId);
        if (selectedCharacter != null)
            return selectedCharacter;

        CharacterSaveData firstOccupiedCharacter = GetFirstOccupiedCharacter(accountProfile);
        if (firstOccupiedCharacter != null)
        {
            accountProfile.SelectedCharacterId = firstOccupiedCharacter.CharacterId;
            return firstOccupiedCharacter;
        }

        return null;
    }

    public IReadOnlyList<CharacterSlotData> GetCharacterSlots(AccountProfileData accountProfile)
    {
        if (accountProfile?.CharacterSlots == null)
            return Array.Empty<CharacterSlotData>();

        return accountProfile.CharacterSlots;
    }

    public CharacterSaveData GetCharacterAtSlot(AccountProfileData accountProfile, int slotIndex)
    {
        if (accountProfile == null)
            return null;

        CharacterSlotData slot = FindSlot(accountProfile, slotIndex);
        return slot != null ? FindCharacter(accountProfile, slot.CharacterId) : null;
    }

    public bool TrySelectCharacter(AccountProfileData accountProfile, string characterId, out CharacterSaveData selectedCharacter)
    {
        selectedCharacter = null;

        if (accountProfile == null || string.IsNullOrWhiteSpace(characterId))
            return false;

        selectedCharacter = FindCharacter(accountProfile, characterId.Trim());
        if (selectedCharacter == null)
            return false;

        accountProfile.SelectedCharacterId = selectedCharacter.CharacterId;
        return true;
    }

    public bool TryCreateCharacter(
        AccountProfileData accountProfile,
        int slotIndex,
        string nickname,
        CharacterAppearanceData appearance,
        string startMapId,
        string startSpawnId,
        out CharacterSaveData createdCharacter)
    {
        createdCharacter = null;

        if (accountProfile == null)
            return false;

        NormalizeAccountProfile(accountProfile, startMapId, startSpawnId);

        CharacterSlotData slot = FindSlot(accountProfile, slotIndex);
        if (slot == null || slot.IsOccupied)
            return false;

        createdCharacter = CharacterSaveFactory.CreateDefaultDrifterCharacter(
            nickname,
            appearance,
            startMapId,
            startSpawnId);

        accountProfile.Characters.Add(createdCharacter);
        slot.CharacterId = createdCharacter.CharacterId;

        if (string.IsNullOrWhiteSpace(accountProfile.SelectedCharacterId))
            accountProfile.SelectedCharacterId = createdCharacter.CharacterId;

        return true;
    }

    private AccountProfileData CreateEmptyAccountProfile()
    {
        AccountProfileData accountProfile = new AccountProfileData
        {
            DataVersion = CurrentDataVersion,
            AccountId = Guid.NewGuid().ToString("N"),
            Username = "LocalAccount",
            MaxCharacterSlots = DefaultCharacterSlots,
            SelectedCharacterId = string.Empty,
            CharacterSlots = new List<CharacterSlotData>(),
            Characters = new List<CharacterSaveData>()
        };

        for (int slotIndex = 0; slotIndex < DefaultCharacterSlots; slotIndex++)
        {
            accountProfile.CharacterSlots.Add(new CharacterSlotData
            {
                SlotIndex = slotIndex,
                CharacterId = string.Empty
            });
        }

        return accountProfile;
    }

    private void NormalizeAccountProfile(AccountProfileData accountProfile, string startMapId, string startSpawnId)
    {
        if (accountProfile == null)
            return;

        accountProfile.DataVersion = CurrentDataVersion;
        accountProfile.AccountId = string.IsNullOrWhiteSpace(accountProfile.AccountId)
            ? Guid.NewGuid().ToString("N")
            : accountProfile.AccountId.Trim();

        accountProfile.Username = string.IsNullOrWhiteSpace(accountProfile.Username)
            ? "LocalAccount"
            : accountProfile.Username.Trim();

        accountProfile.MaxCharacterSlots = Math.Max(DefaultCharacterSlots, accountProfile.MaxCharacterSlots);
        accountProfile.CharacterSlots ??= new List<CharacterSlotData>();
        accountProfile.Characters ??= new List<CharacterSaveData>();

        EnsureSlots(accountProfile);
        NormalizeCharacters(accountProfile, startMapId, startSpawnId);
        ClearDanglingSlots(accountProfile);

        if (FindCharacter(accountProfile, accountProfile.SelectedCharacterId) == null)
        {
            CharacterSaveData firstOccupiedCharacter = GetFirstOccupiedCharacter(accountProfile);
            accountProfile.SelectedCharacterId = firstOccupiedCharacter != null
                ? firstOccupiedCharacter.CharacterId
                : string.Empty;
        }
    }

    private void EnsureSlots(AccountProfileData accountProfile)
    {
        HashSet<int> existingSlotIndices = new HashSet<int>();

        for (int index = accountProfile.CharacterSlots.Count - 1; index >= 0; index--)
        {
            CharacterSlotData slot = accountProfile.CharacterSlots[index];
            if (slot == null || slot.SlotIndex < 0 || slot.SlotIndex >= accountProfile.MaxCharacterSlots || !existingSlotIndices.Add(slot.SlotIndex))
                accountProfile.CharacterSlots.RemoveAt(index);
        }

        for (int slotIndex = 0; slotIndex < accountProfile.MaxCharacterSlots; slotIndex++)
        {
            if (FindSlot(accountProfile, slotIndex) != null)
                continue;

            accountProfile.CharacterSlots.Add(new CharacterSlotData
            {
                SlotIndex = slotIndex,
                CharacterId = string.Empty
            });
        }

        accountProfile.CharacterSlots.Sort((left, right) => left.SlotIndex.CompareTo(right.SlotIndex));
    }

    private void NormalizeCharacters(AccountProfileData accountProfile, string startMapId, string startSpawnId)
    {
        for (int index = accountProfile.Characters.Count - 1; index >= 0; index--)
        {
            CharacterSaveData character = accountProfile.Characters[index];
            if (character == null)
            {
                accountProfile.Characters.RemoveAt(index);
                continue;
            }

            character.CharacterId = string.IsNullOrWhiteSpace(character.CharacterId)
                ? Guid.NewGuid().ToString("N")
                : character.CharacterId.Trim();

            character.Nickname = string.IsNullOrWhiteSpace(character.Nickname)
                ? $"Character {index + 1}"
                : character.Nickname.Trim();

            character.Appearance ??= new CharacterAppearanceData();
            character.RuntimeData ??= CharacterSaveFactory.CreateDefaultRuntimeData(startMapId, startSpawnId);

            NormalizeRuntimeData(character.RuntimeData, startMapId, startSpawnId);
        }
    }

    private void NormalizeRuntimeData(PlayerRuntimeData runtimeData, string startMapId, string startSpawnId)
    {
        if (runtimeData == null)
            return;

        runtimeData.Inventory ??= new List<InventoryEntry>();
        runtimeData.EquippedItems ??= new List<EquippedItemEntry>();
        runtimeData.UnlockedSkills ??= new List<PlayerSkillEntry>();
        PlayerPassiveRuntimeUtility.EnsureCollections(runtimeData);
        PlayerInputBindingUtility.EnsureDefaultInputData(runtimeData);
        runtimeData.QuestProgress ??= new List<PlayerQuestProgressEntry>();
        NormalizeQuestProgress(runtimeData.QuestProgress);

        PlayerProgressionRules.Normalize(runtimeData);

        if (string.IsNullOrWhiteSpace(runtimeData.CurrentMapId))
            runtimeData.CurrentMapId = startMapId ?? string.Empty;

        if (string.IsNullOrWhiteSpace(runtimeData.LastSpawnId))
            runtimeData.LastSpawnId = string.IsNullOrWhiteSpace(startSpawnId)
                ? SceneSpawnPoint.DefaultSpawnId
                : SceneSpawnPoint.NormalizeSpawnId(startSpawnId);

        runtimeData.PendingMapId = string.Empty;
        runtimeData.PendingSpawnId = string.Empty;
    }

    private void NormalizeQuestProgress(List<PlayerQuestProgressEntry> questProgress)
    {
        if (questProgress == null)
            return;

        for (int index = questProgress.Count - 1; index >= 0; index--)
        {
            PlayerQuestProgressEntry entry = questProgress[index];
            if (entry == null)
            {
                questProgress.RemoveAt(index);
                continue;
            }

            entry.ObjectiveProgress ??= new List<PlayerQuestObjectiveProgress>();
        }
    }

    private void ClearDanglingSlots(AccountProfileData accountProfile)
    {
        for (int index = 0; index < accountProfile.CharacterSlots.Count; index++)
        {
            CharacterSlotData slot = accountProfile.CharacterSlots[index];
            if (slot == null || string.IsNullOrWhiteSpace(slot.CharacterId))
                continue;

            if (FindCharacter(accountProfile, slot.CharacterId) == null)
                slot.CharacterId = string.Empty;
        }
    }

    private CharacterSaveData GetFirstOccupiedCharacter(AccountProfileData accountProfile)
    {
        if (accountProfile?.CharacterSlots == null)
            return null;

        for (int index = 0; index < accountProfile.CharacterSlots.Count; index++)
        {
            CharacterSlotData slot = accountProfile.CharacterSlots[index];
            if (slot == null || string.IsNullOrWhiteSpace(slot.CharacterId))
                continue;

            CharacterSaveData character = FindCharacter(accountProfile, slot.CharacterId);
            if (character != null)
                return character;
        }

        return null;
    }

    private CharacterSaveData FindCharacter(AccountProfileData accountProfile, string characterId)
    {
        if (accountProfile?.Characters == null || string.IsNullOrWhiteSpace(characterId))
            return null;

        string normalizedCharacterId = characterId.Trim();

        for (int index = 0; index < accountProfile.Characters.Count; index++)
        {
            CharacterSaveData character = accountProfile.Characters[index];
            if (character != null && character.CharacterId == normalizedCharacterId)
                return character;
        }

        return null;
    }

    private CharacterSlotData FindSlot(AccountProfileData accountProfile, int slotIndex)
    {
        if (accountProfile?.CharacterSlots == null)
            return null;

        for (int index = 0; index < accountProfile.CharacterSlots.Count; index++)
        {
            CharacterSlotData slot = accountProfile.CharacterSlots[index];
            if (slot != null && slot.SlotIndex == slotIndex)
                return slot;
        }

        return null;
    }

}

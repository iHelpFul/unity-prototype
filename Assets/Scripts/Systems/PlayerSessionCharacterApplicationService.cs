using System;
using System.Collections.Generic;

public class PlayerSessionCharacterApplicationService
{
    private readonly PlayerSessionPersistenceService persistenceService;
    private readonly PlayerSessionRuntimeService runtimeService;
    private readonly PlayerSessionEventPublisher eventPublisher;
    private readonly Func<string> getActiveSceneName;
    private readonly Func<string> getDefaultMapId;
    private readonly Func<string> getDefaultSpawnId;
    private readonly Action savePlayer;

    public PlayerSessionCharacterApplicationService(
        PlayerSessionPersistenceService persistenceService,
        PlayerSessionRuntimeService runtimeService,
        PlayerSessionEventPublisher eventPublisher,
        Func<string> getActiveSceneName,
        Func<string> getDefaultMapId,
        Func<string> getDefaultSpawnId,
        Action savePlayer)
    {
        this.persistenceService = persistenceService;
        this.runtimeService = runtimeService;
        this.eventPublisher = eventPublisher;
        this.getActiveSceneName = getActiveSceneName;
        this.getDefaultMapId = getDefaultMapId;
        this.getDefaultSpawnId = getDefaultSpawnId;
        this.savePlayer = savePlayer;
    }

    public AccountProfileData AccountData { get; private set; }
    public CharacterSaveData ActiveCharacter { get; private set; }
    public PlayerRuntimeData PlayerData { get; private set; }

    public void Save()
    {
        if (AccountData == null)
            return;

        savePlayer?.Invoke();
    }

    public void PublishSessionState(PlayerCharacter player)
    {
        if (PlayerData == null || player == null || !player.IsLocalPlayer)
            return;

        eventPublisher?.PublishSessionState(PlayerData, player);
    }

    public void LoadOrCreateSession()
    {
        string activeSceneName = ResolveActiveSceneName();
        string defaultMapId = ResolveDefaultMapId();
        string defaultSpawnId = ResolveDefaultSpawnId();

        AccountData = persistenceService.LoadOrCreateAccountProfile(defaultMapId, defaultSpawnId);

        CharacterSaveData resolvedCharacter = persistenceService.ResolveSelectedCharacter(
            AccountData,
            defaultMapId,
            defaultSpawnId);

        ApplyCharacterSession(
            resolvedCharacter,
            activeSceneName,
            defaultMapId,
            defaultSpawnId);
    }

    public IReadOnlyList<CharacterSlotData> GetCharacterSlots()
    {
        return persistenceService.GetCharacterSlots(AccountData);
    }

    public CharacterSaveData GetCharacterAtSlot(int slotIndex)
    {
        return persistenceService.GetCharacterAtSlot(AccountData, slotIndex);
    }

    public bool TrySelectCharacter(
        string characterId)
    {
        string activeSceneName = ResolveActiveSceneName();
        string defaultMapId = ResolveDefaultMapId();
        string defaultSpawnId = ResolveDefaultSpawnId();

        if (!persistenceService.TrySelectCharacter(AccountData, characterId, out CharacterSaveData selectedCharacter))
            return false;

        ApplyCharacterSession(
            selectedCharacter,
            activeSceneName,
            defaultMapId,
            defaultSpawnId);

        savePlayer?.Invoke();
        return true;
    }

    public bool TryCreateCharacter(
        int slotIndex,
        string nickname,
        CharacterAppearanceData appearance)
    {
        string activeSceneName = ResolveActiveSceneName();
        string defaultMapId = ResolveDefaultMapId();
        string defaultSpawnId = ResolveDefaultSpawnId();

        if (!persistenceService.TryCreateCharacter(
            AccountData,
            slotIndex,
            nickname,
            appearance,
            defaultMapId,
            defaultSpawnId,
            out CharacterSaveData createdCharacter))
        {
            return false;
        }

        if (ActiveCharacter == null)
        {
            ApplyCharacterSession(
                createdCharacter,
                activeSceneName,
                defaultMapId,
                defaultSpawnId);
        }

        savePlayer?.Invoke();
        return true;
    }

    private string ResolveActiveSceneName()
    {
        return getActiveSceneName != null ? getActiveSceneName() : string.Empty;
    }

    private string ResolveDefaultMapId()
    {
        return getDefaultMapId != null ? getDefaultMapId() : string.Empty;
    }

    private string ResolveDefaultSpawnId()
    {
        return getDefaultSpawnId != null ? getDefaultSpawnId() : string.Empty;
    }

    private void ApplyCharacterSession(
        CharacterSaveData character,
        string activeSceneName,
        string defaultMapId,
        string defaultSpawnId)
    {
        ActiveCharacter = character;
        PlayerData = runtimeService.ApplyCharacterRuntime(
            character,
            activeSceneName,
            defaultMapId,
            defaultSpawnId);
    }
}

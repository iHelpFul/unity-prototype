using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-965)]
public class CharacterFlowService : MonoBehaviour
{
    [SerializeField] private float postLoadSettleTime = 0.05f;
    [SerializeField] private GameBootstrap bootstrap;

    private Coroutine activeEnterWorldRoutine;

    public bool IsEnteringWorld => activeEnterWorldRoutine != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeInstance()
    {
        CharacterFlowService[] services = FindObjectsByType<CharacterFlowService>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (services.Length > 0)
            return;

        GameObject serviceObject = new GameObject("[CharacterFlowService]");
        serviceObject.AddComponent<CharacterFlowService>();
    }

    private void Awake()
    {
        if (ShouldDestroyDuplicate())
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        ResolveBootstrap();
    }

    private void Start()
    {
        PublishSelectionState();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<CharacterSelectionRefreshRequestEvent>(OnRefreshRequested);
        EventBus.Subscribe<CharacterSlotSelectRequestEvent>(OnSlotSelectRequested);
        EventBus.Subscribe<CharacterCreateRequestEvent>(OnCharacterCreateRequested);
        EventBus.Subscribe<CharacterEnterWorldRequestEvent>(OnEnterWorldRequested);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<CharacterSelectionRefreshRequestEvent>(OnRefreshRequested);
        EventBus.Unsubscribe<CharacterSlotSelectRequestEvent>(OnSlotSelectRequested);
        EventBus.Unsubscribe<CharacterCreateRequestEvent>(OnCharacterCreateRequested);
        EventBus.Unsubscribe<CharacterEnterWorldRequestEvent>(OnEnterWorldRequested);
    }

    private void OnRefreshRequested(CharacterSelectionRefreshRequestEvent e)
    {
        PublishSelectionState();
    }

    private void OnSlotSelectRequested(CharacterSlotSelectRequestEvent e)
    {
        ResolveBootstrap();
        PlayerSessionCharacterApplicationService characterSession = bootstrap != null ? bootstrap.CharacterSession : null;
        if (characterSession == null)
        {
            PublishResult(CharacterFlowAction.Select, false, "Character session is not ready.", e.SlotIndex);
            return;
        }

        CharacterSaveData character = characterSession.GetCharacterAtSlot(e.SlotIndex);
        if (character == null)
        {
            PublishResult(CharacterFlowAction.Select, false, "That slot is empty.", e.SlotIndex);
            PublishSelectionState();
            return;
        }

        bool didSelect = characterSession.TrySelectCharacter(character.CharacterId);
        PublishResult(
            CharacterFlowAction.Select,
            didSelect,
            didSelect ? $"Selected {character.Nickname}." : "The character could not be selected.",
            e.SlotIndex,
            character.CharacterId);
        PublishSelectionState();
    }

    private void OnCharacterCreateRequested(CharacterCreateRequestEvent e)
    {
        ResolveBootstrap();
        PlayerSessionCharacterApplicationService characterSession = bootstrap != null ? bootstrap.CharacterSession : null;
        if (characterSession == null)
        {
            PublishResult(CharacterFlowAction.Create, false, "Character session is not ready.", e.SlotIndex);
            return;
        }

        CharacterAppearanceData appearance = e.Appearance != null ? e.Appearance.Clone() : new CharacterAppearanceData();
        string validationMessage;
        if (!ValidateCreateRequest(e.SlotIndex, e.Nickname, out validationMessage))
        {
            PublishResult(CharacterFlowAction.Create, false, validationMessage, e.SlotIndex);
            PublishSelectionState();
            return;
        }

        string normalizedNickname = e.Nickname.Trim();
        if (!characterSession.TryCreateCharacter(e.SlotIndex, normalizedNickname, appearance))
        {
            PublishResult(CharacterFlowAction.Create, false, "The character could not be created.", e.SlotIndex);
            PublishSelectionState();
            return;
        }

        CharacterSaveData createdCharacter = characterSession.GetCharacterAtSlot(e.SlotIndex);
        if (createdCharacter != null)
            characterSession.TrySelectCharacter(createdCharacter.CharacterId);

        PublishResult(
            CharacterFlowAction.Create,
            createdCharacter != null,
            createdCharacter != null ? $"{createdCharacter.Nickname} is ready." : "The character was created, but could not be selected.",
            e.SlotIndex,
            createdCharacter != null ? createdCharacter.CharacterId : string.Empty);
        PublishSelectionState();
    }

    private void OnEnterWorldRequested(CharacterEnterWorldRequestEvent e)
    {
        if (IsEnteringWorld)
        {
            PublishResult(CharacterFlowAction.EnterWorld, false, "Already entering the world.");
            return;
        }

        ResolveBootstrap();
        PlayerSessionCharacterApplicationService characterSession = bootstrap != null ? bootstrap.CharacterSession : null;
        if (characterSession == null || characterSession.ActiveCharacter == null || characterSession.PlayerData == null)
        {
            PublishResult(CharacterFlowAction.EnterWorld, false, "Select a character first.");
            PublishSelectionState();
            return;
        }

        string targetMapId = MapRegistry.NormalizeMapId(characterSession.PlayerData.CurrentMapId);
        string targetSpawnId = SceneSpawnPoint.NormalizeSpawnId(characterSession.PlayerData.LastSpawnId);
        if (string.IsNullOrWhiteSpace(targetMapId))
        {
            PublishResult(CharacterFlowAction.EnterWorld, false, "That character has no starting map.");
            PublishSelectionState();
            return;
        }

        if (!WorldRuntimeSceneUtility.TryResolveSceneName(targetMapId, out string sceneName))
        {
            PublishResult(CharacterFlowAction.EnterWorld, false, $"Could not resolve a scene for map '{targetMapId}'.");
            PublishSelectionState();
            return;
        }

        activeEnterWorldRoutine = StartCoroutine(EnterWorldRoutine(sceneName, targetMapId, targetSpawnId));
        PublishSelectionState();
    }

    private IEnumerator EnterWorldRoutine(string sceneName, string targetMapId, string targetSpawnId)
    {
        ResolveBootstrap();
        WorldRuntimeSceneUtility.BeginSceneTransition(
            bootstrap,
            null,
            string.Empty,
            targetMapId,
            targetSpawnId);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (loadOperation == null)
        {
            bootstrap?.MapSession?.ClearPendingMapTransition();
            activeEnterWorldRoutine = null;
            PublishResult(CharacterFlowAction.EnterWorld, false, "The world could not be loaded.");
            PublishSelectionState();
            yield break;
        }

        yield return WorldRuntimeSceneUtility.WaitForSceneLoadAndSettle(loadOperation, postLoadSettleTime);

        ResolveBootstrap();
        WorldRuntimeSceneUtility.CompleteSceneTransition(
            bootstrap,
            sceneName,
            targetMapId,
            targetSpawnId,
            FindObjectsInactive.Include,
            false);

        activeEnterWorldRoutine = null;
        PublishResult(CharacterFlowAction.EnterWorld, true, "Entering world.");
        PublishSelectionState();
    }

    private void PublishSelectionState()
    {
        ResolveBootstrap();
        CharacterSelectionSnapshot snapshot = BuildSnapshot();
        if (snapshot == null)
            return;

        EventBus.Publish(new CharacterSelectionStateChangedEvent
        {
            Snapshot = snapshot
        });
    }

    private CharacterSelectionSnapshot BuildSnapshot()
    {
        PlayerSessionCharacterApplicationService characterSession = bootstrap != null ? bootstrap.CharacterSession : null;
        if (characterSession == null || characterSession.AccountData == null)
            return null;

        IReadOnlyList<CharacterSlotData> slots = characterSession.GetCharacterSlots();
        List<CharacterSlotSnapshot> slotSnapshots = new List<CharacterSlotSnapshot>();
        int selectedSlotIndex = -1;

        for (int index = 0; index < slots.Count; index++)
        {
            CharacterSlotData slot = slots[index];
            if (slot == null)
                continue;

            CharacterSaveData character = characterSession.GetCharacterAtSlot(slot.SlotIndex);
            bool isSelected = character != null
                && characterSession.ActiveCharacter != null
                && character.CharacterId == characterSession.ActiveCharacter.CharacterId;

            if (isSelected)
                selectedSlotIndex = slot.SlotIndex;

            slotSnapshots.Add(new CharacterSlotSnapshot(
                slot.SlotIndex,
                character != null,
                isSelected,
                character != null ? character.CharacterId : string.Empty,
                character != null ? character.Nickname : string.Empty,
                character != null && character.RuntimeData != null ? character.RuntimeData.Level : 0,
                character != null && character.RuntimeData != null ? character.RuntimeData.CurrentJob : PlayerJobType.Novice,
                character != null && character.RuntimeData != null
                    ? GameBootstrap.FormatJobName(character.RuntimeData.CurrentJob)
                    : string.Empty,
                character != null && character.RuntimeData != null ? character.RuntimeData.CurrentMapId : string.Empty,
                character != null
                    ? CharacterAppearanceResolver.Resolve(
                        character.Appearance,
                        character.RuntimeData != null ? character.RuntimeData.Inventory : null,
                        character.RuntimeData != null ? character.RuntimeData.EquippedItems : null)
                    : null));
        }

        return new CharacterSelectionSnapshot(
            characterSession.AccountData.Username,
            characterSession.AccountData.MaxCharacterSlots,
            characterSession.ActiveCharacter != null ? characterSession.ActiveCharacter.CharacterId : string.Empty,
            selectedSlotIndex,
            characterSession.ActiveCharacter != null && characterSession.PlayerData != null,
            IsEnteringWorld,
            CharacterAppearanceCatalogDatabase.GetCatalog(),
            slotSnapshots);
    }

    private bool ValidateCreateRequest(int slotIndex, string nickname, out string message)
    {
        message = string.Empty;

        PlayerSessionCharacterApplicationService characterSession = bootstrap != null ? bootstrap.CharacterSession : null;
        if (characterSession == null)
        {
            message = "Character session is not ready.";
            return false;
        }

        IReadOnlyList<CharacterSlotData> slots = characterSession.GetCharacterSlots();
        CharacterSlotData targetSlot = null;

        for (int index = 0; index < slots.Count; index++)
        {
            CharacterSlotData slot = slots[index];
            if (slot != null && slot.SlotIndex == slotIndex)
            {
                targetSlot = slot;
                break;
            }
        }

        if (targetSlot == null)
        {
            message = "That slot does not exist.";
            return false;
        }

        if (targetSlot.IsOccupied)
        {
            message = "That slot is already occupied.";
            return false;
        }

        string normalizedNickname = string.IsNullOrWhiteSpace(nickname) ? string.Empty : nickname.Trim();
        if (normalizedNickname.Length < 3)
        {
            message = "Nickname must be at least 3 characters.";
            return false;
        }

        if (normalizedNickname.Length > 16)
        {
            message = "Nickname must be 16 characters or fewer.";
            return false;
        }

        for (int index = 0; index < slots.Count; index++)
        {
            CharacterSaveData character = characterSession.GetCharacterAtSlot(slots[index].SlotIndex);
            if (character == null || string.IsNullOrWhiteSpace(character.Nickname))
                continue;

            if (string.Equals(character.Nickname.Trim(), normalizedNickname, System.StringComparison.OrdinalIgnoreCase))
            {
                message = "That nickname is already in use on this account.";
                return false;
            }
        }

        return true;
    }

    private void ResolveBootstrap()
    {
        bootstrap = GameBootstrap.FindReadyBootstrap(bootstrap);
    }

    private bool ShouldDestroyDuplicate()
    {
        CharacterFlowService[] services = FindObjectsByType<CharacterFlowService>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        CharacterFlowService keeper = this;

        for (int index = 0; index < services.Length; index++)
        {
            CharacterFlowService service = services[index];
            if (service == null)
                continue;

            if (service.GetInstanceID() < keeper.GetInstanceID())
                keeper = service;
        }

        return keeper != this;
    }

    private void PublishResult(
        CharacterFlowAction action,
        bool isSuccess,
        string message,
        int slotIndex = -1,
        string characterId = "")
    {
        EventBus.Publish(new CharacterFlowResultEvent
        {
            Action = action,
            IsSuccess = isSuccess,
            Message = message,
            SlotIndex = slotIndex,
            CharacterId = characterId ?? string.Empty
        });
    }
}

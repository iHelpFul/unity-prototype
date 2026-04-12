using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class GameBootstrap : MonoBehaviour
{
    [Header("Character Defaults")]
    [SerializeField] private string defaultCharacterStartMapId = "Map_01";
    [SerializeField] private string defaultCharacterStartSpawnId = SceneSpawnPoint.DefaultSpawnId;

    private readonly PlayerSessionPersistenceService persistenceService = new PlayerSessionPersistenceService();
    private readonly PlayerSessionInventoryService inventoryService = new PlayerSessionInventoryService();
    private readonly PlayerSessionEquipmentService equipmentService = new PlayerSessionEquipmentService();
    private readonly PlayerSessionSkillService skillService = new PlayerSessionSkillService();
    private readonly PlayerSessionMapStateService mapStateService = new PlayerSessionMapStateService();
    private PlayerSessionEventPublisher eventPublisher;
    private PlayerSessionRuntimeService runtimeService;
    private PlayerSessionCharacterApplicationService characterSessionApplicationService;
    private PlayerSessionInventoryApplicationService inventoryApplicationService;
    private PlayerSessionEquipmentApplicationService equipmentApplicationService;
    private PlayerSessionCurrencyApplicationService currencyApplicationService;
    private PlayerSessionSkillApplicationService skillApplicationService;
    private PlayerSessionJobApplicationService jobApplicationService;
    private PlayerSessionMapApplicationService mapApplicationService;
    public PlayerSessionCharacterApplicationService CharacterSession => characterSessionApplicationService;
    public PlayerSessionInventoryApplicationService InventorySession => inventoryApplicationService;
    public PlayerSessionEquipmentApplicationService EquipmentSession => equipmentApplicationService;
    public PlayerSessionCurrencyApplicationService CurrencySession => currencyApplicationService;
    public PlayerSessionSkillApplicationService SkillSession => skillApplicationService;
    public PlayerSessionJobApplicationService JobSession => jobApplicationService;
    public PlayerSessionMapApplicationService MapSession => mapApplicationService;

    public bool HasSessionData => CharacterSession != null
        && CharacterSession.AccountData != null
        && CharacterSession.ActiveCharacter != null
        && CharacterSession.PlayerData != null;

    private void Awake()
    {
        if (ShouldDestroyDuplicate())
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        EnsureSessionServices();
        characterSessionApplicationService.LoadOrCreateSession();
        SavePlayer();
    }

    private void Start()
    {
        BindPersistentRuntimeServices();
        WorldRuntimeSceneUtility.BindSceneRuntimeContext(
            this,
            null,
            FindObjectsInactive.Include);
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SavePlayer();
    }

    private void OnApplicationQuit()
    {
        SavePlayer();
    }

    private void SavePlayer()
    {
        persistenceService.Save(CharacterSession != null ? CharacterSession.AccountData : null);
    }

    public static GameBootstrap FindReadyBootstrap(GameBootstrap current = null)
    {
        if (current != null && current.HasSessionData)
            return current;

        GameBootstrap[] bootstraps = FindObjectsByType<GameBootstrap>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        GameBootstrap fallback = null;

        foreach (GameBootstrap candidate in bootstraps)
        {
            if (candidate == null)
                continue;

            if (candidate.HasSessionData)
                return candidate;

            fallback ??= candidate;
        }

        return fallback;
    }

    private int GetEffectiveMaxHP()
    {
        EnsureSessionServices();
        return runtimeService.GetEffectiveMaxHP();
    }

    private int GetEffectiveMaxMP()
    {
        EnsureSessionServices();
        return runtimeService.GetEffectiveMaxMP();
    }

    private string GetActiveSceneName()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        return activeScene.IsValid() ? activeScene.name : string.Empty;
    }

    private string GetDefaultCharacterStartMapId()
    {
        string configuredMapId = MapRegistry.NormalizeMapId(defaultCharacterStartMapId);
        if (!string.IsNullOrWhiteSpace(configuredMapId))
            return configuredMapId;

        return GetActiveSceneName();
    }

    private string GetDefaultCharacterStartSpawnId()
    {
        return SceneSpawnPoint.NormalizeSpawnId(defaultCharacterStartSpawnId);
    }

    private bool IsSupportedSessionPlayer(PlayerCharacter player)
    {
        return player == null || player.IsLocalPlayer;
    }

    private void ClampVitalsToEquipmentBonuses()
    {
        EnsureSessionServices();
        runtimeService.ClampVitalsToEquipmentBonuses();
    }

    private PlayerCharacter ResolveScenePlayer(PlayerCharacter player)
    {
        if (player != null)
            return player;

        return WorldRuntimeSceneUtility.FindLocalPlayer(FindObjectsInactive.Include);
    }

    private PlayerCharacter ResolveSupportedScenePlayer(PlayerCharacter player)
    {
        PlayerCharacter scenePlayer = ResolveScenePlayer(player);
        return IsSupportedSessionPlayer(scenePlayer) ? scenePlayer : null;
    }

    private bool ShouldDestroyDuplicate()
    {
        GameBootstrap[] bootstraps = FindObjectsByType<GameBootstrap>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        GameBootstrap keeper = this;

        foreach (GameBootstrap existingBootstrap in bootstraps)
        {
            if (existingBootstrap == null)
                continue;

            if (existingBootstrap.GetInstanceID() < keeper.GetInstanceID())
                keeper = existingBootstrap;
        }

        return keeper != this;
    }

    private void EnsureSessionServices()
    {
        eventPublisher ??= new PlayerSessionEventPublisher(inventoryService, equipmentService);
        runtimeService ??= new PlayerSessionRuntimeService(
            inventoryService,
            equipmentService,
            skillService,
            mapStateService);
        characterSessionApplicationService ??= new PlayerSessionCharacterApplicationService(
            persistenceService,
            runtimeService,
            eventPublisher,
            GetActiveSceneName,
            GetDefaultCharacterStartMapId,
            GetDefaultCharacterStartSpawnId,
            SavePlayer);
        inventoryApplicationService ??= new PlayerSessionInventoryApplicationService(
            inventoryService,
            equipmentService,
            eventPublisher,
            () => CharacterSession != null ? CharacterSession.PlayerData : null,
            GetEffectiveMaxHP,
            GetEffectiveMaxMP,
            SavePlayer);
        equipmentApplicationService ??= new PlayerSessionEquipmentApplicationService(
            inventoryService,
            equipmentService,
            eventPublisher,
            () => CharacterSession != null ? CharacterSession.PlayerData : null,
            () => CharacterSession != null ? CharacterSession.ActiveCharacter : null,
            ClampVitalsToEquipmentBonuses,
            ResolveSupportedScenePlayer,
            SavePlayer);
        currencyApplicationService ??= new PlayerSessionCurrencyApplicationService(
            eventPublisher,
            () => CharacterSession != null ? CharacterSession.PlayerData : null,
            SavePlayer);
        skillApplicationService ??= new PlayerSessionSkillApplicationService(
            skillService,
            SavePlayer);
        jobApplicationService ??= new PlayerSessionJobApplicationService(
            skillService,
            eventPublisher,
            () => CharacterSession != null ? CharacterSession.PlayerData : null,
            SavePlayer);
        mapApplicationService ??= new PlayerSessionMapApplicationService(
            mapStateService,
            () => CharacterSession != null ? CharacterSession.PlayerData : null,
            SavePlayer);
    }

    private void BindPersistentRuntimeServices()
    {
        CharacterFlowService[] characterFlowServices = FindObjectsByType<CharacterFlowService>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int index = 0; index < characterFlowServices.Length; index++)
        {
            CharacterFlowService service = characterFlowServices[index];
            if (service != null)
                service.BindBootstrap(this);
        }

        MapTransitionService[] mapTransitionServices = FindObjectsByType<MapTransitionService>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int index = 0; index < mapTransitionServices.Length; index++)
        {
            MapTransitionService service = mapTransitionServices[index];
            if (service != null)
                service.BindBootstrap(this);
        }

        NpcVendorService[] vendorServices = FindObjectsByType<NpcVendorService>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int index = 0; index < vendorServices.Length; index++)
        {
            NpcVendorService service = vendorServices[index];
            if (service != null)
                service.BindBootstrap(this);
        }

        NpcJobAdvancementService[] advancementServices = FindObjectsByType<NpcJobAdvancementService>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int index = 0; index < advancementServices.Length; index++)
        {
            NpcJobAdvancementService service = advancementServices[index];
            if (service != null)
                service.BindBootstrap(this);
        }
    }

    public static string FormatJobName(PlayerJobType jobType)
    {
        return PlayerJobCombatProfiles.GetDisplayName(jobType);
    }
}

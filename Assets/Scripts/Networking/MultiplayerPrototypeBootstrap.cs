using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum MultiplayerPrototypeStartMode
{
    Manual,
    Host,
    Client
}

[DisallowMultipleComponent]
public class MultiplayerPrototypeBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private string gameplaySceneName = MultiplayerPrototypeRuntime.DefaultGameplaySceneName;
    [SerializeField] private string hostAddress = MultiplayerPrototypeRuntime.DefaultAddress;
    [SerializeField] private ushort hostPort = MultiplayerPrototypeRuntime.DefaultPort;
    [SerializeField] private MultiplayerPrototypeStartMode autoStartMode = MultiplayerPrototypeStartMode.Manual;
    [SerializeField] private bool showRuntimeGui = true;
    [SerializeField] private bool forceRunInBackground = true;
    [SerializeField] private int prototypeTargetFrameRate = 120;

    private static MultiplayerPrototypeBootstrap instance;

    private readonly HashSet<ulong> spawnedPlayerClients = new HashSet<ulong>();

    private NetworkManager networkManager;
    private UnityTransport transport;
    private string portText;
    private string statusMessage = "Choose Host or Client to enter the prototype.";
    private bool callbacksRegistered;
    private bool sceneCallbacksRegistered;
    private GameBootstrap sessionBootstrap;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        portText = hostPort.ToString();
        DontDestroyOnLoad(gameObject);

        ApplyPrototypeRuntimeSettings();
        MultiplayerPrototypeRuntime.Enable(gameplaySceneName);
        EnsureBootCamera();
        EnsureSessionBootstrap();
        EnsureNetworkManager();
    }

    private void Start()
    {
        switch (autoStartMode)
        {
            case MultiplayerPrototypeStartMode.Host:
                StartHost();
                break;

            case MultiplayerPrototypeStartMode.Client:
                StartClient();
                break;
        }
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        MultiplayerPrototypeEnemyCoordinator.FindActive()?.Unbind();
        UnregisterSceneCallbacks();
        UnregisterCallbacks();
        instance = null;
    }

    public void StartHost()
    {
        if (!TryApplyPortField() || !ValidateConfiguration())
            return;

        EnsureNetworkManager();
        RegisterCallbacks();
        ConfigureTransport(listenAddress: "0.0.0.0");
        spawnedPlayerClients.Clear();

        if (!networkManager.StartHost())
        {
            statusMessage = "Host failed to start. Check the console for details.";
            return;
        }

        EnsureEnemyCoordinator();
        RegisterSceneCallbacks();
        statusMessage = $"Host listening on {hostAddress}:{hostPort}.";
    }

    public void StartClient()
    {
        if (!TryApplyPortField() || !ValidateConfiguration())
            return;

        EnsureNetworkManager();
        RegisterCallbacks();
        ConfigureTransport();

        if (!networkManager.StartClient())
        {
            statusMessage = "Client failed to start. Check the console for details.";
            return;
        }

        EnsureEnemyCoordinator();
        RegisterSceneCallbacks();
        statusMessage = $"Connecting to {hostAddress}:{hostPort}...";
    }

    public void ShutdownSession()
    {
        MultiplayerPrototypeEnemyCoordinator.FindActive()?.Unbind();

        if (networkManager != null && networkManager.IsListening)
            networkManager.Shutdown();

        spawnedPlayerClients.Clear();
        statusMessage = "Prototype session stopped.";
    }

    private void EnsureBootCamera()
    {
        if (Camera.main != null)
            return;

        GameObject cameraObject = new GameObject("Prototype Boot Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
        cameraObject.AddComponent<AudioListener>();
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
    }

    private void ApplyPrototypeRuntimeSettings()
    {
        Application.runInBackground = forceRunInBackground;

        int resolvedTargetFrameRate = prototypeTargetFrameRate > 0 ? prototypeTargetFrameRate : 120;
        Application.targetFrameRate = resolvedTargetFrameRate;
    }

    private void EnsureSessionBootstrap()
    {
        if (sessionBootstrap != null)
            return;

        sessionBootstrap = GameBootstrap.FindReadyBootstrap();
        if (sessionBootstrap != null)
            return;

        GameObject bootstrapObject = new GameObject("[GameBootstrap]");
        sessionBootstrap = bootstrapObject.AddComponent<GameBootstrap>();
    }

    private void EnsureNetworkManager()
    {
        if (networkManager == null)
            networkManager = NetworkManager.Singleton;

        if (networkManager == null)
        {
            GameObject networkManagerObject = new GameObject("[MultiplayerPrototypeNetworkManager]");
            networkManager = networkManagerObject.AddComponent<NetworkManager>();
        }

        if (transport == null)
            transport = networkManager.GetComponent<UnityTransport>();

        if (transport == null)
            transport = networkManager.gameObject.AddComponent<UnityTransport>();

        ConfigureNetworkConfig();
    }

    private void ConfigureNetworkConfig()
    {
        if (networkManager.NetworkConfig == null)
            networkManager.NetworkConfig = new NetworkConfig();

        networkManager.NetworkConfig.NetworkTransport = transport;
        networkManager.NetworkConfig.TickRate = 60;
        networkManager.NetworkConfig.EnableSceneManagement = true;
        networkManager.NetworkConfig.ConnectionApproval = false;
        networkManager.NetworkConfig.AutoSpawnPlayerPrefabClientSide = false;
        networkManager.NetworkConfig.NetworkTopology = NetworkTopologyTypes.ClientServer;
        networkManager.NetworkConfig.PlayerPrefab = playerPrefab;
        networkManager.NetworkConfig.Prefabs ??= new NetworkPrefabs();

        if (!ContainsRegisteredPrefab(playerPrefab))
        {
            networkManager.NetworkConfig.Prefabs.Add(new NetworkPrefab
            {
                Prefab = playerPrefab
            });
        }
    }

    private bool ContainsRegisteredPrefab(GameObject prefab)
    {
        if (prefab == null || networkManager.NetworkConfig?.Prefabs == null)
            return false;

        IReadOnlyList<NetworkPrefab> prefabs = networkManager.NetworkConfig.Prefabs.Prefabs;
        for (int index = 0; index < prefabs.Count; index++)
        {
            NetworkPrefab registeredPrefab = prefabs[index];
            if (registeredPrefab != null && registeredPrefab.Prefab == prefab)
                return true;
        }

        return false;
    }

    private void ConfigureTransport(string listenAddress = null)
    {
        if (transport == null)
            return;

        string resolvedAddress = string.IsNullOrWhiteSpace(hostAddress)
            ? MultiplayerPrototypeRuntime.DefaultAddress
            : hostAddress.Trim();

        transport.SetConnectionData(resolvedAddress, hostPort, listenAddress);
    }

    private void RegisterCallbacks()
    {
        if (callbacksRegistered || networkManager == null)
            return;

        networkManager.OnServerStarted += OnServerStarted;
        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        callbacksRegistered = true;
    }

    private void UnregisterCallbacks()
    {
        if (!callbacksRegistered || networkManager == null)
            return;

        networkManager.OnServerStarted -= OnServerStarted;
        networkManager.OnClientConnectedCallback -= OnClientConnected;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        callbacksRegistered = false;
    }

    private void RegisterSceneCallbacks()
    {
        if (sceneCallbacksRegistered || networkManager?.SceneManager == null)
            return;

        networkManager.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        networkManager.SceneManager.OnSynchronizeComplete += OnSynchronizeComplete;
        sceneCallbacksRegistered = true;
    }

    private void UnregisterSceneCallbacks()
    {
        if (!sceneCallbacksRegistered || networkManager?.SceneManager == null)
            return;

        networkManager.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
        networkManager.SceneManager.OnSynchronizeComplete -= OnSynchronizeComplete;
        sceneCallbacksRegistered = false;
    }

    private void OnServerStarted()
    {
        RegisterSceneCallbacks();
        statusMessage = $"Server ready on {hostAddress}:{hostPort}.";
        EnsureEnemyCoordinator();
        LoadGameplaySceneIfNeeded();
    }

    private void OnClientConnected(ulong clientId)
    {
        if (networkManager == null)
            return;

        if (networkManager.IsServer && clientId != networkManager.LocalClientId)
            statusMessage = $"Client {clientId} connected.";

        if (networkManager.IsClient && clientId == networkManager.LocalClientId && !networkManager.IsServer)
        {
            RegisterSceneCallbacks();
            statusMessage = $"Connected to {hostAddress}:{hostPort}. Waiting for scene sync.";
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        spawnedPlayerClients.Remove(clientId);

        if (networkManager == null)
            return;

        if (clientId == networkManager.LocalClientId)
            statusMessage = "Disconnected from the prototype session.";
        else if (networkManager.IsServer)
            statusMessage = $"Client {clientId} disconnected.";
    }

    private void LoadGameplaySceneIfNeeded()
    {
        if (networkManager == null || !networkManager.IsServer || networkManager.SceneManager == null)
            return;

        if (IsGameplaySceneActive())
        {
            EnsureEnemyCoordinator();
            SpawnPlayerForClient(networkManager.LocalClientId);
            return;
        }

        SceneEventProgressStatus sceneLoadStatus = networkManager.SceneManager.LoadScene(
            gameplaySceneName,
            LoadSceneMode.Single);

        if (sceneLoadStatus != SceneEventProgressStatus.Started)
            statusMessage = $"Scene load failed: {sceneLoadStatus}.";
    }

    private void OnLoadEventCompleted(
        string sceneName,
        LoadSceneMode loadSceneMode,
        List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut)
    {
        if (networkManager == null
            || !networkManager.IsServer
            || !string.Equals(sceneName, gameplaySceneName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (clientsCompleted == null)
            return;

        EnsureEnemyCoordinator();

        for (int index = 0; index < clientsCompleted.Count; index++)
            SpawnPlayerForClient(clientsCompleted[index]);
    }

    private void OnSynchronizeComplete(ulong clientId)
    {
        if (networkManager == null || !networkManager.IsServer || !IsGameplaySceneActive())
            return;

        EnsureEnemyCoordinator();
        SpawnPlayerForClient(clientId);
        MultiplayerPrototypeEnemyCoordinator.FindActive()?.SynchronizeClient(clientId);
    }

    private void SpawnPlayerForClient(ulong clientId)
    {
        if (playerPrefab == null || spawnedPlayerClients.Contains(clientId))
            return;

        if (!networkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient connectedClient))
            return;

        if (connectedClient.PlayerObject != null)
        {
            spawnedPlayerClients.Add(clientId);
            return;
        }

        GameObject playerInstance = Instantiate(playerPrefab);
        playerInstance.name = $"{playerPrefab.name}_{clientId}";
        SceneManager.MoveGameObjectToScene(playerInstance, SceneManager.GetActiveScene());
        ApplySpawnTransform(playerInstance.transform, clientId);

        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Destroy(playerInstance);
            Debug.LogError("Player prototype prefab requires a NetworkObject component.");
            return;
        }

        networkObject.SpawnAsPlayerObject(clientId, true);
        spawnedPlayerClients.Add(clientId);
    }

    private void ApplySpawnTransform(Transform playerTransform, ulong clientId)
    {
        if (playerTransform == null)
            return;

        SceneSpawnPoint spawnPoint = SceneSpawnPoint.FindDefault();
        Vector3 basePosition = spawnPoint != null ? spawnPoint.transform.position : playerTransform.position;
        Quaternion baseRotation = spawnPoint != null ? spawnPoint.transform.rotation : playerTransform.rotation;

        playerTransform.SetPositionAndRotation(
            basePosition + Vector3.right * GetSpawnOffset(clientId),
            baseRotation);
    }

    private float GetSpawnOffset(ulong clientId)
    {
        if (networkManager == null)
            return 0f;

        List<ulong> connectedClientIds = new List<ulong>(networkManager.ConnectedClientsIds);
        connectedClientIds.Sort();

        int index = connectedClientIds.IndexOf(clientId);
        if (index <= 0)
            return 0f;

        int side = index % 2 == 1 ? 1 : -1;
        int row = (index + 1) / 2;
        return side * row * 1.8f;
    }

    private void EnsureEnemyCoordinator()
    {
        if (networkManager == null || !networkManager.IsListening)
            return;

        MultiplayerPrototypeEnemyCoordinator.EnsureInitialized(gameObject, networkManager);
    }

    private bool IsGameplaySceneActive()
    {
        return string.Equals(
            SceneManager.GetActiveScene().name,
            gameplaySceneName,
            StringComparison.OrdinalIgnoreCase);
    }

    private bool ValidateConfiguration()
    {
        if (playerPrefab == null)
        {
            statusMessage = "Assign the generated PlayerNetworkPrototype prefab first.";
            return false;
        }

        if (!playerPrefab.TryGetComponent(out NetworkObject _))
        {
            statusMessage = "Player prefab is missing a NetworkObject component.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            statusMessage = "Set a gameplay scene name before starting.";
            return false;
        }

        return true;
    }

    private bool TryApplyPortField()
    {
        if (ushort.TryParse(portText, out ushort parsedPort) && parsedPort > 0)
        {
            hostPort = parsedPort;
            return true;
        }

        statusMessage = "Enter a valid port number.";
        return false;
    }

    private void OnGUI()
    {
        if (!showRuntimeGui)
            return;

        GUILayout.BeginArea(new Rect(16f, 16f, 360f, 300f), GUI.skin.window);
        GUILayout.Label("Multiplayer Prototype");
        GUILayout.Label($"Scene: {gameplaySceneName}");
        GUILayout.Label($"Status: {statusMessage}");

        if (networkManager == null || !networkManager.IsListening)
        {
            DrawCharacterSelectionGui();
            GUILayout.Label("Address");
            hostAddress = GUILayout.TextField(hostAddress ?? string.Empty);
            GUILayout.Label("Port");
            portText = GUILayout.TextField(portText ?? hostPort.ToString());

            if (GUILayout.Button("Start Host"))
                StartHost();

            if (GUILayout.Button("Start Client"))
                StartClient();
        }
        else
        {
            string mode = networkManager.IsHost
                ? "Host"
                : networkManager.IsServer ? "Server" : "Client";

            GUILayout.Label($"Mode: {mode}");
            GUILayout.Label($"Active Scene: {SceneManager.GetActiveScene().name}");

            if (GUILayout.Button("Shutdown Session"))
                ShutdownSession();
        }

        GUILayout.EndArea();
    }

    private void DrawCharacterSelectionGui()
    {
        PlayerSessionCharacterApplicationService characterSession = sessionBootstrap != null ? sessionBootstrap.CharacterSession : null;
        if (characterSession == null)
        {
            GUILayout.Label("Character: session not ready.");
            return;
        }

        CharacterSaveData activeCharacter = characterSession.ActiveCharacter;
        if (activeCharacter == null)
        {
            GUILayout.Label("Character: no active character.");
            return;
        }

        string jobName = activeCharacter.RuntimeData != null
            ? GameBootstrap.FormatJobName(activeCharacter.RuntimeData.CurrentJob)
            : "Unknown";

        GUILayout.Label($"Character: {activeCharacter.Nickname}");
        GUILayout.Label($"Job: {jobName}");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Prev Character"))
            CyclePrototypeCharacter(-1);

        if (GUILayout.Button("Next Character"))
            CyclePrototypeCharacter(1);
        GUILayout.EndHorizontal();
    }

    private void CyclePrototypeCharacter(int direction)
    {
        PlayerSessionCharacterApplicationService characterSession = sessionBootstrap != null ? sessionBootstrap.CharacterSession : null;
        if (characterSession == null)
        {
            statusMessage = "Character session is not ready.";
            return;
        }

        List<CharacterSaveData> availableCharacters = new List<CharacterSaveData>();
        IReadOnlyList<CharacterSlotData> slots = characterSession.GetCharacterSlots();

        for (int index = 0; index < slots.Count; index++)
        {
            CharacterSlotData slot = slots[index];
            if (slot == null)
                continue;

            CharacterSaveData character = characterSession.GetCharacterAtSlot(slot.SlotIndex);
            if (character != null)
                availableCharacters.Add(character);
        }

        if (availableCharacters.Count == 0)
        {
            statusMessage = "No saved characters are available.";
            return;
        }

        int currentIndex = -1;
        for (int index = 0; index < availableCharacters.Count; index++)
        {
            CharacterSaveData candidate = availableCharacters[index];
            if (candidate == null || characterSession.ActiveCharacter == null)
                continue;

            if (string.Equals(candidate.CharacterId, characterSession.ActiveCharacter.CharacterId, StringComparison.Ordinal))
            {
                currentIndex = index;
                break;
            }
        }

        if (currentIndex < 0)
            currentIndex = 0;

        int nextIndex = (currentIndex + direction + availableCharacters.Count) % availableCharacters.Count;
        CharacterSaveData nextCharacter = availableCharacters[nextIndex];
        if (nextCharacter == null || string.IsNullOrWhiteSpace(nextCharacter.CharacterId))
        {
            statusMessage = "That character is not valid for this prototype.";
            return;
        }

        if (characterSession.TrySelectCharacter(nextCharacter.CharacterId))
            statusMessage = $"Selected {nextCharacter.Nickname} for this prototype session.";
        else
            statusMessage = $"Could not select {nextCharacter.Nickname}.";
    }
}

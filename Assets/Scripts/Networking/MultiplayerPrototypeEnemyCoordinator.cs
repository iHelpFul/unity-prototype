using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public struct MultiplayerPrototypeEnemyState : IEquatable<MultiplayerPrototypeEnemyState>
{
    public ulong EnemyId;
    public Vector3 Position;
    public float Yaw;
    public int CurrentHp;
    public bool IsDead;
    public bool IsVisible;

    public bool Equals(MultiplayerPrototypeEnemyState other)
    {
        return EnemyId == other.EnemyId
            && Position.Equals(other.Position)
            && Yaw.Equals(other.Yaw)
            && CurrentHp == other.CurrentHp
            && IsDead == other.IsDead
            && IsVisible == other.IsVisible;
    }

    public override bool Equals(object obj)
    {
        return obj is MultiplayerPrototypeEnemyState other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(EnemyId, Position, Yaw, CurrentHp, IsDead, IsVisible);
    }
}

[DisallowMultipleComponent]
public class MultiplayerPrototypeEnemyCoordinator : MonoBehaviour
{
    private const string DamageRequestMessageName = "PrototypeEnemyDamageRequest";
    private const string DamageResultMessageName = "PrototypeEnemyDamageResult";
    private const string SnapshotMessageName = "PrototypeEnemySnapshot";

    private static MultiplayerPrototypeEnemyCoordinator instance;

    [SerializeField] private float periodicSnapshotInterval = 0.08f;

    private NetworkManager networkManager;
    private bool handlersRegistered;
    private float nextPeriodicSnapshotTime;
    private int lastAuthoritySceneHandle = -1;

    public static MultiplayerPrototypeEnemyCoordinator Instance => instance;

    public static MultiplayerPrototypeEnemyCoordinator FindActive()
    {
        if (instance != null)
            return instance;

        instance = UnityEngine.Object.FindAnyObjectByType<MultiplayerPrototypeEnemyCoordinator>(FindObjectsInactive.Include);
        return instance;
    }

    public static MultiplayerPrototypeEnemyCoordinator EnsureInitialized(
        GameObject owner,
        NetworkManager activeNetworkManager)
    {
        if (!MultiplayerPrototypeRuntime.IsEnabled || owner == null || activeNetworkManager == null)
            return FindActive();

        MultiplayerPrototypeEnemyCoordinator coordinator = FindActive();
        if (coordinator == null)
            coordinator = owner.AddComponent<MultiplayerPrototypeEnemyCoordinator>();

        coordinator.Bind(activeNetworkManager);
        return coordinator;
    }

    public static bool TryRequestDamage(
        EnemyHealth enemy,
        int damage,
        float direction,
        PlayerCharacter attacker,
        bool commitDeath)
    {
        if (!MultiplayerPrototypeRuntime.IsEnabled || enemy == null)
            return false;

        MultiplayerPrototypeEnemyCoordinator coordinator = FindActive();
        if (coordinator == null || coordinator.networkManager == null || !coordinator.networkManager.IsListening)
            return false;

        return coordinator.RequestDamageInternal(
            enemy,
            Mathf.Max(1, damage),
            direction,
            attacker,
            commitDeath);
    }

    public static void NotifyEnemyRespawned(EnemyHealth enemy)
    {
        if (!MultiplayerPrototypeRuntime.IsEnabled || enemy == null)
            return;

        MultiplayerPrototypeEnemyCoordinator coordinator = FindActive();
        if (coordinator == null || coordinator.networkManager == null || !coordinator.networkManager.IsServer)
            return;

        coordinator.BroadcastEnemySnapshot(enemy);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

        Unbind();
    }

    private void Update()
    {
        if (networkManager == null || !networkManager.IsListening)
            return;

        ApplySceneAuthorityMode();

        if (!networkManager.IsServer)
            return;

        if (Time.unscaledTime < nextPeriodicSnapshotTime)
            return;

        IReadOnlyList<ulong> remoteClientIds = GetRemoteClientIds();
        if (remoteClientIds.Count == 0)
            return;

        MultiplayerPrototypeEnemyState[] states = CaptureActiveSceneStates();
        if (states.Length == 0)
            return;

        SendSnapshotMessage(remoteClientIds, states);
        nextPeriodicSnapshotTime = Time.unscaledTime + Mathf.Max(0.03f, periodicSnapshotInterval);
    }

    public void Bind(NetworkManager activeNetworkManager)
    {
        if (activeNetworkManager == null)
            return;

        if (networkManager == activeNetworkManager && handlersRegistered)
            return;

        if (networkManager != activeNetworkManager)
            Unbind();

        networkManager = activeNetworkManager;
        RegisterHandlers();
        MultiplayerPrototypeSceneEnemyRegistry.Invalidate();
        lastAuthoritySceneHandle = -1;
        nextPeriodicSnapshotTime = 0f;
    }

    public void Unbind()
    {
        UnregisterHandlers();
        networkManager = null;
        lastAuthoritySceneHandle = -1;
    }

    public void SynchronizeClient(ulong clientId)
    {
        if (networkManager == null || !networkManager.IsServer || clientId == networkManager.LocalClientId)
            return;

        MultiplayerPrototypeEnemyState[] states = CaptureActiveSceneStates();
        if (states.Length == 0)
            return;

        SendSnapshotMessage(clientId, states);
    }

    private void RegisterHandlers()
    {
        if (handlersRegistered || networkManager == null || networkManager.CustomMessagingManager == null)
            return;

        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(
            DamageRequestMessageName,
            OnDamageRequestMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(
            DamageResultMessageName,
            OnDamageResultMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(
            SnapshotMessageName,
            OnSnapshotMessage);

        handlersRegistered = true;
    }

    private void UnregisterHandlers()
    {
        if (!handlersRegistered || networkManager == null || networkManager.CustomMessagingManager == null)
            return;

        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(DamageRequestMessageName);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(DamageResultMessageName);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(SnapshotMessageName);
        handlersRegistered = false;
    }

    private bool RequestDamageInternal(
        EnemyHealth enemy,
        int damage,
        float direction,
        PlayerCharacter attacker,
        bool commitDeath)
    {
        if (!MultiplayerPrototypeSceneEnemyRegistry.TryGetEnemyId(enemy, out ulong enemyId))
            return false;

        if (networkManager.IsServer)
        {
            ProcessDamage(enemyId, damage, direction, commitDeath, ResolveAttackerClientId(attacker));
            return true;
        }

        SendDamageRequest(enemyId, damage, direction, commitDeath);
        return true;
    }

    private void OnDamageRequestMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (networkManager == null || !networkManager.IsServer)
            return;

        reader.ReadValueSafe(out ulong enemyId);
        reader.ReadValueSafe(out int damage);
        reader.ReadValueSafe(out float direction);
        reader.ReadValueSafe(out bool commitDeath);

        ProcessDamage(enemyId, Mathf.Max(1, damage), direction, commitDeath, senderClientId);
    }

    private void OnDamageResultMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (networkManager == null || networkManager.IsServer)
            return;

        MultiplayerPrototypeEnemyState state = ReadEnemyState(ref reader);
        reader.ReadValueSafe(out int appliedDamage);
        reader.ReadValueSafe(out float direction);
        reader.ReadValueSafe(out ulong killerClientId);

        if (!MultiplayerPrototypeSceneEnemyRegistry.TryResolveEnemy(state.EnemyId, out EnemyHealth enemy))
            return;

        bool becameDead = enemy.ApplyAuthoritativeState(
            state.CurrentHp,
            state.IsDead,
            state.IsVisible,
            state.Position,
            Quaternion.Euler(0f, state.Yaw, 0f),
            appliedDamage,
            direction,
            playDamageFeedback: appliedDamage > 0);

        if (becameDead && killerClientId == networkManager.LocalClientId)
            PublishLocalKillCredit(enemy);
    }

    private void OnSnapshotMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (networkManager == null || networkManager.IsServer)
            return;

        reader.ReadValueSafe(out int stateCount);
        for (int index = 0; index < stateCount; index++)
        {
            MultiplayerPrototypeEnemyState state = ReadEnemyState(ref reader);
            if (!MultiplayerPrototypeSceneEnemyRegistry.TryResolveEnemy(state.EnemyId, out EnemyHealth enemy))
                continue;

            enemy.ApplyAuthoritativeState(
                state.CurrentHp,
                state.IsDead,
                state.IsVisible,
                state.Position,
                Quaternion.Euler(0f, state.Yaw, 0f),
                0,
                0f,
                playDamageFeedback: false);
        }
    }

    private void ProcessDamage(
        ulong enemyId,
        int damage,
        float direction,
        bool commitDeath,
        ulong attackerClientId)
    {
        if (networkManager == null || !networkManager.IsServer)
            return;

        if (!MultiplayerPrototypeSceneEnemyRegistry.TryResolveEnemy(enemyId, out EnemyHealth enemy))
            return;

        if (enemy == null || enemy.IsDead)
            return;

        int previousHp = enemy.CurrentHP;
        PlayerCharacter attacker = ResolveAttacker(attackerClientId);
        enemy.TakeDamage(damage, direction, attacker, commitDeath);

        int appliedDamage = Mathf.Max(0, previousHp - enemy.CurrentHP);
        BroadcastDamageResult(enemy, enemyId, appliedDamage, direction, attackerClientId);
    }

    private void BroadcastDamageResult(
        EnemyHealth enemy,
        ulong enemyId,
        int appliedDamage,
        float direction,
        ulong killerClientId)
    {
        if (enemy == null || networkManager == null)
            return;

        IReadOnlyList<ulong> remoteClientIds = GetRemoteClientIds();
        if (remoteClientIds.Count == 0)
            return;

        using (FastBufferWriter writer = new FastBufferWriter(128, Allocator.Temp))
        {
            WriteEnemyState(writer, BuildState(enemy, enemyId));
            writer.WriteValueSafe(appliedDamage);
            writer.WriteValueSafe(direction);
            writer.WriteValueSafe(killerClientId);

            networkManager.CustomMessagingManager.SendNamedMessage(
                DamageResultMessageName,
                remoteClientIds,
                writer);
        }
    }

    private void BroadcastEnemySnapshot(EnemyHealth enemy)
    {
        if (enemy == null || !MultiplayerPrototypeSceneEnemyRegistry.TryGetEnemyId(enemy, out ulong enemyId))
            return;

        IReadOnlyList<ulong> remoteClientIds = GetRemoteClientIds();
        if (remoteClientIds.Count == 0)
            return;

        MultiplayerPrototypeEnemyState[] states = { BuildState(enemy, enemyId) };
        SendSnapshotMessage(remoteClientIds, states);
    }

    private void SendDamageRequest(ulong enemyId, int damage, float direction, bool commitDeath)
    {
        if (networkManager == null || networkManager.IsServer)
            return;

        using (FastBufferWriter writer = new FastBufferWriter(32, Allocator.Temp))
        {
            writer.WriteValueSafe(enemyId);
            writer.WriteValueSafe(damage);
            writer.WriteValueSafe(direction);
            writer.WriteValueSafe(commitDeath);
            networkManager.CustomMessagingManager.SendNamedMessage(
                DamageRequestMessageName,
                NetworkManager.ServerClientId,
                writer);
        }
    }

    private void SendSnapshotMessage(ulong clientId, MultiplayerPrototypeEnemyState[] states)
    {
        if (states == null || states.Length == 0)
            return;

        using (FastBufferWriter writer = CreateSnapshotWriter(states))
        {
            networkManager.CustomMessagingManager.SendNamedMessage(
                SnapshotMessageName,
                clientId,
                writer);
        }
    }

    private void SendSnapshotMessage(IReadOnlyList<ulong> clientIds, MultiplayerPrototypeEnemyState[] states)
    {
        if (clientIds == null || clientIds.Count == 0 || states == null || states.Length == 0)
            return;

        using (FastBufferWriter writer = CreateSnapshotWriter(states))
        {
            networkManager.CustomMessagingManager.SendNamedMessage(
                SnapshotMessageName,
                clientIds,
                writer);
        }
    }

    private void ApplySceneAuthorityMode()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
            return;

        if (lastAuthoritySceneHandle != activeScene.handle)
        {
            MultiplayerPrototypeSceneEnemyRegistry.Invalidate();
            lastAuthoritySceneHandle = activeScene.handle;
        }

        bool isAuthoritativeSimulation = networkManager != null && networkManager.IsServer;
        IReadOnlyList<EnemyHealth> enemies = MultiplayerPrototypeSceneEnemyRegistry.GetSceneEnemies(activeScene);
        for (int index = 0; index < enemies.Count; index++)
        {
            EnemyHealth enemy = enemies[index];
            if (enemy != null)
                enemy.SetPrototypeAuthorityMode(isAuthoritativeSimulation);
        }
    }

    private FastBufferWriter CreateSnapshotWriter(MultiplayerPrototypeEnemyState[] states)
    {
        FastBufferWriter writer = new FastBufferWriter(
            Mathf.Max(32, 8 + states.Length * 40),
            Allocator.Temp);

        writer.WriteValueSafe(states.Length);
        for (int index = 0; index < states.Length; index++)
            WriteEnemyState(writer, states[index]);

        return writer;
    }

    private MultiplayerPrototypeEnemyState[] CaptureActiveSceneStates()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        IReadOnlyList<EnemyHealth> enemies = MultiplayerPrototypeSceneEnemyRegistry.GetSceneEnemies(activeScene);
        if (enemies.Count == 0)
            return Array.Empty<MultiplayerPrototypeEnemyState>();

        MultiplayerPrototypeEnemyState[] states = new MultiplayerPrototypeEnemyState[enemies.Count];
        for (int index = 0; index < enemies.Count; index++)
        {
            EnemyHealth enemy = enemies[index];
            MultiplayerPrototypeSceneEnemyRegistry.TryGetEnemyId(enemy, out ulong enemyId);
            states[index] = BuildState(enemy, enemyId);
        }

        return states;
    }

    private MultiplayerPrototypeEnemyState BuildState(EnemyHealth enemy, ulong enemyId)
    {
        return new MultiplayerPrototypeEnemyState
        {
            EnemyId = enemyId,
            Position = enemy.transform.position,
            Yaw = enemy.transform.eulerAngles.y,
            CurrentHp = enemy.CurrentHP,
            IsDead = enemy.IsDead,
            IsVisible = enemy.IsVisible
        };
    }

    private PlayerCharacter ResolveAttacker(ulong clientId)
    {
        if (networkManager == null
            || !networkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient networkClient)
            || networkClient.PlayerObject == null)
        {
            return null;
        }

        return networkClient.PlayerObject.GetComponent<PlayerCharacter>();
    }

    private ulong ResolveAttackerClientId(PlayerCharacter attacker)
    {
        if (attacker != null
            && attacker.TryGetComponent(out NetworkObject attackerNetworkObject)
            && attackerNetworkObject.IsSpawned)
        {
            return attackerNetworkObject.OwnerClientId;
        }

        return networkManager != null ? networkManager.LocalClientId : 0UL;
    }

    private List<ulong> GetRemoteClientIds()
    {
        List<ulong> remoteClientIds = new List<ulong>();
        if (networkManager == null)
            return remoteClientIds;

        IReadOnlyList<ulong> connectedClients = networkManager.ConnectedClientsIds;
        for (int index = 0; index < connectedClients.Count; index++)
        {
            ulong clientId = connectedClients[index];
            if (clientId == networkManager.LocalClientId)
                continue;

            remoteClientIds.Add(clientId);
        }

        return remoteClientIds;
    }

    private void PublishLocalKillCredit(EnemyHealth enemy)
    {
        if (enemy == null || enemy.Stats == null)
            return;

        PlayerCharacter localPlayer = WorldRuntimeSceneUtility.FindLocalPlayer(FindObjectsInactive.Include);
        if (localPlayer == null)
            return;

        EventBus.Publish(new EnemyDiedEvent
        {
            Enemy = enemy.transform,
            Killer = localPlayer,
            Type = enemy.Stats.EnemyType,
            ExpReward = enemy.Stats.ExpReward
        });

        EventBus.Publish(new GameplayNotificationEvent
        {
            Target = localPlayer,
            CharacterId = localPlayer.CharacterId,
            Category = GameplayNotificationCategory.Kill,
            Message = $"Killed {GetEnemyDisplayName(enemy.Stats.EnemyType)} +{enemy.Stats.ExpReward} EXP"
        });
    }

    private static void WriteEnemyState(FastBufferWriter writer, MultiplayerPrototypeEnemyState state)
    {
        writer.WriteValueSafe(state.EnemyId);
        writer.WriteValueSafe(state.Position);
        writer.WriteValueSafe(state.Yaw);
        writer.WriteValueSafe(state.CurrentHp);
        writer.WriteValueSafe(state.IsDead);
        writer.WriteValueSafe(state.IsVisible);
    }

    private static MultiplayerPrototypeEnemyState ReadEnemyState(ref FastBufferReader reader)
    {
        MultiplayerPrototypeEnemyState state = default;
        reader.ReadValueSafe(out state.EnemyId);
        reader.ReadValueSafe(out state.Position);
        reader.ReadValueSafe(out state.Yaw);
        reader.ReadValueSafe(out state.CurrentHp);
        reader.ReadValueSafe(out state.IsDead);
        reader.ReadValueSafe(out state.IsVisible);
        return state;
    }

    private static string GetEnemyDisplayName(EnemyType enemyType)
    {
        string rawName = enemyType.ToString();
        if (string.IsNullOrEmpty(rawName))
            return "Monster";

        StringBuilder builder = new StringBuilder(rawName.Length + 4);
        for (int index = 0; index < rawName.Length; index++)
        {
            char character = rawName[index];
            if (index > 0 && char.IsUpper(character) && !char.IsWhiteSpace(rawName[index - 1]))
                builder.Append(' ');

            builder.Append(character);
        }

        return builder.ToString();
    }
}

internal static class MultiplayerPrototypeSceneEnemyRegistry
{
    private sealed class EnemySortEntry
    {
        public EnemyHealth Enemy;
        public string SortKey;
    }

    private static readonly Dictionary<ulong, EnemyHealth> enemiesById = new Dictionary<ulong, EnemyHealth>();
    private static readonly List<EnemyHealth> orderedEnemies = new List<EnemyHealth>();
    private static readonly Dictionary<EnemyHealth, ulong> enemyIds = new Dictionary<EnemyHealth, ulong>();

    private static int cachedSceneHandle = -1;

    public static void Invalidate()
    {
        cachedSceneHandle = -1;
        enemiesById.Clear();
        orderedEnemies.Clear();
    }

    public static bool TryGetEnemyId(EnemyHealth enemy, out ulong enemyId)
    {
        if (enemy == null)
        {
            enemyId = 0UL;
            return false;
        }

        EnsureCache(enemy.gameObject.scene);
        return enemyIds.TryGetValue(enemy, out enemyId) && enemyId != 0UL;
    }

    public static bool TryResolveEnemy(ulong enemyId, out EnemyHealth enemy)
    {
        EnsureCache(SceneManager.GetActiveScene());
        if (enemiesById.TryGetValue(enemyId, out enemy) && enemy != null)
            return true;

        enemy = null;
        return false;
    }

    public static IReadOnlyList<EnemyHealth> GetSceneEnemies(Scene scene)
    {
        EnsureCache(scene);
        return orderedEnemies;
    }

    private static void EnsureCache(Scene scene)
    {
        if (!scene.IsValid())
        {
            Invalidate();
            return;
        }

        if (cachedSceneHandle == scene.handle)
            return;

        enemiesById.Clear();
        orderedEnemies.Clear();
        enemyIds.Clear();

        EnemyHealth[] sceneEnemies = UnityEngine.Object.FindObjectsByType<EnemyHealth>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        List<EnemySortEntry> sortEntries = new List<EnemySortEntry>();

        for (int index = 0; index < sceneEnemies.Length; index++)
        {
            EnemyHealth enemy = sceneEnemies[index];
            if (enemy == null || enemy.gameObject.scene.handle != scene.handle)
                continue;

            sortEntries.Add(new EnemySortEntry
            {
                Enemy = enemy,
                SortKey = BuildStableSortKey(enemy)
            });
        }

        sortEntries.Sort((left, right) => string.CompareOrdinal(left.SortKey, right.SortKey));

        for (int index = 0; index < sortEntries.Count; index++)
        {
            EnemyHealth enemy = sortEntries[index].Enemy;
            ulong enemyId = (ulong)(index + 1);
            enemiesById[enemyId] = enemy;
            orderedEnemies.Add(enemy);
            enemyIds[enemy] = enemyId;
        }

        cachedSceneHandle = scene.handle;
    }

    private static string BuildStableSortKey(EnemyHealth enemy)
    {
        if (enemy == null)
            return string.Empty;

        Transform target = enemy.transform;
        Vector3 anchorPosition = enemy.HasSpawnIdentity ? enemy.SpawnPosition : target.position;

        StringBuilder builder = new StringBuilder(192);
        builder.Append(target.gameObject.scene.path);
        AppendHierarchySegment(builder, target);
        builder.Append('|');
        builder.Append(enemy.Stats != null ? enemy.Stats.EnemyType.ToString() : "Unknown");
        builder.Append('|');
        AppendRounded(builder, anchorPosition.x);
        builder.Append('|');
        AppendRounded(builder, anchorPosition.y);
        builder.Append('|');
        AppendRounded(builder, anchorPosition.z);
        return builder.ToString();
    }

    private static void AppendHierarchySegment(StringBuilder builder, Transform target)
    {
        if (target.parent != null)
            AppendHierarchySegment(builder, target.parent);

        builder.Append('/');
        builder.Append(target.name);
        builder.Append('#');
        builder.Append(target.GetSiblingIndex());
    }

    private static void AppendRounded(StringBuilder builder, float value)
    {
        builder.Append(Mathf.RoundToInt(value * 100f));
    }
}

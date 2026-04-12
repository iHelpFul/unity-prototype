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

[Serializable]
public struct MultiplayerPrototypeEnemyDamageResult
{
    public MultiplayerPrototypeEnemyState State;
    public int DisplayDamage;
    public int AppliedDamage;
    public float Direction;
    public ulong KillerClientId;
    public uint ActionId;
    public int HitIndex;
    public int TotalHits;
    public bool PlayImpactFeedback;
}

[DisallowMultipleComponent]
public class MultiplayerPrototypeEnemyCoordinator : MonoBehaviour
{
    private const string DamageRequestMessageName = "PrototypeEnemyDamageRequest";
    private const string SkillSequenceRequestMessageName = "PrototypeEnemySkillSequenceRequest";
    private const string DamageResultMessageName = "PrototypeEnemyDamageResult";
    private const string SnapshotMessageName = "PrototypeEnemySnapshot";

    private static MultiplayerPrototypeEnemyCoordinator instance;

    [SerializeField] private float periodicSnapshotInterval = 0.08f;

    private NetworkManager networkManager;
    private bool handlersRegistered;
    private float nextPeriodicSnapshotTime;
    private int lastAuthoritySceneHandle = -1;
    private uint nextSkillSequenceActionId = 1U;

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
        float direction,
        PlayerCharacter attacker,
        bool commitDeath,
        string skillId)
    {
        if (!MultiplayerPrototypeRuntime.IsEnabled || enemy == null)
            return false;

        MultiplayerPrototypeEnemyCoordinator coordinator = FindActive();
        if (coordinator == null || coordinator.networkManager == null || !coordinator.networkManager.IsListening)
            return false;

        return coordinator.RequestDamageInternal(
            enemy,
            direction,
            attacker,
            commitDeath,
            skillId);
    }

    public static bool TryRequestSkillSequence(
        EnemyHealth enemy,
        float direction,
        PlayerCharacter attacker,
        string skillId)
    {
        if (!MultiplayerPrototypeRuntime.IsEnabled || enemy == null)
            return false;

        MultiplayerPrototypeEnemyCoordinator coordinator = FindActive();
        if (coordinator == null || coordinator.networkManager == null || !coordinator.networkManager.IsListening)
            return false;

        return coordinator.RequestSkillSequenceInternal(
            enemy,
            direction,
            attacker,
            skillId);
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
            SkillSequenceRequestMessageName,
            OnSkillSequenceRequestMessage);
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
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(SkillSequenceRequestMessageName);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(DamageResultMessageName);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(SnapshotMessageName);
        handlersRegistered = false;
    }

    private bool RequestDamageInternal(
        EnemyHealth enemy,
        float direction,
        PlayerCharacter attacker,
        bool commitDeath,
        string skillId)
    {
        if (!MultiplayerPrototypeSceneEnemyRegistry.TryGetEnemyId(enemy, out ulong enemyId))
            return true;

        if (networkManager.IsServer)
        {
            ProcessDamage(
                enemyId,
                direction,
                commitDeath,
                ResolveAttackerClientId(attacker),
                skillId);
            return true;
        }

        SendDamageRequest(enemyId, direction, commitDeath, skillId);
        return true;
    }

    private bool RequestSkillSequenceInternal(
        EnemyHealth enemy,
        float direction,
        PlayerCharacter attacker,
        string skillId)
    {
        if (!MultiplayerPrototypeSceneEnemyRegistry.TryGetEnemyId(enemy, out ulong enemyId))
            return true;

        if (networkManager.IsServer)
        {
            StartAuthoritativeSkillSequence(
                enemyId,
                direction,
                ResolveAttackerClientId(attacker),
                skillId);
            return true;
        }

        SendSkillSequenceRequest(enemyId, direction, skillId);
        return true;
    }

    private void OnDamageRequestMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (networkManager == null || !networkManager.IsServer)
            return;

        reader.ReadValueSafe(out ulong enemyId);
        reader.ReadValueSafe(out float direction);
        reader.ReadValueSafe(out bool commitDeath);
        reader.ReadValueSafe(out string skillId);

        ProcessDamage(
            enemyId,
            direction,
            commitDeath,
            senderClientId,
            skillId);
    }

    private void OnSkillSequenceRequestMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (networkManager == null || !networkManager.IsServer)
            return;

        reader.ReadValueSafe(out ulong enemyId);
        reader.ReadValueSafe(out float direction);
        reader.ReadValueSafe(out string skillId);

        StartAuthoritativeSkillSequence(
            enemyId,
            direction,
            senderClientId,
            skillId);
    }

    private void OnDamageResultMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (networkManager == null || networkManager.IsServer)
            return;

        MultiplayerPrototypeEnemyDamageResult result = ReadDamageResult(ref reader);

        if (!MultiplayerPrototypeSceneEnemyRegistry.TryResolveEnemy(result.State.EnemyId, out EnemyHealth enemy))
            return;

        bool becameDead = enemy.ApplyAuthoritativeState(
            result.State.CurrentHp,
            result.State.IsDead,
            result.State.IsVisible,
            result.State.Position,
            Quaternion.Euler(0f, result.State.Yaw, 0f),
            result.DisplayDamage,
            result.Direction,
            playDamageNumber: result.DisplayDamage > 0,
            playImpactFeedback: result.PlayImpactFeedback);

        if (becameDead && result.KillerClientId == networkManager.LocalClientId)
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
                playDamageNumber: false,
                playImpactFeedback: false);
        }
    }

    private bool ProcessDamage(
        ulong enemyId,
        float direction,
        bool commitDeath,
        ulong attackerClientId,
        string skillId,
        uint actionId = 0U,
        int hitIndex = 0,
        int totalHits = 1,
        bool allowDeadTargetSequenceResult = false,
        bool playImpactFeedback = true)
    {
        if (networkManager == null || !networkManager.IsServer)
            return false;

        if (!MultiplayerPrototypeSceneEnemyRegistry.TryResolveEnemy(enemyId, out EnemyHealth enemy))
            return false;

        if (enemy == null)
            return false;

        bool targetAlreadyDead = enemy.IsDead;
        if (targetAlreadyDead && !allowDeadTargetSequenceResult)
            return false;

        if (enemy.Stats == null)
        {
            Debug.LogWarning(
                $"[MultiplayerPrototypeEnemyCoordinator] Rejected damage intent for enemy 0x{enemyId:X8} from client {attackerClientId}: enemy stats are missing.");
            return false;
        }

        int previousHp = enemy.CurrentHP;
        PlayerCharacter attacker = ResolveAttacker(attackerClientId);
        if (!TryResolveAuthoritativeDamage(
            attackerClientId,
            skillId,
            out int resolvedDamage,
            out string rejectionReason))
        {
            Debug.LogWarning(
                $"[MultiplayerPrototypeEnemyCoordinator] Rejected damage intent for enemy 0x{enemyId:X8} from client {attackerClientId}: {rejectionReason}");
            return false;
        }

        int displayDamage = ResolveDisplayDamage(enemy, resolvedDamage);
        if (!targetAlreadyDead)
        {
            enemy.TakeDamage(
                resolvedDamage,
                direction,
                attacker,
                commitDeath,
                publishDamageFeedback: false);
        }

        int appliedDamage = targetAlreadyDead
            ? 0
            : Mathf.Max(0, previousHp - enemy.CurrentHP);

        MultiplayerPrototypeEnemyDamageResult result = BuildDamageResult(
            enemy,
            enemyId,
            displayDamage,
            appliedDamage,
            direction,
            attackerClientId,
            actionId,
            hitIndex,
            totalHits,
            playImpactFeedback && appliedDamage > 0);

        BroadcastDamageResult(result);

        if (networkManager.IsClient && result.DisplayDamage > 0)
            enemy.PlayAuthoritativeDamageFeedback(result.DisplayDamage, direction, result.PlayImpactFeedback);

        return true;
    }

    private void BroadcastDamageResult(MultiplayerPrototypeEnemyDamageResult result)
    {
        if (networkManager == null)
            return;

        IReadOnlyList<ulong> remoteClientIds = GetRemoteClientIds();
        if (remoteClientIds.Count == 0)
            return;

        using (FastBufferWriter writer = new FastBufferWriter(160, Allocator.Temp))
        {
            WriteDamageResult(writer, result);

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

    private void SendDamageRequest(
        ulong enemyId,
        float direction,
        bool commitDeath,
        string skillId)
    {
        if (networkManager == null || networkManager.IsServer)
            return;

        string normalizedSkillId = string.IsNullOrWhiteSpace(skillId) ? string.Empty : skillId;
        int requiredSize =
            FastBufferWriter.GetWriteSize(enemyId)
            + FastBufferWriter.GetWriteSize(direction)
            + FastBufferWriter.GetWriteSize(commitDeath)
            + FastBufferWriter.GetWriteSize(normalizedSkillId);

        using (FastBufferWriter writer = new FastBufferWriter(Mathf.Max(32, requiredSize), Allocator.Temp))
        {
            writer.WriteValueSafe(enemyId);
            writer.WriteValueSafe(direction);
            writer.WriteValueSafe(commitDeath);
            writer.WriteValueSafe(normalizedSkillId);
            networkManager.CustomMessagingManager.SendNamedMessage(
                DamageRequestMessageName,
                NetworkManager.ServerClientId,
                writer);
        }
    }

    private void SendSkillSequenceRequest(
        ulong enemyId,
        float direction,
        string skillId)
    {
        if (networkManager == null || networkManager.IsServer)
            return;

        string normalizedSkillId = string.IsNullOrWhiteSpace(skillId) ? string.Empty : skillId;
        int requiredSize =
            FastBufferWriter.GetWriteSize(enemyId)
            + FastBufferWriter.GetWriteSize(direction)
            + FastBufferWriter.GetWriteSize(normalizedSkillId);

        using (FastBufferWriter writer = new FastBufferWriter(Mathf.Max(32, requiredSize), Allocator.Temp))
        {
            writer.WriteValueSafe(enemyId);
            writer.WriteValueSafe(direction);
            writer.WriteValueSafe(normalizedSkillId);
            networkManager.CustomMessagingManager.SendNamedMessage(
                SkillSequenceRequestMessageName,
                NetworkManager.ServerClientId,
                writer);
        }
    }

    private bool TryResolveAuthoritativeDamage(
        ulong attackerClientId,
        string skillId,
        out int resolvedDamage,
        out string rejectionReason)
    {
        resolvedDamage = 0;
        rejectionReason = string.Empty;

        if (!TryResolveAttackerCombatState(attackerClientId, out NetworkPlayerPrototypeCombatState combatState))
        {
            rejectionReason = "missing authoritative combat state.";
            return false;
        }

        PlayerCombatSnapshot snapshot = combatState.ToCombatSnapshot();
        int baseDamage = DamageCalculator.CalculateDamage(
            snapshot.Strength,
            snapshot.Dexterity,
            snapshot.WeaponAttack,
            snapshot.SkillMastery);

        if (!string.IsNullOrWhiteSpace(skillId))
        {
            PlayerSkillDefinition definition = PlayerSkillDatabase.GetDefinition(skillId);
            if (definition == null)
            {
                rejectionReason = $"unknown skill '{skillId}'.";
                return false;
            }

            resolvedDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * Mathf.Max(0.1f, definition.DamageMultiplier)));
            return true;
        }

        PlayerBasicAttackProfile profile = PlayerJobCombatProfiles.GetBasicAttackProfile(combatState.CurrentJob);
        float comboMultiplier = 1f;

        if (profile != null && profile.SupportsComboCounter && combatState.ComboCounter > 0)
            comboMultiplier += combatState.ComboCounter * profile.ComboDamageBonusPerStack;

        float basicMultiplier = profile != null ? profile.BasicDamageMultiplier : 1f;
        resolvedDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * basicMultiplier * comboMultiplier));
        return true;
    }

    private static int ResolveDisplayDamage(EnemyHealth enemy, int resolvedDamage)
    {
        if (enemy == null || enemy.Stats == null)
            return Mathf.Max(1, resolvedDamage);

        return Mathf.Max(1, resolvedDamage - enemy.Stats.Defense);
    }

    private void StartAuthoritativeSkillSequence(
        ulong enemyId,
        float direction,
        ulong attackerClientId,
        string skillId)
    {
        if (networkManager == null || !networkManager.IsServer)
            return;

        PlayerSkillDefinition definition = PlayerSkillDatabase.GetDefinition(skillId);
        if (!IsSupportedAuthoritativeSkillSequence(definition))
        {
            Debug.LogWarning(
                $"[MultiplayerPrototypeEnemyCoordinator] Rejected skill sequence intent for enemy 0x{enemyId:X8} from client {attackerClientId}: unsupported multi-hit skill '{skillId}'.");
            return;
        }

        uint actionId = ResolveNextSkillSequenceActionId();
        StartCoroutine(RunAuthoritativeSkillSequence(
            actionId,
            enemyId,
            direction,
            attackerClientId,
            definition));
    }

    private System.Collections.IEnumerator RunAuthoritativeSkillSequence(
        uint actionId,
        ulong enemyId,
        float direction,
        ulong attackerClientId,
        PlayerSkillDefinition definition)
    {
        int totalHits = Mathf.Max(1, definition.HitCount);
        float hitInterval = Mathf.Max(0.01f, definition.HitInterval);

        for (int hitIndex = 0; hitIndex < totalHits; hitIndex++)
        {
            if (hitIndex > 0)
                yield return new WaitForSeconds(hitInterval);

            if (networkManager == null || !networkManager.IsServer || !networkManager.IsListening)
                yield break;

            bool producedResult = ProcessDamage(
                enemyId,
                direction,
                commitDeath: true,
                attackerClientId: attackerClientId,
                skillId: definition.SkillId,
                actionId: actionId,
                hitIndex: hitIndex,
                totalHits: totalHits,
                allowDeadTargetSequenceResult: hitIndex > 0,
                playImpactFeedback: hitIndex == 0);

            if (!producedResult)
                yield break;
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

    private MultiplayerPrototypeEnemyDamageResult BuildDamageResult(
        EnemyHealth enemy,
        ulong enemyId,
        int displayDamage,
        int appliedDamage,
        float direction,
        ulong killerClientId,
        uint actionId,
        int hitIndex,
        int totalHits,
        bool playImpactFeedback)
    {
        return new MultiplayerPrototypeEnemyDamageResult
        {
            State = BuildState(enemy, enemyId),
            DisplayDamage = displayDamage,
            AppliedDamage = appliedDamage,
            Direction = direction,
            KillerClientId = killerClientId,
            ActionId = actionId,
            HitIndex = hitIndex,
            TotalHits = totalHits,
            PlayImpactFeedback = playImpactFeedback
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

    private bool TryResolveAttackerCombatState(
        ulong clientId,
        out NetworkPlayerPrototypeCombatState combatState)
    {
        combatState = default;

        if (networkManager == null
            || !networkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient networkClient)
            || networkClient.PlayerObject == null)
        {
            return false;
        }

        NetworkPlayerPrototypeAvatar avatar =
            networkClient.PlayerObject.GetComponent<NetworkPlayerPrototypeAvatar>();

        return avatar != null && avatar.TryGetAuthoritativeCombatState(out combatState);
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

    private static void WriteDamageResult(FastBufferWriter writer, MultiplayerPrototypeEnemyDamageResult result)
    {
        WriteEnemyState(writer, result.State);
        writer.WriteValueSafe(result.DisplayDamage);
        writer.WriteValueSafe(result.AppliedDamage);
        writer.WriteValueSafe(result.Direction);
        writer.WriteValueSafe(result.KillerClientId);
        writer.WriteValueSafe(result.ActionId);
        writer.WriteValueSafe(result.HitIndex);
        writer.WriteValueSafe(result.TotalHits);
        writer.WriteValueSafe(result.PlayImpactFeedback);
    }

    private static MultiplayerPrototypeEnemyDamageResult ReadDamageResult(ref FastBufferReader reader)
    {
        MultiplayerPrototypeEnemyDamageResult result = default;
        result.State = ReadEnemyState(ref reader);
        reader.ReadValueSafe(out result.DisplayDamage);
        reader.ReadValueSafe(out result.AppliedDamage);
        reader.ReadValueSafe(out result.Direction);
        reader.ReadValueSafe(out result.KillerClientId);
        reader.ReadValueSafe(out result.ActionId);
        reader.ReadValueSafe(out result.HitIndex);
        reader.ReadValueSafe(out result.TotalHits);
        reader.ReadValueSafe(out result.PlayImpactFeedback);
        return result;
    }

    private uint ResolveNextSkillSequenceActionId()
    {
        uint actionId = nextSkillSequenceActionId;
        nextSkillSequenceActionId = nextSkillSequenceActionId == uint.MaxValue
            ? 1U
            : nextSkillSequenceActionId + 1U;
        return actionId;
    }

    private static bool IsSupportedAuthoritativeSkillSequence(PlayerSkillDefinition definition)
    {
        return definition != null
            && definition.TargetingMode == PlayerSkillTargetingMode.FrontSingleTarget
            && definition.HitCount > 1;
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
    private static readonly Dictionary<ulong, EnemyHealth> enemiesById = new Dictionary<ulong, EnemyHealth>();
    private static readonly List<EnemyHealth> orderedEnemies = new List<EnemyHealth>();
    private static readonly Dictionary<EnemyHealth, ulong> enemyIds = new Dictionary<EnemyHealth, ulong>();

    private static int cachedSceneHandle = -1;

    public static void Invalidate()
    {
        cachedSceneHandle = -1;
        enemiesById.Clear();
        orderedEnemies.Clear();
        enemyIds.Clear();
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

        List<SceneEntityEnemyRegistration> registrations = new List<SceneEntityEnemyRegistration>();
        List<string> validationErrors = new List<string>();
        int totalSceneEnemies = SceneEntityIdValidationUtility.CollectEnemyRegistrations(
            scene,
            registrations,
            validationErrors);

        for (int index = 0; index < registrations.Count; index++)
        {
            SceneEntityEnemyRegistration registration = registrations[index];
            enemiesById[registration.RuntimeId] = registration.Enemy;
            orderedEnemies.Add(registration.Enemy);
            enemyIds[registration.Enemy] = registration.RuntimeId;
        }

        if (validationErrors.Count > 0)
        {
            for (int index = 0; index < validationErrors.Count; index++)
                Debug.LogError(validationErrors[index]);

            Debug.LogError(
                $"[MultiplayerPrototypeEnemyCoordinator] SceneEntityId validation failed for '{scene.name}'. Registered {registrations.Count}/{totalSceneEnemies} enemies. Errors: {validationErrors.Count}. Use Tools > Multiplayer Prototype > Scene Entity IDs > Validate Enemy IDs.");
        }

        cachedSceneHandle = scene.handle;
    }
}

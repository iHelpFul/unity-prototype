using UnityEngine;

public class PlayerCharacter : MonoBehaviour
{
    [Header("Player Context")]
    [SerializeField] private bool isLocalPlayer = true;
    [SerializeField] private string characterId;
    [SerializeField] private GameBootstrap bootstrap;

    [Header("Base Stats")]
    [SerializeField] private PlayerBaseStats baseStats;

    [Header("Combat")]
    [SerializeField] private int baseWeaponPower = 15;
    [SerializeField] private float skillMastery = 0.6f;
    [SerializeField] private float timeToStun;
    [SerializeField] private float timeToInv;
    [SerializeField] private float timeToFlash;

    [Header("Consumables")]
    [SerializeField] private float manualPickupRange = 1.6f;
    [SerializeField] private PlayerInteractionController interactionController;

    [SerializeField] private PlayerRuntimeStateController runtimeStateController;
    [SerializeField] private PlayerActionStateController actionStateController;
    [SerializeField] private PlayerCombatStateController combatStateController;

    private bool? runtimeLocalPlayerOverride;
    private string runtimeCharacterId;

    public bool IsLocalPlayer => runtimeLocalPlayerOverride ?? isLocalPlayer;
    public string CharacterId => ResolveCharacterId();
    public bool IsStunned => runtimeStateController != null && runtimeStateController.IsStunned;
    public bool IsDead => runtimeStateController != null && runtimeStateController.IsDead;
    public PlayerJobType CurrentJob => runtimeStateController != null
        ? runtimeStateController.CurrentJob
        : PlayerJobType.Drifter;
    public int UnspentStatPoints => runtimeStateController != null
        ? runtimeStateController.UnspentStatPoints
        : 0;
    public bool HasPendingJobAdvancement => runtimeStateController != null
        && runtimeStateController.HasPendingJobAdvancement;
    public int CurrentMomentumStacks => combatStateController != null
        ? combatStateController.CurrentMomentumStacks
        : 0;
    public PlayerActionStateType CurrentActionState => actionStateController != null
        ? actionStateController.CurrentActionState
        : PlayerActionStateType.None;
    public bool HasPendingSkillCommit => actionStateController != null && actionStateController.HasPendingSkillCommit;
    public bool ShouldSuppressInterruptingAnimation => actionStateController != null
        && actionStateController.ShouldSuppressInterruptingAnimation;
    public bool ShouldSuppressHitReactionAnimation => actionStateController != null
        && actionStateController.ShouldSuppressHitReactionAnimation;
    public PlayerActionStateController ActionStateController => actionStateController;
    public PlayerCombatStateController CombatStateController => combatStateController;
    public PlayerRuntimeData RuntimeData => runtimeStateController != null ? runtimeStateController.RuntimeData : null;

    public void SetRuntimeLocalPlayer(bool isRuntimeLocalPlayer)
    {
        runtimeLocalPlayerOverride = isRuntimeLocalPlayer;
        runtimeStateController?.RefreshRuntimeOwnershipState();
    }

    public void SetRuntimeCharacterId(string resolvedCharacterId)
    {
        runtimeCharacterId = PlayerRuntimeIdentityUtility.NormalizeCharacterId(resolvedCharacterId);
    }

    public void BindBootstrap(GameBootstrap sessionBootstrap)
    {
        bootstrap = sessionBootstrap;
        runtimeStateController?.BindBootstrap(sessionBootstrap);
        interactionController?.BindBootstrap(sessionBootstrap);
    }

    public void RefreshPrototypeSessionState()
    {
        runtimeStateController?.RefreshSessionBindings();

        if (IsLocalPlayer)
            bootstrap?.CharacterSession?.PublishSessionState(this);
    }

    private void Awake()
    {
        if (runtimeStateController == null)
            runtimeStateController = GetComponent<PlayerRuntimeStateController>();

        if (runtimeStateController == null)
            runtimeStateController = gameObject.AddComponent<PlayerRuntimeStateController>();

        if (actionStateController == null)
            actionStateController = GetComponent<PlayerActionStateController>();

        if (actionStateController == null)
            actionStateController = gameObject.AddComponent<PlayerActionStateController>();

        if (combatStateController == null)
            combatStateController = GetComponent<PlayerCombatStateController>();

        if (combatStateController == null)
            combatStateController = gameObject.AddComponent<PlayerCombatStateController>();

        runtimeStateController.Initialize(
            this,
            baseStats,
            baseWeaponPower,
            skillMastery,
            timeToStun,
            timeToInv,
            timeToFlash);
        actionStateController.Initialize(this);
        combatStateController.Initialize(this);

        if (interactionController == null)
            interactionController = GetComponent<PlayerInteractionController>();

        if (interactionController == null)
            interactionController = gameObject.AddComponent<PlayerInteractionController>();

        interactionController.Initialize(this, manualPickupRange);

        if (bootstrap != null)
        {
            BindBootstrap(bootstrap);
        }
    }

    private void Start()
    {
        if (!IsLocalPlayer)
            return;

        runtimeStateController?.RefreshSessionBindings();
        bootstrap?.CharacterSession?.PublishSessionState(this);
    }

    public PlayerCombatSnapshot GetCombatSnapshot()
    {
        return runtimeStateController != null
            ? runtimeStateController.GetCombatSnapshot()
            : new PlayerCombatSnapshot();
    }

    public PlayerBasicAttackProfile GetBasicAttackProfile()
    {
        return runtimeStateController != null
            ? runtimeStateController.GetBasicAttackProfile()
            : PlayerJobCombatProfiles.GetBasicAttackProfile(PlayerJobType.Drifter);
    }

    public bool TrySpendStatPoints(PlayerProgressionStatType statType, int points)
    {
        return runtimeStateController != null
            && runtimeStateController.TrySpendStatPoints(statType, points);
    }

    public void TakeDamage(int amount)
    {
        runtimeStateController?.TakeDamage(amount);
    }

    public void RestoreHP(int amount)
    {
        runtimeStateController?.RestoreHP(amount);
    }

    public void RestoreMP(int amount)
    {
        runtimeStateController?.RestoreMP(amount);
    }

    public bool HasEnoughMP(int amount)
    {
        return runtimeStateController != null && runtimeStateController.HasEnoughMP(amount);
    }

    public bool TrySpendMP(int amount)
    {
        return runtimeStateController != null && runtimeStateController.TrySpendMP(amount);
    }

    public void GainCombatMomentum(int amount)
    {
        combatStateController?.GainMomentum(amount);
    }

    public void ConsumeCombatMomentum(int amount)
    {
        combatStateController?.ConsumeMomentum(amount);
    }

    public void ResetCombatMomentum()
    {
        combatStateController?.ResetMomentum();
    }

    public void ResetAfterDeath()
    {
        runtimeStateController?.ResetAfterDeath();
    }

    private string ResolveCharacterId()
    {
        if (IsLocalPlayer)
        {
            CharacterSaveData activeCharacter = bootstrap != null ? bootstrap.CharacterSession?.ActiveCharacter : null;
            if (activeCharacter != null)
            {
                characterId = PlayerRuntimeIdentityUtility.NormalizeCharacterId(activeCharacter.CharacterId);
                runtimeCharacterId = characterId;
                return characterId;
            }
        }

        string normalizedRuntimeCharacterId = PlayerRuntimeIdentityUtility.NormalizeCharacterId(runtimeCharacterId);
        if (!string.IsNullOrWhiteSpace(normalizedRuntimeCharacterId))
        {
            characterId = normalizedRuntimeCharacterId;
            return characterId;
        }

        return PlayerRuntimeIdentityUtility.NormalizeCharacterId(characterId);
    }
}


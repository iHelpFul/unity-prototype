using UnityEngine;

public class PlayerInteractionController : MonoBehaviour
{
    [SerializeField] private float manualPickupRange = 1.6f;

    private PlayerCharacter character;
    private PlayerRuntimeStateController runtimeStateController;
    private GameBootstrap bootstrap;

    private void OnEnable()
    {
        EventBus.Subscribe<InteractPressedEvent>(OnInteractPressed);
        EventBus.Subscribe<UseConsumablePressedEvent>(OnUseConsumablePressed);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<InteractPressedEvent>(OnInteractPressed);
        EventBus.Unsubscribe<UseConsumablePressedEvent>(OnUseConsumablePressed);
    }

    public void Initialize(PlayerCharacter ownerCharacter, float configuredManualPickupRange)
    {
        character = ownerCharacter;
        manualPickupRange = Mathf.Max(0.1f, configuredManualPickupRange);
        runtimeStateController = character != null
            ? character.GetComponent<PlayerRuntimeStateController>()
            : null;
    }

    public void BindBootstrap(GameBootstrap sessionBootstrap)
    {
        bootstrap = sessionBootstrap;
    }

    private void OnInteractPressed(InteractPressedEvent e)
    {
        if (character == null
            || !PlayerRuntimeIdentityUtility.MatchesCharacter(
                character,
                character.CharacterId,
                e.Player,
                e.CharacterId)
            || character.IsDead)
        {
            return;
        }

        WorldLootPickup nearestPickup = FindNearestManualPickup();
        nearestPickup?.TryCollect(character);
    }

    private void OnUseConsumablePressed(UseConsumablePressedEvent e)
    {
        if (character == null
            || runtimeStateController == null
            || runtimeStateController.RuntimeData == null
            || bootstrap == null)
        {
            return;
        }

        if (!PlayerRuntimeIdentityUtility.MatchesCharacter(
            character,
            character.CharacterId,
            e.Player,
            e.CharacterId)
            || character.IsDead)
        {
            return;
        }

        PlayerSessionInventoryApplicationService inventorySession = bootstrap.InventorySession;
        if (inventorySession == null)
            return;

        string itemId = ItemDatabase.GetConsumableItemId(e.ConsumableType);
        if (string.IsNullOrEmpty(itemId))
            return;

        ItemDefinition definition = ItemDatabase.GetDefinition(itemId);
        if (definition == null)
            return;

        int restoreHPAmount = definition.RestoreHP;
        int restoreMPAmount = definition.RestoreMP;

        bool canRestoreHP = restoreHPAmount > 0
            && runtimeStateController.RuntimeData.CurrentHP < runtimeStateController.GetEffectiveMaxHP();

        bool canRestoreMP = restoreMPAmount > 0
            && runtimeStateController.RuntimeData.CurrentMP < runtimeStateController.GetEffectiveMaxMP();

        if (!canRestoreHP && !canRestoreMP)
            return;

        if (!inventorySession.TryConsumeConsumable(character, e.ConsumableType))
            return;

        if (canRestoreHP)
            character.RestoreHP(restoreHPAmount);

        if (canRestoreMP)
            character.RestoreMP(restoreMPAmount);
    }

    private WorldLootPickup FindNearestManualPickup()
    {
        WorldLootPickup[] worldPickups = FindObjectsByType<WorldLootPickup>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        WorldLootPickup nearestPickup = null;
        float bestSqrDistance = manualPickupRange * manualPickupRange;

        for (int index = 0; index < worldPickups.Length; index++)
        {
            WorldLootPickup pickup = worldPickups[index];

            if (pickup == null || !pickup.CanBePickedUp(character, manualPickupRange))
                continue;

            float sqrDistance = (pickup.transform.position - character.transform.position).sqrMagnitude;
            if (sqrDistance > bestSqrDistance)
                continue;

            bestSqrDistance = sqrDistance;
            nearestPickup = pickup;
        }

        return nearestPickup;
    }
}

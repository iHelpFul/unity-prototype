using System;
using System.Collections.Generic;

public class PlayerSessionActionBarApplicationService
{
    private readonly PlayerSessionActionBarService actionBarService;
    private readonly PlayerSessionEventPublisher eventPublisher;
    private readonly Func<PlayerRuntimeData> runtimeDataProvider;
    private readonly Action savePlayer;

    public PlayerSessionActionBarApplicationService(
        PlayerSessionActionBarService actionBarService,
        PlayerSessionEventPublisher eventPublisher,
        Func<PlayerRuntimeData> runtimeDataProvider,
        Action savePlayer)
    {
        this.actionBarService = actionBarService;
        this.eventPublisher = eventPublisher;
        this.runtimeDataProvider = runtimeDataProvider;
        this.savePlayer = savePlayer;
    }

    public IReadOnlyList<PlayerActionBarSlotEntry> GetSlots()
    {
        return actionBarService.GetSlots();
    }

    public PlayerActionBarSlotEntry GetSlot(int slotIndex)
    {
        return actionBarService.GetSlot(slotIndex);
    }

    public bool TryAssignActiveSkillToSlot(string skillId, int slotIndex, PlayerCharacter player = null)
    {
        if (!actionBarService.TryAssignActiveSkillToSlot(skillId, slotIndex))
            return false;

        PublishSlotChanged(slotIndex, player);
        savePlayer?.Invoke();
        return true;
    }

    public bool TryAssignConsumableToSlot(PlayerConsumableType consumableType, int slotIndex, PlayerCharacter player = null)
    {
        if (!actionBarService.TryAssignConsumableToSlot(consumableType, slotIndex))
            return false;

        PublishSlotChanged(slotIndex, player);
        savePlayer?.Invoke();
        return true;
    }

    public bool TryAssignSystemActionToSlot(PlayerInputActionId actionId, int slotIndex, PlayerCharacter player = null)
    {
        if (!actionBarService.TryAssignSystemActionToSlot(actionId, slotIndex))
            return false;

        PublishSlotChanged(slotIndex, player);
        savePlayer?.Invoke();
        return true;
    }

    public bool TryClearSlot(int slotIndex, PlayerCharacter player = null)
    {
        if (!actionBarService.TryClearSlot(slotIndex))
            return false;

        PublishSlotChanged(slotIndex, player);
        savePlayer?.Invoke();
        return true;
    }

    public void PublishActionBarState(PlayerCharacter player)
    {
        PlayerRuntimeData runtimeData = runtimeDataProvider?.Invoke();
        eventPublisher?.PublishActionBarState(runtimeData, player);
    }

    private void PublishSlotChanged(int slotIndex, PlayerCharacter player)
    {
        PlayerActionBarSlotEntry slot = actionBarService.GetSlot(slotIndex);
        eventPublisher?.PublishActionBarSlotChanged(player, slot);
    }
}

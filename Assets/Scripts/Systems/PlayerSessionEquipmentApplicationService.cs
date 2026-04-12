using System;
using System.Collections.Generic;

public class PlayerSessionEquipmentApplicationService
{
    private readonly PlayerSessionInventoryService inventoryService;
    private readonly PlayerSessionEquipmentService equipmentService;
    private readonly PlayerSessionEventPublisher eventPublisher;
    private readonly Func<PlayerRuntimeData> getPlayerData;
    private readonly Func<CharacterSaveData> getActiveCharacter;
    private readonly Action clampVitalsToEquipmentBonuses;
    private readonly Func<PlayerCharacter, PlayerCharacter> resolveSupportedScenePlayer;
    private readonly Action savePlayer;

    public PlayerSessionEquipmentApplicationService(
        PlayerSessionInventoryService inventoryService,
        PlayerSessionEquipmentService equipmentService,
        PlayerSessionEventPublisher eventPublisher,
        Func<PlayerRuntimeData> getPlayerData,
        Func<CharacterSaveData> getActiveCharacter,
        Action clampVitalsToEquipmentBonuses,
        Func<PlayerCharacter, PlayerCharacter> resolveSupportedScenePlayer,
        Action savePlayer)
    {
        this.inventoryService = inventoryService;
        this.equipmentService = equipmentService;
        this.eventPublisher = eventPublisher;
        this.getPlayerData = getPlayerData;
        this.getActiveCharacter = getActiveCharacter;
        this.clampVitalsToEquipmentBonuses = clampVitalsToEquipmentBonuses;
        this.resolveSupportedScenePlayer = resolveSupportedScenePlayer;
        this.savePlayer = savePlayer;
    }

    public IReadOnlyList<EquippedItemEntry> GetEquippedItems()
    {
        return equipmentService.EquippedItems;
    }

    public InventoryEntry GetEquippedEntry(EquipmentSlotType slot)
    {
        return equipmentService.GetEquippedEntry(slot);
    }

    public ItemStatModifierData GetEquipmentStatBonuses()
    {
        return equipmentService.GetTotalStatBonuses();
    }

    public CharacterAppearanceData GetResolvedActiveCharacterAppearance()
    {
        CharacterSaveData activeCharacter = ResolveActiveCharacter();
        if (activeCharacter == null)
            return null;

        return equipmentService.BuildResolvedAppearance(activeCharacter.Appearance);
    }

    public string AddEquipmentItem(
        PlayerCharacter player,
        string itemId,
        ItemEquipmentAppearanceData appearanceOverride,
        bool autoEquip)
    {
        PlayerRuntimeData playerData = ResolvePlayerData();
        string entryId = equipmentService.AddEquipmentItem(itemId, appearanceOverride, autoEquip);
        if (string.IsNullOrWhiteSpace(entryId))
            return string.Empty;

        eventPublisher.PublishInventoryChanged(player, itemId);

        if (autoEquip)
        {
            EquipmentSlotType slot = ItemDatabase.TryGetDefinition(itemId, out ItemDefinition definition)
                ? definition.EquipmentSlot
                : EquipmentSlotType.None;

            RefreshEquippedState(
                playerData,
                player,
                slot,
                entryId,
                itemId,
                true,
                clampVitalsToEquipmentBonuses,
                resolveSupportedScenePlayer);
        }

        savePlayer?.Invoke();
        return entryId;
    }

    public bool TryEquipInventoryEntry(
        PlayerCharacter player,
        string inventoryEntryId)
    {
        PlayerRuntimeData playerData = ResolvePlayerData();
        InventoryEntry inventoryEntry = inventoryService.GetEntryById(inventoryEntryId);
        if (inventoryEntry == null)
            return false;

        if (!equipmentService.TryEquipEntry(inventoryEntryId, out EquipmentSlotType slot, out string replacedInventoryEntryId))
            return false;

        RefreshEquippedState(
            playerData,
            player,
            slot,
            inventoryEntryId,
            inventoryEntry.ItemId,
            true,
            clampVitalsToEquipmentBonuses,
            resolveSupportedScenePlayer);

        if (!string.IsNullOrWhiteSpace(replacedInventoryEntryId))
        {
            InventoryEntry replacedEntry = inventoryService.GetEntryById(replacedInventoryEntryId);
            PublishEquipmentChangedForScenePlayer(
                player,
                slot,
                replacedInventoryEntryId,
                replacedEntry != null ? replacedEntry.ItemId : string.Empty,
                false,
                resolveSupportedScenePlayer);
        }

        savePlayer?.Invoke();
        return true;
    }

    public bool TryUnequipItem(
        PlayerCharacter player,
        EquipmentSlotType slot)
    {
        PlayerRuntimeData playerData = ResolvePlayerData();
        InventoryEntry equippedEntry = equipmentService.GetEquippedEntry(slot);
        if (equippedEntry == null || !equipmentService.TryUnequip(slot, out string inventoryEntryId))
            return false;

        RefreshEquippedState(
            playerData,
            player,
            slot,
            inventoryEntryId,
            equippedEntry.ItemId,
            false,
            clampVitalsToEquipmentBonuses,
            resolveSupportedScenePlayer);

        savePlayer?.Invoke();
        return true;
    }

    private void RefreshEquippedState(
        PlayerRuntimeData playerData,
        PlayerCharacter player,
        EquipmentSlotType slot,
        string inventoryEntryId,
        string itemId,
        bool isEquipped,
        Action clampVitalsToEquipmentBonuses,
        Func<PlayerCharacter, PlayerCharacter> resolveSupportedScenePlayer)
    {
        clampVitalsToEquipmentBonuses?.Invoke();

        PlayerCharacter scenePlayer = resolveSupportedScenePlayer != null
            ? resolveSupportedScenePlayer(player)
            : player;

        if (scenePlayer == null)
            return;

        eventPublisher.PublishSessionState(playerData, scenePlayer);

        if (slot != EquipmentSlotType.None)
            eventPublisher.PublishEquipmentChanged(scenePlayer, slot, inventoryEntryId, itemId, isEquipped);
    }

    private void PublishEquipmentChangedForScenePlayer(
        PlayerCharacter player,
        EquipmentSlotType slot,
        string inventoryEntryId,
        string itemId,
        bool isEquipped,
        Func<PlayerCharacter, PlayerCharacter> resolveSupportedScenePlayer)
    {
        PlayerCharacter scenePlayer = resolveSupportedScenePlayer != null
            ? resolveSupportedScenePlayer(player)
            : player;

        if (scenePlayer == null)
            return;

        eventPublisher.PublishEquipmentChanged(scenePlayer, slot, inventoryEntryId, itemId, isEquipped);
    }

    private PlayerRuntimeData ResolvePlayerData()
    {
        return getPlayerData != null ? getPlayerData() : null;
    }

    private CharacterSaveData ResolveActiveCharacter()
    {
        return getActiveCharacter != null ? getActiveCharacter() : null;
    }
}

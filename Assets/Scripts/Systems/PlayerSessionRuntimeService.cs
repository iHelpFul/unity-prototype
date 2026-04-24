using System.Collections.Generic;
using UnityEngine;

public class PlayerSessionRuntimeService
{
    private readonly PlayerSessionInventoryService inventoryService;
    private readonly PlayerSessionEquipmentService equipmentService;
    private readonly PlayerSessionSkillService skillService;
    private readonly PlayerSessionPassiveService passiveService;
    private readonly PlayerSessionActionBarService actionBarService;
    private readonly PlayerSessionMapStateService mapStateService;

    private PlayerRuntimeData data;

    public PlayerSessionRuntimeService(
        PlayerSessionInventoryService inventoryService,
        PlayerSessionEquipmentService equipmentService,
        PlayerSessionSkillService skillService,
        PlayerSessionPassiveService passiveService,
        PlayerSessionActionBarService actionBarService,
        PlayerSessionMapStateService mapStateService)
    {
        this.inventoryService = inventoryService;
        this.equipmentService = equipmentService;
        this.skillService = skillService;
        this.passiveService = passiveService;
        this.actionBarService = actionBarService;
        this.mapStateService = mapStateService;
    }

    public bool HasActiveRuntimeData => data != null;

    public PlayerRuntimeData ApplyCharacterRuntime(
        CharacterSaveData character,
        string activeSceneName,
        string defaultMapId,
        string defaultSpawnId)
    {
        data = character != null ? character.RuntimeData : null;
        BindServices();
        NormalizeLoadedPlayerData(activeSceneName, defaultMapId, defaultSpawnId);
        return data;
    }

    public int GetEffectiveMaxHP()
    {
        if (data == null)
            return 0;

        ItemStatModifierData bonuses = equipmentService.GetTotalStatBonuses();
        return Mathf.Max(1, data.MaxHP + bonuses.MaxHP);
    }

    public int GetEffectiveMaxMP()
    {
        if (data == null)
            return 0;

        ItemStatModifierData bonuses = equipmentService.GetTotalStatBonuses();
        return Mathf.Max(0, data.MaxMP + bonuses.MaxMP);
    }

    public void ClampVitalsToEquipmentBonuses()
    {
        if (data == null)
            return;

        data.CurrentHP = Mathf.Clamp(data.CurrentHP, 0, GetEffectiveMaxHP());
        data.CurrentMP = Mathf.Clamp(data.CurrentMP, 0, GetEffectiveMaxMP());
    }

    private void BindServices()
    {
        inventoryService.SetRuntimeData(data);
        equipmentService.SetRuntimeData(data, inventoryService);
        skillService.SetRuntimeData(data);
        passiveService.SetRuntimeData(data);
        actionBarService.SetRuntimeData(data);
        mapStateService.SetRuntimeData(data);
    }

    private void NormalizeLoadedPlayerData(
        string activeSceneName,
        string defaultMapId,
        string defaultSpawnId)
    {
        if (data == null)
            return;

        PlayerProgressionRules.Normalize(data);
        data.QuestProgress ??= new List<PlayerQuestProgressEntry>();

        if (data.UnspentStatPoints < 0)
            data.UnspentStatPoints = 0;

        if (data.UnspentSkillPoints < 0)
            data.UnspentSkillPoints = 0;

        if (data.Might < 0)
            data.Might = 0;

        if (data.Precision < 0)
            data.Precision = 0;

        if (data.Arcane < 0)
            data.Arcane = 0;

        if (data.Finesse < 0)
            data.Finesse = 0;

        if (data.HitRate < 0)
            data.HitRate = 0;

        if (data.MaxHP <= 0)
            data.MaxHP = 100;

        if (data.MaxMP <= 0)
            data.MaxMP = 50;

        int effectiveMaxHP = GetEffectiveMaxHP();
        if (data.CurrentHP <= 0 || data.CurrentHP > effectiveMaxHP)
            data.CurrentHP = effectiveMaxHP;

        int effectiveMaxMP = GetEffectiveMaxMP();
        if (data.CurrentMP < 0 || data.CurrentMP > effectiveMaxMP)
            data.CurrentMP = effectiveMaxMP;

        if (data.Mesos < 0)
            data.Mesos = 0;

        inventoryService.MigrateLegacyConsumables();
        skillService.EnsureDefaultSkillsForJob(data.CurrentJob);
        passiveService.EnsureDefaultPassivesForJob(data.CurrentJob);
        PlayerInputBindingUtility.EnsureDefaultInputData(data);

        if (string.IsNullOrWhiteSpace(data.CurrentMapId))
            data.CurrentMapId = !string.IsNullOrWhiteSpace(defaultMapId) ? defaultMapId : activeSceneName ?? string.Empty;

        if (string.IsNullOrWhiteSpace(data.LastSpawnId))
            data.LastSpawnId = string.IsNullOrWhiteSpace(defaultSpawnId)
                ? SceneSpawnPoint.DefaultSpawnId
                : SceneSpawnPoint.NormalizeSpawnId(defaultSpawnId);

        mapStateService.ClearPendingTransition();
    }
}

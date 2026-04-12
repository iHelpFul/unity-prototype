using System;
using System.Collections.Generic;

public static class CharacterSaveFactory
{
    public static CharacterSaveData CreateDefaultDrifterCharacter(
        string nickname,
        CharacterAppearanceData appearance,
        string startMapId,
        string startSpawnId)
    {
        CharacterSaveData character = new CharacterSaveData
        {
            CharacterId = Guid.NewGuid().ToString("N"),
            Nickname = string.IsNullOrWhiteSpace(nickname) ? "Character" : nickname.Trim(),
            Appearance = appearance != null ? appearance.Clone() : new CharacterAppearanceData(),
            RuntimeData = CreateDefaultRuntimeData(startMapId, startSpawnId)
        };

        CharacterStarterEquipmentFactory.ApplyStarterEquipment(character);
        return character;
    }

    public static PlayerRuntimeData CreateDefaultRuntimeData(string startMapId, string startSpawnId)
    {
        PlayerRuntimeData runtimeData = new PlayerRuntimeData
        {
            CurrentJob = PlayerJobType.Drifter,
            Level = 1,
            CurrentExp = 0,
            RequiredExp = 0,
            Might = 0,
            Precision = 0,
            Arcane = 0,
            Finesse = 0,
            HitRate = 4,
            UnspentStatPoints = 0,
            MaxHP = 100,
            CurrentHP = 100,
            MaxMP = 50,
            CurrentMP = 50,
            Mesos = 0,
            RedPotionCount = 0,
            BluePotionCount = 0,
            HasPendingJobAdvancement = false,
            CurrentMapId = startMapId ?? string.Empty,
            LastSpawnId = string.IsNullOrWhiteSpace(startSpawnId) ? SceneSpawnPoint.DefaultSpawnId : SceneSpawnPoint.NormalizeSpawnId(startSpawnId),
            PendingMapId = string.Empty,
            PendingSpawnId = string.Empty,
            Inventory = new List<InventoryEntry>(),
            EquippedItems = new List<EquippedItemEntry>(),
            UnlockedSkills = new List<PlayerSkillEntry>()
        };

        PlayerProgressionRules.RefreshDerivedState(runtimeData);
        return runtimeData;
    }
}


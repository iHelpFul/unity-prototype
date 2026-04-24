using System.Collections.Generic;
using UnityEngine;

public enum PlayerConsumableType
{
    RedPotion,
    BluePotion
}

public enum PlayerQuestProgressStatus
{
    None,
    Accepted,
    Completed
}

[System.Serializable]
public class PlayerRuntimeData
{
    public PlayerJobType CurrentJob;
    public int Level;
    public int CurrentExp;
    public int RequiredExp;

    public int Might;
    public int Precision;
    public int Arcane;
    public int Finesse;
    public int HitRate;
    public int UnspentStatPoints;
    public int UnspentSkillPoints;

    public int MaxHP;
    public int CurrentHP;

    public int MaxMP;
    public int CurrentMP;

    public int Mesos;

    // Legacy fields kept for save migration. New runtime logic uses Inventory.
    public int RedPotionCount;
    public int BluePotionCount;

    public List<InventoryEntry> Inventory = new List<InventoryEntry>();
    public List<EquippedItemEntry> EquippedItems = new List<EquippedItemEntry>();
    public List<PlayerSkillEntry> UnlockedSkills = new List<PlayerSkillEntry>();
    public List<PlayerPassiveEntry> UnlockedPassives = new List<PlayerPassiveEntry>();
    public List<PlayerActionBarSlotEntry> ActionBarSlots = new List<PlayerActionBarSlotEntry>();
    public List<PlayerInputBindingEntry> InputBindings = new List<PlayerInputBindingEntry>();
    public List<PlayerQuestProgressEntry> QuestProgress = new List<PlayerQuestProgressEntry>();
    public bool HasPendingJobAdvancement;

    public string CurrentMapId;
    public string LastSpawnId;
    public string PendingMapId;
    public string PendingSpawnId;
}

[System.Serializable]
public sealed class PlayerQuestObjectiveProgress
{
    public int ObjectiveIndex;
    public NpcQuestObjectiveType ObjectiveType;
    public string TargetId;
    public int BaselineValue;
    public int RequiredAmount;
}

[System.Serializable]
public sealed class PlayerQuestProgressEntry
{
    public string QuestId;
    public PlayerQuestProgressStatus Status;
    public int CompletionCount;
    public List<PlayerQuestObjectiveProgress> ObjectiveProgress = new List<PlayerQuestObjectiveProgress>();
}

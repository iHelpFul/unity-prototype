using System.Collections.Generic;
using UnityEngine;

public enum PlayerConsumableType
{
    RedPotion,
    BluePotion
}

[System.Serializable]
public class PlayerRuntimeData
{
    public PlayerJobType CurrentJob;
    public int Level;
    public int CurrentExp;
    public int RequiredExp;

    public int Strength;
    public int Dexterity;

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
    public bool HasPendingJobAdvancement;

    public string CurrentMapId;
    public string LastSpawnId;
    public string PendingMapId;
    public string PendingSpawnId;
}

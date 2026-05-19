using System.Collections.Generic;
using UnityEngine;

public enum PlayerInputActionId
{
    None = 0,
    Move = 1,
    Jump = 2,
    BasicAttack = 3,
    SkillSlot3Shortcut = 4,
    Interact = 5,
    ActionBarSlot = 6,
    UseRedPotion = 7,
    UseBluePotion = 8,
    InventoryToggle = 9,
    ProgressionToggle = 10,
    QuestLogToggle = 11,
    SkillAndPassiveWindowToggle = 12
}

public enum PlayerActionBarAssignmentKind
{
    None = 0,
    ActiveSkill = 1,
    Consumable = 2,
    SystemAction = 3
}

[System.Serializable]
public class PlayerActionBarSlotEntry
{
    public int SlotIndex = 1;
    public PlayerActionBarAssignmentKind AssignmentKind = PlayerActionBarAssignmentKind.None;
    public string AssignedId = string.Empty;
    public PlayerConsumableType ConsumableType = PlayerConsumableType.RedPotion;
    public PlayerInputActionId SystemAction = PlayerInputActionId.None;
}

[System.Serializable]
public class PlayerInputBindingEntry
{
    public string BindingId = string.Empty;
    public PlayerInputActionId ActionId = PlayerInputActionId.None;
    public int SlotIndex;
    public string DisplayName = string.Empty;
    public string BindingPath = string.Empty;
}

public static class PlayerInputBindingUtility
{
    public const int PermanentActionBarSlotCount = 6;

    public static void EnsureDefaultInputData(PlayerRuntimeData data)
    {
        if (data == null)
            return;

        data.ActionBarSlots ??= new List<PlayerActionBarSlotEntry>();
        data.InputBindings ??= new List<PlayerInputBindingEntry>();

        NormalizeActionBarSlots(data.ActionBarSlots);
        NormalizeInputBindings(data.InputBindings);
        EnsurePermanentActionBarSlots(data.ActionBarSlots);
        MigrateLegacySkillAssignmentsIntoActionBar(data);
        SortActionBarSlots(data.ActionBarSlots);
    }

    public static PlayerActionBarSlotEntry GetActionBarSlot(PlayerRuntimeData data, int slotIndex)
    {
        EnsureDefaultInputData(data);

        if (data?.ActionBarSlots == null || slotIndex <= 0)
            return null;

        return FindActionBarSlot(data.ActionBarSlots, slotIndex);
    }

    public static PlayerActionBarSlotEntry GetOrCreateActionBarSlot(PlayerRuntimeData data, int slotIndex)
    {
        if (data == null || slotIndex <= 0)
            return null;

        EnsureDefaultInputData(data);
        PlayerActionBarSlotEntry slot = FindActionBarSlot(data.ActionBarSlots, slotIndex);
        if (slot != null)
            return slot;

        slot = new PlayerActionBarSlotEntry
        {
            SlotIndex = slotIndex
        };
        data.ActionBarSlots.Add(slot);
        SortActionBarSlots(data.ActionBarSlots);
        return slot;
    }

    public static void AssignActiveSkillToActionBarSlot(PlayerRuntimeData data, string skillId, int slotIndex)
    {
        if (data == null || string.IsNullOrWhiteSpace(skillId) || slotIndex <= 0)
            return;

        PlayerActionBarSlotEntry slot = GetOrCreateActionBarSlot(data, slotIndex);
        if (slot == null)
            return;

        ClearActiveSkillAssignments(data.ActionBarSlots, skillId.Trim());
        slot.AssignmentKind = PlayerActionBarAssignmentKind.ActiveSkill;
        slot.AssignedId = skillId.Trim();
        slot.SystemAction = PlayerInputActionId.None;
    }

    public static void ClearActionBarAssignment(PlayerRuntimeData data, int slotIndex)
    {
        PlayerActionBarSlotEntry slot = GetActionBarSlot(data, slotIndex);
        if (slot == null)
            return;

        slot.AssignmentKind = PlayerActionBarAssignmentKind.None;
        slot.AssignedId = string.Empty;
        slot.SystemAction = PlayerInputActionId.None;
    }

    public static int ResolveActionBarSlotForSkill(PlayerRuntimeData data, string skillId)
    {
        EnsureDefaultInputData(data);

        return ResolveActionBarSlotForSkill(data?.ActionBarSlots, skillId);
    }

    private static void MigrateLegacySkillAssignmentsIntoActionBar(PlayerRuntimeData data)
    {
        if (data?.UnlockedSkills == null)
            return;

        for (int index = 0; index < data.UnlockedSkills.Count; index++)
        {
            PlayerSkillEntry skillEntry = data.UnlockedSkills[index];
            if (skillEntry == null
                || string.IsNullOrWhiteSpace(skillEntry.SkillId)
                || skillEntry.LegacyAssignedSlotIndex <= 0)
            {
                continue;
            }

            string normalizedSkillId = skillEntry.SkillId.Trim();
            if (ResolveActionBarSlotForSkill(data.ActionBarSlots, normalizedSkillId) > 0)
            {
                skillEntry.LegacyAssignedSlotIndex = -1;
                continue;
            }

            PlayerActionBarSlotEntry slot = FindActionBarSlot(data.ActionBarSlots, skillEntry.LegacyAssignedSlotIndex);
            if (slot != null && slot.AssignmentKind == PlayerActionBarAssignmentKind.None)
            {
                slot.AssignmentKind = PlayerActionBarAssignmentKind.ActiveSkill;
                slot.AssignedId = normalizedSkillId;
                slot.SystemAction = PlayerInputActionId.None;
            }

            skillEntry.LegacyAssignedSlotIndex = -1;
        }
    }

    private static PlayerActionBarSlotEntry FindActionBarSlot(
        List<PlayerActionBarSlotEntry> slots,
        int slotIndex)
    {
        if (slots == null || slotIndex <= 0)
            return null;

        for (int index = 0; index < slots.Count; index++)
        {
            PlayerActionBarSlotEntry slot = slots[index];
            if (slot != null && slot.SlotIndex == slotIndex)
                return slot;
        }

        return null;
    }

    private static int ResolveActionBarSlotForSkill(
        List<PlayerActionBarSlotEntry> slots,
        string skillId)
    {
        if (slots == null || string.IsNullOrWhiteSpace(skillId))
            return -1;

        string normalizedSkillId = skillId.Trim();
        for (int index = 0; index < slots.Count; index++)
        {
            PlayerActionBarSlotEntry slot = slots[index];
            if (slot != null
                && slot.AssignmentKind == PlayerActionBarAssignmentKind.ActiveSkill
                && slot.AssignedId == normalizedSkillId)
            {
                return slot.SlotIndex;
            }
        }

        return -1;
    }

    private static void EnsurePermanentActionBarSlots(List<PlayerActionBarSlotEntry> slots)
    {
        for (int slotIndex = 1; slotIndex <= PermanentActionBarSlotCount; slotIndex++)
        {
            bool exists = false;
            for (int index = 0; index < slots.Count; index++)
            {
                if (slots[index] != null && slots[index].SlotIndex == slotIndex)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                slots.Add(new PlayerActionBarSlotEntry
                {
                    SlotIndex = slotIndex
                });
            }
        }

        SortActionBarSlots(slots);
    }

    private static void NormalizeActionBarSlots(List<PlayerActionBarSlotEntry> slots)
    {
        HashSet<int> usedSlots = new HashSet<int>();

        for (int index = slots.Count - 1; index >= 0; index--)
        {
            PlayerActionBarSlotEntry slot = slots[index];
            if (slot == null || slot.SlotIndex <= 0 || !usedSlots.Add(slot.SlotIndex))
            {
                slots.RemoveAt(index);
                continue;
            }

            slot.AssignedId = string.IsNullOrWhiteSpace(slot.AssignedId) ? string.Empty : slot.AssignedId.Trim();

            if (!System.Enum.IsDefined(typeof(PlayerActionBarAssignmentKind), slot.AssignmentKind))
                slot.AssignmentKind = PlayerActionBarAssignmentKind.None;

            if (!System.Enum.IsDefined(typeof(PlayerInputActionId), slot.SystemAction))
                slot.SystemAction = PlayerInputActionId.None;

            if (slot.AssignmentKind == PlayerActionBarAssignmentKind.ActiveSkill && string.IsNullOrWhiteSpace(slot.AssignedId))
                slot.AssignmentKind = PlayerActionBarAssignmentKind.None;
        }
    }

    private static void NormalizeInputBindings(List<PlayerInputBindingEntry> bindings)
    {
        HashSet<string> usedBindingIds = new HashSet<string>();

        for (int index = bindings.Count - 1; index >= 0; index--)
        {
            PlayerInputBindingEntry binding = bindings[index];
            if (binding == null)
            {
                bindings.RemoveAt(index);
                continue;
            }

            binding.BindingId = string.IsNullOrWhiteSpace(binding.BindingId)
                ? BuildBindingId(binding.ActionId, binding.SlotIndex)
                : binding.BindingId.Trim();
            binding.DisplayName = string.IsNullOrWhiteSpace(binding.DisplayName) ? string.Empty : binding.DisplayName.Trim();
            binding.BindingPath = string.IsNullOrWhiteSpace(binding.BindingPath) ? string.Empty : binding.BindingPath.Trim();
            binding.SlotIndex = Mathf.Max(0, binding.SlotIndex);

            if (!System.Enum.IsDefined(typeof(PlayerInputActionId), binding.ActionId))
                binding.ActionId = PlayerInputActionId.None;

            if (!usedBindingIds.Add(binding.BindingId))
                bindings.RemoveAt(index);
        }
    }

    private static void ClearActiveSkillAssignments(List<PlayerActionBarSlotEntry> slots, string skillId)
    {
        if (slots == null || string.IsNullOrWhiteSpace(skillId))
            return;

        for (int index = 0; index < slots.Count; index++)
        {
            PlayerActionBarSlotEntry slot = slots[index];
            if (slot == null
                || slot.AssignmentKind != PlayerActionBarAssignmentKind.ActiveSkill
                || slot.AssignedId != skillId)
            {
                continue;
            }

            slot.AssignmentKind = PlayerActionBarAssignmentKind.None;
            slot.AssignedId = string.Empty;
            slot.SystemAction = PlayerInputActionId.None;
        }
    }

    private static string BuildBindingId(PlayerInputActionId actionId, int slotIndex)
    {
        return slotIndex > 0
            ? $"{actionId}_{slotIndex}"
            : actionId.ToString();
    }

    private static void SortActionBarSlots(List<PlayerActionBarSlotEntry> slots)
    {
        slots.Sort((left, right) =>
        {
            int leftIndex = left != null ? left.SlotIndex : int.MaxValue;
            int rightIndex = right != null ? right.SlotIndex : int.MaxValue;
            return leftIndex.CompareTo(rightIndex);
        });
    }
}

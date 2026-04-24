using System.Collections.Generic;

public class PlayerSessionActionBarService
{
    private PlayerRuntimeData data;

    public void SetRuntimeData(PlayerRuntimeData runtimeData)
    {
        data = runtimeData;
        PlayerInputBindingUtility.EnsureDefaultInputData(data);
    }

    public IReadOnlyList<PlayerActionBarSlotEntry> GetSlots()
    {
        PlayerInputBindingUtility.EnsureDefaultInputData(data);
        return data?.ActionBarSlots != null
            ? (IReadOnlyList<PlayerActionBarSlotEntry>)data.ActionBarSlots
            : System.Array.Empty<PlayerActionBarSlotEntry>();
    }

    public PlayerActionBarSlotEntry GetSlot(int slotIndex)
    {
        return PlayerInputBindingUtility.GetActionBarSlot(data, slotIndex);
    }

    public bool TryAssignActiveSkillToSlot(string skillId, int slotIndex)
    {
        if (data?.UnlockedSkills == null || string.IsNullOrWhiteSpace(skillId) || slotIndex <= 0)
            return false;

        string normalizedSkillId = skillId.Trim();
        if (!PlayerSkillDatabase.TryGetDefinition(normalizedSkillId, out _))
            return false;

        for (int index = 0; index < data.UnlockedSkills.Count; index++)
        {
            PlayerSkillEntry skillEntry = data.UnlockedSkills[index];
            if (skillEntry == null)
                continue;

            if (skillEntry.SkillId == normalizedSkillId)
            {
                ClearExistingAssignmentsForId(PlayerActionBarAssignmentKind.ActiveSkill, normalizedSkillId);
                PlayerInputBindingUtility.AssignActiveSkillToActionBarSlot(data, normalizedSkillId, slotIndex);
                return true;
            }
        }

        return false;
    }

    public bool TryAssignConsumableToSlot(PlayerConsumableType consumableType, int slotIndex)
    {
        if (data == null || slotIndex <= 0)
            return false;

        PlayerActionBarSlotEntry slot = PlayerInputBindingUtility.GetOrCreateActionBarSlot(data, slotIndex);
        if (slot == null)
            return false;

        slot.AssignmentKind = PlayerActionBarAssignmentKind.Consumable;
        slot.AssignedId = consumableType.ToString();
        slot.ConsumableType = consumableType;
        slot.SystemAction = PlayerInputActionId.None;
        return true;
    }

    public bool TryAssignSystemActionToSlot(PlayerInputActionId actionId, int slotIndex)
    {
        if (data == null || slotIndex <= 0 || !IsValidSystemAction(actionId))
            return false;

        PlayerActionBarSlotEntry slot = PlayerInputBindingUtility.GetOrCreateActionBarSlot(data, slotIndex);
        if (slot == null)
            return false;

        ClearExistingSystemAction(actionId);
        slot.AssignmentKind = PlayerActionBarAssignmentKind.SystemAction;
        slot.AssignedId = actionId.ToString();
        slot.SystemAction = actionId;
        return true;
    }

    public bool TryClearSlot(int slotIndex)
    {
        if (data == null || slotIndex <= 0)
            return false;

        PlayerInputBindingUtility.ClearActionBarAssignment(data, slotIndex);
        return true;
    }

    private void ClearExistingAssignmentsForId(PlayerActionBarAssignmentKind assignmentKind, string assignedId)
    {
        if (data?.ActionBarSlots == null || string.IsNullOrWhiteSpace(assignedId))
            return;

        for (int index = 0; index < data.ActionBarSlots.Count; index++)
        {
            PlayerActionBarSlotEntry slot = data.ActionBarSlots[index];
            if (slot == null
                || slot.AssignmentKind != assignmentKind
                || slot.AssignedId != assignedId)
            {
                continue;
            }

            slot.AssignmentKind = PlayerActionBarAssignmentKind.None;
            slot.AssignedId = string.Empty;
            slot.SystemAction = PlayerInputActionId.None;
        }
    }

    private void ClearExistingSystemAction(PlayerInputActionId actionId)
    {
        if (data?.ActionBarSlots == null || actionId == PlayerInputActionId.None)
            return;

        for (int index = 0; index < data.ActionBarSlots.Count; index++)
        {
            PlayerActionBarSlotEntry slot = data.ActionBarSlots[index];
            if (slot == null
                || slot.AssignmentKind != PlayerActionBarAssignmentKind.SystemAction
                || slot.SystemAction != actionId)
            {
                continue;
            }

            slot.AssignmentKind = PlayerActionBarAssignmentKind.None;
            slot.AssignedId = string.Empty;
            slot.SystemAction = PlayerInputActionId.None;
        }
    }

    private static bool IsValidSystemAction(PlayerInputActionId actionId)
    {
        return actionId == PlayerInputActionId.InventoryToggle
            || actionId == PlayerInputActionId.ProgressionToggle
            || actionId == PlayerInputActionId.QuestLogToggle
            || actionId == PlayerInputActionId.SkillAndPassiveWindowToggle
            || actionId == PlayerInputActionId.Interact;
    }
}

using System.Collections.Generic;
using UnityEngine;

public class PlayerSessionSkillService
{
    private readonly PlayerSkillRuntimeModule runtimeModule = new PlayerSkillRuntimeModule();
    private PlayerRuntimeData data;

    public void SetRuntimeData(PlayerRuntimeData runtimeData)
    {
        data = runtimeData;
        EnsureSkillCollections();
        runtimeModule.Clear();
    }

    public IReadOnlyList<PlayerSkillEntry> GetUnlockedSkills()
    {
        if (data == null)
            return System.Array.Empty<PlayerSkillEntry>();

        return data.UnlockedSkills != null
            ? (IReadOnlyList<PlayerSkillEntry>)data.UnlockedSkills
            : System.Array.Empty<PlayerSkillEntry>();
    }

    public PlayerSkillEntry GetAssignedSkill(int slotIndex)
    {
        if (data?.UnlockedSkills == null || slotIndex <= 0)
            return null;

        PlayerActionBarSlotEntry actionBarSlot = PlayerInputBindingUtility.GetActionBarSlot(data, slotIndex);
        return actionBarSlot != null
            && actionBarSlot.AssignmentKind == PlayerActionBarAssignmentKind.ActiveSkill
            && !string.IsNullOrWhiteSpace(actionBarSlot.AssignedId)
            ? FindUnlockedSkill(actionBarSlot.AssignedId)
            : null;
    }

    public PlayerSkillDefinition GetAssignedSkillDefinition(int slotIndex)
    {
        PlayerSkillEntry assignedSkill = GetAssignedSkill(slotIndex);
        return assignedSkill != null
            ? PlayerSkillDatabase.GetDefinition(assignedSkill.SkillId)
            : null;
    }

    public bool TryAssignSkillToSlot(string skillId, int slotIndex)
    {
        if (data?.UnlockedSkills == null || slotIndex <= 0)
            return false;

        string normalizedSkillId = string.IsNullOrWhiteSpace(skillId) ? string.Empty : skillId.Trim();
        if (!PlayerSkillDatabase.TryGetDefinition(normalizedSkillId, out _))
            return false;

        if (FindUnlockedSkill(normalizedSkillId) == null)
            return false;

        PlayerInputBindingUtility.AssignActiveSkillToActionBarSlot(data, normalizedSkillId, slotIndex);
        return true;
    }

    public bool TryUnlockSkill(string skillId, int skillLevel = 1)
    {
        if (data == null)
            return false;

        if (!PlayerSkillDatabase.TryGetDefinition(skillId, out PlayerSkillDefinition definition))
            return false;

        EnsureSkillCollections();

        foreach (PlayerSkillEntry skillEntry in data.UnlockedSkills)
        {
            if (skillEntry != null && skillEntry.SkillId == definition.SkillId)
            {
                skillEntry.SkillLevel = Mathf.Clamp(skillLevel, 1, definition.MaxLevel);
                if (PlayerInputBindingUtility.ResolveActionBarSlotForSkill(data, definition.SkillId) <= 0
                    && definition.DefaultSlotIndex > 0)
                {
                    PlayerInputBindingUtility.AssignActiveSkillToActionBarSlot(data, definition.SkillId, definition.DefaultSlotIndex);
                }

                return true;
            }
        }

        data.UnlockedSkills.Add(new PlayerSkillEntry
        {
            SkillId = definition.SkillId,
            SkillLevel = Mathf.Clamp(skillLevel, 1, definition.MaxLevel)
        });

        if (definition.DefaultSlotIndex > 0)
            PlayerInputBindingUtility.AssignActiveSkillToActionBarSlot(data, definition.SkillId, definition.DefaultSlotIndex);

        return true;
    }

    public void EnsureDefaultSkillsForJob(PlayerJobType jobType)
    {
        if (data == null)
            return;

        EnsureSkillCollections();

        IReadOnlyList<PlayerSkillDefinition> defaultSkills =
            PlayerJobCombatProfiles.GetDefaultSkillsForJob(jobType);

        foreach (PlayerSkillDefinition defaultSkill in defaultSkills)
        {
            bool alreadyUnlocked = false;

            foreach (PlayerSkillEntry existingSkill in data.UnlockedSkills)
            {
                if (existingSkill != null && existingSkill.SkillId == defaultSkill.SkillId)
                {
                    alreadyUnlocked = true;

                    if (PlayerInputBindingUtility.ResolveActionBarSlotForSkill(data, defaultSkill.SkillId) <= 0
                        && defaultSkill.DefaultSlotIndex > 0)
                    {
                        PlayerInputBindingUtility.AssignActiveSkillToActionBarSlot(data, defaultSkill.SkillId, defaultSkill.DefaultSlotIndex);
                    }

                    break;
                }
            }

            if (alreadyUnlocked)
                continue;

            data.UnlockedSkills.Add(new PlayerSkillEntry
            {
                SkillId = defaultSkill.SkillId,
                SkillLevel = 1
            });

            if (defaultSkill.DefaultSlotIndex > 0)
                PlayerInputBindingUtility.AssignActiveSkillToActionBarSlot(data, defaultSkill.SkillId, defaultSkill.DefaultSlotIndex);
        }
    }

    public bool IsSkillOnCooldown(string skillId)
    {
        return runtimeModule.IsOnCooldown(skillId);
    }

    public float GetRemainingSkillCooldown(string skillId)
    {
        return runtimeModule.GetRemainingCooldown(skillId);
    }

    public void StartSkillCooldown(string skillId, float cooldown)
    {
        runtimeModule.StartCooldown(skillId, cooldown);
    }

    private void EnsureSkillCollections()
    {
        if (data != null)
        {
            data.UnlockedSkills ??= new List<PlayerSkillEntry>();
            PlayerInputBindingUtility.EnsureDefaultInputData(data);
        }
    }

    private PlayerSkillEntry FindUnlockedSkill(string skillId)
    {
        if (data?.UnlockedSkills == null || string.IsNullOrWhiteSpace(skillId))
            return null;

        string normalizedSkillId = skillId.Trim();
        for (int index = 0; index < data.UnlockedSkills.Count; index++)
        {
            PlayerSkillEntry skillEntry = data.UnlockedSkills[index];
            if (skillEntry != null && skillEntry.SkillId == normalizedSkillId)
                return skillEntry;
        }

        return null;
    }
}

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

        foreach (PlayerSkillEntry skillEntry in data.UnlockedSkills)
        {
            if (skillEntry != null && skillEntry.AssignedSlotIndex == slotIndex)
                return skillEntry;
        }

        return null;
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

        if (!PlayerSkillDatabase.TryGetDefinition(skillId, out _))
            return false;

        PlayerSkillEntry targetEntry = null;

        foreach (PlayerSkillEntry skillEntry in data.UnlockedSkills)
        {
            if (skillEntry == null)
                continue;

            if (skillEntry.AssignedSlotIndex == slotIndex)
                skillEntry.AssignedSlotIndex = -1;

            if (skillEntry.SkillId == skillId)
                targetEntry = skillEntry;
        }

        if (targetEntry == null)
            return false;

        targetEntry.AssignedSlotIndex = slotIndex;
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
                if (skillEntry.AssignedSlotIndex <= 0 && definition.DefaultSlotIndex > 0)
                    skillEntry.AssignedSlotIndex = definition.DefaultSlotIndex;

                return true;
            }
        }

        data.UnlockedSkills.Add(new PlayerSkillEntry
        {
            SkillId = definition.SkillId,
            SkillLevel = Mathf.Clamp(skillLevel, 1, definition.MaxLevel),
            AssignedSlotIndex = definition.DefaultSlotIndex
        });

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

                    if (existingSkill.AssignedSlotIndex <= 0 && defaultSkill.DefaultSlotIndex > 0)
                        existingSkill.AssignedSlotIndex = defaultSkill.DefaultSlotIndex;

                    break;
                }
            }

            if (alreadyUnlocked)
                continue;

            data.UnlockedSkills.Add(new PlayerSkillEntry
            {
                SkillId = defaultSkill.SkillId,
                SkillLevel = 1,
                AssignedSlotIndex = defaultSkill.DefaultSlotIndex
            });
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
            data.UnlockedSkills ??= new List<PlayerSkillEntry>();
    }
}

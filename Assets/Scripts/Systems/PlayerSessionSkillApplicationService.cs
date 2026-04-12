using System;
using System.Collections.Generic;

public class PlayerSessionSkillApplicationService
{
    private readonly PlayerSessionSkillService skillService;
    private readonly Action savePlayer;

    public PlayerSessionSkillApplicationService(
        PlayerSessionSkillService skillService,
        Action savePlayer)
    {
        this.skillService = skillService;
        this.savePlayer = savePlayer;
    }

    public IReadOnlyList<PlayerSkillEntry> GetUnlockedSkills()
    {
        return skillService.GetUnlockedSkills();
    }

    public PlayerSkillEntry GetAssignedSkill(int slotIndex)
    {
        return skillService.GetAssignedSkill(slotIndex);
    }

    public PlayerSkillDefinition GetAssignedSkillDefinition(int slotIndex)
    {
        return skillService.GetAssignedSkillDefinition(slotIndex);
    }

    public bool TryAssignSkillToSlot(string skillId, int slotIndex)
    {
        if (!skillService.TryAssignSkillToSlot(skillId, slotIndex))
            return false;

        savePlayer?.Invoke();
        return true;
    }

    public bool TryUnlockSkill(string skillId, int skillLevel)
    {
        if (!skillService.TryUnlockSkill(skillId, skillLevel))
            return false;

        savePlayer?.Invoke();
        return true;
    }

    public bool IsSkillOnCooldown(string skillId)
    {
        return skillService.IsSkillOnCooldown(skillId);
    }

    public float GetRemainingSkillCooldown(string skillId)
    {
        return skillService.GetRemainingSkillCooldown(skillId);
    }

    public void StartSkillCooldown(string skillId, float cooldown)
    {
        skillService.StartSkillCooldown(skillId, cooldown);
    }
}

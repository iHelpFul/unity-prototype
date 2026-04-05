using System.Collections.Generic;
using UnityEngine;

public class PlayerSkillRuntimeModule
{
    private readonly Dictionary<string, float> cooldownEndTimes = new Dictionary<string, float>();

    public bool IsOnCooldown(string skillId)
    {
        return GetRemainingCooldown(skillId) > 0f;
    }

    public float GetRemainingCooldown(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return 0f;

        if (!cooldownEndTimes.TryGetValue(skillId, out float cooldownEndTime))
            return 0f;

        float remainingCooldown = cooldownEndTime - Time.time;
        if (remainingCooldown <= 0f)
        {
            cooldownEndTimes.Remove(skillId);
            return 0f;
        }

        return remainingCooldown;
    }

    public void StartCooldown(string skillId, float cooldown)
    {
        if (string.IsNullOrWhiteSpace(skillId) || cooldown <= 0f)
            return;

        cooldownEndTimes[skillId] = Time.time + cooldown;
    }

    public void Clear()
    {
        cooldownEndTimes.Clear();
    }
}

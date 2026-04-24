using UnityEngine;

public static class PlayerProgressionRules
{
    public static void Normalize(PlayerRuntimeData data)
    {
        if (data == null)
            return;

        if (!System.Enum.IsDefined(typeof(PlayerJobType), data.CurrentJob))
            data.CurrentJob = PlayerJobType.Drifter;

        if (data.Level <= 0)
            data.Level = 1;

        if (data.CurrentExp < 0)
            data.CurrentExp = 0;

        if (data.UnspentStatPoints < 0)
            data.UnspentStatPoints = 0;

        if (data.UnspentSkillPoints < 0)
            data.UnspentSkillPoints = 0;

        PlayerInputBindingUtility.EnsureDefaultInputData(data);
        RefreshDerivedState(data);
    }

    public static void RefreshDerivedState(PlayerRuntimeData data)
    {
        if (data == null)
            return;

        if (!System.Enum.IsDefined(typeof(PlayerJobType), data.CurrentJob))
            data.CurrentJob = PlayerJobType.Drifter;

        if (data.Level <= 0)
            data.Level = 1;

        data.RequiredExp = GetRequiredExpForLevel(data.Level);
        data.HasPendingJobAdvancement = PlayerJobCombatProfiles.IsJobAdvancementAvailable(data);
    }

    public static int GetStatPointsAwardedPerLevelUp()
    {
        return PlayerProgressionProfiles.Active.GetStatPointsAwardedPerLevelUp();
    }

    public static int GetSkillPointsAwardedPerLevelUp()
    {
        return PlayerProgressionProfiles.Active.GetSkillPointsAwardedPerLevelUp();
    }

    public static int GetMaxHpGainPerLevel()
    {
        return PlayerProgressionProfiles.Active.GetMaxHpGainPerLevel();
    }

    public static int GetMaxMpGainPerLevel()
    {
        return PlayerProgressionProfiles.Active.GetMaxMpGainPerLevel();
    }

    public static int GetRequiredExpForLevel(int level)
    {
        return PlayerProgressionProfiles.Active.GetRequiredExpForLevel(level);
    }
}


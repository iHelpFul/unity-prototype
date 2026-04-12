using UnityEngine;

public static class PlayerProgressionRules
{
    public static void Normalize(PlayerRuntimeData data)
    {
        if (data == null)
            return;

        if (!System.Enum.IsDefined(typeof(PlayerJobType), data.CurrentJob))
            data.CurrentJob = PlayerJobType.Novice;

        if (data.Level <= 0)
            data.Level = 1;

        if (data.CurrentExp < 0)
            data.CurrentExp = 0;

        RefreshDerivedState(data);
    }

    public static void RefreshDerivedState(PlayerRuntimeData data)
    {
        if (data == null)
            return;

        if (!System.Enum.IsDefined(typeof(PlayerJobType), data.CurrentJob))
            data.CurrentJob = PlayerJobType.Novice;

        if (data.Level <= 0)
            data.Level = 1;

        data.RequiredExp = GetRequiredExpForLevel(data.Level);
        data.HasPendingJobAdvancement = PlayerJobCombatProfiles.IsJobAdvancementAvailable(data);
    }

    public static int GetRequiredExpForLevel(int level)
    {
        if (level <= 1)
            return 15;

        float multiplier = 14.5f;
        float exponent = level < 6 ? 1.4f : 2.25f;
        float offset = 0f;

        if (level >= 6)
            offset = -100f;

        if (level >= 10)
            multiplier = 16.2f;

        int result = Mathf.RoundToInt(multiplier * Mathf.Pow(level, exponent) + offset);
        return Mathf.Max(result, 15);
    }
}

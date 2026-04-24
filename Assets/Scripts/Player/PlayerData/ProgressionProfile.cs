using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class ProgressionLevelExpOverride
{
    [SerializeField] private int level = 1;
    [SerializeField] private int requiredExp = 15;

    public int Level => level;
    public int RequiredExp => requiredExp;

    public void Sanitize()
    {
        level = Mathf.Max(1, level);
        requiredExp = Mathf.Max(1, requiredExp);
    }
}

[CreateAssetMenu(menuName = "Game Data/Progression/Progression Profile")]
public class ProgressionProfile : ScriptableObject
{
    [Header("Level Rewards")]
    [SerializeField] private int statPointsAwardedPerLevelUp = 6;
    [SerializeField] private int skillPointsAwardedPerLevelUp = 1;
    [SerializeField] private int maxHpGainPerLevel = 20;
    [SerializeField] private int maxMpGainPerLevel = 10;

    [Header("Experience Curve")]
    [SerializeField] private int levelOneRequiredExp = 15;
    [SerializeField] private int minimumRequiredExp = 15;
    [SerializeField] private int earlyCurveMaxLevel = 5;
    [SerializeField] private int lateMultiplierStartLevel = 10;
    [SerializeField] private float earlyMultiplier = 14.5f;
    [SerializeField] private float lateMultiplier = 16.2f;
    [SerializeField] private float earlyExponent = 1.4f;
    [SerializeField] private float lateExponent = 2.25f;
    [SerializeField] private float lateOffset = -100f;
    [SerializeField] private List<ProgressionLevelExpOverride> expOverrides = new List<ProgressionLevelExpOverride>();

    public int GetStatPointsAwardedPerLevelUp()
    {
        return Mathf.Max(0, statPointsAwardedPerLevelUp);
    }

    public int GetSkillPointsAwardedPerLevelUp()
    {
        return Mathf.Max(0, skillPointsAwardedPerLevelUp);
    }

    public int GetMaxHpGainPerLevel()
    {
        return Mathf.Max(0, maxHpGainPerLevel);
    }

    public int GetMaxMpGainPerLevel()
    {
        return Mathf.Max(0, maxMpGainPerLevel);
    }

    public int GetRequiredExpForLevel(int level)
    {
        int sanitizedLevel = Mathf.Max(1, level);
        if (TryGetExpOverride(sanitizedLevel, out int requiredExp))
            return requiredExp;

        if (sanitizedLevel <= 1)
            return Mathf.Max(1, levelOneRequiredExp);

        float multiplier = sanitizedLevel >= lateMultiplierStartLevel
            ? lateMultiplier
            : earlyMultiplier;
        float exponent = sanitizedLevel <= earlyCurveMaxLevel
            ? earlyExponent
            : lateExponent;
        float offset = sanitizedLevel <= earlyCurveMaxLevel ? 0f : lateOffset;

        int result = Mathf.RoundToInt(multiplier * Mathf.Pow(sanitizedLevel, exponent) + offset);
        return Mathf.Max(result, Mathf.Max(1, minimumRequiredExp));
    }

    private bool TryGetExpOverride(int level, out int requiredExp)
    {
        requiredExp = 0;
        if (expOverrides == null)
            return false;

        for (int index = 0; index < expOverrides.Count; index++)
        {
            ProgressionLevelExpOverride overrideEntry = expOverrides[index];
            if (overrideEntry == null || overrideEntry.Level != level)
                continue;

            requiredExp = Mathf.Max(1, overrideEntry.RequiredExp);
            return true;
        }

        return false;
    }

    private void OnValidate()
    {
        statPointsAwardedPerLevelUp = Mathf.Max(0, statPointsAwardedPerLevelUp);
        skillPointsAwardedPerLevelUp = Mathf.Max(0, skillPointsAwardedPerLevelUp);
        maxHpGainPerLevel = Mathf.Max(0, maxHpGainPerLevel);
        maxMpGainPerLevel = Mathf.Max(0, maxMpGainPerLevel);
        levelOneRequiredExp = Mathf.Max(1, levelOneRequiredExp);
        minimumRequiredExp = Mathf.Max(1, minimumRequiredExp);
        earlyCurveMaxLevel = Mathf.Max(1, earlyCurveMaxLevel);
        lateMultiplierStartLevel = Mathf.Max(earlyCurveMaxLevel + 1, lateMultiplierStartLevel);
        earlyMultiplier = Mathf.Max(0f, earlyMultiplier);
        lateMultiplier = Mathf.Max(0f, lateMultiplier);
        earlyExponent = Mathf.Max(0.01f, earlyExponent);
        lateExponent = Mathf.Max(0.01f, lateExponent);
        expOverrides ??= new List<ProgressionLevelExpOverride>();

        HashSet<int> usedLevels = new HashSet<int>();
        for (int index = expOverrides.Count - 1; index >= 0; index--)
        {
            ProgressionLevelExpOverride overrideEntry = expOverrides[index];
            if (overrideEntry == null)
            {
                expOverrides.RemoveAt(index);
                continue;
            }

            overrideEntry.Sanitize();
            if (!usedLevels.Add(overrideEntry.Level))
                expOverrides.RemoveAt(index);
        }

        expOverrides.Sort((left, right) => left.Level.CompareTo(right.Level));
    }
}

public static class PlayerProgressionProfiles
{
    private const string ResourcePath = "GameData/ProgressionProfile";

    private static ProgressionProfile activeProfile;

    public static ProgressionProfile Active
    {
        get
        {
            if (activeProfile != null)
                return activeProfile;

            activeProfile = Resources.Load<ProgressionProfile>(ResourcePath);
            if (activeProfile != null)
                return activeProfile;

            activeProfile = ScriptableObject.CreateInstance<ProgressionProfile>();
            activeProfile.hideFlags = HideFlags.HideAndDontSave;
            return activeProfile;
        }
    }

    public static void ResetCache()
    {
        activeProfile = null;
    }
}

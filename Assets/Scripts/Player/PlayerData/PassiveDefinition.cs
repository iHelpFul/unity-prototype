using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PassiveStatModifier
{
    [SerializeField] private PlayerProgressionStatType statType = PlayerProgressionStatType.Might;
    [SerializeField] private int flatBonus;

    public PlayerProgressionStatType StatType => statType;
    public int FlatBonus => flatBonus;
}

[System.Serializable]
public class PassiveAttackFamilyModifier
{
    [SerializeField] private CombatAttackFamily attackFamily = CombatAttackFamily.None;
    [SerializeField] private float damageBonus;
    [SerializeField] private float rangeBonus;
    [SerializeField] private float surgeChanceBonus;
    [SerializeField] private float surgePowerBonus;

    public CombatAttackFamily AttackFamily => attackFamily;
    public float DamageBonus => damageBonus;
    public float RangeBonus => rangeBonus;
    public float SurgeChanceBonus => surgeChanceBonus;
    public float SurgePowerBonus => surgePowerBonus;
}

[System.Serializable]
public class PassiveLevelDefinition
{
    [SerializeField] private int level = 1;
    [SerializeField] private List<PassiveStatModifier> statModifiers = new List<PassiveStatModifier>();
    [SerializeField] private List<PassiveAttackFamilyModifier> attackFamilyModifiers = new List<PassiveAttackFamilyModifier>();
    [SerializeField] private float projectileSpeedBonus;
    [SerializeField] private float globalRangeBonus;
    [SerializeField] private float surgeChanceBonus;
    [SerializeField] private float surgePowerBonus;
    [SerializeField] private int momentumGainBonus;
    [SerializeField] private float stateDurationBonus;

    public int Level => level;
    public IReadOnlyList<PassiveStatModifier> StatModifiers => statModifiers;
    public IReadOnlyList<PassiveAttackFamilyModifier> AttackFamilyModifiers => attackFamilyModifiers;
    public float ProjectileSpeedBonus => projectileSpeedBonus;
    public float GlobalRangeBonus => globalRangeBonus;
    public float SurgeChanceBonus => surgeChanceBonus;
    public float SurgePowerBonus => surgePowerBonus;
    public int MomentumGainBonus => momentumGainBonus;
    public float StateDurationBonus => stateDurationBonus;

    public void Sanitize()
    {
        level = Mathf.Max(1, level);
        statModifiers ??= new List<PassiveStatModifier>();
        attackFamilyModifiers ??= new List<PassiveAttackFamilyModifier>();
        projectileSpeedBonus = Mathf.Max(0f, projectileSpeedBonus);
        globalRangeBonus = Mathf.Max(0f, globalRangeBonus);
        surgeChanceBonus = Mathf.Max(0f, surgeChanceBonus);
        surgePowerBonus = Mathf.Max(0f, surgePowerBonus);
        momentumGainBonus = Mathf.Max(0, momentumGainBonus);
        stateDurationBonus = Mathf.Max(0f, stateDurationBonus);
    }
}

[CreateAssetMenu(menuName = "Game Data/Skills/Passive Definition")]
public class PassiveDefinition : ScriptableObject
{
    [SerializeField] private string passiveId = string.Empty;
    [SerializeField] private string displayName = "New Passive";
    [SerializeField] private string description = string.Empty;
    [SerializeField] private Sprite icon;
    [SerializeField] private PlayerJobType[] allowedJobs = new PlayerJobType[0];
    [SerializeField] private List<PassiveLevelDefinition> levels = new List<PassiveLevelDefinition>();

    public string PassiveId => passiveId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public IReadOnlyList<PlayerJobType> AllowedJobs => allowedJobs;
    public IReadOnlyList<PassiveLevelDefinition> Levels => levels;

    public PassiveLevelDefinition GetLevelDefinition(int passiveLevel = 1)
    {
        if (levels == null || levels.Count == 0)
            return null;

        int clampedLevel = Mathf.Max(1, passiveLevel);
        PassiveLevelDefinition bestMatch = null;

        for (int index = 0; index < levels.Count; index++)
        {
            PassiveLevelDefinition candidate = levels[index];
            if (candidate == null)
                continue;

            if (candidate.Level == clampedLevel)
                return candidate;

            if (candidate.Level <= clampedLevel)
            {
                if (bestMatch == null || candidate.Level > bestMatch.Level)
                    bestMatch = candidate;
            }
        }

        return bestMatch;
    }

    public bool SupportsJob(PlayerJobType jobType)
    {
        if (allowedJobs == null || allowedJobs.Length == 0)
            return true;

        for (int index = 0; index < allowedJobs.Length; index++)
        {
            if (allowedJobs[index] == jobType)
                return true;
        }

        return false;
    }

    private void OnValidate()
    {
        passiveId = string.IsNullOrWhiteSpace(passiveId) ? string.Empty : passiveId.Trim();
        displayName = string.IsNullOrWhiteSpace(displayName) ? "Unnamed Passive" : displayName.Trim();
        description = string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim();
        allowedJobs ??= new PlayerJobType[0];
        levels ??= new List<PassiveLevelDefinition>();

        int highestLevel = 0;
        for (int index = 0; index < levels.Count; index++)
        {
            PassiveLevelDefinition levelDefinition = levels[index];
            if (levelDefinition == null)
                continue;

            levelDefinition.Sanitize();
            highestLevel = Mathf.Max(highestLevel, levelDefinition.Level);
        }
    }
}

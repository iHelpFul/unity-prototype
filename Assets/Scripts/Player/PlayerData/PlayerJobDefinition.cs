using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/Jobs/Player Job Definition")]
public class PlayerJobDefinition : ScriptableObject
{
    [SerializeField] private PlayerJobType jobType = PlayerJobType.Novice;
    [SerializeField] private string displayName = "Novice";
    [SerializeField] private string description = string.Empty;
    [SerializeField] private Sprite icon;
    [SerializeField] private PlayerProgressionStatType coreStat = PlayerProgressionStatType.Might;
    [SerializeField] private PlayerProgressionStatType secondaryStat = PlayerProgressionStatType.Precision;
    [SerializeField] private WeaponType[] allowedWeaponTypes = new WeaponType[0];
    [SerializeField] private int advancementLevelRequirement = 10;
    [SerializeField] private PlayerBasicAttackProfile basicAttackProfile;
    [SerializeField] private AnimationProfile animationProfile;
    [SerializeField] private CombatFormulaProfile combatFormulaProfile;
    [SerializeField] private int baseMaxGauge = 10;
    [SerializeField] private List<PlayerSkillDefinition> defaultSkills = new List<PlayerSkillDefinition>();
    [SerializeField] private List<PassiveDefinition> defaultPassives = new List<PassiveDefinition>();

    public PlayerJobType JobType => jobType;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public PlayerProgressionStatType CoreStat => coreStat;
    public PlayerProgressionStatType SecondaryStat => secondaryStat;
    public WeaponType[] AllowedWeaponTypes => allowedWeaponTypes;
    public int AdvancementLevelRequirement => advancementLevelRequirement;
    public PlayerBasicAttackProfile BasicAttackProfile => basicAttackProfile;
    public AnimationProfile AnimationProfile => animationProfile;
    public CombatFormulaProfile CombatFormulaProfile => combatFormulaProfile;
    public int BaseMaxGauge => baseMaxGauge;
    public IReadOnlyList<PlayerSkillDefinition> DefaultSkills => defaultSkills;
    public IReadOnlyList<PassiveDefinition> DefaultPassives => defaultPassives;

    public void SetDefaultSkills(IReadOnlyList<PlayerSkillDefinition> newDefaultSkills)
    {
        defaultSkills = new List<PlayerSkillDefinition>();

        if (newDefaultSkills == null)
            return;

        for (int index = 0; index < newDefaultSkills.Count; index++)
        {
            PlayerSkillDefinition definition = newDefaultSkills[index];
            if (definition != null)
                defaultSkills.Add(definition);
        }
    }

    public void SetDefaultPassives(IReadOnlyList<PassiveDefinition> newDefaultPassives)
    {
        defaultPassives = new List<PassiveDefinition>();

        if (newDefaultPassives == null)
            return;

        for (int index = 0; index < newDefaultPassives.Count; index++)
        {
            PassiveDefinition definition = newDefaultPassives[index];
            if (definition != null)
                defaultPassives.Add(definition);
        }
    }

    public bool SupportsWeaponType(WeaponType weaponType)
    {
        if (allowedWeaponTypes == null || allowedWeaponTypes.Length == 0)
            return false;

        for (int index = 0; index < allowedWeaponTypes.Length; index++)
        {
            if (allowedWeaponTypes[index] == weaponType)
                return true;
        }

        return false;
    }

    private void OnValidate()
    {
        Sanitize();
    }

    private void Sanitize()
    {
        displayName = string.IsNullOrWhiteSpace(displayName) ? jobType.ToString() : displayName.Trim();
        description = string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim();
        advancementLevelRequirement = Mathf.Max(0, advancementLevelRequirement);
        baseMaxGauge = Mathf.Max(0, baseMaxGauge);
        allowedWeaponTypes ??= new WeaponType[0];
        defaultSkills ??= new List<PlayerSkillDefinition>();
        defaultPassives ??= new List<PassiveDefinition>();

        HashSet<string> uniqueSkillIds = new HashSet<string>();
        List<PlayerSkillDefinition> normalizedSkills = new List<PlayerSkillDefinition>();

        for (int index = 0; index < defaultSkills.Count; index++)
        {
            PlayerSkillDefinition definition = defaultSkills[index];
            if (definition == null || string.IsNullOrWhiteSpace(definition.SkillId))
                continue;

            if (!uniqueSkillIds.Add(definition.SkillId))
                continue;

            normalizedSkills.Add(definition);
        }

        normalizedSkills.Sort((left, right) => left.DefaultSlotIndex.CompareTo(right.DefaultSlotIndex));
        defaultSkills = normalizedSkills;

        HashSet<string> uniquePassiveIds = new HashSet<string>();
        List<PassiveDefinition> normalizedPassives = new List<PassiveDefinition>();

        for (int index = 0; index < defaultPassives.Count; index++)
        {
            PassiveDefinition definition = defaultPassives[index];
            if (definition == null || string.IsNullOrWhiteSpace(definition.PassiveId))
                continue;

            if (!uniquePassiveIds.Add(definition.PassiveId))
                continue;

            normalizedPassives.Add(definition);
        }

        defaultPassives = normalizedPassives;
    }
}

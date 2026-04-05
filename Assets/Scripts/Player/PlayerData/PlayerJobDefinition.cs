using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/Jobs/Player Job Definition")]
public class PlayerJobDefinition : ScriptableObject
{
    [SerializeField] private PlayerJobType jobType = PlayerJobType.Novice;
    [SerializeField] private string displayName = "Novice";
    [SerializeField] private int advancementLevelRequirement = 10;
    [SerializeField] private PlayerBasicAttackProfile basicAttackProfile;
    [SerializeField] private List<PlayerSkillDefinition> defaultSkills = new List<PlayerSkillDefinition>();

    public PlayerJobType JobType => jobType;
    public string DisplayName => displayName;
    public int AdvancementLevelRequirement => advancementLevelRequirement;
    public PlayerBasicAttackProfile BasicAttackProfile => basicAttackProfile;
    public IReadOnlyList<PlayerSkillDefinition> DefaultSkills => defaultSkills;

    public void Initialize(
        PlayerJobType newJobType,
        string newDisplayName,
        int newAdvancementLevelRequirement,
        PlayerBasicAttackProfile newBasicAttackProfile,
        IReadOnlyList<PlayerSkillDefinition> newDefaultSkills)
    {
        jobType = newJobType;
        displayName = newDisplayName;
        advancementLevelRequirement = newAdvancementLevelRequirement;
        basicAttackProfile = newBasicAttackProfile;
        SetDefaultSkills(newDefaultSkills);
        Sanitize();
    }

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

    public static PlayerJobDefinition CreateTransient(
        PlayerJobType newJobType,
        string newDisplayName,
        int newAdvancementLevelRequirement,
        PlayerBasicAttackProfile newBasicAttackProfile,
        IReadOnlyList<PlayerSkillDefinition> newDefaultSkills)
    {
        PlayerJobDefinition definition = CreateInstance<PlayerJobDefinition>();
        definition.hideFlags = HideFlags.HideAndDontSave;
        definition.Initialize(
            newJobType,
            newDisplayName,
            newAdvancementLevelRequirement,
            newBasicAttackProfile,
            newDefaultSkills);
        return definition;
    }

    private void OnValidate()
    {
        Sanitize();
    }

    private void Sanitize()
    {
        displayName = string.IsNullOrWhiteSpace(displayName) ? jobType.ToString() : displayName.Trim();
        advancementLevelRequirement = Mathf.Max(0, advancementLevelRequirement);

        if (defaultSkills == null)
        {
            defaultSkills = new List<PlayerSkillDefinition>();
            return;
        }

        HashSet<string> uniqueIds = new HashSet<string>();
        List<PlayerSkillDefinition> normalized = new List<PlayerSkillDefinition>();

        for (int index = 0; index < defaultSkills.Count; index++)
        {
            PlayerSkillDefinition definition = defaultSkills[index];
            if (definition == null || string.IsNullOrWhiteSpace(definition.SkillId))
                continue;

            if (!uniqueIds.Add(definition.SkillId))
                continue;

            normalized.Add(definition);
        }

        normalized.Sort((left, right) => left.DefaultSlotIndex.CompareTo(right.DefaultSlotIndex));
        defaultSkills = normalized;
    }
}

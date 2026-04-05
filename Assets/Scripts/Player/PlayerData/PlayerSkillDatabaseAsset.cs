using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/Skills/Player Skill Database")]
public class PlayerSkillDatabaseAsset : ScriptableObject
{
    [SerializeField] private List<PlayerSkillDefinition> definitions = new List<PlayerSkillDefinition>();

    public IReadOnlyList<PlayerSkillDefinition> Definitions => definitions;

    public void SetDefinitions(IReadOnlyList<PlayerSkillDefinition> newDefinitions)
    {
        definitions = new List<PlayerSkillDefinition>();

        if (newDefinitions == null)
            return;

        for (int index = 0; index < newDefinitions.Count; index++)
        {
            PlayerSkillDefinition definition = newDefinitions[index];
            if (definition != null)
                definitions.Add(definition);
        }
    }

    private void OnValidate()
    {
        if (definitions == null)
        {
            definitions = new List<PlayerSkillDefinition>();
            return;
        }

        HashSet<string> uniqueIds = new HashSet<string>();
        List<PlayerSkillDefinition> normalized = new List<PlayerSkillDefinition>();

        for (int index = 0; index < definitions.Count; index++)
        {
            PlayerSkillDefinition definition = definitions[index];
            if (definition == null || string.IsNullOrWhiteSpace(definition.SkillId))
                continue;

            if (!uniqueIds.Add(definition.SkillId))
                continue;

            normalized.Add(definition);
        }

        definitions = normalized;
    }
}

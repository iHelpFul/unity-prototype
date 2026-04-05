using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/Jobs/Player Job Database")]
public class PlayerJobDatabaseAsset : ScriptableObject
{
    [SerializeField] private List<PlayerJobDefinition> definitions = new List<PlayerJobDefinition>();

    public IReadOnlyList<PlayerJobDefinition> Definitions => definitions;

    public void SetDefinitions(IReadOnlyList<PlayerJobDefinition> newDefinitions)
    {
        definitions = new List<PlayerJobDefinition>();

        if (newDefinitions == null)
            return;

        for (int index = 0; index < newDefinitions.Count; index++)
        {
            PlayerJobDefinition definition = newDefinitions[index];
            if (definition != null)
                definitions.Add(definition);
        }
    }

    private void OnValidate()
    {
        if (definitions == null)
        {
            definitions = new List<PlayerJobDefinition>();
            return;
        }

        HashSet<PlayerJobType> uniqueTypes = new HashSet<PlayerJobType>();
        List<PlayerJobDefinition> normalized = new List<PlayerJobDefinition>();

        for (int index = 0; index < definitions.Count; index++)
        {
            PlayerJobDefinition definition = definitions[index];
            if (definition == null)
                continue;

            if (!uniqueTypes.Add(definition.JobType))
                continue;

            normalized.Add(definition);
        }

        definitions = normalized;
    }
}

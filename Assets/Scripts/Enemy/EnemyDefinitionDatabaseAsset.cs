using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/Enemies/Enemy Definition Database")]
public class EnemyDefinitionDatabaseAsset : ScriptableObject
{
    [SerializeField] private List<EnemyDefinition> definitions = new List<EnemyDefinition>();

    public IReadOnlyList<EnemyDefinition> Definitions => definitions;

    public void SetDefinitions(IReadOnlyList<EnemyDefinition> newDefinitions)
    {
        definitions = new List<EnemyDefinition>();

        if (newDefinitions == null)
            return;

        for (int index = 0; index < newDefinitions.Count; index++)
        {
            EnemyDefinition definition = newDefinitions[index];
            if (definition != null)
                definitions.Add(definition);
        }
    }

    private void OnValidate()
    {
        if (definitions == null)
        {
            definitions = new List<EnemyDefinition>();
            return;
        }

        HashSet<EnemyType> uniqueTypes = new HashSet<EnemyType>();
        List<EnemyDefinition> normalized = new List<EnemyDefinition>();

        for (int index = 0; index < definitions.Count; index++)
        {
            EnemyDefinition definition = definitions[index];
            if (definition == null)
                continue;

            if (!uniqueTypes.Add(definition.EnemyType))
                continue;

            normalized.Add(definition);
        }

        definitions = normalized;
    }
}

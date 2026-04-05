using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/Items/Item Database")]
public class ItemDatabaseAsset : ScriptableObject
{
    [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();

    public IReadOnlyList<ItemDefinition> Items => items;

    public void SetItems(IReadOnlyList<ItemDefinition> definitions)
    {
        items = new List<ItemDefinition>();

        if (definitions == null)
            return;

        for (int index = 0; index < definitions.Count; index++)
        {
            ItemDefinition definition = definitions[index];
            if (definition != null)
                items.Add(definition);
        }
    }

    private void OnValidate()
    {
        if (items == null)
        {
            items = new List<ItemDefinition>();
            return;
        }

        HashSet<string> uniqueIds = new HashSet<string>();
        List<ItemDefinition> normalized = new List<ItemDefinition>();

        for (int index = 0; index < items.Count; index++)
        {
            ItemDefinition definition = items[index];
            if (definition == null || string.IsNullOrWhiteSpace(definition.ItemId))
                continue;

            if (!uniqueIds.Add(definition.ItemId))
                continue;

            normalized.Add(definition);
        }

        items = normalized;
    }
}

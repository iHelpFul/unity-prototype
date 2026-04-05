using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/NPC/Vendor Definition")]
public class NpcVendorDefinition : ScriptableObject
{
    [SerializeField] private bool buysPlayerItems = true;
    [SerializeField] private List<ItemDefinition> stockedItems = new List<ItemDefinition>();

    public bool BuysPlayerItems => buysPlayerItems;
    public IReadOnlyList<ItemDefinition> StockedItems => stockedItems;

    public void Initialize(bool newBuysPlayerItems, IReadOnlyList<ItemDefinition> newStockedItems)
    {
        buysPlayerItems = newBuysPlayerItems;
        stockedItems = new List<ItemDefinition>();

        if (newStockedItems == null)
            return;

        for (int index = 0; index < newStockedItems.Count; index++)
        {
            ItemDefinition item = newStockedItems[index];
            if (item != null)
                stockedItems.Add(item);
        }

        Sanitize();
    }

    private void OnValidate()
    {
        Sanitize();
    }

    private void Sanitize()
    {
        if (stockedItems == null)
        {
            stockedItems = new List<ItemDefinition>();
            return;
        }

        HashSet<string> uniqueIds = new HashSet<string>();
        List<ItemDefinition> normalized = new List<ItemDefinition>();

        for (int index = 0; index < stockedItems.Count; index++)
        {
            ItemDefinition definition = stockedItems[index];
            if (definition == null || string.IsNullOrWhiteSpace(definition.ItemId))
                continue;

            if (!uniqueIds.Add(definition.ItemId))
                continue;

            normalized.Add(definition);
        }

        stockedItems = normalized;
    }
}

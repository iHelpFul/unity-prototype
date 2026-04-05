using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class NpcVendor : MonoBehaviour
{
    [SerializeField] private NpcVendorDefinition definition;
    [SerializeField] private bool buysPlayerItems = true;
    [SerializeField] private List<string> stockedItemIds = new List<string>();
    private readonly List<string> resolvedDefinitionItemIds = new List<string>();

    public bool BuysPlayerItems => definition != null ? definition.BuysPlayerItems : buysPlayerItems;
    public IReadOnlyList<string> StockedItemIds
    {
        get
        {
            RefreshResolvedStock();
            return definition != null ? resolvedDefinitionItemIds : stockedItemIds;
        }
    }

    public bool SellsItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return false;

        string normalizedItemId = itemId.Trim();

        IReadOnlyList<string> stock = StockedItemIds;
        for (int index = 0; index < stock.Count; index++)
        {
            string stockedItemId = stock[index];
            if (!string.IsNullOrWhiteSpace(stockedItemId) && stockedItemId.Trim() == normalizedItemId)
                return true;
        }

        return false;
    }

    private void Reset()
    {
        RefreshResolvedStock();

        if (stockedItemIds == null)
            stockedItemIds = new List<string>();

        if (definition != null || stockedItemIds.Count > 0)
            return;

        stockedItemIds.Add(ItemDatabase.RedPotionId);
        stockedItemIds.Add(ItemDatabase.BluePotionId);
    }

    private void OnValidate()
    {
        RefreshResolvedStock();

        if (definition != null)
            return;

        if (stockedItemIds == null)
        {
            stockedItemIds = new List<string>();
            return;
        }

        HashSet<string> uniqueIds = new HashSet<string>();
        List<string> normalizedIds = new List<string>();

        for (int index = 0; index < stockedItemIds.Count; index++)
        {
            string normalizedId = string.IsNullOrWhiteSpace(stockedItemIds[index])
                ? string.Empty
                : stockedItemIds[index].Trim();

            if (string.IsNullOrEmpty(normalizedId) || !uniqueIds.Add(normalizedId))
                continue;

            normalizedIds.Add(normalizedId);
        }

        stockedItemIds = normalizedIds;
    }

    private void RefreshResolvedStock()
    {
        resolvedDefinitionItemIds.Clear();

        if (definition?.StockedItems == null)
            return;

        HashSet<string> uniqueIds = new HashSet<string>();

        for (int index = 0; index < definition.StockedItems.Count; index++)
        {
            ItemDefinition item = definition.StockedItems[index];
            if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
                continue;

            if (!uniqueIds.Add(item.ItemId))
                continue;

            resolvedDefinitionItemIds.Add(item.ItemId);
        }
    }
}

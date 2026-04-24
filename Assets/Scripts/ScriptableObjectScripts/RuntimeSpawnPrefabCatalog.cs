using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/Runtime/Spawn Prefabs")]
public class RuntimeSpawnPrefabCatalog : ScriptableObject
{
    [Header("World Loot")]
    [Tooltip("Fallback prefab used for item/meso pickups when no specific prefab is set.")]
    [SerializeField] private GameObject worldLootPickupPrefab;
    [Tooltip("Optional dedicated fallback for meso pickups.")]
    [SerializeField] private GameObject mesoWorldLootPickupPrefab;
    [Tooltip("Optional dedicated fallback for item pickups.")]
    [SerializeField] private GameObject itemWorldLootPickupPrefab;

    public GameObject WorldLootPickupPrefab => worldLootPickupPrefab;

    public GameObject GetWorldLootPrefab(WorldLootType lootType, string itemId = "")
    {
        switch (lootType)
        {
            case WorldLootType.Mesos:
                if (mesoWorldLootPickupPrefab != null)
                    return mesoWorldLootPickupPrefab;

                return worldLootPickupPrefab;

            case WorldLootType.Item:
                string normalizedItemId = string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim();
                if (!string.IsNullOrWhiteSpace(normalizedItemId)
                    && ItemDatabase.TryGetDefinition(normalizedItemId, out ItemDefinition itemDefinition)
                    && itemDefinition.WorldLootPickupPrefab != null)
                {
                    return itemDefinition.WorldLootPickupPrefab;
                }

                if (itemWorldLootPickupPrefab != null)
                    return itemWorldLootPickupPrefab;

                return worldLootPickupPrefab;
        }

        return worldLootPickupPrefab;
    }
}

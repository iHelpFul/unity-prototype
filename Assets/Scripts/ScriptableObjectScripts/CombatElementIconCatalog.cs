using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/UI/Combat Element Icon Catalog")]
public class CombatElementIconCatalog : ScriptableObject
{
    [System.Serializable]
    private struct Entry
    {
        public CombatElementType ElementType;
        public Sprite Icon;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public Sprite GetIcon(CombatElementType elementType)
    {
        if (elementType == CombatElementType.None || entries == null)
            return null;

        for (int index = 0; index < entries.Count; index++)
        {
            if (entries[index].ElementType == elementType)
                return entries[index].Icon;
        }

        return null;
    }
}

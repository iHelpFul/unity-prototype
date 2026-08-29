using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "VFX/Vfx Library")]
public class VfxLibrary : ScriptableObject
{
    public VfxEntry[] Entries;

    // נקרא אוטומטית ברגע יצירת ה-Asset ב-Project Window
    private void Reset()
    {
        PopulateEntries();
    }

    // מאפשר גם לסנכרן ידנית בלחיצה ימנית על הקומפוננטה ב-Inspector
    [ContextMenu("Sync With VfxType Enum")]
    public void PopulateEntries()
    {
        Array enumValues = Enum.GetValues(typeof(VfxType));
        
        // שמירת ערכים קיימים במילון כדי לא לדרוס Prefabs שהוגדרו כבר
        Dictionary<VfxType, VfxEntry> existingMap = new Dictionary<VfxType, VfxEntry>();
        if (Entries != null)
        {
            foreach (var entry in Entries)
            {
                if (entry != null && !existingMap.ContainsKey(entry.Type))
                {
                    existingMap.Add(entry.Type, entry);
                }
            }
        }

        Entries = new VfxEntry[enumValues.Length];

        for (int i = 0; i < enumValues.Length; i++)
        {
            VfxType type = (VfxType)enumValues.GetValue(i);

            if (existingMap.TryGetValue(type, out VfxEntry existingEntry))
            {
                // שומר על ה-Entry הקיים
                Entries[i] = existingEntry;
            }
            else
            {
                // יוצר Entry חדש עם ערכי ברירת מחדל
                Entries[i] = new VfxEntry
                {
                    Type = type,
                    Prefabs = new GameObject[0],
                    MinScale = 1f,
                    MaxScale = 1f,
                    DefaultLifetime = 3f,
                    LeftFacingYaw = 180f
                };
            }
        }
    }

    private void OnValidate()
    {
        if (Entries == null || Entries.Length == 0)
        {
            PopulateEntries();
            return;
        }

        for (int index = 0; index < Entries.Length; index++)
        {
            VfxEntry entry = Entries[index];
            if (entry == null)
                continue;

            entry.DefaultLifetime = Mathf.Max(0f, entry.DefaultLifetime);
        }
    }
}
[System.Serializable]
public class VfxEntry
{
    public VfxType Type;
    public GameObject[] Prefabs;

    [Range(0.1f, 2f)] public float MinScale = 1f;
    [Range(0.1f, 2f)] public float MaxScale = 1f;
    [Min(0f)] public float DefaultLifetime = 3f;
    public bool UseFacingYaw;
    public float RightFacingYaw;
    public float LeftFacingYaw = 180f;
}

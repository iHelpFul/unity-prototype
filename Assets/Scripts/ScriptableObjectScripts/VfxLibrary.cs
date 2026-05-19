using UnityEngine;


[CreateAssetMenu(menuName = "VFX/Vfx Library")]
public class VfxLibrary : ScriptableObject
{
    public VfxEntry[] Entries;

    private void OnValidate()
    {
        if (Entries == null)
            return;

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

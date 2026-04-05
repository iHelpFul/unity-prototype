using UnityEngine;


[CreateAssetMenu(menuName = "VFX/Vfx Library")]
public class VfxLibrary : ScriptableObject
{
    public VfxEntry[] Entries;
}

[System.Serializable]
public class VfxEntry
{
    public VfxType Type;
    public GameObject[] Prefabs;

    [Range(0.5f, 2f)] public float MinScale = 1f;
    [Range(0.5f, 2f)] public float MaxScale = 1f;
}

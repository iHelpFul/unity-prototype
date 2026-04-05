using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Sfx Library")]
public class SfxLibrary : ScriptableObject
{
    public SfxEntry[] Entries;
}

[System.Serializable]
public class SfxEntry
{
    public SfxType Type;
    public AudioClip[] Clips;

    [Range(0f, 1f)] public float MinVolume = 0.8f;
    [Range(0f, 1f)] public float MaxVolume = 1f;

    [Range(0.5f, 2f)] public float MinPitch = 0.9f;
    [Range(0.5f, 2f)] public float MaxPitch = 1.1f;
}

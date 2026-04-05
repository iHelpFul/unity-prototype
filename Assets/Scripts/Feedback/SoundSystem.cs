using UnityEngine;
using System.Collections.Generic;

public class SoundSystem : MonoBehaviour
{
    [SerializeField] private AudioSource audioSourcePrefab;
    [SerializeField] private SfxLibrary library;

    private Dictionary<SfxType, SfxEntry> lookup;

    private void Awake()
    {
        lookup = new Dictionary<SfxType, SfxEntry>();

        foreach (var entry in library.Entries)
        {
            if (!lookup.ContainsKey(entry.Type))
                lookup.Add(entry.Type, entry);
        }
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlaySfxEvent>(OnPlaySfx);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlaySfxEvent>(OnPlaySfx);
    }

    private void OnPlaySfx(PlaySfxEvent e)
    {
        if (!lookup.TryGetValue(e.Type, out var entry))
            return;

        if (entry.Clips.Length == 0)
            return;

        AudioClip clip = entry.Clips[Random.Range(0, entry.Clips.Length)];

        AudioSource source = Instantiate(audioSourcePrefab, e.Position, Quaternion.identity);

        source.clip = clip;
        source.pitch = Random.Range(entry.MinPitch, entry.MaxPitch);
        source.volume = Random.Range(entry.MinVolume, entry.MaxVolume);

        source.Play();

        Destroy(source.gameObject, clip.length / source.pitch);
    }
}

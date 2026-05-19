using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SoundSystem : MonoBehaviour
{
    private readonly struct ActiveSfxKey
    {
        public ActiveSfxKey(SfxType type, Transform trackingTarget)
        {
            Type = type;
            TrackingTarget = trackingTarget;
        }

        public SfxType Type { get; }
        public Transform TrackingTarget { get; }
    }

    [SerializeField] private AudioSource audioSourcePrefab;
    [SerializeField] private SfxLibrary library;

    private Dictionary<SfxType, SfxEntry> lookup;
    private Dictionary<ActiveSfxKey, AudioSource> activePersistentSources;

    private void Awake()
    {
        lookup = new Dictionary<SfxType, SfxEntry>();
        activePersistentSources = new Dictionary<ActiveSfxKey, AudioSource>();

        foreach (var entry in library.Entries)
        {
            if (!lookup.ContainsKey(entry.Type))
                lookup.Add(entry.Type, entry);
        }
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlaySfxEvent>(OnPlaySfx);
        EventBus.Subscribe<StopSfxEvent>(OnStopSfx);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlaySfxEvent>(OnPlaySfx);
        EventBus.Unsubscribe<StopSfxEvent>(OnStopSfx);
    }

    private void OnPlaySfx(PlaySfxEvent e)
    {
        if (e.Delay > 0f)
        {
            StartCoroutine(PlayDelayed(e));
            return;
        }

        PlayNow(e);
    }

    private IEnumerator PlayDelayed(PlaySfxEvent e)
    {
        yield return new WaitForSeconds(e.Delay);
        PlayNow(e);
    }

    private void PlayNow(PlaySfxEvent e)
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
        source.loop = e.Persistent;

        if (e.FollowTarget != null)
        {
            AudioFollowTarget followTarget = source.GetComponent<AudioFollowTarget>();
            if (followTarget == null)
                followTarget = source.gameObject.AddComponent<AudioFollowTarget>();

            followTarget.Initialize(e.FollowTarget, e.FollowOffset);
            source.transform.position = e.FollowTarget.position + e.FollowOffset;
        }

        source.Play();

        bool canTrackPersistently = e.Persistent && e.TrackingTarget != null;
        if (canTrackPersistently)
        {
            ActiveSfxKey key = new ActiveSfxKey(e.Type, e.TrackingTarget);
            if (activePersistentSources.TryGetValue(key, out AudioSource existingSource))
            {
                if (existingSource != null)
                    Destroy(existingSource.gameObject);

                activePersistentSources.Remove(key);
            }

            activePersistentSources[key] = source;
            return;
        }

        Destroy(source.gameObject, clip.length / Mathf.Max(0.01f, source.pitch));
    }

    private void OnStopSfx(StopSfxEvent e)
    {
        if (e.TrackingTarget == null)
            return;

        ActiveSfxKey key = new ActiveSfxKey(e.Type, e.TrackingTarget);
        if (!activePersistentSources.TryGetValue(key, out AudioSource source))
            return;

        if (source != null)
            Destroy(source.gameObject);

        activePersistentSources.Remove(key);
    }
}

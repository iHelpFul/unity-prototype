using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VfxSystem : MonoBehaviour
{
    private readonly struct ActiveVfxKey
    {
        public ActiveVfxKey(VfxType type, Transform trackingTarget)
        {
            Type = type;
            TrackingTarget = trackingTarget;
        }

        public VfxType Type { get; }
        public Transform TrackingTarget { get; }
    }

    [SerializeField] private VfxLibrary library;

    private Dictionary<VfxType, VfxEntry> lookup;
    private Dictionary<ActiveVfxKey, GameObject> activePersistentInstances;

    private void Awake()
    {
        lookup = new Dictionary<VfxType, VfxEntry>();
        activePersistentInstances = new Dictionary<ActiveVfxKey, GameObject>();

        foreach (var entry in library.Entries)
        {
            if (!lookup.ContainsKey(entry.Type))
                lookup.Add(entry.Type, entry);
        }
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlayVfxEvent>(OnPlayVfx);
        EventBus.Subscribe<StopVfxEvent>(OnStopVfx);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayVfxEvent>(OnPlayVfx);
        EventBus.Unsubscribe<StopVfxEvent>(OnStopVfx);
    }

    private void OnPlayVfx(PlayVfxEvent e)
    {
        if (e.Delay > 0f)
        {
            StartCoroutine(PlayDelayed(e));
            return;
        }

        PlayNow(e);
    }

    private IEnumerator PlayDelayed(PlayVfxEvent e)
    {
        yield return new WaitForSeconds(e.Delay);
        PlayNow(e);
    }

    private void PlayNow(PlayVfxEvent e)
    {
        if (!lookup.TryGetValue(e.Type, out var entry))
            return;

        if (entry.Prefabs.Length == 0)
            return;

        GameObject prefab =
            entry.Prefabs[Random.Range(0, entry.Prefabs.Length)];

        GameObject instance =
            Instantiate(prefab, e.Position, e.Rotation);

        if (e.FollowTarget != null)
        {
            VfxFollowTarget followTarget = instance.GetComponent<VfxFollowTarget>();
            if (followTarget == null)
                followTarget = instance.AddComponent<VfxFollowTarget>();

            followTarget.Initialize(
                e.FollowTarget,
                e.FollowOffset,
                entry.UseFacingYaw,
                entry.RightFacingYaw,
                entry.LeftFacingYaw);
            Vector3 resolvedOffset = e.FollowOffset;
            if (entry.UseFacingYaw)
            {
                resolvedOffset.x = e.FollowTarget.forward.x >= 0f
                    ? Mathf.Abs(resolvedOffset.x)
                    : -Mathf.Abs(resolvedOffset.x);
            }

            instance.transform.position = e.FollowTarget.position + resolvedOffset;
            if (entry.UseFacingYaw)
            {
                float yaw = e.FollowTarget.forward.x >= 0f
                    ? entry.RightFacingYaw
                    : entry.LeftFacingYaw;
                instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }

        float scale =
            Random.Range(entry.MinScale, entry.MaxScale);

        instance.transform.localScale *= scale;

        bool canTrackPersistently = e.Persistent && e.TrackingTarget != null;
        if (canTrackPersistently)
        {
            ActiveVfxKey key = new ActiveVfxKey(e.Type, e.TrackingTarget);
            if (activePersistentInstances.TryGetValue(key, out GameObject existingInstance))
            {
                if (existingInstance != null)
                    Destroy(existingInstance);

                activePersistentInstances.Remove(key);
            }

            activePersistentInstances[key] = instance;
            return;
        }

        float lifetime = e.OverrideLifetime ? e.Lifetime : entry.DefaultLifetime;
        if (lifetime > 0f)
            Destroy(instance, lifetime);
    }

    private void OnStopVfx(StopVfxEvent e)
    {
        if (e.TrackingTarget == null)
            return;

        ActiveVfxKey key = new ActiveVfxKey(e.Type, e.TrackingTarget);
        if (!activePersistentInstances.TryGetValue(key, out GameObject instance))
            return;

        if (instance != null)
            Destroy(instance);

        activePersistentInstances.Remove(key);
    }
}

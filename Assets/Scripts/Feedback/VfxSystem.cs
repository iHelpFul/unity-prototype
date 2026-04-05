using UnityEngine;
using System.Collections.Generic;

public class VfxSystem : MonoBehaviour
{
    [SerializeField] private VfxLibrary library;

    private Dictionary<VfxType, VfxEntry> lookup;

    private void Awake()
    {
        lookup = new Dictionary<VfxType, VfxEntry>();

        foreach (var entry in library.Entries)
        {
            if (!lookup.ContainsKey(entry.Type))
                lookup.Add(entry.Type, entry);
        }
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlayVfxEvent>(OnPlayVfx);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayVfxEvent>(OnPlayVfx);
    }

    private void OnPlayVfx(PlayVfxEvent e)
    {
        if (!lookup.TryGetValue(e.Type, out var entry))
            return;

        if (entry.Prefabs.Length == 0)
            return;

        GameObject prefab =
            entry.Prefabs[Random.Range(0, entry.Prefabs.Length)];

        GameObject instance =
            Instantiate(prefab, e.Position, e.Rotation);

        float scale =
            Random.Range(entry.MinScale, entry.MaxScale);

        instance.transform.localScale *= scale;

        Destroy(instance, 3f);
    }
}
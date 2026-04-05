using System.Collections.Generic;
using UnityEngine;

public class KillTrackerSystem : MonoBehaviour
{
    private Dictionary<EnemyType, int> killCounts =
        new Dictionary<EnemyType, int>();

    private void OnEnable()
    {
        EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
    }

    private void OnEnemyDied(EnemyDiedEvent e)
    {
        if (!killCounts.ContainsKey(e.Type))
            killCounts[e.Type] = 0;

        killCounts[e.Type]++;

        Debug.Log($"Killed {e.Type}: {killCounts[e.Type]}");
    }

    public int GetKillCount(EnemyType type)
    {
        return killCounts.TryGetValue(type, out int count) ? count : 0;
    }
}
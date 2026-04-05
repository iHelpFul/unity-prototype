using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-950)]
public class FieldSpawnDirector : MonoBehaviour
{
    [Header("Field Spawn Control")]
    [SerializeField] private bool autoRegisterSceneEnemies = true;
    [SerializeField, Min(0)] private int maxAliveEnemies;
    [SerializeField, Min(0.25f)] private float respawnDelayMultiplier = 1f;
    [SerializeField, Min(0.25f)] private float lowDensityRespawnMultiplier = 0.8f;
    [SerializeField, Min(0f)] private float respawnCadence = 1.5f;
    [SerializeField, Min(0)] private int maxRespawnsPerCadence = 3;
    [SerializeField, Min(0.1f)] private float queueCheckInterval = 0.35f;

    private readonly List<EnemyHealth> registeredEnemies = new List<EnemyHealth>();
    private readonly Dictionary<EnemyHealth, int> reservedRespawnSlots = new Dictionary<EnemyHealth, int>();
    private readonly Dictionary<int, int> slotReservationCounts = new Dictionary<int, int>();

    public float QueueCheckInterval => Mathf.Max(0.1f, queueCheckInterval);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeInstance()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        FieldSpawnDirector[] directors = FindObjectsByType<FieldSpawnDirector>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (FieldSpawnDirector director in directors)
        {
            if (director != null && director.gameObject.scene == activeScene)
                return;
        }

        GameObject directorObject = new GameObject("[FieldSpawnDirector]");
        directorObject.AddComponent<FieldSpawnDirector>();
    }

    private void Awake()
    {
        if (ShouldDestroyDuplicateInScene())
        {
            Destroy(gameObject);
            return;
        }

        AutoRegisterSceneEnemies();
    }

    public void Register(EnemyHealth enemy)
    {
        if (enemy == null || registeredEnemies.Contains(enemy))
            return;

        registeredEnemies.Add(enemy);
    }

    public void Unregister(EnemyHealth enemy)
    {
        if (enemy == null)
            return;

        registeredEnemies.Remove(enemy);
        ReleaseRespawnReservation(enemy);
    }

    public float GetRespawnDelay(EnemyHealth enemy, float baseDelay)
    {
        Register(enemy);
        PruneMissingEnemies();

        float delay = Mathf.Max(0.5f, baseDelay * Mathf.Max(0.25f, respawnDelayMultiplier));
        int aliveCap = ResolveAliveCap();

        if (aliveCap > 0 && CountAliveEnemies() <= Mathf.Max(1, aliveCap / 2))
            delay *= Mathf.Max(0.25f, lowDensityRespawnMultiplier);

        delay = ReserveCadencedDelay(enemy, delay);
        return Mathf.Max(0.5f, delay);
    }

    public bool CanRespawn(EnemyHealth enemy)
    {
        Register(enemy);
        PruneMissingEnemies();

        int aliveCap = ResolveAliveCap();
        if (aliveCap <= 0)
            return true;

        return CountAliveEnemies() < aliveCap;
    }

    public void NotifyRespawnCompleted(EnemyHealth enemy)
    {
        if (enemy == null)
            return;

        ReleaseRespawnReservation(enemy);
    }

    private void AutoRegisterSceneEnemies()
    {
        if (!autoRegisterSceneEnemies)
            return;

        registeredEnemies.Clear();

        EnemyHealth[] sceneEnemies = FindObjectsByType<EnemyHealth>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (EnemyHealth enemy in sceneEnemies)
        {
            if (enemy != null && enemy.gameObject.scene == gameObject.scene)
                Register(enemy);
        }
    }

    private bool ShouldDestroyDuplicateInScene()
    {
        FieldSpawnDirector[] directors = FindObjectsByType<FieldSpawnDirector>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        FieldSpawnDirector keeper = this;

        foreach (FieldSpawnDirector director in directors)
        {
            if (director == null || director.gameObject.scene != gameObject.scene)
                continue;

            if (director.GetInstanceID() < keeper.GetInstanceID())
                keeper = director;
        }

        return keeper != this;
    }

    private int ResolveAliveCap()
    {
        if (maxAliveEnemies > 0)
            return maxAliveEnemies;

        return registeredEnemies.Count > 0 ? registeredEnemies.Count : 0;
    }

    private int CountAliveEnemies()
    {
        int aliveCount = 0;

        foreach (EnemyHealth enemy in registeredEnemies)
        {
            if (enemy == null || enemy.IsDead)
                continue;

            aliveCount++;
        }

        return aliveCount;
    }

    private void PruneMissingEnemies()
    {
        for (int index = registeredEnemies.Count - 1; index >= 0; index--)
        {
            if (registeredEnemies[index] == null)
                registeredEnemies.RemoveAt(index);
        }

        List<EnemyHealth> missingReservations = new List<EnemyHealth>();

        foreach (KeyValuePair<EnemyHealth, int> reservation in reservedRespawnSlots)
        {
            if (reservation.Key == null)
                missingReservations.Add(reservation.Key);
        }

        foreach (EnemyHealth missingEnemy in missingReservations)
            ReleaseRespawnReservation(missingEnemy);
    }

    private float ReserveCadencedDelay(EnemyHealth enemy, float delay)
    {
        float cadence = Mathf.Max(0f, respawnCadence);
        if (enemy == null || cadence <= 0.01f)
            return delay;

        ReleaseRespawnReservation(enemy);
        PruneExpiredReservations();

        float earliestRespawnTime = Time.time + delay;
        int slotIndex = Mathf.CeilToInt(earliestRespawnTime / cadence);

        if (maxRespawnsPerCadence > 0)
        {
            while (GetReservedCount(slotIndex) >= maxRespawnsPerCadence)
                slotIndex++;
        }

        ReserveRespawnSlot(enemy, slotIndex);

        float snappedRespawnTime = slotIndex * cadence;
        return Mathf.Max(0.5f, snappedRespawnTime - Time.time);
    }

    private void ReserveRespawnSlot(EnemyHealth enemy, int slotIndex)
    {
        reservedRespawnSlots[enemy] = slotIndex;
        slotReservationCounts[slotIndex] = GetReservedCount(slotIndex) + 1;
    }

    private void ReleaseRespawnReservation(EnemyHealth enemy)
    {
        if (ReferenceEquals(enemy, null))
            return;

        if (!reservedRespawnSlots.TryGetValue(enemy, out int slotIndex))
            return;

        reservedRespawnSlots.Remove(enemy);

        if (!slotReservationCounts.TryGetValue(slotIndex, out int reservationCount))
            return;

        reservationCount--;
        if (reservationCount <= 0)
            slotReservationCounts.Remove(slotIndex);
        else
            slotReservationCounts[slotIndex] = reservationCount;
    }

    private int GetReservedCount(int slotIndex)
    {
        return slotReservationCounts.TryGetValue(slotIndex, out int reservationCount)
            ? reservationCount
            : 0;
    }

    private void PruneExpiredReservations()
    {
        if (slotReservationCounts.Count == 0)
            return;

        int currentSlotIndex = Mathf.FloorToInt(Time.time / Mathf.Max(0.01f, respawnCadence)) - 1;
        List<int> expiredSlots = new List<int>();

        foreach (KeyValuePair<int, int> reservation in slotReservationCounts)
        {
            if (reservation.Key <= currentSlotIndex)
                expiredSlots.Add(reservation.Key);
        }

        foreach (int expiredSlot in expiredSlots)
            slotReservationCounts.Remove(expiredSlot);
    }
}

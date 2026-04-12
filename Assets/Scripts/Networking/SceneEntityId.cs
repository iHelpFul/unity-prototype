using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class SceneEntityId : MonoBehaviour
{
    public const string EnemyPrefix = "map01_enemy_";
    public const int EnemyHexLength = 8;

    [SerializeField] private string sceneId = string.Empty;

    public string SceneId => sceneId;

    public bool TryGetRuntimeId(out ulong runtimeId)
    {
        return TryParseEnemyRuntimeId(sceneId, out runtimeId);
    }

    public static bool IsValidEnemySceneId(string candidate)
    {
        return TryParseEnemyRuntimeId(candidate, out _);
    }

    public static bool TryParseEnemyRuntimeId(string candidate, out ulong runtimeId)
    {
        runtimeId = 0UL;

        if (string.IsNullOrEmpty(candidate)
            || !candidate.StartsWith(EnemyPrefix, StringComparison.Ordinal)
            || candidate.Length != EnemyPrefix.Length + EnemyHexLength)
        {
            return false;
        }

        uint parsedId = 0U;
        for (int index = EnemyPrefix.Length; index < candidate.Length; index++)
        {
            int nibble = ParseUpperHexNibble(candidate[index]);
            if (nibble < 0)
                return false;

            parsedId = (parsedId << 4) | (uint)nibble;
        }

        if (parsedId == 0U)
            return false;

        runtimeId = parsedId;
        return true;
    }

    public static string FormatEnemySceneId(uint runtimeId)
    {
        if (runtimeId == 0U)
            throw new ArgumentOutOfRangeException(nameof(runtimeId), "Enemy runtime ID must be non-zero.");

        return $"{EnemyPrefix}{runtimeId:X8}";
    }

#if UNITY_EDITOR
    public void EditorSetSceneId(string value)
    {
        sceneId = value ?? string.Empty;
    }
#endif

    private static int ParseUpperHexNibble(char value)
    {
        if (value >= '0' && value <= '9')
            return value - '0';

        if (value >= 'A' && value <= 'F')
            return value - 'A' + 10;

        return -1;
    }
}

public readonly struct SceneEntityEnemyRegistration
{
    public readonly EnemyHealth Enemy;
    public readonly string SceneId;
    public readonly ulong RuntimeId;

    public SceneEntityEnemyRegistration(EnemyHealth enemy, string sceneId, ulong runtimeId)
    {
        Enemy = enemy;
        SceneId = sceneId;
        RuntimeId = runtimeId;
    }
}

public static class SceneEntityIdValidationUtility
{
    public static int CollectEnemyRegistrations(
        Scene scene,
        List<SceneEntityEnemyRegistration> registrations,
        List<string> errors)
    {
        registrations.Clear();
        errors.Clear();

        if (!scene.IsValid())
            return 0;

        EnemyHealth[] allEnemies = UnityEngine.Object.FindObjectsByType<EnemyHealth>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        Dictionary<string, SceneEntityEnemyRegistration> registrationsBySceneId =
            new Dictionary<string, SceneEntityEnemyRegistration>(StringComparer.Ordinal);
        Dictionary<ulong, SceneEntityEnemyRegistration> registrationsByRuntimeId =
            new Dictionary<ulong, SceneEntityEnemyRegistration>();

        int totalSceneEnemies = 0;

        for (int index = 0; index < allEnemies.Length; index++)
        {
            EnemyHealth enemy = allEnemies[index];
            if (enemy == null || enemy.gameObject.scene.handle != scene.handle)
                continue;

            totalSceneEnemies++;

            if (!TryCreateEnemyRegistration(enemy, out SceneEntityEnemyRegistration registration, out string errorMessage))
            {
                errors.Add(errorMessage);
                continue;
            }

            if (registrationsBySceneId.TryGetValue(registration.SceneId, out SceneEntityEnemyRegistration duplicateBySceneId))
            {
                errors.Add(
                    $"[MultiplayerPrototypeEnemyCoordinator] Duplicate SceneEntityId '{registration.SceneId}' on '{GetHierarchyPath(duplicateBySceneId.Enemy.transform)}' and '{GetHierarchyPath(enemy.transform)}'.");
                continue;
            }

            if (registrationsByRuntimeId.TryGetValue(registration.RuntimeId, out SceneEntityEnemyRegistration duplicateByRuntimeId))
            {
                errors.Add(
                    $"[MultiplayerPrototypeEnemyCoordinator] Duplicate runtime enemy ID 0x{registration.RuntimeId:X8} from '{duplicateByRuntimeId.SceneId}' and '{registration.SceneId}' on '{GetHierarchyPath(duplicateByRuntimeId.Enemy.transform)}' and '{GetHierarchyPath(enemy.transform)}'.");
                continue;
            }

            registrationsBySceneId.Add(registration.SceneId, registration);
            registrationsByRuntimeId.Add(registration.RuntimeId, registration);
            registrations.Add(registration);
        }

        registrations.Sort((left, right) => string.CompareOrdinal(left.SceneId, right.SceneId));
        return totalSceneEnemies;
    }

    public static string GetHierarchyPath(Transform target)
    {
        if (target == null)
            return "<null>";

        List<string> segments = new List<string>(8);
        Transform current = target;
        while (current != null)
        {
            segments.Add(current.name);
            current = current.parent;
        }

        segments.Reverse();
        return string.Join("/", segments);
    }

    private static bool TryCreateEnemyRegistration(
        EnemyHealth enemy,
        out SceneEntityEnemyRegistration registration,
        out string errorMessage)
    {
        registration = default;
        errorMessage = string.Empty;

        if (enemy == null)
        {
            errorMessage = "[MultiplayerPrototypeEnemyCoordinator] Encountered a null enemy during SceneEntityId validation.";
            return false;
        }

        string hierarchyPath = GetHierarchyPath(enemy.transform);
        SceneEntityId entityId = enemy.GetComponent<SceneEntityId>();
        if (entityId == null)
        {
            errorMessage =
                $"[MultiplayerPrototypeEnemyCoordinator] Enemy '{hierarchyPath}' is missing SceneEntityId. Use Tools > Multiplayer Prototype > Scene Entity IDs > Assign Missing Enemy IDs.";
            return false;
        }

        if (string.IsNullOrEmpty(entityId.SceneId))
        {
            errorMessage =
                $"[MultiplayerPrototypeEnemyCoordinator] Enemy '{hierarchyPath}' has an empty SceneEntityId. Expected format '{SceneEntityId.EnemyPrefix}XXXXXXXX'.";
            return false;
        }

        if (!SceneEntityId.TryParseEnemyRuntimeId(entityId.SceneId, out ulong runtimeId))
        {
            errorMessage =
                $"[MultiplayerPrototypeEnemyCoordinator] Enemy '{hierarchyPath}' has invalid SceneEntityId '{entityId.SceneId}'. Expected format '{SceneEntityId.EnemyPrefix}XXXXXXXX' with uppercase hex.";
            return false;
        }

        registration = new SceneEntityEnemyRegistration(enemy, entityId.SceneId, runtimeId);
        return true;
    }
}

using System.Collections.Generic;
using UnityEngine;

public class KillTrackerSystem : MonoBehaviour
{
    private Dictionary<EnemyType, int> killCounts =
        new Dictionary<EnemyType, int>();
    private readonly Dictionary<string, Dictionary<EnemyType, int>> killCountsByCharacter =
        new Dictionary<string, Dictionary<EnemyType, int>>();

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

        string characterKey = ResolveCharacterKey(e.Killer);
        if (!string.IsNullOrWhiteSpace(characterKey))
        {
            EnsureCharacterBucket(characterKey);
            Dictionary<EnemyType, int> characterKills = killCountsByCharacter[characterKey];
            if (!characterKills.ContainsKey(e.Type))
                characterKills[e.Type] = 0;

            characterKills[e.Type]++;
        }

        Debug.Log($"Killed {e.Type}: {killCounts[e.Type]}");
    }

    public int GetKillCount(EnemyType type)
    {
        return killCounts.TryGetValue(type, out int count) ? count : 0;
    }

    public int GetKillCountForCharacter(string characterId, EnemyType type)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return GetKillCount(type);

        string key = characterId.Trim();
        return killCountsByCharacter.TryGetValue(key, out Dictionary<EnemyType, int> characterKills)
            && characterKills != null
            && characterKills.TryGetValue(type, out int count)
            ? count
            : 0;
    }

    private string ResolveCharacterKey(PlayerCharacter player)
    {
        if (player == null || !player.IsLocalPlayer)
            return string.Empty;

        return PlayerRuntimeIdentityUtility.NormalizeCharacterId(player.CharacterId);
    }

    private void EnsureCharacterBucket(string characterKey)
    {
        if (string.IsNullOrWhiteSpace(characterKey))
            return;

        if (!killCountsByCharacter.ContainsKey(characterKey))
            killCountsByCharacter[characterKey] = new Dictionary<EnemyType, int>();
    }
}

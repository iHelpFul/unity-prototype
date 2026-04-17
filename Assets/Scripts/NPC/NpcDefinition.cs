using System.Collections.Generic;
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/NPC/NPC Definition")]
public class NpcDefinition : ScriptableObject
{
    [SerializeField] private string npcId = string.Empty;
    [SerializeField] private string displayName = string.Empty;
    [SerializeField] private Sprite portrait;
    [SerializeField] private List<NpcQuestDefinition> quests = new List<NpcQuestDefinition>();

    public string NpcId => NormalizeId(npcId);
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? NpcId : displayName.Trim();
    public Sprite Portrait => portrait;
    public IReadOnlyList<NpcQuestDefinition> Quests => quests != null
        ? quests
        : Array.Empty<NpcQuestDefinition>();

    private static string NormalizeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim();
    }

    public bool TryGetQuest(string questId, out NpcQuestDefinition quest)
    {
        quest = null;

        if (quests == null || quests.Count == 0)
            return false;

        string normalizedQuestId = NormalizeToken(questId);
        if (string.IsNullOrWhiteSpace(normalizedQuestId))
            return false;

        for (int index = 0; index < quests.Count; index++)
        {
            NpcQuestDefinition candidate = quests[index];
            if (candidate == null)
                continue;

            if (NormalizeToken(candidate.QuestId) == normalizedQuestId
                || NormalizeToken(candidate.name) == normalizedQuestId)
            {
                quest = candidate;
                return true;
            }
        }

        return false;
    }

    public bool HasAnyQuests()
    {
        return Quests.Count > 0;
    }

    private static string NormalizeToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim().Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
    }
}

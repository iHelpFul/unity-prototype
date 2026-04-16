using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(NpcInteractable))]
public class NpcQuestProvider : MonoBehaviour
{
    [SerializeField] private List<NpcQuestDefinition> quests = new List<NpcQuestDefinition>();

    public IReadOnlyList<NpcQuestDefinition> Quests => quests;

    public bool TryGetQuest(string questId, out NpcQuestDefinition quest)
    {
        quest = null;

        if (quests == null)
            return false;

        string normalizedQuestId = string.IsNullOrWhiteSpace(questId) ? string.Empty : questId.Trim();
        for (int index = 0; index < quests.Count; index++)
        {
            NpcQuestDefinition candidate = quests[index];
            if (candidate == null)
                continue;

            if (candidate.QuestId == normalizedQuestId || candidate.name == normalizedQuestId)
            {
                quest = candidate;
                return true;
            }
        }

        return false;
    }

}

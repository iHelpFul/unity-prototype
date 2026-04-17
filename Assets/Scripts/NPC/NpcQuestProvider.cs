using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(NpcInteractable))]
public class NpcQuestProvider : MonoBehaviour
{
    private NpcInteractable interactable;

    public NpcDefinition NpcDefinition => ResolveInteractable() != null ? ResolveInteractable().NpcDefinition : null;
    public IReadOnlyList<NpcQuestDefinition> Quests => NpcDefinition != null ? NpcDefinition.Quests : System.Array.Empty<NpcQuestDefinition>();

    public bool TryGetQuest(string questId, out NpcQuestDefinition quest)
    {
        quest = null;

        if (NpcDefinition == null)
            return false;

        return NpcDefinition.TryGetQuest(questId, out quest);
    }

    private NpcInteractable ResolveInteractable()
    {
        if (interactable == null)
            interactable = GetComponent<NpcInteractable>();

        return interactable;
    }
}

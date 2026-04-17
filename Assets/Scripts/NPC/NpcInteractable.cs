using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NpcInteractable : MonoBehaviour
{
    [SerializeField] private NpcDefinition npcDefinition;
    [SerializeField] private bool requiresInteract = true;
    [SerializeField] private NpcPromptType promptType = NpcPromptType.Shop;
    [SerializeField] private Transform promptAnchor;
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 2.3f, 0f);
    [SerializeField] private string promptActionText = "Press E";
    [SerializeField] private string promptLabelOverride = string.Empty;

    public NpcDefinition NpcDefinition => npcDefinition;
    public NpcPromptType PromptType => promptType;

    private readonly HashSet<PlayerCharacter> overlappingPlayers = new HashSet<PlayerCharacter>();

    public string NpcId => npcDefinition != null
        ? NormalizeId(npcDefinition.NpcId)
        : NormalizeId(gameObject.name);

    public string DisplayName => npcDefinition != null
        ? npcDefinition.DisplayName
        : ResolveFallbackDisplayName();

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void Awake()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void OnEnable()
    {
        EventBus.Subscribe<InteractPressedEvent>(OnInteractPressed);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<InteractPressedEvent>(OnInteractPressed);

        foreach (PlayerCharacter player in overlappingPlayers)
        {
            if (player != null && player.IsLocalPlayer)
                PublishPromptHidden(player);
        }

        overlappingPlayers.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerCharacter player = other.GetComponentInParent<PlayerCharacter>();
        if (!IsEligiblePlayer(player))
            return;

        overlappingPlayers.Add(player);

        PublishPromptShown(player);

        if (!requiresInteract)
            PublishInteractionRequest(player);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerCharacter player = other.GetComponentInParent<PlayerCharacter>();
        if (player != null)
        {
            overlappingPlayers.Remove(player);
            PublishPromptHidden(player);
        }
    }

    private void OnInteractPressed(InteractPressedEvent e)
    {
        if (!requiresInteract)
            return;

        PlayerCharacter overlappingPlayer = ResolveOverlappingPlayer(e.Player, e.CharacterId);
        if (overlappingPlayer == null)
            return;

        PublishInteractionRequest(overlappingPlayer);
    }

    private void PublishInteractionRequest(PlayerCharacter player)
    {
        if (!IsEligiblePlayer(player))
            return;

        EventBus.Publish(new NpcInteractionRequestEvent
        {
            Requester = player,
            CharacterId = player.CharacterId,
            Npc = this
        });
    }

    private bool IsEligiblePlayer(PlayerCharacter player)
    {
        return player != null && player.IsLocalPlayer && !player.IsDead;
    }

    public Vector3 GetPromptWorldPosition()
    {
        Transform anchor = promptAnchor != null ? promptAnchor : transform;
        return anchor.position + promptOffset;
    }

    private void PublishPromptShown(PlayerCharacter player)
    {
        if (!requiresInteract || !IsEligiblePlayer(player))
            return;

        EventBus.Publish(new NpcPromptShownEvent
        {
            Player = player,
            CharacterId = player.CharacterId,
            Npc = this,
            PrimaryText = string.IsNullOrWhiteSpace(promptActionText) ? "Press E" : promptActionText.Trim(),
            SecondaryText = GetPromptSecondaryText()
        });
    }

    private void PublishPromptHidden(PlayerCharacter player)
    {
        if (player == null || !player.IsLocalPlayer)
            return;

        EventBus.Publish(new NpcPromptHiddenEvent
        {
            Player = player,
            CharacterId = player.CharacterId,
            NpcId = NpcId
        });
    }

    private string GetPromptSecondaryText()
    {
        if (!string.IsNullOrWhiteSpace(promptLabelOverride))
            return promptLabelOverride.Trim();

        return promptType switch
        {
            NpcPromptType.Shop => "SHOP",
            NpcPromptType.Job => "JOB",
            NpcPromptType.Quest => "QUEST",
            _ => "TALK"
        };
    }

    private static string NormalizeId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "npc_vendor" : value.Trim();
    }

    private string ResolveFallbackDisplayName()
    {
        return string.IsNullOrWhiteSpace(gameObject.name) ? NpcId : gameObject.name.Trim();
    }

    private PlayerCharacter ResolveOverlappingPlayer(PlayerCharacter player, string characterId)
    {
        if (player != null && overlappingPlayers.Contains(player))
            return player;

        foreach (PlayerCharacter overlappingPlayer in overlappingPlayers)
        {
            if (overlappingPlayer == null)
                continue;

            if (PlayerRuntimeIdentityUtility.MatchesCharacter(
                    overlappingPlayer,
                    overlappingPlayer.CharacterId,
                    player,
                    characterId))
                return overlappingPlayer;
        }

        return null;
    }
}

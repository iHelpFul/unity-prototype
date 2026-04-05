using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PortalTrigger : MonoBehaviour
{
    [SerializeField] private string portalId = "portal";
    [SerializeField] private string targetMapId;
    [SerializeField] private string targetSpawnId = SceneSpawnPoint.DefaultSpawnId;
    [SerializeField] private bool requiresInteract = true;

    private readonly HashSet<PlayerCharacter> overlappingPlayers = new HashSet<PlayerCharacter>();

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
        overlappingPlayers.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerCharacter player = other.GetComponentInParent<PlayerCharacter>();
        if (!IsEligiblePlayer(player))
            return;

        overlappingPlayers.Add(player);

        if (!requiresInteract)
            PublishTransitionRequest(player);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerCharacter player = other.GetComponentInParent<PlayerCharacter>();
        if (player != null)
            overlappingPlayers.Remove(player);
    }

    private void OnInteractPressed(InteractPressedEvent e)
    {
        if (!requiresInteract)
            return;

        PlayerCharacter overlappingPlayer = ResolveOverlappingPlayer(e.Player, e.CharacterId);
        if (overlappingPlayer == null)
            return;

        PublishTransitionRequest(overlappingPlayer);
    }

    private void PublishTransitionRequest(PlayerCharacter player)
    {
        if (!IsEligiblePlayer(player) || string.IsNullOrWhiteSpace(targetMapId))
            return;

        EventBus.Publish(new MapTransitionRequestEvent
        {
            Requester = player,
            CharacterId = player.CharacterId,
            SourcePortalId = portalId,
            TargetMapId = targetMapId,
            TargetSpawnId = targetSpawnId
        });
    }

    private bool IsEligiblePlayer(PlayerCharacter player)
    {
        return player != null && player.IsLocalPlayer && !player.IsDead;
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

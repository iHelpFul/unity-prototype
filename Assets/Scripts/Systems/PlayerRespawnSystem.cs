using System.Collections;
using UnityEngine;

public class PlayerRespawnSystem : MonoBehaviour
{
    [SerializeField] private PlayerCharacter player;
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float respawnDelay = 2f;

    private Vector3 fallbackRespawnPosition;
    private string trackedCharacterId;

    private void Awake()
    {
        ResolveTrackedPlayer();

        if (player != null)
        {
            fallbackRespawnPosition = player.transform.position;
            trackedCharacterId = player.CharacterId;
        }
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
    }

    private void OnPlayerDied(PlayerDiedEvent e)
    {
        ResolveTrackedPlayer();

        if (player == null && e.Target != null && e.Target.IsLocalPlayer)
        {
            player = e.Target;
            fallbackRespawnPosition = player.transform.position;
            trackedCharacterId = player.CharacterId;
        }

        if (!PlayerRuntimeIdentityUtility.MatchesCharacter(
                player,
                trackedCharacterId,
                e.Target,
                e.CharacterId))
            return;

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        ResolveTrackedPlayer();

        if (player == null)
            yield break;

        CharacterController controller = player.GetComponent<CharacterController>();
        Vector3 respawnPosition = respawnPoint != null ? respawnPoint.position : fallbackRespawnPosition;

        if (controller != null)
            controller.enabled = false;

        player.transform.position = respawnPosition;

        if (controller != null)
            controller.enabled = true;

        player.ResetAfterDeath();
    }

    private void ResolveTrackedPlayer()
    {
        if (player != null && player.IsLocalPlayer && player.gameObject.scene.IsValid())
        {
            trackedCharacterId = player.CharacterId;
            return;
        }

        player = WorldRuntimeSceneUtility.FindLocalPlayer(FindObjectsInactive.Include);
        if (player != null)
            trackedCharacterId = player.CharacterId;
    }
}

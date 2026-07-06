using UnityEngine;

public class Prototype3DSceneModeActivator : MonoBehaviour
{
    [SerializeField] private bool activateOnStart = true;
    [SerializeField] private PlayerCharacter player;
    [SerializeField] private bool setPlayerMovementToFull3D = true;
    [SerializeField] private bool setPlayerRotationMode;
    [SerializeField] private PlayerRotationMode playerRotationMode = PlayerRotationMode.MovementDirection;
    [SerializeField] private bool setSceneEnemiesToFull3D = true;

    private void Start()
    {
        if (activateOnStart)
            Activate();
    }

    public void Activate()
    {
        PlayerCharacter resolvedPlayer = ResolvePlayer();

        if (resolvedPlayer != null)
        {
            if (setPlayerMovementToFull3D)
            {
                PlayerMotor motor = resolvedPlayer.GetComponent<PlayerMotor>();
                motor?.SetMovementPlaneMode(PlayerMovementPlaneMode.Full3D);
            }

            if (setPlayerRotationMode)
            {
                PlayerMovementController movementController =
                    resolvedPlayer.GetComponent<PlayerMovementController>();
                movementController?.SetRotationMode(playerRotationMode);
            }
        }

        if (setSceneEnemiesToFull3D)
            ActivateFull3DEnemies();
    }

    private PlayerCharacter ResolvePlayer()
    {
        if (player != null)
            return player;

        PlayerCharacter[] players = FindObjectsByType<PlayerCharacter>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int index = 0; index < players.Length; index++)
        {
            PlayerCharacter candidate = players[index];
            if (candidate != null && candidate.IsLocalPlayer)
                return candidate;
        }

        return players.Length > 0 ? players[0] : null;
    }

    private static void ActivateFull3DEnemies()
    {
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int index = 0; index < enemies.Length; index++)
        {
            EnemyAI enemy = enemies[index];
            if (enemy != null)
                enemy.SetMovementPlaneMode(EnemyMovementPlaneMode.Full3D);
        }
    }
}

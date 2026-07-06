using UnityEngine;

public enum PlayerMovementPlaneMode
{
    LockedZ = 0,
    Full3D = 1
}

[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private PlayerMovementPlaneMode movementPlaneMode = PlayerMovementPlaneMode.LockedZ;

    public bool IsGrounded => controller.isGrounded;
    public PlayerMovementPlaneMode MovementPlaneMode => movementPlaneMode;
    private float lockedZ;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        lockedZ = transform.position.z;
    }

    public void ApplyMovement(Vector3 velocity)
    {
        if (controller == null)
            return;

        controller.Move(velocity * Time.deltaTime);
        if (movementPlaneMode == PlayerMovementPlaneMode.Full3D)
            return;

        Vector3 pos = transform.position;
        pos.z = lockedZ;
        transform.position = pos;
    }

    public void SetMovementPlaneMode(PlayerMovementPlaneMode planeMode, bool recacheLockedZ = true)
    {
        movementPlaneMode = planeMode;

        if (recacheLockedZ)
            lockedZ = transform.position.z;
    }

    public void RecacheLockedZ()
    {
        lockedZ = transform.position.z;
    }
}

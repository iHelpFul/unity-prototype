using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    [SerializeField] private CharacterController controller;

    public bool IsGrounded => controller.isGrounded;
    private float lockedZ;

    private void Awake()
    {
        lockedZ = transform.position.z;
    }
    public void ApplyMovement(Vector3 velocity)
    {
        controller.Move(velocity * Time.deltaTime);
        Vector3 pos = transform.position;
        pos.z = lockedZ;
        transform.position = pos;
    }
}

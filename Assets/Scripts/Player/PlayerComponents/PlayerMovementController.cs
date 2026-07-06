using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerRotationMode
{
    MovementDirection = 0,
    CameraForward = 1,
    None = 2
}

public class PlayerMovementController : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private PlayerRotationMode rotationMode = PlayerRotationMode.MovementDirection;
    [SerializeField, Min(0f)] private float rotationSharpness;

    private PlayerMotor motor;
    private Transform visual;
    private Transform cameraTransform;
    private PlayerCharacter character;
    private PlayerCombatController combatController;

    private PlayerMovementModel movementModel;
    private Vector2 moveInput;
    private Vector3 cameraForward;
    private Vector3 cameraRight;

    public float HorizontalSpeed => movementModel != null ? movementModel.HorizontalSpeed : 0f;
    public float VerticalVelocity => movementModel != null ? movementModel.VerticalVelocity : 0f;
    public bool JumpedThisFrame => movementModel != null && movementModel.JumpedThisFrame;
    public bool IsGrounded => motor != null && motor.IsGrounded;
    public Vector2 CurrentMoveInput => moveInput;
    public PlayerRotationMode RotationMode => rotationMode;

    public void SetRotationMode(PlayerRotationMode newRotationMode)
    {
        rotationMode = newRotationMode;
    }

    public void Initialize(
        PlayerMotor motor,
        Transform visual,
        Transform cameraTransform,
        PlayerCharacter character,
        PlayerCombatController combatController)
    {
        this.motor = motor;
        this.visual = visual != null ? visual : transform;
        this.cameraTransform = cameraTransform;
        this.character = character;
        this.combatController = combatController;
        EnsureMovementModel();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<MoveInputEvent>(OnMove);
        EventBus.Subscribe<JumpPressedEvent>(OnJumpPressed);
        EventBus.Subscribe<JumpReleasedEvent>(OnJumpReleased);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<MoveInputEvent>(OnMove);
        EventBus.Unsubscribe<JumpPressedEvent>(OnJumpPressed);
        EventBus.Unsubscribe<JumpReleasedEvent>(OnJumpReleased);
    }

    public void Tick(float deltaTime)
    {
        EnsureMovementModel();

        UpdateCameraVectors();

        bool canApplyHorizontalMovement = character != null
            && character.ActionStateController != null
            && character.ActionStateController.CanApplyHorizontalMovement
            && !character.IsStunned;

        Vector2 finalInput = canApplyHorizontalMovement ? moveInput : Vector2.zero;

        bool isGrounded = motor != null && motor.IsGrounded;

        movementModel.Tick(
            deltaTime,
            isGrounded,
            finalInput,
            cameraForward,
            cameraRight
        );

        if (motor != null)
            motor.ApplyMovement(movementModel.Velocity);

        if (character != null
            && !character.IsStunned
            && character.ActionStateController != null
            && character.ActionStateController.CanRotateFromMovementInput)
        {
            HandleRotation();
        }
    }

    private void UpdateCameraVectors()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform == null)
        {
            cameraForward = Vector3.forward;
            cameraRight = Vector3.right;
            return;
        }

        cameraForward = cameraTransform.forward;
        cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();
    }

    private void HandleRotation()
    {
        if (visual == null || rotationMode == PlayerRotationMode.None)
            return;

        Vector3 desiredForward = ResolveDesiredRotationDirection();
        desiredForward.y = 0f;

        if (desiredForward.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(desiredForward.normalized, Vector3.up);
        float sharpness = Mathf.Max(0f, rotationSharpness);

        if (sharpness <= 0f)
        {
            visual.rotation = targetRotation;
            return;
        }

        float interpolation = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
        visual.rotation = Quaternion.Slerp(visual.rotation, targetRotation, interpolation);
    }

    private Vector3 ResolveDesiredRotationDirection()
    {
        switch (rotationMode)
        {
            case PlayerRotationMode.CameraForward:
                return cameraForward;

            case PlayerRotationMode.None:
                return Vector3.zero;

            default:
                return cameraForward * moveInput.y + cameraRight * moveInput.x;
        }
    }

    public Vector3 ResolveWorldMoveDirection()
    {
        UpdateCameraVectors();

        Vector3 inputDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;
        inputDirection.y = 0f;
        return inputDirection.sqrMagnitude > 0.0001f ? inputDirection.normalized : Vector3.zero;
    }

    private void OnMove(MoveInputEvent e)
    {
        if (!MatchesInputPlayer(e.Player, e.CharacterId))
            return;

        moveInput = e.Direction;
    }

    private void OnJumpPressed(JumpPressedEvent e)
    {
        if (!MatchesInputPlayer(e.Player, e.CharacterId))
            return;

        if (character == null
            || character.IsStunned
            || character.IsDead
            || character.ActionStateController == null
            || !character.ActionStateController.CanStartJump)
        {
            return;
        }

        EnsureMovementModel();
        movementModel.PressJump();
        combatController?.HandleJumpPressed();
    }

    private void OnJumpReleased(JumpReleasedEvent e)
    {
        if (!MatchesInputPlayer(e.Player, e.CharacterId))
            return;

        EnsureMovementModel();
        movementModel.ReleaseJump();
    }

    private void EnsureMovementModel()
    {
        if (movementModel == null)
            movementModel = new PlayerMovementModel();
    }

    private bool MatchesInputPlayer(PlayerCharacter player, string characterId)
    {
        return character != null
            && PlayerRuntimeIdentityUtility.MatchesCharacter(
                character,
                character.CharacterId,
                player,
                characterId);
    }
}

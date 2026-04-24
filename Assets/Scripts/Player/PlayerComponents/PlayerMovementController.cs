using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementController : MonoBehaviour
{
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
        if (visual == null)
            return;

        Vector3 inputDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;
        inputDirection.y = 0f;

        if (inputDirection.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(inputDirection);
        visual.rotation = targetRotation;
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

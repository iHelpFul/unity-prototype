using UnityEngine;

public class PlayerMovementModel
{
    private float gravity = -20f;
    private float fallMultiplier = 1.5f;
    private float lowJumpMultiplier = 2f;

    private float maxGroundSpeed = 4.25f;
    private float groundAcceleration = 18f;
    private float groundDeceleration = 22f;
    private float airAcceleration = 17.5f;

    private float jumpHeight = 3f;

    private float coyoteTime = 0.15f;
    private float jumpBufferTime = 0.15f;

    private float verticalVelocity;
    private Vector3 horizontalVelocity;

    private float coyoteCounter;
    private float jumpBufferCounter;

    private bool jumpHeld;

    public Vector3 Velocity => horizontalVelocity + Vector3.up * verticalVelocity;

    public bool JumpedThisFrame { get; private set; }

    public float VerticalVelocity => verticalVelocity;

    public float HorizontalSpeed
    {
        get
        {
            Vector3 v = horizontalVelocity;
            v.y = 0f;
            return v.magnitude;
        }
    }


    public void Tick(float deltaTime, bool isGrounded, Vector2 input, Vector3 forward, Vector3 right)
    {
        JumpedThisFrame = false;
        HandleTimers(deltaTime, isGrounded);
        HandleHorizontal(deltaTime, input, isGrounded, forward, right);
        HandleJump(isGrounded);
        ApplyGravity(deltaTime, isGrounded);
    }

    public void PressJump()
    {
        jumpBufferCounter = jumpBufferTime;
        jumpHeld = true;
    }

    public void ReleaseJump()
    {
        jumpHeld = false;
    }

    public void ResetHorizontalVelocity()
    {
        horizontalVelocity = Vector3.zero;
    }

    private void HandleTimers(float dt, bool isGrounded)
    {
        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= dt;

        jumpBufferCounter -= dt;
    }

    private void HandleJump(bool isGrounded)
    {
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            verticalVelocity = Mathf.Sqrt(2f * -gravity * jumpHeight);
            JumpedThisFrame = true;
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }
    }

    private void ApplyGravity(float dt, bool isGrounded)
    {
        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        float currentGravity = gravity;

        if (verticalVelocity < 0f)
            currentGravity *= fallMultiplier;
        else if (verticalVelocity > 0f && !jumpHeld)
            currentGravity *= lowJumpMultiplier;

        verticalVelocity += currentGravity * dt;
    }

    private void HandleHorizontal(float dt, Vector2 input, bool isGrounded, Vector3 forward, Vector3 right)
    {
        Vector3 targetDir = (forward * input.y + right * input.x).normalized;
        Vector3 targetVelocity = targetDir * maxGroundSpeed;

        float accel = isGrounded ? groundAcceleration : airAcceleration;
        float decel = groundDeceleration;

        if (input.magnitude > 0.1f)
        {
            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                targetVelocity,
                accel * dt
            );
        }
        else if (isGrounded)
        {
            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                Vector3.zero,
                decel * dt
            );
        }
    }
}

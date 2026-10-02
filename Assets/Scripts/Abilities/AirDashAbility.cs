using UnityEngine;

public class AirDashAbility : PlayerAbility
{
    private readonly AirDashAbilityData airDashData;

    private bool isDashing;
    private float dashTimer;
    private float cooldownTimer;

    private Vector3 dashDirection;
    private float dashSpeed;


    public AirDashAbility(
        PlayerAbilityContext context,
        AirDashAbilityData data)
        : base(context, data)
    {
        airDashData = data;
    }


    // ============================================================
    // INPUT
    // ============================================================

    public override bool CanActivate()
    {
        if (isDashing)
            return false;

        if (cooldownTimer > 0f)
            return false;

        // Air Dash only works while airborne.
        if (Context.IsGrounded)
            return false;

        return true;
    }

    public override bool TryActivate()
    {
        StartDash();

        return true;
    }


    // ============================================================
    // UPDATE
    // ============================================================

    public override void OnUpdate()
    {
        UpdateCooldown();
        UpdateDash();
    }


    // ============================================================
    // LIFECYCLE
    // ============================================================

    public override void OnRemoved()
    {
        if (isDashing)
        {
            EndDash();
        }
    }


    // ============================================================
    // DASH
    // ============================================================

    private void StartDash()
    {
        dashDirection = GetDashDirection();

        dashSpeed =
            airDashData.DashDistance /
            airDashData.DashDuration;

        dashTimer =
            airDashData.DashDuration;

        isDashing = true;

        Context.SetGravityOverride(true);

        Context.SetMovementOverride(
            dashDirection * dashSpeed
        );

        Debug.Log("AIR DASH START");
    }


    private void UpdateDash()
    {
        if (!isDashing)
            return;

        dashTimer -= Time.deltaTime;

        if (dashTimer <= 0f)
        {
            EndDash();
            return;
        }

        Context.SetMovementOverride(
            dashDirection * dashSpeed
        );
    }


    private void EndDash()
    {
        isDashing = false;
        dashTimer = 0f;

        Context.ClearMovementOverride();
        Context.SetGravityOverride(false);

        cooldownTimer =
            airDashData.Cooldown;

        Debug.Log("AIR DASH END");
    }


    // ============================================================
    // COOLDOWN
    // ============================================================

    private void UpdateCooldown()
    {
        if (cooldownTimer <= 0f)
            return;

        cooldownTimer -= Time.deltaTime;

        if (cooldownTimer < 0f)
        {
            cooldownTimer = 0f;
        }
    }


    // ============================================================
    // DIRECTION
    // ============================================================

    private Vector3 GetDashDirection()
    {
        Vector2 movementInput =
        Context.MovementInput;

        // No movement input = camera-forward dash.
        if (movementInput.sqrMagnitude <= 0.001f)
        {
            movementInput = Vector2.up;
        }

        //return Context.GetCameraRelativeDirection3D(
        //    movementInput
        //);

        // DASH FORWARD TOWARD THE CAMERAS FACING
        return Context.CameraForward;
    }
}
using UnityEngine;

public class QuickStepAbility : PlayerAbility
{
    private readonly QuickStepAbilityData quickStepData;

    private bool isQuickStepping;
    private float quickStepTimer;
    private float cooldownTimer;

    private Vector3 quickStepDirection;
    private float quickStepSpeed;


    public QuickStepAbility(
        PlayerAbilityContext context,
        QuickStepAbilityData data)
        : base(context, data)
    {
        quickStepData = data;
    }


    // ============================================================
    // INPUT
    // ============================================================


    public override bool CanActivate()
    {
        if (isQuickStepping)
            return false;

        if (cooldownTimer > 0f)
            return false;

        if (!Context.IsGrounded)
            return false;


        return true;
    }


    public override bool TryActivate()
    {
        StartQuickStep();

        return true;
    }


    // ============================================================
    // UPDATE
    // ============================================================

    public override void OnUpdate()
    {
        UpdateCooldown();
        UpdateQuickStep();
    }


    // ============================================================
    // LIFECYCLE
    // ============================================================

    public override void OnRemoved()
    {
        if (isQuickStepping)
        {
            EndQuickStep();
        }
    }


    // ============================================================
    // DASH
    // ============================================================

    private void StartQuickStep()
    {
        quickStepDirection = GetQuickStepDirection();

        quickStepSpeed =
            quickStepData.QuickStepDistance /
            quickStepData.QuickStepDuration;

        quickStepTimer = quickStepData.QuickStepDuration;

        isQuickStepping = true;

        Context.SetMovementOverride(
            quickStepDirection * quickStepSpeed
        );

        Debug.Log("QUICKSTEP START");
    }


    private void UpdateQuickStep()
    {
        if (!isQuickStepping)
            return;

        quickStepTimer -= Time.deltaTime;

        if (quickStepTimer <= 0f)
        {
            EndQuickStep();
            return;
        }

        Context.SetMovementOverride(
            quickStepDirection * quickStepSpeed
        );
    }


    private void EndQuickStep()
    {
        isQuickStepping = false;
        quickStepTimer = 0f;

        Context.ClearMovementOverride();

        cooldownTimer = quickStepData.Cooldown;

        Debug.Log("QUICKSTEP END");
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

    private Vector3 GetQuickStepDirection()
    {
        Vector2 movementInput =
            Context.MovementInput;

        // No movement input = camera-forward dash.
        if (movementInput.sqrMagnitude <= 0.001f)
        {
            movementInput = Vector2.up;
        }

        return Context.GetCameraRelativeDirection(
            movementInput
        );
    }
}
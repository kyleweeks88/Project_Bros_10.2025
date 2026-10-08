using UnityEngine;

public class ExtraJumpAbility : PlayerAbility
{
    private readonly ExtraJumpAbilityData extraJumpData;

    private int remainingJumps;

    // ============================================================
    // PROPERTIES
    // ============================================================

    public int RemainingJumps =>
        remainingJumps;

    public int MaxExtraJumps =>
        extraJumpData.ExtraJumps;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public ExtraJumpAbility(
        PlayerAbilityContext context,
        ExtraJumpAbilityData data)
        : base(context, data)
    {
        extraJumpData = data;
    }


    // ============================================================
    // LIFECYCLE
    // ============================================================

    public override void OnAdded()
    {
        ResetJumps();
    }


    public override void OnGrounded()
    {
        Debug.Log("Extra jump grounded");
        ResetJumps();
    }


    // ============================================================
    // INPUT
    // ============================================================

    public override bool CanActivate()
    {
        // Normal ground jump belongs to PlayerLocomotion.
        if (Context.IsGrounded)
            return false;

        // No extra jumps remaining
        if (remainingJumps <= 0)
            return false;

        return true;
    }

    public override bool TryActivate()
    {
        if (!CanActivate())
            return false;

        Context.PerformExtraJump();
        remainingJumps--;

        return true;
    }


    // ============================================================
    // JUMPS
    // ============================================================

    private void ResetJumps()
    {
        remainingJumps = extraJumpData.ExtraJumps;
        Debug.Log($"{remainingJumps} reset to " +
            $"{extraJumpData.ExtraJumps}"
            );
    }
}
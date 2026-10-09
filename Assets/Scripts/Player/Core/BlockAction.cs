using UnityEngine;

public class BlockAction : PlayerAction
{
    private const float BlockingMovementSpeedMultiplier = 0.5f;

    public override PlayerActionType ActionType =>
        PlayerActionType.Block;

    public BlockAction(PlayerActionContext context)
        : base(context)
    {
    }

    public override void OnStarted()
    {
        IsComplete = false;

        Context.Locomotion.SetMovementSpeedMultiplier(
            BlockingMovementSpeedMultiplier);
    }

    public override void OnUpdate()
    {
        bool bothHeavyButtonsHeld =
            Context.Input.HeavyAttack1Held &&
            Context.Input.HeavyAttack2Held;

        if (!bothHeavyButtonsHeld ||
            !Context.Locomotion.IsGrounded)
        {
            IsComplete = true;
        }
    }

    public override void OnEnded()
    {
        Context.Locomotion.SetMovementSpeedMultiplier(1f);
    }
}

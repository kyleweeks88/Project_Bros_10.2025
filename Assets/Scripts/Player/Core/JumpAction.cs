using UnityEngine;
using System;

public class JumpAction : PlayerAction
{
    private readonly Func<bool> performJump;

    public override PlayerActionType ActionType =>
        PlayerActionType.Jump;

    public JumpAction(
        PlayerActionContext context,
        Func<bool> performJump)
        : base(context)
    { 
        this.performJump = performJump;
    }

    public override void OnStarted()
    {
        IsComplete =
            performJump == null || !performJump();
    }

    public override void OnUpdate()
    {
        // Jump is a brief action representing the launch,
        // not the whole time the player is airborne.
        IsComplete = true;
    }
}

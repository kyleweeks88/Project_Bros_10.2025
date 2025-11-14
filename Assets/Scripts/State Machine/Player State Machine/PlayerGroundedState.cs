using UnityEngine;

// ROOT STATE
public class PlayerGroundedState : PlayerBaseState
{
    public PlayerGroundedState(PlayerStateMachine _currentContext, PlayerStateFactory _playerStateFactory) 
    : base (_currentContext, _playerStateFactory) 
    {
        bs_isRootState = true;
        InitializeSubState();
    }

    public override void EnterState()
    {
    }

    public override void InitializeSubState()
    {
        if (!bs_Ctx.sm_isMovementPressed 
            && !bs_Ctx.sm_PlayerInputHandler.SprintPressed
            && !bs_Ctx.sm_PlayerInputHandler.CrouchPressed)
        {
            SetSubState(bs_Factory.Idle());
        }
        else if (bs_Ctx.sm_isMovementPressed 
            && !bs_Ctx.sm_PlayerInputHandler.SprintPressed
            && !bs_Ctx.sm_PlayerInputHandler.CrouchPressed)
        {
            SetSubState(bs_Factory.Walk());
        }
        else if(bs_Ctx.sm_isMovementPressed
            && bs_Ctx.sm_PlayerInputHandler.SprintPressed
            && !bs_Ctx.sm_PlayerInputHandler.CrouchPressed)
        {
            SetSubState(bs_Factory.Run());
        }
        else if (bs_Ctx.sm_isMovementPressed
            && !bs_Ctx.sm_PlayerInputHandler.SprintPressed
            && bs_Ctx.sm_PlayerInputHandler.CrouchPressed)
        {
            SetSubState(bs_Factory.Crouch());
        }
    }

    public override void UpdateState()
    {
        bs_Ctx.sm_currentMovementY = -0.5f;
        CheckSwitchStates();

        Debug.Log("GROUNDED STATE");
    }

    public override void CheckSwitchStates()
    {
        // IF A PLAYER PRESSES JUMP BUTTON
        if(bs_Ctx.sm_PlayerInputHandler.JumpPressed && !bs_Ctx.sm_requireNewJumpPress)
        {
            SwitchState(bs_Factory.Jump());
        }

        // IF A PLAYER FALLS
        if(bs_Ctx.sm_isFalling)
        {
            SwitchState(bs_Factory.Fall());
        }

        if (bs_Ctx.sm_isClimbing)
        {
            SwitchState(bs_Factory.Climb());
        }
    }

    public override void ExitState()
    {
    }
}

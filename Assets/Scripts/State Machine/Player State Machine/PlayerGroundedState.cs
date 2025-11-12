using UnityEngine;

public class PlayerGroundedState : PlayerBaseState
{
    public PlayerGroundedState(PlayerStateMachine _currentContext, PlayerStateFactory _playerStateFactory) 
    : base (_currentContext, _playerStateFactory) { }

    public override void EnterState()
    {
        Debug.Log("GROUNDED STATE");
    }

    public override void InitializeSubState()
    {
        //throw new System.NotImplementedException();
    }

    public override void UpdateState()
    {
        ctx.m_currentMovementY = -0.5f;
        CheckSwitchStates();
    }

    public override void CheckSwitchStates()
    {
        // IF A PLAYER PRESSES JUMP
        if(ctx.m_PlayerInputHandler.JumpPressed && !ctx.m_requireNewJumpPress)
        {
            SwitchState(factory.Jump());
        }

        // IF A PLAYER FALLS
        if(ctx.m_isFalling)
        {
            SwitchState(factory.Fall());
        }
    }

    public override void ExitState()
    {
        Debug.Log("Exit Grounded");
    }
}

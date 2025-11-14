using UnityEngine;
using System;

// SUB STATE
public class PlayerIdleState : PlayerBaseState
{
    public PlayerIdleState(PlayerStateMachine _currentContext, PlayerStateFactory _playerStateFactory)
    : base(_currentContext, _playerStateFactory) { }

    public override void EnterState()
    {
    }

    public override void InitializeSubState()
    {
    }

    public override void UpdateState()
    {
        bs_Ctx.sm_targetSpeed = 0f;
        CheckSwitchStates();

        Debug.Log("TEST: IDLE STATE");
    }

    public override void CheckSwitchStates()
    {
        if (bs_Ctx.sm_PlayerInputHandler.CrouchPressed)
        {
            SwitchState(bs_Factory.Crouch());
        }
        else if (bs_Ctx.sm_isMovementPressed 
            && bs_Ctx.sm_PlayerInputHandler.SprintPressed)
        {
            SwitchState(bs_Factory.Run());
        }
        else if (bs_Ctx.sm_isMovementPressed)
        {
            SwitchState(bs_Factory.Walk());
        }
    }

    public override void ExitState()
    {
    }
}

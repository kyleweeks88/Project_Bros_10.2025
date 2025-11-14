using UnityEngine;

// SUB STATE
public class PlayerWalkState : PlayerBaseState
{
    public PlayerWalkState(PlayerStateMachine _currentContext, PlayerStateFactory _playerStateFactory)
    : base(_currentContext, _playerStateFactory) { }

    public override void EnterState()
    {
        //ctx.m_Animator.SetBool(ctx.m_isWalkingHash, true);
        //ctx.m_Animator.SetBool(ctx.m_isSprintingHash, false);
    }

    public override void InitializeSubState()
    {
    }

    public override void UpdateState()
    {
        bs_Ctx.sm_targetSpeed = bs_Ctx.sm_moveSpeed;
        CheckSwitchStates();

        Debug.Log("TEST: WALK STATE");
    }

    public override void CheckSwitchStates()
    {
        if (!bs_Ctx.sm_isMovementPressed)
        {
            SwitchState(bs_Factory.Idle());
        }
        else if (bs_Ctx.sm_isMovementPressed 
            && bs_Ctx.sm_PlayerInputHandler.SprintPressed)
        {
            SwitchState(bs_Factory.Run());
        }
        else if (bs_Ctx.sm_isMovementPressed
            && bs_Ctx.sm_PlayerInputHandler.CrouchPressed)
        {
            SwitchState(bs_Factory.Crouch());
        }
    }

    public override void ExitState()
    {
    }
}

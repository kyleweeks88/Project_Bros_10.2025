using UnityEngine;

// SUB STATE
public class PlayerRunState : PlayerBaseState
{
    public PlayerRunState(PlayerStateMachine _currentContext, PlayerStateFactory _playerStateFactory)
    : base(_currentContext, _playerStateFactory) { }

    public override void EnterState()
    {
        bs_Ctx.sm_Animator.SetBool(bs_Ctx.sm_isWalkingHash, false);
        bs_Ctx.sm_Animator.SetBool(bs_Ctx.sm_isSprintingHash, true);

        bs_Ctx.sm_isSprinting = true;
    }

    public override void InitializeSubState()
    {
        //throw new System.NotImplementedException();
    }

    public override void UpdateState()
    {
        bs_Ctx.sm_targetSpeed = bs_Ctx.sm_moveSpeed * bs_Ctx.sm_sprintMultiplier;

        CheckSwitchStates();
        Debug.Log("TEST: SPRINT STATE");
    }

    public override void CheckSwitchStates()
    {
        if (!bs_Ctx.sm_isMovementPressed)
        {
            SwitchState(bs_Factory.Idle());
        }
        else if (bs_Ctx.sm_isMovementPressed && !bs_Ctx.sm_PlayerInputHandler.SprintPressed)
        {
            SwitchState(bs_Factory.Walk());
        }
        else if(bs_Ctx.sm_PlayerInputHandler.CrouchPressed)
        {
            SwitchState(bs_Factory.Crouch());
        }
    }

    public override void ExitState()
    {
        bs_Ctx.sm_isSprinting = false;
        bs_Ctx.sm_targetSpeed = bs_Ctx.sm_moveSpeed;
    }
}

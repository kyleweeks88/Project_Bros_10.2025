using UnityEngine;

// ROOT STATE
public class PlayerClimbState : PlayerBaseState
{
    Vector3 climbDirection;

    public PlayerClimbState(PlayerStateMachine _currentContext, PlayerStateFactory _playerStateFactory)
    : base(_currentContext, _playerStateFactory) 
    {
        bs_isRootState = true;
        InitializeSubState();
    }


    public override void EnterState()
    {
        //climbDirection = bs_Ctx.CalculateWorldDirection(new Vector3(0, bs_Ctx.sm_PlayerInputHandler.MovementInput.y, 0));
    }

    public override void InitializeSubState()
    {
    }

    public override void UpdateState()
    {
        Vector3 climb = bs_Ctx.CalculateWorldDirection(new Vector3(0, bs_Ctx.sm_PlayerInputHandler.MovementInput.y, 0));
        bs_Ctx.sm_currentMovementX = climb.x;
        bs_Ctx.sm_currentMovementY = climb.y * bs_Ctx.sm_moveSpeed;

        CheckSwitchStates();

        Debug.Log("TEST: CLIMB STATE");
    }

    public override void CheckSwitchStates()
    {
        if(!bs_Ctx.sm_isClimbing)
        {
            if (bs_Ctx.sm_characterController.isGrounded)
            {
                SwitchState(bs_Factory.Grounded());
            }
            else if(!bs_Ctx.sm_characterController.isGrounded)
            {
                SwitchState(bs_Factory.Fall());
            }
        }
    }

    public override void ExitState()
    {
    }
}

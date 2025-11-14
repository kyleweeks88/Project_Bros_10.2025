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
        climbDirection = bs_Ctx.CalculateWorldDirection(new Vector3(0, bs_Ctx.sm_PlayerInputHandler.MovementInput.y, 0));
    }

    public override void InitializeSubState()
    {
    }

    public override void UpdateState()
    {
        bs_Ctx.sm_currentMovementX = climbDirection.x;
        bs_Ctx.sm_currentMovementY = climbDirection.y * bs_Ctx.sm_moveSpeed;

        CheckSwitchStates();

        Debug.Log("TEST: CLIMB STATE");
    }

    public override void CheckSwitchStates()
    {
        if(!bs_Ctx.sm_isClimbing)
        {
            ExitState();
            Debug.Log("TEST");
        }
    }

    public override void ExitState()
    {
    }
}

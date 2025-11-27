using UnityEngine;

// bs_ IS GETTER/SETTER FROM THE BASE STATE AND PASSED TO THIS CONCRETE STATE
// sm_ IS GETTER/SETTER FROM THE STATE MACHINE
public class PlayerJumpState : PlayerBaseState
{
    public PlayerJumpState(PlayerStateMachine _currentContext, PlayerStateFactory _playerStateFactory)
    : base(_currentContext, _playerStateFactory) 
    {
        bs_isRootState = true;
        InitializeSubState();
    }

    public override void EnterState()
    {
        HandleJumping();
    }

    public override void InitializeSubState()
    {
        //throw new System.NotImplementedException();
    }

    public override void UpdateState()
    {
        CheckSwitchStates();
        Debug.Log("JUMPING STATE");
    }

    public override void CheckSwitchStates()
    {
        if(bs_Ctx.sm_characterController.isGrounded)
        {
            SwitchState(bs_Factory.Grounded());
        }

        // IF A PLAYER FALLS
        if (bs_Ctx.sm_isFalling)
        {
            SwitchState(bs_Factory.Fall());
        }
    }

    public override void ExitState()
    {
        bs_Ctx.sm_Animator.SetBool(bs_Ctx.sm_isJumpingHash, false);
        if(bs_Ctx.sm_PlayerInputHandler.JumpPressed)
        {
            bs_Ctx.sm_requireNewJumpPress = true;
        }
    }

    private void HandleJumping()
    {
        bs_Ctx.sm_Animator.SetBool(bs_Ctx.sm_isJumpingHash, true);
        bs_Ctx.sm_isJumping = true;
        bs_Ctx.sm_currentMovementY = bs_Ctx.sm_initialJumpVelocity * 0.5f;
    }
}

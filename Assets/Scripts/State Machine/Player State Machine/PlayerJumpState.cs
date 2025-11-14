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

    public override void EnterState()
    {
        Debug.Log("JUMPING STATE");
        HandleJumping();
    }

    public override void ExitState()
    {
        bs_Ctx.sm_Animator.SetBool(bs_Ctx.sm_isJumpingHash, false);
        if(bs_Ctx.sm_PlayerInputHandler.JumpPressed)
        {
            bs_Ctx.sm_requireNewJumpPress = true;
        }
    }

    public override void InitializeSubState()
    {
        //throw new System.NotImplementedException();
    }

    public override void UpdateState()
    {
        CheckSwitchStates();
        //HandleGravity();
    }

    private void HandleJumping()
    {
        bs_Ctx.sm_Animator.SetBool(bs_Ctx.sm_isJumpingHash, true);
        bs_Ctx.sm_isJumping = true;
        bs_Ctx.sm_currentMovementY = bs_Ctx.sm_initialJumpVelocity * 0.5f;
    }

    //private void HandleGravity()
    //{
    //    ctx.m_isFalling = ctx.m_currentMovementY <= 0.0f || !ctx.m_PlayerInputHandler.JumpPressed;
    //    float fallMultiplier = 2f;

    //    if (ctx.m_isFalling)
    //    {
    //        float previousYVelocity = ctx.m_currentMovementY;
    //        float newYVelocity = ctx.m_currentMovementY + (ctx.m_gravity * fallMultiplier * Time.deltaTime);
    //        float nextYVelocity = Mathf.Max((previousYVelocity + newYVelocity) * 0.5f, -10f);
    //        ctx.m_currentMovementY = nextYVelocity;
    //        Debug.Log("BUG");
    //    }
    //    else
    //    {
    //        float previousYVelocity = ctx.m_currentMovementY;
    //        float newYVelocity = ctx.m_currentMovementY + (ctx.m_gravity * Time.deltaTime);
    //        float nextYVelocity = (previousYVelocity + newYVelocity) * 0.5f;
    //        ctx.m_currentMovementY = nextYVelocity;
    //    }
    //}
}

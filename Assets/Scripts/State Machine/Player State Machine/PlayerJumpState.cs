using UnityEngine;

public class PlayerJumpState : PlayerBaseState
{
    public PlayerJumpState(PlayerStateMachine _currentContext, PlayerStateFactory _playerStateFactory)
    : base(_currentContext, _playerStateFactory) { }

    public override void CheckSwitchStates()
    {
        if(ctx.m_characterController.isGrounded)
        {
            SwitchState(factory.Grounded());
        }

        // IF A PLAYER FALLS
        if (ctx.m_isFalling)
        {
            SwitchState(factory.Fall());
        }
    }

    public override void EnterState()
    {
        Debug.Log("JUMPING STATE");
        HandleJumping();
    }

    public override void ExitState()
    {
        ctx.m_Animator.SetBool(ctx.m_isJumpingHash, false);
        if(ctx.m_PlayerInputHandler.JumpPressed)
        {
            ctx.m_requireNewJumpPress = true;
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
        ctx.m_Animator.SetBool(ctx.m_isJumpingHash, true);
        ctx.m_isJumping = true;
        ctx.m_currentMovementY = ctx.m_initialJumpVelocity * 0.5f;
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

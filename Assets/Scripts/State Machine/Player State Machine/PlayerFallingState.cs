using UnityEngine;

// ROOT STATE
public class PlayerFallingState : PlayerBaseState
{
    public PlayerFallingState(PlayerStateMachine _currentContext, PlayerStateFactory _playerStateFactory)
:   base(_currentContext, _playerStateFactory) 
    {
        bs_isRootState = true;
        InitializeSubState();
    }

    public override void EnterState()
    {
        HandleAnimation(true);
    }

    public override void InitializeSubState()
    {
    }

    public override void UpdateState()
    {
        HandleFalling();

        CheckSwitchStates();

        Debug.Log("TEST: FALLING STATE");
    }

    public override void CheckSwitchStates()
    {
        if (bs_Ctx.sm_characterController.isGrounded)
        {
            SwitchState(bs_Factory.Grounded());
        }
    }

    public override void ExitState()
    {
        HandleAnimation(false);
    }

    void HandleFalling()
    {
        float fallMultiplier = 2f;

        float previousYVelocity = bs_Ctx.sm_currentMovementY;
        float newYVelocity = bs_Ctx.sm_currentMovementY + (bs_Ctx.sm_gravity * fallMultiplier * Time.deltaTime);
        float nextYVelocity = Mathf.Max((previousYVelocity + newYVelocity) * 0.5f, -10f);
        bs_Ctx.sm_currentMovementY = nextYVelocity;
    }

    void HandleAnimation(bool _animBool)
    {
        bs_Ctx.sm_Animator.SetBool(bs_Ctx.sm_isFallingHash, _animBool);
    }
}

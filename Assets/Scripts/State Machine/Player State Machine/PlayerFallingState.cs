using UnityEngine;

public class PlayerFallingState : PlayerBaseState
{
    public PlayerFallingState(PlayerStateMachine _currentContext, PlayerStateFactory _playerStateFactory)
:   base(_currentContext, _playerStateFactory) { }

    public override void EnterState()
    {
        
    }

    public override void InitializeSubState()
    {
    }

    public override void UpdateState()
    {
        float fallMultiplier = 2f;

        float previousYVelocity = ctx.m_currentMovementY;
        float newYVelocity = ctx.m_currentMovementY + (ctx.m_gravity * fallMultiplier * Time.deltaTime);
        float nextYVelocity = Mathf.Max((previousYVelocity + newYVelocity) * 0.5f, -10f);
        ctx.m_currentMovementY = nextYVelocity;
        Debug.Log("TEST: FALLING STATE");

        CheckSwitchStates();
    }

    public override void CheckSwitchStates()
    {
        if (ctx.m_characterController.isGrounded)
        {
            SwitchState(factory.Grounded());
        }
    }

    public override void ExitState()
    {
        //ctx.m_currentMovementY = 0f;
    }
}

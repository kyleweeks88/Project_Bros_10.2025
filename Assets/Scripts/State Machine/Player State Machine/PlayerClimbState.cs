using UnityEngine;

public class PlayerClimbState : PlayerBaseState
{
    public PlayerClimbState(PlayerStateMachine _currentContext, PlayerStateFactory _playerStateFactory)
    : base(_currentContext, _playerStateFactory) { }

    public override void CheckSwitchStates()
    {
        throw new System.NotImplementedException();
    }

    public override void EnterState()
    {
        //    Vector3 worldDirection = CalculateWorldDirection(new Vector3(0, playerInputHandler.MovementInput.y, 0));
        //    currentMovement.x = worldDirection.x;
        //    currentMovement.y = worldDirection.y * currentSpeed * 2f;
    }

    public override void ExitState()
    {
        throw new System.NotImplementedException();
    }

    public override void InitializeSubState()
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateState()
    {
        throw new System.NotImplementedException();
    }
}

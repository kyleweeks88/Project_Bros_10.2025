using UnityEngine;
using System;

public class PlayerIdleState : PlayerBaseState
{
    public PlayerIdleState(PlayerStateMachine _currentContext, PlayerStateFactory _playerStateFactory)
    : base(_currentContext, _playerStateFactory) { }

    public override void CheckSwitchStates()
    {
        throw new NotImplementedException();
    }

    public override void EnterState()
    {
        throw new NotImplementedException();
    }

    public override void ExitState()
    {
        throw new NotImplementedException();
    }

    public override void InitializeSubState()
    {
        throw new NotImplementedException();
    }

    public override void UpdateState()
    {
        throw new NotImplementedException();
    }
}

public abstract class PlayerBaseState
{
    protected PlayerStateMachine ctx;
    protected PlayerStateFactory factory;
    public PlayerBaseState(PlayerStateMachine _currentContext, PlayerStateFactory _playerStateFactory)
    {
        ctx = _currentContext;
        factory = _playerStateFactory;
    }

    public abstract void EnterState();
    public abstract void UpdateState();
    public abstract void ExitState();
    public abstract void CheckSwitchStates();
    public abstract void InitializeSubState();

    void UpdateStates() { }
    protected void SwitchState(PlayerBaseState _newState) {
        // CURRENT STATE EXITS STATE
        ExitState();

        // NEW STATE ENTERS STATE
        _newState.EnterState();

        // SWITCH CURRENT STATE OF CONTEXT
        ctx.CurrentState = _newState;
    }
    protected void SetSuperState() { }
    protected void SetSubState() { }
}

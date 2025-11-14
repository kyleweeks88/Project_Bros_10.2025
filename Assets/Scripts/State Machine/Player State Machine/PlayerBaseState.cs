public abstract class PlayerBaseState
{
    private bool isRootState = false;
    private PlayerStateMachine ctx;
    private PlayerStateFactory factory;
    private PlayerBaseState currentSubState;
    private PlayerBaseState currentSuperState;

    protected bool bs_isRootState { set { isRootState = value; } }
    protected PlayerStateMachine bs_Ctx { get { return ctx; } }
    protected PlayerStateFactory bs_Factory { get { return factory; } }

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

    public void UpdateStates() 
    {
        UpdateState();
        if(currentSubState != null)
        {
            currentSubState.UpdateStates();
        }
    }

    protected void SwitchState(PlayerBaseState _newState) {
        // CURRENT STATE EXITS STATE
        ExitState();

        // NEW STATE ENTERS STATE
        _newState.EnterState();

        // IF THIS STATE IS A ROOTSTATE
        if (isRootState)
        {
            // SWITCH CURRENT STATE OF CONTEXT
            ctx.CurrentState = _newState;
        }
        // IF NOT A ROOTSTATE, THEN ITS A SUBSTATE
        // IF THE SUBSTATE HAS A CURRENT SUPERSTATE
        else if(currentSuperState != null)
        {
            // SET THE CURRENT SUPERSTATE'S SUBSTATE TO THE _newState
            currentSuperState.SetSubState(_newState);
        }
    }
    protected void SetSuperState(PlayerBaseState _newSuperState) {
        currentSuperState = _newSuperState;
    }
    protected void SetSubState(PlayerBaseState _newSubState) {
        currentSubState = _newSubState;
        _newSubState.SetSuperState(this);
    }
}

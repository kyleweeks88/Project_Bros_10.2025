
public class PlayerStateFactory
{
    PlayerStateMachine context;

    public PlayerStateFactory(PlayerStateMachine _currentContext)
    {
        context = _currentContext;
    }

    public PlayerBaseState Idle() {
        return new PlayerIdleState(context, this);
    }
    public PlayerBaseState Walk() {
        return new PlayerWalkState(context, this);
    }
    public PlayerBaseState Run() {
        return new PlayerRunState(context, this);
    }
    public PlayerBaseState Crouch() {
        return new PlayerCrouchState(context, this);
    }
    public PlayerBaseState Jump() {
        return new PlayerJumpState(context, this);
    }
    public PlayerBaseState Grounded()
    {
        return new PlayerGroundedState(context, this);
    }
    public PlayerBaseState Climb() { 
        return new PlayerClimbState(context, this);
    }
    public PlayerBaseState Fall()
    {
        return new PlayerFallingState(context, this);
    }
}

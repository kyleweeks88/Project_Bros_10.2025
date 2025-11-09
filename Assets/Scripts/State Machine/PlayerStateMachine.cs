using UnityEngine;

public class PlayerStateMachine : StateManager<PlayerStateMachine.PlayerState>
{
    public enum PlayerState
    {
        Default,
        Sprint,
        Crouch,
        Jump,
        Disabled
    }

    private void Awake()
    {
        CurrentState = States[PlayerState.Default];
    }
}

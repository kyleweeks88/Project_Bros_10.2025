using System;

public class PlayerActionController : EntityActionController<PlayerAction>
{
    public event Action<PlayerActionType> ActionStarted;
    public event Action<PlayerAction> ActionEnded;

    public PlayerActionController(PlayerActionContext context)
    {
        // The context is retained in the constructor for compatibility with
        // existing setup. Individual PlayerAction instances receive it.
    }

    protected override void OnActionStarted(PlayerAction action)
    {
        ActionStarted?.Invoke(action.ActionType);
    }

    protected override void OnActionEnded(PlayerAction action)
    {
        ActionEnded?.Invoke(action);
    }
}

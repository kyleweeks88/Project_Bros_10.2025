public abstract class PlayerAction : EntityAction
{
    protected PlayerActionContext Context { get; }

    public abstract PlayerActionType ActionType { get; }

    protected PlayerAction(PlayerActionContext context)
    {
        Context = context;
    }
}

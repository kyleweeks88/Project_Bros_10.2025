public abstract class PlayerAction
{
    protected PlayerActionContext Context { get; }

    public abstract PlayerActionType ActionType { get; }

    public bool IsComplete { get; protected set; }

    protected PlayerAction(PlayerActionContext context)
    {
        Context = context;
    }

    public virtual void OnStarted()
    {
    }

    public virtual void OnUpdate()
    {
    }

    public virtual void OnEnded()
    {
    }
}

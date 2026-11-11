public abstract class EntityAction
{
    public bool IsComplete { get; protected set; }

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

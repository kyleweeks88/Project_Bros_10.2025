public abstract class NpcAction : EntityAction
{
    protected NpcActionContext Context { get; }

    public abstract NpcActionType ActionType { get; }

    protected NpcAction(NpcActionContext context)
    {
        Context = context;
    }
}

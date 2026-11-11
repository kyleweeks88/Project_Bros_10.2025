using System;

public class NpcActionController : EntityActionController<NpcAction>
{
    public NpcActionContext Context { get; }

    public event Action<NpcActionType> ActionStarted;
    public event Action<NpcAction> ActionEnded;

    public NpcActionController(NpcActionContext context)
    {
        Context = context;
    }

    protected override void OnActionStarted(NpcAction action)
    {
        ActionStarted?.Invoke(action.ActionType);
    }

    protected override void OnActionEnded(NpcAction action)
    {
        ActionEnded?.Invoke(action);
    }
}

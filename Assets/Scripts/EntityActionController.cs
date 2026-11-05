public class EntityActionController<TAction>
    where TAction : EntityAction
{
    private TAction currentAction;

    public TAction CurrentAction => currentAction;
    public bool IsBusy => currentAction != null;

    public bool TryStartAction(TAction action)
    {
        if (action == null || IsBusy)
            return false;

        currentAction = action;
        currentAction.OnStarted();
        OnActionStarted(currentAction);
        return true;
    }

    public void Update()
    {
        if (!IsBusy)
            return;

        currentAction.OnUpdate();

        if (currentAction.IsComplete)
            EndAction();
    }

    public bool EndAction()
    {
        if (!IsBusy)
            return false;

        TAction endedAction = currentAction;
        endedAction.OnEnded();
        currentAction = null;
        OnActionEnded(endedAction);
        return true;
    }

    public TSpecificAction GetCurrentAction<TSpecificAction>()
        where TSpecificAction : TAction
    {
        return currentAction as TSpecificAction;
    }

    protected virtual void OnActionStarted(TAction action)
    {
    }

    protected virtual void OnActionEnded(TAction action)
    {
    }
}

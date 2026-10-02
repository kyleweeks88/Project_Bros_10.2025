using System;

public class PlayerActionController
{
    private readonly PlayerActionContext context;

    private PlayerAction currentAction;

    public PlayerAction CurrentAction => currentAction;
    public bool IsBusy => currentAction != null;

    public event Action<PlayerActionType> ActionStarted;
    public event Action<PlayerAction> ActionEnded;

    public PlayerActionController(PlayerActionContext context)
    {
        this.context = context;
    }

    public bool TryStartAction(PlayerAction action)
    {
        if (action == null)
            return false;

        if (IsBusy)
            return false;

        currentAction = action;

        currentAction.OnStarted();
        ActionStarted?.Invoke(currentAction.ActionType);

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

        PlayerAction endedAction = currentAction;

        endedAction.OnEnded();

        currentAction = null;

        ActionEnded?.Invoke(endedAction);

        return true;
    }

    public T GetCurrentAction<T>()
    where T : PlayerAction
        {
            return currentAction as T;
        }
}
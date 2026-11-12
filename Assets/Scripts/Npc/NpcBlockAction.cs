using UnityEngine;

public class NpcBlockAction : NpcAction
{
    public override NpcActionType ActionType =>
        NpcActionType.Block;

    public NpcBlockAction(NpcActionContext context)
        : base(context)
    { }

    public override void OnStarted()
    {
        IsComplete = false;
    }

    public override void OnUpdate()
    {
        // NPC BLOCKING IS ONLY EFFECTIVE WHILE GROUNDED
        if (!Context.Locomotion.IsGrounded)
            IsComplete = true;
    }
}

using UnityEngine;

public class NpcMeleeAttackAction : NpcAction
{
    private readonly MeleeAttackData attackData;
    private MeleeAttackExecution execution;
    private Vector3 attackDirection;

    public override NpcActionType ActionType =>
        NpcActionType.MeleeAttack;

    public bool IsInStartup =>
        execution != null && execution.IsInStartup;

    public float StartupProgress =>
        execution != null ? execution.StartupProgress : 0f;

    public NpcMeleeAttackAction(
        NpcActionContext context,
        MeleeAttackData attackData)
        : base(context)
    {
        this.attackData = attackData;
    }

    public override void OnStarted()
    {
        IsComplete = false;

        attackDirection = Context.Transform.forward;
        attackDirection.y = 0f;

        if (attackDirection.sqrMagnitude > 0.001f)
            attackDirection.Normalize();

        execution = new MeleeAttackExecution(
            attackData,
            Context.MeleeCombat,
            Context.DamageController,
            Context.Transform,
            () => 1f,
            ApplyAttackMovement,
            null
        );

        execution.Start();
    }

    public override void OnUpdate()
    {
        execution?.Update();

        if (execution != null && execution.IsComplete)
            IsComplete = true;
    }

    public override void OnEnded()
    {
        execution?.End();
        Context.Locomotion.CancelAttackMovement();
    }

    private void ApplyAttackMovement()
    {
        if (attackData.MovementDuration <= 0f)
            return;

        Vector3 movement =
            attackDirection *
            attackData.HorizontalMovementDistance;

        float verticalMovement = 0f;

        switch (attackData.PlayerVerticalMoveDirection)
        {
            case VerticalAttackDirection.Up:
                verticalMovement =
                    attackData.VerticalMovementDistance;
                break;

            case VerticalAttackDirection.Down:
                verticalMovement =
                    -attackData.VerticalMovementDistance;
                break;
        }

        movement.y = verticalMovement;

        float distance = movement.magnitude;

        if (distance <= 0.001f)
            return;

        Context.Locomotion.ApplyAttackMovement(
            movement.normalized,
            distance,
            attackData.MovementDuration
        );
    }
}

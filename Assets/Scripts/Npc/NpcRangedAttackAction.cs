using UnityEngine;

public class NpcRangedAttackAction : NpcAction
{
    private readonly RangedAttackData attackData;
    private readonly Transform projectileSpawnPoint;
    private readonly Transform targetTransform;
    private readonly Vector3 targetAimPointOffset;

    private RangedAttackExecution execution;

    public override NpcActionType ActionType =>
        NpcActionType.RangedAttack;

    public bool IsInStartup =>
        execution != null && execution.IsInStartup;

    public float StartupProgress =>
        execution != null ? execution.StartupProgress : 0f;

    public bool CanCancelBeforeLaunch =>
        execution != null &&
        execution.CanCancelBeforeLaunch;

    public NpcRangedAttackAction(
        NpcActionContext context,
        RangedAttackData attackData,
        Transform projectileSpawnPoint,
        Transform targetTransform,
        Vector3 targetAimPoint)
        : base(context)
    {
        this.attackData = attackData;
        this.projectileSpawnPoint = projectileSpawnPoint;
        this.targetTransform = targetTransform;
        targetAimPointOffset = targetTransform != null
            ? targetTransform.InverseTransformPoint(targetAimPoint)
            : Vector3.zero;
    }

    public override void OnStarted()
    {
        IsComplete = false;

        execution = new RangedAttackExecution(
            attackData,
            Context.DamageController,
            Context.Transform,
            projectileSpawnPoint,
            GetAimDirection);

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
    }

    private Vector3 GetAimDirection()
    {
        Vector3 origin = projectileSpawnPoint != null
            ? projectileSpawnPoint.position
            : Context.Transform.position;

        Vector3 targetPosition = targetTransform != null
            ? targetTransform.TransformPoint(targetAimPointOffset)
            : origin + Context.Transform.forward;

        return targetPosition - origin;
    }
}

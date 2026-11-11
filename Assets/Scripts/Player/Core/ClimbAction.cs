using UnityEngine;

public class ClimbAction : PlayerAction
{
    private Vector3 lastSurfaceNormal;
    private bool hasLeftGroundWhileClimbing;

    public Vector3 JumpAwayDirection => lastSurfaceNormal;

    private readonly ClimbableSurface surface;

    private const float ProbeDistance = 0.5f;

    public override PlayerActionType ActionType =>
        PlayerActionType.Climb;

    public ClimbAction(
        PlayerActionContext context,
        ClimbableSurface surface)
        : base(context)
    {
        this.surface = surface;
    }

    public override void OnStarted()
    {
        hasLeftGroundWhileClimbing =
            !Context.Locomotion.IsTouchingGround;

        lastSurfaceNormal = -Context.Transform.forward;
        lastSurfaceNormal.y = 0f;

        if (lastSurfaceNormal.sqrMagnitude > 0.001f)
            lastSurfaceNormal.Normalize();

        Context.Locomotion.SetClimbing(true);
        Context.Locomotion.SetRotationLocked(true);
    }

    public override void OnUpdate()
    {
        if (!Context.Locomotion.IsTouchingGround)
        {
            hasLeftGroundWhileClimbing = true;
        }
        else if (hasLeftGroundWhileClimbing)
        {
            IsComplete = true;
            return;
        }

        ProbeSurface();
    }

    public override void OnEnded()
    {
        Context.Locomotion.ClearClimbMovement();
        Context.Locomotion.SetClimbing(false);
        Context.Locomotion.SetRotationLocked(false);
    }

    public bool CanStart()
    {
        return Physics.Raycast(
            Context.Locomotion.ClimbProbeOrigin,
            Context.Transform.forward,
            out RaycastHit hit,
            ProbeDistance) &&
            hit.collider.TryGetComponent(
                out ClimbableSurface hitSurface) &&
            hitSurface == surface;
    }

    private void ProbeSurface()
    {
        Vector3 origin =
            Context.Locomotion.ClimbProbeOrigin;

        Vector3 direction =
            Context.Transform.forward;

        if (!Physics.Raycast(
            origin,
            direction,
            out RaycastHit hit,
            ProbeDistance))
        {
            Context.Locomotion.ClearClimbMovement();
            IsComplete = true;
            return;
        }

        if (!hit.collider.TryGetComponent(
            out ClimbableSurface hitSurface))
        {
            Context.Locomotion.ClearClimbMovement();
            IsComplete = true;
            return;
        }

        lastSurfaceNormal = hit.normal;
        lastSurfaceNormal.y = 0f;

        if (lastSurfaceNormal.sqrMagnitude > 0.001f)
            lastSurfaceNormal.Normalize();

        Debug.DrawRay(
            origin,
            direction * hit.distance,
            Color.blue
        );

        Debug.DrawRay(
            hit.point,
            direction * (ProbeDistance - hit.distance),
            Color.green
        );

        Debug.DrawRay(
            hit.point,
            hit.normal * 0.5f,
            Color.red
        );

        Vector3 inputMovement =
            Context.Transform.up * Context.Input.MovementInput.y +
            Context.Transform.right * Context.Input.MovementInput.x;

        Vector3 constrainedMovement =
            Vector3.ProjectOnPlane(
                inputMovement,
                hit.normal
            );

        if (constrainedMovement.sqrMagnitude > 0.001f)
        {
            constrainedMovement.Normalize();
            Context.Locomotion.SetClimbMovement(
                constrainedMovement * 5f
            );

            Context.Locomotion.SetClimbRotation(hit.normal);
        }
        else
        {
            Context.Locomotion.ClearClimbMovement();
        }
    }
}

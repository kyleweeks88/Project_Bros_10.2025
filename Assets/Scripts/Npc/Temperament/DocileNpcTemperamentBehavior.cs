using UnityEngine;

public class DocileNpcTemperamentBehavior : NpcTemperamentBehavior
{
    private const float FaceTargetDistance = 3f;

    public DocileNpcTemperamentBehavior(NpcController npc)
        : base(npc)
    {
    }

    public override void OnEnter()
    {
        npc.Locomotion?.SetChasing(false);
    }

    public override void OnUpdate()
    {
        if (npc.Awareness != NpcAwareness.Aware ||
            npc.CurrentTarget == null)
        {
            return;
        }

        Vector3 offset =
            npc.CurrentTarget.position - npc.transform.position;

        offset.y = 0f;

        if (offset.sqrMagnitude <= FaceTargetDistance * FaceTargetDistance)
            npc.Locomotion?.FaceTarget(npc.CurrentTarget.position);
    }

    public override void OnExit()
    {
        npc.Locomotion?.SetChasing(false);
    }
}

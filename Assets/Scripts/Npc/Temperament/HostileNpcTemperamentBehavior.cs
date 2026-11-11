public class HostileNpcTemperamentBehavior : NpcTemperamentBehavior
{
    public HostileNpcTemperamentBehavior(NpcController npc)
        : base(npc)
    {
    }

    public override void OnEnter()
    {
        npc.Locomotion?.SetChasing(
            npc.Awareness == NpcAwareness.Aware);
    }

    public override void OnAwarenessChanged(
        NpcAwareness previousAwareness,
        NpcAwareness newAwareness)
    {
        npc.Locomotion?.SetChasing(
            newAwareness == NpcAwareness.Aware);

        if (newAwareness == NpcAwareness.Unaware)
            npc.Actions?.EndAction();
    }

    public override void OnExit()
    {
        npc.Locomotion?.SetChasing(false);
    }
}

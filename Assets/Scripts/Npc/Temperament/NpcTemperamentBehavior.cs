using UnityEngine;

public abstract class NpcTemperamentBehavior
{
    protected readonly NpcController npc;

    protected NpcTemperamentBehavior(NpcController npc)
    {
        this.npc = npc;
    }

    public virtual void OnEnter() { }

    public virtual void OnUpdate() { }

    public virtual void OnAttacked(Transform sourceTransform) { }

    public virtual void OnAwarenessChanged(
        NpcAwareness previousAwareness,
        NpcAwareness newAwareness)
    {
    }

    public virtual void OnExit() { }
}

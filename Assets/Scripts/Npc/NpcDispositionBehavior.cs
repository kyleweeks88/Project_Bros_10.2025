public abstract class NpcDispositionBehavior
{
    protected readonly NpcController npc;

    protected NpcDispositionBehavior(NpcController npc)
    {
        this.npc = npc;
    }

    public virtual void OnEnter() { }

    public abstract void OnUpdate();

    public virtual void OnAwarenessChanged(
        NpcAwareness previousAwareness,
        NpcAwareness newAwareness)
    {
    }

    public virtual void OnExit() { }
}
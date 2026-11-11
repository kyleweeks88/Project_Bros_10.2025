using UnityEngine;

public class ProvokableNpcTemperamentBehavior : DocileNpcTemperamentBehavior
{
    public ProvokableNpcTemperamentBehavior(NpcController npc)
        : base(npc)
    {
    }

    public override void OnAttacked(Transform sourceTransform)
    {
        npc.BecomeHostileToAttacker(sourceTransform);
    }
}

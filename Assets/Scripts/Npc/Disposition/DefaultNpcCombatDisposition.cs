using UnityEngine;

public class DefaultNpcCombatDisposition : NpcCombatDisposition
{
    public DefaultNpcCombatDisposition(
        NpcController npc,
        NpcCombatDispositionData dispositionData = null)
        : base(npc, dispositionData)
    {
    }

    public override void OnUpdate()
    {
        if (UpdateReactiveBlock())
            return;

        if (npc.Temperament != NpcTemperamentType.Hostile ||
            npc.Awareness != NpcAwareness.Aware)
        {
            return;
        }

        Transform target = npc.CurrentTarget;

        if (target == null)
            return;

        PrepareForTarget(target);

        if (TryGetMeleeAttackData(out MeleeAttackData meleeAttackData) &&
            UpdateMeleeCombat(target, meleeAttackData))
        {
            return;
        }

        UpdateRangedCombat(target);
    }
}

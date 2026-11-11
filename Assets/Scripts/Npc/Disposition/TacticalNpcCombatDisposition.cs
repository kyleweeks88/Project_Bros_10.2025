using UnityEngine;

public class TacticalNpcCombatDisposition : NpcCombatDisposition
{
    private enum Phase
    {
        OpeningRangedAttack,
        ChosenStyle,
        RangedOnly
    }

    private enum AttackStyle
    {
        Melee,
        Ranged
    }

    private readonly TacticalNpcCombatDispositionData settings;
    private readonly EntityStats stats;

    private Phase phase = Phase.OpeningRangedAttack;
    private AttackStyle chosenStyle;

    public TacticalNpcCombatDisposition(
        NpcController npc,
        TacticalNpcCombatDispositionData settings)
        : base(npc)
    {
        this.settings = settings;
        stats = npc.GetComponent<EntityStats>();
    }

    public override void OnEnter()
    {
        base.OnEnter();
        phase = Phase.OpeningRangedAttack;
        chosenStyle = AttackStyle.Ranged;
    }

    public override void OnUpdate()
    {
        if (npc.Temperament != NpcTemperamentType.Hostile ||
            npc.Awareness != NpcAwareness.Aware)
        {
            return;
        }

        Transform target = npc.CurrentTarget;

        if (target == null)
            return;

        PrepareForTarget(target);

        if (phase == Phase.OpeningRangedAttack)
        {
            if (UpdateRangedCombat(target))
                ChooseAttackStyle();

            return;
        }

        if (phase == Phase.ChosenStyle && HasReachedHealthThreshold())
            phase = Phase.RangedOnly;

        if (phase == Phase.RangedOnly)
        {
            if (AdjustRangedDistance(target))
                return;

            UpdateRangedCombat(target);
            return;
        }

        if (chosenStyle == AttackStyle.Ranged)
        {
            Vector3 toTarget = target.position - npc.transform.position;
            toTarget.y = 0f;

            float meleeEngagementDistance =
                npc.MeleeEngagementDistance;

            bool targetIsCloseEnoughForMelee =
                toTarget.sqrMagnitude <=
                meleeEngagementDistance * meleeEngagementDistance;

            if (targetIsCloseEnoughForMelee &&
                TryGetMeleeAttackData(
                    out MeleeAttackData closeRangeMeleeAttackData))
            {
                if (!UpdateMeleeCombat(target, closeRangeMeleeAttackData))
                    npc.Locomotion.ChaseTarget();

                return;
            }

            UpdateRangedCombat(target);
            return;
        }

        if (TryGetMeleeAttackData(out MeleeAttackData meleeAttackData))
        {
            if (!UpdateMeleeCombat(target, meleeAttackData))
                npc.Locomotion.ChaseTarget();

            return;
        }

        // If the melee attack is missing from this NPC's setup, keep it
        // functional by using its configured ranged attack instead.
        UpdateRangedCombat(target);
    }

    private void ChooseAttackStyle()
    {
        float meleeChance = settings != null
            ? Mathf.Clamp(settings.MeleeChoiceChance, 0f, 100f)
            : 50f;

        chosenStyle = Random.Range(0f, 100f) < meleeChance
            ? AttackStyle.Melee
            : AttackStyle.Ranged;

        phase = Phase.ChosenStyle;
    }

    private bool HasReachedHealthThreshold()
    {
        if (stats == null || stats.MaxHealth <= 0f)
            return false;

        float healthLossThreshold = settings != null
            ? Mathf.Clamp(
                settings.HealthLossBeforeRangedRetreat,
                0f,
                100f)
            : 65f;

        float remainingHealthFraction =
            stats.CurrentHealth / stats.MaxHealth;

        float remainingHealthThreshold =
            1f - healthLossThreshold / 100f;

        return remainingHealthFraction <= remainingHealthThreshold;
    }
}

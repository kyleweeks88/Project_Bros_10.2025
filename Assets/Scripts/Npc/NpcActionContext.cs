using UnityEngine;

public class NpcActionContext
{
    public Transform Transform { get; }
    public NPCLocomotion Locomotion { get; }
    public NpcCombatController MeleeCombat { get; }
    public DamageController DamageController { get; }
    public EntityStats Stats { get; }

    public NpcActionContext(
        Transform transform,
        NPCLocomotion locomotion,
        NpcCombatController meleeCombat,
        DamageController damageController,
        EntityStats stats)
    {
        Transform = transform;
        Locomotion = locomotion;
        MeleeCombat = meleeCombat;
        DamageController = damageController;
        Stats = stats;
    }
}

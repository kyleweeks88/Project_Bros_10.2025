using UnityEngine;

public class PlayerActionContext
{
    public Transform Transform { get; }
    public PlayerInputHandler Input { get; }
    public PlayerLocomotion Locomotion { get; }
    public MeleeCombatController MeleeCombat { get; }
    public DamageController DamageController { get; }
    public EntityStats Stats { get; }

    public PlayerActionContext(
        Transform transform,
        PlayerInputHandler input,
        PlayerLocomotion locomotion,
        MeleeCombatController meleeCombat,
        DamageController damageController,
        EntityStats stats)
    {
        Transform = transform;
        Input = input;
        Locomotion = locomotion;
        MeleeCombat = meleeCombat;
        DamageController = damageController;
        Stats = stats;
    }
}

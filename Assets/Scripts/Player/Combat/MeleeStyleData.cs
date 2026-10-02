using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "MeleeStyleData",
    menuName = "Combat/Melee Style Data"
)]
public class MeleeStyleData : ScriptableObject
{
    [SerializeField] private string styleName;

    [Header("Light Attack 1")]
    [SerializeField] private List<MeleeAttackData> lightAttack1Chain = new();

    [Header("Light Attack 2")]
    [SerializeField] private List<MeleeAttackData> lightAttack2Chain = new();

    [Header("Heavy Attack 1")]
    [SerializeField] private List<MeleeAttackData> heavyAttack1Chain = new();

    [Header("Heavy Attack 2")]
    [SerializeField] private List<MeleeAttackData> heavyAttack2Chain = new();

    [Header("Airborne Attacks")]
    [SerializeField] private MeleeAttackData airborneLightAttack1;
    [SerializeField] private MeleeAttackData airborneLightAttack2;
    [SerializeField] private MeleeAttackData airborneHeavyAttack1;
    [SerializeField] private MeleeAttackData airborneHeavyAttack2;

    #region COMBO CHAINS
    public string StyleName => styleName;

    public IReadOnlyList<MeleeAttackData> LightAttack1Chain =>
        lightAttack1Chain;

    public IReadOnlyList<MeleeAttackData> LightAttack2Chain =>
        lightAttack2Chain;

    public IReadOnlyList<MeleeAttackData> HeavyAttack1Chain =>
        heavyAttack1Chain;

    public IReadOnlyList<MeleeAttackData> HeavyAttack2Chain =>
        heavyAttack2Chain;
    #endregion

    #region AIRBORNE ATTACKS
    public MeleeAttackData AirborneLightAttack1 => airborneLightAttack1;
    public MeleeAttackData AirborneLightAttack2 => airborneLightAttack2;
    public MeleeAttackData AirborneHeavyAttack1 => airborneHeavyAttack1;
    public MeleeAttackData AirborneHeavyAttack2 => airborneHeavyAttack2;
    #endregion

    public IReadOnlyList<MeleeAttackData> GetChain(
        MeleeAttackType attackType)
    {
        return attackType switch
        {
            MeleeAttackType.LightAttack1 => lightAttack1Chain,
            MeleeAttackType.LightAttack2 => lightAttack2Chain,
            MeleeAttackType.HeavyAttack1 => heavyAttack1Chain,
            MeleeAttackType.HeavyAttack2 => heavyAttack2Chain,
            _ => null
        };
    }

    public MeleeAttackData GetAirborneAttack(MeleeAttackType attackType)
    {
        // THIS IS JUST A FANCY WAY OF RETURNING
        // A SWITCH STATEMENT
        return attackType switch
        {
            MeleeAttackType.LightAttack1 => airborneLightAttack1,
            MeleeAttackType.LightAttack2 => airborneLightAttack2,
            MeleeAttackType.HeavyAttack1 => airborneHeavyAttack1,
            MeleeAttackType.HeavyAttack2 => airborneHeavyAttack2,
            _ => null
        };
    }
}

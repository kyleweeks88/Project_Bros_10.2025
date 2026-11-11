using UnityEngine;

[CreateAssetMenu(
    fileName = "TacticalNpcCombatDisposition",
    menuName = "NPC/Combat Disposition/Tactical"
)]
public class TacticalNpcCombatDispositionData : NpcCombatDispositionData
{
    [Header("Tactical Behavior")]
    [SerializeField, Range(0f, 100f)]
    private float meleeChoiceChance = 50f;

    [SerializeField, Range(0f, 100f)]
    private float healthLossBeforeRangedRetreat = 65f;

    public float MeleeChoiceChance => meleeChoiceChance;
    public float HealthLossBeforeRangedRetreat =>
        healthLossBeforeRangedRetreat;

    public override NpcCombatDisposition CreateRuntime(
        NpcController npc)
    {
        return new TacticalNpcCombatDisposition(npc, this);
    }
}

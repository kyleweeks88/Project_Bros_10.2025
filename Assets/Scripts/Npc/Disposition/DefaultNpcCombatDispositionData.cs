using UnityEngine;

[CreateAssetMenu(
    fileName = "DefaultNpcCombatDisposition",
    menuName = "NPC/Combat Disposition/Default"
)]
public class DefaultNpcCombatDispositionData : NpcCombatDispositionData
{
    public override NpcCombatDisposition CreateRuntime(
        NpcController npc)
    {
        return new DefaultNpcCombatDisposition(npc, this);
    }
}

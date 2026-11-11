using UnityEngine;

public class NpcCombatController : IMeleeCombat
{
    private readonly MeleeStyleData meleeStyle;
    private readonly GameObject meleeHitbox;
    private readonly MeleeHitbox hitbox;
    private readonly Transform attackerTransform;

    public MeleeStyleData MeleeStyle => meleeStyle;

    public NpcCombatController(
        MeleeStyleData meleeStyle,
        GameObject meleeHitbox,
        Transform attackerTransform)
    {
        this.meleeStyle = meleeStyle;
        this.meleeHitbox = meleeHitbox;
        this.attackerTransform = attackerTransform;

        if (meleeHitbox != null)
        {
            hitbox = meleeHitbox.GetComponent<MeleeHitbox>();
            meleeHitbox.SetActive(false);
        }
        else
        {
            Debug.LogWarning(
                "NpcCombatController has no melee hitbox assigned.");
        }
    }

    public bool TryGetAttackData(
        MeleeAttackType attackType,
        out MeleeAttackData attackData)
    {
        attackData = null;

        if (meleeStyle == null)
            return false;

        var chain = meleeStyle.GetChain(attackType);

        if (chain == null || chain.Count == 0)
            return false;

        attackData = chain[0];
        return attackData != null;
    }

    public void SetHitboxActive(bool isActive)
    {
        if (meleeHitbox != null)
            meleeHitbox.SetActive(isActive);
    }

    public void SetHitboxDamage(
        DamageInfo damageInfo,
        float horizontalKnockbackForce,
        float verticalKnockbackForce,
        VerticalAttackDirection targetVerticalKnockbackDirection,
        System.Action onAttackConnected)
    {
        if (hitbox == null)
            return;

        hitbox.SetAttackDamage(
            damageInfo,
            attackerTransform,
            horizontalKnockbackForce,
            verticalKnockbackForce,
            targetVerticalKnockbackDirection,
            onAttackConnected);
    }
}

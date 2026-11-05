using System;

public interface IMeleeCombat
{
    void SetHitboxActive(bool isActive);

    void SetHitboxDamage(
        DamageInfo damageInfo,
        float horizontalKnockbackForce,
        float verticalKnockbackForce,
        VerticalAttackDirection targetVerticalKnockbackDirection,
        Action onAttackConnected);
}
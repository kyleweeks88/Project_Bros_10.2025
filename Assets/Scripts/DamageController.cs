using UnityEngine;

public class DamageController
{
    private bool isInvulnerable;
    public bool IsInvulnerable => isInvulnerable;

    private readonly EntityStats stats;
    private readonly IPhysicsMove physicsMove;

    public DamageController(
        EntityStats stats,
        IPhysicsMove physicsMove)
    {
        this.stats = stats;
        this.physicsMove = physicsMove;
    }

    public void ReceiveDamage(
        DamageInfo damageInfo,
        Transform defenderTransform = null,
        bool isBlocking = false)
    {
        if(isInvulnerable)
        {
            Debug.Log("Incoming damage ignored during Dodge.");
            return;
        }

        DamageInfo finalDamage =
            CalculateIncomingDamage(
                damageInfo,
                defenderTransform,
                isBlocking);

        stats.TakeDamage(finalDamage.Amount);

        if (stats.IsDead) return;

        physicsMove.ApplyKnockback(
            finalDamage.KnockbackForce);
    }

    public DamageInfo CalculateIncomingDamage(
        DamageInfo damageInfo,
        Transform defenderTransform = null,
        bool isBlocking = false)
    {
        float finalDamage = damageInfo.Amount;

        float knockbackMultiplier =
            1f - stats.KnockbackResistance;

        bool blockIsEffective =
            IsBlockEffective(
                damageInfo,
                defenderTransform,
                isBlocking);

        if (blockIsEffective)
        {
            finalDamage *=
                1f - stats.BlockDamageMitigation;

            knockbackMultiplier *=
                1f - stats.BlockKnockbackMitigation;
        }

        Vector3 finalKnockback =
            damageInfo.KnockbackForce *
            knockbackMultiplier;

        return new DamageInfo(
            finalDamage,
            finalKnockback,
            damageInfo.SourceType,
            damageInfo.SourcePosition);
    }

    public DamageInfo CalculateOutgoingDamage(
        float amount,
        Vector3 knockBackForce,
        DamageSourceType sourceType,
        Vector3 sourcePosition)
    {
        float calculatedDamage = 0f;

        switch (sourceType)
        {
            case DamageSourceType.Melee:
                calculatedDamage =
                    stats.DamageOutput * amount;
                Debug.Log("OUTGOING: Melee Attack Damage Source");
                break;

            case DamageSourceType.Ability:
                calculatedDamage = amount;
                Debug.Log("OUTGOING: Ability Damage Source");
                break;
        }

        return new DamageInfo(
            calculatedDamage,
            knockBackForce,
            sourceType,
            sourcePosition);
    }

    private bool IsBlockEffective(
    DamageInfo damageInfo,
    Transform defenderTransform,
    bool isBlocking)
    {
        if (!isBlocking || defenderTransform == null)
            return false;

        Vector3 directionToSource =
            damageInfo.SourcePosition -
            defenderTransform.position;

        directionToSource.y = 0f;

        Vector3 forward = defenderTransform.forward;
        forward.y = 0f;

        if (directionToSource.sqrMagnitude <= 0.001f ||
            forward.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        float angleFromFront =
            Vector3.Angle(forward, directionToSource);

        float halfConeAngle =
            stats.BlockAngle * 0.5f;

        return angleFromFront <= halfConeAngle;
    }

    public void SetInvulnerable(bool value)
    {
        isInvulnerable = value;
    }
}
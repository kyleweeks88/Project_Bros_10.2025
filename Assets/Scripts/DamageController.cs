using UnityEngine;
using System;

public class DamageController
{
    public event Action KnockbackActionCancelRequested;
    public event Action RagdollRequested;

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
        if (isInvulnerable)
        {
            Debug.Log("Incoming damage ignored during Dodge.");
            return;
        }

        DamageInfo finalDamage =
            CalculateIncomingDamage(
                damageInfo,
                defenderTransform,
                isBlocking,
                out bool poiseBroken,
                out bool shouldCancelAction,
                out bool shouldRagdoll);

        stats.TakeDamage(finalDamage.Amount);

        if (stats.IsDead)
            return;

        if (!poiseBroken ||
            finalDamage.KnockbackForce.sqrMagnitude <= 0.001f)
        {
            return;
        }

        if (shouldCancelAction)
            KnockbackActionCancelRequested?.Invoke();

        if(shouldRagdoll)
            RagdollRequested?.Invoke();

        physicsMove.ApplyKnockback(
            finalDamage.KnockbackForce);
    }

    public DamageInfo CalculateIncomingDamage(
        DamageInfo damageInfo,
        Transform defenderTransform,
        bool isBlocking,
        out bool poiseBroken,
        out bool shouldCancelAction,
        out bool shouldRagdoll)
    {
        float finalDamage = damageInfo.Amount;

        bool blockIsEffective =
            IsBlockEffective(
                damageInfo,
                defenderTransform,
                isBlocking);

        float thresholdImpact =
            damageInfo.KnockbackForce.magnitude;

        if (blockIsEffective)
        {
            finalDamage *=
                1f - stats.BlockDamageMitigation;

            // Blocking reduces threshold depletion only
            thresholdImpact *=
                1f - stats.BlockKnockbackMitigation;
        }

        // Poise is checked against block-mitigated impact.
        poiseBroken =
            stats.ShouldApplyKnockback(thresholdImpact);

        float resistance = stats.KnockbackResistance;
        float incomingMagnitude =
            damageInfo.KnockbackForce.magnitude;

        float knockbackMultiplier;

        shouldCancelAction = false;
        shouldRagdoll = false;

        if(incomingMagnitude <= resistance * 0.5f)
        {
            knockbackMultiplier = 0f;
        }
        else if(incomingMagnitude <= resistance)
        {
            knockbackMultiplier = 0.5f;
        }
        else
        {
            knockbackMultiplier = 1f;
            shouldCancelAction = true;

            if(incomingMagnitude > resistance * 2f &&
                incomingMagnitude >= 10f)
            {
                shouldRagdoll = true;
            }
        }

        // Resistance selects a tier; it no longer
        // continuously scales force.
        Vector3 finalKnockback =
            damageInfo.KnockbackForce * knockbackMultiplier;

        return new DamageInfo(
            finalDamage,
            finalKnockback,
            damageInfo.SourceType,
            damageInfo.SourcePosition,
            damageInfo.SourceTransform);
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

            case DamageSourceType.Projectile:
                calculatedDamage = stats.DamageOutput * amount;
                Debug.Log("OUTGOING: Projectile Attack Damage Source");
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
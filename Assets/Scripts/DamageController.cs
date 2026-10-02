using UnityEngine;

public class DamageController
{
    private readonly EntityStats stats;
    private readonly IPhysicsMove physicsMove;

    public DamageController(
        EntityStats stats,
        IPhysicsMove physicsMove
        )
    {
        this.stats = stats;
        this.physicsMove = physicsMove;
    }

    #region RECEIVE DAMAGE

    public void ReceiveDamage(DamageInfo damageInfo)
    {
        DamageInfo finalDamage =
            CalculateIncomingDamage(damageInfo);

        stats.TakeDamage(finalDamage.Amount);

        physicsMove.ApplyKnockback(
            finalDamage.KnockbackForce);
    }

    #endregion

    #region CALCULATE DAMAGE

    // INCOMING DAMAGE
    public DamageInfo CalculateIncomingDamage(DamageInfo damageInfo)
    {
        float finalDamage = damageInfo.Amount;

        float resistance =
            stats.KnockbackResistance;

        Vector3 finalKnockback =
            damageInfo.KnockbackForce * (1f - resistance);

        return new DamageInfo(
            finalDamage,
            finalKnockback,
            damageInfo.SourceType
            );
    }

    // OUTGOING DAMAGE
    public DamageInfo CalculateOutgoingDamage(
    float amount,
    Vector3 knockBackForce,
    DamageSourceType sourceType)
    {
        float calculatedDamage = 0f;

        switch(sourceType)
        {
            case DamageSourceType.Melee:
                // ADD MELEE MODIFIERS
                calculatedDamage =
                    stats.DamageOutput * amount;
                Debug.Log("OUTGOING: Melee Attack Damage Source");
                break;

            case DamageSourceType.Ability:
                // ADD ABILITY MODIFIERS
                calculatedDamage = amount;
                Debug.Log("OUTGOING: Ability Damage Source");
                break;
        }

        return new DamageInfo(
            calculatedDamage,
            knockBackForce,
            sourceType
            );
    }

    #endregion

    // CALCULATE DAMAGE OVER TIME
}
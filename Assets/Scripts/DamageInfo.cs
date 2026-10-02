using UnityEngine;

public readonly struct DamageInfo
{
    public float Amount { get; }
    public Vector3 KnockbackForce { get; }
    public DamageSourceType SourceType { get; }

    public DamageInfo(
        float amount,
        Vector3 knockbackForce,
        DamageSourceType sourceType)
    {
        Amount = amount;
        KnockbackForce = knockbackForce;
        SourceType = sourceType;
    }
}
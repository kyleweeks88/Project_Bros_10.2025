using UnityEngine;

public readonly struct DamageInfo
{
    public float Amount { get; }
    public Vector3 KnockbackForce { get; }
    public DamageSourceType SourceType { get; }
    public Vector3 SourcePosition { get; }

    public Transform SourceTransform { get; }

    public DamageInfo(
        float amount,
        Vector3 knockbackForce,
        DamageSourceType sourceType,
        Vector3 sourcePosition,
        Transform sourceTransform = null)
    {
        Amount = amount;
        KnockbackForce = knockbackForce;
        SourceType = sourceType;
        SourcePosition = sourcePosition;
        SourceTransform = sourceTransform;
    }
}
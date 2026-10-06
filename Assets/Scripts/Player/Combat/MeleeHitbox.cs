using UnityEngine;
using System.Collections.Generic;
using System;

public class MeleeHitbox : MonoBehaviour
{
    private readonly HashSet<IDamageable> affectedTargets =
        new HashSet<IDamageable>();

    private DamageInfo damageInfo;

    private Transform attackerTransform;
    private float horizontalKnockbackForce;
    private float verticalKnockbackForce;
    private VerticalAttackDirection targetVerticalKnockbackDirection;

    private Action onAttackConnected;

    public void SetAttackDamage(
        DamageInfo damageInfo,
        Transform attackerTransform,
        float horizontalKnockbackForce,
        float verticalKnockbackForce,
        VerticalAttackDirection targetVerticalKnockbackDirection,
        Action onAttackConnected)
    {
        this.damageInfo = damageInfo;
        this.attackerTransform = attackerTransform;
        this.horizontalKnockbackForce = horizontalKnockbackForce;
        this.verticalKnockbackForce = verticalKnockbackForce;
        this.targetVerticalKnockbackDirection =
            targetVerticalKnockbackDirection;
        this.onAttackConnected = onAttackConnected;

        affectedTargets.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == attackerTransform ||
            other.transform.IsChildOf(attackerTransform))
            return;

        if (!other.TryGetComponent<IDamageable>(
            out IDamageable damageable))
        {
            return;
        }

        if (!affectedTargets.Add(damageable))
            return;

        Vector3 knockbackDirection =
            other.transform.position -
            attackerTransform.position;

        // Ground-based melee should not launch targets upward.
        knockbackDirection.y = 0f;

        if (knockbackDirection.sqrMagnitude <= 0.001f)
            return;

        knockbackDirection.Normalize();

        Vector3 knockbackForce =
            knockbackDirection *
            horizontalKnockbackForce;

        switch(targetVerticalKnockbackDirection)
        {
            case VerticalAttackDirection.Up:
                knockbackForce.y = verticalKnockbackForce;
                break;

            case VerticalAttackDirection.Down:
                knockbackForce.y = -verticalKnockbackForce;
                break;

            case VerticalAttackDirection.None:
                knockbackForce.y = 0f;
                break;
        }

        DamageInfo finalDamage =
            new DamageInfo(
                damageInfo.Amount,
                knockbackForce,
                damageInfo.SourceType
            );

        damageable.ReceiveDamage(finalDamage);

        Action callback = onAttackConnected;
        onAttackConnected = null;
        callback?.Invoke();

        Debug.Log(
            $"Melee hit {other.gameObject.name} | " +
            $"Power: {finalDamage.Amount:F2} | " +
            $"Knockback: {finalDamage.KnockbackForce}"
        );
    }
}

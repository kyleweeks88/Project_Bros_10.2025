using System.Collections.Generic;
using UnityEngine;

public class StompAbility : PlayerAbility
{
    private readonly StompAbilityData stompData;

    private bool isStomping;


    public StompAbility(
        PlayerAbilityContext context,
        StompAbilityData data)
        : base(context, data)
    {
        stompData = data;
    }

    // ============================================================
    // INPUT
    // ============================================================

    public override bool CanActivate()
    {
        // Stomp is strictly grounded.
        if (!Context.IsGrounded)
            return false;


        if (isStomping)
            return false;

        return true;
    }


    public override bool TryActivate()
    {
        StartStomp();

        return true;
    }


    // ============================================================
    // STOMP
    // ============================================================

    private void StartStomp()
    {
        isStomping = true;

        PerformImpact();

        isStomping = false;
    }


    private void PerformImpact()
    {
        HashSet<IDamageable> affectedTargets =
            new HashSet<IDamageable> ();

        Vector3 impactCenter =
            Context.Transform.position;

        Collider[] colliders =
            Physics.OverlapSphere(
                impactCenter,
                stompData.ImpactRadius
            );

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];

            // Ignore the player.
            if (collider.transform == Context.Transform)
                continue;

            if (collider.TryGetComponent<IDamageable>(
                out IDamageable damageable))
            {
                if (!affectedTargets.Add(damageable))
                    continue;

                Vector3 direction =
                    collider.ClosestPoint(impactCenter)
                    - impactCenter;

                direction.y = 0f;

                if (direction.sqrMagnitude <= 0.001f)
                    continue;

                direction.Normalize();

                Vector3 knockback =
                    direction * stompData.KnockbackForce;

                knockback.y =
                    stompData.UpwardModifier;

                DamageInfo damageInfo =
                    Context.DamageController.CalculateOutgoingDamage(
                        stompData.Damage,
                        knockback,
                        DamageSourceType.Ability,
                        Context.Transform.position
                    );

                damageable.ReceiveDamage(damageInfo);
            }

            // Rigidbody effects will go here later.
        }

        Debug.Log("STOMP IMPACT");
    }
}
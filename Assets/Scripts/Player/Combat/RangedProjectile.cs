using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class RangedProjectile : MonoBehaviour
{
    private Rigidbody body;
    private Transform owner;
    private RangedAttackData attackData;
    private Vector3 travelDirection;
    private Vector3 sourcePosition;
    private DamageInfo damageInfo;

    private Vector3 previousPosition;
    private float distanceTravelled;
    private bool isInitialized;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.useGravity = false;
        body.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        GetComponent<Collider>().isTrigger = true;
    }

    public void Initialize(
        Transform owner,
        Vector3 direction,
        RangedAttackData attackData,
        DamageInfo damageInfo)
    {
        if (attackData == null || direction.sqrMagnitude <= 0.001f)
        {
            Destroy(gameObject);
            return;
        }

        this.owner = owner;
        this.attackData = attackData;
        this.damageInfo = damageInfo;

        travelDirection = direction.normalized;
        sourcePosition = owner != null
            ? owner.position
            : transform.position;

        body.linearVelocity =
            travelDirection * attackData.ProjectileSpeed;

        previousPosition = transform.position;
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized)
            return;

        distanceTravelled +=
            Vector3.Distance(previousPosition, transform.position);

        previousPosition = transform.position;

        if (distanceTravelled >= attackData.MaxRange)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isInitialized || IsOwnerCollider(other))
            return;

        if (other.TryGetComponent<IDamageable>(
                out IDamageable damageable))
        {
            damageable.ReceiveDamage(CreateImpactDamage());
        }

        Debug.Log($"Projectile contacted: {other.name}", other);
        Destroy(gameObject);
    }

    private bool IsOwnerCollider(Collider other)
    {
        return owner != null &&
               (other.transform == owner ||
                other.transform.IsChildOf(owner));
    }

    private DamageInfo CreateImpactDamage()
    {
        Vector3 horizontalDirection = travelDirection;
        horizontalDirection.y = 0f;

        if (horizontalDirection.sqrMagnitude > 0.001f)
            horizontalDirection.Normalize();

        Vector3 knockback =
            horizontalDirection *
            attackData.HorizontalKnockbackForce;

        switch (attackData.TargetVerticalKnockbackDirection)
        {
            case VerticalAttackDirection.Up:
                knockback.y = attackData.VerticalKnockbackForce;
                break;

            case VerticalAttackDirection.Down:
                knockback.y = -attackData.VerticalKnockbackForce;
                break;
        }

        return new DamageInfo(
            damageInfo.Amount,
            knockback,
            DamageSourceType.Projectile,
            sourcePosition,
            owner);
    }
}
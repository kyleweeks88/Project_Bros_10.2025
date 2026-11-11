using UnityEngine;

[CreateAssetMenu(
    fileName = "RangedAttackData",
    menuName = "Combat/Ranged Attack Data"
)]
public class RangedAttackData : ScriptableObject
{
    [Header("Attack")]
    [SerializeField] private string attackName;
    [SerializeField, Min(0f)] private float startupTime = 0.4f;
    [SerializeField, Min(0f)] private float recoveryTime = 0.8f;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField, Min(0f)] private float projectileSpeed = 12f;
    [SerializeField, Min(0f)] private float maxRange = 12f;
    [Tooltip("Set this to at least the radius of the projectile.")]
    [SerializeField, Min(0f)] private float projectileClearanceRadius = 0.25f;

    [Header("Damage")]
    [SerializeField, Min(0f)] private float damageMultiplier = 1f;
    [SerializeField, Min(0f)] private float horizontalKnockbackForce = 5f;
    [SerializeField, Min(0f)] private float verticalKnockbackForce = 0f;
    [SerializeField]
    private VerticalAttackDirection targetVerticalKnockbackDirection =
        VerticalAttackDirection.None;

    public string AttackName => attackName;
    public float StartupTime => startupTime;
    public float RecoveryTime => recoveryTime;

    public GameObject ProjectilePrefab => projectilePrefab;
    public float ProjectileSpeed => projectileSpeed;
    public float MaxRange => maxRange;
    public float ProjectileClearanceRadius => projectileClearanceRadius;

    public float DamageMultiplier => damageMultiplier;
    public float HorizontalKnockbackForce => horizontalKnockbackForce;
    public float VerticalKnockbackForce => verticalKnockbackForce;
    public VerticalAttackDirection TargetVerticalKnockbackDirection =>
        targetVerticalKnockbackDirection;

}
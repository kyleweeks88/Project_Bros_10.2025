using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EntityStats))]
public class EnemyController : MonoBehaviour, ITargetable, IDamageable
{
    // TEMPORARY MELEE TESTING
    [Header("TEMP MELEE")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackKnockback = 8f;
    [SerializeField] private float attackCooldown = 1f;

    private float attackTimer;
    private Transform playerTarget;


    private EntityStats stats;
    private DamageController damageController;
    private NPCLocomotion locomotion;
    private CharacterController characterController;
    private NavMeshAgent navMeshAgent;

    public NPCLocomotion Locomotion => locomotion;

    private void Awake()
    {
        stats = GetComponent<EntityStats>();
        characterController = GetComponent<CharacterController>();
        navMeshAgent = GetComponent<NavMeshAgent>();

        locomotion = new NPCLocomotion(
            characterController,
            navMeshAgent
            );

        damageController = new DamageController(
            stats,
            locomotion
            );
    }

    private void OnEnable()
    {
        stats.Died += Death;
    }

    private void Start()
    {
        locomotion.Start();

        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerTarget = player.transform;
        }
    }

    private void Update()
    {
        locomotion.Update();

        TryAttackPlayer();
    }

    private void OnDisable()
    {
        stats.Died -= Death;
    }

    public Transform TargetTransform => this.transform;

    public void ReceiveDamage(DamageInfo damageInfo)
    {
        damageController.ReceiveDamage(damageInfo);
    }

    void Death()
    {
        Object.Destroy(this.gameObject);
    }

    private void TryAttackPlayer()
    {
        if (playerTarget == null)
            return;

        attackTimer -= Time.deltaTime;

        if (attackTimer > 0f)
            return;

        Vector3 direction =
            playerTarget.position -
            transform.position;

        direction.y = 0f;

        float distance =
            direction.magnitude;

        if (distance > attackRange)
            return;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        direction.Normalize();

        Vector3 knockback =
            direction * attackKnockback;

        DamageInfo damageInfo =
            new DamageInfo(
                attackDamage,
                knockback,
                DamageSourceType.Melee
            );

        if (playerTarget.TryGetComponent<IDamageable>(
            out IDamageable damageable))
        {
            damageable.ReceiveDamage(damageInfo);

            Debug.Log(
                $"NPC attacked Player | " +
                $"Damage: {damageInfo.Amount} | " +
                $"Knockback: {damageInfo.KnockbackForce}"
            );

            attackTimer = attackCooldown;
        }
    }
}

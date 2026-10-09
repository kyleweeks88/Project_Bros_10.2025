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
    // ATTACK VIS
    [SerializeField] private float attackWindupDuration = 0.4f;
    [SerializeField] private float attackStrikeFlashDuration = 0.15f;
    [SerializeField] private Color attackWindupColor = Color.yellow;
    [SerializeField] private Color attackStrikeColor = Color.red;

    private bool isAttackWindingUp;
    private float attackWindupTimer;
    private float attackStrikeFlashTimer;

    private Renderer attackRenderer;
    private MaterialPropertyBlock attackPropertyBlock;
    private MaterialPropertyBlock originalPropertyBlock;

    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId =
        Shader.PropertyToID("_Color");


    private float attackTimer;
    private Transform playerTarget;

    // COMPONENTS
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
        attackRenderer = GetComponentInChildren<Renderer>();

        if (attackRenderer != null)
        {
            attackPropertyBlock = new MaterialPropertyBlock();
            originalPropertyBlock = new MaterialPropertyBlock();

            attackRenderer.GetPropertyBlock(originalPropertyBlock);
        }

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
        UpdateAttackVisual();
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

        Vector3 direction =
            playerTarget.position - transform.position;

        direction.y = 0f;

        float distance = direction.magnitude;

        if (distance > attackRange ||
            direction.sqrMagnitude <= 0.001f)
        {
            if (isAttackWindingUp)
                CancelAttackWindup();

            if (attackTimer > 0f)
                attackTimer -= Time.deltaTime;

            return;
        }

        if (!isAttackWindingUp)
        {
            if (attackTimer > 0f)
            {
                attackTimer -= Time.deltaTime;
                return;
            }

            isAttackWindingUp = true;
            attackWindupTimer = attackWindupDuration;
            SetAttackColor(attackWindupColor);
            return;
        }

        attackWindupTimer -= Time.deltaTime;

        if (attackWindupTimer > 0f)
            return;

        direction.Normalize();

        Vector3 knockback =
            direction * attackKnockback;

        DamageInfo damageInfo =
            new DamageInfo(
                attackDamage,
                knockback,
                DamageSourceType.Melee,
                transform.position
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

        isAttackWindingUp = false;
        attackStrikeFlashTimer = attackStrikeFlashDuration;
        SetAttackColor(attackStrikeColor);
    }

    private void UpdateAttackVisual()
    {
        if (attackStrikeFlashTimer <= 0f)
            return;

        attackStrikeFlashTimer -= Time.deltaTime;

        if (attackStrikeFlashTimer <= 0f &&
            !isAttackWindingUp)
        {
            RestoreAttackColor();
        }
    }

    private void CancelAttackWindup()
    {
        isAttackWindingUp = false;
        attackWindupTimer = 0f;
        RestoreAttackColor();
    }

    private void SetAttackColor(Color color)
    {
        if (attackRenderer == null)
            return;

        attackRenderer.GetPropertyBlock(attackPropertyBlock);
        attackPropertyBlock.SetColor(BaseColorId, color);
        attackPropertyBlock.SetColor(ColorId, color);
        attackRenderer.SetPropertyBlock(attackPropertyBlock);
    }

    private void RestoreAttackColor()
    {
        if (attackRenderer == null)
            return;

        attackRenderer.SetPropertyBlock(originalPropertyBlock);
    }
}

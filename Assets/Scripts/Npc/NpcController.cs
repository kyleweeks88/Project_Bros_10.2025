using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EntityStats))]
[RequireComponent(typeof(FactionMember))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NavMeshAgent))]
public class NpcController : MonoBehaviour, ITargetable, IDamageable
{
    #region REFERENCES
    [Header("Awareness")]
    [SerializeField, Min(0f)]
    private float awarenessDistance = 10f;
    [Tooltip("How far an NPC will chase a target after detecting it.")]
    [SerializeField, Min(0f)]
    private float targetRetentionDistance = 20f;
    [SerializeField] private NpcAwareness awareness = NpcAwareness.Unaware;
    public NpcAwareness Awareness => awareness;

    [Header("Disposition")]
    [SerializeField]
    private NpcDispositionType disposition = NpcDispositionType.Hostile;
    public NpcDispositionType Disposition => disposition;
    public event System.Action<NpcDispositionType, NpcDispositionType>
        DispositionChanged;
    private NpcDispositionBehavior currentBehavior;

    [Header("Combat")]
    // RANGED
    [SerializeField] private RangedAttackData rangedAttackData;
    [SerializeField] private Transform projectileSpawnPoint;
    public RangedAttackData RangedAttack => rangedAttackData;
    public Transform ProjectileSpawnPoint => projectileSpawnPoint;
    [Tooltip("Desired horizontal distance to maintain while using ranged attacks.")]
    [SerializeField, Min(0f)]
    private float preferredRangedDistance = 6f;
    [Tooltip("Allowed distance variation around the preferred ranged distance.")]
    [SerializeField, Min(0f)]
    private float rangedDistanceTolerance = 0.5f;
    public float PreferredRangedDistance => preferredRangedDistance;
    public float RangedDistanceTolerance => rangedDistanceTolerance;

    // MELEE
    [SerializeField, Min(0f)]
    private float engagementDistance = 2f;
    public float EngagementDistance => engagementDistance;
    [SerializeField] private MeleeStyleData meleeStyle;
    [SerializeField] private GameObject meleeHitbox;


    // COMPONENTS
    public Transform TargetTransform => this.transform;

    private FactionMember factionMember;
    private FactionMember currentTargetMember;

    public Transform CurrentTarget =>
        currentTargetMember != null
            ? currentTargetMember.transform
            : null;

    [SerializeField, Min(0f)]
    private float targetScanInterval = 0.25f;

    private float targetScanTimer;
    private readonly Collider[] targetScanBuffer = new Collider[32];

    private EntityStats stats;
    private DamageController damageController;
    private NPCLocomotion locomotion;
    private CharacterController characterController;
    private NavMeshAgent navMeshAgent;
    private NpcCombatController combatController;
    private NpcActionController actions;

    public NpcCombatController CombatController => combatController;
    public NPCLocomotion Locomotion => locomotion;
    public NpcActionController Actions => actions;
    #endregion


    #region LIFECYCLE
    private void Awake()
    {
        factionMember = GetComponent<FactionMember>();
        stats = GetComponent<EntityStats>();
        characterController = GetComponent<CharacterController>();
        navMeshAgent = GetComponent<NavMeshAgent>();

        locomotion = new NPCLocomotion(
            characterController,
            navMeshAgent,
            engagementDistance
            );

        damageController = new DamageController(
            stats,
            locomotion
            );

        if(meleeStyle != null && meleeHitbox != null)
        {
            combatController = new NpcCombatController(
            meleeStyle,
            meleeHitbox,
            transform);
        }

        NpcActionContext actionContext = new NpcActionContext(
            transform,
            locomotion,
            combatController,
            damageController,
            stats);

        actions = new NpcActionController(actionContext);
    }

    private void OnEnable()
    {
        stats.Died += Death;
    }

    private void Start()
    {
        SwitchDispositionBehavior();
    }

    private void Update()
    {
        UpdateAwareness();

        locomotion.SetTarget(CurrentTarget);
        locomotion.Update();

        currentBehavior?.OnUpdate();
        actions.Update();
    }

    private void OnDisable()
    {
        ReleaseCurrentRangedPosition();
        actions?.EndAction();
        stats.Died -= Death;
    }
    #endregion


    #region DISPOSITION & AWARENESS
    private NpcDispositionBehavior CreateDispositionBehavior()
    {
        switch (disposition)
        {
            case NpcDispositionType.Hostile:
                return new HostileNpcDispositionBehavior(this);

            default:
                return null;
        }
    }

    private void SwitchDispositionBehavior()
    {
        currentBehavior?.OnExit();

        currentBehavior = CreateDispositionBehavior();

        currentBehavior?.OnEnter();
    }

    public void SetDisposition(NpcDispositionType newDisposition)
    {
        if (disposition == newDisposition)
            return;

        NpcDispositionType previousDisposition = disposition;
        disposition = newDisposition;

        bool isHostile =
            disposition == NpcDispositionType.Hostile;

        if (!isHostile)
        {
            SetAwareness(NpcAwareness.Unaware);
            actions?.EndAction();
        }

        SwitchDispositionBehavior();

        DispositionChanged?.Invoke(
            previousDisposition,
            disposition);
    }

    private void UpdateAwareness()
    {
        if (disposition != NpcDispositionType.Hostile)
        {
            if (currentTargetMember != null)
            {
                ReleaseCurrentRangedPosition();
                currentTargetMember = null;
                actions?.EndAction();
            }

            SetAwareness(NpcAwareness.Unaware);
            return;
        }

        targetScanTimer -= Time.deltaTime;

        if (targetScanTimer > 0f)
            return;

        targetScanTimer = Mathf.Max(0.05f, targetScanInterval);

        FactionMember newTarget =
            IsValidTarget(currentTargetMember,
            Mathf.Max(awarenessDistance, targetRetentionDistance))
                ? currentTargetMember
                : FindClosestHostileTarget();

        if (newTarget != currentTargetMember)
        {
            ReleaseCurrentRangedPosition();

            currentTargetMember = newTarget;
            actions?.EndAction();
        }

        SetAwareness(
            currentTargetMember != null
                ? NpcAwareness.Aware
                : NpcAwareness.Unaware);
    }

    private FactionMember FindClosestHostileTarget()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            awarenessDistance,
            targetScanBuffer,
            ~0,
            QueryTriggerInteraction.Collide);

        FactionMember closestTarget = null;
        float closestDistanceSquared = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            FactionMember candidate =
                targetScanBuffer[i].GetComponentInParent<FactionMember>();

            if (!IsValidTarget(candidate, awarenessDistance))
                continue;

            float distanceSquared =
                (candidate.transform.position - transform.position).sqrMagnitude;

            if (distanceSquared < closestDistanceSquared)
            {
                closestDistanceSquared = distanceSquared;
                closestTarget = candidate;
            }
        }

        return closestTarget;
    }

    private bool IsValidTarget(
        FactionMember candidate,
        float maxDistance)
    {
        if (candidate == null ||
            candidate == factionMember ||
            !factionMember.IsHostileTo(candidate))
        {
            return false;
        }

        EntityStats candidateStats =
            candidate.GetComponent<EntityStats>();

        if (candidateStats != null && candidateStats.IsDead)
            return false;

        float maxDistanceSquared = maxDistance * maxDistance;

        return (candidate.transform.position - transform.position).sqrMagnitude
               <= maxDistanceSquared;
    }

    private void SetAwareness(NpcAwareness newAwareness)
    {
        if (awareness == newAwareness)
            return;

        NpcAwareness previousAwareness = awareness;
        awareness = newAwareness;

        currentBehavior?.OnAwarenessChanged(
            previousAwareness,
            awareness);
    }
    #endregion


    #region ENGAGEMENT RANGE
    public void SetEngagementDistance(float distance)
    {
        engagementDistance = Mathf.Max(0f, distance);
        locomotion?.SetEngagementDistance(engagementDistance);
    }

    private void ReleaseCurrentRangedPosition()
    {
        if (currentTargetMember == null)
            return;

        NpcEngagementCoordinator coordinator =
            currentTargetMember.GetComponent<NpcEngagementCoordinator>();

        coordinator?.ReleaseRangedPosition(this);
    }
    #endregion


    public void ReceiveDamage(DamageInfo damageInfo)
    {
        damageController.ReceiveDamage(damageInfo);
    }

    void Death()
    {
        Object.Destroy(this.gameObject);
    }

    #region DEBUG GIZMOS
    private void OnDrawGizmosSelected()
    {
        if (engagementDistance <= 0f)
            return;

        const int segments = 48;
        float angleStep = 2f * Mathf.PI / segments;

        Color previousColor = Gizmos.color;
        Gizmos.color = Color.yellow;

        Vector3 previousPoint =
            transform.position +
            new Vector3(engagementDistance, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i;

            Vector3 nextPoint =
                transform.position +
                new Vector3(
                    Mathf.Cos(angle) * engagementDistance,
                    0f,
                    Mathf.Sin(angle) * engagementDistance);

            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }

        Gizmos.color = previousColor;
    }
    #endregion
}

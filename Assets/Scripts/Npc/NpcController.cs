using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

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
    [Tooltip("A new hostile must be this much closer before the NPC switches targets.")]
    [SerializeField, Min(0f)]
    private float targetSwitchDistanceAdvantage = 1.5f;
    [Tooltip("How long an attacker stays the NPC's priority target after hitting it.")]
    [SerializeField, Min(0f)]
    private float attackerTargetPriorityDuration = 2f;

    private float attackerTargetPriorityTimer;
    [SerializeField] private NpcAwareness awareness = NpcAwareness.Unaware;
    public NpcAwareness Awareness => awareness;

    [Header("Disposition")]
    [SerializeField]
    private NpcDispositionType disposition = NpcDispositionType.Hostile;
    public NpcDispositionType Disposition => disposition;
    public event System.Action<NpcDispositionType, NpcDispositionType>
        DispositionChanged;
    private NpcDispositionBehavior currentBehavior;

    [Header("Maneuvering")]
    [SerializeField, Min(0f)]
    private float attackApproachPadding = 4f;

    [SerializeField, Min(0f)]
    private float zigZagLateralDistance = 1.25f;

    [SerializeField, Min(0.5f)]
    private float zigZagWaypointSpacing = 3f;

    public float AttackApproachPadding =>
        attackApproachPadding;

    public float ZigZagLateralDistance =>
        zigZagLateralDistance;

    public float ZigZagWaypointSpacing =>
        zigZagWaypointSpacing;


    [Header("Ranged Combat")]
    // RANGED
    [SerializeField] private RangedAttackData rangedAttackData;
    [SerializeField] private Transform projectileSpawnPoint;
    public RangedAttackData RangedAttack => rangedAttackData;
    public Transform ProjectileSpawnPoint => projectileSpawnPoint;
    [Tooltip("Desired horizontal distance to maintain while using ranged attacks.")]
    [SerializeField, Min(0f)]
    private float preferredRangedDistance = 8f;
    [Tooltip("Allowed distance variation around the preferred ranged distance.")]
    [SerializeField, Min(0f)]
    private float rangedDistanceTolerance = 0.5f;
    public float PreferredRangedDistance => preferredRangedDistance;
    public float RangedDistanceTolerance => rangedDistanceTolerance;

    // MELEE
    [Header("Melee Combat")]
    [SerializeField, Min(0f)]
    private float meleeEngagementDistance = 2f;
    public float MeleeEngagementDistance => meleeEngagementDistance;
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
            meleeEngagementDistance
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
        ReleaseCurrentApproachPosition();
        actions?.EndAction();
        stats.Died -= Death;
    }
    #endregion


    #region TARGETING
    private void SetCurrentTarget(FactionMember newTarget)
    {
        if (currentTargetMember == newTarget)
            return;

        ReleaseCurrentApproachPosition();
        currentTargetMember = newTarget;
        actions?.EndAction();
    }

    private void RetargetToAttacker(Transform sourceTransform)
    {
        if (disposition != NpcDispositionType.Hostile ||
            sourceTransform == null)
        {
            return;
        }

        FactionMember attacker =
            sourceTransform.GetComponentInParent<FactionMember>();

        if (!IsHostileTarget(attacker))
            return;

        if (combatController != null &&
            currentTargetMember != null &&
            IsHostileTarget(currentTargetMember))
        {
            Vector3 toAttacker =
                attacker.transform.position - transform.position;

            Vector3 toCurrentTarget =
                currentTargetMember.transform.position - transform.position;

            toAttacker.y = 0f;
            toCurrentTarget.y = 0f;

            if (toAttacker.sqrMagnitude > toCurrentTarget.sqrMagnitude)
                return;
        }

        SetCurrentTarget(attacker);
        attackerTargetPriorityTimer = attackerTargetPriorityDuration;
        SetAwareness(NpcAwareness.Aware);
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

    private bool IsMeaningfullyCloser(
    FactionMember challenger,
    FactionMember currentTarget)
    {
        float challengerDistance =
            Vector3.Distance(transform.position, challenger.transform.position);

        float currentDistance =
            Vector3.Distance(transform.position, currentTarget.transform.position);

        return challengerDistance + targetSwitchDistanceAdvantage <
               currentDistance;
    }

    private bool IsHostileTarget(FactionMember candidate)
    {
        if (candidate == null ||
            candidate == factionMember ||
            !factionMember.IsHostileTo(candidate))
        {
            return false;
        }

        EntityStats candidateStats =
            candidate.GetComponent<EntityStats>();

        return candidateStats == null || !candidateStats.IsDead;
    }

    private bool IsValidTarget(
        FactionMember candidate,
        float maxDistance)
    {
        if (!IsHostileTarget(candidate))
            return false;

        float maxDistanceSquared = maxDistance * maxDistance;

        return (candidate.transform.position - transform.position).sqrMagnitude
               <= maxDistanceSquared;
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
                ReleaseCurrentApproachPosition();
                currentTargetMember = null;
                actions?.EndAction();
            }

            SetAwareness(NpcAwareness.Unaware);
            return;
        }

        attackerTargetPriorityTimer = Mathf.Max(
            0f,
            attackerTargetPriorityTimer - Time.deltaTime);

        targetScanTimer -= Time.deltaTime;

        if (targetScanTimer > 0f)
            return;

        targetScanTimer = Mathf.Max(0.05f, targetScanInterval);

        float targetRange =
            Mathf.Max(awarenessDistance, targetRetentionDistance);

        bool currentTargetIsValid =
            IsValidTarget(currentTargetMember, targetRange) ||
            (attackerTargetPriorityTimer > 0f &&
             IsHostileTarget(currentTargetMember));

        FactionMember closestTarget =
            FindClosestHostileTarget();

        FactionMember newTarget =
            currentTargetIsValid
                ? currentTargetMember
                : closestTarget;

        if (currentTargetIsValid &&
            closestTarget != null &&
            closestTarget != currentTargetMember &&
            attackerTargetPriorityTimer <= 0f &&
            IsMeaningfullyCloser(closestTarget, currentTargetMember))
        {
            newTarget = closestTarget;
        }

        if (newTarget != currentTargetMember)
            SetCurrentTarget(newTarget);

        SetAwareness(
            currentTargetMember != null
                ? NpcAwareness.Aware
                : NpcAwareness.Unaware);
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
        meleeEngagementDistance = Mathf.Max(0f, distance);
        locomotion?.SetEngagementDistance(meleeEngagementDistance);
    }

    private void ReleaseCurrentApproachPosition()
    {
        if (currentTargetMember == null)
            return;

        NpcEngagementCoordinator coordinator =
            currentTargetMember.GetComponent<NpcEngagementCoordinator>();

        coordinator?.ReleaseRangedPosition(this);
        coordinator?.ReleaseMeleePosition(this);
    }
    #endregion


    public void ReceiveDamage(DamageInfo damageInfo)
    {
        damageController.ReceiveDamage(damageInfo);

        if (stats != null && stats.IsDead)
            return;

        RetargetToAttacker(damageInfo.SourceTransform);
    }

    void Death()
    {
        Object.Destroy(this.gameObject);
    }

    #region DEBUG GIZMOS
    private void OnDrawGizmosSelected()
    {
        if (meleeEngagementDistance <= 0f)
            return;

        const int segments = 48;
        float angleStep = 2f * Mathf.PI / segments;

        Color previousColor = Gizmos.color;
        Gizmos.color = Color.yellow;

        Vector3 previousPoint =
            transform.position +
            new Vector3(meleeEngagementDistance, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i;

            Vector3 nextPoint =
                transform.position +
                new Vector3(
                    Mathf.Cos(angle) * meleeEngagementDistance,
                    0f,
                    Mathf.Sin(angle) * meleeEngagementDistance);

            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }

        Gizmos.color = previousColor;
    }
    #endregion
}

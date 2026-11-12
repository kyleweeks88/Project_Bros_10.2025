using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

[RequireComponent(typeof(EntityStats))]
[RequireComponent(typeof(FactionMember))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NavMeshAgent))]
public class NpcController : MonoBehaviour, ITargetable, IDamageable
{
    #region PROPERTIES
    [Header("Awareness")]
    [SerializeField, Min(0f)]
    private float awarenessDistance = 10f;
    [Tooltip("How far an NPC will chase a target after detecting it.")]
    [SerializeField] private NpcAwareness awareness = NpcAwareness.Unaware;
    public NpcAwareness Awareness => awareness;


    [Header("Temperament")]
    [SerializeField, FormerlySerializedAs("disposition")]
    private NpcTemperamentType temperament = NpcTemperamentType.Hostile;
    public NpcTemperamentType Temperament => temperament;
    public event System.Action<NpcTemperamentType, NpcTemperamentType>
        TemperamentChanged;
    private NpcTemperamentBehavior currentTemperamentBehavior;


    [Header("Target Priority")]
    [SerializeField, Min(0f)]
    private float targetRetentionDistance = 20f;
    [Tooltip("A new hostile must be this much closer before the NPC switches targets.")]
    [SerializeField, Min(0f)]
    private float targetSwitchDistanceAdvantage = 1.5f;
    [Tooltip("How long an attacker stays the NPC's priority target after hitting it.")]
    [SerializeField, Min(0f)]
    private float attackerTargetPriorityDuration = 2f;
    private float attackerTargetPriorityTimer;
    [SerializeField, Min(0f)]
    private float targetScanInterval = 0.25f;
    private float targetScanTimer;
    private readonly Collider[] targetScanBuffer = new Collider[32];


    [Header("Combat Disposition")]
    [Tooltip("Creates this NPC's runtime combat decision behavior. If empty, uses the default behavior.")]
    [SerializeField]
    private NpcCombatDispositionData combatDispositionData;

    public float AttackApproachPadding =>
        combatDispositionData != null
            ? combatDispositionData.AttackApproachPadding
            : 4f;

    public float ZigZagLateralDistance =>
        combatDispositionData != null
            ? combatDispositionData.ZigZagLateralDistance
            : 1.25f;

    public float ZigZagWaypointSpacing =>
        combatDispositionData != null
            ? combatDispositionData.ZigZagWaypointSpacing
            : 3f;

    public NpcMeleeApproachPreference MeleeApproachPreference =>
        combatDispositionData != null
            ? combatDispositionData.MeleeApproachPreference
            : NpcMeleeApproachPreference.Any;

    public float PreferredMeleeApproachChance =>
        combatDispositionData != null
            ? combatDispositionData.PreferredMeleeApproachChance
            : 100f;

    public float MeleeSlotSearchRetreatDistance =>
        combatDispositionData != null
            ? combatDispositionData.MeleeSlotSearchRetreatDistance
            : 3f;

    public float MeleeSlotSearchDuration =>
        combatDispositionData != null
            ? combatDispositionData.MeleeSlotSearchDuration
            : 2f;

    public float MeleeSlotSearchRetryInterval =>
        combatDispositionData != null
            ? combatDispositionData.MeleeSlotSearchRetryInterval
            : 0.3f;

    public float MeleeEngagementDistance =>
        combatDispositionData != null
            ? combatDispositionData.MeleeEngagementDistance
            : 2f;

    public float PreferredRangedDistance =>
        combatDispositionData != null
            ? combatDispositionData.PreferredRangedDistance
            : 8f;

    public float RangedDistanceTolerance =>
        combatDispositionData != null
            ? combatDispositionData.RangedDistanceTolerance
            : 0.5f;


    [Header("Ranged Combat")]
    // RANGED
    [SerializeField] private RangedAttackData rangedAttackData;
    [SerializeField] private Transform projectileSpawnPoint;
    public RangedAttackData RangedAttack => rangedAttackData;
    public Transform ProjectileSpawnPoint => projectileSpawnPoint;

    // MELEE
    [Header("Melee Combat")]
    [SerializeField] private MeleeStyleData meleeStyle;
    [SerializeField] private GameObject meleeHitbox;


    // BLOCKING
    public bool IsBlocking =>
        actions != null &&
        actions.CurrentAction?.ActionType == NpcActionType.Block &&
        locomotion != null &&
        locomotion.IsGrounded;

    // COMPONENTS
    public Transform TargetTransform => this.transform;

    private FactionMember factionMember;
    private FactionMember currentTargetMember;
    private FactionMember provokedTargetMember;

    public Transform CurrentTarget =>
        currentTargetMember != null
            ? currentTargetMember.transform
            : null;

    private EntityStats stats;
    private DamageController damageController;
    private NPCLocomotion locomotion;
    private CharacterController characterController;
    private NavMeshAgent navMeshAgent;
    private NpcMeleeController meleeController;
    private NpcActionController actions;
    private NpcCombatDisposition combatDisposition;

    public NpcMeleeController MeleeController => meleeController;
    public NpcCombatDisposition CombatDisposition => combatDisposition;
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
            MeleeEngagementDistance
            );

        damageController = new DamageController(
            stats,
            locomotion
            );

        if(meleeStyle != null && meleeHitbox != null)
        {
            meleeController = new NpcMeleeController(
            meleeStyle,
            meleeHitbox,
            transform);
        }

        NpcActionContext actionContext = new NpcActionContext(
            transform,
            locomotion,
            meleeController,
            damageController,
            stats);

        actions = new NpcActionController(actionContext);
        combatDisposition = combatDispositionData != null
            ? combatDispositionData.CreateRuntime(this)
            : null;

        if (combatDisposition == null)
            combatDisposition = new DefaultNpcCombatDisposition(this);
        damageController.KnockbackActionCancelRequested += 
            CancelAttackOnKnockback;
        navMeshAgent.speed = stats.MoveSpeed;
    }

    private void OnEnable()
    {
        stats.Died += Death;
    }

    private void Start()
    {
        SwitchTemperamentBehavior();
    }

    private void Update()
    {
        UpdateAwareness();

        locomotion.SetTarget(CurrentTarget);
        locomotion.Update();

        currentTemperamentBehavior?.OnUpdate();
        if (temperament == NpcTemperamentType.Hostile)
            combatDisposition?.OnUpdate();

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
        combatDisposition?.OnTargetChanged();
        actions?.EndAction();
    }

    private void RetargetToAttacker(Transform sourceTransform)
    {
        if (temperament != NpcTemperamentType.Hostile ||
            sourceTransform == null)
        {
            return;
        }

        FactionMember attacker =
            sourceTransform.GetComponentInParent<FactionMember>();

        if (!IsHostileTarget(attacker))
            return;

        if (meleeController != null &&
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

    private FactionMember FindClosestAwarenessTarget()
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

            if (!IsValidAwarenessTarget(candidate, awarenessDistance))
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
        if (candidate == null || candidate == factionMember)
        {
            return false;
        }

        EntityStats candidateStats =
            candidate.GetComponent<EntityStats>();

        if (candidateStats != null && candidateStats.IsDead)
            return false;

        return candidate == provokedTargetMember ||
               factionMember.IsHostileTo(candidate);
    }

    private bool IsAwarenessTarget(FactionMember candidate)
    {
        if (temperament == NpcTemperamentType.Hostile)
            return IsHostileTarget(candidate);

        if (candidate == null || candidate == factionMember)
            return false;

        EntityStats candidateStats =
            candidate.GetComponent<EntityStats>();

        return candidateStats == null || !candidateStats.IsDead;
    }

    private bool IsValidAwarenessTarget(
        FactionMember candidate,
        float maxDistance)
    {
        if (!IsAwarenessTarget(candidate))
            return false;

        float maxDistanceSquared = maxDistance * maxDistance;

        return (candidate.transform.position - transform.position).sqrMagnitude
               <= maxDistanceSquared;
    }
    #endregion


    #region TEMPERAMENT & AWARENESS
    private NpcTemperamentBehavior CreateTemperamentBehavior()
    {
        switch (temperament)
        {
            case NpcTemperamentType.Hostile:
                return new HostileNpcTemperamentBehavior(this);

            case NpcTemperamentType.Docile:
                return new DocileNpcTemperamentBehavior(this);

            case NpcTemperamentType.Provokable:
                return new ProvokableNpcTemperamentBehavior(this);

            default:
                return null;
        }
    }

    private void SwitchTemperamentBehavior()
    {
        currentTemperamentBehavior?.OnExit();
        combatDisposition?.OnExit();

        currentTemperamentBehavior = CreateTemperamentBehavior();

        currentTemperamentBehavior?.OnEnter();
        if (temperament == NpcTemperamentType.Hostile)
            combatDisposition?.OnEnter();
    }

    public void SetTemperament(NpcTemperamentType newTemperament)
    {
        if (temperament == newTemperament)
            return;

        NpcTemperamentType previousTemperament = temperament;
        temperament = newTemperament;

        if (temperament != NpcTemperamentType.Hostile)
        {
            ReleaseCurrentApproachPosition();
            actions?.EndAction();
            provokedTargetMember = null;
        }
        else if (currentTargetMember != null &&
                 !IsHostileTarget(currentTargetMember))
        {
            SetCurrentTarget(null);
            SetAwareness(NpcAwareness.Unaware);
        }

        SwitchTemperamentBehavior();

        TemperamentChanged?.Invoke(
            previousTemperament,
            temperament);
    }

    private void UpdateAwareness()
    {
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
            IsValidAwarenessTarget(currentTargetMember, targetRange) ||
            (attackerTargetPriorityTimer > 0f &&
             IsHostileTarget(currentTargetMember));

        FactionMember closestTarget =
            FindClosestAwarenessTarget();

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

        currentTemperamentBehavior?.OnAwarenessChanged(
            previousAwareness,
            awareness);
        combatDisposition?.OnAwarenessChanged(
            previousAwareness,
            awareness);
    }
    #endregion


    #region ENGAGEMENT RANGE
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


    private void CancelAttackOnKnockback()
    {
        if(actions?.CurrentAction == null) 
            return;

        NpcActionType actionType =
            actions.CurrentAction.ActionType;

        if(actionType == NpcActionType.MeleeAttack ||
            actionType == NpcActionType.RangedAttack)
        {
            actions.EndAction();
        }
    }


    public void ReceiveDamage(DamageInfo damageInfo)
    {
        damageController.ReceiveDamage(
            damageInfo,
            transform,
            IsBlocking);

        if (stats != null && stats.IsDead)
            return;

        currentTemperamentBehavior?.OnAttacked(
            damageInfo.SourceTransform);

        if(temperament == NpcTemperamentType.Hostile)
        {
            RetargetToAttacker(damageInfo.SourceTransform);
            combatDisposition?.OnAttacked(damageInfo.SourceTransform);
        }
    }

    public void BecomeHostileToAttacker(Transform sourceTransform)
    {
        FactionMember attacker = sourceTransform != null
            ? sourceTransform.GetComponentInParent<FactionMember>()
            : null;

        if (attacker != null && attacker != factionMember)
            provokedTargetMember = attacker;

        SetTemperament(NpcTemperamentType.Hostile);

        if (attacker != null && IsHostileTarget(attacker))
        {
            SetCurrentTarget(attacker);
            attackerTargetPriorityTimer = attackerTargetPriorityDuration;
            SetAwareness(NpcAwareness.Aware);
        }
    }

    void Death()
    {
        Object.Destroy(this.gameObject);
    }

    #region DEBUG GIZMOS
    private void OnDrawGizmosSelected()
    {
        float engagementDistance = MeleeEngagementDistance;

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

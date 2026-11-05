using UnityEngine;
using UnityEngine.AI;

public class HostileNpcDispositionBehavior : NpcDispositionBehavior
{
    private const float SightCheckInterval = 0.2f;
    private float sightCheckTimer;
    private bool hasCachedClearShot;
    private Vector3 cachedRangedAimPoint;

    private const int AngleSearchCandidateCount = 8;
    private const float AngleSearchSampleRadius = 2f;
    private const float AngleSearchRetryInterval = 0.4f;

    private bool hasLastSeenPosition;
    private Vector3 lastSeenTargetPosition;
    private Vector3 lastSeenAimPoint;

    private bool movingToSearchPoint;
    private float angleSearchTimer;
    private int nextSearchCandidate;

    private Transform trackedTarget;

    public HostileNpcDispositionBehavior(NpcController npc)
        : base(npc)
    { 
    }

    // WHEN THE NPC ENTERS THE HOSTILE STATE
    public override void OnEnter()
    {
        // CHECK IF THE NPC IS AWARE OF A TARGET
        // IF SO, SetChasing IS SET TRUE
        npc.Locomotion?.SetChasing(
            npc.Awareness == NpcAwareness.Aware);
    }

    public override void OnUpdate()
    {
        if (npc.Awareness != NpcAwareness.Aware)
            return;

        Transform target = npc.CurrentTarget;

        if (target == null)
            return;

        if (target != trackedTarget)
        {
            trackedTarget = target;
            sightCheckTimer = 0f;
            hasCachedClearShot = false;
            hasLastSeenPosition = false;
            movingToSearchPoint = false;
            angleSearchTimer = 0f;
            nextSearchCandidate = 0;
        }

        Vector3 toTarget = target.position - npc.transform.position;
        toTarget.y = 0f;

        float distanceSquared = toTarget.sqrMagnitude;

        MeleeAttackData meleeAttackData = null;

        bool hasMeleeAttack =
            npc.CombatController != null &&
            npc.CombatController.TryGetAttackData(
                MeleeAttackType.LightAttack1,
                out meleeAttackData);

        bool useMelee =
            hasMeleeAttack &&
            distanceSquared <=
                npc.EngagementDistance * npc.EngagementDistance;

        if (useMelee)
        {
            movingToSearchPoint = false;
            npc.Locomotion.SetChasing(false);

            if (!npc.Locomotion.FaceTarget(target.position) ||
                npc.Actions == null ||
                npc.Actions.IsBusy)
            {
                return;
            }

            npc.Actions.TryStartAction(
                new NpcMeleeAttackAction(
                    npc.Actions.Context,
                    meleeAttackData));

            return;
        }

        bool withinRangedDistance =
            npc.RangedAttack != null &&
            distanceSquared <=
                npc.RangedAttack.MaxRange * npc.RangedAttack.MaxRange;

        if (!withinRangedDistance)
        {
            sightCheckTimer = 0f;
            hasCachedClearShot = false;
            movingToSearchPoint = false;
            npc.Locomotion.ChaseTarget();
            return;
        }

        sightCheckTimer -= Time.deltaTime;

        if (sightCheckTimer <= 0f)
        {
            hasCachedClearShot =
                TryGetVisibleAimPoint(
                    target,
                    out cachedRangedAimPoint);

            if (hasCachedClearShot)
            {
                lastSeenTargetPosition = target.position;
                lastSeenAimPoint = cachedRangedAimPoint;
                hasLastSeenPosition = true;
            }

            sightCheckTimer = SightCheckInterval;
        }

        if (hasCachedClearShot)
        {
            movingToSearchPoint = false;

            if (AdjustRangedDistance(target))
                return;

            if (!npc.Locomotion.FaceTarget(target.position) ||
                npc.Actions == null ||
                npc.Actions.IsBusy)
            {
                return;
            }

            npc.Actions.TryStartAction(
                new NpcRangedAttackAction(
                    npc.Actions.Context,
                    npc.RangedAttack,
                    npc.ProjectileSpawnPoint,
                    target,
                    cachedRangedAimPoint));

            return;
        }

        UpdateAngleSearch(target);
    }

    private bool AdjustRangedDistance(Transform target)
    {
        float preferredDistance =
            Mathf.Max(0.1f, npc.PreferredRangedDistance);

        float tolerance =
            Mathf.Max(0f, npc.RangedDistanceTolerance);

        NpcEngagementCoordinator coordinator =
            target.GetComponent<NpcEngagementCoordinator>();

        if (coordinator == null)
        {
            npc.Locomotion.SetChasing(false);
            return false;
        }

        if (!coordinator.TryGetRangedPosition(
        npc,
        preferredDistance,
        out Vector3 desiredPosition))
        {
            npc.Locomotion.SetChasing(false);
            return false;
        }

        if (!NavMesh.SamplePosition(
                desiredPosition,
                out NavMeshHit navHit,
                AngleSearchSampleRadius,
                npc.Locomotion.NavigationAreaMask))
        {
            npc.Locomotion.SetChasing(false);
            return false;
        }

        desiredPosition = navHit.position;

        Vector3 offset = desiredPosition - npc.transform.position;
        offset.y = 0f;

        float arrivalTolerance = Mathf.Max(0.25f, tolerance);

        if (offset.sqrMagnitude <= arrivalTolerance * arrivalTolerance)
        {
            npc.Locomotion.SetChasing(false);
            return false;
        }

        npc.Locomotion.MoveTo(desiredPosition);
        return true;
    }

    public override void OnAwarenessChanged(
        NpcAwareness previousAwareness,
        NpcAwareness newAwareness)
    {
        npc.Locomotion?.SetChasing(
            newAwareness == NpcAwareness.Aware);

        if (newAwareness == NpcAwareness.Unaware)
            npc.Actions?.EndAction();
    }

    public override void OnExit()
    {
        npc.Locomotion?.SetChasing(false);
    }

    private bool TryGetVisibleAimPoint(
        Transform target,
        out Vector3 aimPoint)
    {
        Vector3 origin = npc.ProjectileSpawnPoint != null
            ? npc.ProjectileSpawnPoint.position
            : npc.transform.position;

        if (!target.TryGetComponent<Collider>(
                out Collider targetCollider))
        {
            aimPoint = target.position;
            return HasClearRayToPoint(target, origin, aimPoint);
        }

        Bounds bounds = targetCollider.bounds;
        Vector3 center = bounds.center;

        if (HasClearRayToPoint(target, origin, center))
        {
            aimPoint = center;
            return true;
        }

        Vector3 upperPoint =
            center + Vector3.up * bounds.extents.y * 0.6f;

        if (HasClearRayToPoint(target, origin, upperPoint))
        {
            aimPoint = upperPoint;
            return true;
        }

        Vector3 sideOffset =
            target.right * bounds.extents.x * 0.6f;

        Vector3 leftPoint = center - sideOffset;

        if (HasClearRayToPoint(target, origin, leftPoint))
        {
            aimPoint = leftPoint;
            return true;
        }

        Vector3 rightPoint = center + sideOffset;

        if (HasClearRayToPoint(target, origin, rightPoint))
        {
            aimPoint = rightPoint;
            return true;
        }

        aimPoint = center;
        return false;
    }

    private bool HasClearRayToPoint(
        Transform target,
        Vector3 origin,
        Vector3 targetPoint)
    {
        Vector3 toPoint = targetPoint - origin;
        float distance = toPoint.magnitude;

        if (distance <= 0.001f)
            return true;

        float radius = npc.RangedAttack.ProjectileClearanceRadius;

        if (!Physics.SphereCast(
                origin,
                radius,
                toPoint / distance,
                out RaycastHit hit,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        return hit.transform == target ||
               hit.transform.IsChildOf(target);
    }

    private void UpdateAngleSearch(Transform target)
    {
        if (!hasLastSeenPosition)
        {
            npc.Locomotion.ChaseTarget();
            return;
        }

        if (movingToSearchPoint &&
            !npc.Locomotion.HasReachedMoveDestination)
        {
            return;
        }

        movingToSearchPoint = false;
        angleSearchTimer -= Time.deltaTime;

        if (angleSearchTimer > 0f)
            return;

        if (TryFindAngleSearchPosition(
                target,
                out Vector3 searchPosition))
        {
            npc.Locomotion.MoveTo(searchPosition);
            movingToSearchPoint = true;
        }
        else
        {
            npc.Locomotion.ChaseTarget();
        }

        angleSearchTimer = AngleSearchRetryInterval;
    }

    private bool TryFindAngleSearchPosition(
        Transform target,
        out Vector3 searchPosition)
    {
        searchPosition = default;

        Vector3 awayFromTarget =
            npc.transform.position - lastSeenTargetPosition;
        awayFromTarget.y = 0f;

        if (awayFromTarget.sqrMagnitude <= 0.001f)
            awayFromTarget = -npc.transform.forward;

        awayFromTarget.Normalize();

        float searchRadius = Mathf.Max(
            0.5f,
            Mathf.Min(
                npc.PreferredRangedDistance,
                npc.RangedAttack.MaxRange));

        Vector3 localSpawnOffset =
            npc.ProjectileSpawnPoint != null
                ? npc.transform.InverseTransformPoint(
                    npc.ProjectileSpawnPoint.position)
                : Vector3.up;

        bool foundFallback = false;
        Vector3 fallbackPosition = default;

        bool foundClearShotPosition = false;
        Vector3 bestClearShotPosition = default;
        float bestClearShotDistanceSquared = float.MaxValue;

        int startIndex = nextSearchCandidate;
        nextSearchCandidate =
            (nextSearchCandidate + 1) %
            AngleSearchCandidateCount;

        for (int offset = 0; offset < AngleSearchCandidateCount; offset++)
        {
            int candidateIndex =
                (startIndex + offset) % AngleSearchCandidateCount;

            float angle = candidateIndex * 45f;

            Vector3 radialDirection =
                Quaternion.AngleAxis(angle, Vector3.up) *
                awayFromTarget;

            Vector3 rawCandidate =
                lastSeenTargetPosition +
                radialDirection * searchRadius;

            if (!NavMesh.SamplePosition(
                    rawCandidate,
                    out NavMeshHit navHit,
                    AngleSearchSampleRadius,
                    npc.Locomotion.NavigationAreaMask))
            {
                continue;
            }

            Vector3 candidate = navHit.position;

            if (!npc.Locomotion.CanReach(candidate))
                continue;

            if ((candidate - npc.transform.position).sqrMagnitude < 0.25f)
                continue;

            if (!foundFallback)
            {
                fallbackPosition = candidate;
                foundFallback = true;
            }

            Vector3 facingDirection =
                lastSeenAimPoint - candidate;
            facingDirection.y = 0f;

            if (facingDirection.sqrMagnitude <= 0.001f)
                facingDirection = -radialDirection;

            Quaternion candidateRotation =
                Quaternion.LookRotation(facingDirection.normalized);

            Vector3 candidateMuzzlePosition =
                candidate +
                candidateRotation * localSpawnOffset;

            bool candidateHasClearShot =
                HasClearRayToPoint(
                    target,
                    candidateMuzzlePosition,
                    lastSeenAimPoint);

            if (!candidateHasClearShot)
                continue;

            float candidateDistanceSquared =
                (candidate - npc.transform.position).sqrMagnitude;

            if (candidateDistanceSquared <
                bestClearShotDistanceSquared)
            {
                bestClearShotDistanceSquared =
                    candidateDistanceSquared;
                bestClearShotPosition = candidate;
                foundClearShotPosition = true;
            }
        }

        if (foundClearShotPosition)
        {
            searchPosition = bestClearShotPosition;
            return true;
        }

        if (foundFallback)
        {
            searchPosition = fallbackPosition;
            return true;
        }

        return false;
    }
}
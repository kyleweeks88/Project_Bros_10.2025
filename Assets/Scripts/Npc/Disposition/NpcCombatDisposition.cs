using UnityEngine;
using UnityEngine.AI;

public abstract class NpcCombatDisposition
{
    protected readonly NpcController npc;
    protected readonly NpcMeleeApproachPlanner meleeApproachPlanner;

    private const float ApproachWaypointSampleRadius = 2f;
    private const float ApproachWaypointTargetReevaluationDistance = 1.5f;
    private const int ApproachWaypointSampleAttempts = 5;

    private bool hasApproachWaypoint;
    private Vector3 approachWaypoint;
    private Vector3 targetPositionAtApproachWaypoint;

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
    private bool hasAngleSearchDestination;
    private Vector3 angleSearchDestination;
    private float angleSearchTimer;
    private int nextSearchCandidate;
    private Transform trackedTarget;

    protected NpcCombatDisposition(NpcController npc)
    {
        this.npc = npc;
        meleeApproachPlanner = new NpcMeleeApproachPlanner(npc);
    }

    public virtual void OnEnter() { }

    public abstract void OnUpdate();

    protected void PrepareForTarget(Transform target)
    {
        if (target == trackedTarget)
            return;

        trackedTarget = target;
        sightCheckTimer = 0f;
        hasCachedClearShot = false;
        hasLastSeenPosition = false;
        movingToSearchPoint = false;
        hasAngleSearchDestination = false;
        angleSearchTimer = 0f;
        nextSearchCandidate = 0;
        hasApproachWaypoint = false;
    }

    protected bool TryGetMeleeAttackData(out MeleeAttackData meleeAttackData)
    {
        meleeAttackData = null;

        return npc.CombatController != null &&
               npc.CombatController.TryGetAttackData(
                   MeleeAttackType.LightAttack1,
                   out meleeAttackData);
    }

    protected bool UpdateMeleeCombat(
        Transform target,
        MeleeAttackData meleeAttackData)
    {
        bool hasMeleePosition =
            TryMoveToMeleePosition(target, out bool reachedMeleePosition);

        if (meleeApproachPlanner.IsSearching)
            return true;

        if (hasMeleePosition && !reachedMeleePosition)
            return true;

        Vector3 toTarget = target.position - npc.transform.position;
        toTarget.y = 0f;

        bool useMelee =
            hasMeleePosition ||
            toTarget.sqrMagnitude <=
                npc.MeleeEngagementDistance * npc.MeleeEngagementDistance;

        if (!useMelee)
            return false;

        npc.Locomotion.SetChasing(false);

        if (!npc.Locomotion.FaceTarget(target.position) ||
            npc.Actions == null ||
            npc.Actions.IsBusy)
        {
            return true;
        }

        npc.Actions.TryStartAction(
            new NpcMeleeAttackAction(
                npc.Actions.Context,
                meleeAttackData));

        return true;
    }

    protected bool UpdateRangedCombat(Transform target)
    {
        Vector3 toTarget = target.position - npc.transform.position;
        toTarget.y = 0f;

        float distanceSquared = toTarget.sqrMagnitude;

        bool withinRangedDistance =
            npc.RangedAttack != null &&
            distanceSquared <=
                npc.RangedAttack.MaxRange * npc.RangedAttack.MaxRange;

        if (!withinRangedDistance)
        {
            sightCheckTimer = 0f;
            hasCachedClearShot = false;
            movingToSearchPoint = false;
            hasAngleSearchDestination = false;
            hasApproachWaypoint = false;
            npc.Locomotion.ChaseTarget();
            return false;
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

        if (!hasCachedClearShot)
        {
            UpdateAngleSearch(target);
            return false;
        }

        bool wasSearchingForAngle = hasAngleSearchDestination;

        movingToSearchPoint = false;
        hasAngleSearchDestination = false;

        if (wasSearchingForAngle)
            hasApproachWaypoint = false;

        if (AdjustRangedDistance(target))
            return false;

        if (!npc.Locomotion.FaceTarget(target.position) ||
            npc.Actions == null ||
            npc.Actions.IsBusy)
        {
            return false;
        }

        return npc.Actions.TryStartAction(
            new NpcRangedAttackAction(
                npc.Actions.Context,
                npc.RangedAttack,
                npc.ProjectileSpawnPoint,
                target,
                cachedRangedAimPoint));
    }

    private void ResetCombatState()
    {
        meleeApproachPlanner.Reset();
        hasApproachWaypoint = false;
        hasAngleSearchDestination = false;
        movingToSearchPoint = false;
        hasCachedClearShot = false;
        hasLastSeenPosition = false;
        trackedTarget = null;
        sightCheckTimer = 0f;
        angleSearchTimer = 0f;
        nextSearchCandidate = 0;
    }

    public virtual void OnTargetChanged()
    {
        ResetCombatState();
    }

    public virtual void OnAwarenessChanged(
        NpcAwareness previousAwareness,
        NpcAwareness newAwareness)
    {
        if (newAwareness == NpcAwareness.Unaware)
            ResetCombatState();
    }

    public virtual void OnExit()
    {
        ResetCombatState();
    }

    protected bool AdjustRangedDistance(Transform target)
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
            hasApproachWaypoint = false;
            npc.Locomotion.SetChasing(false);
            return false;
        }

        MoveTowardsApproachPosition(
            target,
            desiredPosition,
            preferredDistance + npc.AttackApproachPadding);
        return true;
    }

    private bool TryMoveToMeleePosition(
        Transform target,
        out bool reachedPosition)
    {
        if (!meleeApproachPlanner.TryGetMeleePosition(
                target,
                out Vector3 desiredPosition,
                out reachedPosition))
        {
            hasApproachWaypoint = false;
            return false;
        }

        if (reachedPosition)
        {
            hasApproachWaypoint = false;
            npc.Locomotion.SetChasing(false);
            return true;
        }

        MoveTowardsApproachPosition(
            target,
            desiredPosition,
            npc.MeleeEngagementDistance +
            npc.AttackApproachPadding);
        return true;
    }

    private void MoveTowardsApproachPosition(
        Transform target,
        Vector3 finalPosition,
        float directApproachDistance)
    {
        Vector3 offsetToTarget =
            target.position - npc.transform.position;

        offsetToTarget.y = 0f;

        bool shouldZigZag =
            offsetToTarget.sqrMagnitude >
            directApproachDistance * directApproachDistance;

        if (shouldZigZag &&
            TryGetApproachWaypoint(
                target,
                finalPosition,
                out Vector3 waypoint))
        {
            npc.Locomotion.MoveTo(waypoint);
            return;
        }

        hasApproachWaypoint = false;
        npc.Locomotion.MoveTo(finalPosition);
    }

    private bool TryGetApproachWaypoint(
        Transform target,
        Vector3 finalPosition,
        out Vector3 waypoint)
    {
        waypoint = default;

        if (hasApproachWaypoint)
        {
            Vector3 targetMovement =
                target.position - targetPositionAtApproachWaypoint;

            targetMovement.y = 0f;

            float reevaluationDistanceSquared =
                ApproachWaypointTargetReevaluationDistance *
                ApproachWaypointTargetReevaluationDistance;

            if (targetMovement.sqrMagnitude <= reevaluationDistanceSquared &&
                !npc.Locomotion.HasReachedMoveDestination)
            {
                waypoint = approachWaypoint;
                return true;
            }

            hasApproachWaypoint = false;
        }

        Vector3 directionToFinal =
            finalPosition - npc.transform.position;

        directionToFinal.y = 0f;

        float distanceToFinal = directionToFinal.magnitude;

        if (distanceToFinal <= 0.5f)
            return false;

        Vector3 forward = directionToFinal / distanceToFinal;
        Vector3 lateral = Vector3.Cross(Vector3.up, forward);

        float spacing =
            Mathf.Max(0.5f, npc.ZigZagWaypointSpacing);

        float maxLateralOffset =
            Mathf.Max(0f, npc.ZigZagLateralDistance);

        for (int attempt = 0;
             attempt < ApproachWaypointSampleAttempts;
             attempt++)
        {
            float forwardDistance = Mathf.Min(
                distanceToFinal * 0.75f,
                Random.Range(spacing * 0.75f, spacing * 1.25f));

            if (forwardDistance <= 0.5f)
                continue;

            float lateralOffset = maxLateralOffset > 0f
                ? Random.Range(-maxLateralOffset, maxLateralOffset)
                : 0f;

            Vector3 rawCandidate =
                npc.transform.position +
                forward * forwardDistance +
                lateral * lateralOffset;

            if (Vector3.Dot(
                    finalPosition - rawCandidate,
                    forward) <= 0.5f)
            {
                continue;
            }

            if (!NavMesh.SamplePosition(
                    rawCandidate,
                    out NavMeshHit waypointHit,
                    ApproachWaypointSampleRadius,
                    npc.Locomotion.NavigationAreaMask))
            {
                continue;
            }

            Vector3 candidate = waypointHit.position;

            if (!npc.Locomotion.CanReach(candidate))
                continue;

            approachWaypoint = candidate;
            targetPositionAtApproachWaypoint = target.position;
            hasApproachWaypoint = true;

            waypoint = candidate;
            return true;
        }

        return false;
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
            movingToSearchPoint = false;
            hasAngleSearchDestination = false;
            hasApproachWaypoint = false;
            npc.Locomotion.ChaseTarget();
            return;
        }

        if (hasAngleSearchDestination)
        {
            if (movingToSearchPoint &&
                !npc.Locomotion.HasReachedMoveDestination)
            {
                return;
            }

            movingToSearchPoint = false;

            Vector3 offsetToSearchPosition =
                angleSearchDestination - npc.transform.position;

            offsetToSearchPosition.y = 0f;

            if (offsetToSearchPosition.sqrMagnitude <= 0.25f)
            {
                hasAngleSearchDestination = false;
                hasApproachWaypoint = false;
            }
            else
            {
                float directApproachDistance =
                    npc.PreferredRangedDistance +
                    npc.AttackApproachPadding;

                MoveTowardsApproachPosition(
                    target,
                    angleSearchDestination,
                    directApproachDistance);

                movingToSearchPoint = true;
                return;
            }
        }

        // A clear-shot ranged slot may have had a cached waypoint. Start a
        // fresh waypoint chain for angle-search destinations.
        if (!hasAngleSearchDestination)
            hasApproachWaypoint = false;

        angleSearchTimer -= Time.deltaTime;

        if (angleSearchTimer > 0f)
            return;

        if (TryFindAngleSearchPosition(
                target,
                out Vector3 searchPosition))
        {
            angleSearchDestination = searchPosition;
            hasAngleSearchDestination = true;

            float directApproachDistance =
                npc.PreferredRangedDistance +
                npc.AttackApproachPadding;

            MoveTowardsApproachPosition(
                target,
                angleSearchDestination,
                directApproachDistance);

            movingToSearchPoint = true;
        }
        else
        {
            hasApproachWaypoint = false;
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

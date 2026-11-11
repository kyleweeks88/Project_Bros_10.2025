using UnityEngine;
using UnityEngine.AI;

public class NpcMeleeApproachPlanner
{
    private const float SearchArrivalTolerance = 0.4f;
    private const float SearchNavMeshSampleRadius = 2f;
    private const float TargetMovementReevaluationDistance = 1.5f;
    private const int SearchPointSampleAttempts = 6;

    private readonly NpcController npc;

    private Transform trackedTarget;
    private NpcMeleeApproachPreference activePreference;
    private bool isSearching;
    private bool reachedRetreatDistance;
    private bool hasSearchDestination;
    private Vector3 searchDestination;
    private Vector3 targetPositionAtSearchDestination;
    private float preferredSlotRetryTimer;
    private float searchTimer;
    private float searchPointRetryTimer;
    private int searchDirection = 1;

    public bool IsSearching => isSearching;

    public NpcMeleeApproachPlanner(NpcController npc)
    {
        this.npc = npc;
    }

    public bool TryGetMeleePosition(
        Transform target,
        out Vector3 position,
        out bool reachedPosition)
    {
        position = default;
        reachedPosition = false;

        if (target == null)
        {
            Reset();
            return false;
        }

        if (target != trackedTarget)
            BeginForTarget(target);

        NpcEngagementCoordinator coordinator =
            target.GetComponent<NpcEngagementCoordinator>();

        if (coordinator == null)
        {
            EndSearch();
            return false;
        }

        if (coordinator.TryGetMeleePosition(
                npc,
                npc.MeleeEngagementDistance,
                activePreference,
                activePreference != NpcMeleeApproachPreference.Any,
                out position))
        {
            EndSearch();
            reachedPosition = IsAtPosition(position);
            return true;
        }

        if (activePreference == NpcMeleeApproachPreference.Any)
            return false;

        return UpdatePreferredSlotSearch(
            target,
            coordinator,
            out position,
            out reachedPosition);
    }

    public void Reset()
    {
        trackedTarget = null;
        activePreference = NpcMeleeApproachPreference.Any;
        EndSearch();
    }

    private void BeginForTarget(Transform target)
    {
        EndSearch();
        trackedTarget = target;

        NpcMeleeApproachPreference requestedPreference =
            npc.MeleeApproachPreference;

        float preferenceChance = Mathf.Clamp(
            npc.PreferredMeleeApproachChance,
            0f,
            100f);

        bool usePreferredApproach =
            requestedPreference != NpcMeleeApproachPreference.Any &&
            Random.Range(0f, 100f) < preferenceChance;

        activePreference = usePreferredApproach
            ? requestedPreference
            : NpcMeleeApproachPreference.Any;
    }

    private bool UpdatePreferredSlotSearch(
        Transform target,
        NpcEngagementCoordinator coordinator,
        out Vector3 position,
        out bool reachedPosition)
    {
        position = default;
        reachedPosition = false;

        if (!isSearching)
            BeginSearch();

        preferredSlotRetryTimer -= Time.deltaTime;

        if (preferredSlotRetryTimer <= 0f)
        {
            preferredSlotRetryTimer =
                Mathf.Max(0.05f, npc.MeleeSlotSearchRetryInterval);

            if (coordinator.TryGetMeleePosition(
                    npc,
                    npc.MeleeEngagementDistance,
                    activePreference,
                    true,
                    out position))
            {
                EndSearch();
                reachedPosition = IsAtPosition(position);
                return true;
            }
        }

        if (!reachedRetreatDistance)
        {
            if (IsAtSearchDistance(target))
            {
                reachedRetreatDistance = true;
                searchTimer =
                    Mathf.Max(0.1f, npc.MeleeSlotSearchDuration);
                hasSearchDestination = false;
            }
            else
            {
                MoveToRetreatDistance(target);
                return false;
            }
        }

        searchTimer -= Time.deltaTime;

        if (searchTimer <= 0f)
        {
            if (coordinator.TryGetMeleePosition(
                    npc,
                    npc.MeleeEngagementDistance,
                    NpcMeleeApproachPreference.Any,
                    true,
                    out position))
            {
                activePreference = NpcMeleeApproachPreference.Any;
                EndSearch();
                reachedPosition = IsAtPosition(position);
                return true;
            }

            searchTimer =
                Mathf.Max(0.1f, npc.MeleeSlotSearchDuration);
            hasSearchDestination = false;
        }

        searchPointRetryTimer -= Time.deltaTime;

        bool targetMoved =
            (target.position - targetPositionAtSearchDestination).sqrMagnitude >
            TargetMovementReevaluationDistance *
            TargetMovementReevaluationDistance;

        if ((!hasSearchDestination ||
             npc.Locomotion.HasReachedMoveDestination ||
             targetMoved) &&
            searchPointRetryTimer <= 0f)
        {
            if (TryChooseSearchPoint(target, out searchDestination))
            {
                targetPositionAtSearchDestination = target.position;
                hasSearchDestination = true;
                searchPointRetryTimer =
                    Mathf.Max(0.05f, npc.MeleeSlotSearchRetryInterval);
            }
            else
            {
                hasSearchDestination = false;
                searchPointRetryTimer = 0.25f;
            }
        }

        if (hasSearchDestination)
            npc.Locomotion.MoveTo(searchDestination);

        return false;
    }

    private void BeginSearch()
    {
        isSearching = true;
        reachedRetreatDistance = false;
        hasSearchDestination = false;
        preferredSlotRetryTimer = 0f;
        searchTimer = Mathf.Max(0.1f, npc.MeleeSlotSearchDuration);
        searchPointRetryTimer = 0f;
        searchDirection = Random.value < 0.5f ? -1 : 1;
    }

    private void MoveToRetreatDistance(Transform target)
    {
        if (!hasSearchDestination ||
            npc.Locomotion.HasReachedMoveDestination ||
            (target.position - targetPositionAtSearchDestination).sqrMagnitude >
            TargetMovementReevaluationDistance *
            TargetMovementReevaluationDistance)
        {
            Vector3 awayFromTarget = GetAwayDirection(target);
            float currentDistance = GetHorizontalDistanceToTarget(target);
            float desiredDistance =
                npc.MeleeEngagementDistance +
                npc.MeleeSlotSearchRetreatDistance;

            float retreatDistance = Mathf.Max(
                desiredDistance,
                currentDistance + SearchArrivalTolerance);

            Vector3 rawPosition =
                target.position + awayFromTarget * retreatDistance;

            if (TrySampleReachablePosition(rawPosition, out searchDestination))
            {
                targetPositionAtSearchDestination = target.position;
                hasSearchDestination = true;
            }
        }

        if (hasSearchDestination)
            npc.Locomotion.MoveTo(searchDestination);
    }

    private bool IsAtSearchDistance(Transform target)
    {
        float desiredDistance =
            npc.MeleeEngagementDistance +
            npc.MeleeSlotSearchRetreatDistance;

        return GetHorizontalDistanceToTarget(target) >=
                   desiredDistance - SearchArrivalTolerance &&
               (!hasSearchDestination ||
                npc.Locomotion.HasReachedMoveDestination);
    }

    private bool TryChooseSearchPoint(
        Transform target,
        out Vector3 position)
    {
        position = default;

        Vector3 awayFromTarget = GetAwayDirection(target);
        float searchRadius = Mathf.Max(
            npc.MeleeEngagementDistance +
            npc.MeleeSlotSearchRetreatDistance,
            GetHorizontalDistanceToTarget(target));

        for (int attempt = 0;
             attempt < SearchPointSampleAttempts;
             attempt++)
        {
            float angle = Random.Range(35f, 100f) * searchDirection;
            searchDirection *= -1;

            Vector3 direction =
                Quaternion.AngleAxis(angle, Vector3.up) * awayFromTarget;

            Vector3 rawPosition =
                target.position + direction * searchRadius;

            Vector3 lateral = Vector3.Cross(Vector3.up, direction);
            rawPosition += lateral * Random.Range(
                -npc.ZigZagLateralDistance,
                npc.ZigZagLateralDistance);

            if (TrySampleReachablePosition(rawPosition, out position))
                return true;
        }

        return false;
    }

    private bool TrySampleReachablePosition(
        Vector3 rawPosition,
        out Vector3 position)
    {
        if (!NavMesh.SamplePosition(
                rawPosition,
                out NavMeshHit navHit,
                SearchNavMeshSampleRadius,
                npc.Locomotion.NavigationAreaMask))
        {
            position = default;
            return false;
        }

        position = navHit.position;
        return npc.Locomotion.CanReach(position);
    }

    private Vector3 GetAwayDirection(Transform target)
    {
        Vector3 awayFromTarget =
            npc.transform.position - target.position;
        awayFromTarget.y = 0f;

        if (awayFromTarget.sqrMagnitude <= 0.001f)
        {
            awayFromTarget = -target.forward;
            awayFromTarget.y = 0f;
        }

        if (awayFromTarget.sqrMagnitude <= 0.001f)
            awayFromTarget = -npc.transform.forward;

        return awayFromTarget.normalized;
    }

    private float GetHorizontalDistanceToTarget(Transform target)
    {
        Vector3 offset = npc.transform.position - target.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private bool IsAtPosition(Vector3 position)
    {
        Vector3 offset = position - npc.transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= SearchArrivalTolerance *
               SearchArrivalTolerance;
    }

    private void EndSearch()
    {
        isSearching = false;
        reachedRetreatDistance = false;
        hasSearchDestination = false;
        preferredSlotRetryTimer = 0f;
        searchTimer = 0f;
        searchPointRetryTimer = 0f;
    }
}

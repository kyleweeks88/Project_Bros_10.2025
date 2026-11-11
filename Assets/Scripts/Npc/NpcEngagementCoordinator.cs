using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NpcEngagementCoordinator : MonoBehaviour
{
    #region REFERENCES

    #region MELEE POSITIONS
    [Header("Melee Positions")]
    [SerializeField, Min(1)]
    private int meleeSlotCount = 6;

    [SerializeField, Min(0f)]
    private float meleeSlotChoiceSlack = 1f;

    [SerializeField, Min(0f)]
    private float meleeRadiusVariation = 0.35f;

    [SerializeField, Min(0f)]
    private float meleeTargetMovementReevaluationDistance = 1.5f;

    private float meleeSlotAngleOffset;

    private const float MeleeSlotSampleRadius = 2f;

    private readonly Dictionary<NpcController, MeleeSlotAssignment> meleeSlots =
        new Dictionary<NpcController, MeleeSlotAssignment>();

    private readonly List<NpcController> staleMeleeRequesters =
        new List<NpcController>();

    private readonly List<int> availableMeleeSlots =
        new List<int>();

    private readonly List<int> nearBestMeleeSlots =
        new List<int>();
    #endregion

    #region RANGED POSITIONS
    // RANGED
    [Header("Ranged Positions")]
    [SerializeField, Min(1)]
    private int rangedSlotCount = 8;

    [SerializeField, Min(0f)]
    private float rangedSlotChoiceSlack = 2f;

    [SerializeField, Min(0f)]
    private float rangedTargetMovementReevaluationDistance = 2f;

    private float rangedSlotAngleOffset;

    private const float RangedSlotSampleRadius = 2f;

    private readonly List<int> availableRangedSlots =
        new List<int>();

    private readonly List<int> nearBestRangedSlots =
        new List<int>();

    private readonly Dictionary<NpcController, RangedSlotAssignment> rangedSlots =
        new Dictionary<NpcController, RangedSlotAssignment>();

    private readonly List<NpcController> staleRangedRequesters =
        new List<NpcController>();
    #endregion

    #endregion

    private struct MeleeSlotAssignment
    {
        public int Slot;
        public Vector3 TargetPositionAtAssignment;
        public Vector3 TargetForwardAtAssignment;
        public float DistanceOffset;
        public NpcMeleeApproachPreference Preference;

        public MeleeSlotAssignment(
            int slot,
            Vector3 targetPosition,
            Vector3 targetForward,
            float distanceOffset,
            NpcMeleeApproachPreference preference)
        {
            Slot = slot;
            TargetPositionAtAssignment = targetPosition;
            TargetForwardAtAssignment = targetForward;
            DistanceOffset = distanceOffset;
            Preference = preference;
        }
    }

    private struct RangedSlotAssignment
    {
        public int Slot;
        public Vector3 TargetPositionAtAssignment;

        public RangedSlotAssignment(int slot, Vector3 targetPosition)
        {
            Slot = slot;
            TargetPositionAtAssignment = targetPosition;
        }
    }

    private void Awake()
    {
        meleeSlotAngleOffset = Random.Range(0f, 360f);
        rangedSlotAngleOffset = Random.Range(0f, 360f);
    }

    private Vector3 GetSlotPosition(int slot, float distance)
    {
        float angle =
            rangedSlotAngleOffset + slot * (360f / rangedSlotCount);

        Vector3 direction =
            Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;

        return transform.position +
               direction * Mathf.Max(0f, distance);
    }

    #region MELEE
    public bool TryGetMeleePosition(
        NpcController requester,
        float distance,
        NpcMeleeApproachPreference preference,
        bool requireAvailableSlot,
        out Vector3 position)
    {
        position = default;

        if (requester == null)
            return false;

        RemoveDestroyedRequesters();

        int slot = GetOrAssignMeleeSlot(
            requester,
            distance,
            preference,
            requireAvailableSlot);

        if (slot < 0)
            return false;

        if (!meleeSlots.TryGetValue(
        requester,
        out MeleeSlotAssignment assignment))
        {
            return false;
        }

        position = GetMeleeSlotPosition(
            slot,
            distance,
            assignment.DistanceOffset);

        return true;
    }

    private int GetOrAssignMeleeSlot(
        NpcController requester,
        float distance,
        NpcMeleeApproachPreference preference,
        bool requireAvailableSlot)
    {
        if (meleeSlots.TryGetValue(
                requester,
                out MeleeSlotAssignment existingAssignment) &&
            existingAssignment.Slot >= 0 &&
            existingAssignment.Slot < meleeSlotCount &&
            existingAssignment.Preference == preference)
        {
            Vector3 targetMovement =
                transform.position -
                existingAssignment.TargetPositionAtAssignment;

            targetMovement.y = 0f;

            float reevaluationDistanceSquared =
                meleeTargetMovementReevaluationDistance *
                meleeTargetMovementReevaluationDistance;

            bool targetHasNotTurnedMuch =
                preference == NpcMeleeApproachPreference.Any ||
                Vector3.Angle(
                    GetFlatForward(existingAssignment.TargetForwardAtAssignment),
                    GetFlatForward(transform.forward)) <= 25f;

            Vector3 existingPosition = GetMeleeSlotPosition(
                existingAssignment.Slot,
                distance,
                existingAssignment.DistanceOffset);

            if (targetMovement.sqrMagnitude <= reevaluationDistanceSquared &&
                targetHasNotTurnedMuch &&
                IsPreferredMeleePosition(existingPosition, preference))
            {
                return existingAssignment.Slot;
            }
        }

        meleeSlots.Remove(requester);

        availableMeleeSlots.Clear();

        int closestReachableSlot = -1;
        float closestReachableDistanceSquared = float.MaxValue;

        int closestAvailableSlot = -1;
        float closestAvailableDistanceSquared = float.MaxValue;

        for (int slot = 0; slot < meleeSlotCount; slot++)
        {
            if (!TryGetReachableMeleeSlotPosition(
                    requester,
                    slot,
                    distance,
                    out Vector3 candidatePosition))
            {
                continue;
            }

            if (!IsPreferredMeleePosition(candidatePosition, preference))
                continue;

            Vector3 offset =
                requester.transform.position - candidatePosition;

            offset.y = 0f;

            float distanceSquared = offset.sqrMagnitude;

            if (distanceSquared < closestReachableDistanceSquared)
            {
                closestReachableDistanceSquared = distanceSquared;
                closestReachableSlot = slot;
            }

            bool slotIsTaken = false;

            foreach (KeyValuePair<NpcController, MeleeSlotAssignment> assignment
                     in meleeSlots)
            {
                if (assignment.Key != requester &&
                    assignment.Value.Slot == slot)
                {
                    slotIsTaken = true;
                    break;
                }
            }

            if (slotIsTaken)
                continue;

            availableMeleeSlots.Add(slot);

            if (distanceSquared < closestAvailableDistanceSquared)
            {
                closestAvailableDistanceSquared = distanceSquared;
                closestAvailableSlot = slot;
            }
        }

        if (closestReachableSlot < 0 ||
            (requireAvailableSlot && availableMeleeSlots.Count == 0))
            return -1;

        int selectedSlot = closestReachableSlot;

        if (availableMeleeSlots.Count > 0)
        {
            float choiceDistance =
                Mathf.Sqrt(closestAvailableDistanceSquared) +
                meleeSlotChoiceSlack;

            float choiceDistanceSquared = choiceDistance * choiceDistance;

            nearBestMeleeSlots.Clear();

            foreach (int slot in availableMeleeSlots)
            {
                if (!TryGetReachableMeleeSlotPosition(
                        requester,
                        slot,
                        distance,
                        out Vector3 candidatePosition))
                {
                    continue;
                }

                Vector3 offset =
                    requester.transform.position - candidatePosition;

                offset.y = 0f;

                if (offset.sqrMagnitude <= choiceDistanceSquared)
                    nearBestMeleeSlots.Add(slot);
            }

            if (nearBestMeleeSlots.Count > 0)
            {
                selectedSlot = nearBestMeleeSlots[
                    Random.Range(0, nearBestMeleeSlots.Count)];
            }
            else
            {
                selectedSlot = closestAvailableSlot;
            }
        }

        float distanceOffset =
            Random.Range(0f, Mathf.Min(meleeRadiusVariation, distance));

        if (!TryGetReachableMeleeSlotPosition(
                requester,
                selectedSlot,
                distance,
                out _,
                distanceOffset))
        {
            distanceOffset = 0f;
        }

        meleeSlots[requester] =
            new MeleeSlotAssignment(
                selectedSlot,
                transform.position,
                GetFlatForward(transform.forward),
                distanceOffset,
                preference);

        return selectedSlot;
    }

    private bool IsPreferredMeleePosition(
        Vector3 position,
        NpcMeleeApproachPreference preference)
    {
        if (preference == NpcMeleeApproachPreference.Any)
            return true;

        Vector3 directionToPosition = position - transform.position;
        directionToPosition.y = 0f;

        Vector3 targetForward = GetFlatForward(transform.forward);

        if (directionToPosition.sqrMagnitude <= 0.001f ||
            targetForward.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        float alignment = Vector3.Dot(
            targetForward,
            directionToPosition.normalized);

        switch (preference)
        {
            case NpcMeleeApproachPreference.Front:
                return alignment >= 0.5f;

            case NpcMeleeApproachPreference.Flank:
                return alignment < 0.5f;

            default:
                return true;
        }
    }

    private static Vector3 GetFlatForward(Vector3 forward)
    {
        forward.y = 0f;

        if (forward.sqrMagnitude > 0.001f)
            forward.Normalize();

        return forward;
    }

    private bool TryGetReachableMeleeSlotPosition(
        NpcController requester,
        int slot,
        float distance,
        out Vector3 position,
        float distanceOffset = 0f)
    {
        Vector3 slotPosition =
            GetMeleeSlotPosition(slot, distance, distanceOffset);

        if (!NavMesh.SamplePosition(
                slotPosition,
                out NavMeshHit navHit,
                MeleeSlotSampleRadius,
                requester.Locomotion.NavigationAreaMask))
        {
            position = default;
            return false;
        }

        position = navHit.position;
        return requester.Locomotion.CanReach(position);
    }

    private Vector3 GetMeleeSlotPosition(
        int slot,
        float distance,
        float distanceOffset = 0f)
    {
        float angle =
            meleeSlotAngleOffset + slot * (360f / meleeSlotCount);

        Vector3 direction =
            Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;

        float variedDistance =
            Mathf.Max(0f, distance - distanceOffset);

        return transform.position + direction * variedDistance;
    }

    public void ReleaseMeleePosition(NpcController requester)
    {
        if (requester != null)
            meleeSlots.Remove(requester);
    }
    #endregion

    #region RANGED
    private int GetOrAssignRangedSlot(
        NpcController requester,
        float distance)
    {
        if (rangedSlots.TryGetValue(
                requester,
                out RangedSlotAssignment existingAssignment) &&
            existingAssignment.Slot >= 0 &&
            existingAssignment.Slot < rangedSlotCount)
        {
            Vector3 targetMovement =
                transform.position -
                existingAssignment.TargetPositionAtAssignment;

            targetMovement.y = 0f;

            float reevaluationDistanceSquared =
                rangedTargetMovementReevaluationDistance *
                rangedTargetMovementReevaluationDistance;

            if (targetMovement.sqrMagnitude <= reevaluationDistanceSquared)
                return existingAssignment.Slot;
        }

        availableRangedSlots.Clear();

        int closestReachableSlot = -1;
        float closestReachableDistanceSquared = float.MaxValue;

        int closestAvailableSlot = -1;
        float closestAvailableDistanceSquared = float.MaxValue;

        for (int slot = 0; slot < rangedSlotCount; slot++)
        {
            if (!TryGetReachableSlotPosition(
                    requester,
                    slot,
                    distance,
                    out Vector3 candidatePosition))
            {
                continue;
            }

            Vector3 offset =
                requester.transform.position - candidatePosition;
            offset.y = 0f;

            float distanceSquared = offset.sqrMagnitude;

            if (distanceSquared < closestReachableDistanceSquared)
            {
                closestReachableDistanceSquared = distanceSquared;
                closestReachableSlot = slot;
            }

            bool slotIsTaken = false;

            foreach (KeyValuePair<NpcController, RangedSlotAssignment> assignment
                     in rangedSlots)
            {
                if (assignment.Key != requester &&
                    assignment.Value.Slot == slot)
                {
                    slotIsTaken = true;
                    break;
                }
            }

            if (slotIsTaken)
                continue;

            availableRangedSlots.Add(slot);

            if (distanceSquared < closestAvailableDistanceSquared)
            {
                closestAvailableDistanceSquared = distanceSquared;
                closestAvailableSlot = slot;
            }
        }

        if (closestReachableSlot < 0)
            return -1;

        int selectedSlot;

        if (availableRangedSlots.Count == 0)
        {
            selectedSlot = closestReachableSlot;
        }
        else
        {
            float choiceDistance =
                Mathf.Sqrt(closestAvailableDistanceSquared) +
                rangedSlotChoiceSlack;

            float choiceDistanceSquared = choiceDistance * choiceDistance;

            nearBestRangedSlots.Clear();

            foreach (int slot in availableRangedSlots)
            {
                if (!TryGetReachableSlotPosition(
                        requester,
                        slot,
                        distance,
                        out Vector3 candidatePosition))
                {
                    continue;
                }

                Vector3 offset =
                    requester.transform.position - candidatePosition;
                offset.y = 0f;

                if (offset.sqrMagnitude <= choiceDistanceSquared)
                    nearBestRangedSlots.Add(slot);
            }

            selectedSlot = nearBestRangedSlots[
                Random.Range(0, nearBestRangedSlots.Count)];
        }

        rangedSlots[requester] =
            new RangedSlotAssignment(selectedSlot, transform.position);

        return selectedSlot;
    }

    private bool TryGetReachableSlotPosition(
    NpcController requester,
    int slot,
    float distance,
    out Vector3 position)
    {
        Vector3 slotPosition = GetSlotPosition(slot, distance);

        if (!NavMesh.SamplePosition(
                slotPosition,
                out NavMeshHit navHit,
                RangedSlotSampleRadius,
                requester.Locomotion.NavigationAreaMask))
        {
            position = default;
            return false;
        }

        position = navHit.position;
        return requester.Locomotion.CanReach(position);
    }

    public bool TryGetRangedPosition(
        NpcController requester,
        float distance,
        out Vector3 position)
    {
        position = default;

        if (requester == null)
            return false;

        RemoveDestroyedRequesters();

        int slot = GetOrAssignRangedSlot(requester, distance);

        if (slot < 0)
            return false;

        position = GetSlotPosition(slot, distance);
        return true;
    }

    public void ReleaseRangedPosition(NpcController requester)
    {
        if (requester != null)
            rangedSlots.Remove(requester);
    }
    #endregion

    private void RemoveDestroyedRequesters()
    {
        // RANGED
        staleRangedRequesters.Clear();

        foreach (KeyValuePair<NpcController, RangedSlotAssignment> assignment
                 in rangedSlots)
        {
            if (assignment.Key == null)
                staleRangedRequesters.Add(assignment.Key);
        }

        for (int i = 0; i < staleRangedRequesters.Count; i++)
            rangedSlots.Remove(staleRangedRequesters[i]);

        // MELEE
        staleMeleeRequesters.Clear();

        foreach (KeyValuePair<NpcController, MeleeSlotAssignment> assignment
                 in meleeSlots)
        {
            if (assignment.Key == null)
                staleMeleeRequesters.Add(assignment.Key);
        }

        for (int i = 0; i < staleMeleeRequesters.Count; i++)
            meleeSlots.Remove(staleMeleeRequesters[i]);
    }
}

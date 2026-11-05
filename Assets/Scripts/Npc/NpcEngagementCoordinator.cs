using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NpcEngagementCoordinator : MonoBehaviour
{
    #region REFERENCES
    [Header("Ranged Positions")]
    [SerializeField, Min(1)]
    private int rangedSlotCount = 8;

    [SerializeField, Min(0f)]
    private float slotChoiceSlack = 2f;

    [SerializeField, Min(0f)]
    private float targetMovementReevaluationDistance = 2f;

    private float slotAngleOffset;

    private const float RangedSlotSampleRadius = 2f;

    private readonly List<int> availableRangedSlots =
        new List<int>();

    private readonly List<int> nearBestRangedSlots =
        new List<int>();

    private readonly Dictionary<NpcController, RangedSlotAssignment> rangedSlots =
        new Dictionary<NpcController, RangedSlotAssignment>();

    private readonly List<NpcController> staleRequesters =
        new List<NpcController>();
    #endregion

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
        slotAngleOffset = Random.Range(0f, 360f);
    }

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
                targetMovementReevaluationDistance *
                targetMovementReevaluationDistance;

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
                slotChoiceSlack;

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

    private Vector3 GetSlotPosition(int slot, float distance)
    {
        float angle =
            slotAngleOffset + slot * (360f / rangedSlotCount);

        Vector3 direction =
            Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;

        return transform.position +
               direction * Mathf.Max(0f, distance);
    }

    private void RemoveDestroyedRequesters()
    {
        staleRequesters.Clear();

        foreach (KeyValuePair<NpcController, RangedSlotAssignment> assignment
                 in rangedSlots)
        {
            if (assignment.Key == null)
                staleRequesters.Add(assignment.Key);
        }

        for (int i = 0; i < staleRequesters.Count; i++)
            rangedSlots.Remove(staleRequesters[i]);
    }
}
using System;
using UnityEngine;

public class TargetLockController
{
    private readonly Transform playerTransform;
    private readonly Transform cameraTransform;

    private readonly float lockOnRange;
    private readonly LayerMask targetMask;

    private ITargetable currentTarget;

    public bool IsLocked => currentTarget != null;
    public ITargetable CurrentTarget => currentTarget;

    public event Action<ITargetable> TargetLocked;
    public event Action TargetUnlocked;

    public TargetLockController(
        Transform playerTransform,
        Transform cameraTransform,
        float lockOnRange,
        LayerMask targetMask)
    {
        this.playerTransform = playerTransform;
        this.cameraTransform = cameraTransform;
        this.lockOnRange = lockOnRange;
        this.targetMask = targetMask;
    }

    public void ToggleLock()
    {
        if (IsLocked)
        {
            Unlock();
            return;
        }

        TryLock();
    }

    private void TryLock()
    {
        ITargetable target = FindBestTarget();

        if (target == null)
        {
            return;
        }

        currentTarget = target;
        TargetLocked?.Invoke(currentTarget);
    }

    public void Unlock()
    {
        if (!IsLocked)
            return;

        currentTarget = null;
        TargetUnlocked?.Invoke();
    }

    private ITargetable FindBestTarget()
    {
        Collider[] hits = Physics.OverlapSphere(
            playerTransform.position,
            lockOnRange,
            targetMask);

        ITargetable bestTarget = null;
        float bestScore = float.MinValue;

        foreach (Collider hit in hits)
        {
            if (!hit.TryGetComponent(
                out ITargetable target))
            {
                continue;
            }

            Transform targetTransform =
                target.TargetTransform;

            if (targetTransform == null)
                continue;

            Vector3 direction =
                targetTransform.position -
                cameraTransform.position;

            float distance = direction.magnitude;

            if (distance <= 0.001f)
                continue;

            direction.Normalize();

            float cameraAlignment =
                Vector3.Dot(
                    cameraTransform.forward,
                    direction);

            if (cameraAlignment <= 0f)
                continue;

            float score =
                cameraAlignment / distance;

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = target;
            }
        }

        return bestTarget;
    }
}
using UnityEngine;
using UnityEngine.AI;

public class NPCLocomotion : IPhysicsMove
{
    #region REFERRENCES
    public int NavigationAreaMask =>
    navMeshAgent != null
        ? navMeshAgent.areaMask
        : NavMesh.AllAreas;

    private Transform targetTransform;

    private bool hasMoveDestination;
    private Vector3 moveDestination;

    private bool hasRequestedDestination;
    private Vector3 lastRequestedDestination;

    private const float DestinationUpdateThreshold = 0.25f;

    private bool isChasing;
    private float engagementStoppingDistance;

    public bool IsGrounded =>
    characterController.isGrounded;

    private readonly CharacterController characterController;
    private readonly NavMeshAgent navMeshAgent;
    private readonly NavMeshPath reusablePath;
    #endregion


    #region ATTACK MOVEMENT
    private Vector3 attackMovementVelocity;
    private float attackMovementTimer;
    private float attackMovementDuration;
    #endregion

    #region PHYSICS MOVEMENT
    private Vector3 physicsVelocity;

    private float groundedVerticalVelocity;
    private const float PhysicsDeceleration = 20f;
    private const float Gravity = -25f;
    private const float ApexGravitySuspensionDuration = 0.25f;

    private float apexSuspensionTimer;
    private bool isRisingFromKnockback;
    private bool isPhysicsMoving;
    #endregion

    #region CONSTRUCTOR
    public NPCLocomotion(
        CharacterController characterController,
        NavMeshAgent navMeshAgent,
        float stoppingDistance)
    {
        this.characterController = characterController;
        this.navMeshAgent = navMeshAgent;
        reusablePath = new NavMeshPath();

        if (this.navMeshAgent != null)
        {
            this.navMeshAgent.stoppingDistance =
                Mathf.Max(0f, stoppingDistance);
        }

        engagementStoppingDistance = Mathf.Max(0f, stoppingDistance);

        if (this.navMeshAgent != null)
            this.navMeshAgent.stoppingDistance = engagementStoppingDistance;
    }
    #endregion

    #region LIFECYCLE
    public void Update()
    {
        HandleMovement();

        if (!isChasing || targetTransform == null)
            return;

        if (navMeshAgent == null ||
            !navMeshAgent.isActiveAndEnabled ||
            !navMeshAgent.isOnNavMesh)
        {
            return;
        }

        Vector3 destination;

        if (hasMoveDestination)
        {
            destination = moveDestination;
        }
        else if (targetTransform != null)
        {
            destination = targetTransform.position;
        }
        else
        {
            return;
        }

        float updateThresholdSquared =
            DestinationUpdateThreshold * DestinationUpdateThreshold;

        if (!hasRequestedDestination ||
            (destination - lastRequestedDestination).sqrMagnitude >=
                updateThresholdSquared)
        {
            if (navMeshAgent.SetDestination(destination))
            {
                lastRequestedDestination = destination;
                hasRequestedDestination = true;
            }
        }
    }
    #endregion

    public bool CanReach(Vector3 destination)
    {
        if (navMeshAgent == null ||
            !navMeshAgent.isActiveAndEnabled ||
            !navMeshAgent.isOnNavMesh)
        {
            return false;
        }

        bool pathFound = NavMesh.CalculatePath(
            navMeshAgent.nextPosition,
            destination,
            navMeshAgent.areaMask,
            reusablePath);

        return pathFound &&
               reusablePath.status == NavMeshPathStatus.PathComplete;
    }

    public void SetTarget(Transform target)
    {
        if (targetTransform == target)
            return;

        targetTransform = target;
        hasMoveDestination = false;
        hasRequestedDestination = false;
    }

    public void MoveTo(Vector3 destination)
    {
        float updateThresholdSquared =
            DestinationUpdateThreshold * DestinationUpdateThreshold;

        bool destinationChanged =
            !hasMoveDestination ||
            (destination - moveDestination).sqrMagnitude >=
                updateThresholdSquared;

        hasMoveDestination = true;

        if (destinationChanged)
        {
            moveDestination = destination;
            hasRequestedDestination = false;
        }

        SetChasing(true);
    }

    public bool HasReachedMoveDestination
    {
        get
        {
            if (!hasMoveDestination)
                return true;

            if (navMeshAgent == null ||
                !navMeshAgent.isActiveAndEnabled ||
                !navMeshAgent.isOnNavMesh)
            {
                return true;
            }

            if (navMeshAgent.pathPending)
                return false;

            if (!navMeshAgent.hasPath)
                return true;

            return navMeshAgent.remainingDistance <=
                   Mathf.Max(
                       navMeshAgent.stoppingDistance,
                       DestinationUpdateThreshold);
        }
    }

    private void HandleMovement()
    {
        Vector3 attackMovementDelta =
            GetAttackMovementDelta();

        if (!isPhysicsMoving)
        {
            if(IsGrounded && groundedVerticalVelocity < 0f)
            {
                groundedVerticalVelocity = -2f;
            }
            else
            {
                groundedVerticalVelocity += Gravity * Time.deltaTime;
            }

            Vector3 navigationMovement =
                navMeshAgent != null
                ? navMeshAgent.velocity
                : Vector3.zero;

            Vector3 gravityMovement =
                Vector3.up * groundedVerticalVelocity * Time.deltaTime;

            characterController.Move(
                navigationMovement * Time.deltaTime +
                gravityMovement +
                attackMovementDelta
                );

            return;
        }

        HandlePhysicsMovement(attackMovementDelta);
    }

    public bool FaceTarget(Vector3 targetPosition)
    {
        Transform npcTransform = characterController.transform;

        Vector3 direction = targetPosition - npcTransform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return true;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        float turnSpeed = navMeshAgent != null
            ? navMeshAgent.angularSpeed
            : 360f;

        npcTransform.rotation = Quaternion.RotateTowards(
            npcTransform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime);

        return Quaternion.Angle(
            npcTransform.rotation,
            targetRotation) <= 5f;
    }

    public void SetEngagementDistance(float distance)
    {
        engagementStoppingDistance = Mathf.Max(0f, distance);

        if (navMeshAgent == null) return;

        navMeshAgent.stoppingDistance = isChasing
            ? 0f
            : engagementStoppingDistance;
    }

    #region CHASING TARGET
    public void ChaseTarget()
    {
        bool wasUsingMoveDestination = hasMoveDestination;

        hasMoveDestination = false;

        if (wasUsingMoveDestination)
            hasRequestedDestination = false;

        SetChasing(true);
    }

    public void SetChasing(bool shouldChase)
    {
        bool stateChanged = isChasing != shouldChase;
        isChasing = shouldChase;

        if (!shouldChase)
        {
            hasMoveDestination = false;
            hasRequestedDestination = false;
        }

        if (navMeshAgent == null ||
            !navMeshAgent.isActiveAndEnabled ||
            !navMeshAgent.isOnNavMesh)
        {
            return;
        }

        navMeshAgent.stoppingDistance = shouldChase
            ? 0f
            : engagementStoppingDistance;

        // Let the agent turn while moving. Turn manually while stopped.
        navMeshAgent.updateRotation = shouldChase;

        if (!stateChanged)
            return;

        navMeshAgent.isStopped = !shouldChase;

        if (!shouldChase)
        {
            navMeshAgent.ResetPath();
            hasRequestedDestination = false;
        }
    }
    #endregion

    #region ATTACK MOVEMENT
    public void ApplyAttackMovement(
    Vector3 direction,
    float distance,
    float duration)
    {
        if (distance <= 0f ||
            duration <= 0f ||
            direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        direction.Normalize();

        attackMovementVelocity =
            direction * (distance / duration);

        attackMovementTimer = 0f;
        attackMovementDuration = duration;
    }

    public void CancelAttackMovement()
    {
        attackMovementVelocity = Vector3.zero;
        attackMovementTimer = attackMovementDuration;
    }

    private Vector3 GetAttackMovementDelta()
    {
        float remainingTime =
            attackMovementDuration - attackMovementTimer;

        if (remainingTime <= 0f)
            return Vector3.zero;

        float movementTime =
            Mathf.Min(Time.deltaTime, remainingTime);

        attackMovementTimer += movementTime;

        Vector3 movementDelta =
            attackMovementVelocity * movementTime;

        if (attackMovementTimer >= attackMovementDuration)
            attackMovementVelocity = Vector3.zero;

        return movementDelta;
    }
    #endregion

    #region PHYSICS MOVEMENT
    public void ApplyKnockback(Vector3 force)
    {
        if (navMeshAgent == null)
            return;

        physicsVelocity.x += force.x;
        physicsVelocity.z += force.z;

        if (force.y > 0f)
        {
            physicsVelocity.y = force.y;
            isRisingFromKnockback = true;
            apexSuspensionTimer = 0f;
        }
        else
        {
            physicsVelocity.y += force.y;
        }

        if (!isPhysicsMoving &&
            navMeshAgent.isActiveAndEnabled &&
            navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.ResetPath();
        }

        isPhysicsMoving = true;
    }

    private void HandlePhysicsMovement(Vector3 attackMovementDelta)
    {
        if (apexSuspensionTimer > 0f)
        {
            apexSuspensionTimer = Mathf.Max(
                0f,
                apexSuspensionTimer - Time.deltaTime
            );

            physicsVelocity.y = 0f;
        }
        else if (!IsGrounded)
        {
            float nextVerticalVelocity =
                physicsVelocity.y + Gravity * Time.deltaTime;

            // Begin the hang-time when gravity would carry an upward
            // knockback through its apex on this frame.
            if (isRisingFromKnockback &&
                physicsVelocity.y > 0f &&
                nextVerticalVelocity <= 0f)
            {
                apexSuspensionTimer =
                    ApexGravitySuspensionDuration;
                physicsVelocity.y = 0f;
                isRisingFromKnockback = false;
            }
            else
            {
                physicsVelocity.y = nextVerticalVelocity;
            }
        }
        else
        {
            isRisingFromKnockback = false;

            if (physicsVelocity.y < 0f)
            {
                physicsVelocity.y = -2f;
            }
        }

        characterController.Move(
            physicsVelocity * Time.deltaTime +
            attackMovementDelta
        );

        DeceleratePhysicsVelocity();

        // TESTING THIS FOR NOW AFTER NOT PROPERLY
        // RESETTING THE isPhysicsMoving BOOL.
        Vector3 horizontalVelocity =
            new Vector3(
                physicsVelocity.x,
                0f,
                physicsVelocity.z
            );

        if (horizontalVelocity.sqrMagnitude <= 0.001f &&
            IsGrounded)
        {
            physicsVelocity.x = 0f;
            physicsVelocity.z = 0f;
            physicsVelocity.y = 0f;
            apexSuspensionTimer = 0f;
            isRisingFromKnockback = false;

            navMeshAgent.Warp(
                characterController.transform.position
            );

            isPhysicsMoving = false;
        }
    }

    private void DeceleratePhysicsVelocity()
    {
        Vector3 horizontalVelocity =
            new Vector3(
                physicsVelocity.x,
                0f,
                physicsVelocity.z
            );

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            Vector3.zero,
            PhysicsDeceleration * Time.deltaTime
        );

        physicsVelocity.x = horizontalVelocity.x;
        physicsVelocity.z = horizontalVelocity.z;
    }
    #endregion
}

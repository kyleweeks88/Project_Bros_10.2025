using UnityEngine;
using UnityEngine.AI;

public class NPCLocomotion : IPhysicsMove
{
    // TESTING PURPOSES
    private Transform targetTransform;

    private readonly CharacterController characterController;
    private readonly NavMeshAgent navMeshAgent;

    private Vector3 physicsVelocity;

    private const float PhysicsDeceleration = 20f;
    private const float Gravity = -25f;
    private const float ApexGravitySuspensionDuration = 0.25f;

    private float apexSuspensionTimer;
    private bool isRisingFromKnockback;
    private bool isPhysicsMoving;

    public bool IsGrounded =>
        characterController.isGrounded;

    public NPCLocomotion(
        CharacterController characterController,
        NavMeshAgent navMeshAgent)
    {
        this.characterController = characterController;
        this.navMeshAgent = navMeshAgent;
    }

    public void Start()
    {
        targetTransform =
            GameObject.FindGameObjectWithTag("Player").transform;
    }

    public void Update()
    {
        HandleMovement();

        // TESTING ONLY
        navMeshAgent.SetDestination(
            targetTransform.position
        );
    }

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

        if (!isPhysicsMoving)
        {
            navMeshAgent.ResetPath();
        }

        isPhysicsMoving = true;
    }

    private void HandleMovement()
    {
        if (!isPhysicsMoving)
        {
            Vector3 navigationMovement =
                navMeshAgent.velocity;

            characterController.Move(
                navigationMovement * Time.deltaTime
            );

            return;
        }

        HandlePhysicsMovement();
    }

    private void HandlePhysicsMovement()
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
            physicsVelocity * Time.deltaTime
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
}

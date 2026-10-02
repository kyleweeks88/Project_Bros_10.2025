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
            GameObject.FindGameObjectWithTag
            ("Player").transform;
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

        physicsVelocity += force;

        if(!isPhysicsMoving)
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
        if (!IsGrounded)
        {
            physicsVelocity.y +=
                Gravity * Time.deltaTime;
        }
        else if (physicsVelocity.y < 0f)
        {
            physicsVelocity.y = -2f;
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
            PhysicsDeceleration *
            Time.deltaTime
        );

        physicsVelocity.x =
            horizontalVelocity.x;

        physicsVelocity.z =
            horizontalVelocity.z;
    }
}

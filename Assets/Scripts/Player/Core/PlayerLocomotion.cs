using System;
using UnityEngine;

public class PlayerLocomotion : IPhysicsMove
{
    // COMPONENT REF
    private readonly EntityStats entityStats;
    private readonly CharacterController characterController;
    private readonly PlayerInputHandler input;
    private readonly Transform cameraTransform;
    private readonly Transform playerTransform;

    // ATTACK MOVEMENT
    private Vector3 attackMovementVelocity;
    private float attackMovementTimer;
    private float attackMovementDuration;
    private bool pendingMeleeApexHang;
    private float meleeApexHangTimer;
    private const float MeleeApexGravitySuspensionDuration = 0.25f;

    // JUMP ATTACK MOVEMENT
    private float gravityMultiplier = 1f;
    private float airborneAttackVerticalTimer;
    private const float
        AirborneAttackVerticalTransitionTime = 0.2f;
    private bool airborneAttackVerticalTransition;
    private bool hasUsedFirstAirborneAttackMovement;

    // ATTACK ROTATION
    private bool rotationLocked;
    public bool RotationLocked => rotationLocked;

    // PHYSICS FORCE
    private Vector3 physicsForceVelocity;
    private const float PhysicsForceDeceleration = 20f;

    // MOVEMENT
    private Vector3 velocity;
    private const float SprintSpeedMultiplier = 1.6f;
    //private const float MoveSpeed = 5f;
    //private const float SprintSpeed = 8f;
    private const float Acceleration = 20f;
    private const float Deceleration = 50f;
    private bool movementLocked;
    public bool MovementLocked => movementLocked;

    // ABILITY MOVEMENT
    private Vector3 abilityMovement;
    private bool abilityMovementOverride;
    private bool abilityGravityOverride;

    // GRAVITY AND JUMPING
    private const float Gravity = -25f;
    private bool jumpRequested;

    // CLIMBING
    private Vector3 climbMovement;
    private bool climbing;
    public bool IsClimbing => climbing;
    public Vector3 ClimbProbeOrigin
    {
        get
        {
            Vector3 center = playerTransform.TransformPoint(
                characterController.center);

            return center +
                playerTransform.forward *
                characterController.radius;
        }
    }

    // STATES
    private LocomotionState currentState;
    public LocomotionState CurrentState => currentState;
    public bool IsGrounded => characterController.isGrounded;
    public bool IsFalling => currentState == LocomotionState.Airborne && velocity.y <= 0f;
    public bool IsJumping => velocity.y > 0f;
    public float VerticalVelocity
    {
        get => velocity.y;
        set => velocity.y = value;
    }

    // EVENTS
    public event Action Landed;

    // CONSTRUCTOR
    public PlayerLocomotion(
    CharacterController characterController,
    PlayerInputHandler input,
    Camera camera,
    Transform playerTransform,
    EntityStats entityStats)
    {
        this.characterController = characterController;
        this.input = input;
        this.playerTransform = playerTransform;

        cameraTransform = camera != null
            ? camera.transform
            : null;

        currentState = characterController.isGrounded
            ? LocomotionState.Grounded
            : LocomotionState.Airborne;

        input.jumpEventPressed += OnJumpPressed;
        this.entityStats = entityStats;
    }

    // LIFECYCLE
    public void Update()
    {
        HandleJump();

        if (!climbing)
        {
            HandleGravity();
        }

        Vector3 movement;

        if(climbing)
        {
            movement =
                climbMovement * Time.deltaTime;
        }
        else
        {
            HandleMovement();

            movement = abilityMovementOverride
                ? abilityMovement * Time.deltaTime
                : velocity * Time.deltaTime;
        }

        movement += GetAttackMovement();
        movement += GetPhysicsMovement();

        characterController.Move(movement);

        UpdateLocomotionState();
    }

    #region STATE LOGIC
    private void UpdateLocomotionState()
    {
        LocomotionState previousState = currentState;

        currentState = characterController.isGrounded
            ? LocomotionState.Grounded
            : LocomotionState.Airborne;

        if(previousState == LocomotionState.Airborne &&
            currentState == LocomotionState.Grounded)
        {
            hasUsedFirstAirborneAttackMovement = false;
            Landed?.Invoke();
        }
    }
    #endregion

    #region EVENT LOGIC
    public void Dispose()
    {
        if(input != null)
            input.jumpEventPressed -= OnJumpPressed;
    }

    private void OnJumpPressed()
    {
        jumpRequested = true;
    }
    #endregion

    #region ROTATION
    public void SetRotationLocked(bool locked)
    {
        rotationLocked = locked;
    }

    public void UpdateRotation(float targetYaw)
    {
        if (rotationLocked)
            return;

        playerTransform.rotation = Quaternion.Euler(
            0f,
            targetYaw,
            0f
            );
    }
    #endregion

    #region ATTACK MOVEMENT

    public bool TryUseFirstAirborneAttackMovement()
    {
        if (hasUsedFirstAirborneAttackMovement)
            return false;

        hasUsedFirstAirborneAttackMovement = true;
        return true;
    }

    private Vector3 GetAttackMovement()
    {
        if(attackMovementTimer >= attackMovementDuration)
        {
            attackMovementVelocity = Vector3.zero;
            return Vector3.zero;
        }

        attackMovementTimer += Time.deltaTime;

        return attackMovementVelocity * Time.deltaTime;
    }


    public void ApplyAttackMovement(
        Vector3 direction,
        float distance,
        float duration,
        bool useApexHangTime = false)
    {
        pendingMeleeApexHang = false;
        meleeApexHangTimer = 0f;

        if (distance <= 0f || duration <= 0f)
            return;

        //direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        direction.Normalize();
        pendingMeleeApexHang = useApexHangTime;

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

    #endregion

    #region ABILITY MOVEMENT

    public void SetAbilityMovementOverride(Vector3 movement)
    {
        abilityMovement = movement;
        abilityMovementOverride = true;
    }

    public void ClearAbilityMovementOverride()
    {
        abilityMovement = Vector3.zero;
        abilityMovementOverride = false;
    }

    public void SetAbilityGravityOverride(bool value)
    {
        abilityGravityOverride = value;
    }

    public void PerformExtraJump()
    {
        if (entityStats.JumpHeight <= 0f)
            return;

        VerticalVelocity = Mathf.Sqrt(
            entityStats.JumpHeight * -2f * Gravity
        );
    }

    #endregion

    #region MOVEMENT

    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;
    }

    private void HandleMovement()
    {
        if(movementLocked)
        {
            Vector3 horizVelocity = new Vector3(
                velocity.x,
                0f,
                velocity.z);

            horizVelocity = Vector3.MoveTowards(
                horizVelocity,
                Vector3.zero,
                Deceleration * Time.deltaTime);

            velocity.x = horizVelocity.x;
            velocity.z = horizVelocity.z;

            return;
        }

        Vector2 inputDirection = input.MovementInput;

        if(inputDirection.sqrMagnitude > 1f)
        {
            inputDirection.Normalize();
        }

        Vector3 forward = cameraTransform != null
            ? cameraTransform.forward
            : Vector3.forward;

        Vector3 right = cameraTransform != null
            ? cameraTransform.right
            : Vector3.right;

        // We don't want the camera's vertical angle
        // affecting ground movement.
        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 movement =
            forward * inputDirection.y +
            right * inputDirection.x;

        if (movement.sqrMagnitude > 1f)
        {
            movement.Normalize();
        }

        float targetSpeed = input.SprintPressed
            ? entityStats.MoveSpeed * SprintSpeedMultiplier
            : entityStats.MoveSpeed;

        Vector3 targetVelocity = movement * targetSpeed;

        float accelerationRate = movement.sqrMagnitude > 0.01f
            ? Acceleration
            : Deceleration;

        Vector3 horizontalVelocity = new Vector3(
            velocity.x,
            0f,
            velocity.z
        );

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            accelerationRate * Time.deltaTime
        );

        velocity.x = horizontalVelocity.x;
        velocity.z = horizontalVelocity.z;
    }

    #endregion

    #region GRAVITY

    private void HandleGravity()
    {
        if (abilityGravityOverride)
            return;

        if(airborneAttackVerticalTransition)
        {
            HandleAirborneAttackVerticalTransition();
            return;
        }

        if (characterController.isGrounded)
        {
            pendingMeleeApexHang = false;
            meleeApexHangTimer = 0f;
        }

        if (meleeApexHangTimer > 0f)
        {
            meleeApexHangTimer = Mathf.Max(
                0f,
                meleeApexHangTimer - Time.deltaTime);
            velocity.y = 0f;
            return;
        }

        if (characterController.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        // Melee movement is a timed displacement, so its apex is the
        // end of the upward movement rather than a gravity velocity crossing.
        if (pendingMeleeApexHang &&
            attackMovementTimer >= attackMovementDuration)
        {
            pendingMeleeApexHang = false;
            meleeApexHangTimer =
                MeleeApexGravitySuspensionDuration;
            velocity.y = 0f;
            attackMovementVelocity.y = 0f;
            return;
        }

        velocity.y +=
            Gravity *
            gravityMultiplier *
            Time.deltaTime;
    }

    public void SetGravityMultiplier(float multiplier)
    {
        gravityMultiplier = Mathf.Max(0f, multiplier);
    }
    #endregion

    #region JUMP & AIRBORNE MOVEMENT
    private void HandleJump()
    {
        if (!jumpRequested)
            return;

        // Consume the input immediately.
        jumpRequested = false;

        if (!characterController.isGrounded)
            return;

        velocity.y = Mathf.Sqrt(
            entityStats.JumpHeight * -2f * Gravity
        );
    }

    public void StartAirborneAttack(
        float gravityMultiplier)
    {
        SetGravityMultiplier(gravityMultiplier);

        airborneAttackVerticalTimer = 0f;
        airborneAttackVerticalTransition = true;
    }

    private void HandleAirborneAttackVerticalTransition()
    {
        airborneAttackVerticalTimer += Time.deltaTime;

        velocity.y = Mathf.MoveTowards(
            velocity.y,
            0f,
            Mathf.Abs(velocity.y) /
            AirborneAttackVerticalTransitionTime *
            Time.deltaTime
            );

        if(velocity.y <= 0f ||
            airborneAttackVerticalTimer >=
            AirborneAttackVerticalTransitionTime)
        {
            velocity.y = 0f;
            airborneAttackVerticalTransition = false;
        }
    }

    public void EndAirborneAttack()
    {
        airborneAttackVerticalTransition = false;
    }
    #endregion

    #region CLIMBING

    public void SetClimbRotation(Vector3 surfaceNormal)
    {
        if (surfaceNormal.sqrMagnitude <= 0.001f)
            return;

        surfaceNormal.y = 0f;

        if (surfaceNormal.sqrMagnitude <= 0.001f)
            return;

        surfaceNormal.Normalize();

        playerTransform.rotation =
            Quaternion.LookRotation(
                -surfaceNormal,
                Vector3.up
                );
    }

    public void SetClimbing(bool value)
    {
        climbing = value;

        if(climbing)
            velocity.y = 0f;
    }

    public void SetClimbMovement(Vector3 movement)
    {
        climbMovement = movement;
    }

    public void ClearClimbMovement()
    {
        climbMovement = Vector3.zero;
    }

    #endregion

    #region PHYSICS MOVEMENT

    public void ApplyKnockback(Vector3 force)
    {
        physicsForceVelocity += force;
    }

    private Vector3 GetPhysicsMovement()
    {
        if(physicsForceVelocity.sqrMagnitude <= 0.001f)
        {
            physicsForceVelocity = Vector3.zero;
            return Vector3.zero;
        }

        Vector3 movement =
            physicsForceVelocity * Time.deltaTime;

        physicsForceVelocity = Vector3.MoveTowards(
            physicsForceVelocity,
            Vector3.zero,
            PhysicsForceDeceleration * Time.deltaTime
            );

        return movement;
    }

    #endregion 
}

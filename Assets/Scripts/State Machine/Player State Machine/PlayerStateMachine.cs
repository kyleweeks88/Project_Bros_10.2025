using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    // STATE MACHINE REFERENCES
    PlayerBaseState currentState;
    PlayerStateFactory states;

    // COMPONENT REFERENCES
    private Camera mainCamera;
    private CharacterController characterController;
    private PlayerInputHandler playerInputHandler;
    private Animator animator;

    [Header("Movement Parameters")]
    private Vector3 currentMovement;
    private Vector3 inputDir;
    [SerializeField] float moveSpeed = 3.0f;
    [SerializeField] float moveAcceleration = 6f;
    [SerializeField] float sprintMultiplier = 2.0f;
    [SerializeField] float crouchDeduction = 2.0f;
    [SerializeField] float currentSpeed;
    [SerializeField] float targetSpeed;
    public bool canMove = true;
    public bool isMovementPressed;
    public bool isSprinting;
    public bool isCrouching;
    public bool isClimbing;
    public bool groundedFailSafe;

    [Header("Jump Parameters")]
    [SerializeField] float maxJumpHeight = 4.0f;
    [SerializeField] float maxJumpTime = 0.75f;
    [SerializeField] float initialJumpVelocity;
    [SerializeField] bool isJumping;
    [SerializeField] bool isFalling;
    public LayerMask ledgeMask;
    bool requireNewJumpPress;

    [Header("Look Parameters")]
    [SerializeField] float mouseSensitivity = 0.1f;
    [SerializeField] float upDownLookRange = 80f;
    float verticalRotation;

    [Header("Physics Parameters")]
    [SerializeField] private CapsuleCollider physCollider;
    private Rigidbody rigidBody;
    [SerializeField] private float gravity = -9.8f;

    // ANIMATOR REFERENCES
    int isWalkingHash;
    int isClimbingHash;
    int isJumpingHash;
    int isFallingHash;
    int velocityXHash;
    int velocityZHash;

    #region GETTERS AND SETTERS
    // GENERAL
    public PlayerBaseState CurrentState { get { return currentState; } set { currentState = value; } }
    public CharacterController sm_characterController { get { return characterController; } }
    public PlayerInputHandler sm_PlayerInputHandler { get { return playerInputHandler; } }
    public Camera sm_mainCamera { get { return mainCamera; } }
    public float sm_gravity { get { return gravity; } }
    public bool sm_isFalling { get { return isFalling; } set { isFalling = value; } }
    public bool sm_groundedFailSafe { get { return groundedFailSafe; } }
    // MOVEMENT
    public bool sm_isMovementPressed { get { return isMovementPressed; } }
    public float sm_currentMovementY { get { return currentMovement.y; } set { currentMovement.y = value; } }
    public float sm_currentMovementX { get { return currentMovement.x; } set { currentMovement.x = value; } }
    public float sm_moveSpeed { get { return moveSpeed; } }
    public float sm_targetSpeed { set { targetSpeed = value; } }
    // ANIMATION
    public Animator sm_Animator { get { return animator; } }
    public int sm_isWalkingHash { get { return isWalkingHash; } }
    public int sm_isSprintingHash { get { return isWalkingHash; } }
    public int sm_isFallingHash { get { return isFallingHash; } }
    public int sm_isClimbingHash { get { return isClimbingHash; } }
    // JUMPING
    public float sm_initialJumpVelocity { get { return initialJumpVelocity; } set { initialJumpVelocity = value; } }
    public int sm_isJumpingHash { get { return isJumpingHash; } }
    public bool sm_requireNewJumpPress { get { return requireNewJumpPress; } set { requireNewJumpPress = value; } }
    public bool sm_isJumping { set { isJumping = value; } }
    // SPRINTING
    public bool sm_isSprinting { set { isSprinting = value; } }
    public float sm_sprintMultiplier { get { return sprintMultiplier; } }
    // CROUCHING
    public bool sm_isCrouching { set { isCrouching = value; } }
    public float sm_crouchDeduction { get { return crouchDeduction; } }
    // CLIMBING
    public bool sm_isClimbing { get { return isClimbing; } }
    #endregion


    private void OnEnable()
    {
        playerInputHandler.jumpEventStarted += JumpPressed;
        playerInputHandler.jumpEventCancelled += JumpReleased;
    }

    private void Awake()
    {
        playerInputHandler = GetComponent<PlayerInputHandler>();
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        rigidBody = GetComponent<Rigidbody>();
        mainCamera = Camera.main;

        isClimbingHash = Animator.StringToHash("isClimbing");
        isJumpingHash = Animator.StringToHash("isJumping");
        isFallingHash = Animator.StringToHash("isFalling");
        velocityXHash = Animator.StringToHash("velocityX");
        velocityZHash = Animator.StringToHash("velocityZ");

        SetupJumpVariables();
    }

    private void Start()
    {
        // INITIALIZE THE STATEMACHINE FACTORY AND START STATE
        states = new PlayerStateFactory(this);
        currentState = states.Grounded();
        currentState.EnterState();

        rigidBody.isKinematic = true;
        physCollider.enabled = false;

        targetSpeed = moveSpeed;
    }

    private void Update()
    {
        currentState.UpdateStates();

        Movement();
        Rotation();
        Gravity();
        LocomotionAnimation();
    }

    #region ROTATION
    private void Rotation()
    {
        float mouseXRot = playerInputHandler.RotationInput.x * mouseSensitivity;
        float mouseYRot = playerInputHandler.RotationInput.y * mouseSensitivity;

        ApplyVerticalRotation(mouseYRot);
        ApplyHorizontalRotation(mouseXRot);
    }

    private void ApplyVerticalRotation(float rotationAmount)
    {
        // Stop the cam from rotating too far and clipping
        verticalRotation = Mathf.Clamp(verticalRotation - rotationAmount, -upDownLookRange, upDownLookRange);
        mainCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }

    private void ApplyHorizontalRotation(float rotationAmount)
    {
        transform.Rotate(0, rotationAmount, 0);
    }
    #endregion

    #region MOVEMENT
    void Movement()
    {
        isMovementPressed = inputDir.x != 0 || inputDir.z != 0;
        if (!isMovementPressed)
        {
            currentSpeed -= moveAcceleration * Time.deltaTime;
        }
        else
        {
            currentSpeed += moveAcceleration * Time.deltaTime;
        }

        if (!isClimbing)
        {
            Vector3 worldDirection = CalculateWorldDirection(new Vector3(playerInputHandler.MovementInput.x, 0, playerInputHandler.MovementInput.y));
            currentMovement.x = worldDirection.x * currentSpeed;
            currentMovement.z = worldDirection.z * currentSpeed;
        }

        currentSpeed = Mathf.Clamp(currentSpeed, 0.5f, targetSpeed);
        characterController.Move(currentMovement * Time.deltaTime);
    }
    #endregion

    #region JUMPING
    void SetupJumpVariables()
    {
        float timeToApex = maxJumpTime / 2;
        gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        initialJumpVelocity = (2 * maxJumpHeight) / timeToApex;
    }

    void JumpPressed()
    {
        requireNewJumpPress = false;
    }

    void JumpReleased()
    {
        requireNewJumpPress = true;
    }
    #endregion

    #region GENERAL
    public Vector3 CalculateWorldDirection(Vector3 _newInputDir)
    {
        inputDir = _newInputDir;
        Vector3 worldDirection = transform.TransformDirection(inputDir);

        return worldDirection.normalized;
    }

    private void Gravity()
    {
        GroundedFailSafe();
        isFalling = !groundedFailSafe && currentMovement.y <= -0.5f;

        float previousYVelocity = currentMovement.y;
        float newYVelocity = currentMovement.y + (gravity * Time.deltaTime);
        float nextYVelocity = (previousYVelocity + newYVelocity) * 0.5f;
        currentMovement.y = nextYVelocity;
    }

    void GroundedFailSafe()
    {
        // CHECK FOR OVERHEAD OBJECTS WITH RAYCAST BEFORE STANDING BACK UP
        Vector3 rayOrigin = characterController.transform.position;
        Vector3 rayDirection = Vector3.down;
        Debug.DrawRay(rayOrigin, rayDirection, Color.red);
        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hitInfo, 1f))
        {
            if (!characterController.isGrounded)
            {
                Debug.Log("STILL GROUNDED");
                groundedFailSafe = true;
            }
            else
            {
                Debug.Log("STILL GROUNDED");
                groundedFailSafe = true;
            }
        }
        else
        {
            Debug.Log("ACTUALLY NOT GROUNDED");
            groundedFailSafe = false;
        }
    }

    private void LocomotionAnimation()
    {
        animator.SetFloat(velocityZHash, inputDir.z * currentSpeed);
        animator.SetFloat(velocityXHash, inputDir.x * currentSpeed);
    }
    #endregion
}
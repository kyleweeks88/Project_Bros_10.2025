using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
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

    [Header("Jump Parameters")]
    [SerializeField] float maxJumpHeight = 4.0f;
    [SerializeField] float maxJumpTime = 0.75f;
    [SerializeField] float initialJumpVelocity;
    [SerializeField] bool isJumping;
    [SerializeField] bool isFalling;
    public LayerMask ledgeMask;

    [Header("Look Parameters")]
    [SerializeField] float mouseSensitivity = 0.1f;
    [SerializeField] float upDownLookRange = 80f;
    float verticalRotation;

    [Header("Physics Parameters")]
    [SerializeField] private CapsuleCollider physCollider;
    private Rigidbody rigidBody;
    [SerializeField] private float gravity = -9.8f;

    // COMPONENT REFERENCES
    private Camera mainCamera;
    private CharacterController characterController;
    private PlayerInputHandler playerInputHandler;
    private Animator animator;

    // ANIMATOR REFERENCES
    int isClimbingHash;
    int isJumpingHash;
    int isFallingHash;
    int velocityXHash;
    int velocityZHash;
    bool requireNewJumpPress;

    PlayerBaseState currentState;
    PlayerStateFactory states;

    #region Getters and Setters
    // GETTERS AND SETTERS
    public PlayerBaseState CurrentState { get { return currentState; } set { currentState = value; } }
    public PlayerInputHandler m_PlayerInputHandler { get { return playerInputHandler; } }
    public Animator m_Animator { get { return animator; } }
    public CharacterController m_characterController { get { return characterController; } }
    // JUMPING
    public int m_isJumpingHash { get { return isJumpingHash; } }
    public bool m_requireNewJumpPress { get { return requireNewJumpPress; } set { requireNewJumpPress = value; } }
    public bool m_isJumping { set { isJumping = value; } }
    public bool m_isFalling { get { return isFalling; } set { isFalling = value; } }
    public float m_currentMovementY { get { return currentMovement.y; } set {  currentMovement.y = value; }}
    public float m_initialJumpVelocity { get { return initialJumpVelocity; } set { initialJumpVelocity = value; } }
    public float m_gravity { get { return gravity; } }
    #endregion


    private void OnEnable()
    {
        playerInputHandler.jumpEventStarted += JumpPressed;
        playerInputHandler.jumpEventCancelled += JumpReleased;
    }

    private void Awake()
    {
        states = new PlayerStateFactory(this);
        currentState = states.Grounded();
        currentState.EnterState();

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
        rigidBody.isKinematic = true;
        physCollider.enabled = false;

        targetSpeed = moveSpeed;
    }


    private void Update()
    {
        currentState.UpdateState();

        HandleMovement();
        HandleRotation();
        HandleGravity();
        HandleAnimation();
        //Debug.Log("Test:" + currentMovement.y);
    }

    void JumpPressed()
    {
        requireNewJumpPress = false;
    }

    void JumpReleased()
    {
        requireNewJumpPress = true;
    }

    private Vector3 CalculateWorldDirection(Vector3 _newInputDir)
    {
        inputDir = _newInputDir;
        Vector3 worldDirection = transform.TransformDirection(inputDir);

        return worldDirection.normalized;
    }

    private void HandleGravity()
    {
        isFalling = !characterController.isGrounded && currentMovement.y <= -1.0f;

        float previousYVelocity = currentMovement.y;
        float newYVelocity = currentMovement.y + (gravity * Time.deltaTime);
        float nextYVelocity = (previousYVelocity + newYVelocity) * 0.5f;
        currentMovement.y = nextYVelocity;
    }

    #region Rotation
    private void HandleRotation()
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

    #region Movement
    void HandleMovement()
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

        Vector3 worldDirection = CalculateWorldDirection(new Vector3(playerInputHandler.MovementInput.x, 0, playerInputHandler.MovementInput.y));
        currentMovement.x = worldDirection.x * currentSpeed;
        currentMovement.z = worldDirection.z * currentSpeed;

        currentSpeed = Mathf.Clamp(currentSpeed, 0.5f, targetSpeed);
        characterController.Move(currentMovement * Time.deltaTime);
    }
    #endregion

    void SetupJumpVariables()
    {
        float timeToApex = maxJumpTime / 2;
        gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        initialJumpVelocity = (2 * maxJumpHeight) / timeToApex;
    }

    private void HandleAnimation()
    {
        animator.SetFloat(velocityZHash, inputDir.z * currentSpeed);
        animator.SetFloat(velocityXHash, inputDir.x * currentSpeed);
        animator.SetBool(isFallingHash, isFalling);
        animator.SetBool(isClimbingHash, isClimbing);
    }
}


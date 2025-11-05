using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Parameters")]
    private Vector3 currentMovement;
    private Vector3 inputDir;
    [SerializeField] float moveSpeed = 3.0f;
    [SerializeField] float moveAcceleration = 6f;
    [SerializeField] float sprintMultiplier = 2.0f;
    [SerializeField] float crouchDeduction = 2.0f;
    [SerializeField] float currentSpeed;
    public bool isMovementPressed;
    public bool isSprinting;
    public bool isCrouching;
    public bool isGrounded;

    [Header("Jump Parameters")]
    [SerializeField] float maxJumpHeight = 1.0f;
    [SerializeField] float maxJumpTime = 0.5f;
    [SerializeField] bool isJumping;
    [SerializeField] float initialJumpVelocity;

    [Header("Look Parameters")]
    [SerializeField] float mouseSensitivity = 0.1f;
    [SerializeField] float upDownLookRange = 80f;
    float verticalRotation;

    [Header("Physics Parameters")]
    [SerializeField] private CapsuleCollider physCollider;
    private Rigidbody rigidBody;
    [SerializeField] private float gravityMultiplier = 1.0f;
    [SerializeField] private float gravity = -9.8f;

    // COMPONENT REFERENCES
    private Camera mainCamera;
    private CharacterController characterController;
    private PlayerInputHandler playerInputHandler;
    private Animator animator;

    // ANIMATOR REFERENCES
    //int isWalkingHash;
    //int isRunningHash;
    int isJumpingHash;
    int velocityXHash;
    int velocityZHash;
    bool isJumpAnimating;

    private void Awake()
    {
        playerInputHandler = GetComponent<PlayerInputHandler>();
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        rigidBody = GetComponent<Rigidbody>();
        mainCamera = Camera.main;

        //isWalkingHash = Animator.StringToHash("isWalking");
        //isRunningHash = Animator.StringToHash("isRunning");
        isJumpingHash = Animator.StringToHash("isJumping");
        velocityXHash = Animator.StringToHash("velocityX");
        velocityZHash = Animator.StringToHash("velocityZ");

        //SetupJumpVariables();
    }

    private void Start()
    {
        rigidBody.isKinematic = true;
        physCollider.enabled = false;

        currentSpeed = moveSpeed;
    }

    private void Update()
    {
        isGrounded = characterController.isGrounded;
        Debug.Log(currentMovement.y);
        HandleRotation();
        HandleAnimation();
        HandleGravity();
        HandleJumping();
        HandleCrouching();
        HandleSprinting();

        TestMove();
        //HandleMovement();
    }

    private void HandleGravity()
    {
        if (characterController.isGrounded)
        {
            if (isJumpAnimating)
            {
                animator.SetBool(isJumpingHash, false);
                isJumpAnimating = false;
            }

            currentMovement.y = -0.5f;
        }
        else
        {
            float previousYVelocity = currentMovement.y;
            float newYVelocity = currentMovement.y + (gravity * Time.deltaTime);
            float nextYVelocity = (previousYVelocity + newYVelocity) * 0.5f;
            currentMovement.y = nextYVelocity;
        }
    }

    #region Movement
    private Vector3 CalculateWorldDirection()
    {
        inputDir = new Vector3(playerInputHandler.MovementInput.x, 0f, playerInputHandler.MovementInput.y);
        Vector3 worldDirection = transform.TransformDirection(inputDir);

        return worldDirection.normalized;
    }

    private void HandleMovement()
    {
        Vector3 worldDirection = CalculateWorldDirection();
        currentMovement.x = worldDirection.x * currentSpeed;
        currentMovement.z = worldDirection.z * currentSpeed;

        isMovementPressed = inputDir.x != 0 || inputDir.z != 0;

        HandleSprinting();
        HandleCrouching();
        characterController.Move(currentMovement * Time.deltaTime);
    }

    void TestMove()
    {
        Vector3 worldDirection = CalculateWorldDirection();
        currentMovement.x = worldDirection.x * (currentSpeed + moveAcceleration * Time.deltaTime);
        currentMovement.z = worldDirection.z * (currentSpeed + moveAcceleration * Time.deltaTime);

        isMovementPressed = inputDir.x != 0 || inputDir.z != 0;
/*        if (!isMovementPressed)
        {
            currentSpeed -= moveAcceleration * Time.deltaTime;
        }
        else
        {
            currentSpeed += moveAcceleration * Time.deltaTime;
        }*/

        currentSpeed = Mathf.Clamp(currentSpeed, 0f, moveSpeed);
        characterController.Move(currentMovement * Time.deltaTime);
    }

    private void HandleSprinting()
    {
        if (playerInputHandler.SprintPressed && !isSprinting)
        {
            isSprinting = true;
            currentSpeed = moveSpeed * sprintMultiplier;
        }
        else if (!playerInputHandler.SprintPressed && isSprinting)
        {
            isSprinting = false;
            currentSpeed = moveSpeed;
        }
    }

    private void HandleCrouching()
    {
        // SPEED REDUCED
        // TRIGGER A BOOL FOR ANIMATION
        if (playerInputHandler.CrouchPressed && !isCrouching)
        {
            isCrouching = true;
            currentSpeed = moveSpeed / crouchDeduction;
        }
        else if (!playerInputHandler.CrouchPressed && isCrouching)
        {
            isCrouching = false;
            currentSpeed = moveSpeed;
        }
    }
    #endregion

    #region Jumping
    void SetupJumpVariables()
    {
        float timeToApex = maxJumpTime / 2;
        gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        initialJumpVelocity = (2 * maxJumpHeight) / timeToApex;
    }

    private void HandleJumping()
    {
        if (characterController.isGrounded && !isJumping && playerInputHandler.JumpPressed)
        {
            animator.SetBool(isJumpingHash, true);
            isJumpAnimating = true;
            isJumping = true;
            currentMovement.y = initialJumpVelocity * 0.5f;
            Debug.Log(currentMovement.y);
        }
        else if(!playerInputHandler.JumpPressed && isJumping && characterController.isGrounded)
        {
            isJumping = false;
        }
    }
    #endregion

    #region Rotation 
    private void HandleRotation()
    {
        float mouseXRot = playerInputHandler.RotationInput.x * mouseSensitivity;
        float mouseYRot = playerInputHandler.RotationInput.y * mouseSensitivity;

        ApplyHorizontalRotation(mouseXRot);
        ApplyVerticalRotation(mouseYRot);
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

    private void HandleAnimation()
    {
        animator.SetFloat(velocityZHash, inputDir.z * currentSpeed);
        animator.SetFloat(velocityXHash, inputDir.x * currentSpeed);
    }
}

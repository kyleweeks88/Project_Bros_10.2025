using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Parameters")]
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float sprintMultiplier = 2.0f;
    private Vector3 currentMovement;
    private float currentSpeed => walkSpeed * (playerInputHandler.SprintPressed ? sprintMultiplier : 1);
    public bool isMovementPressed;

    [Header("Jump Parameters")]
    private float initialJumpVelocity;
    [SerializeField] private float maxJumpHeight = 1.0f;
    [SerializeField] private float maxJumpTime = 0.5f;
    [SerializeField] private bool isJumping;

    [Header("Look Parameters")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float upDownLookRange = 80f;
    private float verticalRotation;

    [Header("Physics Parameters")]
    [SerializeField] private CapsuleCollider physCollider;
    private Rigidbody rigidBody;
    [SerializeField] private float gravityMultiplier = 1.0f;
    [SerializeField] private float gravity = -9.8f;

    // REFERENCES
    private Camera mainCamera;
    private CharacterController characterController;
    private PlayerInputHandler playerInputHandler;
    [SerializeField] private Animator animator;

    int isWalkingHash;
    int isRunningHash;
    int isJumpingHash;
    bool isJumpAnimating;

    private void Awake()
    {
        playerInputHandler = GetComponent<PlayerInputHandler>();
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        rigidBody = GetComponent<Rigidbody>();
        mainCamera = Camera.main;

        isWalkingHash = Animator.StringToHash("isWalking");
        isRunningHash = Animator.StringToHash("isRunning");
        isJumpingHash = Animator.StringToHash("isJumping");

        SetupJumpVariables();
    }

    private void Start()
    {
        rigidBody.isKinematic = true;
        physCollider.enabled = false;
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleAnimation();
        HandleGravity();
        HandleJumping();
    }

    private Vector3 CalculateWorldDirection()
    {
        Vector3 inputDir = new Vector3(playerInputHandler.MovementInput.x, 0f, playerInputHandler.MovementInput.y);
        Vector3 worldDirection = transform.TransformDirection(inputDir);
        
        return worldDirection.normalized;
    }

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
        }
        else if(!playerInputHandler.JumpPressed && isJumping && characterController.isGrounded)
        {
            //animator.SetBool(isJumpingHash, false);
            isJumping = false;
        }
    }

    private void HandleGravity()
    {
        if(characterController.isGrounded)
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

    private void HandleMovement()
    {
        Vector3 worldDirection = CalculateWorldDirection();
        currentMovement.x = worldDirection.x * currentSpeed;
        currentMovement.z = worldDirection.z * currentSpeed;
        isMovementPressed = currentMovement.x != 0 || currentMovement.z != 0;

        characterController.Move(currentMovement * Time.deltaTime);
    }

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

    private void HandleAnimation()
    {
        bool isWalking = animator.GetBool(isWalkingHash);
        bool isRunning = animator.GetBool(isRunningHash);

        if(isMovementPressed && !isWalking)
        {
            animator.SetBool(isWalkingHash, true);
        }
        else if(!isMovementPressed && isWalking)
        {
            animator.SetBool(isWalkingHash, false);
        }

        if((isMovementPressed && playerInputHandler.SprintPressed) && !isRunning)
        {
            animator.SetBool(isRunningHash, true);
        }
        else if((!isMovementPressed || !playerInputHandler.SprintPressed) && isRunning)
        {
            animator.SetBool(isRunningHash, false);
        }
    }
}

using System.Collections;
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
    [SerializeField] float targetSpeed;
    public bool canMove = true;
    public bool isMovementPressed;
    public bool isSprinting;
    public bool isCrouching;
    public bool isGrounded;
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
    bool isJumpAnimating;

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
        rigidBody.isKinematic = true;
        physCollider.enabled = false;

        targetSpeed = moveSpeed;
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleCrouching();
        HandleSprinting();
        LedgeClimbing();

        HandleGravity();
        HandleJumping();
        HandleAnimation();
    }

    private void HandleGravity()
    {
        isFalling = !characterController.isGrounded && currentMovement.y < -5f && !playerInputHandler.JumpPressed;
        float fallMultiplier = 2f;

        if (characterController.isGrounded)
        {
            if (isJumpAnimating)
            {
                animator.SetBool(isJumpingHash, false);
                isJumpAnimating = false;
            }

            currentMovement.y = -0.5f;
        }
        else if(isFalling)
        {
            float previousYVelocity = currentMovement.y;
            float newYVelocity = currentMovement.y + (gravity * fallMultiplier * Time.deltaTime);
            float nextYVelocity = Mathf.Max((previousYVelocity + newYVelocity) * 0.5f, -10f);
            currentMovement.y = nextYVelocity;
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
    /*private Vector3 CalculateWorldDirection()
    {
        inputDir = new Vector3(playerInputHandler.MovementInput.x, 0f, playerInputHandler.MovementInput.y);
        Vector3 worldDirection = transform.TransformDirection(inputDir);

        return worldDirection.normalized;
    }*/

    private Vector3 NewCalculateWorldDirection(Vector3 _newInputDir)
    {
        inputDir = _newInputDir;
        //inputDir = new Vector3(playerInputHandler.MovementInput.x, playerInputHandler.MovementInput.y, 0f);
        Vector3 worldDirection = transform.TransformDirection(inputDir);

        return worldDirection.normalized;
    }

    /*    private void HandleMovement()
        {
            Vector3 worldDirection = CalculateWorldDirection();
            currentMovement.x = worldDirection.x * currentSpeed;
            currentMovement.z = worldDirection.z * currentSpeed;

            isMovementPressed = inputDir.x != 0 || inputDir.z != 0;

            HandleSprinting();
            HandleCrouching();
            characterController.Move(currentMovement * Time.deltaTime);
        }*/

    void HandleMovement()
    {
        if (canMove)
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

            if (isClimbing)
            {
                Vector3 worldDirection = NewCalculateWorldDirection(new Vector3(0, playerInputHandler.MovementInput.y, 0));
                currentMovement.x = worldDirection.x;
                currentMovement.y = worldDirection.y * currentSpeed * 2f;
            }
            else
            {
                Vector3 worldDirection = NewCalculateWorldDirection(new Vector3(playerInputHandler.MovementInput.x, 0, playerInputHandler.MovementInput.y));
                currentMovement.x = worldDirection.x * currentSpeed;
                currentMovement.z = worldDirection.z * currentSpeed;
            }

            currentSpeed = Mathf.Clamp(currentSpeed, 0.5f, targetSpeed);
            characterController.Move(currentMovement * Time.deltaTime);
        }
        else
        {
            return;
        }
    }

    private void HandleSprinting()
    {
        if (playerInputHandler.SprintPressed && !isSprinting)
        {
            isSprinting = true;
            targetSpeed = moveSpeed * sprintMultiplier;
        }
        else if (!playerInputHandler.SprintPressed && isSprinting)
        {
            isSprinting = false;
            targetSpeed = moveSpeed;
        }
    }

    private void HandleCrouching()
    {
        // SPEED REDUCED
        // TRIGGER A BOOL FOR ANIMATION
        // REDUCE COLLIDER HEIGHT
        if (playerInputHandler.CrouchPressed && !isCrouching)
        {
            isCrouching = true;

            mainCamera.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            characterController.height = 1f;
            characterController.center = new Vector3(0f, 0.5f, 0f);
            targetSpeed = moveSpeed / crouchDeduction;
        }
        else if (!playerInputHandler.CrouchPressed && isCrouching)
        {
            // CHECK FOR OVERHEAD OBJECTS WITH RAYCAST BEFORE STANDING BACK UP
            Vector3 rayOrigin = characterController.transform.position + characterController.center + Vector3.up * (characterController.height / 2f);
            Vector3 rayDirection = Vector3.up; // Or any other desired direction
            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hitInfo, 2f))
            {
                Debug.Log("BONK");
                return;
            }
            else
            {
                isCrouching = false;

                mainCamera.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                characterController.height = 2f;
                characterController.center = new Vector3(0f, 1f, 0f);
                targetSpeed = moveSpeed;
            }
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
        if (!isJumping && characterController.isGrounded && playerInputHandler.JumpPressed)
        {
            animator.SetBool(isJumpingHash, true);
            isJumpAnimating = true;
            isJumping = true;
            currentMovement.y = initialJumpVelocity * 0.5f;
        }
        else if(!playerInputHandler.JumpPressed && isJumping && characterController.isGrounded)
        {
            isJumping = false;
        }
    }

    void LedgeClimbing()
    {
        Vector3 rayOrigin = characterController.transform.position + Vector3.up * (characterController.height / 2f);
        Debug.DrawRay(rayOrigin, transform.forward, Color.red);

        if(isJumping && inputDir.z != 0)
        {
            if(Physics.Raycast(rayOrigin, transform.forward, out RaycastHit firstHit, .5f, ledgeMask))
            {
                if(firstHit.collider.isTrigger)
                {
                    gravity = 0f;
                    canMove = false;
                    characterController.enabled = false;

                    if(Physics.Raycast(firstHit.point + (transform.forward * characterController.radius * 2f) + (Vector3.up * 0.6f * characterController.height), Vector3.down, out RaycastHit secondHit, characterController.height))
                        StartCoroutine(LerpVault(1, secondHit.point, 0.5f));
                }
            }
        }
    }

    IEnumerator LerpVault(int _animDuration, Vector3 _targetPos, float _duration)
    {
        //vaultCoroutineStarted = true;
        int counter = _animDuration;
        while (counter > 0)
        {
            yield return new WaitForSeconds(1);
            counter--;
        }

        float _time = 0f;
        Vector3 _startPosition = transform.position;

        while(_time < _duration)
        {
            transform.position = Vector3.Lerp(_startPosition, _targetPos, _time / _duration);
            _time += Time.deltaTime;
            yield return null;
        }
        transform.position = _targetPos;
        gravity = -48f;
        canMove = true;
        characterController.enabled = true;
    }
    #endregion

    #region Rotation 
    private void HandleRotation()
    {
        float mouseXRot = playerInputHandler.RotationInput.x * mouseSensitivity;
        float mouseYRot = playerInputHandler.RotationInput.y * mouseSensitivity;

        ApplyVerticalRotation(mouseYRot);

        if (!isClimbing)
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

    private void HandleAnimation()
    {
        animator.SetFloat(velocityZHash, inputDir.z * currentSpeed);
        animator.SetFloat(velocityXHash, inputDir.x * currentSpeed);
        animator.SetBool(isFallingHash, isFalling);
        animator.SetBool(isClimbingHash, isClimbing);
    }
}
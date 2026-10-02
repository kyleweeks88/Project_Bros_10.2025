using UnityEngine;

public class CameraController
{
    private readonly Transform cameraTransform;
    private readonly Transform playerTransform;
    private readonly PlayerInputHandler input;

    private ITargetable lockTarget;

    public bool IsLockedOn => lockTarget != null;

    private float yaw;
    public float Yaw => yaw;

    public Vector3 Forward => cameraTransform.forward.normalized;

    public Vector3 HorizontalForward
    {
        get
        {
            Vector3 forward = cameraTransform.forward;
            forward.y = 0f;
            return forward.normalized;
        }
    }

    private const float LookSensitivity = 0.25f;

    private float pitch;
    private const float MinPitch = -40f;
    private const float MaxPitch = 70f;

    private const float FollowDistance = 2.5f;
    private const float FollowHeight = 1.75f;

    private const float FollowSmoothTime = 0.05f;
    private Vector3 cameraPositionVelocity;

    // TARGET LOCK
    private const float LockRotationSmoothTime = 0.12f;
    private float lockYawVelocity;

    private const float LockCameraPadding = 0.25f;
    private const float MaxLockCameraDistance = 6f;

    // CONSTRUCTOR
    public CameraController(
        Transform cameraTransform,
        Transform playerTransform,
        PlayerInputHandler input
        )
    {
        this.cameraTransform = cameraTransform;
        this.playerTransform = playerTransform;
        this.input = input;

        yaw = playerTransform.eulerAngles.y;
        pitch = cameraTransform.eulerAngles.x;
    }

    #region UPDATES

    public void Update()
    {
        if (IsLockedOn)
        {
            HandleTargetLockRotation();
            return;
        }

        HandleRotation();
    }

    public void LateUpdate()
    {
        UpdateCameraTransform();
    }

    #endregion

    // MOVE CAMERA BASED ON YAW, PITCH AND LOCK-ON DISTANCE
    private void UpdateCameraTransform()
    {
        Quaternion rotation = Quaternion.Euler(
            pitch,
            yaw,
            0f
        );

        float cameraDistance = FollowDistance;

        Vector3 cameraTarget =
            playerTransform.position;

        if (IsLockedOn)
        {
            Vector3 playerPosition =
                playerTransform.position;

            Vector3 targetPos =
                lockTarget.TargetTransform.position;

            cameraTarget =
                Vector3.Lerp(
                    playerPosition,
                    targetPos,
                    0.5f
                );

            float targetDistance =
                Vector3.Distance(
                    playerPosition,
                    targetPos
                );

            cameraDistance =
                Mathf.Clamp(
                    FollowDistance +
                    targetDistance * 0.5f +
                    LockCameraPadding,
                    FollowDistance,
                    MaxLockCameraDistance
                );
        }

        Vector3 offset =
            rotation * Vector3.back * cameraDistance;

        offset.y += FollowHeight;

        Vector3 targetPosition =
            cameraTarget + offset;

        cameraTransform.position = Vector3.SmoothDamp(
            cameraTransform.position,
            targetPosition,
            ref cameraPositionVelocity,
            FollowSmoothTime
        );

        cameraTransform.rotation = rotation;
    }

    // YAW AND PITCH BASED ON INPUT
    private void HandleRotation()
    {
        Vector2 lookInput = input.RotationInput;

        yaw += lookInput.x * LookSensitivity;
        pitch -= lookInput.y * LookSensitivity;

        pitch = Mathf.Clamp(
            pitch,
            MinPitch,
            MaxPitch
        );
    }

    #region TARGET LOCK

    public void SetLockTarget(ITargetable target)
    {
        lockTarget = target;
    }

    public void ClearLockTarget()
    {
        lockTarget = null;
        lockYawVelocity = 0f;
    }

    // YAW BASED ON TARGET POSITION, PITCH BASED ON INPUT
    private void HandleTargetLockRotation()
    {
        if (lockTarget == null ||
            lockTarget.TargetTransform == null)
        {
            ClearLockTarget();
            return;
        }

        if (Vector3.Distance(playerTransform.position, lockTarget.TargetTransform.position) > 6f)
        {
            ClearLockTarget();
            return;
        }

        Vector2 lookInput = input.RotationInput;

        pitch -= lookInput.y * LookSensitivity;

        Vector3 direction =
            lockTarget.TargetTransform.position -
            playerTransform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        float targetYaw =
            Mathf.Atan2(
                direction.x,
                direction.z
            ) * Mathf.Rad2Deg;

        yaw = Mathf.SmoothDampAngle(
            yaw,
            targetYaw,
            ref lockYawVelocity,
            LockRotationSmoothTime
        );

        pitch = Mathf.Clamp(
            pitch,
            MinPitch,
            MaxPitch
        );
    }

    #endregion

    public Vector3 GetHorizontalDirection(Vector2 inputDirection)
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 direction =
            forward * inputDirection.y +
            right * inputDirection.x;

        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        return direction.normalized;
    }
}
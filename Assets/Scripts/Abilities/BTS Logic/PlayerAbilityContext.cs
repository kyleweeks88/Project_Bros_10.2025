using UnityEngine;

public class PlayerAbilityContext
{
    public Transform Transform { get; }
    public PlayerInputHandler Input { get; }
    public PlayerLocomotion Locomotion { get; }
    public CameraController Camera { get; }

    public DamageController DamageController { get; }

    public bool IsGrounded => Locomotion.IsGrounded;
    public bool IsFalling => Locomotion.IsFalling;
    public bool IsJumping => Locomotion.IsJumping;
    public float VerticalVelocity
    {
        get => Locomotion.VerticalVelocity;
        set => Locomotion.VerticalVelocity = value;
    }

    public Vector2 MovementInput => Input.MovementInput;

    public PlayerAbilityContext(
        Transform transform,
        PlayerInputHandler input,
        PlayerLocomotion locomotion,
        CameraController camera,
        DamageController damageController)
    {
        Transform = transform;
        Input = input;
        Locomotion = locomotion;
        Camera = camera;
        DamageController = damageController;
    }

    public void SetMovementOverride(Vector3 movement)
    {
        Locomotion.SetAbilityMovementOverride(movement);
    }

    public void ClearMovementOverride()
    {
        Locomotion.ClearAbilityMovementOverride();
    }

    public void SetGravityOverride(bool value)
    {
        Locomotion.SetAbilityGravityOverride(value);
    }

    public void PerformExtraJump(float jumpHeight)
    {
        Locomotion.PerformExtraJump(jumpHeight);
    }

    public Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        return Camera.GetHorizontalDirection(input);
    }

    public Vector3 CameraForward => Camera.Forward;
    public Vector3 CameraHorizontalForward => Camera.HorizontalForward;
}

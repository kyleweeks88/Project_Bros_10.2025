using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class PlayerInputHandler : MonoBehaviour, PlayerInput.IPlayerActions
{
    public Vector2 MovementInput { get; private set; }
    public Vector2 RotationInput { get; private set; }
    public bool JumpPressed {get; private set;}
    public bool SprintPressed { get; private set; }
    public bool CrouchPressed { get; private set; }

    #region Input Unity Action Events
    public event UnityAction interactEvent;
    #endregion

    // Input Asset Singleton
    PlayerInput playerInput;
    public PlayerInput PlayerInput
    {
        get
        {
            if(playerInput != null) { return playerInput; }
            return playerInput = new PlayerInput();
        }
    }

    private void OnEnable() => EnableGameplayInput();

    public void EnableGameplayInput()
    {
        PlayerInput.MenuInteraction.Disable();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        PlayerInput.Player.Enable();
        PlayerInput.Player.SetCallbacks(this);
    }

    private void OnDisable() => DisableAllInput();

    public void DisableAllInput()
    {
        playerInput.Player.Disable();
        playerInput.MenuInteraction.Disable();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        playerInput.Player.Jump.performed += inputInfo => JumpPressed = true;
        playerInput.Player.Jump.canceled += inputInfo => JumpPressed = false;
    }

    public void OnMovement(InputAction.CallbackContext context)
    {
        playerInput.Player.Movement.performed += inputInfo
            => MovementInput = inputInfo.ReadValue<Vector2>();
        playerInput.Player.Movement.canceled += inputInfo
            => MovementInput = inputInfo.ReadValue<Vector2>();
    }

    public void OnRotation(InputAction.CallbackContext context)
    {
        playerInput.Player.Rotation.performed += inputInfo
            => RotationInput = inputInfo.ReadValue<Vector2>();
        playerInput.Player.Rotation.canceled += inputInfo
            => RotationInput = inputInfo.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        playerInput.Player.Sprint.performed += inputInfo => SprintPressed = true;
        playerInput.Player.Sprint.canceled += inputInfo => SprintPressed = false;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (interactEvent != null &&
            context.phase == InputActionPhase.Performed)
            interactEvent.Invoke();
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        playerInput.Player.Crouch.started += inputInfo => CrouchPressed = true;
        playerInput.Player.Crouch.canceled += inputInfo => CrouchPressed = false;
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class PlayerInputHandler : MonoBehaviour, PlayerInput.IPlayerActions
{
    // ============================================================
    // INPUT VALUES
    // ============================================================

    public Vector2 MovementInput { get; private set; }
    public Vector2 RotationInput { get; private set; }

    // public bool JumpPressed { get; private set; }
    public bool SprintPressed { get; private set; }
    // public bool CrouchPressed { get; private set; }
    // public bool TestAttackPressed { get; private set; }


    // ============================================================
    // INPUT EVENTS
    // ============================================================

    public event UnityAction targetLockEvent;

    public event UnityAction interactEvent;
    public event UnityAction perspectiveChangeEvent;

    public event UnityAction jumpEventPressed;
    public event UnityAction jumpEventStarted;
    public event UnityAction jumpEventCancelled;

    public event UnityAction<AbilitySlot> abilitySlotEvent;
    public event UnityAction<MeleeAttackType> meleeAttackStartedEvent;
    public event UnityAction<MeleeAttackType> meleeAttackReleasedEvent;


    // ============================================================
    // INPUT ASSET
    // ============================================================

    private PlayerInput playerInput;

    public PlayerInput PlayerInput
    {
        get
        {
            if (playerInput == null)
            {
                playerInput = new PlayerInput();
            }

            return playerInput;
        }
    }


    // ============================================================
    // SINGLETON
    // ============================================================

    public static PlayerInputHandler Instance { get; private set; }


    // ============================================================
    // UNITY LIFECYCLE
    // ============================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        EnableGameplayInput();
    }

    private void OnDisable()
    {
        DisableAllInput();
    }


    // ============================================================
    // INPUT ENABLE / DISABLE
    // ============================================================

    public void EnableGameplayInput()
    {
        PlayerInput.MenuInteraction.Disable();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        PlayerInput.Player.SetCallbacks(this);
        PlayerInput.Player.Enable();
    }

    public void DisableAllInput()
    {
        PlayerInput.Player.Disable();
        PlayerInput.MenuInteraction.Disable();

        MovementInput = Vector2.zero;
        RotationInput = Vector2.zero;

        //JumpPressed = false;
        SprintPressed = false;
        //CrouchPressed = false;
    }


    // ============================================================
    // MOVEMENT
    // ============================================================

    public void OnMovement(InputAction.CallbackContext context)
    {
        MovementInput = context.ReadValue<Vector2>();
    }


    // ============================================================
    // ROTATION
    // ============================================================

    public void OnRotation(InputAction.CallbackContext context)
    {
        RotationInput = context.ReadValue<Vector2>();
    }


    // ============================================================
    // JUMP
    // ============================================================

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            jumpEventPressed?.Invoke();
            jumpEventStarted?.Invoke();
        }

        if (context.canceled)
        {
            //JumpPressed = false;
            jumpEventCancelled?.Invoke();
        }
    }


    // ============================================================
    // SPRINT
    // ============================================================

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.started || context.performed)
        {
            SprintPressed = true;
        }

        if (context.canceled)
        {
            SprintPressed = false;
        }
    }


    // ============================================================
    // CROUCH
    // ============================================================

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.started || context.performed)
        {
            //CrouchPressed = true;
        }

        if (context.canceled)
        {
            //CrouchPressed = false;
        }
    }


    // ============================================================
    // INTERACT
    // ============================================================

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            interactEvent?.Invoke();
        }
    }


    // ============================================================
    // PERSPECTIVE
    // ============================================================

    public void OnPerspective(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            perspectiveChangeEvent?.Invoke();
        }
    }


    // ============================================================
    // ABILITIES
    // ============================================================

    public void OnAbility1(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            abilitySlotEvent?.Invoke(AbilitySlot.Slot1);
        }
    }

    public void OnAbility2(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            abilitySlotEvent?.Invoke(AbilitySlot.Slot2);
        }
    }

    public void OnAbility3(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            abilitySlotEvent?.Invoke(AbilitySlot.Slot3);
        }
    }

    public void OnAbility4(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            abilitySlotEvent?.Invoke(AbilitySlot.Slot4);
        }
    }


    // ============================================================
    // MELEE ATTACKS
    // ============================================================

    public void OnHeavyAttack1(InputAction.CallbackContext context)
    {
        if (context.started)
            meleeAttackStartedEvent?.Invoke(
                MeleeAttackType.HeavyAttack1);

        if (context.canceled)
            meleeAttackReleasedEvent?.Invoke(
                MeleeAttackType.HeavyAttack1);
    }

    public void OnHeavyAttack2(InputAction.CallbackContext context)
    {
        if (context.started)
            meleeAttackStartedEvent?.Invoke(
                MeleeAttackType.HeavyAttack2);

        if (context.canceled)
            meleeAttackReleasedEvent?.Invoke(
                MeleeAttackType.HeavyAttack2);
    }

    public void OnLightAttack1(InputAction.CallbackContext context)
    {
        if (context.started)
            meleeAttackStartedEvent?.Invoke(
                MeleeAttackType.LightAttack1);
    }

    public void OnLightAttack2(InputAction.CallbackContext context)
    {
        if (context.started)
            meleeAttackStartedEvent?.Invoke(
                MeleeAttackType.LightAttack2);
    }

    // ============================================================
    // TARGET LOCK
    // ============================================================

    public void OnTargetLock(InputAction.CallbackContext context)
    {
        if (context.started)
            targetLockEvent?.Invoke();
    }
}


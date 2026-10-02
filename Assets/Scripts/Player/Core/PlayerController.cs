using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour, IDamageable
{
    // CONTROLLERS
    private DamageController damageController;
    private MeleeCombatController meleeCombat;
    private CharacterController characterController;
    private CameraController cameraController;
    private PlayerActionController actions;
    private PlayerAbilityController abilities;
    public PlayerAbilityController Abilities => abilities;
    private TargetLockController targetLock;
    public TargetLockController TargetLock => targetLock;

    // CONTEXT
    private PlayerActionContext actionContext;
    private PlayerAbilityContext abilityContext;

    // OTHER 
    private EntityStats myStats;

    private PlayerInteraction interaction;

    private PlayerInputHandler input;
    public PlayerInputHandler Input => input;

    private PlayerLocomotion locomotion;
    public PlayerLocomotion Locomotion => locomotion;


    // TEMPORARY FOR PROTO TESTING ???
    [Header("Melee Style")]
    [SerializeField] private MeleeStyleData meleeStyle;
    [SerializeField] private GameObject meleeHitbox;


    #region LIFECYCLE

    private void Awake()
    {
        Camera mainCamera = Camera.main;

        myStats = GetComponent<EntityStats>();
        interaction = GetComponent<PlayerInteraction>();
        characterController = GetComponent<CharacterController>();
        input = PlayerInputHandler.Instance;

        locomotion = new PlayerLocomotion(
            characterController,
            input,
            mainCamera,
            transform
        );

        cameraController = new CameraController(
            mainCamera.transform,
            transform,
            input//,
            //locomotion
        );

        targetLock = new TargetLockController(
            transform,
            mainCamera.transform,
            5f,
            LayerMask.GetMask("Targetable")
            );

        damageController = new DamageController(
            myStats,
            locomotion);

        meleeCombat = new MeleeCombatController(
            meleeHitbox,
            transform
            );

        meleeCombat.SetMeleeStyle(meleeStyle);

        abilityContext = new PlayerAbilityContext(
            transform,
            input,
            locomotion,
            cameraController,
            damageController
        );

        abilities = new PlayerAbilityController(abilityContext);

        actionContext = new PlayerActionContext(
            transform,
            input,
            locomotion,
            meleeCombat,
            damageController
        );

        actions = new PlayerActionController(actionContext);

        input.targetLockEvent += OnTargetLock;
        input.abilitySlotEvent += OnAbilitySlot;
        input.jumpEventPressed += OnJumpPressed;
        input.meleeAttackStartedEvent += OnMeleeInputStarted;
        input.meleeAttackReleasedEvent += OnMeleeInputReleased;
        actions.ActionEnded += OnActionEnded;
        interaction.Interacted += OnInteracted;
        locomotion.Landed += abilities.NotifyGrounded;
        myStats.Died += OnDeath;
    }

    private void Update()
    {
        cameraController.Update();

        locomotion.UpdateRotation(cameraController.Yaw);

        abilities.UpdateAbilities();
        locomotion.Update();

        actions.Update();
    }

    private void LateUpdate()
    {
        cameraController.LateUpdate();
    }

    #endregion

    private void OnTargetLock()
    {
        targetLock.ToggleLock();

        if(targetLock.IsLocked)
        {
            cameraController.SetLockTarget(
                targetLock.CurrentTarget);

            Debug.Log(
                $"Locked onto: {targetLock.CurrentTarget.TargetTransform.name}"
                );
        }
        else
        {
            cameraController.ClearLockTarget();

            Debug.Log("Target unlocked.");
        }
    }

    private void OnInteracted(Interactable interactedObject)
    {
        if (actions.IsBusy)
            return;

        // CLIMBABLE SURFACE INTERACTION
        if(interactedObject is ClimbableSurface)
        {
            ClimbableSurface surface =
                (ClimbableSurface)interactedObject;

            ClimbAction climbAction =
                new ClimbAction(
                    actionContext,
                    surface);

            actions.TryStartAction(climbAction);

            Debug.Log(
                $"PlayerController interacted with " +
                $"climbable surface: {interactedObject.name}"
            );

            // CHANGE THIS!!! vvvvvvvvvvvvvvvvvv
            abilities.NotifyGrounded(); // ?????
            Debug.Log("TEMPORARY CODE WARNING!!!!!!" +
                "THIS NEEDS TO CHANGE, CLIMB SHOULD NOT RESET" +
                "JUMPS IN THIS WAY.");
        }
        // NOT A CLIMBABLE SURFACE INTERACTION
        else
        {
            Debug.Log(
                $"PlayerController interacted with " +
                $"an interactable: {interactedObject.name}"
            );
        }
    }

    private void OnAbilitySlot(AbilitySlot slot)
    {
        abilities.TryActivateSlot(slot);
    }

    private void OnJumpPressed()
    {
        if(actions.GetCurrentAction<ClimbAction>()
            != null)
        {
            actions.EndAction();
            return;
        }

        // Normal jumping belongs to PlayerLocomotion.
        // If the player is airborne, give ExtraJump a chance to consume this press.
        if (locomotion.IsGrounded)
            return;

        ExtraJumpAbility extraJump =
            abilities.GetAbility<ExtraJumpAbility>();

        extraJump?.TryActivate();
    }

    private void OnMeleeInputStarted(
        MeleeAttackType attackType)
    {
        if (actions.IsBusy)
        {
            MeleeAttackAction currentAttack =
                actions.GetCurrentAction<MeleeAttackAction>();

            if (currentAttack == null)
                return;

            if(currentAttack.CanBufferComboInput)
            {
                currentAttack.BufferComboInput();
                return;
            }

            return;
        }

        StartMeleeAttack(attackType);
    }

    private void StartMeleeAttack(
        MeleeAttackType attackType)
    {
        bool isAirborneAttack =
            !locomotion.IsGrounded;

        if(!isAirborneAttack)
        {
            if (!meleeCombat.StartChain(attackType))
                return;
        }
        else
        {
            if (!meleeCombat.StartAirborneAttack(attackType))
                return;
        }

        locomotion.UpdateRotation(
            cameraController.Yaw);

        MeleeAttackData attackData =
            meleeCombat.CurrentAttackData;

        if (attackData == null)
            return;

        MeleeAttackAction attack =
            new MeleeAttackAction(
                actionContext,
                attackData,
                isAirborneAttack
            );

        actions.TryStartAction(attack);
    }

    private void OnMeleeInputReleased(
    MeleeAttackType attackType)
    {
        MeleeAttackAction meleeAttack =
            actions.GetCurrentAction<MeleeAttackAction>();

        if (meleeAttack == null)
            return;

        meleeAttack.ReleaseAttack();
    }

    private void OnActionEnded(PlayerAction endedAction)
    {
        if (endedAction is not MeleeAttackAction meleeAttack)
            return;

        if(meleeAttack.IsAirborneAttack)
        {
            meleeCombat.ResetChain();
            return;
        }

        if(!meleeAttack.ComboInputBuffered)
        {
            meleeCombat.ResetChain();
            return;
        }

        meleeAttack.ConsumeComboInput();

        if(!meleeCombat.AdvanceChain())
        {
            meleeCombat.ResetChain();
            return;
        }

        MeleeAttackData nextAttackData =
            meleeCombat.CurrentAttackData;

        if(nextAttackData == null)
        {
            meleeCombat.ResetChain();
            return;
        }

        locomotion.UpdateRotation(
            cameraController.Yaw);

        MeleeAttackAction nextAttack =
            new MeleeAttackAction(
                actionContext,
                nextAttackData,
                false
                );

        actions.TryStartAction(nextAttack);
    }

    private void OnDestroy()
    {
        if (input != null)
        {
            input.targetLockEvent -= OnTargetLock;
            input.abilitySlotEvent -= OnAbilitySlot;
            input.jumpEventPressed -= OnJumpPressed;
            input.meleeAttackStartedEvent -= OnMeleeInputStarted;
            input.meleeAttackReleasedEvent -= OnMeleeInputReleased;
            actions.ActionEnded -= OnActionEnded;
            interaction.Interacted -= OnInteracted;
            locomotion.Landed -= abilities.NotifyGrounded;
            myStats.Died -= OnDeath;
        }

        locomotion?.Dispose();
    }

    public void ReceiveDamage(DamageInfo damageInfo)
    {
        damageController.ReceiveDamage(damageInfo);
    }

    private void OnDeath()
    {
        transform.position = Vector3.zero;

        Debug.Log("YOU HAVE DIED!");
    }
}

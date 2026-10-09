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


    // TEMPORARY FOR PROTO TESTING ??? //
    [Header("Melee Style")]
    [SerializeField] private MeleeStyleData meleeStyle;
    [SerializeField] private GameObject meleeHitbox;

    // DODGING STUFF
    private const float LightAttackDodgeChordWindow = 0.12f;
    private float pendingLightAttackTimer;
    private MeleeAttackType? pendingLightAttackType;
    [SerializeField, Min(0f)]
    private float dodgeIFrameDuration = 0.25f;
    [SerializeField, Min(0f)]
    private float dodgeRecoveryDuration = 0.2f;

    // BLOCKING STUFF
    private const float HeavyAttackChordWindow = 0.12f;
    private float pendingHeavyAttackTimer;
    private MeleeAttackType? pendingHeavyAttackType;
    public bool IsBlocking =>
        actions != null &&
        actions.GetCurrentAction<BlockAction>() != null &&
        locomotion != null &&
        locomotion.IsGrounded;

    // DEBUG GIZMOS AND STUFF
    [Header("Block Debug")]
    [SerializeField, Min(0.1f)]
    private float blockDebugRadius = 3f;


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
            transform,
            myStats
        );

        cameraController = new CameraController(
            mainCamera.transform,
            transform,
            input
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
            damageController,
            myStats
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
        locomotion.Landed += myStats.RefillJuggleCount;
        myStats.Died += OnDeath;
    }

    private void Update()
    {
        cameraController.Update();

        locomotion.UpdateRotation(cameraController.Yaw);

        abilities.UpdateAbilities();
        locomotion.Update();

        UpdateHeavyAttackChord();
        UpdateLightAttackChord();
        actions.Update();
    }

    private void LateUpdate()
    {
        cameraController.LateUpdate();
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
            locomotion.Landed -= myStats.RefillJuggleCount;
            myStats.Died -= OnDeath;
        }

        locomotion?.Dispose();
    }
    #endregion


    public bool ApplyStatUpgrade(EntityStatUpgradeData upgradeData)
    {
        if(myStats == null) return false;

        return myStats.ApplyUpgrade(upgradeData);
    }

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

    #region MELEE INPUT
    private void OnMeleeInputStarted(MeleeAttackType attackType)
    {
        if (IsBlocking)
            return;

        if (attackType == MeleeAttackType.LightAttack1 ||
            attackType == MeleeAttackType.LightAttack2)
        {
            if (pendingLightAttackType.HasValue &&
                input.LightAttack1Held &&
                input.LightAttack2Held)
            {
                pendingLightAttackType = null;
                TryStartDodge();
                return;
            }

            if (!pendingLightAttackType.HasValue)
            {
                pendingLightAttackType = attackType;
                pendingLightAttackTimer = 0f;
            }

            return;
        }

        if (IsHeavyAttack(attackType))
        {
            if (input.HeavyAttack1Held &&
                input.HeavyAttack2Held &&
                locomotion.IsGrounded)
            {
                pendingHeavyAttackType = null;
                TryStartBlock();
                return;
            }

            if (!pendingHeavyAttackType.HasValue)
            {
                pendingHeavyAttackType = attackType;
                pendingHeavyAttackTimer = 0f;
            }

            return;
        }

        HandleMeleeInputStarted(attackType);
    }

    private void HandleMeleeInputStarted(MeleeAttackType attackType)
    {
        if (actions.IsBusy)
        {
            MeleeAttackAction currentAttack =
                actions.GetCurrentAction<MeleeAttackAction>();

            if (currentAttack == null)
                return;

            if (currentAttack.CanBufferComboInput)
                currentAttack.BufferComboInput();

            return;
        }

        StartMeleeAttack(attackType);
    }

    private bool IsHeavyAttack(MeleeAttackType attackType)
    {
        return attackType == MeleeAttackType.HeavyAttack1 ||
               attackType == MeleeAttackType.HeavyAttack2;
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

    private void OnMeleeInputReleased(MeleeAttackType attackType)
    {
        if (actions.GetCurrentAction<BlockAction>() != null)
        {
            actions.EndAction();
            return;
        }

        if (pendingHeavyAttackType == attackType)
        {
            pendingHeavyAttackType = null;

            MeleeAttackAction previousAttack =
                actions.GetCurrentAction<MeleeAttackAction>();

            HandleMeleeInputStarted(attackType);

            MeleeAttackAction currentAttack =
                actions.GetCurrentAction<MeleeAttackAction>();

            if (currentAttack != null &&
                currentAttack != previousAttack)
            {
                currentAttack.ReleaseAttack();
            }

            return;
        }

        MeleeAttackAction meleeAttack =
            actions.GetCurrentAction<MeleeAttackAction>();

        meleeAttack?.ReleaseAttack();
    }

    private void UpdateHeavyAttackChord()
    {
        if (!pendingHeavyAttackType.HasValue)
            return;

        pendingHeavyAttackTimer += Time.deltaTime;

        if (pendingHeavyAttackTimer < HeavyAttackChordWindow)
            return;

        MeleeAttackType attackType =
            pendingHeavyAttackType.Value;

        pendingHeavyAttackType = null;

        HandleMeleeInputStarted(attackType);
    }

    private void UpdateLightAttackChord()
    {
        if (!pendingLightAttackType.HasValue)
            return;

        pendingLightAttackTimer += Time.deltaTime;

        if (pendingLightAttackTimer < LightAttackDodgeChordWindow)
            return;

        MeleeAttackType attackType =
            pendingLightAttackType.Value;

        pendingLightAttackType = null;

        HandleMeleeInputStarted(attackType);
    }

    private void TryStartDodge()
    {
        if (actions.IsBusy)
            return;

        DodgeAction dodge = new DodgeAction(
            actionContext,
            dodgeIFrameDuration,
            dodgeRecoveryDuration);

        actions.TryStartAction(dodge);
    }

    private void TryStartBlock()
    {
        if (actions.IsBusy || !locomotion.IsGrounded)
            return;

        actions.TryStartAction(
            new BlockAction(actionContext));
    }
    #endregion

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

    public void ReceiveDamage(DamageInfo damageInfo)
    {
        damageController.ReceiveDamage(
            damageInfo,
            transform,
            IsBlocking);
    }

    private void OnDeath()
    {
        transform.position = Vector3.zero;

        Debug.Log("YOU HAVE DIED!");
    }

    #region DEBUG GIZMOS
    private void OnDrawGizmosSelected()
    {
        EntityStats stats = GetComponent<EntityStats>();

        if (stats == null)
            return;

        Vector3 forward = transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.001f)
            return;

        forward.Normalize();

        float halfAngle = stats.BlockAngle * 0.5f;
        Vector3 origin = transform.position + Vector3.up * 0.1f;

        Color previousColor = Gizmos.color;
        Gizmos.color = IsBlocking ? Color.green : Color.cyan;

        Vector3 previousPoint =
            origin +
            Quaternion.AngleAxis(-halfAngle, Vector3.up) *
            forward *
            blockDebugRadius;

        const int segments = 24;

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);

            Vector3 direction =
                Quaternion.AngleAxis(angle, Vector3.up) * forward;

            Vector3 point = origin + direction * blockDebugRadius;

            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }

        Vector3 leftEdge =
            Quaternion.AngleAxis(-halfAngle, Vector3.up) *
            forward;

        Vector3 rightEdge =
            Quaternion.AngleAxis(halfAngle, Vector3.up) *
            forward;

        Gizmos.DrawLine(origin, origin + leftEdge * blockDebugRadius);
        Gizmos.DrawLine(origin, origin + rightEdge * blockDebugRadius);

        Gizmos.color = previousColor;

        // DODGE GIZMOS
        CharacterController playerCollider =
        GetComponent<CharacterController>();

        if (playerCollider != null)
        {
            Color prevColor = Gizmos.color;

            bool dodgeIFramesActive =
                Application.isPlaying &&
                damageController != null &&
                damageController.IsInvulnerable;

            Gizmos.color = dodgeIFramesActive
                ? Color.green
                : Color.red;

            Gizmos.DrawWireCube(
                playerCollider.bounds.center,
                playerCollider.bounds.size);

            Gizmos.color = prevColor;
        }
    }
    #endregion
}

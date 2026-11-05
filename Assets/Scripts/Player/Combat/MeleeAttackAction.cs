using UnityEngine;

public class MeleeAttackAction : PlayerAction
{
    // TESTING PURPOSES
    // MIGHT EVENTUALLY BE AFFECTED BY EntityStats
    // OR SOME ABILITY MODIFIER
    private const float AirborneAttackGravityMultiplier = 0.25f;

    public override PlayerActionType ActionType =>
    PlayerActionType.MeleeAttack;

    private readonly MeleeAttackData attackData;
    public MeleeAttackData AttackData => attackData;

    #region ATTACK CANCELLING
    private bool wasCancelled;
    public bool WasCancelled => wasCancelled;
    public bool CanCancelBeforeHitbox =>
        isCharging ||
        (execution != null && execution.CanCancelBeforeHitbox);
    #endregion

    #region ATTACK PHASE STUFF
    private MeleeAttackExecution execution;

    private bool isCharging;
    private float chargeTimer;
    private bool attackReleased;

    public bool IsRecovering =>
    execution != null && execution.IsRecovering;
    #endregion

    #region COMBO STUFF
    public bool CanBufferComboInput
    {
        get
        {
            if (execution == null || !execution.IsRecovering)
                return false;

            return execution.RecoveryElapsed >=
                attackData.ComboWindowStart -
                attackData.InputBufferTime;
        }
    }

    private bool comboInputBuffered;
    public bool ComboInputBuffered => comboInputBuffered;

    public void BufferComboInput()
    {
        comboInputBuffered = true;

        Debug.Log(
            $"Combo input buffered: {attackData.AttackType}"
            );
    }

    public bool ConsumeComboInput()
    {
        if (!comboInputBuffered)
            return false;

        comboInputBuffered = false;
        return true;
    }

    public bool IsComboWindowOpen { get; private set; }

    #endregion

    #region CHARGE STUFF
    private float chargePercent;
    public float ChargePercent => chargePercent;

    public float AttackMultiplier
    {
        get
        {
            return Mathf.Lerp(
                attackData.MinimumPower,
                attackData.MaximumPower,
                chargePercent);
        }
    }
    #endregion

    private Vector3 attackDirection;

    // PENDING ATTACK MOVEMENT
    private Vector3 pendingAttackMovementDirection;
    private float pendingAttackMovementDistance;
    private float pendingAttackMovementDuration;
    private bool hasPendingAttackMovement;
    private bool pendingAttackUsesApexHangTime;

    private readonly bool isAirborneAttack;
    public bool IsAirborneAttack => isAirborneAttack;

    // CONSTRUCTOR
    public MeleeAttackAction(
        PlayerActionContext context,
        MeleeAttackData attackData,
        bool isAirborneAttack)
        : base(context)
    {
        this.attackData = attackData;
        this.isAirborneAttack = isAirborneAttack;
    }

    #region FUNCTIONS

    public override void OnStarted()
    {
        wasCancelled = false;
        hasPendingAttackMovement = false;
        pendingAttackUsesApexHangTime = false;

        isCharging = attackData.CanCharge;
        chargePercent = 0f;
        chargeTimer = 0f;
        attackReleased = false;

        execution = new MeleeAttackExecution(
            attackData,
            Context.MeleeCombat,
            Context.DamageController,
            Context.Transform,
            () => AttackMultiplier,
            ApplyAttackMovement,
            OnAttackConnected
            );

        IsComplete = false;
        IsComboWindowOpen = false;
        comboInputBuffered = false;

        // CAPTURE THE PLAYER'S FACING WHEN ATTACK BEGINS
        attackDirection = Context.Transform.forward;
        attackDirection.y = 0f;
        attackDirection.Normalize();

        // LOCK THE PLAYER MODELS ROTATION
        Context.Locomotion.SetRotationLocked(true);
        Context.Locomotion.SetMovementLocked(true);

        Context.MeleeCombat.SetHitboxActive(false);


        if(isAirborneAttack)
        {
            Context.Locomotion.StartAirborneAttack(
                AirborneAttackGravityMultiplier
                );
        }

        if (isCharging)
        {
            Debug.Log($"Charging {attackData.AttackType}");
        }
        else
        {
            execution.Start();

            Debug.Log(
                $"Melee Attack Started: {attackData.AttackType}"
            );
        }
    }

    public void ReleaseAttack()
    {
        if (!isCharging)
            return;

        attackReleased = true;
    }

    public override void OnEnded()
    {
        hasPendingAttackMovement = false;
        pendingAttackUsesApexHangTime = false;

        // TESTING PURPOSES
        if(isAirborneAttack)
        {
            Context.Locomotion.EndAirborneAttack();
            Context.Locomotion.SetGravityMultiplier(1f);
        }
        // TESTING

        Context.Locomotion.CancelAttackMovement();

        Context.Locomotion.SetRotationLocked(false);
        Context.Locomotion.SetMovementLocked(false);

        Context.MeleeCombat.SetHitboxActive(false);

        Debug.Log(
            $"Melee Attack Ended: {attackData.AttackType}"
        );

        execution?.End();
    }

    public override void OnUpdate()
    {
        if (isCharging)
        {
            UpdateCharging();
            return;
        }

        execution?.Update();
        UpdateRecovery();

        if (execution != null && execution.IsComplete)
            IsComplete = true;
    }

    private void UpdateCharging()
    {
        chargeTimer += Time.deltaTime;

        chargePercent = Mathf.Clamp01(
            chargeTimer / attackData.MaxChargeTime
        );

        if (attackReleased ||
            chargeTimer >= attackData.MaxChargeTime)
        {
            Debug.Log(
                $"Charge Released: {chargeTimer:F2}s"
            );

            chargeTimer = 0f;
            isCharging = false;
            execution.Start();
        }
    }

    private void UpdateRecovery()
    {
        if (execution == null || !execution.IsRecovering)
            return;

        if (!IsComboWindowOpen &&
            execution.RecoveryElapsed >=
            attackData.ComboWindowStart)
        {
            IsComboWindowOpen = true;

            Debug.Log(
                $"COMBO WINDOW OPEN:" +
                $"{attackData.AttackType}"
                );
        }

        // IF THE PLAYER BUFFERED THE NEXT ATTACK,
        // TRANSITION AS SOON AS THE COMBO WINDOW OPENS.
        if(IsComboWindowOpen &&
            comboInputBuffered)
        {
            Debug.Log(
                $"COMBO TRANSITION:" +
                $"{attackData.AttackType}"
                );

            execution.CompleteRecovery();
            IsComplete = true;
            return;
        }
    }

    private void ApplyAttackMovement()
    {
        if (attackData.MovementDuration <= 0f ||
            (attackData.HorizontalMovementDistance <= 0f &&
             attackData.VerticalMovementDistance <= 0f))
        {
            return;
        }

        // Grounded attacks keep their existing horizontal movement.
        if (!isAirborneAttack)
        {
            Context.Locomotion.ApplyAttackMovement(
                attackDirection,
                attackData.HorizontalMovementDistance,
                attackData.MovementDuration
            );

            return;
        }

        Vector3 aerialMovement =
            attackDirection * attackData.HorizontalMovementDistance;

        float verticalMovement = 0f;

        switch (attackData.PlayerVerticalMoveDirection)
        {
            case VerticalAttackDirection.Up:
                verticalMovement = attackData.VerticalMovementDistance;
                break;

            case VerticalAttackDirection.Down:
                verticalMovement = -attackData.VerticalMovementDistance;
                break;
        }

        aerialMovement.y = verticalMovement;

        float distance = aerialMovement.magnitude;

        // An attack with no effective movement doesn't use the free movement.
        if (distance <= 0.001f)
            return;

        Vector3 direction = aerialMovement.normalized;
        bool useApexHangTime = verticalMovement > 0f;

        // The first airborne attack with movement gets to move immediately.
        if (Context.Locomotion.TryUseFirstAirborneAttackMovement())
        {
            if (!Context.Stats.TryConsumeJuggleCount())
                return;

            Context.Locomotion.ApplyAttackMovement(
                direction,
                distance,
                attackData.MovementDuration,
                useApexHangTime
            );

            return;
        }

        // Later airborne attacks save their movement until this attack hits.
        pendingAttackMovementDirection = direction;
        pendingAttackMovementDistance = distance;
        pendingAttackMovementDuration = attackData.MovementDuration;
        pendingAttackUsesApexHangTime = useApexHangTime;
        hasPendingAttackMovement = true;
    }

    private void OnAttackConnected()
    {
        if (!hasPendingAttackMovement)
            return;

        hasPendingAttackMovement = false;

        if (!Context.Stats.TryConsumeJuggleCount())
            return;

        Context.Locomotion.ApplyAttackMovement(
            pendingAttackMovementDirection,
            pendingAttackMovementDistance,
            pendingAttackMovementDuration,
            pendingAttackUsesApexHangTime
        );

        pendingAttackUsesApexHangTime = false;
    }

    public bool TryCancelBeforeHitbox()
    {
        if (!CanCancelBeforeHitbox)
            return false;

        wasCancelled = true;
        comboInputBuffered = false;

        return true;
    }

    #endregion
}

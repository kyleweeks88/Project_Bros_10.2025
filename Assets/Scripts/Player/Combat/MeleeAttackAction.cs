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

    #region ATTACK PHASE STUFF
    private enum AttackPhase
    {
        Charging,
        Startup,
        Active,
        Recovery
    }

    private AttackPhase phase;
    private float timer;
    private bool attackReleased;

    public bool IsRecovering =>
    phase == AttackPhase.Recovery;
    #endregion

    #region COMBO STUFF
    public bool CanBufferComboInput
    {
        get
        {
            if (phase != AttackPhase.Recovery)
                return false;

            return timer >=
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
        chargePercent = 0f;
        timer = 0f;
        attackReleased = false;
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


        // TESTING PURPOSES
        if(isAirborneAttack)
        {
            Context.Locomotion.StartAirborneAttack(
                AirborneAttackGravityMultiplier
                );
        }
        // TESTING

        if (attackData.CanCharge)
        {
            phase = AttackPhase.Charging;

            Debug.Log(
                $"Charging {attackData.AttackType}"
            );
        }
        else
        {
            phase = AttackPhase.Startup;

            Debug.Log(
                $"Melee Attack Started: {attackData.AttackType}"
            );
        }
    }

    public void ReleaseAttack()
    {
        if (phase != AttackPhase.Charging)
            return;

        attackReleased = true;
    }

    public override void OnEnded()
    {
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
    }

    public override void OnUpdate()
    {
        timer += Time.deltaTime;

        switch (phase)
        {
            case AttackPhase.Charging:
                UpdateCharging();
                break;

            case AttackPhase.Startup:
                UpdateStartup();
                break;

            case AttackPhase.Active:
                UpdateActive();
                break;

            case AttackPhase.Recovery:
                UpdateRecovery();
                break;
        }
    }

    private void UpdateCharging()
    {
        chargePercent = Mathf.Clamp01(
        timer / attackData.MaxChargeTime
        );

        if (attackReleased ||
            timer >= attackData.MaxChargeTime)
        {
            Debug.Log(
                $"Charge Released: {timer:F2}s"
            );

            timer = 0f;
            phase = AttackPhase.Startup;
        }
    }

    private void UpdateStartup()
    {
        if (timer < attackData.StartupTime)
            return;

        ApplyAttackMovement();

        DamageInfo damageInfo =
            Context.DamageController.CalculateOutgoingDamage(
                AttackMultiplier,
                Vector3.zero,
                DamageSourceType.Melee
                );

        Context.MeleeCombat.SetHitboxDamage(
            damageInfo,
            attackData.HorizontalKnockbackForce,
            attackData.VerticalKnockbackForce,
            attackData.TargetVerticalKnockbackDirection
        );

        Context.MeleeCombat.SetHitboxActive(true);

        phase = AttackPhase.Active;
        timer = 0f;

        if (attackData.CanCharge)
        {
            Debug.Log(
            $"Melee Hitbox ACTIVE | " +
            $"Charge: {chargePercent:P0} | " +
            $"Power: {AttackMultiplier:F2}"
            );  
        }
    }

    private void UpdateActive()
    {
        if (timer < attackData.ActiveTime)
            return;

        timer = 0f;
        phase = AttackPhase.Recovery;

        Context.MeleeCombat.SetHitboxActive(false);
    }

    private void UpdateRecovery()
    {
        if(!IsComboWindowOpen &&
            timer >= attackData.ComboWindowStart)
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

            IsComplete = true;
            return;
        }

        // NO BUFFERED INPUT, SO FINISH THE FULL RECOVERY
        if (timer >= attackData.RecoveryTime)
        {
            IsComplete = true;
        }
    }

    private void ApplyAttackMovement()
    {
        if (attackData.HorizontalMovementDistance <= 0f &&
            attackData.VerticalMovementDistance <= 0f)
        {
            return;
        }

        Vector3 movementDirection = attackDirection;

        // Grounded attacks remain horizontal.
        if (!isAirborneAttack)
        {
            Context.Locomotion.ApplyAttackMovement(
                movementDirection,
                attackData.HorizontalMovementDistance,
                attackData.MovementDuration
            );

            return;
        }

        // Build the airborne movement using
        // independent horizontal and vertical distances.
        Vector3 horizontalMovement =
            attackDirection *
            attackData.HorizontalMovementDistance;

        float verticalMovement = 0f;

        switch (attackData.PlayerVerticalMoveDirection)
        {
            case VerticalAttackDirection.Up:
                verticalMovement =
                    attackData.VerticalMovementDistance;
                break;

            case VerticalAttackDirection.Down:
                verticalMovement =
                    -attackData.VerticalMovementDistance;
                break;

            case VerticalAttackDirection.None:
                break;
        }

        Vector3 aerialMovement =
            horizontalMovement;

        aerialMovement.y = verticalMovement;

        float distance = aerialMovement.magnitude;

        if (distance <= 0.001f)
            return;

        Vector3 aerialDirection =
            aerialMovement.normalized;

        Context.Locomotion.ApplyAttackMovement(
            aerialDirection,
            distance,
            attackData.MovementDuration
        );
    }

    #endregion
}
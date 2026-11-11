using System;
using UnityEngine;

public class MeleeAttackExecution
{
    private enum Phase
    {
        NotStarted,
        Startup,
        Active,
        Recovery,
        Complete
    }

    private readonly MeleeAttackData attackData;
    private readonly IMeleeCombat meleeCombat;
    private readonly DamageController damageController;
    private readonly Transform attackerTransform;
    private readonly Func<float> getDamageMultiplier;
    private readonly Action applyAttackMovement;
    private readonly Action onAttackConnected;

    private Phase phase = Phase.NotStarted;
    private float timer;

    public bool IsComplete => phase == Phase.Complete;
    public bool IsInStartup => phase == Phase.Startup;
    public float StartupProgress =>
        phase != Phase.Startup || attackData.StartupTime <= 0f
            ? 1f
            : Mathf.Clamp01(timer / attackData.StartupTime);
    public bool IsRecovering => phase == Phase.Recovery;
    public float RecoveryElapsed => IsRecovering ? timer : 0f;

    public bool CanCancelBeforeHitbox =>
        phase == Phase.Startup &&
        timer < attackData.StartupTime;

    public MeleeAttackExecution(
        MeleeAttackData attackData,
        IMeleeCombat meleeCombat,
        DamageController damageController,
        Transform attackerTransform,
        Func<float> getDamageMultiplier,
        Action applyAttackMovement,
        Action onAttackConnected)
    {
        this.attackData = attackData;
        this.meleeCombat = meleeCombat;
        this.damageController = damageController;
        this.attackerTransform = attackerTransform;
        this.getDamageMultiplier = getDamageMultiplier;
        this.applyAttackMovement = applyAttackMovement;
        this.onAttackConnected = onAttackConnected;
    }

    public void Start()
    {
        timer = 0f;
        phase = Phase.Startup;
        meleeCombat.SetHitboxActive(false);
    }

    public void Update()
    {
        if (phase == Phase.NotStarted ||
            phase == Phase.Complete)
        {
            return;
        }

        timer += Time.deltaTime;

        switch (phase)
        {
            case Phase.Startup:
                UpdateStartup();
                break;

            case Phase.Active:
                UpdateActive();
                break;

            case Phase.Recovery:
                UpdateRecovery();
                break;
        }
    }

    private void UpdateStartup()
    {
        if (timer < attackData.StartupTime)
            return;

        applyAttackMovement?.Invoke();

        DamageInfo damageInfo =
            damageController.CalculateOutgoingDamage(
                getDamageMultiplier(),
                Vector3.zero,
                DamageSourceType.Melee,
                attackerTransform.position);

        meleeCombat.SetHitboxDamage(
            damageInfo,
            attackData.HorizontalKnockbackForce,
            attackData.VerticalKnockbackForce,
            attackData.TargetVerticalKnockbackDirection,
            onAttackConnected);

        meleeCombat.SetHitboxActive(true);

        timer = 0f;
        phase = Phase.Active;
    }

    private void UpdateActive()
    {
        if (timer < attackData.ActiveTime)
            return;

        meleeCombat.SetHitboxActive(false);

        timer = 0f;
        phase = Phase.Recovery;
    }

    private void UpdateRecovery()
    {
        if (timer >= attackData.RecoveryTime)
            phase = Phase.Complete;
    }

    public void CompleteRecovery()
    {
        if (phase == Phase.Recovery)
            phase = Phase.Complete;
    }

    public void End()
    {
        meleeCombat.SetHitboxActive(false);
        phase = Phase.Complete;
    }
}

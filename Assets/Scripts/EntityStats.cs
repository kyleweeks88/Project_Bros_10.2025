using System;
using UnityEngine;

public class EntityStats : MonoBehaviour
{
    #region REFERENCES

    [Header("Movement")]
    [SerializeField] private float baseMoveSpeed = 1.0f;
    private float moveSpeedModifier;
    public float MoveSpeed =>
        MathF.Max(0f, baseMoveSpeed * (1f + moveSpeedModifier));

    [Header("Jumps")]
    [SerializeField] private float baseJumpHeight = 1.5f;
    private float jumpHeightModifier;
    public float JumpHeight =>
    MathF.Max(0f, baseJumpHeight * (1f + jumpHeightModifier));

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Combat")]
    [SerializeField] private float damageOutput = 10f;

    #region BLOCKING VALUES
    [Header("Blocking")]
    [SerializeField, Range(0f, 1f)]
    private float baseBlockDamageMitigation = 0.5f;
    private float blockDamageMitigationModifier;
    [SerializeField, Range(0f, 1f)]
    private float baseBlockKnockbackMitigation = 0.75f;
    private float blockKnockbackMitigationModifier;
    [SerializeField, Range(0f, 360f)]
    private float baseBlockAngle = 60f;
    private float blockAngleModifier;

    public float BlockDamageMitigation =>
    Mathf.Clamp01(
        baseBlockDamageMitigation *
        (1f + blockDamageMitigationModifier));

    public float BlockKnockbackMitigation =>
        Mathf.Clamp01(
            baseBlockKnockbackMitigation *
            (1f + blockKnockbackMitigationModifier));

    public float BlockAngle =>
        Mathf.Clamp(
            baseBlockAngle * (1f + blockAngleModifier),
            0f,
            360f);
    #endregion

    #region KNOCKBACK VALUES
    [Header("Knockback")]
    [SerializeField, Range(1f, 10f)] 
    private float knockbackResistance = 1f;
    [SerializeField, Min(0f)]
    private float baseKnockbackThreshold = 0f;
    [Tooltip("Seconds after threshold loss before refill begins.")]
    [SerializeField, Min(0f)]
    private float knockbackThresholdRefillDelay = 1f;
    [Tooltip("Knockback threshold restored per second.")]
    [SerializeField, Min(0f)]
    private float knockbackThresholdRefillRate = 10f;
    private float knockbackThresholdModifier;
    private float knockbackThresholdRefillTimer;

    public float KnockbackResistance =>
        Mathf.Clamp(knockbackResistance, 1f, 10f);

    public float MaxKnockbackThreshold =>
        Mathf.Max(
            0f,
            baseKnockbackThreshold * (1f + knockbackThresholdModifier));

    public float CurrentKnockbackThreshold { get; private set; }
    public float KnockbackThresholdFill =>
        MaxKnockbackThreshold <= 0f
            ? 1f
            : Mathf.Clamp01(
                CurrentKnockbackThreshold / MaxKnockbackThreshold);
    #endregion

    [Header("Various")]
    [SerializeField] private int maxJuggleCount = 1;
    private int juggleCount;


    public int JuggleCount => juggleCount;
    public int MaxJuggleCount => maxJuggleCount;
    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public float DamageOutput => damageOutput;

    public bool IsDead { get; private set; }

    public event Action<float> DamageTaken;
    public event Action Died;

    #endregion


    #region LIFECYCLE

    private void Awake()
    {
        CurrentHealth = MaxHealth;
        CurrentKnockbackThreshold = MaxKnockbackThreshold;
        juggleCount = Mathf.Max(0, maxJuggleCount);
    }

    private void Update()
    {
        if (CurrentKnockbackThreshold >= MaxKnockbackThreshold)
            return;

        if (knockbackThresholdRefillTimer > 0f)
        {
            knockbackThresholdRefillTimer = Mathf.Max(
                0f,
                knockbackThresholdRefillTimer - Time.deltaTime);

            return;
        }

        CurrentKnockbackThreshold = Mathf.MoveTowards(
            CurrentKnockbackThreshold,
            MaxKnockbackThreshold,
            knockbackThresholdRefillRate * Time.deltaTime);
    }

    #endregion


    public bool ApplyUpgrade(EntityStatUpgradeData upgradeData)
    {
        if (upgradeData == null)
            return false;

        switch(upgradeData.StatType)
        {
            case EntityStatUpgradeType.JumpHeight:
                AddJumpHeightModifier(upgradeData.PercentageIncrease);
                return true;

            case EntityStatUpgradeType.MoveSpeed:
                AddMoveSpeedModifier(upgradeData.PercentageIncrease);
                return true;

            case EntityStatUpgradeType.BlockDamageMitigation:
                AddBlockDamageMitigationModifier(upgradeData.PercentageIncrease);
                return true;

            case EntityStatUpgradeType.BlockKnockbackMitigation:
                AddBlockKnockbackMitigationModifier(upgradeData.PercentageIncrease);
                return true;

            case EntityStatUpgradeType.BlockAngle:
                AddBlockAngleModifier(upgradeData.PercentageIncrease);
                return true;

            case EntityStatUpgradeType.KnockbackThreshold:
                AddKnockbackThresholdModifier(upgradeData.PercentageIncrease);
                return true;

            default:
                return false;
        }
    }

    #region KNOCKBACK
    public bool ShouldApplyKnockback(float knockbackForceMagnitude)
    {
        if (knockbackForceMagnitude <= 0.001f)
            return false;

        float maxThreshold = MaxKnockbackThreshold;

        // A zero maximum keeps the existing unrestricted knockback behavior.
        if (maxThreshold <= 0f)
            return true;

        bool isRefilling =
            CurrentKnockbackThreshold < maxThreshold &&
            knockbackThresholdRefillTimer <= 0f;

        // During refill, hits still apply knockback but leave the meter alone.
        if (isRefilling)
            return true;

        // The meter is empty and waiting for its refill delay.
        // Keep applying knockback without restarting the delay.
        if (CurrentKnockbackThreshold <= 0f)
            return true;

        knockbackThresholdRefillTimer =
            knockbackThresholdRefillDelay;

        CurrentKnockbackThreshold = Mathf.Max(
            0f,
            CurrentKnockbackThreshold - knockbackForceMagnitude);

        // The hit that empties the meter applies knockback.
        return CurrentKnockbackThreshold <= 0f;
    }

    public void AddKnockbackThresholdModifier(float percentageIncrease)
    {
        knockbackThresholdModifier += percentageIncrease / 100f;

        CurrentKnockbackThreshold = Mathf.Min(
            CurrentKnockbackThreshold,
            MaxKnockbackThreshold);
    }

    public void RemoveKnockbackThresholdModifier(float percentageIncrease)
    {
        knockbackThresholdModifier -= percentageIncrease / 100f;

        CurrentKnockbackThreshold = Mathf.Min(
            CurrentKnockbackThreshold,
            MaxKnockbackThreshold);
    }
    #endregion

    #region BLOCKING
    public void AddBlockDamageMitigationModifier(float percentageIncrease)
    {
        blockDamageMitigationModifier += percentageIncrease / 100f;
    }

    public void AddBlockKnockbackMitigationModifier(float percentageIncrease)
    {
        blockKnockbackMitigationModifier += percentageIncrease / 100f;
    }

    public void AddBlockAngleModifier(float percentageIncrease)
    {
        blockAngleModifier += percentageIncrease / 100f;
    }
    #endregion

    #region MOVEMENT
    public void AddMoveSpeedModifier(float percentageIncrease)
    {
        moveSpeedModifier += percentageIncrease / 100f;
    }

    public void RemoveMoveSpeedModifier(float percentageIncrease)
    {
        moveSpeedModifier -= percentageIncrease / 100f;
    }
    #endregion

    #region Jumps
    public void AddJumpHeightModifier(float percentageIncrease)
    {
        jumpHeightModifier += percentageIncrease / 100f;
    }

    public void RemoveJumpHeightModifier(float percentageIncrease)
    {
        jumpHeightModifier -= percentageIncrease / 100f;
    }
    #endregion

    #region Juggle Count
    public bool TryConsumeJuggleCount()
    {
        if (juggleCount <= 0)
            return false;

        juggleCount--;
        return true;
    }

    public void RefillJuggleCount()
    {
        juggleCount = Mathf.Max(0, maxJuggleCount);
    }

    public void AddJuggleCount(int amount = 1)
    {
        if (amount <= 0)
            return;

        maxJuggleCount += amount;
        juggleCount = Mathf.Min(
            juggleCount + amount,
            maxJuggleCount
            );
    }
    #endregion

    #region Health & Dying
    public void TakeDamage(float damage)
    {
        if (IsDead)
            return;

        if (damage <= 0f)
            return;

        CurrentHealth -= damage;

        CurrentHealth = Mathf.Max(
            CurrentHealth,
            0f
        );

        DamageTaken?.Invoke(damage);

        Debug.Log($"this {this.name} took: {damage}" +
            $" damage."
            );

        if (CurrentHealth <= 0f)
        {
            Debug.Log($"{this.name} has died!");

            Die();
        }
    }

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        Died?.Invoke();
    }
    #endregion
}

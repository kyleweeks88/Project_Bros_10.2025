using UnityEngine;

public enum VerticalAttackDirection
{
    None,
    Up,
    Down
}

[CreateAssetMenu(
    fileName = "MeleeAttackData",
    menuName = "Combat/Melee Attack Data"
)]
public class MeleeAttackData : ScriptableObject
{
    [Header("Name")]
    [SerializeField] private string attackName;

    [Header("Input Settings")]
    [SerializeField] private MeleeAttackType attackType;

    [Header("Attack Phases")]
    [SerializeField] private float startupTime = 0.2f;
    [Tooltip("The period which the attack hitbox is active")]
    [SerializeField] private float activeTime = 0.2f;
    [SerializeField] private float recoveryTime = 0.6f;

    [Header("Attack Movement")]
    [SerializeField] private float movementDuration = 0.1f;
    [SerializeField] private float horizontalMovementDistance = 0f;
    [SerializeField] private float verticalMovementDistance = 0f;
    
    [SerializeField] private VerticalAttackDirection
                    playerVerticalMoveDirection =
                    VerticalAttackDirection.None;

    public float MovementDuration => movementDuration;
    public float HorizontalMovementDistance 
        => horizontalMovementDistance;
    public float VerticalMovementDistance 
        => verticalMovementDistance;
    public VerticalAttackDirection PlayerVerticalMoveDirection
        => playerVerticalMoveDirection;


    [Header("Combo Settings")]
    [Tooltip("How long into recovery before the next combo " +
        "input is accepted.")]
    [SerializeField] private float comboWindowStart = 0.15f;
    [Tooltip("Extra cushion of time before recovery begins" +
        "to press the next attack input")]
    [SerializeField] private float inputBufferTime = 0.25f;

    [Header("Charged Attack Settings")]
    [SerializeField] private bool canCharge = false;
    [SerializeField] private float maxChargeTime = 1.5f;
    [SerializeField] private float minimumPower = 1f;
    [SerializeField] private float maximumPower = 2f;

    [Header("Knockback Force")]
    [SerializeField] private VerticalAttackDirection
                targetVerticalKnockbackDirection =
                VerticalAttackDirection.None;
    [SerializeField] private float horizontalKnockbackForce = 0f;
    [SerializeField] private float verticalKnockbackForce = 0f;
    public float HorizontalKnockbackForce => horizontalKnockbackForce;
    public float VerticalKnockbackForce => verticalKnockbackForce;
    public VerticalAttackDirection TargetVerticalKnockbackDirection
    => targetVerticalKnockbackDirection;


    public string AttackName => attackName;
    public MeleeAttackType AttackType => attackType;
    public float StartupTime => startupTime;
    public float ActiveTime => activeTime;
    public float RecoveryTime => recoveryTime;
    public float ComboWindowStart => comboWindowStart;
    public float InputBufferTime => inputBufferTime;

    public bool CanCharge => canCharge;
    public float MaxChargeTime => maxChargeTime;

    public float MinimumPower => minimumPower;
    public float MaximumPower => maximumPower;
}
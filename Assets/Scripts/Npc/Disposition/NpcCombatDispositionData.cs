using UnityEngine;

public abstract class NpcCombatDispositionData : ScriptableObject
{
    [Header("Approach")]
    [SerializeField, Min(0f)]
    private float attackApproachPadding = 4f;

    [SerializeField, Min(0f)]
    private float zigZagLateralDistance = 1.25f;

    [SerializeField, Min(0.5f)]
    private float zigZagWaypointSpacing = 3f;

    [Header("Melee Approach Preference")]
    [SerializeField]
    private NpcMeleeApproachPreference meleeApproachPreference =
        NpcMeleeApproachPreference.Any;

    [SerializeField, Range(0f, 100f)]
    private float preferredMeleeApproachChance = 100f;

    [SerializeField, Min(0f)]
    private float meleeSlotSearchRetreatDistance = 3f;

    [SerializeField, Min(0.1f)]
    private float meleeSlotSearchDuration = 2f;

    [SerializeField, Min(0.05f)]
    private float meleeSlotSearchRetryInterval = 0.3f;

    [Header("Melee")]
    [SerializeField, Min(0f)]
    private float meleeEngagementDistance = 2f;

    [Header("Blocking")]
    [SerializeField]
    private bool blockingEnabled;

    [SerializeField, Min(1)]
    private int hitsToTriggerBlock = 3;

    [SerializeField, Min(0.1f)]
    private float blockHitCountWindow = 1f;

    [SerializeField, Min(0f)]
    private float blockPressureWindow = 1.5f;

    [Header("Ranged")]
    [SerializeField, Min(0f)]
    private float preferredRangedDistance = 8f;

    [SerializeField, Min(0f)]
    private float rangedDistanceTolerance = 0.5f;

    public float AttackApproachPadding => attackApproachPadding;
    public float ZigZagLateralDistance => zigZagLateralDistance;
    public float ZigZagWaypointSpacing => zigZagWaypointSpacing;
    public NpcMeleeApproachPreference MeleeApproachPreference =>
        meleeApproachPreference;
    public float PreferredMeleeApproachChance =>
        preferredMeleeApproachChance;
    public float MeleeSlotSearchRetreatDistance =>
        meleeSlotSearchRetreatDistance;
    public float MeleeSlotSearchDuration => meleeSlotSearchDuration;
    public float MeleeSlotSearchRetryInterval =>
        meleeSlotSearchRetryInterval;
    public float MeleeEngagementDistance => meleeEngagementDistance;
    public bool BlockingEnabled => blockingEnabled;
    public int HitsToTriggerBlock => hitsToTriggerBlock;
    public float BlockHitCountWindow => blockHitCountWindow;
    public float BlockPressureWindow => blockPressureWindow;
    public float PreferredRangedDistance => preferredRangedDistance;
    public float RangedDistanceTolerance => rangedDistanceTolerance;

    public abstract NpcCombatDisposition CreateRuntime(
        NpcController npc);
}

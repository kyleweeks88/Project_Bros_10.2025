using UnityEngine;

public enum EntityStatUpgradeType
{
    JumpHeight,
    MoveSpeed,
    BlockDamageMitigation,
    BlockKnockbackMitigation,
    BlockAngle
}

[CreateAssetMenu(
    fileName = "EntityStatUpgrade",
    menuName = "Stats/Entity Stat Upgrade")]
public class EntityStatUpgradeData : ScriptableObject
{
    [SerializeField] private EntityStatUpgradeType statType;
    [SerializeField] private float percentageIncrease = 20f;

    public EntityStatUpgradeType StatType => statType;
    public float PercentageIncrease => percentageIncrease;
}
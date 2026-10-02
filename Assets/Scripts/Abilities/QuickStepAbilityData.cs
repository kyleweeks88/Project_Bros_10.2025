using UnityEngine;

[CreateAssetMenu(
    fileName = "QuickStepAbilityData",
    menuName = "Player Abilities/QuickStep")]
public class QuickStepAbilityData : PlayerAbilityData
{
    [Header("QuickStep Settings")]
    [SerializeField] private float quickStepDistance = 4f;
    [SerializeField] private float quickStepDuration = 0.15f;
    [SerializeField] private float cooldown = 1f;

    public float QuickStepDistance => quickStepDistance;
    public float QuickStepDuration => quickStepDuration;
    public float Cooldown => cooldown;
}

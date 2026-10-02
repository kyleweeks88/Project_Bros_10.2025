using UnityEngine;

[CreateAssetMenu(
    fileName = "AirDashAbilityData",
    menuName = "Player Abilities/Air Dash")]
public class AirDashAbilityData : PlayerAbilityData
{
    [Header("Air Dash Settings")]
    [SerializeField] private float dashDistance = 4f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float cooldown = 1f;

    public float DashDistance => dashDistance;
    public float DashDuration => dashDuration;
    public float Cooldown => cooldown;
}
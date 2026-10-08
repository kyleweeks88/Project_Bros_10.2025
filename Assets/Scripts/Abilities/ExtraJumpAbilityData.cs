using UnityEngine;

[CreateAssetMenu(
    fileName = "ExtraJumpAbilityData",
    menuName = "Player Abilities/Extra Jump")]
public class ExtraJumpAbilityData : PlayerAbilityData
{
    [Header("Extra Jump Settings")]
    [SerializeField] private int extraJumps = 1;

    //[SerializeField] private float jumpHeight = 2f;

    public int ExtraJumps => extraJumps;

    //public float JumpHeight =>
    //    jumpHeight;
}
using UnityEngine;

[CreateAssetMenu(
    fileName = "StompAbilityData",
    menuName = "Player Abilities/Stomp")]
public class StompAbilityData : PlayerAbilityData
{
    [Header("Stomp Settings")]
    [SerializeField] private float impactRadius = 3f;
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float upwardModifier = 1f;
    [SerializeField] private float damage = 25f;

    // NOT USED YET....
    // [SerializeField] private float physicsForce;

    public float ImpactRadius => impactRadius;
    public float KnockbackForce => knockbackForce;
    public float UpwardModifier => upwardModifier;
    public float Damage => damage;
}
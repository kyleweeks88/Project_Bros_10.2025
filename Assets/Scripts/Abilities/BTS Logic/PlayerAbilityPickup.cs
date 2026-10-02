using UnityEngine;

public class PlayerAbilityPickup : MonoBehaviour
{
    [Header("Ability")]
    [SerializeField] private PlayerAbilityData abilityData;

    [Header("Pickup")]
    [SerializeField] private bool destroyOnPickup = true;

    private bool collected;

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player == null)
            return;

        if (abilityData == null)
        {
            Debug.LogWarning(
                $"{name} has no PlayerAbilityData assigned."
            );

            return;
        }

        bool added =
            player.Abilities.AddAbility(abilityData);

        if (!added)
            return;

        collected = true;

        Debug.Log(
            $"Unlocked ability: {abilityData.AbilityName}"
        );

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
    }
}
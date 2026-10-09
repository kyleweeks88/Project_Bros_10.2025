using UnityEngine;

public class StatUpgradePickup : MonoBehaviour
{
    [Header("Upgrade")]
    [SerializeField] private EntityStatUpgradeData upgradeData;

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

        if (upgradeData == null)
        {
            Debug.LogWarning(
                $"{name} has no EntityStatUpgradeData assigned."
            );

            return;
        }

        if (!player.ApplyStatUpgrade(upgradeData))
            return;

        collected = true;

        Debug.Log(
            $"Applied stat upgrade: {upgradeData.StatType}"
        );

        if (destroyOnPickup)
            Destroy(gameObject);
    }
}
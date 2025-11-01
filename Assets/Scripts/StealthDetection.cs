using UnityEngine;

public class StealthDetection : MonoBehaviour
{
    public float noiseRadius;
    private float currentNoiseRadius;

    private PlayerController playerController;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
    } 

    private void Start()
    {
        currentNoiseRadius = 0f;
    }

    void Update()
    {
        NoiseSource();
    }

    void NoiseSource()
    {
        if (playerController.isMovementPressed)
        {
            if (playerController.isCrouching)
            {
                currentNoiseRadius = noiseRadius / 2f;
            }
            else if (playerController.isSprinting)
            {
                currentNoiseRadius = noiseRadius * 2f;
            }
            else
            {
                currentNoiseRadius = noiseRadius;
            }
        }
        else
        {
            currentNoiseRadius = 0f;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, currentNoiseRadius);
    }
}

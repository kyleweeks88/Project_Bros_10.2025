using UnityEngine;

public class StealthDetection : MonoBehaviour
{
    public float crouchNoiseRadius;
    public float walkNoiseRadius;
    public float sprintNoiseRadius;
    private float currentNoiseRadius;

    private PlayerController playerController;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
    } 

    private void Start()
    {
        currentNoiseRadius = 0.5f;
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
                currentNoiseRadius = crouchNoiseRadius;
            }
            else if (playerController.isSprinting)
            {
                currentNoiseRadius = sprintNoiseRadius;
            }
            else
            {
                currentNoiseRadius = walkNoiseRadius;
            }
        }
        else
        {
            currentNoiseRadius = 0.5f;
        }
    }

    private void OnDrawGizmos()
    {
        if(currentNoiseRadius >= sprintNoiseRadius)
        {
            Gizmos.color = Color.red;
        }
        else if(currentNoiseRadius >= walkNoiseRadius)
        {
            Gizmos.color = Color.yellow;
        }
        else if(currentNoiseRadius >= crouchNoiseRadius)
        {
            Gizmos.color = Color.green;
        }
        else
        {
            Gizmos.color = Color.blue;
        }
        Gizmos.DrawWireSphere(transform.position, currentNoiseRadius);
    }
}

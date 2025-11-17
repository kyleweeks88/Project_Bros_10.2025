using UnityEngine;

public class StealthDetection : MonoBehaviour
{
    public float crouchNoiseRadius;
    public float walkNoiseRadius;
    public float sprintNoiseRadius;
    private float currentNoiseRadius;

    private PlayerStateMachine playerStateMachine;

    void Awake()
    {
        playerStateMachine = GetComponent<PlayerStateMachine>();
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
        // SHOULD ALL THIS BE SET BY THE STATES IN THE STATE MACHINE
        // INSTEAD OF CHECKING THE CONDITIONALS HERE???
        if (playerStateMachine.isMovementPressed)
        {
            if (playerStateMachine.isCrouching)
            {
                currentNoiseRadius = crouchNoiseRadius;
            }
            else if (playerStateMachine.isSprinting)
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

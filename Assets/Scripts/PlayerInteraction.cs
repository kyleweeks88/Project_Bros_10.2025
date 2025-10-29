using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    // REFERENCES
    [SerializeField] private Camera playerCam;
    private PlayerInputHandler playerInputHandler;

    [SerializeField] private float interactRange;

    private void OnEnable()
    {
        playerInputHandler.interactEvent += PlayerInteract;
    }

    private void OnDisable()
    {
        playerInputHandler.interactEvent -= PlayerInteract;
    }

    private void Awake()
    {
        playerInputHandler = GetComponent<PlayerInputHandler>();
    }

    private void PlayerInteract()
    {
        // CHECK THE interactRange RADIUS FOR COLLIDERS
        Collider[] interactColliders = Physics.OverlapSphere(transform.position, interactRange);
        foreach(Collider collider in interactColliders)
        {
            // IS THAT collider INTRACTABLE?
            if(collider.TryGetComponent(out Interactable _interactedObject))
            {
                // SHOOT A RAY FROM PLAYER POV TO SEE IF ITS LOOKING AT THE INTERACTABLE
                if(Physics.Raycast(playerCam.transform.position, playerCam.transform.forward, out RaycastHit hitInfo, 100f))
                {
                    if (hitInfo.collider.gameObject == _interactedObject.gameObject)
                        _interactedObject.Interact(this);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}

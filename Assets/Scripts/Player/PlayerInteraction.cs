using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public event System.Action<Interactable> Interacted;

    // REFERENCES
    [SerializeField] private LayerMask interactMask;
    [SerializeField] private Camera playerCam;

    [SerializeField] private float interactRange;

    private void OnEnable()
    {
        PlayerInputHandler.Instance.interactEvent += PlayerInteract;
    }

    private void OnDisable()
    {
        PlayerInputHandler.Instance.interactEvent -= PlayerInteract;
    }

    private void PlayerInteract()
    {
        Ray ray = playerCam.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f));

        if (!Physics.Raycast(
            ray,
            out RaycastHit hitInfo,
            interactRange,
            interactMask))
        {
            return;
        }

        if (hitInfo.collider.TryGetComponent(
            out Interactable interactedObject))
        {
            interactedObject.Interact(gameObject);
            Interacted?.Invoke(interactedObject);
        }
    }
}

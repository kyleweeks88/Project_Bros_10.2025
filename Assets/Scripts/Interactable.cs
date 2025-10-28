using UnityEngine;

public class Interactable : MonoBehaviour
{
    public virtual void Interact(PlayerInteraction _interactor)
    {
        Debug.Log(_interactor.name + " interacted with " + gameObject.name);
    }
}

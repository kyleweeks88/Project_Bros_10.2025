using UnityEngine;

public class Interactable : MonoBehaviour
{
    [SerializeField] protected GameObject interactor;

    public virtual void Interact(GameObject _interactor)
    {
        //interactor = _interactor;
        Debug.Log(_interactor.name + " interacted with " + gameObject.name);
    }
}

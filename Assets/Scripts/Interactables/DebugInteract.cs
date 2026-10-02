using UnityEngine;

public class DebugInteract : Interactable
{
    public override void Interact(GameObject interactor)
    {
        Debug.Log(interactor.name + " interacted with object.");
    }
}

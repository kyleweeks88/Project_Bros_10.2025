using UnityEngine;

public class SimpleSwitch : Interactable
{
    //private Material myMat;
    private Renderer myRend;
    Color colorStart = Color.red;
    Color colorEnd = Color.green;

    private void Start()
    {
        myRend = GetComponent<Renderer>();
        myRend.material.color = colorStart;
    }

    public override void Interact(PlayerInteraction _interactor)
    {
        SwitchColor();
    }

    private void SwitchColor()
    {
        if (myRend.material.color == colorStart)
        {
            myRend.material.color = colorEnd;
        }
        else
        {
            myRend.material.color = colorStart;
        }
    }
}

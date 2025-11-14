using UnityEngine;
using System.Collections;

public class Ladder : Interactable
{
    PlayerStateMachine playerStateMachine;
    CharacterController characterController;
    public Transform topLoc;
    public Transform bottomLoc;
    public Transform ledgeLoc;
    float ladderLength;
    bool exitCoroutineStarted;
    bool enterCoroutineStarted;
    bool atBottomOfLadder;

    private void Awake()
    {
        ladderLength = Vector3.Distance(bottomLoc.position, topLoc.position);
    }

    private void Update()
    {
        if (interactor != null)
        {
            // IF PLAYER ENTERED AT BOTTOM AND HAS REACHED THE TOP
            if(atBottomOfLadder && Vector3.Distance(bottomLoc.position, interactor.transform.position) >= ladderLength)
            {
                if (!exitCoroutineStarted)
                {
                    playerStateMachine.enabled = false;
                    characterController.enabled = false;
                    playerStateMachine.transform.position = new Vector3(ledgeLoc.position.x, ledgeLoc.position.y, ledgeLoc.position.z);
                    playerStateMachine.transform.rotation = new Quaternion(ledgeLoc.rotation.x, ledgeLoc.rotation.y, ledgeLoc.rotation.z, 0);
                    StartCoroutine(ExitCountdownTimer(1));
                }
            }
            // IF PLAYER ENTERED AT TOP AND HAS REACHED THE BOTTOM
            else if (!atBottomOfLadder && Vector3.Distance(bottomLoc.position, interactor.transform.position) <= 0.5f)
            {
                if (!exitCoroutineStarted)
                {
                    playerStateMachine.enabled = false;
                    characterController.enabled = false;
                    playerStateMachine.transform.position = new Vector3(bottomLoc.position.x, bottomLoc.position.y, bottomLoc.position.z);
                    playerStateMachine.transform.rotation = new Quaternion(bottomLoc.rotation.x, bottomLoc.rotation.y, bottomLoc.rotation.z, 0);
                    StartCoroutine(ExitCountdownTimer(1));
                }
            }
        }
        else
        {
            return;
        }
    }

    public override void Interact(GameObject _interactor)
    {
        if (playerStateMachine == null)
        {
            interactor = _interactor;
            playerStateMachine = _interactor.GetComponent<PlayerStateMachine>();
            characterController = _interactor.GetComponent<CharacterController>();
            // DISABLE PLAYER MOVEMENT WHILE TRANSITIONING TO LADDER
            playerStateMachine.enabled = false;
            characterController.enabled = false;
            // DETERMINE PLAYER TOP OR BOTTOM
            atBottomOfLadder = _interactor.transform.position.y < topLoc.position.y;
            if (atBottomOfLadder)
            {
                Debug.Log("Entered Bottom");
                playerStateMachine.transform.position = new Vector3(bottomLoc.position.x, bottomLoc.position.y, bottomLoc.position.z);
                playerStateMachine.transform.rotation = new Quaternion(bottomLoc.rotation.x, bottomLoc.rotation.y, bottomLoc.rotation.z, 0);
            }
            else
            {
                Debug.Log("Entered Top");
                playerStateMachine.transform.position = new Vector3(topLoc.position.x, topLoc.position.y, topLoc.position.z);
                playerStateMachine.transform.rotation = new Quaternion(topLoc.rotation.x, topLoc.rotation.y, topLoc.rotation.z, 0);
            }
            // play getting on ladder animation
            // wait til done and reenable the controller
            if (!enterCoroutineStarted)
                StartCoroutine(EnterCountdownTimer(1));
        }
        else
        {
            ExitLadder();
        }
    }

    IEnumerator EnterCountdownTimer(int _seconds)
    {
        enterCoroutineStarted = true;
        int counter = _seconds;
        while(counter > 0)
        {
            yield return new WaitForSeconds(1);
            counter--;
        }

        EnterLadder();
    }

    IEnumerator ExitCountdownTimer(int _seconds)
    {
        exitCoroutineStarted = true;
        int counter = _seconds;
        while (counter > 0)
        {
            yield return new WaitForSeconds(1);
            counter--;
        }

        ExitLadder();
    }

    void EnterLadder()
    {
        StopCoroutine(EnterCountdownTimer(0));
        playerStateMachine.enabled = true;
        characterController.enabled = true;
        playerStateMachine.isClimbing = true;
        enterCoroutineStarted = false;
    }

    void ExitLadder()
    {
        StopCoroutine(ExitCountdownTimer(0));
        playerStateMachine.enabled = true;
        characterController.enabled = true;
        playerStateMachine.isClimbing = false;
        playerStateMachine = null;
        exitCoroutineStarted = false;
        interactor = null;
    }
}

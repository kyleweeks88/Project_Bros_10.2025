using UnityEngine;

// SUB STATE
public class PlayerCrouchState : PlayerBaseState
{
    public PlayerCrouchState(PlayerStateMachine _currentContext, PlayerStateFactory _playerStateFactory)
    : base(_currentContext, _playerStateFactory) { }

    public override void EnterState()
    {
        bs_Ctx.sm_isCrouching = true;

        //bs_Ctx.sm_mainCamera.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        //bs_Ctx.sm_characterController.height = 1f;
        //bs_Ctx.sm_characterController.center = new Vector3(0f, 0.5f, 0f);

        AdjustCameraAndCollider(
            new Vector3(0f, 1.2f, 0f),
            1f,
            new Vector3(0f, 0.5f, 0f));
    }

    public override void InitializeSubState()
    {
    }

    public override void UpdateState()
    {
        bs_Ctx.sm_targetSpeed = bs_Ctx.sm_moveSpeed / bs_Ctx.sm_crouchDeduction;
        CheckSwitchStates();

        Debug.Log("TEST: CROUCH STATE");
    }

    public override void CheckSwitchStates()
    {
        if(!bs_Ctx.sm_PlayerInputHandler.CrouchPressed)
        {
            if (!AboveCollisionDetected())
            {
                if (!bs_Ctx.sm_isMovementPressed)
                {
                    SwitchState(bs_Factory.Idle());
                }
                else if (bs_Ctx.sm_isMovementPressed
                    && !bs_Ctx.sm_PlayerInputHandler.SprintPressed)
                {
                    SwitchState(bs_Factory.Run());
                }
                else if (bs_Ctx.sm_isMovementPressed)
                {
                    SwitchState(bs_Factory.Walk());
                }
            }
            else { return; }
        }
    }

    public override void ExitState()
    {
        bs_Ctx.sm_isCrouching = false;

        //bs_Ctx.sm_mainCamera.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        //bs_Ctx.sm_characterController.height = 2f;
        //bs_Ctx.sm_characterController.center = new Vector3(0f, 1f, 0f);

        AdjustCameraAndCollider(
            new Vector3(0f, 1.6f, 0f),
            2f,
            new Vector3(0f, 1f, 0f));
    }

    bool AboveCollisionDetected()
    {
        // CHECK FOR OVERHEAD OBJECTS WITH RAYCAST BEFORE STANDING BACK UP
        Vector3 rayOrigin = bs_Ctx.sm_characterController.transform.position + bs_Ctx.sm_characterController.center + Vector3.up * (bs_Ctx.sm_characterController.height / 2f);
        Vector3 rayDirection = Vector3.up; // Or any other desired direction
        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hitInfo, 2f))
        {
            Debug.Log("BONK");
            return true;
        }
        else
        {
            return false;
        }
    }

    void AdjustCameraAndCollider(Vector3 _camPos, float _colHeight, Vector3 _colCenter)
    {
        bs_Ctx.sm_mainCamera.transform.localPosition = _camPos;
        bs_Ctx.sm_characterController.height = _colHeight;
        bs_Ctx.sm_characterController.center = _colCenter;
    }
}

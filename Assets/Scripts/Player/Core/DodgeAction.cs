using UnityEngine;

public class DodgeAction : PlayerAction
{
    private readonly float iFrameDuration;
    private readonly float recoveryDuration;

    private float timer;
    private bool invulnerabilityActive;

    public override PlayerActionType ActionType =>
        PlayerActionType.Dodge;

    public DodgeAction(
        PlayerActionContext context,
        float iFrameDuration,
        float recoveryDuration)
        : base(context)
    {
        this.iFrameDuration = Mathf.Max(0f, iFrameDuration);
        this.recoveryDuration = Mathf.Max(0f, recoveryDuration);
    }

    public override void OnStarted()
    {
        timer = 0f;
        IsComplete = false;

        invulnerabilityActive = iFrameDuration > 0f;
        Context.DamageController.SetInvulnerable(
            invulnerabilityActive);
    }

    public override void OnUpdate()
    {
        timer += Time.deltaTime;

        if(invulnerabilityActive && timer >= iFrameDuration)
        {
            invulnerabilityActive = false;
            Context.DamageController.SetInvulnerable(false);
        }

        if (timer >= iFrameDuration + recoveryDuration)
            IsComplete = true;
    }

    public override void OnEnded()
    {
        invulnerabilityActive = false;
        Context.DamageController.SetInvulnerable(false);
    }
}

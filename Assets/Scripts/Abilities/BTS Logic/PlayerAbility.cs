public abstract class PlayerAbility
{
    protected PlayerAbilityContext Context { get; private set; }
    protected PlayerAbilityData Data { get; private set; }

    public PlayerAbilityType AbilityType =>
        Data.AbilityType;


    protected PlayerAbility(
        PlayerAbilityContext context,
        PlayerAbilityData data)
    {
        Context = context;
        Data = data;
    }


    // ============================================================
    // LIFECYCLE
    // ============================================================

    public virtual void OnAdded()
    {
    }

    public virtual void OnRemoved()
    {
    }

    public virtual void OnUpdate()
    {
    }

    public virtual void OnGrounded()
    {

    }

    // ============================================================
    // ACTIVATION
    // ============================================================

    public virtual bool CanActivate()
    {
        return true;
    }

    public virtual bool TryActivate()
    {
        return false;
    }
}


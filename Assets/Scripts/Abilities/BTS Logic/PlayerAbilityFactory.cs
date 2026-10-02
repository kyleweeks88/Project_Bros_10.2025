using UnityEngine;

public static class PlayerAbilityFactory
{
    public static PlayerAbility Create(
        PlayerAbilityContext context,
        PlayerAbilityData data)
    {
        if (data == null)
        {
            Debug.LogWarning(
                "Cannot create ability: AbilityData is null."
            );

            return null;
        }

        switch (data.AbilityType)
        {
            case PlayerAbilityType.QuickStep:
                {
                    QuickStepAbilityData quickStepData =
                        data as QuickStepAbilityData;

                    if (quickStepData == null)
                    {
                        Debug.LogError(
                            "QuickStep ability data is not a QuickStepAbilityData."
                        );

                        return null;
                    }

                    return new QuickStepAbility(
                        context,
                        quickStepData
                    );
                }

            case PlayerAbilityType.ExtraJump:
                {
                    ExtraJumpAbilityData extraJumpData =
                        data as ExtraJumpAbilityData;

                    if (extraJumpData == null)
                    {
                        Debug.LogError(
                            "Extra Jump ability data is not an ExtraJumpAbilityData."
                        );

                        return null;
                    }

                    return new ExtraJumpAbility(
                        context,
                        extraJumpData
                    );
                }

            case PlayerAbilityType.AirDash:
                {
                    AirDashAbilityData airDashData =
                        data as AirDashAbilityData;

                    if (airDashData == null)
                    {
                        Debug.LogError(
                            "Air Dash ability data is not an AirDashAbilityData."
                        );

                        return null;
                    }

                    return new AirDashAbility(
                        context,
                        airDashData
                    );
                }

            case PlayerAbilityType.Stomp:
                {
                    StompAbilityData stompData =
                        data as StompAbilityData;

                    if (stompData == null)
                    {
                        Debug.LogError(
                            "Stomp ability data is not a StompAbilityData."
                        );

                        return null;
                    }

                    return new StompAbility(
                        context,
                        stompData
                    );
                }

            default:
                Debug.LogWarning(
                    $"No runtime ability implementation exists for " +
                    $"{data.AbilityType}."
                );

                return null;
        }
    }
}


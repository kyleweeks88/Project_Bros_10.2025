using System.Collections.Generic;
using UnityEngine;

public enum FactionHostilityMode
{
    ListedFactions,
    AllOtherFactions,
    AllFactions
}

public class FactionMember : MonoBehaviour
{
    [SerializeField]
    private FactionType factionType =
        FactionType.NoFaction;

    [SerializeField]
    private FactionHostilityMode hostilityMode =
    FactionHostilityMode.ListedFactions;

    [SerializeField]
    private List<FactionType> hostileFactions =
        new List<FactionType>();

    public FactionType FactionType => factionType;

    public bool IsHostileTo(FactionMember other)
    {
        if (other == null || other == this)
            return false;

        switch (hostilityMode)
        {
            case FactionHostilityMode.AllFactions:
                return true;

            case FactionHostilityMode.AllOtherFactions:
                return other.FactionType != factionType;

            default:
                return hostileFactions.Contains(other.FactionType);
        }
    }
}
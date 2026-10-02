using UnityEngine;

public abstract class PlayerAbilityData : ScriptableObject
{
    [Header("Ability")]
    [SerializeField] private string abilityName;

    [SerializeField] private PlayerAbilityType abilityType;

    public string AbilityName => abilityName;
    public PlayerAbilityType AbilityType => abilityType;
}

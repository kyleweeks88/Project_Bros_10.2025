using System.Collections.Generic;
using UnityEngine;

public class PlayerAbilityController
{
    private readonly PlayerAbilityContext context;

    // ============================================================
    // EQUIPPED ABILITY SLOTS
    // ============================================================

    private readonly PlayerAbility[] equippedAbilities =
        new PlayerAbility[4];

    public PlayerAbility GetAbilityInSlot(AbilitySlot slot)
    {
        int index = (int)slot;

        if (index < 0 || index >= equippedAbilities.Length)
            return null;

        return equippedAbilities[index];
    }

    public IReadOnlyList<PlayerAbility> EquippedAbilities =>
        equippedAbilities;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public PlayerAbilityController(
        PlayerAbilityContext context)
    {
        this.context = context;
    }


    // ============================================================
    // ACQUIRE ABILITY
    // ============================================================

    // THIS WILL BE IMPLEMENTED EVENTUALLY
    public bool EquipAbility(PlayerAbilityData data, AbilitySlot slot)
    {
        return false;
    }

    public bool AddAbility(PlayerAbilityData data)
    {
        if (data == null)
            return false;

        // Player cannot equip duplicate ability types.
        if (HasAbilityType(data.AbilityType))
            return false;

        // Find an empty slot.
        int emptySlot = FindEmptySlot();

        // All four slots are occupied.
        if (emptySlot == -1)
            return false;

        PlayerAbility ability =
            PlayerAbilityFactory.Create(context, data);

        if (ability == null)
            return false;

        equippedAbilities[emptySlot] = ability;

        ability.OnAdded();

        return true;
    }


    // ============================================================
    // REMOVE ABILITY
    // ============================================================

    public void RemoveAbility<T>() where T : PlayerAbility
    {
        for (int i = 0; i < equippedAbilities.Length; i++)
        {
            if (equippedAbilities[i] is T ability)
            {
                ability.OnRemoved();

                equippedAbilities[i] = null;

                return;
            }
        }
    }


    // ============================================================
    // SLOT ACTIVATION
    // ============================================================

    public bool TryActivateSlot(AbilitySlot slot)
    {
        int index = (int)slot;

        if (index < 0 || index >= equippedAbilities.Length)
            return false;

        PlayerAbility ability = equippedAbilities[index];

        if (ability == null)
            return false;

        if (!ability.CanActivate())
            return false;

        return ability.TryActivate();
    }


    // ============================================================
    // ABILITY LOOKUP
    // ============================================================

    // Find an ability by its runtime class.
    public T GetAbility<T>() where T : PlayerAbility
    {
        for (int i = 0; i < equippedAbilities.Length; i++)
        {
            if (equippedAbilities[i] is T ability)
                return ability;
        }

        return null;
    }


    // Find an ability by its PlayerAbilityType.
    public PlayerAbility GetAbility(PlayerAbilityType abilityType)
    {
        for (int i = 0; i < equippedAbilities.Length; i++)
        {
            PlayerAbility ability = equippedAbilities[i];

            if (ability != null &&
                ability.AbilityType == abilityType)
            {
                return ability;
            }
        }

        return null;
    }


    // ============================================================
    // ABILITY TYPE CHECK
    // ============================================================

    private bool HasAbilityType(PlayerAbilityType abilityType)
    {
        return GetAbility(abilityType) != null;
    }


    // ============================================================
    // FIND EMPTY SLOT
    // ============================================================

    private int FindEmptySlot()
    {
        for (int i = 0; i < equippedAbilities.Length; i++)
        {
            if (equippedAbilities[i] == null)
                return i;
        }

        return -1;
    }


    // ============================================================
    // UPDATE
    // ============================================================

    public void UpdateAbilities()
    {
        for (int i = 0; i < equippedAbilities.Length; i++)
        {
            if (equippedAbilities[i] != null)
            {
                equippedAbilities[i].OnUpdate();
            }
        }
    }


    // ============================================================
    // GROUNDED NOTIFICATION
    // ============================================================

    public void NotifyGrounded()
    {
        for (int i = 0; i < equippedAbilities.Length; i++)
        {
            if (equippedAbilities[i] != null)
            {
                equippedAbilities[i].OnGrounded();
                Debug.Log("NotifyGrounded");
            }
        }
    }
}




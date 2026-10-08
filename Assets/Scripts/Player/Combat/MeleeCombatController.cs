using System.Collections.Generic;
using UnityEngine;
using System;

public class MeleeCombatController
{
    private readonly GameObject meleeHitbox;
    private readonly MeleeHitbox hitbox;
    private readonly Transform attackerTransform;

    #region AIRBORNE ATTACK REF

    private MeleeAttackData currentAirborneAttack;

    #endregion

    #region COMBO CHAIN REF
    private MeleeStyleData meleeStyle;

    private IReadOnlyList<MeleeAttackData> currentChain;
    private int currentChainIndex;

    public int CurrentChainIndex => currentChainIndex;

    public int CurrentChainLength =>
        currentChain?.Count ?? 0;
    #endregion

    public MeleeAttackData CurrentAttackData
    {
        get
        {
            if (currentAirborneAttack != null)
                return currentAirborneAttack;

            if (currentChain == null)
                return null;

            if (currentChainIndex < 0 ||
                currentChainIndex >= currentChain.Count)
            {
                return null;
            }

            return currentChain[currentChainIndex];
        }
    }

    public MeleeCombatController(
        GameObject meleeHitbox,
        Transform attackTransform)
    {
        this.meleeHitbox = meleeHitbox;
        if(meleeHitbox != null )
            hitbox = meleeHitbox.GetComponent<MeleeHitbox>();

        SetHitboxActive(false);

        this.attackerTransform = attackTransform;
    }

    #region AIRBORNE ATTACK LOGIC

    public bool StartAirborneAttack(MeleeAttackType attackType)
    {
        if(meleeStyle == null)
        {
            Debug.LogWarning(
                "No MeleeStyleData assigned!"
                );

            return false;
        }

        MeleeAttackData attack =
            meleeStyle.GetAirborneAttack(attackType);

        if(attack == null)
        {
            Debug.LogWarning(
                $"No airborne attack assigned to {attackType}" +
                $" in style {meleeStyle.StyleName}."
                );

            return false;
        }

        currentAirborneAttack = attack;

        Debug.Log(
            $"Started airborne {attackType}: " +
            $"{currentAirborneAttack.name}"
            );

        return true;
    }

    #endregion

    #region COMBO CHAIN LOGIC
    public void SetMeleeStyle(MeleeStyleData style)
    {
        meleeStyle = style;

        ResetChain();
    }

    public bool StartChain(MeleeAttackType attackType)
    {
        if(meleeStyle == null)
        {
            Debug.LogWarning(
                "No MeleeStyleData assigned!"
                );

            return false;
        }

        IReadOnlyList<MeleeAttackData> chain =
            meleeStyle.GetChain(attackType); 

        if(chain == null || chain.Count == 0)
        {
            Debug.LogWarning(
                $"No attacks assigned to {attackType}" +
                $"in style {meleeStyle.StyleName}."
                );

            return false;
        }

        currentChain = chain;
        currentChainIndex = 0;

        Debug.Log(
            $"Started {attackType} chain |" +
            $"Attack 1/{currentChain.Count}:" +
            $"{CurrentAttackData.name}"
            );

        return true;
    }

    public bool AdvanceChain()
    {
        if (currentChain == null)
            return false;

        if (currentChainIndex + 1 >= currentChain.Count)
            return false;

        currentChainIndex++;

        Debug.Log(
            $"Advanced combo |" +
            $"Attack {currentChainIndex + 1}/" +
            $"{currentChain.Count}:" +
            $"{CurrentAttackData.name}"
            );

        return true;
    }

    public void ResetChain()
    {
        currentChain = null;
        currentChainIndex = 0;
        currentAirborneAttack = null;
    }
    #endregion

    #region HITBOX INFORMATION
    public void SetHitboxActive(bool boolValue)
    {
        if (meleeHitbox == null)
            return;

        meleeHitbox.SetActive(boolValue);
    }

    public void SetHitboxDamage(
        DamageInfo damageInfo,
        float horizontalKnockbackForce,
        float verticalKnockbackForce,
        VerticalAttackDirection targetVerticalKnockbackDirection,
        Action onAttackConnected)
    {
        hitbox.SetAttackDamage(
            damageInfo,
            attackerTransform,
            horizontalKnockbackForce,
            verticalKnockbackForce,
            targetVerticalKnockbackDirection,
            onAttackConnected
        );
    }
    #endregion
}
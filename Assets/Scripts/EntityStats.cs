using System;
using UnityEngine;

public class EntityStats : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Combat")]
    [SerializeField] private float damageOutput = 10f;

    [Header("Various")]
    [SerializeField] private float knockbackResistance = 0f;


    public float KnockbackResistance => knockbackResistance;
    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public float DamageOutput => damageOutput;

    public bool IsDead { get; private set; }

    public event Action<float> DamageTaken;
    public event Action Died;

    private void Awake()
    {
        CurrentHealth = MaxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead)
            return;

        if (damage <= 0f)
            return;

        CurrentHealth -= damage;

        CurrentHealth = Mathf.Max(
            CurrentHealth,
            0f
        );

        DamageTaken?.Invoke(damage);

        Debug.Log($"this {this.name} took: {damage}" +
            $" damage."
            );

        if (CurrentHealth <= 0f)
        {
            Debug.Log($"{this.name} has died!");

            Die();
        }
    }

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        Died?.Invoke();
    }
}
using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;

    public float CurrentHealth { get; private set; }
    public bool  IsDead        { get; private set; }

    public event Action<float, float> OnHealthChanged; // (current, max)
    public event Action               OnDeath;

    public static event Action OnPlayerDied;
    public static event Action OnPlayerDamaged;

    void Awake()
    {
        CurrentHealth = maxHealth;
    }

    void Start()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        OnPlayerDamaged?.Invoke();
        Debug.Log($"Player took {amount} dmg, hp={CurrentHealth}", this);
        if (CurrentHealth <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    void Die()
    {
        IsDead = true;
        Debug.Log("death");
        OnDeath?.Invoke();
        OnPlayerDied?.Invoke();
    }
}

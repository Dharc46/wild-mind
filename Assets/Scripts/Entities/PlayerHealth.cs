using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Player maximum health points.")]
    public float maxHealth = 100f;

    [Tooltip("If true, the player GameObject will be disabled when HP reaches zero.")]
    public bool disableOnDeath = true;

    public float CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0f;

    public event Action<float> OnDamaged;
    public event Action<float> OnHealed;
    public event Action OnDied;

    private void Awake()
    {
        CurrentHealth = Mathf.Max(1f, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f)
            return;

        float applied = Mathf.Min(amount, CurrentHealth);
        CurrentHealth -= applied;
        OnDamaged?.Invoke(applied);

        if (CurrentHealth <= 0f)
        {
            CurrentHealth = 0f;
            HandleDeath();
        }
    }

    public void Heal(float amount)
    {
        if (!IsAlive || amount <= 0f)
            return;

        float previous = CurrentHealth;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        float healed = CurrentHealth - previous;
        if (healed > 0f)
        {
            OnHealed?.Invoke(healed);
        }
    }

    public void RestoreToFull()
    {
        if (!IsAlive)
        {
            CurrentHealth = Mathf.Max(1f, maxHealth);
        }
        else
        {
            CurrentHealth = maxHealth;
        }
    }

    private void HandleDeath()
    {
        OnDied?.Invoke();
        if (disableOnDeath)
        {
            gameObject.SetActive(false);
        }
    }
}

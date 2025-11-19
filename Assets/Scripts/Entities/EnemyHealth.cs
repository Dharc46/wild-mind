using System;
using UnityEngine;

/// <summary>
/// Simple health component for enemies. Handles current HP, damage, healing and death events.
/// Designed to be data-driven (maxHealth can be set from EnemyData at spawn time).
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [Tooltip("Maximum health for this enemy. Can be initialized from EnemyData on spawn.")]
    public float maxHealth = 30f;

    [SerializeField]
    private float currentHealth = -1f;

    public float CurrentHealth
    {
        get => currentHealth;
        private set => currentHealth = Mathf.Clamp(value, 0f, maxHealth);
    }

    public bool IsAlive => CurrentHealth > 0f;

    // Events: damaged amount, healed amount, death
    public event Action<EnemyHealth, float> OnDamaged;
    public event Action<EnemyHealth, float> OnHealed;
    public event Action<EnemyHealth> OnDeath;

    private void Awake()
    {
        if (currentHealth < 0f)
            CurrentHealth = maxHealth;
    }

    /// <summary>
    /// Initialize health to provided max and set current to max.
    /// Call this when spawning the enemy using EnemyData values.
    /// </summary>
    public void Initialize(float max)
    {
        maxHealth = max;
        CurrentHealth = maxHealth;
    }

    /// <summary>
    /// Apply damage; invokes OnDamaged and OnDeath (if HP falls to 0).
    /// Returns the actual damage applied (clamped by current HP).
    /// </summary>
    public float TakeDamage(float amount)
    {
        if (amount <= 0f || !IsAlive)
            return 0f;

        float applied = Mathf.Min(amount, CurrentHealth);
        CurrentHealth -= applied;

        try { OnDamaged?.Invoke(this, applied); } catch (Exception) { }

        if (!IsAlive)
        {
            try { OnDeath?.Invoke(this); } catch (Exception) { }
        }

        return applied;
    }

    /// <summary>
    /// Heal the entity by amount (no effect if dead). Returns actual healed amount.
    /// </summary>
    public float Heal(float amount)
    {
        if (amount <= 0f || !IsAlive)
            return 0f;

        float space = maxHealth - CurrentHealth;
        float healed = Mathf.Min(amount, space);
        CurrentHealth += healed;

        try { OnHealed?.Invoke(this, healed); } catch (Exception) { }

        return healed;
    }

    /// <summary>
    /// Force kill the entity (set HP to 0 and invoke OnDeath).
    /// </summary>
    public void Kill()
    {
        if (!IsAlive)
            return;

        CurrentHealth = 0f;
        try { OnDeath?.Invoke(this); } catch (Exception) { }
    }

    /// <summary>
    /// Restore to full health.
    /// </summary>
    public void RestoreToFull()
    {
        CurrentHealth = maxHealth;
    }

    [ContextMenu("Test TakeDamage (5)")]
    private void ContextTestTakeDamage()
    {
        TakeDamage(5f);
        Debug.Log($"ContextTestTakeDamage called on {name}. CurrentHP = {CurrentHealth}");
    }

    [ContextMenu("Test Heal (5)")]
    private void ContextTestHeal()
    {
        Heal(5f);
        Debug.Log($"ContextTestHeal called on {name}. CurrentHP = {CurrentHealth}");
    }
}

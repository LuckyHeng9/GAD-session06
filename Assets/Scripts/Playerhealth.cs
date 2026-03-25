using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Invincibility After Hit")]
    public float invincibleDuration = 0.5f;   // brief invincibility so one hit doesn't spam

    [Header("Events")]
    public UnityEvent<float> onHealthChanged;  // passes current health (hook up UI)
    public UnityEvent onDeath;

    private bool isDead = false;
    private float invincibleUntil = -1f;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        if (Time.time < invincibleUntil) return;  // still invincible

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        invincibleUntil = Time.time + invincibleDuration;

        onHealthChanged?.Invoke(currentHealth);

        Debug.Log($"Player took {amount} damage. HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        onHealthChanged?.Invoke(currentHealth);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("Player died!");
        onDeath?.Invoke();
        // Hook up onDeath in Inspector to trigger Game Over screen, etc.
    }

    public bool IsDead => isDead;
}
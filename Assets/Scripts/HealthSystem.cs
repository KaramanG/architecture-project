using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Events;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private float maxHealth;

    [SerializeField] private float _health;
    private float health // Using a private backing field with a property for control
    {
        get {
            return _health;
        }
        set {
            float clampedValue = Mathf.Clamp(value, 0f, maxHealth);

            // Only trigger changes if the value is actually different
            if (Mathf.Approximately(_health, clampedValue)) return;

            _health = clampedValue;
            OnHealthChanged?.Invoke(_health, maxHealth); // Notify subscribers about health change

            // Check for death AFTER health is updated
            if (_health <= 0 && !isDead)
            {
                InstanceDie();
            }
        }
    }

    // Events to notify other systems
    public UnityEvent OnPlayerDied;
    public UnityEvent<float, float> OnHealthChanged; // Passes current and max health

    [SerializeField] private HealthBar healthBar; // Assumes you have a HealthBar component/script
    [SerializeField] private bool isPlayer; // Flag to differentiate player/mob

    private bool isDead; // Flag to prevent multiple death calls
    private Animator animator; // Reference to the Animator component

    private void Awake()
    {
        // Only set health to max if not loading a saved game AND this is the player
        // Assuming SaveSystem.IsLoading() and SaveSystem exists.
        if (!(SaveSystem.IsLoading() && isPlayer))
        {
            health = maxHealth; // This will use the 'set' property, triggering OnHealthChanged
        } else {
             // If loading, the SetHealth method will likely be called by the loading system
             // to restore the saved health.
        }
        
        isDead = false; // Start alive

        // Initial UI update
        // OnHealthChanged?.Invoke(_health, maxHealth); // Redundant, the 'set' property already called it in the block above
        if (healthBar != null) {
             healthBar.UpdateHealthBar(); // Make sure HealthBar has a public UpdateHealthBar method
        }


        animator = GetComponent<Animator>();
        // No need to check for null and return here, the script can function without an animator for health/death state
    }

    // Public methods to access health state
    public float GetHealth() { return health; }
    public float GetMaxHealth() { return maxHealth; }
    public bool IsDead() { return isDead; }

    // Method to apply damage
    public void TakeDamage(float damage)
    {
        if (isDead) return; // Cannot take damage if already dead

        health -= damage; // Use the property 'set' which handles clamping and events

        // UI update is handled by the property 'set' via OnHealthChanged event.
        // If HealthBar component subscribes to OnHealthChanged, this line is redundant.
        // If HealthBar doesn't subscribe, this line is necessary. Let's keep it for now.
        if (healthBar != null) {
            healthBar.UpdateHealthBar(); // Make sure HealthBar has a public UpdateHealthBar method
        }
    }

    // Internal method for death logic
    private void InstanceDie()
    {
        if (isDead) return; // Prevent triggering death multiple times

        isDead = true; // Set death flag

        // Trigger death animation if animator exists
        if (animator != null) {
             animator.SetTrigger("Death"); // Make sure you have a "Death" trigger in your Animator Controller
        }
        

        OnPlayerDied?.Invoke(); // Notify subscribers that the entity has died
    }

    // Method to set health directly (e.g., for healing or loading)
    public void SetHealth(float newHealth)
    {
        health = newHealth; // Use the property 'set' which handles clamping and events

        // UI update is handled by the property 'set' via OnHealthChanged event.
        // If HealthBar component subscribes to OnHealthChanged, this line is redundant.
        // If HealthBar doesn't subscribe, this line is necessary. Let's keep it for now.
        if (healthBar != null) {
            healthBar.UpdateHealthBar(); // Make sure HealthBar has a public UpdateHealthBar method
        }
    }

    // Optional: Method to heal
    public void Heal(float amount)
    {
        if (isDead) return;
        health += amount; // Use the property 'set'
    }
}
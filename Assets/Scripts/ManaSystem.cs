using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ManaSystem : MonoBehaviour
{
    [SerializeField] private float maxMana;

    [SerializeField] private float _mana;
    private float mana // Using a private backing field with a property for control
    {
        get
        {
            return _mana;
        }
        set
        {
            float clampedValue = Mathf.Clamp(value, 0f, maxMana);

            // Only trigger changes if the value is actually different
            if (Mathf.Approximately(_mana, clampedValue)) return;

            _mana = clampedValue;
            OnManaChanged?.Invoke(_mana, maxMana); // Notify subscribers about mana change
        }
    }

    public bool canRegenMana = true; // Default to true
    [SerializeField] private float manaRegenRate; // Mana regenerated per second

    // Event to notify other systems about mana change
    public UnityEvent<float, float> OnManaChanged; // Passes current and max mana


    private void Awake()
    {
        // Only set mana to max if not loading a saved game
        // Assuming SaveSystem.IsLoading() and SaveSystem exists.
        if (!SaveSystem.IsLoading())
        {
            mana = maxMana; // This will use the 'set' property, triggering OnManaChanged
        } else {
             // If loading, the SetMana method will likely be called by the loading system
             // to restore the saved mana.
        }

        // OnManaChanged?.Invoke(_mana, maxMana); // Redundant, the 'set' property already called it in the block above
        // canRegenMana is already defaulted to true in the field declaration
    }

    private void FixedUpdate() // FixedUpdate is good for consistent regeneration independent of frame rate
    {
        if (canRegenMana && mana < maxMana) // Only regen if allowed and not already at max mana
        {
            mana += manaRegenRate * Time.fixedDeltaTime; // Regen amount is rate * time passed
        }
    }

    // Public methods to access mana state
    public float GetMana() { return mana; }
    public float GetMaxMana() { return maxMana; }

    // Method to reduce mana (e.g., for casting spells)
    public void ReduceMana(float amount)
    {
        if (mana < amount) return; // Cannot reduce if not enough mana

        mana -= amount; // Use the property 'set' which handles clamping and events
    }

    // Method to set mana directly (e.g., for loading or giving mana)
    public void SetMana(float newMana)
    {
        mana = newMana; // Use the property 'set' which handles clamping and events
    }

    // Optional: Method to add mana (e.g., potions)
    public void AddMana(float amount)
    {
        mana += amount; // Use the property 'set'
    }
}
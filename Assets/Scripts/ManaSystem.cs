using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ManaSystem : MonoBehaviour
{
    [SerializeField] private float maxMana;

    [SerializeField] private float _mana;
    private float mana
    {
        get
        {
            return _mana;
        }
        set
        {
            float clampedValue = Mathf.Clamp(value, 0f, maxMana);

            if (Mathf.Approximately(_mana, clampedValue)) return;

            _mana = clampedValue;
            OnManaChanged?.Invoke(_mana, maxMana);
        }
    }

    public bool canRegenMana;
    [SerializeField] private float manaRegenRate;
    

    public UnityEvent<float, float> OnManaChanged;


    private void Awake()
    {
        mana = maxMana;
        OnManaChanged?.Invoke(_mana, maxMana);
        canRegenMana = true;
    }

    private void FixedUpdate()
    {
        if (canRegenMana)
        {
            mana += manaRegenRate * Time.fixedDeltaTime;
        }
    }

    public float GetMana() { return mana; }
    public float GetMaxMana() { return maxMana; }

    public void ReduceMana(float amount)
    {
        if (mana < amount) return;

        mana -= amount;
    }
}

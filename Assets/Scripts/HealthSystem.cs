using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private float maxHealth;

    [SerializeField] private float _health;
    private float health
    {
        get {
            return _health;
        }
        set {
            if (value <= 0) {
                _health = 0;
                InstanceDie();
                return;
            }
            _health = value;
        }
    }
    
    [SerializeField] private HealthBar healthBar;

    [Header("Animator")]
    [SerializeField] private string deathTriggerName = "Death";

    private bool isDead;
    private Animator animator;

    private void Awake()
    {
        health = maxHealth;
        isDead = false;

        healthBar.UpdateHealthBar();

        animator = GetComponent<Animator>();
        if (animator == null) {
            Debug.Log(gameObject.name + " „q„u„x „p„~„y„}„p„„„€„‚„p");
            return;
        }
    }

    public float GetHealth() { return health; }
    public float GetMaxHealth() { return maxHealth; }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        health -= damage;
        healthBar.UpdateHealthBar();
    }

    private void InstanceDie()
    {
        isDead = true;
        animator.SetTrigger(deathTriggerName);
    }
}

// HitboxScript.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitboxScript : MonoBehaviour
{
    [SerializeField] private PhysicalAttackSystem sourceAttack; // Убедитесь, что это поле назначено в инспекторе
    [SerializeField] private List<LayerMask> targetLayers;

    private Collider hitboxCollider;
    private List<Collider> hitList;


    private void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.isTrigger = true;
        hitboxCollider.enabled = false;

        hitList = new List<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hitboxCollider.enabled) { return; }

        bool layerIsTarget = false;
        foreach (LayerMask mask in targetLayers)
        {
            // Проверяем, принадлежит ли слой объекта other к одному из целевых слоев
            if ((mask.value & (1 << other.gameObject.layer)) != 0)
            {
                layerIsTarget = true;
                break;
            }
        }
        if (!layerIsTarget) { return; }

        if (hitList.Contains(other)) { return; } // Уже ударили эту цель в текущей атаке

        HealthSystem targetHealth = other.GetComponent<HealthSystem>();
        if (targetHealth == null) return; // У цели нет компонента здоровья

        float damageAmount = 0f;
        if (sourceAttack != null) // Получаем урон от источника атаки
        {
            damageAmount = sourceAttack.GetDamage(); // Предполагаем, что GetDamage() существует в PhysicalAttackSystem
        }
        else
        {
            Debug.LogWarning("SourceAttack не назначен в HitboxScript на " + gameObject.name);
            return; // Не можем нанести урон без источника
        }

        targetHealth.TakeDamage(damageAmount);

        // Попытка уведомить BossAI, если это босс
        BossAI bossAI = other.GetComponent<BossAI>();
        if (bossAI != null)
        {
            bossAI.NotifyDamageTaken(damageAmount);
        }

        // Логика оглушения для мобов
        // Убедитесь, что слой "Mobs" правильно назван и используется
        if (other.gameObject.layer == LayerMask.NameToLayer("Mobs")) // Используйте имя слоя, как оно задано в Unity
        {
            MobAI mobAI = other.GetComponent<MobAI>();
            if (mobAI != null)
            {
                mobAI.TakeStun();
            }
        }

        hitList.Add(other);
    }

    public void EnableHitbox()
    {
        hitList.Clear();
        hitboxCollider.enabled = true;
    }

    public void DisableHitbox()
    {
        hitboxCollider.enabled = false;
    }
}
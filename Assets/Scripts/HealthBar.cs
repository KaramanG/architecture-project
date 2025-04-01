using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image healthBarFilling; // Ссылка на Image компонент HealthBarFilling
    public float maxHealth = 100f; // Максимальное количество здоровья
    private float currentHealth; // Текущее количество здоровья

    public Animator animator; // Ссылка на компонент Animator, назначается в Инспекторе

    private bool isDead = false; // Флаг, чтобы смерть срабатывала только один раз

    void Start()
    {
        currentHealth = maxHealth; // Устанавливаем текущее здоровье на максимум при старте
        UpdateHealthBar(); // Обновляем полоску здоровья в начале игры
    }

    // Функция для получения урона
    public void TakeDamage(float damage)
    {
        if (isDead) return; // Если персонаж уже мертв, урон больше не принимается

        currentHealth -= damage; // Уменьшаем текущее здоровье на величину урона
        if (currentHealth < 0)
        {
            currentHealth = 0; // Предотвращаем уход здоровья в отрицательные значения
        }
        UpdateHealthBar(); // Обновляем полоску здоровья после получения урона

        if (currentHealth <= 0)
        {
            Die(); // Вызываем метод смерти, когда здоровье падает до нуля или ниже
        }
    }

    void Die()
    {
        isDead = true; // Устанавливаем флаг смерти, чтобы предотвратить повторные срабатывания

        Debug.Log("Персонаж умер!"); // Сообщение в консоль для отладки

        if (animator != null) // Проверяем, что Animator назначен в Инспекторе
        {
            animator.SetTrigger("Death"); // Активируем триггер "Death" в Animator
        }
        else
        {
            Debug.LogError("Animator не назначен в инспекторе для скрипта HealthBar на объекте: " + gameObject.name);
        }

        // Дополнительные действия после смерти персонажа можно добавить здесь, например:
        // - Отключение управления персонажем
        // - Запуск таймера для уничтожения персонажа
        // - Вызов события смерти для других скриптов
    }

    // Функция для обновления визуального отображения полоски здоровья
    private void UpdateHealthBar()
    {
        if (healthBarFilling != null)
        {
            healthBarFilling.fillAmount = currentHealth / maxHealth; // Заполняем полоску здоровья в зависимости от текущего здоровья
        }
        else
        {
            Debug.LogError("HealthBarFilling Image не назначена! Пожалуйста, перетащите компонент Image HealthBarFilling в поле Health Bar Filling в инспекторе скрипта.");
        }
    }

    // (Опционально) Функция для увеличения здоровья (например, при лечении)
    public void Heal(float amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth; // Предотвращаем превышение максимального здоровья
        }
        UpdateHealthBar(); // Обновляем полоску здоровья после лечения
    }

    // (Опционально) Функция для установки текущего здоровья напрямую (для отладки или особых случаев)
    public void SetHealth(float healthValue)
    {
        currentHealth = healthValue;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        else if (currentHealth < 0)
        {
            currentHealth = 0;
        }
        UpdateHealthBar();
    }

    // (Опционально) Функция для получения текущего здоровья
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    // (Опционально) Функция для получения максимального здоровья
    public float GetMaxHealth()
    {
        return maxHealth;
    }
}
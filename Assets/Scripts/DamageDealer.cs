using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    public float damageAmount = 10f; // Количество урона для нанесения
    public KeyCode damageKey = KeyCode.Space; // Клавиша для нанесения урона (например, пробел)

    void Update()
    {
        if (Input.GetKeyDown(damageKey))
        {
            // Находим компонент HealthBar на этом же GameObject или на родительском (если скрипт на другом объекте)
            HealthBar healthBar = GetComponent<HealthBar>(); // Если DamageDealer на том же объекте, что и HealthBar
            // HealthBar healthBar = GetComponentInParent<HealthBar>(); // Если DamageDealer на дочернем объекте, а HealthBar на родительском
            // HealthBar healthBar = GameObject.FindGameObjectWithTag("Player").GetComponent<HealthBar>(); // Если HealthBar на GameObject с тегом "Player"

            if (healthBar != null)
            {
                healthBar.TakeDamage(damageAmount); // Наносим урон
            }
            else
            {
                Debug.LogError("Компонент HealthBar не найден на объекте!");
            }
        }
    }
}
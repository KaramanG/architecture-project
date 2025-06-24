using UnityEngine; // Нужно для Vector3
using System;      // Нужно для [Serializable]
// using System.Collections; // Эти не нужны для простого класса данных
// using System.Collections.Generic; // Эти не нужны

// Класс, представляющий данные игрока для сохранения
// Должен быть сериализуемым для BinaryFormatter или JsonUtility
[Serializable] // <-- Оставляем этот атрибут
public class PlayerData // <--- Убедись, что здесь НЕТ ": MonoBehaviour"
{
    public float health;
    public float mana;
    // Используем твои имена полей
    public float positionX;
    public float positionY;
    public float positionZ;

    // Конструктор для создания объекта данных из текущего состояния игрока
    public PlayerData(float currentHealth, float currentMana, Vector3 position)
    {
        this.health = currentHealth;
        this.mana = currentMana;
        this.positionX = position.x; // Используем твои имена полей
        this.positionY = position.y;
        this.positionZ = position.z;
    }

    // Метод для получения позиции в виде Vector3
    public Vector3 GetPosition()
    {
        return new Vector3(positionX, positionY, positionZ); // Используем твои имена полей
    }

    // Пустой конструктор нужен для десериализации JsonUtility,
    // и хотя BinaryFormatter может обойтись без него, лучше его оставить на всякий случай.
    public PlayerData() { }
}
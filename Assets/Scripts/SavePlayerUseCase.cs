using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SavePlayerUseCase
{
    private readonly IPlayerRepository _playerRepository; // Зависимость от интерфейса репозитория

    // Получаем репозиторий через конструктор (Dependency Injection)
    public SavePlayerUseCase(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    // Метод выполнения задачи: собирает данные и просит репозиторий их сохранить.
    public void Execute(HealthSystem health, ManaSystem mana, Transform playerTransform)
    {
        // Создаем объект с данными для сохранения, используя PlayerData
        PlayerData dataToSave = new PlayerData(
            health != null ? health.GetHealth() : 0f, // Безопасно получаем здоровье
            mana != null ? mana.GetMana() : 0f,     // Безопасно получаем ману
            playerTransform != null ? playerTransform.position : Vector3.zero // Безопасно получаем позицию
        );

        // Просим репозиторий сохранить эти данные
        _playerRepository.Save(dataToSave);

        // Debug.Log("SavePlayerUseCase executed."); // Отладочный лог
    }
}

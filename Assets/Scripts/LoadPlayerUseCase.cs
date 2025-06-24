using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadPlayerUseCase
{
    private readonly IPlayerRepository _playerRepository; // Зависимость от интерфейса репозитория

    // Получаем репозиторий через конструктор
    public LoadPlayerUseCase(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    // Метод выполнения задачи: просит репозиторий загрузить данные и возвращает их.
    // Этот интерактор НЕ должен применять данные к игроку и НЕ должен перезагружать сцену.
    // Его единственная задача - получить данные из места хранения.
    public PlayerData Execute()
    {
        // Просим репозиторий загрузить данные
        PlayerData loadedData = _playerRepository.Load();

        // Debug.Log("LoadPlayerUseCase executed. Data loaded: " + (loadedData != null)); // Отладочный лог

        return loadedData; // Возвращаем загруженные данные (может быть null)
    }

    // Опционально: Интерактор для проверки наличия сохранения
    public bool CanExecute()
    {
        return _playerRepository.HasSave();
    }
}
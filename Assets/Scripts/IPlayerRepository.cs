using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerRepository
{
    // Сохранить данные игрока
    void Save(PlayerData data);

    // Загрузить данные игрока. Возвращает null, если данных нет или ошибка.
    PlayerData Load();

    // Проверить, существуют ли сохраненные данные
    bool HasSave();

    // Удалить сохраненные данные (опционально, но полезно)
    void DeleteSave();
}
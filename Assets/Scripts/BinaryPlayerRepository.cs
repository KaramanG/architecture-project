using UnityEngine;
using System.IO; // Нужен для File
using System.Runtime.Serialization.Formatters.Binary; // Нужен для BinaryFormatter
using System; // Нужен для Exception

// Класс, который реализует IPlayerRepository, используя BinaryFormatter для сохранения в файл.
// НЕ является Monobehaviour. Содержит только логику работы с данными и файлами.
public class BinaryPlayerRepository : IPlayerRepository
{
    private readonly string _saveFileName = "playerData.sav"; // Имя файла сохранения
    private string _saveFilePath; // Полный путь к файлу сохранения

    // Конструктор: определяется путь к файлу
    public BinaryPlayerRepository()
    {
        _saveFilePath = Path.Combine(Application.persistentDataPath, _saveFileName);
        // Debug.Log($"BinaryPlayerRepository initialized. Save path: {_saveFilePath}"); // Отладочный лог
    }

    // Реализация метода сохранения из интерфейса IPlayerRepository
    public void Save(PlayerData data)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = null;

        try
        {
            stream = new FileStream(_saveFilePath, FileMode.Create);
            formatter.Serialize(stream, data);
            // Debug.Log($"Player data saved successfully to {_saveFilePath}"); // Отладочный лог
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save player data using BinaryFormatter: {e.Message}"); // Логируем ошибку
        }
        finally
        {
            // Важно всегда закрывать поток!
            if (stream != null) { stream.Close(); }
        }
    }

    // Реализация метода загрузки из интерфейса IPlayerRepository
    public PlayerData Load()
    {
        // Проверяем существование файла
        if (!File.Exists(_saveFilePath))
        {
            // Debug.LogWarning($"No Binary save file found at {_saveFilePath}"); // Отладочный лог
            return null; // Если файла нет, возвращаем null
        }

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = null;
        PlayerData loadedData = null;

        try
        {
            stream = new FileStream(_saveFilePath, FileMode.Open);
            loadedData = formatter.Deserialize(stream) as PlayerData;

            if (loadedData != null)
            {
                 // Debug.Log($"Player data loaded successfully from {_saveFilePath}"); // Отладочный лог
            }
            else
            {
                 Debug.LogError($"Failed to deserialize player data from {_saveFilePath}. File might be corrupted."); // Логируем ошибку десериализации
                 // Если десериализация не удалась, возможно, файл поврежден. Можно его удалить.
                 DeleteSave(); // Удаляем некорректный файл сохранения
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load player data using BinaryFormatter: {e.Message}"); // Логируем ошибку загрузки
            // Если загрузка не удалась (например, формат файла изменился или он поврежден), удаляем его.
            DeleteSave();
            loadedData = null; // Убедимся, что возвращаем null при ошибке
        }
        finally
        {
            // Важно всегда закрывать поток!
            if (stream != null) { stream.Close(); }
        }

        return loadedData; // Возвращаем загруженные данные (или null)
    }

    // Реализация метода проверки наличия сохранения
    public bool HasSave()
    {
        return File.Exists(_saveFilePath);
    }

    // Реализация метода удаления сохранения
    public void DeleteSave()
    {
        if (File.Exists(_saveFilePath))
        {
            try
            {
                File.Delete(_saveFilePath);
                // Debug.Log($"Player Binary save file deleted from {_saveFilePath}"); // Отладочный лог
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete player Binary save file: {e.Message}"); // Логируем ошибку
            }
        } else {
             // Debug.LogWarning($"No Binary save file found at {_saveFilePath} to delete."); // Отладочный лог
        }
    }
}
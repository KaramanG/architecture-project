using UnityEngine;

// Этот класс будет управлять состоянием мирного режима
// Он может быть либо Monobehaviour на каком-то объекте-менеджере в сцене,
// либо просто статическим классом.
// Сделаем его статическим для простоты доступа из любого места.
public static class PeaceModeManager // <-- Класс статический
{
    // Публичное статическое булевое поле, которое определяет, активен ли мирный режим
    // К нему обращаются другие скрипты, чтобы узнать текущий режим.
    public static bool IsPeacefulModeActive = true; // <-- По умолчанию режим НЕ мирный
    // Можно добавить свойство с get/set, если нужна дополнительная логика при установке значения
    /*
    public static bool IsPeacefulModeActive
    {
        get { return _isPeacefulModeActive; }
        set
        {
            if (_isPeacefulModeActive != value) // Проверяем, изменилось ли значение
            {
                _isPeacefulModeActive = value;
                Debug.Log($"Peaceful Mode is now: {_isPeacefulModeActive}"); // Отладочное сообщение о смене режима
                // Здесь можно вызвать событие (UnityEvent или C# event),
                // чтобы другие системы могли реагировать на смену режима.
            }
        }
    }
    private static bool _isPeacefulModeActive = false;
    */

    // Можно добавить статический метод для удобства переключения (опционально)
    public static void SetPeacefulMode(bool isActive)
    {
        IsPeacefulModeActive = isActive; // Устанавливаем значение напрямую или через свойство
    }

    public static void TogglePeacefulMode()
    {
        IsPeacefulModeActive = !IsPeacefulModeActive; // Переключаем значение на противоположное
    }
}
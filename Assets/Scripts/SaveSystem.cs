using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.SceneManagement;

public class SaveSystem : MonoBehaviour // Оставляем Monobehaviour, т.к. он висит в сцене
{
    [Header("References")]
    [Tooltip("Assign the player's GameObject here, or it will try to find GameObject with tag 'Player'")]
    [SerializeField] private GameObject player; // Ссылка на GameObject игрока

    // --- СТАТИЧЕСКИЕ ПОЛЯ ДЛЯ СОСТОЯНИЯ ЗАГРУЗКИ МЕЖДУ СЦЕНАМИ ---
    // Эти поля будут хранить загруженные данные и флаг загрузки, пока загружается новая сцена.
    private static PlayerData dataToLoad = null; // Статическое поле для хранения загруженных данных
    private static bool isLoadingGame = false; // Статический флаг, указывающий, что происходит загрузка
    // --- КОНЕЦ СТАТИЧЕСКИХ ПОЛЕЙ ---


    // Репозиторий и Интеракторы (не статические, создаются для каждого экземпляра менеджера в сцене)
    private IPlayerRepository _playerRepository;
    private SavePlayerUseCase _savePlayerUseCase;
    private LoadPlayerUseCase _loadPlayerUseCase;
    // DeleteSaveUseCase можно добавить по желанию, или вызывать Delete напрямую у репозитория.


    void Awake()
    {
        // --- Создаем экземпляры Репозитория и Интеракторов ---
        // Используем конкретную реализацию репозитория (Binary)
        _playerRepository = new BinaryPlayerRepository();
        // Создаем интеракторы, передавая им созданный репозиторий
        _savePlayerUseCase = new SavePlayerUseCase(_playerRepository);
        _loadPlayerUseCase = new LoadPlayerUseCase(_playerRepository);
        // --- КОНЕЦ Создания экземпляров ---

        // Находим игрока по тегу, если не назначен вручную (для удобства)
         if(player == null)
         {
             player = GameObject.FindGameObjectWithTag("Player");
             if(player == null)
             {
                 Debug.LogError("Player GameObject (with tag 'Player') not found! Save/Load functionality might not work correctly.", this);
             }
         }

        // В Awake мы только создаем объекты. Логика загрузки/применения данных происходит в Start.
    }

    void Start()
    {
        // --- ЛОГИКА ПРИМЕНЕНИЯ ЗАГРУЖЕННЫХ ДАННЫХ ПОСЛЕ ПЕРЕЗАГРУЗКИ СЦЕНЫ ---
        // Эта часть выполняется, когда новая сцена загрузилась И был флаг isLoadingGame.
        if (isLoadingGame && dataToLoad != null)
        {
            // Убеждаемся, что игрок найден в новой сцене
            if (player == null)
            {
                player = GameObject.FindGameObjectWithTag("Player");
            }

            if (player != null)
            {
                ApplyLoadedData(); // Применяем данные к игроку
                Debug.Log("Loaded data applied to player."); // Отладочный лог
            } else {
                 Debug.LogError("Player not found in the new scene after loading. Could not apply data."); // Отладочный лог
            }


            // Сбрасываем статические флаги после применения данных
            dataToLoad = null;
            isLoadingGame = false;

            // Опционально: сбросить Time.timeScale, если он был изменен при загрузке
            // Time.timeScale = 1.0f; // У вас было это в старом коде Start
        }
        else if (isLoadingGame && dataToLoad == null) // Случай, если флаг был, но данных почему-то нет (ошибка загрузки?)
        {
             Debug.LogWarning("SaveSystem: isLoadingGame was true, but dataToLoad is null. Load process might have failed or no save existed.");
             // Сбрасываем флаги на всякий случай
             isLoadingGame = false;
             // Time.timeScale = 1.0f;
        }
        // --- КОНЕЦ ЛОГИКИ ПРИМЕНЕНИЯ ДАННЫХ ---

        // Если isLoadingGame == false, это обычный старт сцены, ничего не делаем в Start() менеджера.
        // Если нужно автоматическая ЗАГРУЗКА при старте игры (первый запуск),
        // можно вызвать LoadGame() здесь, но убедитесь, что это происходит только при первом запуске сцены в игре, а не при каждой перезагрузке.
        // Проще вызывать LoadGame() из стартового меню или отдельного скрипта-инициализатора.
    }


    // --- ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ ЗАПУСКА ОПЕРАЦИЙ СОХРАНЕНИЯ/ЗАГРУЗКИ (ВЫЗЫВАЮТСЯ ИЗВНЕ) ---

    // Метод для сохранения игры (вызывается из UI, от менеджера игрового процесса и т.д.)
    public void SaveGame()
    {
        // Проверяем, найден ли игрок
        if (player == null)
        {
            Debug.LogWarning("Cannot save game: player GameObject is null.");
            return;
        }

        // Получаем компоненты игрока для сбора данных
        HealthSystem healthSystem = player.GetComponent<HealthSystem>();
        ManaSystem manaSystem = player.GetComponent<ManaSystem>();
        Transform playerTransform = player.transform; // Получаем Transform

        // Вызываем интерактор сохранения, передавая ему данные
        _savePlayerUseCase.Execute(healthSystem, manaSystem, playerTransform);

        Debug.Log("SaveGame called. Data save requested."); // Отладочный лог
    }

    // Метод для запуска загрузки игры (вызывается из UI, от менеджера игрового процесса и т.д.)
    public void LoadGame()
    {
        // Проверяем, есть ли вообще сохраненные данные перед попыткой загрузить
        if (!_playerRepository.HasSave())
        {
             Debug.Log("No save data found to load.");
             // Если сохранения нет, возможно, начинаем новую игру или остаемся в текущем состоянии.
             // Time.timeScale = 1.0f; // У вас было это в старом коде при отсутствии сохранения
             return;
        }

        // Вызываем интерактор загрузки, чтобы получить данные из репозитория
        PlayerData loadedData = _loadPlayerUseCase.Execute();

        // Если данные успешно загружены
        if (loadedData != null)
        {
            // Сохраняем загруженные данные и флаг загрузки в статические поля
            dataToLoad = loadedData;
            isLoadingGame = true;

            Debug.Log("LoadGame called. Data loaded into static fields. Reloading scene..."); // Отладочный лог

            // Перезагружаем текущую сцену.
            // В Start() нового экземпляра SaveSystem после перезагрузки сработает логика применения данных.
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

            // Опционально: заморозить время или показать экран загрузки
            // Time.timeScale = 0f; // У вас было это в старом коде LoadGame
        } else {
             // Если loadedData == null, значит, загрузка не удалась (ошибка уже залогирована в репозитории).
             Debug.LogError("LoadGame failed: Could not load data from repository.");
             dataToLoad = null;
             isLoadingGame = false;
             // Time.timeScale = 1.0f;
        }
    }

     // Метод для удаления сохранения (вызывается из UI, настроек и т.д.)
     public void DeleteSave()
     {
         // Просим репозиторий удалить данные
         _playerRepository.DeleteSave();
         Debug.Log("Save file deletion requested."); // Отладочный лог
     }


    // --- МЕТОД ПРИМЕНЕНИЯ ЗАГРУЖЕННЫХ ДАННЫХ К ИГРОКУ ---
    // Вызывается ТОЛЬКО в Start() SaveSystem после перезагрузки сцены, если есть данные для загрузки.
    private void ApplyLoadedData()
    {
        // Убеждаемся, что игрок и загруженные данные существуют
        if (player == null || dataToLoad == null)
        {
            Debug.LogError("ApplyLoadedData called but player or dataToLoad is null.");
            return;
        }

        // Применяем здоровье и ману, если компоненты существуют
        HealthSystem healthSystem = player.GetComponent<HealthSystem>();
        if (healthSystem != null)
        {
             healthSystem.SetHealth(dataToLoad.health);
             // Если maxHealth мог меняться, возможно, его тоже нужно установить
             // healthSystem.SetMaxHealth(dataToLoad.maxHealth);
        } else { Debug.LogWarning("HealthSystem component not found on player when applying loaded data."); }

        ManaSystem manaSystem = player.GetComponent<ManaSystem>();
        if (manaSystem != null)
        {
             manaSystem.SetMana(dataToLoad.mana);
              // Если maxMana мог меняться, возможно, его тоже нужно установить
             // manaSystem.SetMaxMana(dataToLoad.maxMana);
        } else { Debug.LogWarning("ManaSystem component not found on player when applying loaded data."); }

        // Применяем позицию и вращение игрока
        player.transform.position = dataToLoad.GetPosition();
        // Если сохраняешь вращение, добавь его в PlayerData и примени здесь
        // player.transform.rotation = dataToLoad.GetRotation(); // Если PlayerData хранит Quaternion

        // Debug.Log($"Applied loaded data: Health={dataToLoad.health}, Mana={dataToLoad.mana}, Pos={dataToLoad.GetPosition()}"); // Отладочный лог
    }


    // --- СТАТИЧЕСКИЙ МЕТОД ДЛЯ ПРОВЕРКИ СОСТОЯНИЯ ЗАГРУЗКИ ---
    // Вызывается из других скриптов (например, CharacterMovement.IsLoading())
    public static bool IsLoading()
    {
        return isLoadingGame;
    }

    // Опционально: метод для проверки наличия сохранения
     public bool HasSaveData()
     {
         // Просим репозиторий проверить наличие сохранения
         return _playerRepository.HasSave();
     }
}
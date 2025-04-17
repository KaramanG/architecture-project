using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Main Menu UI")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;

    [Header("Settings UI")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Button backButton;

    [Header("Pause Menu UI")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button exitToMenuButton;

    [Header("Audio")]
    [SerializeField] private AudioSource buttonClickSound;

    private bool isPaused = false;
    private bool isInMainMenu = true;

    private void Start()
    {
        InitializeMainMenu();
        InitializeSettings();
        InitializePauseMenu();
        SetInitialUIState();
    }

    private void Update()
    {
        // Проверяем Esc только если не в главном меню
        if (!isInMainMenu && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
    }

    #region Initialization
    private void SetInitialUIState()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    private void InitializeMainMenu()
    {
        if (playButton != null)
            playButton.onClick.AddListener(() =>
            {
                PlayButtonClickSound();
                PlayGame();
            });

        if (settingsButton != null)
            settingsButton.onClick.AddListener(() =>
            {
                PlayButtonClickSound();
                OpenSettings();
            });
    }

    private void InitializeSettings()
    {
        if (volumeSlider != null)
        {
            volumeSlider.value = PlayerPrefs.GetFloat("Volume", 0.8f);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (backButton != null)
            backButton.onClick.AddListener(() =>
            {
                PlayButtonClickSound();
                ReturnToMainMenu();
            });
    }

    private void InitializePauseMenu()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(() =>
            {
                PlayButtonClickSound();
                ResumeGame();
            });

        if (saveButton != null)
            saveButton.onClick.AddListener(() =>
            {
                PlayButtonClickSound();
                SaveGame();
            });

        if (exitToMenuButton != null)
            exitToMenuButton.onClick.AddListener(() =>
            {
                PlayButtonClickSound();
                ExitToMainMenu();
            });
    }
    #endregion

    #region Main Menu Functions
    public void PlayGame()
    {
        isInMainMenu = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(1); // Загружаем первую игровую сцену
    }

    public void OpenSettings()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            // Гарантируем, что кнопка Back активна
            if (backButton != null) backButton.gameObject.SetActive(true);
        }
    }

    public void ReturnToMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        // Гарантируем, что кнопка Back активна для следующего открытия
        if (backButton != null) backButton.gameObject.SetActive(true);
    }
    #endregion

    #region Pause Menu Functions
    public void TogglePauseMenu()
    {
        isPaused = !isPaused;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(isPaused);
            // Гарантируем, что все кнопки активны
            if (resumeButton != null) resumeButton.gameObject.SetActive(true);
            if (saveButton != null) saveButton.gameObject.SetActive(true);
            if (exitToMenuButton != null) exitToMenuButton.gameObject.SetActive(true);
        }

        Time.timeScale = isPaused ? 0f : 1f;
        AudioListener.pause = isPaused;
    }

    public void ResumeGame()
    {
        TogglePauseMenu();
    }

    public void SaveGame()
    {
        PlayerPrefs.SetInt("SavedLevel", 1);
        PlayerPrefs.Save();
        Debug.Log("Игра сохранена!");
    }

    public void ExitToMainMenu()
    {
        isInMainMenu = true;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(0); // Загружаем главное меню
    }
    #endregion

    #region Audio Functions
    private void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("Volume", value);
    }

    private void PlayButtonClickSound()
    {
        if (buttonClickSound != null && buttonClickSound.isActiveAndEnabled)
            buttonClickSound.Play();
    }
    #endregion
}
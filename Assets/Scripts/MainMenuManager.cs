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
    private bool isInMainMenu;

    private void Start()
    {
        InitializeMainMenu();
        InitializeSettings();
        InitializePauseMenu();
        SetInitialUIState();

        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            isInMainMenu = true;
        }
        else
        {
            isInMainMenu = false;
        }
    }

    private void Update()
    {
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
        SceneManager.LoadScene(1); // Çàãğóæàåì EğâE èãğîâóş ñöåíE
    }

    public void OpenSettings()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            // Ãàğàíòèğóåì, ÷òEEú@E Back àêòèâíE
            if (backButton != null) backButton.gameObject.SetActive(true);
        }
    }

    public void ReturnToMainMenu() //„‘ „~„u „u„q„… „‰„u „€„~„p „t„u„|„p„u„„ „„„p„{ „‰„„„€ „€„ƒ„„„p„r„|„ „{„p„{ „u„ƒ„„„ „„‚„€„ƒ„„„€ „~„€„r„…„ „†„…„~„{„ˆ„y„ „ƒ„€„x„t„p„}
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        // Ãàğàíòèğóåì, ÷òEEú@E Back àêòèâíEäëÿ ñëåäEùåãî úCEûòE
        if (backButton != null) backButton.gameObject.SetActive(true);
    }

    public void GoBackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
    #endregion

    #region Pause Menu Functions
    public void TogglePauseMenu()
    {
        isPaused = !isPaused;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(isPaused);
            // Ãàğàíòèğóåì, ÷òEâñEEú@E àêòèâíE
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
        Debug.Log("ÈãğEñîõğàíåíE");
    }

    public void ExitToMainMenu()
    {
        isInMainMenu = true;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(0); // Çàãğóæàåì ãëàâûMEEE
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
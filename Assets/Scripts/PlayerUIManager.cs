using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerUIManager : MonoBehaviour
{
    [SerializeField] private HealthSystem playerHealthSystem;

    [SerializeField] private Image healthBarFill;
    [SerializeField] private TextMeshProUGUI healthText;

    [SerializeField] private GameObject gameOverScreenPanel;

    private void Awake()
    {
        if (healthBarFill != null)
        {
            healthBarFill.type = Image.Type.Filled;
            healthBarFill.fillMethod = Image.FillMethod.Horizontal;
            healthBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }

    private void Start()
    {
        if (playerHealthSystem == null)
        {
            enabled = false;
            return;
        }

        playerHealthSystem.OnHealthChanged.AddListener(UpdateHealthUI);
        playerHealthSystem.OnPlayerDied.AddListener(ShowGameOverScreen);

        UpdateHealthUI(playerHealthSystem.GetHealth(), playerHealthSystem.GetMaxHealth());
    }

    private void OnDestroy()
    {
        if (playerHealthSystem != null)
        {
            playerHealthSystem.OnHealthChanged.RemoveListener(UpdateHealthUI);
            playerHealthSystem.OnPlayerDied.RemoveListener(ShowGameOverScreen);
        }
    }

    public void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthBarFill != null)
        {
            float fillAmount = (maxHealth > 0) ? (currentHealth / maxHealth) : 0f;
            healthBarFill.fillAmount = fillAmount;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(currentHealth)}";
        }
    }

    private void ShowGameOverScreen()
    {
        if (gameOverScreenPanel != null)
        {
            gameOverScreenPanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }
}

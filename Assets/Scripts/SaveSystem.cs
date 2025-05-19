using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.SceneManagement;

public class SaveSystem : MonoBehaviour
{
    [SerializeField] private GameObject player;

    private static PlayerData dataToLoad = null;
    private static bool isLoadingGame = false;
    private string saveFilePath;

    void Awake()
    {
        saveFilePath = Application.persistentDataPath + "/playerData.sav";
    }

    void Start()
    {
        if (isLoadingGame && dataToLoad != null)
        {
            if (player == null)
            {
                player = GameObject.FindGameObjectWithTag("Player");
            }

            ApplyLoadedData();
            dataToLoad = null;
            isLoadingGame = false;

            Time.timeScale = 1.0f;
        }
        else if (isLoadingGame)
        {
            dataToLoad = null;
            isLoadingGame = false;
            Time.timeScale = 1.0f;
        }
    }

    public void SaveGame()
    {
        HealthSystem healthSystem = player.GetComponent<HealthSystem>();
        ManaSystem manaSystem = player.GetComponent<ManaSystem>();

        Vector3 playerPos = player.transform.position;
        float playerHP = healthSystem.GetHealth();
        float playerMana = manaSystem.GetMana();

        PlayerData data = new PlayerData(playerHP, playerMana, playerPos);

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = null;

        try
        {
            stream = new FileStream(saveFilePath, FileMode.Create);
            formatter.Serialize(stream, data);
        }
        finally
        {
            if (stream != null) { stream.Close(); }
        }
    }

    public void LoadGame()
    {
        if (!File.Exists(saveFilePath))
        {
            return;
        }

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = null;

        try
        {
            stream = new FileStream(saveFilePath, FileMode.Open);
            PlayerData loadedData = formatter.Deserialize(stream) as PlayerData;

            if (loadedData != null)
            {
                dataToLoad = loadedData;
                isLoadingGame = true;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            else
            {
                dataToLoad = null;
                isLoadingGame = false;
            }
        }
        finally
        {
            if (stream != null) { stream.Close(); }
        }
    }

    private void ApplyLoadedData()
    {
        HealthSystem healthSystem = player.GetComponent<HealthSystem>();
        ManaSystem manaSystem = player.GetComponent<ManaSystem>();

        healthSystem.SetHealth(dataToLoad.health);
        manaSystem.SetMana(dataToLoad.mana);

        player.transform.position = dataToLoad.GetPosition();
    }

    public static bool IsLoading()
    {
        return isLoadingGame;
    }
}
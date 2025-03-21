using UnityEngine;
using UnityEngine.AI; // Обязательно подключаем пространство имен для NavMeshAgent

public class MobAI : MonoBehaviour
{
    public float speed = 3.5f;       // Скорость моба, настраивается в инспекторе
    public float stoppingDistance = 1.5f; // Дистанция остановки перед персонажем

    private NavMeshAgent agent;
    private Transform playerTransform;

    void Start()
    {
        // Получаем компонент NavMeshAgent, который должен быть на этом же объекте
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent компонент не найден на объекте " + gameObject.name);
            return; // Прерываем выполнение, если нет NavMeshAgent
        }

        // Ищем персонажа по тегу "Player". Убедись, что у твоего персонажа есть тег "Player"
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("Персонаж с тегом 'Player' не найден на сцене. Убедитесь, что ваш персонаж имеет тег 'Player'.");
            return; // Прерываем выполнение, если игрок не найден
        }

        // Настраиваем NavMeshAgent
        agent.speed = speed;
        agent.stoppingDistance = stoppingDistance;
    }

    void Update()
    {
        // Если персонаж найден и NavMeshAgent существует, указываем ему цель - позицию персонажа
        if (playerTransform != null && agent != null)
        {
            agent.SetDestination(playerTransform.position); // Задаем точку назначения - позицию персонажа
        }
    }
}
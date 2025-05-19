using UnityEngine;
using UnityEngine.AI; // Если используешь NavMeshAgent

public class BossAI : MonoBehaviour
{
    public enum BossState
    {
        Patrolling,
        Chasing,
        Attacking // Опционально, если атака - отдельное состояние
    }

    [Header("Состояние")]
    public BossState currentState = BossState.Patrolling;

    [Header("Ссылки")]
    public Transform playerTransform;
    private NavMeshAgent agent; // Если используется NavMesh
    private Animator animator;  // Если есть аниматор

    [Header("Характеристики")]
    public float health = 100f;
    public float detectionRadius = 20f; // На всякий случай, если захочешь агрить по близости потом
    public float attackRange = 2f;
    public float attackCooldown = 2f;
    private float currentAttackCooldown = 0f;

    [Header("Патрулирование")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 2f;
    private int currentPatrolIndex = 0;
    public float waypointProximityThreshold = 1f; // Насколько близко нужно подойти к точке патруля

    [Header("Преследование")]
    public float chaseSpeed = 4f;

    // Флаг, был ли босс спровоцирован
    private bool isProvoked = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>(); // Получаем Animator, если он есть

        // Попытка найти игрока по тегу, если не назначен вручную
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                Debug.LogError("Игрок не найден! Убедитесь, что у игрока есть тег 'Player' или назначьте его вручную.");
                enabled = false; // Выключаем скрипт, если игрока нет
                return;
            }
        }

        if (agent != null && patrolPoints.Length > 0)
        {
            agent.speed = patrolSpeed;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            currentState = BossState.Patrolling;
        }
        else if (agent == null)
        {
            Debug.LogWarning("NavMeshAgent не найден на боссе. Движение не будет работать.");
            // Можно реализовать простое движение без NavMesh, если нужно
        }
        else if (patrolPoints.Length == 0)
        {
            Debug.LogWarning("Точки патрулирования не заданы. Босс будет стоять на месте до провокации.");
            currentState = BossState.Patrolling; // Остается в патруле, но ничего не делает
        }
    }

    void Update()
    {
        if (playerTransform == null) return; // Если игрок исчез или не был найден

        if (currentAttackCooldown > 0)
        {
            currentAttackCooldown -= Time.deltaTime;
        }

        switch (currentState)
        {
            case BossState.Patrolling:
                HandlePatrolling();
                // Если isProvoked стал true (из TakeDamage), переключаемся
                if (isProvoked)
                {
                    SwitchToChaseState();
                }
                break;

            case BossState.Chasing:
                HandleChasing();
                break;

            case BossState.Attacking:
                HandleAttacking();
                break;
        }

        // Обновление анимаций (пример)
        if (animator != null && agent != null)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    void HandlePatrolling()
    {
        if (agent == null || patrolPoints.Length == 0) return;

        agent.speed = patrolSpeed;
        if (!agent.pathPending && agent.remainingDistance < waypointProximityThreshold)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
        // В этом состоянии босс не ищет игрока активно
    }

    void HandleChasing()
    {
        if (agent == null || playerTransform == null) return;

        agent.speed = chaseSpeed;
        agent.SetDestination(playerTransform.position);

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= attackRange)
        {
            SwitchToAttackState();
        }
        // Опционально: если игрок убежал слишком далеко, можно вернуться к патрулированию
        // if (distanceToPlayer > someMaxChaseDistance) { SwitchToPatrolState(); isProvoked = false; }
    }

    void HandleAttacking()
    {
        if (agent == null || playerTransform == null) return;

        agent.SetDestination(transform.position); // Остановиться для атаки
        // Поворот к игроку
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(directionToPlayer.x, 0, directionToPlayer.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);


        if (currentAttackCooldown <= 0f)
        {
            PerformAttack();
            currentAttackCooldown = attackCooldown;
        }

        // Проверка, не вышел ли игрок из зоны атаки
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer > attackRange)
        {
            SwitchToChaseState();
        }
    }

    void PerformAttack()
    {
        Debug.Log("Босс атакует игрока!");
        if (animator != null)
        {
            animator.SetTrigger("Attack"); // Предполагается, что есть триггер "Attack" в аниматоре
        }
        // Здесь логика нанесения урона игроку
        // Например: playerTransform.GetComponent<PlayerHealth>().TakeDamage(bossDamage);
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        Debug.Log($"Босс получил {amount} урона. Здоровье: {health}");

        if (!isProvoked)
        {
            isProvoked = true;
            Debug.Log("Босс спровоцирован!");
            // Немедленно переключаемся на преследование, если босс был в патруле
            if (currentState == BossState.Patrolling)
            {
                SwitchToChaseState();
            }
        }

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Босс побежден!");
        if (animator != null)
        {
            animator.SetTrigger("Die"); // Предполагается, что есть триггер "Die"
        }
        // Отключить компонент AI или уничтожить объект
        // agent.enabled = false; // если есть NavMeshAgent
        // this.enabled = false;
        Destroy(gameObject, 3f); // Уничтожить через 3 секунды, чтобы анимация смерти проигралась
    }

    void SwitchToChaseState()
    {
        Debug.Log("Босс: Переход в состояние ПРЕСЛЕДОВАНИЯ");
        currentState = BossState.Chasing;
        if (agent != null) agent.speed = chaseSpeed;
        if (animator != null) animator.SetBool("IsChasing", true); // Пример анимационного параметра
    }

    void SwitchToAttackState()
    {
        Debug.Log("Босс: Переход в состояние АТАКИ");
        currentState = BossState.Attacking;
        if (animator != null) animator.SetBool("IsChasing", false); // Возможно, выключить анимацию бега
    }

    // Если нужно будет вернуться к патрулю (например, если игрок далеко убежал и босс "потерял" его)
    // void SwitchToPatrolState()
    // {
    //     Debug.Log("Босс: Переход в состояние ПАТРУЛИРОВАНИЯ");
    //     currentState = BossState.Patrolling;
    //     isProvoked = false; // Сбрасываем провокацию
    //     if (agent != null && patrolPoints.Length > 0)
    //     {
    //         agent.speed = patrolSpeed;
    //         agent.SetDestination(patrolPoints[currentPatrolIndex].position);
    //     }
    //     if (animator != null) animator.SetBool("IsChasing", false);
    // }

    // Отрисовка радиусов в редакторе для удобства
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);
                    if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                    }
                    else if (patrolPoints.Length > 1 && patrolPoints[0] != null) // Замкнуть путь
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[0].position);
                    }
                }
            }
        }
    }
}
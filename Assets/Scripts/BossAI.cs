// BossAI.cs
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(HealthSystem))]
[RequireComponent(typeof(NavMeshAgent))]
public class BossAI : MonoBehaviour
{
    public enum BossState
    {
        Patrolling,
        Chasing,
        Attacking,
        Dead
    }

    [Header("AI State")]
    public BossState currentState = BossState.Patrolling;

    [Header("References")]
    public Transform playerTransform;
    private NavMeshAgent agent;
    private Animator animator;
    private HealthSystem healthSystem;

    [Header("Combat Stats")]
    public float attackRange = 3f;
    public float attackDamage = 15f;
    public float attackCooldown = 2f;
    private float currentAttackCooldown = 0f;

    [Header("Patrolling")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 2f;
    private int currentPatrolIndex = 0;
    public float waypointProximityThreshold = 1f;

    [Header("Chasing")]
    public float chaseSpeed = 5f;

    private bool isProvokedByDamage = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        healthSystem = GetComponent<HealthSystem>();

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
            else
            {
                Debug.LogError("Игрок ('Player' tag) не найден! Босс не будет функционировать.");
                enabled = false; return;
            }
        }
    }

    void Start()
    {
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent не найден на боссе. Движение не будет работать.");
            enabled = false; return;
        }
        SwitchToPatrolState(); // Начинаем с патрулирования
    }

    void SwitchToPatrolState() // Вспомогательный метод для чистоты
    {
        if (patrolPoints.Length > 0 && agent.isOnNavMesh)
        {
            agent.speed = patrolSpeed;
            if (agent.isOnNavMesh) agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            SwitchState(BossState.Patrolling);
        }
        else
        {
            Debug.LogWarning("Точки патрулирования не заданы или NavMeshAgent не активен. Босс будет стоять на месте до провокации.");
            SwitchState(BossState.Patrolling); // Остается в патруле, но ничего не делает
        }
    }


    void Update()
    {
        if (playerTransform == null || currentState == BossState.Dead) return;

        if (healthSystem.IsDead())
        {
            if (currentState != BossState.Dead) SwitchState(BossState.Dead);
            return;
        }

        if (currentAttackCooldown > 0)
        {
            currentAttackCooldown -= Time.deltaTime;
        }

        DecideState();

        // Выполнение действий текущего состояния только если не мертв
        if (currentState != BossState.Dead)
        {
            switch (currentState)
            {
                case BossState.Patrolling:
                    HandlePatrolling();
                    break;
                case BossState.Chasing:
                    HandleChasing();
                    break;
                case BossState.Attacking:
                    HandleAttacking();
                    break;
            }
        }
        UpdateAnimatorParams();
    }

    void DecideState()
    {
        if (currentState == BossState.Dead) return;

        if (isProvokedByDamage)
        {
            // Если спровоцирован, решаем: атаковать или преследовать
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= attackRange)
            {
                if (currentState != BossState.Attacking) SwitchState(BossState.Attacking);
            }
            else // Игрок дальше зоны атаки, но мы спровоцированы -> преследуем
            {
                if (currentState != BossState.Chasing) SwitchState(BossState.Chasing);
            }
        }
        else // НЕ спровоцирован уроном
        {
            // Должен патрулировать
            if (currentState != BossState.Patrolling)
            {
                SwitchState(BossState.Patrolling);
            }
        }
    }


    void SwitchState(BossState newState)
    {
        if (currentState == newState && newState != BossState.Patrolling) return; // Позволяем "обновить" патруль, если уже патрулирует

        // Debug.Log($"Boss: {gameObject.name} switching from {currentState} to {newState}");
        currentState = newState;

        if (agent == null || !agent.isOnNavMesh && newState != BossState.Dead) // Для Dead агент может быть уже выключен
        {
            if (newState != BossState.Dead) Debug.LogWarning("Boss NavMeshAgent is null or not on NavMesh when trying to switch state to " + newState);
            return;
        }


        switch (currentState)
        {
            case BossState.Patrolling:
                agent.speed = patrolSpeed;
                agent.isStopped = false; // Убедимся, что агент может двигаться
                if (patrolPoints.Length > 0)
                {
                    if (agent.isOnNavMesh) agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                }
                else
                {
                    if (agent.isOnNavMesh) agent.ResetPath(); // Стоять на месте
                }
                break;
            case BossState.Chasing:
                agent.speed = chaseSpeed;
                agent.isStopped = false;
                break;
            case BossState.Attacking:
                if (agent.isOnNavMesh) // Только если агент на NavMesh
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                }
                break;
            case BossState.Dead:
                OnBossAIDeath();
                break;
        }
    }

    void HandlePatrolling()
    {
        if (agent == null || !agent.isOnNavMesh || agent.isStopped || patrolPoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance < waypointProximityThreshold)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }

    void HandleChasing()
    {
        if (agent == null || !agent.isOnNavMesh || agent.isStopped || playerTransform == null) return;
        if (agent.destination != playerTransform.position) // Обновляем путь только если он изменился
            agent.SetDestination(playerTransform.position);
    }

    void HandleAttacking()
    {
        if (agent == null || playerTransform == null) return; // Агент может быть не на NavMesh, если он isStopped = true

        transform.LookAt(playerTransform.position); // Простой поворот к игроку

        if (currentAttackCooldown <= 0f)
        {
            PerformAttack();
            currentAttackCooldown = attackCooldown;
        }
    }

    void PerformAttack()
    {
        Debug.Log(gameObject.name + " атакует игрока!");
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        if (playerTransform != null)
        {
            HealthSystem playerHealth = playerTransform.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }
    }

    public void NotifyDamageTaken(float amount)
    {
        if (currentState == BossState.Dead) return;

        Debug.Log($"{gameObject.name} был уведомлен о получении {amount} урона. Текущее здоровье: {healthSystem.GetHealth()}");

        if (!isProvokedByDamage)
        {
            isProvokedByDamage = true;
            Debug.Log(gameObject.name + " спровоцирован уроном!");
            // Немедленно решаем, что делать, а не ждем следующего Update
            DecideState();
        }

        // Проверка на смерть (хотя HealthSystem это уже делает для анимации)
        // HealthSystem уже вызовет анимацию и isDead. Нам нужно только обновить состояние AI.
        if (healthSystem.IsDead() && currentState != BossState.Dead)
        {
            SwitchState(BossState.Dead);
        }
    }

    private void OnBossAIDeath()
    {
        Debug.Log("BossAI: " + gameObject.name + " побежден (AI cleanup)!");
        if (agent != null && agent.isOnNavMesh) // Проверяем, существует ли еще агент
        {
            agent.isStopped = true;
            agent.enabled = false; // Отключаем компонент
        }
        // gameObject.SetActive(false); // Или Destroy(gameObject, delay);
    }

    void UpdateAnimatorParams()
    {
        if (animator != null && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (playerTransform != null) Gizmos.DrawWireSphere(transform.position, attackRange); // Рисуем, только если есть игрок

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);
                    if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null) Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                    else if (patrolPoints.Length > 1 && patrolPoints[0] != null) Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[0].position);
                }
            }
        }
    }
}
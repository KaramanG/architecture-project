using UnityEngine;
using UnityEngine.AI;
using System.Collections; // Required for IEnumerator (if you add coroutines like DelayedDespawn)

// Ensure these components are attached to the GameObject when BossAI is added
[RequireComponent(typeof(HealthSystem))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))] // Added Animator requirement
[RequireComponent(typeof(Rigidbody))] // Added Rigidbody requirement for consistency and death handling

public class BossAI : MonoBehaviour
{
    // Enum для состояний босса
    // Сделаем public, чтобы другие скрипты могли его видеть
    public enum BossState
    {
        Patrolling, // Патрулирует заданные точки
        Chasing,    // Преследует игрока
        Attacking,  // Атакует игрока
        Dead        // Мертв
        // Stunned не добавляем, т.к. у босса по этой логике нет состояния "оглушен"
    }

    // [Header("AI State")] // <-- ЭТА СТРОКА УДАЛЕНА, т.к. Header нельзя применять к свойствам
    // Сделаем CurrentState public get; private set; чтобы можно было читать состояние из других скриптов
    public BossState CurrentState { get; private set; } = BossState.Patrolling;


    [Header("References")] // Этот Header теперь относится к полям ниже
    [Tooltip("Assign the player's Transform here, or it will try to find GameObject with tag 'Player'")]
    public Transform playerTransform; // Ссылка на Transform игрока (можно назначить вручную)
    private NavMeshAgent agent; // Ссылка на NavMeshAgent (будет получена в Awake)
    private Animator animator;  // Ссылка на Animator (будет получена в Awake)
    private HealthSystem healthSystem; // Ссылка на HealthSystem (будет получена в Awake)
    private Rigidbody rb;       // Ссылка на Rigidbody (будет получена в Awake)


    [Header("Combat Stats")]
    [SerializeField] private float attackRange = 3f; // Дистанция, на которой босс может атаковать
    [SerializeField] private float attackDamage = 15f; // Урон от атаки
    [SerializeField] private float attackCooldown = 2f; // Кулдаун между атаками
    private float currentAttackCooldown = 0f; // Таймер текущего кулдауна


    [Header("Patrolling")]
    [Tooltip("Array of points the boss will patrol between")]
    public Transform[] patrolPoints; // Точки патрулирования
    [SerializeField] private float patrolSpeed = 2f; // Скорость патрулирования
    private int currentPatrolIndex = 0; // Индекс текущей точки патрулирования
    [SerializeField] private float waypointProximityThreshold = 1f; // Расстояние до точки, считающееся "достигнутой"


    [Header("Chasing")]
    [SerializeField] private float chaseSpeed = 5f; // Скорость преследования


    // Flags and state variables
    private bool isProvokedByDamage = false; // Флаг, спровоцирован ли босс уроном


    void Awake()
    {
        // --- ИСПРАВЛЕНО: Получаем все необходимые компоненты и проверяем на null ---
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component not found on " + gameObject.name + ". Boss AI will not work correctly! Script disabled.", this);
            enabled = false; // Скрипт не может работать без NavMeshAgent
            return;
        }

        animator = GetComponent<Animator>();
        if (animator == null) Debug.LogWarning("Animator component not found on " + gameObject.name + ". Boss animations will not work.", this);

        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem == null) Debug.LogWarning("HealthSystem component not found on " + gameObject.name + ". Boss death and health logic will not work.", this);

        rb = GetComponent<Rigidbody>();
        if (rb == null) Debug.LogWarning("Rigidbody component not found on " + gameObject.name + ". Boss physics might not behave as expected, especially on death.", this);


        // Находим игрока по тегу "Player", если playerTransform не назначен вручную
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                Debug.LogError("Player GameObject (with 'Player' tag) not found for Boss: " + gameObject.name + ". AI requires a player target! Script disabled.", this);
                enabled = false; // Скрипт не может работать без игрока
                return;
            }
        }

        // Начальная настройка NavMeshAgent (скорость и дистанция остановки могут меняться в SwitchState)
        agent.speed = patrolSpeed; // Начальная скорость - патрулирование
        agent.stoppingDistance = attackRange; // Устанавливаем дистанцию остановки равной дистанции атаки
        agent.updateRotation = true; // Позволяем NavMeshAgent вращать босса
        agent.updatePosition = true; // Позволяем NavMeshAgent двигать босса

        CurrentState = BossState.Patrolling; // Начальное состояние
        currentAttackCooldown = 0f; // Таймер кулдауна атаки
    }

    void Start()
    {
        // Убедимся, что агент действителен после Awake
        if (agent == null || !enabled) return;

        // Начинаем патрулирование при старте
        SwitchState(BossState.Patrolling);
    }

    void Update()
    {
        // Если игрок исчез, босс мертв, или скрипт выключен, прекращаем обработку
        if (playerTransform == null || CurrentState == BossState.Dead || !enabled) return;

        // --- ИСПРАВЛЕНО: Улучшена проверка на смерть (на случай, если HealthSystem был, но стал null или умер другим способом) ---
        // Проверяем здоровье босса, это приоритетное состояние
        if (healthSystem != null && healthSystem.IsDead())
        {
            if (CurrentState != BossState.Dead) SwitchState(BossState.Dead); // Переходим в состояние смерти, если еще не там
            return; // Прекращаем обработку, если босс мертв
        }
        // --- КОНЕЦ ИСПРАВЛЕНО ---


        // Обновляем таймер кулдауна атаки
        if (currentAttackCooldown > 0)
        {
            currentAttackCooldown -= Time.deltaTime;
        }

        // Определяем текущее состояние на основе логики AI
        DecideState();

        // Выполняем действия, соответствующие текущему состоянию (если босс не мертв)
        // Проверка currentState != BossState.Dead уже есть в начале Update
        switch (CurrentState)
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
                // Dead state logic is in OnBossAIDeath and handled by the return statement at the start of Update
        }

        // Обновляем параметры аниматора в конце Update
        UpdateAnimatorParams();
    }

    // Определяет, в какое состояние должен перейти босс
    void DecideState()
    {
        // Босс принимает решение только если не мертв
        if (CurrentState == BossState.Dead) return;

        // --- ИСПРАВЛЕНО: Логика принятия решения о состоянии ---
        // Если босс спровоцирован (получил урон), он преследует или атакует
        if (isProvokedByDamage)
        {
            // Проверяем дистанцию до игрока
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (distanceToPlayer <= attackRange)
            {
                // Игрок в радиусе атаки
                if (CurrentState != BossState.Attacking) SwitchState(BossState.Attacking);
            }
            else // Игрок дальше радиуса атаки, но спровоцирован -> преследуем
            {
                if (CurrentState != BossState.Chasing) SwitchState(BossState.Chasing);
            }
        }
        else // Босс не спровоцирован -> патрулирует
        {
            if (CurrentState != BossState.Patrolling)
            {
                SwitchState(BossState.Patrolling);
            }
        }
        // --- КОНЕЦ ИСПРАВЛЕНО ---
    }


    // Переключает состояние босса и настраивает NavMeshAgent
    void SwitchState(BossState newState)
    {
        // Переключаем состояние только если оно новое
        if (CurrentState == newState) return;

        // Debug.Log($"Boss: {gameObject.name} switching from {CurrentState} to {newState}");
        CurrentState = newState; // Устанавливаем новое состояние

        // --- ИСПРАВЛЕНО: Управление NavMeshAgent при смене состояния с проверкой на null ---
        // Убеждаемся, что агент существует и активен перед использованием
        if (agent != null && agent.enabled)
        {
            switch (CurrentState)
            {
                case BossState.Patrolling:
                    agent.speed = patrolSpeed; // Устанавливаем скорость патрулирования
                    agent.isStopped = false; // Разрешаем движение
                    // Цель устанавливается в HandlePatrolling или при входе в состояние, если точки есть
                    if (patrolPoints != null && patrolPoints.Length > 0 && agent.isOnNavMesh)
                    {
                         agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                    } else if (agent.isOnNavMesh) {
                         agent.ResetPath(); // Если нет точек, стоим на месте
                    }
                    break;

                case BossState.Chasing:
                    agent.speed = chaseSpeed; // Устанавливаем скорость преследования
                    agent.isStopped = false; // Разрешаем движение
                    // Цель устанавливается в HandleChasing
                    break;

                case BossState.Attacking:
                    // Останавливаем агента при атаке
                    if (agent.isOnNavMesh) // Проверяем, что агент на NavMesh перед остановкой/сбросом
                    {
                        agent.isStopped = true;
                        agent.ResetPath();
                    }
                    // Вращение к игроку и логика атаки происходит в HandleAttacking
                    break;

                case BossState.Dead:
                    // Логика смерти обрабатывается в OnBossAIDeath
                    // Если агент был включен, он будет отключен там
                     if (agent != null && agent.isOnNavMesh)
                     {
                         agent.isStopped = true;
                         agent.enabled = false;
                     }
                    OnBossAIDeath(); // Вызываем метод обработки смерти
                    break;
            }
        }
        else if (CurrentState != BossState.Dead) // Если агент отсутствует или выключен, но не мертв, логируем ошибку
        {
            Debug.LogWarning("Boss NavMeshAgent is null or disabled when trying to switch state to " + newState + " on " + gameObject.name);
            // Возможно, стоит переключиться в Idle или как-то еще обработать отсутствие агента
            // For now, just log and return. The AI will be stuck.
        }
        // --- КОНЕЦ ИСПРАВЛЕНО ---
    }

    // --- Методы обработки каждого состояния ---

    void HandlePatrolling()
    {
        // --- ИСПРАВЛЕНО: Проверки на null для агента и точек патрулирования ---
        if (agent == null || !agent.enabled || !agent.isOnNavMesh || patrolPoints == null || patrolPoints.Length == 0)
        {
            // Если патрулировать невозможно, переходим в Idle или остаемся в текущем состоянии и логируем
            if (CurrentState == BossState.Patrolling) Debug.LogWarning("Boss " + gameObject.name + " cannot patrol: NavMeshAgent issue or no patrol points.");
            // Можно добавить логику перехода в Idle: SwitchState(BossState.Idle);
            return;
        }

        // Проверяем, достиг ли босс текущей точки патрулирования
        // Проверяем remainingDistance и pathPending для надежности
        if (!agent.pathPending && agent.remainingDistance <= waypointProximityThreshold)
        {
            // Переходим к следующей точке в массиве (по кругу)
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            // Устанавливаем новую цель для агента
            if (patrolPoints[currentPatrolIndex] != null)
            {
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            } else {
                 Debug.LogError("Patrol point " + currentPatrolIndex + " is null for " + gameObject.name);
                 // Возможно, стоит перейти в Idle или попробовать следующую точку
            }
        }
        // --- КОНЕЦ ИСПРАВЛЕНО ---
    }

    void HandleChasing()
    {
        // --- ИСПРАВЛЕНО: Проверки на null для агента и игрока ---
        if (agent == null || !agent.enabled || !agent.isOnNavMesh || playerTransform == null)
        {
             if (CurrentState == BossState.Chasing) Debug.LogWarning("Boss " + gameObject.name + " cannot chase: NavMeshAgent issue or player is null.");
             // Если преследовать невозможно, переходим в Idle или Patrol
             SwitchState(BossState.Patrolling); // Возвращаемся к патрулированию
             return;
        }

        // Устанавливаем целью позицию игрока.
        // Дополнительная проверка дистанции до текущей цели агента может предотвратить лишние вызовы SetDestination.
        if (Vector3.Distance(agent.destination, playerTransform.position) > 0.1f)
        {
             agent.SetDestination(playerTransform.position);
        }
        // --- КОНЕЦ ИСПРАВЛЕНО ---
    }

    void HandleAttacking()
    {
        // --- ИСПРАВЛЕНО: Проверки на null для агента и игрока ---
        // Агент должен быть остановлен в SwitchState при входе в Attacking.
        // Убеждаемся, что игрок есть.
        if (playerTransform == null)
        {
            if (CurrentState == BossState.Attacking) Debug.LogWarning("Boss " + gameObject.name + " cannot attack: Player is null.");
            SwitchState(BossState.Patrolling); // Если игрок пропал, возвращаемся к патрулированию
            return;
        }


        // Всегда смотрим на игрока во время атаки
        Vector3 lookPos = playerTransform.position - transform.position;
        lookPos.y = 0; // Вращение только по горизонтали
         if (lookPos.magnitude > 0) // Проверка, чтобы избежать Quaternion.LookRotation(Vector3.zero)
         {
             Quaternion targetRotation = Quaternion.LookRotation(lookPos);
             // Плавное вращение
             transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f); // Скорость вращения 10f можно сделать параметром
         }


        // Проверяем, прошло ли достаточно времени с последней атаки
        if (currentAttackCooldown <= 0f)
        {
            PerformAttack(); // Выполняем атаку
            currentAttackCooldown = attackCooldown; // Сбрасываем кулдаун
        }
        // --- КОНЕЦ ИСПРАВЛЕНО ---
    }

    // Выполняет действие атаки
    void PerformAttack()
    {
        // Debug.Log(gameObject.name + " performs attack!");
        // --- ИСПРАВЛЕНО: Проверка на null перед вызовом SetTrigger ---
        if (animator != null)
        {
            // Предполагается, что у вас есть триггер "Attack" в аниматоре
             bool attackTriggerExists = false;
             foreach(var param in animator.parameters)
             {
                 if(param.type == AnimatorControllerParameterType.Trigger && param.name == "Attack")
                 {
                     attackTriggerExists = true;
                     break;
                 }
             }
             if(attackTriggerExists)
             {
                animator.SetTrigger("Attack");
             } else {
                 Debug.LogWarning("Animator trigger 'Attack' not found for " + gameObject.name);
             }
        }
        // --- КОНЕЦ ИСПРАВЛЕНО ---

        // Логика нанесения урона игроку
        // Это пример прямого нанесения урона. Часто урон наносится из Animation Event
        // для синхронизации с моментом удара в анимации.
        // --- ИСПРАВЛЕНО: Проверка на null перед нанесением урона ---
        if (playerTransform != null)
        {
            HealthSystem playerHealth = playerTransform.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                // Проверяем, находится ли игрок еще в радиусе атаки в момент удара (опционально, но хорошо)
                if (Vector3.Distance(transform.position, playerTransform.position) <= attackRange * 1.1f) // Небольшой допуск
                {
                    playerHealth.TakeDamage(attackDamage);
                    // Debug.Log(gameObject.name + " dealt " + attackDamage + " damage to player.");
                }
            }
            else
            {
                // Debug.LogWarning("Player HealthSystem component not found on " + playerTransform.name);
            }
        }
        // --- КОНЕЦ ИСПРАВЛЕНО ---
    }


    // Метод, вызываемый извне (например, от игрока, когда босс получает урон)
    // Должен быть вызван ИЗ Скрипта, Наносящего Урон, после нанесения урона боссу.
    // Например, в скрипте игрока или снаряда, после вызова mobHealth.TakeDamage(amount)
    // добавить: if (mobHealth.GetComponent<BossAI>() != null) mobHealth.GetComponent<BossAI>().NotifyDamageTaken(amount);
    public void NotifyDamageTaken(float amount)
    {
        // Игнорируем урон, если босс уже мертв
        if (CurrentState == BossState.Dead) return;

        // Debug.Log($"{gameObject.name} took {amount} damage. Current Health: {(healthSystem != null ? healthSystem.GetHealth().ToString() : "N/A")}");

        // Если босс не был спровоцирован, становимся спровоцированными
        if (!isProvokedByDamage)
        {
            isProvokedByDamage = true;
            // Debug.Log(gameObject.name + " is now provoked!");
            // Сразу перерешаем состояние, чтобы начать преследование/атаку
            DecideState();
        }

        // Проверяем, умер ли босс в результате этого урона
        // Эту проверку HealthSystem должен обрабатывать сам и переходить в Dead state.
        // Но можно добавить и здесь для надежности AI.
        if (healthSystem != null && healthSystem.IsDead() && CurrentState != BossState.Dead)
        {
            SwitchState(BossState.Dead); // Переходим в состояние смерти
        }
    }


    // Обрабатывает действия при смерти босса
    private void OnBossAIDeath()
    {
        // Убедимся, что логика смерти выполняется только один раз
        if (CurrentState == BossState.Dead && !enabled) return;

        Debug.Log("BossAI: " + gameObject.name + " is dead (AI cleanup)!");

        // --- ИСПРАВЛЕНО: Отключение NavMeshAgent и Rigidbody при смерти ---
        // Отключаем NavMeshAgent, чтобы босс не двигался и не просчитывал пути
        if (agent != null && agent.enabled) // Проверяем, что агент существует и был включен
        {
            agent.isStopped = true; // Останавливаем текущее движение
            agent.ResetPath(); // Очищаем текущий путь
            agent.enabled = false; // Полностью отключаем компонент
        }

        // Замораживаем Rigidbody, чтобы босс не падал и не двигался от физики
        if (rb != null) // Проверяем, что Rigidbody существует
        {
            rb.velocity = Vector3.zero; // Обнуляем скорость
            rb.angularVelocity = Vector3.zero; // Обнуляем вращение
            rb.isKinematic = true; // Переводим в кинематический режим (игнорирует физику)
            // Или использовать constraints: rb.constraints = RigidbodyConstraints.FreezeAll;
        }
        // --- КОНЕЦ ИСПРАВЛЕНО ---

        // Предполагается, что HealthSystem уже проиграл анимацию смерти.
        // Если нет, можно триггерить ее здесь:
        // if (animator != null) animator.SetTrigger("Death"); // Убедитесь, что такой триггер есть

        // Отключаем сам скрипт AI
        enabled = false;

        // Опционально: уничтожить GameObject после задержки (например, после завершения анимации смерти)
        // StartCoroutine(DelayedDespawn(10f)); // Нужен метод DelayedDespawn, если хотите автоудаление
    }

    /* // Пример coroutine для отложенного уничтожения
    private IEnumerator DelayedDespawn(float delay)
    {
        yield return new WaitForSeconds(delay);
        DespawnBoss();
    } */

    // Метод для уничтожения босса (можно вызвать из Animation Event в конце анимации смерти)
    public void DespawnBoss() // Изменил название для ясности, что это босс
    {
         // Убедимся, что мы не пытаемся уничтожить уже уничтоженный объект
         if (gameObject != null)
         {
            Destroy(gameObject); // Уничтожаем этот GameObject
         }
    }


    // Обновляет параметры аниматора на основе состояния моба
    void UpdateAnimatorParams()
    {
        // Убеждаемся, что аниматор существует и агент активен для определения движения
        bool hasAnimator = (animator != null);
        bool isAgentActive = (agent != null && agent.enabled && agent.isOnNavMesh);

        if (!hasAnimator) return; // Ничего не делаем, если нет аниматора

        // Определяем скорость для аниматора
        float speedForAnimator = 0f;
        if (isAgentActive && !agent.isStopped) // Если агент активен и не остановлен вручную
        {
             // Используем скорость агента для определения движения
             speedForAnimator = agent.velocity.magnitude;
             // Добавьте порог, если анимация "ходьба" должна включаться только при достаточной скорости
             // if (speedForAnimator < 0.1f) speedForAnimator = 0f;
        }

        // Предполагается, что у вас есть параметр "Speed" типа Float или "IsWalking" типа Bool
        // На скриншоте был "IsWalking" (Bool). Используем его.
        // Моб "идет" только если speedForAnimator > 0 (т.е. движется) и не находится в состоянии атаки/мертв
        if (animator != null) // Повторная проверка animator на null, хотя уже есть hasAnimator
        {
             bool isWalkingParamExists = false; // Проверяем, есть ли булевый параметр "IsWalking"
             foreach(var param in animator.parameters)
             {
                 if(param.type == AnimatorControllerParameterType.Bool && param.name == "IsWalking")
                 {
                     isWalkingParamExists = true;
                     break;
                 }
             }
             if(isWalkingParamExists)
             {
                 // ИСПРАВЛЕНО: Убрана проверка на CurrentState != BossState.Stunned, т.к. Stunned нет в BossState
                 animator.SetBool("IsWalking", speedForAnimator > 0.01f && CurrentState != BossState.Attacking && CurrentState != BossState.Dead); // Не движется, если атакует или мертв
             } else {
                 // Fallback: если нет "IsWalking", возможно есть "Speed" Float
                 bool speedParamExists = false;
                  foreach(var param in animator.parameters)
                  {
                      if(param.type == AnimatorControllerParameterType.Float && param.name == "Speed")
                      {
                          speedParamExists = true;
                          break;
                      }
                  }
                 if(speedParamExists)
                 {
                    animator.SetFloat("Speed", speedForAnimator);
                 } else {
                     // Debug.LogWarning("Animator parameter 'IsWalking' or 'Speed' not found for " + gameObject.name);
                 }
             }
            // Обратите внимание: триггеры атаки, стана и смерти устанавливаются в SwitchState и OnBossAIDeath.
        }
    }


    // --- Методы для отладки/гизмо ---
    void OnDrawGizmosSelected()
    {
        // Отображение радиуса атаки в редакторе
        Gizmos.color = Color.red;
        if (transform != null) Gizmos.DrawWireSphere(transform.position, attackRange);


        // Отображение точек патрулирования и соединяющих линий
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawSphere(patrolPoints[i].position, 0.3f); // Отображаем точку
                    // Рисуем линии между точками патрулирования (и замыкаем цикл)
                    if (patrolPoints.Length > 1)
                    {
                         int nextIndex = (i + 1) % patrolPoints.Length;
                         if (patrolPoints[nextIndex] != null)
                         {
                             Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
                         }
                    }
                }
            }
        }

        // Отображение цели NavMeshAgent (если есть и не Patrol/Attacking/Dead)
        // ИСПРАВЛЕНО: Убран некорректный чек на BossState.Stunned
         if (agent != null && agent.hasPath && CurrentState != BossState.Patrolling && CurrentState != BossState.Attacking && CurrentState != BossState.Dead)
         {
             Gizmos.color = Color.yellow;
             // Рисуем путь агента
             Vector3 lastCorner = transform.position;
             foreach (var corner in agent.path.corners)
             {
                 Gizmos.DrawLine(lastCorner, corner);
                 lastCorner = corner;
             }
         }

        // Отображение точки убегания (если в состоянии Fleeing - хотя босс не убегает в этом скрипте)
        // В данном скрипте BossAI нет состояния Fleeing. Если бы было, можно было бы добавить Gizmo.
        // Например: if (CurrentState == BossState.Fleeing && agent != null && agent.hasPath) { ... Gizmos.color = Color.cyan; Gizmos.DrawSphere(agent.destination, 0.5f); }
    }

    // --- *** Animation Event Method *** ---
    // Если вы используете Animation Event для нанесения урона, этот метод должен быть вызван из анимации атаки.
    // public void BossDealDamageAnimationEvent()
    // {
    //     // Здесь логика нанесения урона, возможно, проверка на попадание коллайдером и т.п.
    //     // Прямое нанесение урона игроку:
    //      if (playerTransform != null)
    //      {
    //          HealthSystem playerHealth = playerTransform.GetComponent<HealthSystem>();
    //          if (playerHealth != null)
    //          {
    //              // Проверяем, находится ли игрок в радиусе атаки в момент удара (опционально)
    //              if (Vector3.Distance(transform.position, playerTransform.position) <= attackRange * 1.1f) // Небольшой допуск
    //              {
    //                  playerHealth.TakeDamage(attackDamage);
    //                  Debug.Log(gameObject.name + " dealt " + attackDamage + " damage to player via Animation Event.");
    //              }
    //          }
    //      }
    // }

    // --- *** Animation Event Method *** ---
    // Если вы используете Animation Event для завершения анимации атаки и возврата контроля AI.
    // Предполагается, что имя события в анимации - OnBossAttackAnimationEnd
     public void OnBossAttackAnimationEnd()
     {
         // Этот метод вызывается событием Animation Event в конце анимации "Attack".

         // Debug.Log(gameObject.name + ": Animation Event - OnBossAttackAnimationEnd called.");

         // Если босс еще жив и находится в состоянии атаки, возвращаемся к принятию решения
         // (чтобы либо продолжить атаковать, либо начать преследовать, либо патрулировать)
         if ((healthSystem == null || !healthSystem.IsDead()) && CurrentState == BossState.Attacking)
         {
              DecideState(); // Перерешаем состояние
         }
         // Если состояние уже сменилось (например, босс оглушен или убит во время анимации),
         // SwitchState уже позаботился об агенте и других действиях.
     }
    // --- *** КОНЕЦ Animation Event Method *** ---

}
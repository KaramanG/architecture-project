using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BossAI : MonoBehaviour
{
    // Параметры босса
    [Header("Boss Parameters")]
    [SerializeField] private float bossSpeed;
    [SerializeField] private float attackStoppingDistance; // Дистанция, на которой босс останавливается для атаки
    [SerializeField] private float agroRadius; // Радиус, в котором босс становится агрессивным

    [Header("Attack Parameters")]
    [SerializeField] private float normalAttackRate = 1f; // Атак в секунду для обычной атаки
    [SerializeField] private float strongAttackRate = 0.5f; // Атак в секунду для сильной атаки
    [Range(0f, 1f)][SerializeField] private float strongAttackChance = 0.3f; // Шанс использовать сильную атаку при готовности

    // Время последней атаки
    private float lastNormalAttackTime;
    private float lastStrongAttackTime;

    // Компоненты
    private HealthSystem bossHealth;
    private Rigidbody bossRigidbody;
    private NavMeshAgent navMeshAgent;
    private Animator bossAnimator;

    // Имена параметров аниматора
    [Header("Animator Parameters")]
    [SerializeField] private string isWalkingBoolName = "IsWalking";
    [SerializeField] private string normalAttackTriggerName = "Attack"; // Имя триггера для обычной атаки
    [SerializeField] private string strongAttackTriggerName = "StrongAttack"; // Имя триггера для сильной атаки
    [SerializeField] private string deathTriggerName = "Death"; // Имя триггера смерти (используется HealthSystem)

    private GameObject player;

    // Состояния босса
    private enum BossState
    {
        Idle,           // Покой
        Chase,          // Преследование (Агрессия)
        Attacking,      // Обычная атака
        StrongAttacking,// Сильная атака
        Dead            // Смерть
    }

    private BossState currentState;

    private void Awake()
    {
        bossHealth = GetComponent<HealthSystem>();
        if (bossHealth == null) Debug.LogError("BossAI requires a HealthSystem component.");

        bossRigidbody = GetComponent<Rigidbody>();
        if (bossRigidbody == null) Debug.LogWarning("BossAI does not have a Rigidbody. Death constraint won't apply.");

        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent == null) Debug.LogError("BossAI requires a NavMeshAgent component.");

        bossAnimator = GetComponent<Animator>();
        if (bossAnimator == null) Debug.LogError("BossAI requires an Animator component.");

        // Ищем игрока по тегу. Убедись, что у игрока есть тэг "Player"!
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("BossAI cannot find player with tag 'Player'. Boss will remain idle.");
            // Не возвращаемся, позволяем остальным компонентам инициализироваться
        }

        // Настраиваем NavMeshAgent
        if (navMeshAgent != null)
        {
            navMeshAgent.speed = bossSpeed;
            navMeshAgent.stoppingDistance = attackStoppingDistance;
            navMeshAgent.updateRotation = true; // Позволяем NavMeshAgent вращать объект
            navMeshAgent.updatePosition = true;
        }

        // Устанавливаем начальное состояние
        SetState(BossState.Idle);
    }

    void Update()
    {
        // Проверяем состояние смерти первым делом
        if (bossHealth != null && bossHealth.IsDead())
        {
            SetState(BossState.Dead);
            return; // Если босс мертв, ничего больше не делаем
        }

        // Если нет игрока или NavMeshAgent, не двигаемся
        if (player == null || navMeshAgent == null)
        {
            // Если игрок был потерян, переходим в покой
            if (currentState != BossState.Idle)
            {
                SetState(BossState.Idle);
            }
            return;
        }

        // Логика в зависимости от текущего состояния
        switch (currentState)
        {
            case BossState.Idle:
                UpdateIdleState();
                break;
            case BossState.Chase:
                UpdateChaseState();
                break;
            case BossState.Attacking:
                UpdateAttackingState();
                break;
            case BossState.StrongAttacking:
                UpdateStrongAttackingState();
                break;
            case BossState.Dead:
                // В состоянии смерти Update ничего не делает
                break;
        }
    }

    // Метод для смены состояния
    private void SetState(BossState newState)
    {
        if (currentState == newState) return; // Избегаем повторной установки того же состояния

        // Логика выхода из текущего состояния (если нужна)
        ExitState(currentState);

        // Устанавливаем новое состояние
        currentState = newState;

        // Логика входа в новое состояние
        EnterState(newState);
    }

    // Методы входа в состояние
    private void EnterState(BossState state)
    {
        //Debug.Log($"Entering State: {state}"); // Отладочное сообщение

        switch (state)
        {
            case BossState.Idle:
                if (navMeshAgent != null) navMeshAgent.isStopped = true;
                if (bossAnimator != null) bossAnimator.SetBool(isWalkingBoolName, false);
                // Возможно, проиграть анимацию покоя (если она не цикл по умолчанию)
                break;

            case BossState.Chase:
                if (navMeshAgent != null)
                {
                    navMeshAgent.isStopped = false;
                    // Destination будет устанавливаться в UpdateChaseState
                }
                if (bossAnimator != null) bossAnimator.SetBool(isWalkingBoolName, true);
                break;

            case BossState.Attacking:
                if (navMeshAgent != null) navMeshAgent.isStopped = true;
                if (bossAnimator != null) bossAnimator.SetBool(isWalkingBoolName, false);
                LookAtPlayer(); // Поворачиваемся к игроку перед атакой
                if (bossAnimator != null && !string.IsNullOrEmpty(normalAttackTriggerName))
                {
                    bossAnimator.SetTrigger(normalAttackTriggerName);
                }
                lastNormalAttackTime = Time.time;
                // Переход из этого состояния произойдет по событию анимации или таймеру
                break;

            case BossState.StrongAttacking:
                if (navMeshAgent != null) navMeshAgent.isStopped = true;
                if (bossAnimator != null) bossAnimator.SetBool(isWalkingBoolName, false);
                LookAtPlayer(); // Поворачиваемся к игроку перед атакой
                if (bossAnimator != null && !string.IsNullOrEmpty(strongAttackTriggerName))
                {
                    bossAnimator.SetTrigger(strongAttackTriggerName);
                }
                lastStrongAttackTime = Time.time;
                // Переход из этого состояния произойдет по событию анимации или таймеру
                break;

            case BossState.Dead:
                if (navMeshAgent != null) navMeshAgent.isStopped = true;
                if (bossRigidbody != null) bossRigidbody.constraints = RigidbodyConstraints.FreezeAll;
                // Анимация смерти запускается HealthSystem
                this.enabled = false; // Отключаем скрипт AI после смерти
                break;
        }
    }

    // Методы выхода из состояния (сейчас в основном обнуляют действия)
    private void ExitState(BossState state)
    {
        switch (state)
        {
            case BossState.Chase:
                if (navMeshAgent != null) navMeshAgent.ResetPath(); // Останавливаем преследование
                break;
                // Для состояний атаки выход происходит по завершению анимации (через события)
        }
    }

    // Методы обновления состояний (вызываются в Update())
    private void UpdateIdleState()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (distanceToPlayer < agroRadius)
        {
            SetState(BossState.Chase); // Игрок вошел в радиус агрессии
        }
    }

    private void UpdateChaseState()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (distanceToPlayer >= agroRadius)
        {
            SetState(BossState.Idle); // Игрок вышел из радиуса агрессии (или был потерян)
            return;
        }

        if (distanceToPlayer <= attackStoppingDistance)
        {
            // Остановились, чтобы атаковать
            if (navMeshAgent != null) navMeshAgent.isStopped = true;
            if (bossAnimator != null) bossAnimator.SetBool(isWalkingBoolName, false);

            // Логика выбора атаки
            bool canNormalAttack = (Time.time >= lastNormalAttackTime + (1f / normalAttackRate));
            bool canStrongAttack = (Time.time >= lastStrongAttackTime + (1f / strongAttackRate));

            if (canStrongAttack && Random.value < strongAttackChance)
            {
                SetState(BossState.StrongAttacking);
            }
            else if (canNormalAttack)
            {
                SetState(BossState.Attacking);
            }
            // Если ни одна атака не готова, остаемся в Chase, но стоим и ждем
        }
        else
        {
            // Преследуем игрока
            if (navMeshAgent != null && navMeshAgent.destination != player.transform.position)
            {
                if (navMeshAgent.isStopped) navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(player.transform.position);
            }
            if (bossAnimator != null) bossAnimator.SetBool(isWalkingBoolName, true);
        }
    }

    private void UpdateAttackingState()
    {
        // В этом состоянии мы просто ждем завершения анимации атаки.
        // Переход обратно в Chase будет вызван из события анимации.
        // Можно добавить таймер на случай, если событие не сработает, но события лучше.
    }

    private void UpdateStrongAttackingState()
    {
        // В этом состоянии мы просто ждем завершения анимации сильной атаки.
        // Переход обратно в Chase будет вызван из события анимации.
    }

    // Вспомогательные методы

    // Метод для поворота к игроку
    private void LookAtPlayer()
    {
        if (player == null) return;

        Vector3 lookPos = player.transform.position - transform.position;
        lookPos.y = 0; // Остаемся на одной плоскости
        if (lookPos == Vector3.zero) return; // Избегаем ошибки, если находимся в той же точке

        Quaternion targetRotation = Quaternion.LookRotation(lookPos);
        // Плавный поворот
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        // Мгновенный поворот (можно использовать, если плавный поворот не нужен перед атакой)
        // transform.rotation = targetRotation;
    }

    // Методы, вызываемые из Animation Events
    // В анимации обычной атаки добавь событие (Animation Event) в конце анимации,
    // которое вызывает эту функцию.
    public void OnNormalAttackAnimationEnd()
    {
        //Debug.Log("Normal Attack Animation End"); // Отладочное сообщение
        // После атаки возвращаемся в состояние преследования, чтобы ИИ решил, что делать дальше
        SetState(BossState.Chase);
    }

    // В анимации сильной атаки добавь событие (Animation Event) в конце анимации,
    // которое вызывает эту функцию.
    public void OnStrongAttackAnimationEnd()
    {
        //Debug.Log("Strong Attack Animation End"); // Отладочное сообщение
        // После атаки возвращаемся в состояние преследования
        SetState(BossState.Chase);
    }

    // Этот метод может быть вызван извне (например, HealthSystem) или самим AI (в данном случае, DeathState его вызывает через disable)
    // Но HealthSystem уже вызывает Death триггер, и мы отключаем скрипт в EnterState(BossState.Dead)
    // Так что отдельный OnBossDeath не нужен, его логика интегрирована в EnterState(BossState.Dead).

    // Метод для деспавна босса (если нужен)
    public void DespawnBoss()
    {
        Destroy(gameObject);
    }
}

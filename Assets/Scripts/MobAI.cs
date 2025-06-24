// MobAI.cs
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(HealthSystem))]
[RequireComponent(typeof(NavMeshAgent))]
public class MobAI : MonoBehaviour
{
    private enum MobState
    {
        Idle,
        Chasing,
        Attacking,
        Fleeing,
        Stunned,
        Dead
    }

    [Header("AI State")]
    private MobState currentState = MobState.Idle;
    private float stunEndTime = 0f;

    [Header("Movement & Combat")]
    [SerializeField] private float mobSpeed = 3.5f;
    [SerializeField] private float mobFleeSpeed = 5f;
    [SerializeField] private float mobStoppingDistance = 2f;
    [SerializeField] private float mobAgroRadius = 10f;
    [SerializeField] private float mobAttackRate = 1f;
    [SerializeField] private float stunDuration = 1.5f;

    [Header("Peaceful Mode Settings")]
    [SerializeField] private float fleeHealthPercentage = 0.3f; // Убегать при < 30% здоровья
    [SerializeField] private float fleeDistance = 15f;      // На какое расстояние пытаться убежать

    private HealthSystem mobHealth;
    private float lastAttackTime;
    private bool wasPreviouslyAgroSoundPlayed = false;

    private Rigidbody mobRigidbody;
    private NavMeshAgent navMeshAgent;

    private ZombieAudio zombieAudio;
    private Animator mobAnimator;

    [Header("Animator Parameters")]
    [SerializeField] private string isWalkingBoolName = "IsWalking";
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private string isFleeingBoolName = "IsFleeing";
    [SerializeField] private string stunTriggerName = "Stun";

    private Transform playerTransform; // Убрал GameObject player, используем только Transform

    private void Awake()
    {
        mobHealth = GetComponent<HealthSystem>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        mobRigidbody = GetComponent<Rigidbody>();

        zombieAudio = GetComponent<ZombieAudio>();
        mobAnimator = GetComponent<Animator>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("Игрок ('Player' tag) не найден. Моб будет бездействовать: " + gameObject.name);
            enabled = false;
            return;
        }
        playerTransform = playerObj.transform;

        if (navMeshAgent != null)
        {
            navMeshAgent.speed = mobSpeed;
            navMeshAgent.stoppingDistance = mobStoppingDistance;
        }
    }

    void Update()
    {
        if (playerTransform == null || currentState == MobState.Dead) return;

        if (mobHealth.IsDead())
        {
            if (currentState != MobState.Dead) SwitchState(MobState.Dead);
            return;
        }

        if (currentState == MobState.Stunned)
        {
            if (Time.time >= stunEndTime)
            {
                SwitchState(MobState.Idle);
            }
            else
            {
                return; // В стане ничего не делаем
            }
        }

        // Логика выбора поведения
        if (PeaceModeManager.IsPeacefulModeActive)
        {
            HandlePeacefulModeBehavior();
        }
        else
        {
            HandleAggressiveModeBehavior();
        }

        UpdateAnimator();
    }

    void HandlePeacefulModeBehavior()
    {
        // Проверяем, нужно ли убегать, только если еще не убегаем или не оглушены
        if (currentState != MobState.Fleeing && currentState != MobState.Stunned)
        {
            float currentHealth = mobHealth.GetHealth();
            float maxHealth = mobHealth.GetMaxHealth();
            if (maxHealth > 0) // Избегаем деления на ноль, если maxHealth не инициализирован
            {
                float currentHealthRatio = currentHealth / maxHealth;
                // Debug.Log($"{gameObject.name} Health Ratio: {currentHealthRatio}, Flee Threshold: {fleeHealthPercentage}, Current State: {currentState}");
                if (currentHealthRatio <= fleeHealthPercentage)
                {
                    SwitchState(MobState.Fleeing);
                    return; // После переключения в Fleeing, выходим, чтобы ProcessFleeing сработал в следующем цикле Update в switch-case
                }
            }
        }

        // Выполняем действия в зависимости от текущего состояния в мирном режиме
        switch (currentState)
        {
            case MobState.Fleeing:
                ProcessFleeing();
                break;
            case MobState.Stunned:
                // Логика стана уже обработана в Update
                break;
            default: // Idle
                ProcessIdlePeaceful();
                break;
        }
    }

    void HandleAggressiveModeBehavior()
    {
        // Если оглушен, не делаем ничего агрессивного
        if (currentState == MobState.Stunned) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer < mobAgroRadius)
        {
            if (!wasPreviouslyAgroSoundPlayed && zombieAudio != null)
            {
                zombieAudio.PlayAgroSound();
                wasPreviouslyAgroSoundPlayed = true;
            }

            if (distanceToPlayer > navMeshAgent.stoppingDistance)
            {
                SwitchState(MobState.Chasing);
            }
            else
            {
                SwitchState(MobState.Attacking);
            }
        }
        else
        {
            if (wasPreviouslyAgroSoundPlayed)
            {
                wasPreviouslyAgroSoundPlayed = false;
            }
            SwitchState(MobState.Idle);
        }

        // Выполняем действия в зависимости от текущего состояния в агрессивном режиме
        // (Этот switch может быть избыточен, если SwitchState уже все настроил)
        switch (currentState)
        {
            case MobState.Chasing:
                ProcessChasing();
                break;
            case MobState.Attacking:
                ProcessAttacking();
                break;
            case MobState.Idle:
                ProcessIdleAggressive();
                break;
        }
    }

    void SwitchState(MobState newState)
    {
        if (currentState == newState && newState != MobState.Stunned) return;

        // Debug.Log($"{gameObject.name} switching from {currentState} to {newState}");
        currentState = newState;

        // Общие настройки для состояний, где моб не двигается или останавливается
        if (navMeshAgent.isOnNavMesh)
        {
            if (newState == MobState.Idle || newState == MobState.Attacking || newState == MobState.Stunned)
            {
                if (navMeshAgent.hasPath) navMeshAgent.ResetPath();
                navMeshAgent.isStopped = true;
            }
            else // Chasing, Fleeing
            {
                navMeshAgent.isStopped = false;
            }
        }


        switch (currentState)
        {
            case MobState.Idle:
                navMeshAgent.speed = mobSpeed;
                break;
            case MobState.Chasing:
                navMeshAgent.speed = mobSpeed;
                break;
            case MobState.Attacking:
                // navMeshAgent.isStopped = true; // Уже установлено
                break;
            case MobState.Fleeing:
                navMeshAgent.speed = mobFleeSpeed;
                InitiateFleeing(); // Начинаем поиск пути для побега
                break;
            case MobState.Stunned:
                stunEndTime = Time.time + stunDuration;
                if (mobAnimator != null && !string.IsNullOrEmpty(stunTriggerName)) mobAnimator.SetTrigger(stunTriggerName);
                if (zombieAudio != null) zombieAudio.PlayStunSound();
                break;
            case MobState.Dead:
                OnMobAIDeath();
                break;
        }
    }

    void ProcessIdlePeaceful() { /* Моб стоит. NavMeshAgent уже остановлен. */ }
    void ProcessIdleAggressive() { /* Моб стоит. NavMeshAgent уже остановлен. */ }

    void ProcessChasing()
    {
        if (playerTransform == null || !navMeshAgent.isOnNavMesh || navMeshAgent.isStopped) return;
        if (navMeshAgent.destination != playerTransform.position)
            navMeshAgent.SetDestination(playerTransform.position);
    }

    void ProcessAttacking()
    {
        if (playerTransform == null || !navMeshAgent.isOnNavMesh) return; // Хотя агент должен быть isStopped=true

        transform.LookAt(playerTransform.position); // Простой поворот

        if (Time.time >= lastAttackTime + (1f / mobAttackRate))
        {
            PerformAttack();
            lastAttackTime = Time.time;
        }
    }

    void InitiateFleeing()
    {
        if (playerTransform == null || !navMeshAgent.isOnNavMesh || !navMeshAgent.enabled)
        {
            SwitchState(MobState.Idle); // Некуда или не от кого убегать, или агент выключен
            return;
        }
        navMeshAgent.isStopped = false; // Убедимся, что агент может двигаться

        Vector3 fleeDirection = (transform.position - playerTransform.position).normalized;
        Vector3 fleeTargetPosition = transform.position + fleeDirection * fleeDistance;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(fleeTargetPosition, out hit, fleeDistance, NavMesh.AllAreas))
        {
            navMeshAgent.SetDestination(hit.position);
            // Debug.Log($"{gameObject.name} fleeing to {hit.position}");
        }
        else
        {
            Vector3 randomDir = Random.insideUnitSphere * fleeDistance;
            randomDir.y = 0;
            if (NavMesh.SamplePosition(transform.position + randomDir, out hit, fleeDistance, NavMesh.AllAreas))
            {
                navMeshAgent.SetDestination(hit.position);
                // Debug.Log($"{gameObject.name} fleeing to random pos {hit.position}");
            }
            else
            {
                Debug.LogWarning(gameObject.name + " не может найти точку для побега. Остается Idle.");
                SwitchState(MobState.Idle);
            }
        }
        // Можно добавить звук паники/убегания здесь, если он отличается от stun
        // if (zombieAudio != null) zombieAudio.PlayPanicSound();
    }

    void ProcessFleeing()
    {
        if (playerTransform == null || !navMeshAgent.isOnNavMesh || !navMeshAgent.enabled) return;

        // Если агент достиг цели или не может двигаться дальше (застрял)
        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance * 1.1f) // Немного увеличим порог
        {
            // Debug.Log($"{gameObject.name} reached flee destination or cannot move further.");
            float currentHealth = mobHealth.GetHealth();
            float maxHealth = mobHealth.GetMaxHealth();
            float currentHealthRatio = (maxHealth > 0) ? currentHealth / maxHealth : 1f;
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            // Если здоровье восстановилось (маловероятно без механики) или игрок очень далеко
            if (currentHealthRatio > fleeHealthPercentage || distanceToPlayer > fleeDistance * 1.5f)
            {
                // Debug.Log($"{gameObject.name} stopping flee. HealthRatio: {currentHealthRatio}, DistToPlayer: {distanceToPlayer}");
                SwitchState(MobState.Idle); // Вернуться в мирное состояние Idle
            }
            else // Здоровье все еще низкое И игрок все еще относительно близко
            {
                // Debug.Log($"{gameObject.name} health still low and player near. Finding new flee point.");
                InitiateFleeing(); // Найти новую точку для побега
            }
        }
        else if (navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid || navMeshAgent.pathStatus == NavMeshPathStatus.PathPartial)
        {
            // Если путь стал невалидным, попробовать найти новый
            // Debug.LogWarning($"{gameObject.name} flee path became invalid/partial. Finding new flee point.");
            InitiateFleeing();
        }
    }

    private void PerformAttack()
    {
        if (mobAnimator != null) mobAnimator.SetTrigger(attackTriggerName);
        if (zombieAudio != null) zombieAudio.PlayAttackSound();
        Debug.Log(gameObject.name + " атакует игрока!");
        // Добавьте сюда логику нанесения урона игроку, если она не в анимации
    }

    private void OnMobAIDeath()
    {
        if (navMeshAgent.isOnNavMesh && navMeshAgent.enabled)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.enabled = false;
        }
        if (mobRigidbody != null)
        {
            mobRigidbody.isKinematic = true; // Чтобы Rigidbody не конфликтовал после отключения NavMeshAgent
            mobRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }
        // this.enabled = false; // Отключаем AI скрипт
    }

    private void UpdateAnimator()
    {
        if (mobAnimator == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh) return;

        bool isMoving = navMeshAgent.velocity.magnitude > 0.1f &&
                        (currentState == MobState.Chasing || currentState == MobState.Fleeing);
        mobAnimator.SetBool(isWalkingBoolName, isMoving);

        if (!string.IsNullOrEmpty(isFleeingBoolName))
        {
            mobAnimator.SetBool(isFleeingBoolName, currentState == MobState.Fleeing);
        }
    }

    public void TakeStun()
    {
        if (currentState == MobState.Dead) return;
        SwitchState(MobState.Stunned);
        // Debug.Log(gameObject.name + " получил оглушение (TakeStun).");
    }

    public void DespawnMob()
    {
        Destroy(gameObject);
    }
}
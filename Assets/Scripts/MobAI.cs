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
    [SerializeField] private float fleeHealthPercentage = 0.3f; // ������� ��� < 30% ��������
    [SerializeField] private float fleeDistance = 15f;      // �� ����� ���������� �������� �������

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

    private Transform playerTransform; // ����� GameObject player, ���������� ������ Transform

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
            Debug.LogError("����� ('Player' tag) �� ������. ��� ����� ��������������: " + gameObject.name);
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
                return; // � ����� ������ �� ������
            }
        }

        // ������ ������ ���������
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
        // ���������, ����� �� �������, ������ ���� ��� �� ������� ��� �� ��������
        if (currentState != MobState.Fleeing && currentState != MobState.Stunned)
        {
            float currentHealth = mobHealth.GetHealth();
            float maxHealth = mobHealth.GetMaxHealth();
            if (maxHealth > 0) // �������� ������� �� ����, ���� maxHealth �� ���������������
            {
                float currentHealthRatio = currentHealth / maxHealth;
                // Debug.Log($"{gameObject.name} Health Ratio: {currentHealthRatio}, Flee Threshold: {fleeHealthPercentage}, Current State: {currentState}");
                if (currentHealthRatio <= fleeHealthPercentage)
                {
                    SwitchState(MobState.Fleeing);
                    return; // ����� ������������ � Fleeing, �������, ����� ProcessFleeing �������� � ��������� ����� Update � switch-case
                }
            }
        }

        // ��������� �������� � ����������� �� �������� ��������� � ������ ������
        switch (currentState)
        {
            case MobState.Fleeing:
                ProcessFleeing();
                break;
            case MobState.Stunned:
                // ������ ����� ��� ���������� � Update
                break;
            default: // Idle
                ProcessIdlePeaceful();
                break;
        }
    }

    void HandleAggressiveModeBehavior()
    {
        // ���� �������, �� ������ ������ ������������
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

        // ��������� �������� � ����������� �� �������� ��������� � ����������� ������
        // (���� switch ����� ���� ���������, ���� SwitchState ��� ��� ��������)
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

        // ����� ��������� ��� ���������, ��� ��� �� ��������� ��� ���������������
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
                // navMeshAgent.isStopped = true; // ��� �����������
                break;
            case MobState.Fleeing:
                navMeshAgent.speed = mobFleeSpeed;
                InitiateFleeing(); // �������� ����� ���� ��� ������
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

    void ProcessIdlePeaceful() { /* ��� �����. NavMeshAgent ��� ����������. */ }
    void ProcessIdleAggressive() { /* ��� �����. NavMeshAgent ��� ����������. */ }

    void ProcessChasing()
    {
        if (playerTransform == null || !navMeshAgent.isOnNavMesh || navMeshAgent.isStopped) return;
        if (navMeshAgent.destination != playerTransform.position)
            navMeshAgent.SetDestination(playerTransform.position);
    }

    void ProcessAttacking()
    {
        if (playerTransform == null || !navMeshAgent.isOnNavMesh) return; // ���� ����� ������ ���� isStopped=true

        transform.LookAt(playerTransform.position); // ������� �������

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
            SwitchState(MobState.Idle); // ������ ��� �� �� ���� �������, ��� ����� ��������
            return;
        }
        navMeshAgent.isStopped = false; // ��������, ��� ����� ����� ���������

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
                Debug.LogWarning(gameObject.name + " �� ����� ����� ����� ��� ������. �������� Idle.");
                SwitchState(MobState.Idle);
            }
        }
        // ����� �������� ���� ������/�������� �����, ���� �� ���������� �� stun
        // if (zombieAudio != null) zombieAudio.PlayPanicSound();
    }

    void ProcessFleeing()
    {
        if (playerTransform == null || !navMeshAgent.isOnNavMesh || !navMeshAgent.enabled) return;

        // ���� ����� ������ ���� ��� �� ����� ��������� ������ (�������)
        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance * 1.1f) // ������� �������� �����
        {
            // Debug.Log($"{gameObject.name} reached flee destination or cannot move further.");
            float currentHealth = mobHealth.GetHealth();
            float maxHealth = mobHealth.GetMaxHealth();
            float currentHealthRatio = (maxHealth > 0) ? currentHealth / maxHealth : 1f;
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            // ���� �������� �������������� (������������ ��� ��������) ��� ����� ����� ������
            if (currentHealthRatio > fleeHealthPercentage || distanceToPlayer > fleeDistance * 1.5f)
            {
                // Debug.Log($"{gameObject.name} stopping flee. HealthRatio: {currentHealthRatio}, DistToPlayer: {distanceToPlayer}");
                SwitchState(MobState.Idle); // ��������� � ������ ��������� Idle
            }
            else // �������� ��� ��� ������ � ����� ��� ��� ������������ ������
            {
                // Debug.Log($"{gameObject.name} health still low and player near. Finding new flee point.");
                InitiateFleeing(); // ����� ����� ����� ��� ������
            }
        }
        else if (navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid || navMeshAgent.pathStatus == NavMeshPathStatus.PathPartial)
        {
            // ���� ���� ���� ����������, ����������� ����� �����
            // Debug.LogWarning($"{gameObject.name} flee path became invalid/partial. Finding new flee point.");
            InitiateFleeing();
        }
    }

    private void PerformAttack()
    {
        if (mobAnimator != null) mobAnimator.SetTrigger(attackTriggerName);
        if (zombieAudio != null) zombieAudio.PlayAttackSound();
        Debug.Log(gameObject.name + " ������� ������!");
        // �������� ���� ������ ��������� ����� ������, ���� ��� �� � ��������
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
            mobRigidbody.isKinematic = true; // ����� Rigidbody �� ������������ ����� ���������� NavMeshAgent
            mobRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }
        // this.enabled = false; // ��������� AI ������
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
        // Debug.Log(gameObject.name + " ������� ��������� (TakeStun).");
    }

    public void DespawnMob()
    {
        Destroy(gameObject);
    }
}
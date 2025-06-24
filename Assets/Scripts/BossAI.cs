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
                Debug.LogError("����� ('Player' tag) �� ������! ���� �� ����� ���������������.");
                enabled = false; return;
            }
        }
    }

    void Start()
    {
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent �� ������ �� �����. �������� �� ����� ��������.");
            enabled = false; return;
        }
        SwitchToPatrolState(); // �������� � ��������������
    }

    void SwitchToPatrolState() // ��������������� ����� ��� �������
    {
        if (patrolPoints.Length > 0 && agent.isOnNavMesh)
        {
            agent.speed = patrolSpeed;
            if (agent.isOnNavMesh) agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            SwitchState(BossState.Patrolling);
        }
        else
        {
            Debug.LogWarning("����� �������������� �� ������ ��� NavMeshAgent �� �������. ���� ����� ������ �� ����� �� ����������.");
            SwitchState(BossState.Patrolling); // �������� � �������, �� ������ �� ������
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

        // ���������� �������� �������� ��������� ������ ���� �� �����
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
            // ���� �������������, ������: ��������� ��� ������������
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= attackRange)
            {
                if (currentState != BossState.Attacking) SwitchState(BossState.Attacking);
            }
            else // ����� ������ ���� �����, �� �� �������������� -> ����������
            {
                if (currentState != BossState.Chasing) SwitchState(BossState.Chasing);
            }
        }
        else // �� ������������� ������
        {
            // ������ �������������
            if (currentState != BossState.Patrolling)
            {
                SwitchState(BossState.Patrolling);
            }
        }
    }


    void SwitchState(BossState newState)
    {
        if (currentState == newState && newState != BossState.Patrolling) return; // ��������� "��������" �������, ���� ��� �����������

        // Debug.Log($"Boss: {gameObject.name} switching from {currentState} to {newState}");
        currentState = newState;

        if (agent == null || !agent.isOnNavMesh && newState != BossState.Dead) // ��� Dead ����� ����� ���� ��� ��������
        {
            if (newState != BossState.Dead) Debug.LogWarning("Boss NavMeshAgent is null or not on NavMesh when trying to switch state to " + newState);
            return;
        }


        switch (currentState)
        {
            case BossState.Patrolling:
                agent.speed = patrolSpeed;
                agent.isStopped = false; // ��������, ��� ����� ����� ���������
                if (patrolPoints.Length > 0)
                {
                    if (agent.isOnNavMesh) agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                }
                else
                {
                    if (agent.isOnNavMesh) agent.ResetPath(); // ������ �� �����
                }
                break;
            case BossState.Chasing:
                agent.speed = chaseSpeed;
                agent.isStopped = false;
                break;
            case BossState.Attacking:
                if (agent.isOnNavMesh) // ������ ���� ����� �� NavMesh
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
        if (agent.destination != playerTransform.position) // ��������� ���� ������ ���� �� ���������
            agent.SetDestination(playerTransform.position);
    }

    void HandleAttacking()
    {
        if (agent == null || playerTransform == null) return; // ����� ����� ���� �� �� NavMesh, ���� �� isStopped = true

        transform.LookAt(playerTransform.position); // ������� ������� � ������

        if (currentAttackCooldown <= 0f)
        {
            PerformAttack();
            currentAttackCooldown = attackCooldown;
        }
    }

    void PerformAttack()
    {
        Debug.Log(gameObject.name + " ������� ������!");
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

        Debug.Log($"{gameObject.name} ��� ��������� � ��������� {amount} �����. ������� ��������: {healthSystem.GetHealth()}");

        if (!isProvokedByDamage)
        {
            isProvokedByDamage = true;
            Debug.Log(gameObject.name + " ������������� ������!");
            // ���������� ������, ��� ������, � �� ���� ���������� Update
            DecideState();
        }

        // �������� �� ������ (���� HealthSystem ��� ��� ������ ��� ��������)
        // HealthSystem ��� ������� �������� � isDead. ��� ����� ������ �������� ��������� AI.
        if (healthSystem.IsDead() && currentState != BossState.Dead)
        {
            SwitchState(BossState.Dead);
        }
    }

    private void OnBossAIDeath()
    {
        Debug.Log("BossAI: " + gameObject.name + " �������� (AI cleanup)!");
        if (agent != null && agent.isOnNavMesh) // ���������, ���������� �� ��� �����
        {
            agent.isStopped = true;
            agent.enabled = false; // ��������� ���������
        }
        // gameObject.SetActive(false); // ��� Destroy(gameObject, delay);
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
        if (playerTransform != null) Gizmos.DrawWireSphere(transform.position, attackRange); // ������, ������ ���� ���� �����

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
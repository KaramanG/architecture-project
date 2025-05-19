using UnityEngine;
using UnityEngine.AI; // ���� ����������� NavMeshAgent

public class BossAI : MonoBehaviour
{
    public enum BossState
    {
        Patrolling,
        Chasing,
        Attacking // �����������, ���� ����� - ��������� ���������
    }

    [Header("���������")]
    public BossState currentState = BossState.Patrolling;

    [Header("������")]
    public Transform playerTransform;
    private NavMeshAgent agent; // ���� ������������ NavMesh
    private Animator animator;  // ���� ���� ��������

    [Header("��������������")]
    public float health = 100f;
    public float detectionRadius = 20f; // �� ������ ������, ���� �������� ������ �� �������� �����
    public float attackRange = 2f;
    public float attackCooldown = 2f;
    private float currentAttackCooldown = 0f;

    [Header("��������������")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 2f;
    private int currentPatrolIndex = 0;
    public float waypointProximityThreshold = 1f; // ��������� ������ ����� ������� � ����� �������

    [Header("�������������")]
    public float chaseSpeed = 4f;

    // ����, ��� �� ���� �������������
    private bool isProvoked = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>(); // �������� Animator, ���� �� ����

        // ������� ����� ������ �� ����, ���� �� �������� �������
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                Debug.LogError("����� �� ������! ���������, ��� � ������ ���� ��� 'Player' ��� ��������� ��� �������.");
                enabled = false; // ��������� ������, ���� ������ ���
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
            Debug.LogWarning("NavMeshAgent �� ������ �� �����. �������� �� ����� ��������.");
            // ����� ����������� ������� �������� ��� NavMesh, ���� �����
        }
        else if (patrolPoints.Length == 0)
        {
            Debug.LogWarning("����� �������������� �� ������. ���� ����� ������ �� ����� �� ����������.");
            currentState = BossState.Patrolling; // �������� � �������, �� ������ �� ������
        }
    }

    void Update()
    {
        if (playerTransform == null) return; // ���� ����� ����� ��� �� ��� ������

        if (currentAttackCooldown > 0)
        {
            currentAttackCooldown -= Time.deltaTime;
        }

        switch (currentState)
        {
            case BossState.Patrolling:
                HandlePatrolling();
                // ���� isProvoked ���� true (�� TakeDamage), �������������
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

        // ���������� �������� (������)
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
        // � ���� ��������� ���� �� ���� ������ �������
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
        // �����������: ���� ����� ������ ������� ������, ����� ��������� � ��������������
        // if (distanceToPlayer > someMaxChaseDistance) { SwitchToPatrolState(); isProvoked = false; }
    }

    void HandleAttacking()
    {
        if (agent == null || playerTransform == null) return;

        agent.SetDestination(transform.position); // ������������ ��� �����
        // ������� � ������
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(directionToPlayer.x, 0, directionToPlayer.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);


        if (currentAttackCooldown <= 0f)
        {
            PerformAttack();
            currentAttackCooldown = attackCooldown;
        }

        // ��������, �� ����� �� ����� �� ���� �����
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer > attackRange)
        {
            SwitchToChaseState();
        }
    }

    void PerformAttack()
    {
        Debug.Log("���� ������� ������!");
        if (animator != null)
        {
            animator.SetTrigger("Attack"); // ��������������, ��� ���� ������� "Attack" � ���������
        }
        // ����� ������ ��������� ����� ������
        // ��������: playerTransform.GetComponent<PlayerHealth>().TakeDamage(bossDamage);
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        Debug.Log($"���� ������� {amount} �����. ��������: {health}");

        if (!isProvoked)
        {
            isProvoked = true;
            Debug.Log("���� �������������!");
            // ���������� ������������� �� �������������, ���� ���� ��� � �������
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
        Debug.Log("���� ��������!");
        if (animator != null)
        {
            animator.SetTrigger("Die"); // ��������������, ��� ���� ������� "Die"
        }
        // ��������� ��������� AI ��� ���������� ������
        // agent.enabled = false; // ���� ���� NavMeshAgent
        // this.enabled = false;
        Destroy(gameObject, 3f); // ���������� ����� 3 �������, ����� �������� ������ �����������
    }

    void SwitchToChaseState()
    {
        Debug.Log("����: ������� � ��������� �������������");
        currentState = BossState.Chasing;
        if (agent != null) agent.speed = chaseSpeed;
        if (animator != null) animator.SetBool("IsChasing", true); // ������ ������������� ���������
    }

    void SwitchToAttackState()
    {
        Debug.Log("����: ������� � ��������� �����");
        currentState = BossState.Attacking;
        if (animator != null) animator.SetBool("IsChasing", false); // ��������, ��������� �������� ����
    }

    // ���� ����� ����� ��������� � ������� (��������, ���� ����� ������ ������ � ���� "�������" ���)
    // void SwitchToPatrolState()
    // {
    //     Debug.Log("����: ������� � ��������� ��������������");
    //     currentState = BossState.Patrolling;
    //     isProvoked = false; // ���������� ����������
    //     if (agent != null && patrolPoints.Length > 0)
    //     {
    //         agent.speed = patrolSpeed;
    //         agent.SetDestination(patrolPoints[currentPatrolIndex].position);
    //     }
    //     if (animator != null) animator.SetBool("IsChasing", false);
    // }

    // ��������� �������� � ��������� ��� ��������
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
                    else if (patrolPoints.Length > 1 && patrolPoints[0] != null) // �������� ����
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[0].position);
                    }
                }
            }
        }
    }
}
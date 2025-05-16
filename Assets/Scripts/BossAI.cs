using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BossAI : MonoBehaviour
{
    // ��������� �����
    [Header("Boss Parameters")]
    [SerializeField] private float bossSpeed;
    [SerializeField] private float attackStoppingDistance; // ���������, �� ������� ���� ��������������� ��� �����
    [SerializeField] private float agroRadius; // ������, � ������� ���� ���������� �����������

    [Header("Attack Parameters")]
    [SerializeField] private float normalAttackRate = 1f; // ���� � ������� ��� ������� �����
    [SerializeField] private float strongAttackRate = 0.5f; // ���� � ������� ��� ������� �����
    [Range(0f, 1f)][SerializeField] private float strongAttackChance = 0.3f; // ���� ������������ ������� ����� ��� ����������

    // ����� ��������� �����
    private float lastNormalAttackTime;
    private float lastStrongAttackTime;

    // ����������
    private HealthSystem bossHealth;
    private Rigidbody bossRigidbody;
    private NavMeshAgent navMeshAgent;
    private Animator bossAnimator;

    // ����� ���������� ���������
    [Header("Animator Parameters")]
    [SerializeField] private string isWalkingBoolName = "IsWalking";
    [SerializeField] private string normalAttackTriggerName = "Attack"; // ��� �������� ��� ������� �����
    [SerializeField] private string strongAttackTriggerName = "StrongAttack"; // ��� �������� ��� ������� �����
    [SerializeField] private string deathTriggerName = "Death"; // ��� �������� ������ (������������ HealthSystem)

    private GameObject player;

    // ��������� �����
    private enum BossState
    {
        Idle,           // �����
        Chase,          // ������������� (��������)
        Attacking,      // ������� �����
        StrongAttacking,// ������� �����
        Dead            // ������
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

        // ���� ������ �� ����. �������, ��� � ������ ���� ��� "Player"!
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("BossAI cannot find player with tag 'Player'. Boss will remain idle.");
            // �� ������������, ��������� ��������� ����������� ������������������
        }

        // ����������� NavMeshAgent
        if (navMeshAgent != null)
        {
            navMeshAgent.speed = bossSpeed;
            navMeshAgent.stoppingDistance = attackStoppingDistance;
            navMeshAgent.updateRotation = true; // ��������� NavMeshAgent ������� ������
            navMeshAgent.updatePosition = true;
        }

        // ������������� ��������� ���������
        SetState(BossState.Idle);
    }

    void Update()
    {
        // ��������� ��������� ������ ������ �����
        if (bossHealth != null && bossHealth.IsDead())
        {
            SetState(BossState.Dead);
            return; // ���� ���� �����, ������ ������ �� ������
        }

        // ���� ��� ������ ��� NavMeshAgent, �� ���������
        if (player == null || navMeshAgent == null)
        {
            // ���� ����� ��� �������, ��������� � �����
            if (currentState != BossState.Idle)
            {
                SetState(BossState.Idle);
            }
            return;
        }

        // ������ � ����������� �� �������� ���������
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
                // � ��������� ������ Update ������ �� ������
                break;
        }
    }

    // ����� ��� ����� ���������
    private void SetState(BossState newState)
    {
        if (currentState == newState) return; // �������� ��������� ��������� ���� �� ���������

        // ������ ������ �� �������� ��������� (���� �����)
        ExitState(currentState);

        // ������������� ����� ���������
        currentState = newState;

        // ������ ����� � ����� ���������
        EnterState(newState);
    }

    // ������ ����� � ���������
    private void EnterState(BossState state)
    {
        //Debug.Log($"Entering State: {state}"); // ���������� ���������

        switch (state)
        {
            case BossState.Idle:
                if (navMeshAgent != null) navMeshAgent.isStopped = true;
                if (bossAnimator != null) bossAnimator.SetBool(isWalkingBoolName, false);
                // ��������, ��������� �������� ����� (���� ��� �� ���� �� ���������)
                break;

            case BossState.Chase:
                if (navMeshAgent != null)
                {
                    navMeshAgent.isStopped = false;
                    // Destination ����� ��������������� � UpdateChaseState
                }
                if (bossAnimator != null) bossAnimator.SetBool(isWalkingBoolName, true);
                break;

            case BossState.Attacking:
                if (navMeshAgent != null) navMeshAgent.isStopped = true;
                if (bossAnimator != null) bossAnimator.SetBool(isWalkingBoolName, false);
                LookAtPlayer(); // �������������� � ������ ����� ������
                if (bossAnimator != null && !string.IsNullOrEmpty(normalAttackTriggerName))
                {
                    bossAnimator.SetTrigger(normalAttackTriggerName);
                }
                lastNormalAttackTime = Time.time;
                // ������� �� ����� ��������� ���������� �� ������� �������� ��� �������
                break;

            case BossState.StrongAttacking:
                if (navMeshAgent != null) navMeshAgent.isStopped = true;
                if (bossAnimator != null) bossAnimator.SetBool(isWalkingBoolName, false);
                LookAtPlayer(); // �������������� � ������ ����� ������
                if (bossAnimator != null && !string.IsNullOrEmpty(strongAttackTriggerName))
                {
                    bossAnimator.SetTrigger(strongAttackTriggerName);
                }
                lastStrongAttackTime = Time.time;
                // ������� �� ����� ��������� ���������� �� ������� �������� ��� �������
                break;

            case BossState.Dead:
                if (navMeshAgent != null) navMeshAgent.isStopped = true;
                if (bossRigidbody != null) bossRigidbody.constraints = RigidbodyConstraints.FreezeAll;
                // �������� ������ ����������� HealthSystem
                this.enabled = false; // ��������� ������ AI ����� ������
                break;
        }
    }

    // ������ ������ �� ��������� (������ � �������� �������� ��������)
    private void ExitState(BossState state)
    {
        switch (state)
        {
            case BossState.Chase:
                if (navMeshAgent != null) navMeshAgent.ResetPath(); // ������������� �������������
                break;
                // ��� ��������� ����� ����� ���������� �� ���������� �������� (����� �������)
        }
    }

    // ������ ���������� ��������� (���������� � Update())
    private void UpdateIdleState()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (distanceToPlayer < agroRadius)
        {
            SetState(BossState.Chase); // ����� ����� � ������ ��������
        }
    }

    private void UpdateChaseState()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (distanceToPlayer >= agroRadius)
        {
            SetState(BossState.Idle); // ����� ����� �� ������� �������� (��� ��� �������)
            return;
        }

        if (distanceToPlayer <= attackStoppingDistance)
        {
            // ������������, ����� ���������
            if (navMeshAgent != null) navMeshAgent.isStopped = true;
            if (bossAnimator != null) bossAnimator.SetBool(isWalkingBoolName, false);

            // ������ ������ �����
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
            // ���� �� ���� ����� �� ������, �������� � Chase, �� ����� � ����
        }
        else
        {
            // ���������� ������
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
        // � ���� ��������� �� ������ ���� ���������� �������� �����.
        // ������� ������� � Chase ����� ������ �� ������� ��������.
        // ����� �������� ������ �� ������, ���� ������� �� ���������, �� ������� �����.
    }

    private void UpdateStrongAttackingState()
    {
        // � ���� ��������� �� ������ ���� ���������� �������� ������� �����.
        // ������� ������� � Chase ����� ������ �� ������� ��������.
    }

    // ��������������� ������

    // ����� ��� �������� � ������
    private void LookAtPlayer()
    {
        if (player == null) return;

        Vector3 lookPos = player.transform.position - transform.position;
        lookPos.y = 0; // �������� �� ����� ���������
        if (lookPos == Vector3.zero) return; // �������� ������, ���� ��������� � ��� �� �����

        Quaternion targetRotation = Quaternion.LookRotation(lookPos);
        // ������� �������
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        // ���������� ������� (����� ������������, ���� ������� ������� �� ����� ����� ������)
        // transform.rotation = targetRotation;
    }

    // ������, ���������� �� Animation Events
    // � �������� ������� ����� ������ ������� (Animation Event) � ����� ��������,
    // ������� �������� ��� �������.
    public void OnNormalAttackAnimationEnd()
    {
        //Debug.Log("Normal Attack Animation End"); // ���������� ���������
        // ����� ����� ������������ � ��������� �������������, ����� �� �����, ��� ������ ������
        SetState(BossState.Chase);
    }

    // � �������� ������� ����� ������ ������� (Animation Event) � ����� ��������,
    // ������� �������� ��� �������.
    public void OnStrongAttackAnimationEnd()
    {
        //Debug.Log("Strong Attack Animation End"); // ���������� ���������
        // ����� ����� ������������ � ��������� �������������
        SetState(BossState.Chase);
    }

    // ���� ����� ����� ���� ������ ����� (��������, HealthSystem) ��� ����� AI (� ������ ������, DeathState ��� �������� ����� disable)
    // �� HealthSystem ��� �������� Death �������, � �� ��������� ������ � EnterState(BossState.Dead)
    // ��� ��� ��������� OnBossDeath �� �����, ��� ������ ������������� � EnterState(BossState.Dead).

    // ����� ��� �������� ����� (���� �����)
    public void DespawnBoss()
    {
        Destroy(gameObject);
    }
}

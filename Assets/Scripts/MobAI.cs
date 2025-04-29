using UnityEngine;
using UnityEngine.AI;

public class MobAI : MonoBehaviour
{
    [SerializeField] private float mobSpeed;
    [SerializeField] private float mobStoppingDistance;
    [SerializeField] private float mobAgroRadius;
    [SerializeField] private float mobAttackRate;

    private HealthSystem mobHealth;
    private float lastAttackTime;

    private Rigidbody mobRigidbody;
    private NavMeshAgent navMeshAgent;

    private Animator mobAnimator;

    [Header("Animator")]
    [SerializeField] private string isWalkingBoolName = "IsWalking";
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private string deathTriggerName = "Death";

    private GameObject player;

    private void Awake()
    {
        mobHealth = GetComponent<HealthSystem>();

        mobRigidbody = GetComponent<Rigidbody>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        
        mobAnimator = GetComponent<Animator>();

        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        navMeshAgent.speed = mobSpeed;
        navMeshAgent.stoppingDistance = mobStoppingDistance;
    }

    void Update()
    {
        if (mobHealth.IsDead())
        {
            OnMobDeath();
            return;
        }

        if (navMeshAgent == null || player == null) { return; }

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (distanceToPlayer < mobAgroRadius)
        {
            if (distanceToPlayer > mobStoppingDistance)
            {
                if (navMeshAgent.isStopped)
                    navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(player.transform.position);
                mobAnimator.SetBool(isWalkingBoolName, true);
            }
            else
            {
                EnterCombat();
            }
        }
        else
        {
            if (navMeshAgent.hasPath)
                navMeshAgent.ResetPath();
            if (!navMeshAgent.isStopped)
                navMeshAgent.isStopped = true;
            mobAnimator.SetBool(isWalkingBoolName, false);
        }
    }

    private void EnterCombat()
    {
        if (navMeshAgent == null || player == null) { return; }

        navMeshAgent.isStopped = true;
        mobAnimator.SetBool(isWalkingBoolName, false);

        Vector3 lookPos = player.transform.position - transform.position;
        lookPos.y = 0;

        if (lookPos != Vector3.zero)
        {
            Quaternion rotation = Quaternion.LookRotation(lookPos);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 10f);
        }

        if (Time.time >= lastAttackTime + (1f / mobAttackRate))
        {
            AttackPlayer();
            lastAttackTime = Time.time;
        }
    }

    private void AttackPlayer()
    {
        mobAnimator.SetTrigger(attackTriggerName);
    }

    private void OnMobDeath()
    {
        navMeshAgent.isStopped = true;
        mobRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        this.enabled = false;
    }

    public void DespawnMob()
    {
        Destroy(gameObject);
    }
}
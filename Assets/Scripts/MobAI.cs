using UnityEngine;
using UnityEngine.AI;

public class MobAI : MonoBehaviour
{
    [SerializeField] private float mobSpeed;
    [SerializeField] private float mobStoppingDistance;
    [SerializeField] private float mobAgroRadius;

    private HealthSystem mobHealth;
    private NavMeshAgent navMeshAgent;
    private Rigidbody mobRigidbody;

    [Header("Animator")]
    [SerializeField] private string isWalkingBoolName = "IsWalking";
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private string deathTriggerName = "Death";

    private Animator mobAnimator;

    private GameObject player;

    private void Awake()
    {
        mobHealth = GetComponent<HealthSystem>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        mobRigidbody = GetComponent<Rigidbody>();
        mobAnimator = GetComponent<Animator>();

        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.Log(gameObject.name + " „~„u „ƒ„}„€„s „~„p„z„„„y „y„s„‚„€„{„p");
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

        if (Vector3.Distance(transform.position, player.transform.position) < mobAgroRadius)
        {
            SetPlayerDestination();
        }
        else
        {
            mobAnimator.SetBool(isWalkingBoolName, false);
        }
    }

    private void SetPlayerDestination()
    {
        if (navMeshAgent == null || player == null) { return; }

        navMeshAgent.SetDestination(player.transform.position);
        mobAnimator.SetBool(isWalkingBoolName, true);
    }

    private void OnMobDeath()
    {
        mobRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        mobAnimator.SetTrigger(deathTriggerName);
    }

    public void DespawnMob()
    {
        Destroy(gameObject);
    }
}
using UnityEngine;
using UnityEngine.AI;

public class MobAI : MonoBehaviour
{
    [SerializeField] private float mobSpeed;
    [SerializeField] private float mobStoppingDistance;

    private NavMeshAgent navMeshAgent;
    private GameObject player;

    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent == null ) {
            Debug.Log(gameObject.name + " „q„u„x navmeshagent");
            return;
        }

        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) {
            Debug.Log(gameObject.name + " „~„u „ƒ„}„€„s „~„p„z„„„y „y„s„‚„€„{„p");
            return;
        }

        navMeshAgent.speed = mobSpeed;
        navMeshAgent.stoppingDistance = mobStoppingDistance;
    }

    void Update()
    {
        SetPlayerDestination();
    }

    private void SetPlayerDestination()
    {
        if (navMeshAgent == null || player == null) { return; }
        navMeshAgent.SetDestination(player.transform.position);
    }
}
using UnityEngine;
using UnityEngine.AI;

public class MobAI : MonoBehaviour
{
    public float speed = 3.5f;
    public float stoppingDistance = 1.5f;

    private NavMeshAgent agent;
    private Transform playerTransform;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            return;
        }

        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            return;
        }

        agent.speed = speed;
        agent.stoppingDistance = stoppingDistance;
    }

    void Update()
    {
        if (playerTransform != null && agent != null)
        {
            agent.SetDestination(playerTransform.position);
        }
    }
}
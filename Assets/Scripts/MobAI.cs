using UnityEngine;
using UnityEngine.AI; // ����������� ���������� ������������ ���� ��� NavMeshAgent

public class MobAI : MonoBehaviour
{
    public float speed = 3.5f;       // �������� ����, ������������� � ����������
    public float stoppingDistance = 1.5f; // ��������� ��������� ����� ����������

    private NavMeshAgent agent;
    private Transform playerTransform;

    void Start()
    {
        // �������� ��������� NavMeshAgent, ������� ������ ���� �� ���� �� �������
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent ��������� �� ������ �� ������� " + gameObject.name);
            return; // ��������� ����������, ���� ��� NavMeshAgent
        }

        // ���� ��������� �� ���� "Player". �������, ��� � ������ ��������� ���� ��� "Player"
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("�������� � ����� 'Player' �� ������ �� �����. ���������, ��� ��� �������� ����� ��� 'Player'.");
            return; // ��������� ����������, ���� ����� �� ������
        }

        // ����������� NavMeshAgent
        agent.speed = speed;
        agent.stoppingDistance = stoppingDistance;
    }

    void Update()
    {
        // ���� �������� ������ � NavMeshAgent ����������, ��������� ��� ���� - ������� ���������
        if (playerTransform != null && agent != null)
        {
            agent.SetDestination(playerTransform.position); // ������ ����� ���������� - ������� ���������
        }
    }
}
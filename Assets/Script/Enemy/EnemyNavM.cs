using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyNavM : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float patrolRadius = 5f;

    [Header("State")]
    [SerializeField] private float stateUpdateInterval = 0.5f;
    private enum AIState { Patrol, Chase }
    private AIState currentState = AIState.Patrol;
    private Vector3 patrolPoint;
    private bool patrolPointSet;

    private void Awake()
    {
        // Auto-assign NavMeshAgent if not set in inspector
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
            
        // Try to find player by tag if not assigned
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Start()
    {
        // Begin state updates
        StartCoroutine(UpdateAIState());
    }

    void Update()
    {
        switch (currentState)
        {
            case AIState.Patrol:
                Patrol();
                break;
            case AIState.Chase:
                ChasePlayer();
                break;
        }
    }

    private IEnumerator UpdateAIState()
    {
        while (true)
        {
            // Check distance to player
            if (player != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, player.position);
                
                if (distanceToPlayer <= detectionRange)
                {
                    currentState = AIState.Chase;
                }
                else
                {
                    currentState = AIState.Patrol;
                }
            }
            
            yield return new WaitForSeconds(stateUpdateInterval);
        }
    }

    private void Patrol()
    {
        if (!patrolPointSet || agent.remainingDistance < 0.5f)
        {
            SearchForPatrolPoint();
        }
    }

    private void SearchForPatrolPoint()
    {
        // Generate random point within patrol radius
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            patrolPoint = hit.position;
            agent.SetDestination(patrolPoint);
            patrolPointSet = true;
        }
        else
        {
            patrolPointSet = false;
        }
    }

    private void ChasePlayer()
    {
        if (player != null)
        {
            agent.SetDestination(player.position);
            patrolPointSet = false;
        }
        else
        {
            currentState = AIState.Patrol;
        }
    }

    // Optional: Visualize detection range in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);
    }
}

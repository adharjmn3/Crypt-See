using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyNavM : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform player;
    [SerializeField] private float patrolRadius = 5f;
    
    [Header("Detection Settings")]
    [SerializeField] private float suspiciousRange = 15f;
    [SerializeField] private float combatRange = 10f;
    [SerializeField] private float stateUpdateInterval = 0.5f;
    [SerializeField] private float timeToForgetPlayer = 5f;
    
    [Header("Movement Settings")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float suspiciousSpeed = 3f;
    [SerializeField] private float combatSpeed = 3.5f;

    // AI States
    private enum AIState { Patrol, Suspicious, Combat }
    [SerializeField] private AIState currentState = AIState.Patrol;
    
    // Patrol variables
    private Vector3 patrolPoint;
    private bool patrolPointSet;
    
    // Suspicious state variables
    private Vector3 lastKnownPlayerPosition;
    private float suspiciousTimer;
    
    // Combat control
    private EnemyShoot shootComponent;

    private void Awake()
    {
        // Auto-assign components if not set in inspector
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
            
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            
        shootComponent = GetComponent<EnemyShoot>();
        if (shootComponent != null)
            shootComponent.enabled = false; // Disable shooting by default
    }

    void Start()
    {
        // Begin state updates
        StartCoroutine(UpdateAIState());
        agent.speed = patrolSpeed;
    }

    void Update()
    {
        // Execute behavior based on current state
        switch (currentState)
        {
            case AIState.Patrol:
                HandlePatrolState();
                break;
            case AIState.Suspicious:
                HandleSuspiciousState();
                break;
            case AIState.Combat:
                HandleCombatState();
                break;
        }
    }

    private IEnumerator UpdateAIState()
    {
        while (true)
        {
            UpdateState();
            yield return new WaitForSeconds(stateUpdateInterval);
        }
    }

    private void UpdateState()
    {
        // Can't do anything without a player reference
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Determine the new state based on distance to player
        if (distanceToPlayer <= combatRange)
        {
            TransitionToState(AIState.Combat);
        }
        else if (distanceToPlayer <= suspiciousRange)
        {
            TransitionToState(AIState.Suspicious);
            lastKnownPlayerPosition = player.position;
            suspiciousTimer = timeToForgetPlayer;
        }
        else if (currentState != AIState.Suspicious)
        {
            TransitionToState(AIState.Patrol);
        }
    }

    private void TransitionToState(AIState newState)
    {
        // Only process if state is changing
        if (newState == currentState) return;
        
        // Exit old state
        switch (currentState)
        {
            case AIState.Combat:
                if (shootComponent != null)
                    shootComponent.enabled = false;
                break;
        }
        
        // Enter new state
        switch (newState)
        {
            case AIState.Patrol:
                agent.speed = patrolSpeed;
                patrolPointSet = false;
                break;
            case AIState.Suspicious:
                agent.speed = suspiciousSpeed;
                break;
            case AIState.Combat:
                agent.speed = combatSpeed;
                if (shootComponent != null)
                    shootComponent.enabled = true;
                break;
        }
        
        currentState = newState;
    }

    private void HandlePatrolState()
    {
        if (!patrolPointSet || agent.remainingDistance < 0.5f)
        {
            SearchForPatrolPoint();
        }
    }

    private void HandleSuspiciousState()
    {
        // Move toward last known player position
        agent.SetDestination(lastKnownPlayerPosition);
        
        // Update timer
        suspiciousTimer -= Time.deltaTime;
        
        // If reached last known position or timer expired, go back to patrol
        if (suspiciousTimer <= 0 || 
            (agent.remainingDistance < 0.5f && agent.pathStatus == NavMeshPathStatus.PathComplete))
        {
            TransitionToState(AIState.Patrol);
        }
    }

    private void HandleCombatState()
    {
        if (player != null)
        {
            agent.SetDestination(player.position);
        }
        else
        {
            TransitionToState(AIState.Patrol);
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

    // Visualize detection ranges in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, suspiciousRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, combatRange);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);
    }
}

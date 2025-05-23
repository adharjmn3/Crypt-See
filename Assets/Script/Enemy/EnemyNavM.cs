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
    [SerializeField] private float combatEngageDelay = 1.5f; // Time before actually shooting after entering combat mode
    
    [Header("Movement Settings")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float suspiciousSpeed = 3f;
    [SerializeField] private float combatSpeed = 3.5f;
    [SerializeField] private float rotationSpeed = 5f; // Rotation speed for smooth turning
    [SerializeField] private float turnThreshold = 30f; // Angle threshold for slowing down movement
    [SerializeField] private float minimumSpeedFactor = 0.2f; // Minimum speed factor when turning

    // AI States
    private enum AIState { Patrol, Suspicious, Combat }
    [SerializeField] private AIState currentState = AIState.Patrol;
    
    // Patrol variables
    private Vector3 patrolPoint;
    private bool patrolPointSet;
    
    // Suspicious state variables
    private Vector3 lastKnownPlayerPosition;
    private float suspiciousTimer;
    
    // Combat variables
    private EnemyShoot shootComponent;
    private float combatEngageTimer = 0f;
    
    // Movement and rotation variables
    private Vector2 movementDirection;
    private float currentSpeed;
    private float baseSpeed;

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
        baseSpeed = patrolSpeed;
        agent.speed = baseSpeed;
        
        // Configure NavMeshAgent for 2D
        agent.updateRotation = false;
        agent.updateUpAxis = false;
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
        
        // First update facing direction, then adjust movement speed based on rotation
        UpdateFacingDirection();
    }
    
    private void UpdateFacingDirection()
    {
        Vector2 desiredDirection = Vector2.zero;
        bool shouldRotate = false;
        
        // Determine the direction we want to face
        if (currentState == AIState.Combat && player != null && 
            Vector2.Distance(transform.position, player.position) < combatRange) 
        {
            // Face the player directly
            desiredDirection = player.position - transform.position;
            shouldRotate = true;
        }
        // Otherwise rotate based on our nav target direction
        else if (agent.velocity.magnitude > 0.01f)
        {
            // Get the 2D direction we want to move in
            desiredDirection = new Vector2(agent.desiredVelocity.x, agent.desiredVelocity.z);
            shouldRotate = true;
        }
        else
        {
            // If not moving, allow speed to return to normal
            AdjustSpeedBasedOnTurning(0);
            return;
        }
        
        if (shouldRotate && desiredDirection != Vector2.zero)
        {
            // Store this for speed adjustments
            movementDirection = desiredDirection.normalized;
            
            // Calculate the target angle in degrees
            float targetAngle = Mathf.Atan2(movementDirection.y, movementDirection.x) * Mathf.Rad2Deg;
            targetAngle -= 90; // -90 adjusts for sprite facing up by default
            
            // Get current rotation as an angle
            float currentAngle = transform.rotation.eulerAngles.z;
            
            // Find the shortest rotation path
            float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
            
            // Adjust agent speed based on how much we need to turn
            AdjustSpeedBasedOnTurning(Mathf.Abs(angleDifference));
            
            // Calculate smooth rotation amount
            float rotationAmount = Mathf.Sign(angleDifference) * 
                                   Mathf.Min(Mathf.Abs(angleDifference), 
                                             rotationSpeed * Time.deltaTime);
            
            // Apply the rotation
            float newAngle = currentAngle + rotationAmount;
            transform.rotation = Quaternion.Euler(0, 0, newAngle);
        }
    }
    
    private void AdjustSpeedBasedOnTurning(float turnAngle)
    {
        // Only slow down for significant turns
        if (turnAngle > turnThreshold)
        {
            // The larger the turn, the slower we go (down to minimum speed)
            float speedFactor = Mathf.Lerp(1f, minimumSpeedFactor, 
                                         (turnAngle - turnThreshold) / (180f - turnThreshold));
            agent.speed = baseSpeed * speedFactor;
        }
        else
        {
            // When not turning sharply, gradually return to full speed
            agent.speed = Mathf.Lerp(agent.speed, baseSpeed, 2f * Time.deltaTime);
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
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
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
                baseSpeed = patrolSpeed;
                patrolPointSet = false;
                if (shootComponent != null)
                    shootComponent.enabled = false;
                break;
                
            case AIState.Suspicious:
                baseSpeed = suspiciousSpeed;
                if (shootComponent != null)
                    shootComponent.enabled = false;
                break;
                
            case AIState.Combat:
                baseSpeed = combatSpeed;
                // Don't enable shooting immediately - set timer
                combatEngageTimer = combatEngageDelay;
                if (shootComponent != null)
                    shootComponent.enabled = false;
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
            // In combat, we want to move toward the player
            agent.SetDestination(player.position);
            
            // Update combat engagement timer
            if (combatEngageTimer > 0)
            {
                combatEngageTimer -= Time.deltaTime;
                
                // Enable shooting after delay
                if (combatEngageTimer <= 0 && shootComponent != null)
                {
                    shootComponent.enabled = true;
                }
            }
        }
        else
        {
            TransitionToState(AIState.Patrol);
        }
    }

    private void SearchForPatrolPoint()
    {
        // Generate random point within patrol radius (2D)
        Vector2 randomDirection = Random.insideUnitCircle * patrolRadius;
        Vector3 randomPoint = new Vector3(transform.position.x + randomDirection.x, transform.position.y, transform.position.z + randomDirection.y);
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, patrolRadius, NavMesh.AllAreas))
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

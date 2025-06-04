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
    [SerializeField] private float fieldOfViewAngle = 120f; // How wide the AI can see (in degrees)
    
    [Header("Movement Settings")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float suspiciousSpeed = 3f;
    [SerializeField] private float combatSpeed = 3.5f;
    [SerializeField] private float rotationSpeed = 5f; // Rotation speed for smooth turning
    [SerializeField] private float turnThreshold = 30f; // Angle threshold for slowing down movement
    [SerializeField] private float minimumSpeedFactor = 0.2f; // Minimum speed factor when turning
    [SerializeField] private float reachTargetDistance = 0.5f;
    [SerializeField] private float waitAtPatrolPoint = 1.5f; // Maximum time to wait at each patrol point
    [SerializeField] private float stuckDetectionTime = 2.0f; // Time to consider AI is stuck
    [SerializeField] private float stuckDistanceThreshold = 0.2f; // Distance to consider AI is stuck
    [SerializeField] private float rotationCompleteThreshold = 5f; // Angle threshold to consider rotation complete
    [SerializeField] private float obstacleDetectionRange = 1.0f; // How far ahead to check for obstacles
    private float patrolWaitTimer = 0f;
    
    // Target visibility
    [Header("Target Detection")]
    [SerializeField] public bool isTargetInSight = false; // Changed to public
    [SerializeField] private LayerMask obstaclesMask; // Layers that block sight

    // AI States
    private enum AIState { Patrol, Suspicious, Combat }
    [SerializeField] private AIState currentState = AIState.Patrol;
    
    // Movement States
    private enum MovementState { Idle, Rotating, Moving, AvoidingObstacle, Waiting }
    private MovementState movementState = MovementState.Idle;
    
    // Patrol variables
    private Vector3 patrolPoint;
    private bool patrolPointSet;
    private Vector3 startPosition; // Original position for returning after patrols
    
    
    // Suspicious state variables
    private Vector3 lastKnownPlayerPosition;
    private float suspiciousTimer;
    
    // Combat variables
    private EnemyShoot shootComponent;
    private float combatEngageTimer = 0f;
    
    // Movement and rotation variables
    private Vector2 movementDirection;
    private Vector3 currentDestination;
    private float currentSpeed;
    private float baseSpeed;
    private bool isRotating = false;
    private bool isWaitingAtPatrolPoint = false;
    private float targetAngle;
    
    
    // Stuck detection
    private Vector3 lastPosition;
    private float stuckTimer = 0f;
    private int pathfindingAttempts = 0;
    private const int MAX_ATTEMPTS = 3;

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
            
        // Store starting position for patrol returns
        startPosition = transform.position;
        lastPosition = transform.position;
        
        // Set default obstacle layers if not set
        if (obstaclesMask == 0)
            obstaclesMask = LayerMask.GetMask("Default", "Obstacles", "Wall");
    }

    void Start()
    {
        // Begin state updates
        StartCoroutine(UpdateAIState());
        baseSpeed = patrolSpeed;
        agent.speed = 0; // Start with zero speed until rotation is complete
        
        // Configure NavMeshAgent for 2D
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        
        // Initialize movement state
        movementState = MovementState.Idle;
    }

    void Update()
    {
        // Execute behavior based on current AI state
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
        
        // Handle movement based on movement state
        HandleMovementState();
        
        // Check if agent is stuck
        CheckIfStuck();
        
        // Check if we need to re-rotate due to path changes
        CheckForDirectionChanges();
    }
    
    private void RotateTowardsDestination()
    {
        if (agent.pathPending)
            return;
            
        Vector3 direction;
        
        // If we have a valid path with corners
        if (agent.hasPath && agent.path.corners.Length > 1)
        {
            // Get the next corner in our path - this handles zigzag paths better
            direction = agent.path.corners[1] - transform.position;
        }
        else
        {
            // Otherwise use direct direction to destination
            direction = currentDestination - transform.position;
        }
        
        // Calculate the target angle
        targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        targetAngle -= 90; // -90 adjusts for sprite facing up by default
        
        // Get current rotation as an angle
        float currentAngle = transform.rotation.eulerAngles.z;
        
        // Find the shortest rotation path
        float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
        
        // Check if we should still be rotating
        if (Mathf.Abs(angleDifference) <= rotationCompleteThreshold)
        {
            // Rotation is complete, start moving
            movementState = MovementState.Moving;
            agent.isStopped = false;
            agent.speed = baseSpeed;
            return;
        }
        
        // Calculate smooth rotation amount
        float rotationAmount = Mathf.Sign(angleDifference) * 
                               Mathf.Min(Mathf.Abs(angleDifference), 
                                         rotationSpeed * Time.deltaTime);
        
        // Apply the rotation
        float newAngle = currentAngle + rotationAmount;
        transform.rotation = Quaternion.Euler(0, 0, newAngle);
    }
    
    private void CheckForDirectionChanges()
    {
        // Only check when actually moving (not when rotating or waiting)
        if (movementState != MovementState.Moving || agent.velocity.magnitude < 0.1f)
            return;
        
        // If we have a valid path with corners
        if (agent.hasPath && agent.path.corners.Length > 1)
        {
            // Get the next corner in our path
            Vector3 nextCorner = agent.path.corners[1];
            Vector3 directionToCorner = nextCorner - transform.position;
            
            // Convert to 2D direction
            Vector2 movementDir = new Vector2(directionToCorner.x, directionToCorner.y).normalized;
            Vector2 facingDir = transform.up; // The direction the agent is facing
            
            // Calculate angle difference
            float angleDiff = Vector2.Angle(facingDir, movementDir);
            
            // If we're trying to move in a significantly different direction from where we're facing
            if (angleDiff > turnThreshold)
            {
                // We need to rotate to face the new direction
                float newTargetAngle = Mathf.Atan2(movementDir.y, movementDir.x) * Mathf.Rad2Deg;
                newTargetAngle -= 90; // Adjust based on sprite orientation
                
                // Only update if the new angle is significantly different from current target
                if (Mathf.Abs(Mathf.DeltaAngle(targetAngle, newTargetAngle)) > turnThreshold)
                {
                    targetAngle = newTargetAngle;
                    movementState = MovementState.Rotating;
                    agent.isStopped = true;
                }
            }
        }
    }
    
    private void HandleMovementState()
    {
        switch(movementState)
        {
            case MovementState.Idle:
                // In idle state, agent doesn't move
                agent.isStopped = true;
                agent.speed = 0;
                break;
                
            case MovementState.Rotating:
                // Rotate to face destination, don't move yet
                agent.isStopped = true;
                agent.speed = 0;
                RotateTowardsDestination();
                break;
                
            case MovementState.Moving:
                // We're properly facing our destination, now move
                agent.isStopped = false;
                agent.speed = baseSpeed;
                
                // Look for obstacles ahead
                if (CheckForObstaclesAhead())
                {
                    movementState = MovementState.AvoidingObstacle;
                }
                break;
                
            case MovementState.AvoidingObstacle:
                // Stop and compute a new path
                agent.isStopped = true;
                agent.speed = 0;
                TryAlternativePath();
                movementState = MovementState.Rotating;
                break;
                
            case MovementState.Waiting:
                // Used for waiting at patrol points
                agent.isStopped = true;
                agent.speed = 0;
                
                if (isWaitingAtPatrolPoint)
                {
                    patrolWaitTimer -= Time.deltaTime;
                    if (patrolWaitTimer <= 0)
                    {
                        isWaitingAtPatrolPoint = false;
                        movementState = MovementState.Idle;
                    }
                }
                break;
        }
    }
    
    private bool CheckForObstaclesAhead()
    {
        // Only check for obstacles when moving
        if (agent.velocity.magnitude < 0.1f)
            return false;
            
        // Direction we're currently moving in
        Vector2 moveDir = new Vector2(agent.velocity.x, agent.velocity.y).normalized;
        
        // Check for obstacles ahead with a raycast
        RaycastHit2D hit = Physics2D.Raycast(transform.position, moveDir, 
            obstacleDetectionRange, obstaclesMask);
            
        return hit.collider != null;
    }
    
    private void CheckIfStuck()
    {
        // Only check for stuck if we're actively trying to move
        if (agent.pathPending || agent.isStopped || isWaitingAtPatrolPoint || movementState != MovementState.Moving)
        {
            stuckTimer = 0f;
            lastPosition = transform.position;
            return;
        }
        
        // Check if we've moved significantly
        if (Vector3.Distance(transform.position, lastPosition) < stuckDistanceThreshold)
        {
            stuckTimer += Time.deltaTime;
            
            // If stuck for too long, try alternate path
            if (stuckTimer > stuckDetectionTime)
            {
                TryAlternativePath();
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
            pathfindingAttempts = 0;
        }
        
        lastPosition = transform.position;
    }
    
    private void TryAlternativePath()
    {
        pathfindingAttempts++;
        
        if (pathfindingAttempts >= MAX_ATTEMPTS)
        {
            // If we've tried too many times, find a completely new patrol point
            patrolPointSet = false;
            SearchForPatrolPoint();
            pathfindingAttempts = 0;
        }
        else
        {
            // Try a slight deviation from the current path
            Vector2 randomOffset = Random.insideUnitCircle * 2f;
            Vector3 newDestination = new Vector3(
                patrolPoint.x + randomOffset.x,
                patrolPoint.y + randomOffset.y,
                patrolPoint.z
            );
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(newDestination, out hit, patrolRadius, NavMesh.AllAreas))
            {
                patrolPoint = hit.position;
                SetDestinationWithRotation(patrolPoint);
            }
            else
            {
                // If we couldn't find a valid point nearby, try an entirely new point
                SearchForPatrolPoint();
            }
        }
    }
    
    private void HandlePatrolState()
    {
        // If waiting at patrol point
        if (isWaitingAtPatrolPoint)
        {
            patrolWaitTimer -= Time.deltaTime;
            if (patrolWaitTimer <= 0)
            {
                isWaitingAtPatrolPoint = false;
                patrolPointSet = false; // Find new patrol point
            }
            return;
        }
        
        // If we're currently rotating, let the rotation complete
        if (movementState == MovementState.Rotating)
        {
            return;
        }
        
        // Check for obstacles ahead and handle if detected
        if (movementState == MovementState.Moving && CheckForObstaclesAhead())
        {
            // Stop, then start rotating to new direction
            agent.isStopped = true;
            TryAlternativePath();
            movementState = MovementState.Rotating;
            return;
        }
        
        // If we don't have a patrol point or we've reached the current one
        if (!patrolPointSet || agent.remainingDistance < reachTargetDistance)
        {
            if (patrolPointSet && agent.remainingDistance < reachTargetDistance)
            {
                // We've reached our destination, wait here for a bit
                isWaitingAtPatrolPoint = true;
                patrolWaitTimer = Random.Range(0.5f, waitAtPatrolPoint); // Randomized wait time
                agent.isStopped = true;
                return;
            }
            
            // Find a new patrol point
            SearchForPatrolPoint();
        }
    }
    
    private void SearchForPatrolPoint()
    {
        // 50% chance to return to starting position if we're far from it
        if (Vector3.Distance(transform.position, startPosition) > patrolRadius * 1.5f && 
            Random.value < 0.5f)
        {
            patrolPoint = startPosition;
            SetDestinationWithRotation(patrolPoint);
            patrolPointSet = true;
            return;
        }
        
        // Generate random point within patrol radius (2D)
        Vector2 randomDirection = Random.insideUnitCircle * patrolRadius;
        Vector3 randomPoint = new Vector3(
            transform.position.x + randomDirection.x, 
            transform.position.y + randomDirection.y, 
            transform.position.z
        );
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, patrolRadius, NavMesh.AllAreas))
        {
            patrolPoint = hit.position;
            SetDestinationWithRotation(patrolPoint);
            patrolPointSet = true;
        }
    }
    
    private void SetDestinationWithRotation(Vector3 destination)
    {
        // Store current destination
        currentDestination = destination;
        
        // Tell NavMeshAgent where to go
        agent.SetDestination(destination);
        
        // First stop movement to rotate
        agent.isStopped = true;
        
        // Calculate direction to destination
        Vector2 direction = destination - transform.position;
        
        // Calculate target angle for rotation
        targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        targetAngle -= 90; // Adjust based on your sprite's default orientation
        
        // Set state to rotating
        movementState = MovementState.Rotating;
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
        if (player == null) 
        {
            isTargetInSight = false;
            return;
        }
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // Update target visibility status - ensure this happens every frame
        isTargetInSight = IsPlayerInSight();
        
        // Determine the new state based on distance to player and visibility
        if (isTargetInSight && distanceToPlayer <= combatRange)
        {
            TransitionToState(AIState.Combat);
        }
        else if (isTargetInSight && distanceToPlayer <= suspiciousRange)
        {
            TransitionToState(AIState.Suspicious);
            lastKnownPlayerPosition = player.position;
            suspiciousTimer = timeToForgetPlayer;
        }
        else if (currentState == AIState.Suspicious)
        {
            // Stay suspicious until timer expires - handled in HandleSuspiciousState()
        }
        else if (currentState != AIState.Patrol)
        {
            TransitionToState(AIState.Patrol);
        }
    }
    
    // Improve the IsPlayerInSight method to be more accurate
    private bool IsPlayerInSight()
    {
        if (player == null) return false;
        
        // Get direction to player
        Vector2 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;
        
        // If player is beyond our maximum sight range, can't see
        if (distanceToPlayer > suspiciousRange)
            return false;
        
        // Check if player is within field of view angle
        float angle = Vector2.Angle(transform.up, directionToPlayer);
        if (angle > fieldOfViewAngle * 0.5f)
            return false;
        
        // Raycast to check for obstacles between us and player
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, 
            directionToPlayer.normalized, 
            distanceToPlayer,
            obstaclesMask
        );
        
        // True only if nothing blocks the view OR if the first thing hit is the player
        if (hit.collider == null || hit.collider.CompareTag("Player"))
        {
            return true;
        }
        
        return false;
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
                
            case AIState.Patrol:
                isWaitingAtPatrolPoint = false;
                patrolWaitTimer = 0f;
                break;
        }
        
        // Enter new state
        switch (newState)
        {
            case AIState.Patrol:
                baseSpeed = patrolSpeed;
                patrolPointSet = false;
                isWaitingAtPatrolPoint = false;
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

    private void HandleSuspiciousState()
    {
        // Move toward last known player position
        if (movementState == MovementState.Idle)
        {
            SetNewDestination(lastKnownPlayerPosition);
        }
        
        // Update timer
        suspiciousTimer -= Time.deltaTime;
        
        // If reached last known position or timer expired, go back to patrol
        if (suspiciousTimer <= 0 || 
            (agent.remainingDistance < reachTargetDistance && 
             agent.pathStatus == NavMeshPathStatus.PathComplete && 
             movementState != MovementState.Rotating))
        {
            TransitionToState(AIState.Patrol);
            movementState = MovementState.Idle;
        }
    }

    private void HandleCombatState()
    {
        if (player != null)
        {
            // In combat, we want to move toward the player
            if (movementState == MovementState.Idle || 
                Vector3.Distance(player.position, currentDestination) > 1.0f)
            {
                SetNewDestination(player.position);
            }
            
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
            movementState = MovementState.Idle;
        }
    }

    private void SetNewDestination(Vector3 destination)
    {
        currentDestination = destination;
        agent.SetDestination(destination);
        movementState = MovementState.Rotating;
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
        
        // Draw field of view
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Vector3 leftDir = Quaternion.Euler(0, 0, fieldOfViewAngle * 0.5f) * transform.up;
        Vector3 rightDir = Quaternion.Euler(0, 0, -fieldOfViewAngle * 0.5f) * transform.up;
        Gizmos.DrawLine(transform.position, transform.position + leftDir * suspiciousRange);
        Gizmos.DrawLine(transform.position, transform.position + rightDir * suspiciousRange);
        
        // Draw obstacle detection range
        Gizmos.color = Color.red;
        if (agent.velocity.magnitude > 0.1f)
        {
            Vector2 moveDir = new Vector2(agent.velocity.x, agent.velocity.y).normalized;
            Gizmos.DrawLine(transform.position, transform.position + 
                           new Vector3(moveDir.x, moveDir.y, 0) * obstacleDetectionRange);
        }
        
        // Draw sightline to player if in sight
        if (isTargetInSight && player != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, player.position);
        }
        
        // Draw current movement state
        if (Application.isPlaying)
        {
            Gizmos.color = Color.white;
            string stateText = $"State: {movementState}";
            // UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, stateText);
        }
    }
    
    // Public accessor for target visibility
    public bool IsTargetVisible()
    {
        return isTargetInSight;
    }
}

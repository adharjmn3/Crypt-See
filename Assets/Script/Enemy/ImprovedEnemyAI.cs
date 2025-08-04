using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.AI;

// This script now requires NavMeshAgent for movement and other components for stats and abilities.
[RequireComponent(typeof(NavMeshAgent), typeof(EnemyStats), typeof(EnemyShoot), typeof(EnemyVision), typeof(EnemyHearing))]
public class ImprovedEnemyAI : Agent
{
    [Header("Target References")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Transform objectiveTarget; // Example for a secondary target

    [Header("Movement Settings")]
    [Tooltip("How fast the agent rotates to face its target.")]
    [SerializeField] private float rotationSpeed = 10f;
    [Tooltip("The ideal distance to keep from the player during combat.")]
    [SerializeField] private float optimalCombatDistance = 10f;
    [Tooltip("The acceptable range around the optimal distance.")]
    [SerializeField] private float distanceTolerance = 2f;
    [Tooltip("Radius for searching for a random patrol point.")]
    [SerializeField] private float patrolRadius = 20f;

    [Header("State Management")]
    [Tooltip("How long the agent remembers the player's last known position.")]
    [SerializeField] private float memoryDuration = 10f;

    // --- Component References ---
    private NavMeshAgent navAgent;
    private EnemyStats enemyStats;
    private EnemyShoot enemyShoot;
    private EnemyVision enemyVision;
    private EnemyHearing enemyHearing;
    private Transform agentTransform;

    // --- State Tracking ---
    private bool canSeePlayer;
    private bool canHearPlayer;
    private Vector3 lastKnownPlayerPosition;
    private float memoryTimer;
    private bool hasMemoryOfPlayer => memoryTimer > 0;

    // --- Target Management ---
    private Transform currentTarget;

    public override void Initialize()
    {
        // Get all necessary components
        navAgent = GetComponent<NavMeshAgent>();
        enemyStats = GetComponent<EnemyStats>();
        enemyShoot = GetComponent<EnemyShoot>();
        enemyVision = GetComponent<EnemyVision>();
        enemyHearing = GetComponent<EnemyHearing>();
        agentTransform = transform;

        if (playerTarget == null)
        {
            playerTarget = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
        
        // Set up vision and hearing targets
        if (enemyVision != null)
            enemyVision.SetTarget(playerTarget);
        if (enemyHearing != null)
            enemyHearing.SetTarget(playerTarget);
        
        // Note: objectiveTarget would need to be assigned, e.g., through a level manager

        // Configure NavMeshAgent: We handle rotation manually.
        navAgent.updateRotation = false;
        navAgent.updateUpAxis = false;
        
        // Set initial destination to current position
        navAgent.SetDestination(agentTransform.position);
    }

    public override void OnEpisodeBegin()
    {
        // Reset the agent's position and state if needed for training
        // For gameplay, you might handle this differently (e.g., at spawn)
        if (navAgent.isOnNavMesh)
        {
            navAgent.ResetPath();
            navAgent.SetDestination(agentTransform.position);
        }
        
        if (enemyStats != null)
        {
            enemyStats.health = enemyStats.maxHealth; // Reset health
        }
        
        // Reset memory and state
        memoryTimer = 0f;
        lastKnownPlayerPosition = Vector3.zero;
        currentTarget = null;
        
        // Disable shooting initially
        if (enemyShoot != null)
        {
            enemyShoot.enabled = false;
        }
    }

    /// <summary>
    /// Gathers all the information the agent needs to make a decision.
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        // Update sensor information from components
        UpdateSensorInfo();
        
        // --- Agent's own state ---
        // Health (normalized)
        sensor.AddObservation(enemyStats.health / enemyStats.maxHealth);
        
        // Position and rotation
        sensor.AddObservation(agentTransform.position);
        sensor.AddObservation(agentTransform.forward);
        
        // Movement state
        sensor.AddObservation(navAgent.velocity.normalized);
        sensor.AddObservation(navAgent.hasPath ? 1f : 0f);
        
        // --- Player relationship ---
        if (playerTarget != null)
        {
            Vector3 dirToPlayer = (playerTarget.position - agentTransform.position).normalized;
            float distanceToPlayer = Vector3.Distance(agentTransform.position, playerTarget.position);
            
            sensor.AddObservation(dirToPlayer);
            sensor.AddObservation(distanceToPlayer / 50f); // Normalize distance
            sensor.AddObservation(playerTarget.position);
        }
        else
        {
            // Add zeros if no player target
            sensor.AddObservation(Vector3.zero); // direction
            sensor.AddObservation(0f); // distance
            sensor.AddObservation(Vector3.zero); // position
        }
        
        // --- Sensory information ---
        sensor.AddObservation(canSeePlayer ? 1f : 0f);
        sensor.AddObservation(canHearPlayer ? 1f : 0f);
        sensor.AddObservation(hasMemoryOfPlayer ? 1f : 0f);
        
        // Memory information
        if (hasMemoryOfPlayer)
        {
            Vector3 dirToMemory = (lastKnownPlayerPosition - agentTransform.position).normalized;
            float distanceToMemory = Vector3.Distance(agentTransform.position, lastKnownPlayerPosition);
            sensor.AddObservation(dirToMemory);
            sensor.AddObservation(distanceToMemory / 50f);
            sensor.AddObservation(memoryTimer / memoryDuration); // How fresh is the memory
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }
        
        // --- Combat information ---
        sensor.AddObservation(enemyShoot.CanShoot() ? 1f : 0f);
        
        // Distance to optimal combat range
        if (playerTarget != null)
        {
            float distanceToPlayer = Vector3.Distance(agentTransform.position, playerTarget.position);
            float rangeDeviation = Mathf.Abs(distanceToPlayer - optimalCombatDistance) / optimalCombatDistance;
            sensor.AddObservation(rangeDeviation);
        }
        else
        {
            sensor.AddObservation(1f); // Max deviation if no target
        }
        
        // --- Objective information ---
        if (objectiveTarget != null)
        {
            Vector3 dirToObjective = (objectiveTarget.position - agentTransform.position).normalized;
            float distanceToObjective = Vector3.Distance(agentTransform.position, objectiveTarget.position);
            sensor.AddObservation(dirToObjective);
            sensor.AddObservation(distanceToObjective / 50f);
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
        }
    }

    /// <summary>
    /// Receives an action from the model and executes it.
    /// </summary>
    public override void OnActionReceived(ActionBuffers actions)
    {
        var discreteActions = actions.DiscreteActions;
        
        // Action interpretation:
        // 0: Target Selection (0=None, 1=Player, 2=Objective, 3=Last Known Position)
        // 1: Movement Decision (0=Idle, 1=Approach, 2=Retreat, 3=Strafe, 4=Patrol)
        // 2: Combat Action (0=None, 1=Shoot, 2=Aim Only)
        
        int targetAction = discreteActions[0];
        int movementAction = discreteActions[1];
        int combatAction = discreteActions[2];
        
        // --- Target Selection ---
        SelectTarget(targetAction);
        
        // --- Movement Execution ---
        ExecuteMovement(movementAction);
        
        // --- Combat Actions ---
        ExecuteCombat(combatAction);
        
        // --- Reward Calculation ---
        CalculateRewards();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions.Clear();

        // Target selection (manual for testing)
        discreteActions[0] = 1; // Always target player for manual control
        
        // Movement
        discreteActions[1] = 0; // Idle
        if (Input.GetKey(KeyCode.W)) discreteActions[1] = 1; // Approach
        if (Input.GetKey(KeyCode.S)) discreteActions[1] = 2; // Retreat
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D)) discreteActions[1] = 3; // Strafe

        // Combat
        discreteActions[2] = Input.GetKey(KeyCode.Space) ? 1 : 0; // Shoot
    }

    private void Update()
    {
        // Update memory timer
        if (memoryTimer > 0)
        {
            memoryTimer -= Time.deltaTime;
        }
        
        // Update sensor information
        UpdateSensorInfo();
        
        // Handle rotation towards current target
        if (currentTarget != null)
        {
            RotateTowards(currentTarget.position);
        }
        
        // Check for hearing updates
        if (enemyHearing != null)
        {
            enemyHearing.CheckForPlayerMovement();
        }
    }

    private void UpdateSensorInfo()
    {
        // Update vision
        canSeePlayer = enemyVision != null && enemyVision.CanSeePlayer;
        
        // Update hearing
        canHearPlayer = enemyHearing != null && enemyHearing.CanHearPlayer;
        
        // Update memory
        if (canSeePlayer && playerTarget != null)
        {
            lastKnownPlayerPosition = playerTarget.position;
            memoryTimer = memoryDuration;
        }
        else if (canHearPlayer && enemyHearing != null)
        {
            lastKnownPlayerPosition = enemyHearing.LastHeardPosition;
            memoryTimer = memoryDuration;
        }
    }

    private void SelectTarget(int targetAction)
    {
        switch (targetAction)
        {
            case 0: // No target
                currentTarget = null;
                break;
            case 1: // Player
                if (canSeePlayer || hasMemoryOfPlayer)
                    currentTarget = playerTarget;
                break;
            case 2: // Objective
                currentTarget = objectiveTarget;
                break;
            case 3: // Last known position
                if (hasMemoryOfPlayer)
                {
                    // Create a temporary target at last known position
                    // In a real implementation, you might want to use a more sophisticated approach
                    currentTarget = CreateTemporaryTarget(lastKnownPlayerPosition);
                }
                break;
        }
    }

    private Transform CreateTemporaryTarget(Vector3 position)
    {
        // Simple implementation - you might want to pool these
        GameObject tempTarget = new GameObject("TempTarget");
        tempTarget.transform.position = position;
        Destroy(tempTarget, 1f); // Clean up after 1 second
        return tempTarget.transform;
    }

    private void ExecuteMovement(int movementAction)
    {
        Vector3 destination = agentTransform.position;
        
        switch (movementAction)
        {
            case 0: // Idle
                navAgent.ResetPath();
                break;
                
            case 1: // Approach target
                if (currentTarget != null)
                {
                    destination = GetApproachPosition(currentTarget.position);
                    navAgent.SetDestination(destination);
                }
                break;
                
            case 2: // Retreat from target
                if (currentTarget != null)
                {
                    destination = GetRetreatPosition(currentTarget.position);
                    navAgent.SetDestination(destination);
                }
                break;
                
            case 3: // Strafe around target
                if (currentTarget != null)
                {
                    destination = GetStrafePosition(currentTarget.position);
                    navAgent.SetDestination(destination);
                }
                break;
                
            case 4: // Patrol
                destination = GetPatrolPosition();
                navAgent.SetDestination(destination);
                break;
        }
    }

    private void ExecuteCombat(int combatAction)
    {
        switch (combatAction)
        {
            case 0: // No combat action
                if (enemyShoot != null)
                    enemyShoot.enabled = false;
                break;
                
            case 1: // Shoot
                if (enemyShoot != null && canSeePlayer)
                {
                    enemyShoot.enabled = true;
                    bool shotFired = enemyShoot.TryShoot();
                    if (shotFired)
                    {
                        AddReward(0.2f); // Reward for successful shot
                    }
                }
                break;
                
            case 2: // Aim only (prepare to shoot)
                if (enemyShoot != null)
                {
                    enemyShoot.enabled = true;
                    // Don't actually shoot, just prepare
                }
                break;
        }
    }

    private Vector3 GetApproachPosition(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - agentTransform.position).normalized;
        float targetDistance = Mathf.Max(optimalCombatDistance - distanceTolerance, 2f);
        return targetPos - direction * targetDistance;
    }

    private Vector3 GetRetreatPosition(Vector3 targetPos)
    {
        Vector3 direction = (agentTransform.position - targetPos).normalized;
        float retreatDistance = optimalCombatDistance + distanceTolerance;
        return agentTransform.position + direction * retreatDistance;
    }

    private Vector3 GetStrafePosition(Vector3 targetPos)
    {
        Vector3 toTarget = (targetPos - agentTransform.position).normalized;
        Vector3 strafeDirection = Vector3.Cross(toTarget, Vector3.up).normalized;
        
        // Randomly choose left or right strafe
        if (Random.value > 0.5f)
            strafeDirection = -strafeDirection;
        
        return agentTransform.position + strafeDirection * 5f;
    }

    private Vector3 GetPatrolPosition()
    {
        // Generate a random patrol point within patrol radius
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        Vector3 patrolPoint = agentTransform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        
        // Ensure the point is on the NavMesh
        if (UnityEngine.AI.NavMesh.SamplePosition(patrolPoint, out UnityEngine.AI.NavMeshHit hit, patrolRadius, UnityEngine.AI.NavMesh.AllAreas))
        {
            return hit.position;
        }
        
        return agentTransform.position; // Stay put if no valid patrol point found
    }

    private void CalculateRewards()
    {
        // Base survival reward
        AddReward(0.001f);
        
        if (playerTarget == null) return;
        
        float distanceToPlayer = Vector3.Distance(agentTransform.position, playerTarget.position);
        
        // Reward for maintaining optimal combat distance
        if (canSeePlayer)
        {
            float distanceFromOptimal = Mathf.Abs(distanceToPlayer - optimalCombatDistance);
            if (distanceFromOptimal <= distanceTolerance)
            {
                AddReward(0.01f); // Good positioning
            }
            else
            {
                AddReward(-0.005f * (distanceFromOptimal / optimalCombatDistance)); // Penalty for poor positioning
            }
        }
        
        // Reward for keeping track of player
        if (canSeePlayer)
        {
            AddReward(0.005f); // Small reward for maintaining sight
        }
        else if (hasMemoryOfPlayer)
        {
            AddReward(0.002f); // Smaller reward for having memory
        }
        
        // Penalty for taking damage (if health decreased)
        float healthPercentage = enemyStats.health / enemyStats.maxHealth;
        if (healthPercentage < 1f)
        {
            AddReward(-0.1f * (1f - healthPercentage)); // Penalty proportional to damage taken
        }
        
        // Small penalty for each step to encourage efficiency
        AddReward(-1f / MaxStep);
    }

    /// <summary>
    /// Smoothly rotates the agent to face a target position.
    /// </summary>
    private void RotateTowards(Vector3 lookPosition)
    {
        Vector3 direction = (lookPosition - agentTransform.position).normalized;
        direction.y = 0; // Keep rotation on horizontal plane
        
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            agentTransform.rotation = Quaternion.Slerp(agentTransform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    // Public methods for external systems
    public bool IsPlayerVisible() => canSeePlayer;
    public bool IsPlayerAudible() => canHearPlayer;
    public bool HasPlayerMemory() => hasMemoryOfPlayer;
    public Vector3 GetLastKnownPlayerPosition() => lastKnownPlayerPosition;
    public Transform GetCurrentTarget() => currentTarget;
}
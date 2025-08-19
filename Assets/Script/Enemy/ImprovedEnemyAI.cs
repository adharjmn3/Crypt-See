using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.AI;

// This script now requires NavMeshAgent for movement and other components for stats and abilities.
// Updated to fix compilation errors
[RequireComponent(typeof(NavMeshAgent), typeof(EnemyStats), typeof(EnemyShoot))]
[RequireComponent(typeof(EnemyVision), typeof(EnemyHearing))]
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

    [Header("Detection System")]
    [Tooltip("How fast the detection level increases when seeing player.")]
    [SerializeField] private float detectionSpeed = 2f;
    [Tooltip("How fast the detection level decreases when not seeing player.")]
    [SerializeField] private float detectionDecaySpeed = 1f;
    [Tooltip("Maximum detection level (100 = fully detected).")]
    [SerializeField] private float maxDetectionLevel = 100f;
    [Tooltip("Detection level threshold to start combat.")]
    [SerializeField] private float combatThreshold = 80f;
    [Tooltip("Detection level threshold to start investigating.")]
    [SerializeField] private float investigateThreshold = 30f;

    // --- Component References ---
    private NavMeshAgent navAgent;
    private EnemyStats enemyStats;
    private EnemyShoot enemyShoot;
    private EnemyVision enemyVision;
    private EnemyHearing enemyHearing;
    private Awareness awarenessSystem;
    private Transform agentTransform;

    // --- State Tracking ---
    private bool canSeePlayer;
    private bool canHearPlayer;
    private Vector3 lastKnownPlayerPosition;
    private float memoryTimer;
    private bool hasMemoryOfPlayer => memoryTimer > 0;

    // --- Detection System ---
    private float currentDetectionLevel = 0f;
    private DetectionState detectionState = DetectionState.Unaware;

    // --- Target Management ---
    private Transform currentTarget;

    // Detection states for better AI behavior
    public enum DetectionState
    {
        Unaware,        // Green light - not detected
        Investigating,  // Yellow light - suspicious
        Detected,       // Red light - player spotted
        Combat          // Flashing red - attacking
    }

    public override void Initialize()
    {
        // Get all necessary components
        navAgent = GetComponent<NavMeshAgent>();
        enemyStats = GetComponent<EnemyStats>();
        enemyShoot = GetComponent<EnemyShoot>();
        enemyVision = GetComponent<EnemyVision>();
        enemyHearing = GetComponent<EnemyHearing>();
        awarenessSystem = GetComponent<Awareness>();
        agentTransform = transform;

        // Validate critical components
        if (navAgent == null)
            Debug.LogError($"ImprovedEnemyAI on {gameObject.name}: NavMeshAgent component is missing!");
        if (enemyStats == null)
            Debug.LogError($"ImprovedEnemyAI on {gameObject.name}: EnemyStats component is missing!");
        if (enemyShoot == null)
            Debug.LogError($"ImprovedEnemyAI on {gameObject.name}: EnemyShoot component is missing!");
        if (enemyVision == null)
            Debug.LogError($"ImprovedEnemyAI on {gameObject.name}: EnemyVision component is missing!");
        if (enemyHearing == null)
            Debug.LogError($"ImprovedEnemyAI on {gameObject.name}: EnemyHearing component is missing!");

        if (playerTarget == null)
        {
            playerTarget = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (playerTarget == null)
                Debug.LogWarning($"ImprovedEnemyAI on {gameObject.name}: No GameObject with 'Player' tag found!");
        }
        
        // Set up vision and hearing targets
        SetupComponentTarget(enemyVision, "EnemyVision", playerTarget);
        SetupComponentTarget(enemyHearing, "EnemyHearing", playerTarget);
        
        // Note: objectiveTarget would need to be assigned, e.g., through a level manager

        // Configure NavMeshAgent: We handle rotation manually.
        if (navAgent != null)
        {
            navAgent.updateRotation = false;
            navAgent.updateUpAxis = false;
            
            // Set initial destination to current position
            navAgent.SetDestination(agentTransform.position);
        }
    }

    public override void OnEpisodeBegin()
    {
        // Reset the agent's position and state if needed for training
        // For gameplay, you might handle this differently (e.g., at spawn)
        if (navAgent != null && navAgent.isOnNavMesh)
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
        
        // Reset detection system
        currentDetectionLevel = 0f;
        detectionState = DetectionState.Unaware;
        
        // Disable shooting initially
        if (enemyShoot != null)
        {
            enemyShoot.enabled = false;
        }
        
        // Training-specific resets
        ResetTrainingState();
    }

    /// <summary>
    /// Reset training-specific state
    /// </summary>
    private void ResetTrainingState()
    {
        // Reset cumulative reward for this episode
        SetReward(0f);
        
        // Note: StepCount is automatically managed by ML-Agents
        // It will reset to 0 when OnEpisodeBegin() is called
        
        // Log episode start for debugging
        if (Academy.Instance.IsCommunicatorOn)
        {
            Debug.Log($"{gameObject.name}: Episode {CompletedEpisodes + 1} started");
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
        if (enemyStats != null)
        {
            sensor.AddObservation(enemyStats.health / enemyStats.maxHealth);
        }
        else
        {
            sensor.AddObservation(1f); // Default full health if no stats component
        }
        
        // Position and rotation
        sensor.AddObservation(agentTransform.position);
        sensor.AddObservation(agentTransform.forward);
        
        // Movement state
        if (navAgent != null)
        {
            sensor.AddObservation(navAgent.velocity.normalized);
            sensor.AddObservation(navAgent.hasPath ? 1f : 0f);
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
        }
        
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
        
        // --- Detection system information ---
        sensor.AddObservation(currentDetectionLevel / maxDetectionLevel); // Normalized detection level
        sensor.AddObservation((int)detectionState / 3f); // Normalized detection state
        
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
        if (enemyShoot != null)
        {
            sensor.AddObservation(enemyShoot.CanShoot() ? 1f : 0f);
        }
        else
        {
            sensor.AddObservation(0f);
        }
        
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
        if (discreteActions.Length < 3)
        {
            Debug.LogError("ImprovedEnemyAI: DiscreteActions array is too small!");
            return;
        }
        
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
        
        // Update detection system
        UpdateDetectionSystem();
        
        // Handle rotation towards current target
        if (currentTarget != null)
        {
            RotateTowards(currentTarget.position);
        }
        
        // Check for hearing updates
        SafeInvokeMethod(enemyHearing, "CheckForPlayerMovement", "EnemyHearing");
    }

    private void UpdateSensorInfo()
    {
        // Update vision with null checks using reflection
        canSeePlayer = SafeGetBoolProperty(enemyVision, "CanSeePlayer", "EnemyVision");
        
        // Update hearing with null checks using reflection
        canHearPlayer = SafeGetBoolProperty(enemyHearing, "CanHearPlayer", "EnemyHearing");
        
        // Update memory
        if (canSeePlayer && playerTarget != null)
        {
            lastKnownPlayerPosition = playerTarget.position;
            memoryTimer = memoryDuration;
        }
        else if (canHearPlayer && enemyHearing != null)
        {
            Vector3 lastHeardPos = SafeGetVector3Property(enemyHearing, "LastHeardPosition", "EnemyHearing");
            if (lastHeardPos != Vector3.zero)
            {
                lastKnownPlayerPosition = lastHeardPos;
                memoryTimer = memoryDuration;
            }
        }
    }

    private void UpdateDetectionSystem()
    {
        float previousDetectionLevel = currentDetectionLevel;
        
        // Increase detection when seeing or hearing player
        if (canSeePlayer)
        {
            // Direct line of sight - fastest detection
            currentDetectionLevel += detectionSpeed * Time.deltaTime * 2f;
        }
        else if (canHearPlayer)
        {
            // Sound detection - slower than visual
            currentDetectionLevel += detectionSpeed * Time.deltaTime * 0.5f;
        }
        else if (hasMemoryOfPlayer)
        {
            // Investigating based on memory - very slow increase
            currentDetectionLevel += detectionSpeed * Time.deltaTime * 0.2f;
        }
        else
        {
            // Decay detection when no stimulus
            currentDetectionLevel -= detectionDecaySpeed * Time.deltaTime;
        }
        
        // Clamp detection level
        currentDetectionLevel = Mathf.Clamp(currentDetectionLevel, 0f, maxDetectionLevel);
        
        // Update detection state based on level
        DetectionState previousState = detectionState;
        UpdateDetectionState();
        
        // Update awareness system if available
        UpdateAwarenessSystem();
        
        // Trigger state change events
        if (previousState != detectionState)
        {
            OnDetectionStateChanged(previousState, detectionState);
        }
    }

    private void UpdateDetectionState()
    {
        if (currentDetectionLevel >= combatThreshold)
        {
            detectionState = DetectionState.Combat;
        }
        else if (currentDetectionLevel >= investigateThreshold && (canSeePlayer || canHearPlayer))
        {
            detectionState = DetectionState.Detected;
        }
        else if (currentDetectionLevel > 0f || hasMemoryOfPlayer)
        {
            detectionState = DetectionState.Investigating;
        }
        else
        {
            detectionState = DetectionState.Unaware;
        }
    }

    private void UpdateAwarenessSystem()
    {
        if (awarenessSystem != null && awarenessSystem.enemyNPC != null)
        {
            // Map our detection level to the EnemyNPC tension system
            awarenessSystem.enemyNPC.tensionMeter = currentDetectionLevel;
            
            // Update max tension if needed
            if (awarenessSystem.enemyNPC.maxTensionMeter < maxDetectionLevel)
            {
                awarenessSystem.enemyNPC.maxTensionMeter = maxDetectionLevel;
            }
        }
    }

    private void OnDetectionStateChanged(DetectionState previousState, DetectionState newState)
    {
        switch (newState)
        {
            case DetectionState.Unaware:
                Debug.Log($"{gameObject.name}: Lost interest in player");
                break;
            case DetectionState.Investigating:
                Debug.Log($"{gameObject.name}: Something seems suspicious...");
                break;
            case DetectionState.Detected:
                Debug.Log($"{gameObject.name}: Player spotted!");
                break;
            case DetectionState.Combat:
                Debug.Log($"{gameObject.name}: Engaging target!");
                break;
        }
        
        // Reward/penalty for detection state changes
        switch (newState)
        {
            case DetectionState.Detected:
                AddReward(0.1f); // Reward for spotting player
                break;
            case DetectionState.Combat:
                AddReward(0.2f); // Larger reward for full detection
                break;
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
        if (navAgent == null) return; // Safety check
        
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
                // Only allow shooting if detection level is high enough
                if (enemyShoot != null && detectionState >= DetectionState.Detected && canSeePlayer)
                {
                    enemyShoot.enabled = true;
                    bool shotFired = enemyShoot.TryShoot();
                    if (shotFired)
                    {
                        AddReward(0.2f); // Reward for successful shot
                        // Shooting maintains high detection level
                        currentDetectionLevel = Mathf.Max(currentDetectionLevel, combatThreshold);
                    }
                }
                else
                {
                    if (enemyShoot != null)
                        enemyShoot.enabled = false;
                }
                break;
                
            case 2: // Aim only (prepare to shoot)
                if (enemyShoot != null && detectionState >= DetectionState.Investigating)
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
        
        // Reward for maintaining optimal combat distance when in combat
        if (detectionState == DetectionState.Combat)
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
        
        // Reward for detection system progression
        switch (detectionState)
        {
            case DetectionState.Investigating:
                AddReward(0.002f); // Small reward for being alert
                break;
            case DetectionState.Detected:
                AddReward(0.005f); // Medium reward for spotting player
                break;
            case DetectionState.Combat:
                AddReward(0.008f); // High reward for combat readiness
                break;
        }
        
        // Reward for keeping track of player based on detection method
        if (canSeePlayer)
        {
            AddReward(0.005f); // Reward for visual contact
        }
        else if (canHearPlayer)
        {
            AddReward(0.003f); // Smaller reward for audio contact
        }
        else if (hasMemoryOfPlayer)
        {
            AddReward(0.001f); // Smallest reward for memory
        }
        
        // Penalty for taking damage (if health decreased)
        if (enemyStats != null)
        {
            float healthPercentage = enemyStats.health / enemyStats.maxHealth;
            if (healthPercentage < 1f)
            {
                AddReward(-0.1f * (1f - healthPercentage)); // Penalty proportional to damage taken
            }
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
    
    // Detection system accessors
    public float GetDetectionLevel() => currentDetectionLevel;
    public float GetDetectionPercentage() => currentDetectionLevel / maxDetectionLevel;
    public DetectionState GetDetectionState() => detectionState;
    public bool IsInCombat() => detectionState == DetectionState.Combat;
    public bool IsAlerted() => detectionState >= DetectionState.Investigating;

    // Training-specific methods
    public void SetDetectionLevel(float level) => currentDetectionLevel = Mathf.Clamp(level, 0f, maxDetectionLevel);
    public void ForceDetectionState(DetectionState state) => detectionState = state;
    public int GetEpisodeCount() => CompletedEpisodes;
    public float GetEpisodeReward() => GetCumulativeReward();
    
    /// <summary>
    /// Helper method to safely set up component targets using reflection if needed
    /// </summary>
    private void SetupComponentTarget(Component component, string componentName, Transform target)
    {
        if (component == null) return;
        
        try
        {
            // First try direct method call
            var method = component.GetType().GetMethod("SetTarget");
            if (method != null)
            {
                method.Invoke(component, new object[] { target });
                Debug.Log($"Successfully set target for {componentName}");
            }
            else
            {
                Debug.LogWarning($"{componentName}: SetTarget method not found");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to set target for {componentName}: {e.Message}");
        }
    }
    
    /// <summary>
    /// Helper method to safely invoke methods using reflection
    /// </summary>
    private void SafeInvokeMethod(Component component, string methodName, string componentName)
    {
        if (component == null) return;
        
        try
        {
            var method = component.GetType().GetMethod(methodName);
            if (method != null)
            {
                method.Invoke(component, null);
            }
            else
            {
                Debug.LogWarning($"{componentName}: {methodName} method not found");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to invoke {methodName} on {componentName}: {e.Message}");
        }
    }
    
    /// <summary>
    /// Helper method to safely get boolean properties using reflection
    /// </summary>
    private bool SafeGetBoolProperty(Component component, string propertyName, string componentName)
    {
        if (component == null) return false;
        
        try
        {
            var property = component.GetType().GetProperty(propertyName);
            if (property != null && property.PropertyType == typeof(bool))
            {
                return (bool)property.GetValue(component);
            }
            else
            {
                Debug.LogWarning($"{componentName}: {propertyName} property not found or not a boolean");
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to get {propertyName} from {componentName}: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Helper method to safely get Vector3 properties using reflection
    /// </summary>
    private Vector3 SafeGetVector3Property(Component component, string propertyName, string componentName)
    {
        if (component == null) return Vector3.zero;
        
        try
        {
            var property = component.GetType().GetProperty(propertyName);
            if (property != null && property.PropertyType == typeof(Vector3))
            {
                return (Vector3)property.GetValue(component);
            }
            else
            {
                Debug.LogWarning($"{componentName}: {propertyName} property not found or not a Vector3");
                return Vector3.zero;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to get {propertyName} from {componentName}: {e.Message}");
            return Vector3.zero;
        }
    }
}
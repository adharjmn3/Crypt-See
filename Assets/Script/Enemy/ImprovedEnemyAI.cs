using System;
using System.Collections;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.AI; // Required for NavMeshAgent

public class ImprovedEnemyAI : Agent
{
    [Header("General References")]
    [SerializeField] private GameObject targetObj; // Player GameObject
    [SerializeField] private NavMeshAgent agent; // NavMeshAgent component
    [SerializeField] private EnemyVision enemyVision;
    [SerializeField] private EnemyHearing enemyHearing;
    [SerializeField] private EnemyShoot enemyShoot;

    [Header("Agent Settings")]
    [SerializeField] public float tensionMeter;
    [SerializeField] public float maxTensionMeter = 5f;
    [SerializeField] private float fillSpeed = 0.5f;
    [SerializeField] private float drainSpeed = 0.2f;
    [SerializeField] private float memoryDuration = 10f;
    [SerializeField] private float suspiciousDuration = 8f; // How long to stay suspicious
    private float currentMemoryTimer = 0f;
    private float suspiciousTimer = 0f;
    private bool hasPlayerMemory = false;
    private float lastTensionMeter = 0f;

    [Header("Detection Settings")]
    [SerializeField] private float suspiciousRange = 15f;
    [SerializeField] private float combatRange = 10f;
    [SerializeField] private float fieldOfViewAngle = 120f; // How wide the AI can see (in degrees)
    [SerializeField] private LayerMask obstaclesMask; // Layers that block sight
    public bool isTargetInSight = false; // Public for observation, updated internally
    bool isSoundDetected = false;

    [Header("Movement Settings")]
    [SerializeField] private float patrolRadius = 5f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float suspiciousSpeed = 3f;
    [SerializeField] private float combatSpeed = 3.5f;
    [SerializeField] private float rotationSpeed = 5f; // Rotation speed for smooth turning
    [SerializeField] private float turnThreshold = 30f; // Angle threshold for slowing down movement
    [SerializeField] private float reachTargetDistance = 0.5f; // Distance to consider destination reached
    [SerializeField] private float waitAtPatrolPoint = 1.5f; // Maximum time to wait at each patrol point
    [SerializeField] private float stuckDetectionTime = 2.0f; // Time to consider AI is stuck
    [SerializeField] private float stuckDistanceThreshold = 0.2f; // Distance to consider AI is stuck
    [SerializeField] private float rotationCompleteThreshold = 5f; // Angle threshold to consider rotation complete
    [SerializeField] private float obstacleDetectionRange = 1.0f; // How far ahead to check for obstacles
    [SerializeField] private float combatEngageDelay = 1.5f; // Time before actually shooting after entering combat mode
    private float patrolWaitTimer = 0f;
    private float combatEngageTimer = 0f;

    // Internal AI State (driven by ML-Agent's actions)
    private enum AIState { Patrol, Suspicious, Combat, SearchLastKnown }
    private AIState currentAIState = AIState.Patrol;

    // Internal Movement State (for NavMeshAgent's movement logic)
    private enum MovementState { Idle, Rotating, Moving, AvoidingObstacle, Waiting }
    private MovementState movementState = MovementState.Idle;

    // Movement and pathfinding variables
    Vector3 agentPos;
    Vector3 targetPos; // Player's current position
    private Vector3 lastKnownPlayerPosition;
    private Vector3 patrolPoint;
    private bool patrolPointSet;
    private Vector3 startPosition; // Original position for returning after patrols

    private Vector3 currentNavMeshDestination; // The destination currently set for NavMeshAgent
    private float baseAgentSpeed; // Speed determined by the chosen AI state
    private float targetRotationAngle; // For smooth rotation
    private float lastPositionMagnitude; // For stuck detection
    private float stuckTimer = 0f;
    private int pathfindingAttempts = 0;
    private const int MAX_ATTEMPTS = 3;

    public override void Initialize()
    {
        // Auto-assign components if not set in inspector
        if (targetObj == null)
            targetObj = GameObject.FindGameObjectWithTag("Player");
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (enemyVision == null)
            enemyVision = GetComponent<EnemyVision>();
        if (enemyHearing == null)
            enemyHearing = GetComponent<EnemyHearing>();
        if (enemyShoot == null)
            enemyShoot = GetComponent<EnemyShoot>();

        // Ensure target is set for vision
        if (enemyVision != null && targetObj != null)
        {
            enemyVision.SetTarget(targetObj);
        }

        // Configure NavMeshAgent for 2D (assuming a 2D game or top-down 3D with 2D movement)
        if (agent != null)
        {
            agent.updateRotation = false; // We handle rotation manually for 2D sprites
            agent.updateUpAxis = false;
            agent.speed = 0; // Start still
            agent.isStopped = true;
        }

        // Disable shooting at start
        if (enemyShoot != null)
        {
            enemyShoot.enabled = false;
        }

        // Store starting position for patrol returns
        startPosition = transform.position;
        lastPositionMagnitude = transform.position.magnitude;

        // Set default obstacle layers if not set
        if (obstaclesMask == 0)
            obstaclesMask = LayerMask.GetMask("Default", "Obstacles", "Wall");
    }

    public override void OnEpisodeBegin()
    {
        // Reset all relevant states and timers for a new episode
        tensionMeter = 0f;
        lastTensionMeter = 0f;
        currentMemoryTimer = 0f;
        hasPlayerMemory = false;
        isTargetInSight = false;
        isSoundDetected = false;
        combatEngageTimer = 0f;
        patrolWaitTimer = 0f;
        stuckTimer = 0f;
        pathfindingAttempts = 0;

        // Reset NavMeshAgent
        if (agent != null)
        {
            agent.ResetPath();
            agent.isStopped = true;
            agent.speed = 0;
        }

        // Disable shooting
        if (enemyShoot != null)
        {
            enemyShoot.enabled = false;
        }

        // Reset AI and movement states
        currentAIState = AIState.Patrol;
        movementState = MovementState.Idle;
        patrolPointSet = false;

        // Teleport agent to a random starting position on the NavMesh if needed for training variety
        // Example: Randomly place agent within a certain radius of its original start position
        // NavMeshHit hit;
        // if (NavMesh.SamplePosition(startPosition + (Vector3)UnityEngine.Random.insideUnitCircle * 5f, out hit, 10f, NavMesh.AllAreas))
        // {
        //     transform.position = hit.position;
        // }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Ensure references are valid before observing
        if (targetObj == null || agent == null || enemyVision == null || enemyHearing == null)
        {
            // If essential components are missing, provide default observations or end episode
            sensor.AddObservation(Vector3.zero); // Agent Pos
            sensor.AddObservation(Vector3.zero); // Agent Up
            sensor.AddObservation(0f); // isTargetInSight
            sensor.AddObservation(0f); // isSoundDetected
            sensor.AddObservation(0f); // IsTensionMeterFull
            sensor.AddObservation(0f); // Tension Meter
            sensor.AddObservation(0f); // Tension Change
            sensor.AddObservation((float)currentAIState / Enum.GetNames(typeof(AIState)).Length); // Normalized AI State
            sensor.AddObservation(0f); // NavMesh remaining distance
            sensor.AddObservation(0f); // NavMesh has path
            sensor.AddObservation(0f); // NavMesh path complete
            sensor.AddObservation(0f); // NavMesh velocity magnitude
            sensor.AddObservation(Vector3.zero); // Target Pos
            sensor.AddObservation(Vector3.zero); // Relative target pos
            sensor.AddObservation(0f); // Relative target dist
            sensor.AddObservation(Vector3.zero); // Last known player pos
            sensor.AddObservation(Vector3.zero); // Relative last known pos
            sensor.AddObservation(0f); // Relative last known dist
            return;
        }

        agentPos = transform.position;
        targetPos = targetObj.transform.position;

        // Basic Agent Observations
        sensor.AddObservation(agentPos);
        sensor.AddObservation(transform.up.normalized);

        // Status Observations
        sensor.AddObservation(isTargetInSight ? 1f : 0f);
        sensor.AddObservation(isSoundDetected ? 1f : 0f);
        sensor.AddObservation(IsTensionMeterFull() ? 1f : 0f);
        sensor.AddObservation(tensionMeter / maxTensionMeter); // Normalized tension
        sensor.AddObservation(tensionMeter - lastTensionMeter); // Tension change

        // Current AI State (normalized)
        sensor.AddObservation((float)currentAIState / Enum.GetNames(typeof(AIState)).Length);

        // NavMeshAgent Observations
        sensor.AddObservation(agent.remainingDistance / suspiciousRange); // Normalized remaining distance
        sensor.AddObservation(agent.hasPath ? 1f : 0f);
        sensor.AddObservation(agent.pathStatus == NavMeshPathStatus.PathComplete ? 1f : 0f);
        sensor.AddObservation(agent.velocity.magnitude / combatSpeed); // Normalized current speed

        // Target-related Observations (conditional based on visibility/memory)
        if (isTargetInSight || hasPlayerMemory)
        {
            Vector3 targetRelativePosition = targetPos - agentPos;
            sensor.AddObservation(targetPos); // Absolute target position
            sensor.AddObservation(targetRelativePosition.normalized);
            sensor.AddObservation(targetRelativePosition.magnitude / suspiciousRange); // Normalized distance
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
        }

        // Last known player position observation for suspicious state
        if (currentAIState == AIState.Suspicious || currentAIState == AIState.SearchLastKnown)
        {
            Vector3 lastKnownRelativePosition = lastKnownPlayerPosition - agentPos;
            sensor.AddObservation(lastKnownPlayerPosition);
            sensor.AddObservation(lastKnownRelativePosition.normalized);
            sensor.AddObservation(lastKnownRelativePosition.magnitude / suspiciousRange);
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Update basic status (sight, sound, tension)
        StatusUpdate();

        // Decode ML-Agent's actions
        int aiStateAction = actions.DiscreteActions[0]; // Determines high-level AI state
        float moveXOffset = actions.ContinuousActions[0]; // Used for Patrol/Search offsets
        float moveYOffset = actions.ContinuousActions[1]; // Used for Patrol/Search offsets
        float speedFactor = actions.ContinuousActions[2]; // Controls overall movement speed (0 to 1)

        // Clamp speed factor to ensure it's within a valid range
        speedFactor = Mathf.Clamp01(speedFactor);

        // Update the current AI state and handle state-entry logic
        AIState previousAIState = currentAIState;
        currentAIState = (AIState)aiStateAction;

        if (currentAIState != previousAIState)
        {
            if (currentAIState == AIState.Suspicious)
            {
                suspiciousTimer = suspiciousDuration;
            }
            else if (currentAIState == AIState.Combat)
            {
                combatEngageTimer = combatEngageDelay;
            }
        }

        // Execute behavior based on the chosen AI state
        switch (currentAIState)
        {
            case AIState.Patrol:
                HandlePatrolState(moveXOffset, moveYOffset);
                baseAgentSpeed = patrolSpeed;
                if (enemyShoot != null) enemyShoot.enabled = false;
                break;
            case AIState.Suspicious:
                HandleSuspiciousState(); // Agent is deciding to be suspicious, move to last known
                baseAgentSpeed = suspiciousSpeed;
                if (enemyShoot != null) enemyShoot.enabled = false;
                break;
            case AIState.Combat:
                HandleCombatState(); // Agent is deciding to combat, move to player
                baseAgentSpeed = combatSpeed;
                // Shooting handled by combatEngageTimer within HandleCombatState
                break;
            case AIState.SearchLastKnown:
                HandleSearchLastKnownState(moveXOffset, moveYOffset); // Agent actively searching an area
                baseAgentSpeed = suspiciousSpeed;
                if (enemyShoot != null) enemyShoot.enabled = false;
                break;
        }

        // Apply chosen speed to NavMeshAgent
        if (agent != null)
        {
            agent.speed = baseAgentSpeed * speedFactor;
        }

        // Handle internal movement states (rotation, obstacle avoidance, etc.)
        HandleMovementState();

        // Check if agent is stuck
        CheckIfStuck();

        // Update tension meter (from original EnemyNPC)
        HandleTensionMeter();

        // Apply rewards/penalties
        AddRewardsAndPenalties();
    }

    private void AddRewardsAndPenalties()
    {
        // Reward for being alive and active
        AddReward(0.001f);

        // Reward for increasing tension when near player/sound
        if (tensionMeter > lastTensionMeter)
        {
            AddReward(0.005f * (tensionMeter - lastTensionMeter));
        }

        // Penalize for tension draining when player is in sight/sound detected
        if (tensionMeter < lastTensionMeter && (isTargetInSight || isSoundDetected))
        {
            AddReward(-0.002f * (lastTensionMeter - tensionMeter));
        }

        // Reward for being in combat range when player is in sight
        if (currentAIState == AIState.Combat && isTargetInSight && Vector3.Distance(agentPos, targetPos) < combatRange)
        {
            AddReward(0.005f);
        }

        // Penalize for being stuck
        if (stuckTimer > stuckDetectionTime * 0.5f)
        {
            AddReward(-0.01f); // Moderate penalty for being stuck
        }
        if (pathfindingAttempts > 0)
        {
            AddReward(-0.005f * pathfindingAttempts); // Penalty for repeated pathfinding attempts
        }

        // Penalize for taking too long to complete a patrol cycle (if patrol is a goal)
        // This requires tracking patrol cycle time and could be more complex.
        // For now, general time penalty can suffice for overall efficiency.
        AddReward(-0.0001f); // Small constant penalty per step

        lastTensionMeter = tensionMeter; // Update for next observation
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        var continuousActions = actionsOut.ContinuousActions;

        // Manual control for testing (example: W for combat, A for patrol, D for suspicious, S for search)
        if (Input.GetKey(KeyCode.W))
        {
            discreteActions[0] = (int)AIState.Combat;
            continuousActions[0] = 0; // No offset
            continuousActions[1] = 0;
            continuousActions[2] = 1f; // Full speed
        }
        else if (Input.GetKey(KeyCode.A))
        {
            discreteActions[0] = (int)AIState.Patrol;
            continuousActions[0] = UnityEngine.Random.Range(-1f, 1f); // Random offset for patrol
            continuousActions[1] = UnityEngine.Random.Range(-1f, 1f);
            continuousActions[2] = 0.5f; // Half speed
        }
        else if (Input.GetKey(KeyCode.D))
        {
            discreteActions[0] = (int)AIState.Suspicious;
            continuousActions[0] = 0;
            continuousActions[1] = 0;
            continuousActions[2] = 0.7f;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discreteActions[0] = (int)AIState.SearchLastKnown;
            continuousActions[0] = UnityEngine.Random.Range(-1f, 1f);
            continuousActions[1] = UnityEngine.Random.Range(-1f, 1f);
            continuousActions[2] = 0.6f;
        }
        else
        {
            // Default to Patrol if no keys pressed, or a more nuanced idle if desired.
            discreteActions[0] = (int)AIState.Patrol;
            continuousActions[0] = 0;
            continuousActions[1] = 0;
            continuousActions[2] = 0.3f; // Slower speed
        }
    }


    // --- Core AI State Handlers (ML-Agent driven) ---

    private void HandlePatrolState(float offsetX, float offsetY)
    {
        // If agent reaches its current patrol point or has no point set
        if (!patrolPointSet || (agent.hasPath && agent.remainingDistance < reachTargetDistance && movementState != MovementState.Rotating))
        {
            // Agent reached destination, wait for a bit
            if (patrolPointSet && agent.remainingDistance < reachTargetDistance)
            {
                movementState = MovementState.Waiting;
                patrolWaitTimer = UnityEngine.Random.Range(0.5f, waitAtPatrolPoint);
                agent.isStopped = true;
                return; // Wait until timer is done
            }

            // Find a new patrol point if not waiting
            SearchForPatrolPoint(offsetX, offsetY);
        }
        else if (movementState == MovementState.Waiting)
        {
            patrolWaitTimer -= Time.deltaTime;
            if (patrolWaitTimer <= 0)
            {
                movementState = MovementState.Idle; // Allow agent to start pathfinding again
                patrolPointSet = false; // Trigger new point search
            }
        }
    }

    private void HandleSuspiciousState()
    {
        // When suspicious, the goal is to move towards the last known player position
        if (Vector3.Distance(agentPos, lastKnownPlayerPosition) > reachTargetDistance && movementState != MovementState.Rotating)
        {
            SetNavMeshDestination(lastKnownPlayerPosition);
        }

        suspiciousTimer -= Time.deltaTime;
        if (suspiciousTimer <= 0)
        {
            // If timer expires, the agent might naturally transition to Patrol or Search based on RL policy
            // No explicit TransitionToState here, RL policy will decide next state
        }
    }

    private void HandleCombatState()
    {
        if (targetObj != null)
        {
            // Constantly set destination to player's current position
            SetNavMeshDestination(targetPos);

            // Manage shooting delay
            if (combatEngageTimer > 0)
            {
                combatEngageTimer -= Time.deltaTime;
                if (combatEngageTimer <= 0 && enemyShoot != null)
                {
                    enemyShoot.enabled = true;
                }
            } else if (enemyShoot != null && !enemyShoot.enabled) {
                // Ensure shooting is enabled if timer elapsed
                enemyShoot.enabled = true;
            }
        }
        else
        {
            // If target is lost in combat, ML-Agent should learn to transition to another state
            // For now, it will simply stop moving towards target.
        }
    }

    private void HandleSearchLastKnownState(float offsetX, float offsetY)
    {
        // Agent is actively searching the area around the last known player position
        Vector3 searchAreaCenter = lastKnownPlayerPosition;

        // The ML-Agent's continuous actions provide an offset from the search area center
        Vector3 searchPoint = searchAreaCenter + new Vector3(offsetX * patrolRadius, offsetY * patrolRadius, 0);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(searchPoint, out hit, patrolRadius, NavMesh.AllAreas))
        {
            SetNavMeshDestination(hit.position);
        }
        else
        {
            // If the sampled point is invalid, try to move to the center of the search area
            if (NavMesh.SamplePosition(searchAreaCenter, out hit, patrolRadius, NavMesh.AllAreas))
            {
                SetNavMeshDestination(hit.position);
            }
        }

        // Could also incorporate a timer to transition out of search if no success
        // searchTimer -= Time.deltaTime;
        // if (searchTimer <= 0 && !isTargetInSight && !isSoundDetected) { // Transition to Patrol }
    }


    // --- Internal Navigation & Status Update Logic (from EnemyNavM & EnemyNPC) ---

    private void StatusUpdate()
    {
        if (targetObj == null || enemyVision == null || enemyHearing == null) return;

        agentPos = transform.position;
        targetPos = targetObj.transform.position;

        // Update detection status
        isTargetInSight = IsPlayerInSight();
        isSoundDetected = enemyHearing.CanHearPlayer(agentPos, targetPos);

        // Update player memory
        if (isSoundDetected || isTargetInSight)
        {
            currentMemoryTimer = memoryDuration;
            hasPlayerMemory = true;
            lastKnownPlayerPosition = targetPos; // Update last known position
        }

        if (hasPlayerMemory)
        {
            currentMemoryTimer -= Time.deltaTime;
            if (currentMemoryTimer <= 0)
            {
                hasPlayerMemory = false;
            }
        }
    }

    private void HandleTensionMeter()
    {
        if (targetObj == null) return;

        float distance = Vector3.Distance(agentPos, targetPos);
        float distanceFactor = Mathf.Clamp01(1f - (distance / 5f)); // Closer means higher factor

        if (isSoundDetected || isTargetInSight || hasPlayerMemory) // Tension fills if player detected or remembered
        {
            if (distance < 3f) // Max tension if very close
                tensionMeter = maxTensionMeter;
            else
                tensionMeter += Time.deltaTime * fillSpeed * distanceFactor;
        }
        else if (tensionMeter > 0) // Tension drains if no detection/memory
        {
            tensionMeter -= Time.deltaTime * drainSpeed;
        }

        tensionMeter = Mathf.Clamp(tensionMeter, 0f, maxTensionMeter);
    }

    public bool IsTensionMeterFull()
    {
        return tensionMeter >= maxTensionMeter;
    }

    // New NavMesh destination setting method that also triggers rotation
    private void SetNavMeshDestination(Vector3 destination)
    {
        if (agent == null) return;

        currentNavMeshDestination = destination; // Store the desired destination
        if (agent.SetDestination(destination)) // Try to set path
        {
            // Only set to rotating if path is successfully set
            movementState = MovementState.Rotating;
            agent.isStopped = true; // Stop movement until rotation is complete
        }
        else
        {
            // If path could not be set, consider agent idle or stuck
            movementState = MovementState.Idle;
            // Optionally, penalize agent or trigger TryAlternativePath here
        }
    }

    private void RotateTowardsDestination()
    {
        if (agent == null || !agent.hasPath || agent.pathPending || agent.path.corners.Length < 2)
        {
            // If no valid path or pending, stay idle
            movementState = MovementState.Idle;
            return;
        }

        // Get the next corner in our path for precise direction
        Vector3 direction = agent.path.corners[1] - transform.position;

        // Calculate the target angle based on 2D sprite orientation (e.g., sprite facing up is 0 degrees)
        targetRotationAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        targetRotationAngle -= 90; // Adjust for sprite facing up by default

        // Get current rotation as an angle
        float currentAngle = transform.rotation.eulerAngles.z;

        // Find the shortest rotation path
        float angleDifference = Mathf.DeltaAngle(currentAngle, targetRotationAngle);

        // Check if rotation is complete
        if (Mathf.Abs(angleDifference) <= rotationCompleteThreshold)
        {
            movementState = MovementState.Moving; // Rotation is complete, start moving
            agent.isStopped = false; // Allow NavMeshAgent to move
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

    private void HandleMovementState()
    {
        if (agent == null) return;

        switch (movementState)
        {
            case MovementState.Idle:
                agent.isStopped = true;
                agent.speed = 0;
                break;

            case MovementState.Rotating:
                agent.isStopped = true; // Keep agent stopped during rotation
                RotateTowardsDestination();
                break;

            case MovementState.Moving:
                agent.isStopped = false; // Allow agent to move
                // Check for obstacles ahead while moving
                if (CheckForObstaclesAhead())
                {
                    movementState = MovementState.AvoidingObstacle;
                    agent.isStopped = true; // Stop immediately to avoid
                }
                break;

            case MovementState.AvoidingObstacle:
                // Obstacle detected, try to find an alternative path
                TryAlternativePath();
                movementState = MovementState.Rotating; // After finding path, rotate towards it
                break;

            case MovementState.Waiting:
                // Used for waiting at patrol points, agent is already stopped
                break;
        }
    }

    private bool CheckForObstaclesAhead()
    {
        if (agent == null || agent.velocity.magnitude < 0.1f)
            return false;

        Vector2 moveDir = new Vector2(agent.velocity.x, agent.velocity.y).normalized;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, moveDir,
            obstacleDetectionRange, obstaclesMask);

        // Only consider actual obstacles, not the player itself if it happens to be hit by raycast
        return hit.collider != null && !hit.collider.CompareTag("Player");
    }

    private void CheckIfStuck()
    {
        if (agent == null || agent.pathPending || agent.isStopped || movementState == MovementState.Waiting)
        {
            stuckTimer = 0f;
            lastPositionMagnitude = transform.position.magnitude;
            return;
        }

        // Check if agent hasn't moved significantly
        if (Mathf.Abs(transform.position.magnitude - lastPositionMagnitude) < stuckDistanceThreshold)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > stuckDetectionTime)
            {
                TryAlternativePath();
                stuckTimer = 0f;
                AddReward(-0.5f); // Significant penalty for being stuck
            }
        }
        else
        {
            stuckTimer = 0f;
            pathfindingAttempts = 0; // Reset attempts if movement is good
        }

        lastPositionMagnitude = transform.position.magnitude;
    }

    private void TryAlternativePath()
    {
        pathfindingAttempts++;

        if (pathfindingAttempts >= MAX_ATTEMPTS)
        {
            // If too many attempts, abandon current goal and find a new patrol point
            patrolPointSet = false; // Force search for new point
            SearchForPatrolPoint(0,0); // No offsets, just random point around current position
            pathfindingAttempts = 0; // Reset attempts
            AddReward(-0.2f); // Penalty for giving up on current path
            EndEpisode(); // Can also end episode if consistently stuck
            return;
        }

        // Try a slight random deviation from the current destination
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * 2f;
        Vector3 newTarget = currentNavMeshDestination + new Vector3(randomOffset.x, randomOffset.y, 0);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(newTarget, out hit, patrolRadius, NavMesh.AllAreas))
        {
            SetNavMeshDestination(hit.position);
        }
        else
        {
            // If random point is invalid, try a point near the agent's current location
            if (NavMesh.SamplePosition(agentPos + (Vector3)UnityEngine.Random.insideUnitCircle * 2f, out hit, patrolRadius, NavMesh.AllAreas))
            {
                SetNavMeshDestination(hit.position);
            }
        }
    }

    private void SearchForPatrolPoint(float offsetX, float offsetY)
    {
        // 30% chance to return to starting position if we're far from it and player not in sight/memory
        if (Vector3.Distance(transform.position, startPosition) > patrolRadius * 2f &&
            UnityEngine.Random.value < 0.3f && !isTargetInSight && !hasPlayerMemory)
        {
            SetNavMeshDestination(startPosition);
            patrolPoint = startPosition;
            patrolPointSet = true;
            return;
        }

        // Generate a random point within patrol radius, potentially influenced by ML-Agent's offsets
        Vector2 randomDirection = UnityEngine.Random.insideUnitCircle * patrolRadius;
        Vector3 targetCandidate = transform.position + new Vector3(randomDirection.x + offsetX * patrolRadius,
                                                                   randomDirection.y + offsetY * patrolRadius,
                                                                   0);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetCandidate, out hit, patrolRadius, NavMesh.AllAreas))
        {
            patrolPoint = hit.position;
            SetNavMeshDestination(patrolPoint);
            patrolPointSet = true;
        }
        else
        {
            // If ML-Agent's suggestion is off-NavMesh, try a purely random point
            Vector3 fallbackPoint = transform.position + (Vector3)UnityEngine.Random.insideUnitCircle * patrolRadius;
            if (NavMesh.SamplePosition(fallbackPoint, out hit, patrolRadius, NavMesh.AllAreas))
            {
                patrolPoint = hit.position;
                SetNavMeshDestination(patrolPoint);
                patrolPointSet = true;
            }
            else
            {
                // If all fails, simply try setting destination to current position to stop trying to move
                SetNavMeshDestination(transform.position);
                patrolPointSet = false; // No valid patrol point found
            }
        }
    }

    private bool IsPlayerInSight()
    {
        if (targetObj == null) return false;

        Vector2 directionToPlayer = targetObj.transform.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > suspiciousRange)
            return false;

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

        // True if nothing blocks the view OR if the first thing hit is the player
        if (hit.collider == null || hit.collider.CompareTag("Player"))
        {
            return true;
        }

        return false;
    }


    // --- Gizmos for Editor Visualization ---
    private void OnDrawGizmosSelected()
    {
        // Ranges
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, suspiciousRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, combatRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);

        // Field of view
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Vector3 leftDir = Quaternion.Euler(0, 0, fieldOfViewAngle * 0.5f) * transform.up;
        Vector3 rightDir = Quaternion.Euler(0, 0, -fieldOfViewAngle * 0.5f) * transform.up;
        Gizmos.DrawLine(transform.position, transform.position + leftDir * suspiciousRange);
        Gizmos.DrawLine(transform.position, transform.position + rightDir * suspiciousRange);

        // Obstacle detection ray
        Gizmos.color = Color.cyan;
        if (agent != null && agent.velocity.magnitude > 0.1f)
        {
            Vector2 moveDir = new Vector2(agent.velocity.x, agent.velocity.y).normalized;
            Gizmos.DrawLine(transform.position, transform.position +
                           new Vector3(moveDir.x, moveDir.y, 0) * obstacleDetectionRange);
        }

        // Sightline to player if in sight
        if (isTargetInSight && targetObj != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetObj.transform.position);
        }

        // NavMesh Agent's current path
        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.cyan;
            Vector3[] pathCorners = agent.path.corners;
            for (int i = 0; i < pathCorners.Length - 1; i++)
            {
                Gizmos.DrawLine(pathCorners[i], pathCorners[i + 1]);
            }
        }

        // Current AI State text
        if (Application.isPlaying)
        {
            // Debug.DrawRay and Handles.Label for text requires UnityEditor, which can't be used in runtime build.
            // For runtime debugging, you might use UI Text elements.
            // Gizmos.color = Color.white;
            // UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, $"State: {currentAIState}");
            // UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f, $"MovState: {movementState}");
        }
    }
}

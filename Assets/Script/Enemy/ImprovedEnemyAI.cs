using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.AI;

// This script now requires both Agent and NavMeshAgent components
[RequireComponent(typeof(NavMeshAgent), typeof(EnemyStats), typeof(EnemyShoot))]
public class EnemyNPC : Agent
{
    [Header("Target Reference")]
    [SerializeField] private Transform target;

    [Header("Movement Settings")]
    [Tooltip("How far the agent tries to move each step. This acts like a 'look ahead' distance for the NavMesh.")]
    [SerializeField] private float moveDistance = 2f;
    [Tooltip("How fast the agent rotates to face the player.")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Combat Settings")]
    [Tooltip("The ideal distance to keep from the player.")]
    [SerializeField] private float optimalDistance = 8f;
    [Tooltip("The acceptable range around the optimal distance.")]
    [SerializeField] private float distanceTolerance = 2f;

    // --- Component References ---
    private NavMeshAgent navAgent;
    private EnemyStats enemyStats;
    private EnemyShoot enemyShoot;
    private Transform agentTransform;

    // --- State Tracking ---
    private bool canSeePlayer;


    public override void Initialize()
    {
        // Get all necessary components
        navAgent = GetComponent<NavMeshAgent>();
        enemyStats = GetComponent<EnemyStats>();
        enemyShoot = GetComponent<EnemyShoot>();
        agentTransform = transform;

        if (target == null)
        {
            target = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        // Configure NavMeshAgent for our purpose
        // We handle rotation manually, so we disable the agent's auto-rotation.
        navAgent.updateRotation = false;
        navAgent.updateUpAxis = false;
    }

    public override void OnEpisodeBegin()
    {
        // Reset the agent's position and state if needed for training
        // For gameplay, you might handle this differently (e.g., at spawn)
        navAgent.Warp(agentTransform.position); // Teleport agent without pathfinding issues
        enemyStats.health = enemyStats.maxHealth; // Reset health
    }

    /// <summary>
    /// Gathers all the information the agent needs to make a decision.
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        if (target == null)
        {
            sensor.AddObservation(new float[10]); // Add empty data if no target
            return;
        }

        // --- Observations ---
        // 1. Agent's own state (Health, Position)
        sensor.AddObservation(enemyStats.health / enemyStats.maxHealth);
        sensor.AddObservation(agentTransform.position);

        // 2. Relationship to Target (Player)
        Vector3 dirToTarget = (target.position - agentTransform.position).normalized;
        sensor.AddObservation(dirToTarget);
        float distanceToTarget = Vector3.Distance(agentTransform.position, target.position);
        sensor.AddObservation(distanceToTarget);

        // 3. Can the agent see the player? (Raycast check)
        canSeePlayer = CanSeeTarget();
        sensor.AddObservation(canSeePlayer);

        // 4. Agent's current velocity (helps it understand momentum)
        sensor.AddObservation(navAgent.velocity.normalized);

        // 5. Is the agent currently able to shoot? (based on cooldown)
        sensor.AddObservation(enemyShoot.CanShoot());
    }

    /// <summary>
    /// Receives an action from the model and executes it.
    /// </summary>
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (target == null) return;

        // --- Step 1: Handle Rotation ---
        // The agent should always try to face the player during combat.
        RotateTowards(target.position);

        // --- Step 2: Interpret Actions from the Neural Network ---
        var discreteActions = actions.DiscreteActions;

        // Action 0: Movement (0=Idle, 1=Forward, 2=Backward)
        // Action 1: Strafing (0=Idle, 1=Left, 2=Right)
        // Action 2: Shooting (0=Don't Shoot, 1=Shoot)
        int moveAction = discreteActions[0];
        int strafeAction = discreteActions[1];
        bool shootAction = discreteActions[2] == 1;

        // --- Step 3: Calculate the Desired Movement Direction ---
        Vector3 moveDirection = Vector3.zero;

        if (moveAction == 1) moveDirection += agentTransform.forward;
        if (moveAction == 2) moveDirection -= agentTransform.forward;
        if (strafeAction == 1) moveDirection -= agentTransform.right;
        if (strafeAction == 2) moveDirection += agentTransform.right;

        // --- Step 4: Command the NavMeshAgent ---
        // The ML-Agent decides a direction, and we tell the NavMeshAgent to go there.
        // This is the core of the combination: ML choice, NavMesh execution.
        if (moveDirection != Vector3.zero)
        {
            Vector3 destination = agentTransform.position + moveDirection.normalized * moveDistance;
            navAgent.SetDestination(destination);
        }

        // --- Step 5: Execute Shooting Action ---
        if (shootAction && canSeePlayer)
        {
            bool shotFired = enemyShoot.TryShoot(); // TryShoot returns true if a shot was fired
            if (shotFired)
            {
                AddReward(0.2f); // Positive reward for taking a valid shot
            }
        }

        // --- Step 6: Define Rewards to Guide Learning ---
        float distance = Vector3.Distance(agentTransform.position, target.position);

        // Encourage staying at the optimal combat distance
        if (distance > optimalDistance - distanceTolerance && distance < optimalDistance + distanceTolerance)
        {
            AddReward(0.01f);
        }
        else
        {
            // Penalize for being too close or too far
            AddReward(-0.005f);
        }

        // Small penalty for every step to encourage efficiency
        AddReward(-1f / MaxStep);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions.Clear();

        // Movement
        discreteActions[0] = 0; // Idle
        if (Input.GetKey(KeyCode.W)) discreteActions[0] = 1; // Forward
        if (Input.GetKey(KeyCode.S)) discreteActions[0] = 2; // Backward

        // Strafing
        discreteActions[1] = 0; // Idle
        if (Input.GetKey(KeyCode.A)) discreteActions[1] = 1; // Left
        if (Input.GetKey(KeyCode.D)) discreteActions[1] = 2; // Right

        // Shooting
        discreteActions[2] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    /// <summary>
    /// Smoothly rotates the agent to face a target position.
    /// </summary>
    private void RotateTowards(Vector3 lookPosition)
    {
        Vector3 direction = (lookPosition - agentTransform.position).normalized;
        direction.z = 0; // Assuming a 2D plane, ignore Z for rotation
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            agentTransform.rotation = Quaternion.Slerp(agentTransform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    /// <summary>
    /// Checks for a clear line of sight to the target.
    /// </summary>
    private bool CanSeeTarget()
    {
        if (target == null) return false;
        
        // You can add a more sophisticated Field of View check here if needed.
        // For now, we'll use a simple raycast.
        RaycastHit hit;
        Vector3 direction = target.position - agentTransform.position;
        if (Physics.Raycast(agentTransform.position, direction.normalized, out hit, 100f))
        {
            if (hit.transform == target)
            {
                return true; // The first thing we hit was the player
            }
        }
        return false;
    }
}
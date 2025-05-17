using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random; // Explicitly use UnityEngine.Random

public class AgentTrainingSmthSmth : Agent
{
    [Header("Agent Settings")]
    [SerializeField] public float tensionMeter;
    [SerializeField] public float maxTensionMeter = 10f; // Example value
    [SerializeField] private float fillSpeed = 0.5f;
    [SerializeField] private float drainSpeed = 0.2f;
    private EnemyStats enemyStats;

    private float lastTensionMeter = 0f;

    // private EnemyVision enemyVision; // REMOVED
    private EnemyHearing enemyHearing;
    private EnemyMovement enemyMovement;

    private Transform playerTransform; // Should be assigned, e.g., via FindGameObjectWithTag

    [Header("Training Settings - Spawning (Optional)")]
    [SerializeField] private Transform[] agentSpawnPoints; // Assign in Inspector if you want random spawning
    [SerializeField] private Transform[] playerSpawnPoints; // Assign if player also respawns
    [SerializeField] private float sightDetectionRange = 10f; // ADDED: Replacement for enemyVision.detectionRange

    // --- ADD THESE MISSING FIELD DECLARATIONS ---
    [Header("Shooting Mechanics")]
    public GameObject bulletPrefab;
    public Transform bulletSpawnPoint; // Assign the point from where bullets are fired
    public float bulletSpeed = 10f;
    public float baseShootingRange = 10f;
    public int bulletDamage = 10;
    public ParticleSystem muzzleFlash;
    public AudioSource shootAudioSource; // Assign an AudioSource component
    public AudioClip shootSound;
    private float nextFireTime = 0f;
    public LayerMask lineOfSightMask; // Set this in inspector to layers that block LoS (e.g., Walls, Obstacles)

    [Header("Confidence/Fear Modifiers")]
    [Range(0f, 1f)] public float accuracyFearPenaltyFactor = 0.5f; // Max % accuracy reduction due to fear
    [Range(0f, 1f)] public float rateOfFireFearPenaltyFactor = 0.3f; // Max % RoF reduction due to fear
    [Range(0f, 1f)] public float rateOfFireAggressionBonusFactor = 0.2f; // Max % RoF increase due to high health/low fear
    // --- END OF MISSING FIELD DECLARATIONS ---

    private bool isGettingShot = false;

    bool isTargetInSight = false; // This will need to be set by other means now
    bool isSoundDetected = false;

    Vector3 agentPos;
    Vector3 targetPos;

    private float fallbackTimer = 0f;

    // Fields for training logic similar to AgentTraining.cs
    private float lastRotation = 0f;
    private const float spinPenaltyThreshold = 45f; // Degrees
    private const float spinPenaltyAmount = -0.05f; // Adjusted penalty
    private float stuckTimer = 0f;
    private const float noMoveThreshold = 2f; // Seconds
    private const float stuckPenaltyAmount = -0.1f;

    // Action Index for clarity
    private const int ShootActionIndex = 0; // Assuming this will be our first discrete action branch


    public override void Initialize()
    {
        enemyMovement = GetComponent<EnemyMovement>();
        enemyHearing = GetComponent<EnemyHearing>();
        // enemyVision = GetComponent<EnemyVision>(); // REMOVED
        enemyStats = GetComponent<EnemyStats>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            // if (enemyVision != null) enemyVision.SetTarget(playerObj); // REMOVED
        }
        else
        {
            Debug.LogError("Player not found! Assign playerTransform or ensure Player tag exists.");
        }

        if (bulletSpawnPoint == null) Debug.LogError("Bullet Spawn Point not assigned!", this);
        if (bulletPrefab == null) Debug.LogError("Bullet Prefab not assigned!", this);
        // Ensure an AudioSource component exists if shootSound is used
        if (shootSound != null && shootAudioSource == null)
        {
            shootAudioSource = GetComponent<AudioSource>();
            if (shootAudioSource == null) shootAudioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public override void OnEpisodeBegin()
    {
        // Reset agent state
        isGettingShot = false;
        fallbackTimer = 0f;
        tensionMeter = 0f;
        lastTensionMeter = 0f;
        stuckTimer = 0f;

        if (enemyStats != null)
        {
            enemyStats.health = enemyStats.maxHealth;
            enemyStats.fearLevel = 0f;
            // Reset other stats if needed
        }

        // Optional: Reset position and rotation if spawn points are defined
        if (agentSpawnPoints != null && agentSpawnPoints.Length > 0)
        {
            transform.position = agentSpawnPoints[Random.Range(0, agentSpawnPoints.Length)].position;
            transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
        }
        if (playerTransform != null && playerSpawnPoints != null && playerSpawnPoints.Length > 0)
        {
            playerTransform.position = playerSpawnPoints[Random.Range(0, playerSpawnPoints.Length)].position;
        }

        nextFireTime = Time.time; // Reset fire time
    }


    void Update()
    {
        if (playerTransform == null) return;

        agentPos = transform.position;
        targetPos = playerTransform.position;

        // --- Line of Sight Check ---
        isTargetInSight = false;
        if (bulletSpawnPoint != null) 
        {
            Vector3 directionToPlayer = (playerTransform.position - bulletSpawnPoint.position).normalized;
            float distanceToPlayer = Vector3.Distance(bulletSpawnPoint.position, playerTransform.position);

            if (distanceToPlayer <= sightDetectionRange) 
            {
                RaycastHit2D hit = Physics2D.Raycast(bulletSpawnPoint.position, directionToPlayer, distanceToPlayer, lineOfSightMask);
                if (hit.collider == null) 
                {
                    RaycastHit2D playerHit = Physics2D.Raycast(bulletSpawnPoint.position, directionToPlayer, distanceToPlayer, LayerMask.GetMask("Player")); 
                    if(playerHit.collider != null && playerHit.collider.transform == playerTransform)
                    {
                        isTargetInSight = true;
                    }
                }
                else if (hit.collider.transform == playerTransform) 
                {
                     isTargetInSight = true;
                }
            }
        }
        // --- End Line of Sight Check ---

        if (enemyHearing != null) isSoundDetected = enemyHearing.CanHearPlayer(agentPos, targetPos);

        HandleTensionMeter();
        // TryToShoot(); // REMOVE: Agent will decide when to shoot via OnActionReceived

        if (isGettingShot)
        {
            fallbackTimer -= Time.deltaTime;
            FallbackFromPlayer();
            AddReward(0.01f); // Small reward for actively being in fallback state
            if (fallbackTimer <= 0f)
            {
                isGettingShot = false;
            }
        }
    }

    public void OnBulletHit()
    {
        isGettingShot = true;
        AddReward(-0.5f); // Penalty for getting hit

        if (enemyStats != null)
        {
            // EnemyStats should handle increasing fear internally, e.g., in its TakeDamage method
            // enemyStats.IncreaseFear(enemyStats.fearIncreaseOnHit); // Or call it here if not in TakeDamage
            fallbackTimer = enemyStats.fearLevel; // Use fear level for fallback duration
        }
        else
        {
            fallbackTimer = 1.5f; // Default fallback duration
        }
    }

    private void FallbackFromPlayer()
    {
        if (playerTransform == null) return;
        Vector3 directionAway = (agentPos - targetPos).normalized;
        float fallbackSpeed = enemyStats != null ? enemyStats.speed : 3f;
        // Using enemyMovement component if it handles actual movement
        if (enemyMovement != null)
        {
            // Assuming EnemyMovement has a method to move in a specific direction
            // This part needs to be adapted to how your EnemyMovement script works.
            // For simplicity, directly moving here, but ideally use EnemyMovement.
            // enemyMovement.MoveInDirection(directionAway, fallbackSpeed);
            transform.position += directionAway * fallbackSpeed * Time.deltaTime;
        }
        else
        {
            transform.position += directionAway * fallbackSpeed * Time.deltaTime;
        }
    }

    private void HandleTensionMeter()
    {
        if (playerTransform == null) return;
        float distance = Vector3.Distance(agentPos, targetPos);
        // MODIFIED: Using sightDetectionRange field instead of enemyVision.detectionRange
        float distanceFactor = Mathf.Clamp01(1f - (distance / sightDetectionRange)); 

        if (!isGettingShot) // Only manage tension aggressively if not falling back
        {
            if (isTargetInSight)
            {
                if (distance < 2f) // Very close
                {
                    tensionMeter = maxTensionMeter;
                    AddReward(0.02f); // Reward for being close and in sight
                }
                else
                {
                    tensionMeter += Time.deltaTime * fillSpeed * distanceFactor;
                    AddReward(0.005f * distanceFactor); // Small reward for seeing target
                }
            }
            else
            {
                tensionMeter -= Time.deltaTime * drainSpeed;
            }
            tensionMeter = Mathf.Clamp(tensionMeter, 0f, maxTensionMeter);
        }
    }

    // This method is now called when the agent decides to attempt a shot
    private void AttemptShootAction()
    {
        if (playerTransform == null || enemyStats == null || bulletPrefab == null || bulletSpawnPoint == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Calculate dynamic rate of fire
        float currentRateOfFire = enemyStats.rateOfFire;
        float fearFactorForRoF = (enemyStats.fearLevel / enemyStats.maxFearLevel);
        float healthFactorForRoF = (enemyStats.health / enemyStats.maxHealth);

        currentRateOfFire *= (1f - (fearFactorForRoF * rateOfFireFearPenaltyFactor));
        float aggressionMetric = healthFactorForRoF * (1f - fearFactorForRoF); 
        currentRateOfFire *= (1f + (aggressionMetric - 0.5f) * 2f * rateOfFireAggressionBonusFactor); 
        currentRateOfFire = Mathf.Max(0.1f, currentRateOfFire); 

        if (isTargetInSight && distanceToPlayer <= baseShootingRange && Time.time >= nextFireTime)
        {
            Shoot(); // Perform the actual shooting
            nextFireTime = Time.time + 1f / currentRateOfFire;
            AddReward(0.05f); // Small reward for attempting a valid shot
        }
        else
        {
            // Optional: Small penalty if agent tries to shoot but conditions aren't met
            // This can help it learn when NOT to try shooting.
            // AddReward(-0.02f);
        }
    }

    private void Shoot()
    {
        if (enemyStats == null) return;

        // Calculate dynamic accuracy
        float currentAccuracy = enemyStats.accuracy;
        float fearFactorForAccuracy = (enemyStats.fearLevel / enemyStats.maxFearLevel);
        currentAccuracy *= (1f - (fearFactorForAccuracy * accuracyFearPenaltyFactor));
        currentAccuracy = Mathf.Clamp01(currentAccuracy);

        if (Random.value > currentAccuracy)
        {
            // Missed shot - potentially a small negative reward or just no reward
            // AddReward(-0.01f); // Optional: penalty for missing
            return;
        }

        if (muzzleFlash != null) muzzleFlash.Play();
        if (shootAudioSource != null && shootSound != null) shootAudioSource.PlayOneShot(shootSound);

        Vector3 direction = (playerTransform.position - bulletSpawnPoint.position).normalized;
        GameObject bulletGO = Instantiate(bulletPrefab, bulletSpawnPoint.position, Quaternion.LookRotation(Vector3.forward, direction)); // Assuming 2D, rotate to face player

        Bullet bulletScript = bulletGO.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            // Pass this agent as the shooter so the bullet can report back if it hits
            bulletScript.Initialize(baseShootingRange, bulletDamage, Weapon.AmmoType.Kinetic, gameObject);
        }

        Rigidbody2D rb = bulletGO.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = direction * bulletSpeed;
        }
    }

    public void ReportPlayerHitByBullet()
    {
        AddReward(0.5f); // Base reward for hitting player

        // Additional rewards based on confidence/aggression
        if (enemyStats != null)
        {
            bool isConfident = (enemyStats.health / enemyStats.maxHealth > 0.7f) && (enemyStats.fearLevel / enemyStats.maxFearLevel < 0.3f);
            bool isFearful = (enemyStats.health / enemyStats.maxHealth < 0.4f) || (enemyStats.fearLevel / enemyStats.maxFearLevel > 0.6f);

            Vector3 directionToPlayer = (playerTransform.position - transform.position);
            float dotProductForward = Vector3.Dot(transform.up, directionToPlayer.normalized); // Assuming transform.up is forward for 2D

            if (isConfident && dotProductForward > 0.5f) // Confident and facing/moving towards player
            {
                AddReward(0.2f); // Bonus for aggressive hit
            }
            else if (isFearful && dotProductForward < -0.3f) // Fearful and facing/moving away from player (cautious hit)
            {
                AddReward(0.1f); // Bonus for cautious hit
            }
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Ensure playerTransform is not null before accessing its position
        Vector3 safeTargetPos = playerTransform != null ? playerTransform.position : Vector3.zero;

        sensor.AddObservation(transform.localPosition); 
        sensor.AddObservation(playerTransform != null ? playerTransform.localPosition : Vector3.zero);
        sensor.AddObservation(transform.localRotation.normalized); 

        sensor.AddObservation(playerTransform != null ? (safeTargetPos - transform.position).normalized : Vector3.zero);
        sensor.AddObservation(playerTransform != null ? Vector3.Distance(transform.position, safeTargetPos) : 0f);


        sensor.AddObservation(isTargetInSight ? 1f : 0f);
        sensor.AddObservation(isSoundDetected ? 1f : 0f);
        sensor.AddObservation(tensionMeter / maxTensionMeter); 
        sensor.AddObservation(IsTensionMeterFull() ? 1f : 0f);
        sensor.AddObservation(isGettingShot ? 1f : 0f); 

        if (enemyStats != null)
        {
            sensor.AddObservation(enemyStats.health / enemyStats.maxHealth); 
            sensor.AddObservation(enemyStats.fearLevel / enemyStats.maxFearLevel); 
            // Observation for readiness to shoot (based on cooldown)
            sensor.AddObservation(Time.time >= nextFireTime ? 1f : 0f);
        }
        else
        {
            sensor.AddObservation(1f); // Default health
            sensor.AddObservation(0f); // Default fear
            sensor.AddObservation(0f); // Default shoot readiness
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveAction = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f); 
        float lookAction = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        
        // NEW: Get discrete action for shooting
        int shootCommand = actions.DiscreteActions[ShootActionIndex]; // ShootActionIndex is 0

        if (enemyMovement != null)
        {
            enemyMovement.Move(moveAction, lookAction); 
        }

        // If agent decides to shoot
        if (shootCommand == 1)
        {
            AttemptShootAction();
        }

        // --- Rewards/Penalties based on AgentTraining.cs logic ---

        // Penalty for spinning in place
        float currentRotation = transform.localRotation.eulerAngles.z;
        float rotationDelta = Mathf.DeltaAngle(lastRotation, currentRotation);
        if (Mathf.Abs(rotationDelta) > spinPenaltyThreshold && Mathf.Abs(moveAction) < 0.1f) // Check if not moving significantly
        {
            AddReward(spinPenaltyAmount);
        }
        lastRotation = currentRotation;

        // Penalty for being stuck or idle when not seeing/hearing target and not falling back
        if (!isTargetInSight && !isSoundDetected && Mathf.Abs(moveAction) < 0.1f && !isGettingShot)
        {
            stuckTimer += Time.fixedDeltaTime; // Use fixedDeltaTime in fixed update contexts
            if (stuckTimer >= noMoveThreshold)
            {
                AddReward(stuckPenaltyAmount);
                // stuckTimer = 0f; // Optional: reset timer after penalty
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        // Small reward for any movement to discourage complete idleness
        // if (Mathf.Abs(moveAction) > 0.1f || Mathf.Abs(lookAction) > 0.1f)
        // {
        //     AddReward(0.001f);
        // }
        // else if (!isTargetInSight) // Penalize doing nothing only if no target
        // {
        //     AddReward(-0.005f); 
        // }


        // Fallback specific rewards
        if (isGettingShot)
        {
            if (Mathf.Abs(moveAction) > 0.1f)
            {
                // Check if moving away from player
                if (playerTransform != null)
                {
                    Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
                    Vector3 agentForward = transform.up; // Assuming 2D, agent's forward is its up vector
                    if (Vector3.Dot(agentForward * moveAction, directionToPlayer) < -0.3f) // Moving away
                    {
                        AddReward(0.1f); // Good: actively moving away during fallback
                    }
                    else
                    {
                        AddReward(0.05f); // Moving, but not directly away
                    }
                }
            }
            else
            {
                AddReward(-0.1f); // Bad: not moving during fallback
            }
        }
        else if (isTargetInSight && enemyStats != null) // Rewards related to engagement style when NOT falling back
        {
            bool isConfident = (enemyStats.health / enemyStats.maxHealth > 0.7f) && (enemyStats.fearLevel / enemyStats.maxFearLevel < 0.3f);
            float moveInput = actions.ContinuousActions[0]; // Assuming this is forward/backward movement

            if (isConfident && moveInput > 0.1f) // Confident and moving forward
            {
                AddReward(0.01f); // Small reward for aggressive positioning
            }
            // Penalize not shooting if target is in sight, conditions are met, and agent is confident
            if (isConfident && isTargetInSight && Time.time >= nextFireTime && shootCommand == 0)
            {
                AddReward(-0.05f); // Penalty for not taking a good shot opportunity
            }
        }

        // Update lastTensionMeter for next observation
        lastTensionMeter = tensionMeter;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var contActions = actionsOut.ContinuousActions;
        contActions[0] = Input.GetAxis("Vertical");   
        contActions[1] = -Input.GetAxis("Horizontal"); 

        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[ShootActionIndex] = Input.GetKey(KeyCode.Space) ? 1 : 0; // Press Space to shoot
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            // OnBulletHit() is called, which already applies a penalty.
            if (enemyStats != null)
            {
                // Assuming your Bullet script applies damage via TakeDamage, which then calls IncreaseFear
                // If not, you might need to get damage value from bullet and call:
                // enemyStats.TakeDamage(damageFromBullet);
            }
            OnBulletHit();
            // If the agent dies from this hit, end the episode
            if (enemyStats != null && enemyStats.health <= 0)
            {
                SetReward(-1.0f); // Additional penalty for dying
                EndEpisode();
            }
        }

        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.2f); // Penalty for hitting a wall
            // EndEpisode(); // Optional: can be too harsh, agent might learn to avoid walls too much
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            if (isGettingShot)
            {
                AddReward(-1.0f); 
            }
            else if (IsTensionMeterFull() && isTargetInSight)
            {
                // Consider if melee attack is possible/rewardable here
                // For now, just a general engagement outcome
                AddReward(0.1f); // Small reward for reaching player when aggressive
            }
            else
            {
                AddReward(-0.1f); 
            }
            EndEpisode();
        }
    }

    // Helper
    public bool IsTensionMeterFull()
    {
        return tensionMeter >= maxTensionMeter;
    }
}
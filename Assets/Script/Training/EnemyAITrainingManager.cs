using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

/// <summary>
/// Training Environment Manager for ImprovedEnemyAI
/// Handles environment setup, episode management, and training scenarios
/// </summary>
public class EnemyAITrainingManager : MonoBehaviour
{
    [Header("Training Environment")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform[] enemySpawnPoints;
    [SerializeField] private Transform[] objectivePoints;
    [SerializeField] private float episodeTimeLimit = 120f; // 2 minutes per episode
    
    [Header("Training Scenarios")]
    [SerializeField] private bool randomizePlayerPosition = true;
    [SerializeField] private bool randomizeEnemyPositions = true;
    [SerializeField] private bool enableMultipleEnemies = true;
    [SerializeField] private float scenarioVariationChance = 0.3f;
    
    [Header("Difficulty Progression")]
    [SerializeField] private bool enableCurriculumLearning = true;
    [SerializeField] private int easyPhaseSteps = 200000;
    [SerializeField] private int mediumPhaseSteps = 500000;
    
    private List<ImprovedEnemyAI> trainingAgents = new List<ImprovedEnemyAI>();
    private GameObject playerObject;
    private float episodeStartTime;
    private int currentEpisode = 0;
    
    // Training statistics
    private float totalReward = 0f;
    private int successfulDetections = 0;
    private int episodeCount = 0;

    void Start()
    {
        InitializeTrainingEnvironment();
    }

    void Update()
    {
        // Check for episode timeout
        if (Time.time - episodeStartTime > episodeTimeLimit)
        {
            EndEpisode("Timeout");
        }
        
        // Update training statistics
        UpdateTrainingStats();
    }

    /// <summary>
    /// Initialize the training environment
    /// </summary>
    private void InitializeTrainingEnvironment()
    {
        // Find player object
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
        }
        
        // Find all ImprovedEnemyAI agents in the scene
        ImprovedEnemyAI[] agents = FindObjectsOfType<ImprovedEnemyAI>();
        trainingAgents.AddRange(agents);
        
        Debug.Log($"Training Manager: Found {trainingAgents.Count} enemy agents for training");
        
        // Start first episode
        BeginNewEpisode();
    }

    /// <summary>
    /// Begin a new training episode
    /// </summary>
    public void BeginNewEpisode()
    {
        currentEpisode++;
        episodeStartTime = Time.time;
        
        Debug.Log($"Starting Episode {currentEpisode}");
        
        // Reset environment
        ResetEnvironment();
        
        // Apply curriculum learning if enabled
        if (enableCurriculumLearning)
        {
            ApplyCurriculumSettings();
        }
        
        // Randomize scenario if enabled
        if (Random.value < scenarioVariationChance)
        {
            RandomizeScenario();
        }
        
        // Reset all agents
        foreach (var agent in trainingAgents)
        {
            if (agent != null)
            {
                agent.OnEpisodeBegin();
            }
        }
    }

    /// <summary>
    /// End current episode with reason
    /// </summary>
    public void EndEpisode(string reason)
    {
        Debug.Log($"Episode {currentEpisode} ended: {reason}");
        
        // Calculate episode rewards
        CalculateEpisodeRewards();
        
        // Log statistics
        LogEpisodeStats();
        
        // Start new episode
        BeginNewEpisode();
    }

    /// <summary>
    /// Reset the training environment
    /// </summary>
    private void ResetEnvironment()
    {
        // Reset player position
        if (playerObject != null && playerSpawnPoint != null)
        {
            if (randomizePlayerPosition && enemySpawnPoints.Length > 0)
            {
                // Random spawn point
                Transform randomSpawn = enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length)];
                playerObject.transform.position = randomSpawn.position + Random.insideUnitSphere * 5f;
            }
            else
            {
                playerObject.transform.position = playerSpawnPoint.position;
            }
            
            // Reset player rotation
            playerObject.transform.rotation = Quaternion.identity;
        }
        
        // Reset enemy positions
        if (randomizeEnemyPositions)
        {
            for (int i = 0; i < trainingAgents.Count && i < enemySpawnPoints.Length; i++)
            {
                if (trainingAgents[i] != null && enemySpawnPoints[i] != null)
                {
                    Vector3 spawnPos = enemySpawnPoints[i].position + Random.insideUnitSphere * 3f;
                    trainingAgents[i].transform.position = spawnPos;
                    trainingAgents[i].transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                }
            }
        }
    }

    /// <summary>
    /// Apply curriculum learning based on training progress
    /// </summary>
    private void ApplyCurriculumSettings()
    {
        int totalSteps = Academy.Instance.TotalStepCount;
        
        if (totalSteps < easyPhaseSteps)
        {
            // Easy phase: Close spawns, single enemy
            ApplyEasySettings();
        }
        else if (totalSteps < mediumPhaseSteps)
        {
            // Medium phase: Normal spawns, detection challenges
            ApplyMediumSettings();
        }
        else
        {
            // Hard phase: Complex scenarios, multiple enemies
            ApplyHardSettings();
        }
    }

    private void ApplyEasySettings()
    {
        // Spawn player and enemy closer together
        if (playerObject != null && trainingAgents.Count > 0)
        {
            Vector3 playerPos = playerObject.transform.position;
            float closeDistance = Random.Range(5f, 15f);
            Vector3 enemyPos = playerPos + Random.insideUnitSphere * closeDistance;
            trainingAgents[0].transform.position = enemyPos;
        }
        
        // Disable extra enemies
        for (int i = 1; i < trainingAgents.Count; i++)
        {
            if (trainingAgents[i] != null)
            {
                trainingAgents[i].gameObject.SetActive(false);
            }
        }
    }

    private void ApplyMediumSettings()
    {
        // Normal spawning
        // Enable 1-2 enemies
        int activeEnemies = Random.Range(1, 3);
        for (int i = 0; i < trainingAgents.Count; i++)
        {
            if (trainingAgents[i] != null)
            {
                trainingAgents[i].gameObject.SetActive(i < activeEnemies);
            }
        }
    }

    private void ApplyHardSettings()
    {
        // Enable all enemies
        foreach (var agent in trainingAgents)
        {
            if (agent != null)
            {
                agent.gameObject.SetActive(true);
            }
        }
        
        // Add random objectives
        RandomizeObjectives();
    }

    /// <summary>
    /// Randomize training scenario
    /// </summary>
    private void RandomizeScenario()
    {
        float scenario = Random.value;
        
        if (scenario < 0.3f)
        {
            // Stealth scenario: Player tries to avoid detection
            SetupStealthScenario();
        }
        else if (scenario < 0.6f)
        {
            // Combat scenario: Direct confrontation
            SetupCombatScenario();
        }
        else
        {
            // Investigation scenario: Player makes noise then hides
            SetupInvestigationScenario();
        }
    }

    private void SetupStealthScenario()
    {
        // Place player far from enemies
        if (playerObject != null && trainingAgents.Count > 0)
        {
            Vector3 enemyAvgPos = Vector3.zero;
            int activeEnemies = 0;
            
            foreach (var agent in trainingAgents)
            {
                if (agent != null && agent.gameObject.activeInHierarchy)
                {
                    enemyAvgPos += agent.transform.position;
                    activeEnemies++;
                }
            }
            
            if (activeEnemies > 0)
            {
                enemyAvgPos /= activeEnemies;
                Vector3 farPosition = enemyAvgPos + Random.insideUnitSphere * 20f;
                playerObject.transform.position = farPosition;
            }
        }
    }

    private void SetupCombatScenario()
    {
        // Place player in direct line of sight
        if (playerObject != null && trainingAgents.Count > 0)
        {
            var firstEnemy = trainingAgents[0];
            if (firstEnemy != null)
            {
                Vector3 enemyPos = firstEnemy.transform.position;
                Vector3 combatPos = enemyPos + firstEnemy.transform.forward * 15f;
                playerObject.transform.position = combatPos;
            }
        }
    }

    private void SetupInvestigationScenario()
    {
        // Start with player visible, then they'll hide
        SetupCombatScenario();
        
        // Give enemies initial detection boost
        foreach (var agent in trainingAgents)
        {
            if (agent != null)
            {
                // Set initial detection level through reflection
                var detectionField = agent.GetType().GetField("currentDetectionLevel", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (detectionField != null)
                {
                    detectionField.SetValue(agent, 50f); // Start at 50% detection
                }
            }
        }
    }

    private void RandomizeObjectives()
    {
        if (objectivePoints.Length > 0)
        {
            Transform randomObjective = objectivePoints[Random.Range(0, objectivePoints.Length)];
            
            foreach (var agent in trainingAgents)
            {
                if (agent != null)
                {
                    // Set objective target through reflection
                    var objectiveField = agent.GetType().GetField("objectiveTarget", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (objectiveField != null)
                    {
                        objectiveField.SetValue(agent, randomObjective);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Calculate rewards for the current episode
    /// </summary>
    private void CalculateEpisodeRewards()
    {
        totalReward = 0f;
        successfulDetections = 0;
        
        foreach (var agent in trainingAgents)
        {
            if (agent != null)
            {
                totalReward += agent.GetCumulativeReward();
                
                if (agent.IsInCombat())
                {
                    successfulDetections++;
                }
            }
        }
    }

    /// <summary>
    /// Update training statistics
    /// </summary>
    private void UpdateTrainingStats()
    {
        // Update stats every few seconds
        if (Time.time % 5f < Time.deltaTime)
        {
            int detectingEnemies = 0;
            float avgDetectionLevel = 0f;
            
            foreach (var agent in trainingAgents)
            {
                if (agent != null)
                {
                    if (agent.IsAlerted())
                    {
                        detectingEnemies++;
                    }
                    avgDetectionLevel += agent.GetDetectionPercentage();
                }
            }
            
            if (trainingAgents.Count > 0)
            {
                avgDetectionLevel /= trainingAgents.Count;
            }
            
            // Log to console for monitoring
            if (detectingEnemies > 0)
            {
                Debug.Log($"Training Stats - Episode: {currentEpisode}, Detecting: {detectingEnemies}, Avg Detection: {avgDetectionLevel:F1}%");
            }
        }
    }

    /// <summary>
    /// Log episode statistics
    /// </summary>
    private void LogEpisodeStats()
    {
        episodeCount++;
        
        if (episodeCount % 10 == 0) // Log every 10 episodes
        {
            Debug.Log($"=== Episode {currentEpisode} Stats ===");
            Debug.Log($"Total Reward: {totalReward:F2}");
            Debug.Log($"Successful Detections: {successfulDetections}");
            Debug.Log($"Episode Duration: {Time.time - episodeStartTime:F1}s");
            Debug.Log($"Academy Total Steps: {Academy.Instance.TotalStepCount}");
        }
    }

    /// <summary>
    /// Public methods for external triggers
    /// </summary>
    public void TriggerPlayerDetected()
    {
        // Reward bonus for detection
        foreach (var agent in trainingAgents)
        {
            if (agent != null && agent.IsPlayerVisible())
            {
                agent.AddReward(0.5f);
            }
        }
    }

    public void TriggerPlayerEscaped()
    {
        // Small penalty for losing player
        foreach (var agent in trainingAgents)
        {
            if (agent != null)
            {
                agent.AddReward(-0.1f);
            }
        }
    }

    public void TriggerCombatEngagement()
    {
        // Reward for successful combat engagement
        foreach (var agent in trainingAgents)
        {
            if (agent != null && agent.IsInCombat())
            {
                agent.AddReward(0.3f);
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Enum to define the different AI types
public enum EnemyAIType
{
    FiniteStateMachine,
    NavMeshAgent,
    MLAgent,
    Combined // New option to spawn one of each type
}

public class EnemyManager : MonoBehaviour
{
    [Header("AI Selection")]
    [SerializeField] private EnemyAIType selectedAIType = EnemyAIType.FiniteStateMachine;
    [SerializeField] private GameObject finiteStateMachinePrefab;
    [SerializeField] private GameObject navMeshAgentPrefab;
    [SerializeField] private GameObject mlAgentPrefab;
    
    [Header("Enemy Settings")]
    public int maxEnemies = 5; // Maximum number of enemies to spawn
    public List<Transform> spawnPoints = new List<Transform>(); // List of spawn points for enemies

    private bool spawnPointsReady = false; // Flag to indicate if spawn points are ready
    private bool isFixedLevelSetup = false; // Flag to indicate if we've initialized based on Inspector values

    // Property to get the selected enemy prefab based on AI type
    private GameObject enemyPrefab
    {
        get
        {
            switch (selectedAIType)
            {
                case EnemyAIType.FiniteStateMachine:
                    return finiteStateMachinePrefab;
                case EnemyAIType.NavMeshAgent:
                    return navMeshAgentPrefab;
                case EnemyAIType.MLAgent:
                    return mlAgentPrefab;
                case EnemyAIType.Combined:
                    // For Combined mode, this property isn't used directly
                    // but we'll return FSM as a fallback
                    return finiteStateMachinePrefab;
                default:
                    return finiteStateMachinePrefab;
            }
        }
    }

    private IEnumerator Start()
    {
        // Validate the selected prefabs are assigned
        ValidatePrefabs();
        
        // Wait a frame. This gives LevelGenerator (if present) a chance to call InitializeSpawnPoints
        // during its own Start/Awake lifecycle.
        yield return null;

        // If LevelGenerator hasn't called InitializeSpawnPoints by now
        if (!spawnPointsReady)
        {
            // Check if spawnPoints were assigned in the Inspector (fixed level scenario)
            if (this.spawnPoints != null && this.spawnPoints.Count > 0)
            {
                Debug.Log("EnemyManager: No initialization from LevelGenerator. Using predefined spawn points for a fixed level.");
                spawnPointsReady = true;    // Mark as ready
                isFixedLevelSetup = true;   // Mark as a fixed setup
                SpawnEnemies();             // Spawn using Inspector-defined points
            }
            else
            {
                Debug.LogWarning("EnemyManager: Not initialized by LevelGenerator and no predefined spawn points found in Inspector. No enemies will be spawned unless InitializeSpawnPoints is called.");
            }
        }
        // If spawnPointsReady is true at this point, it means LevelGenerator called InitializeSpawnPoints,
        // which would have already triggered SpawnEnemies.
    }

    private void ValidatePrefabs()
    {
        if (selectedAIType == EnemyAIType.Combined)
        {
            // For Combined mode, check all prefabs
            if (finiteStateMachinePrefab == null)
                Debug.LogError("EnemyManager: FiniteStateMachinePrefab is not assigned");
            if (navMeshAgentPrefab == null)
                Debug.LogError("EnemyManager: NavMeshAgentPrefab is not assigned");
            if (mlAgentPrefab == null)
                Debug.LogError("EnemyManager: MLAgentPrefab is not assigned");
        }
        else if (enemyPrefab == null)
        {
            Debug.LogError($"EnemyManager: No prefab assigned for the selected AI type: {selectedAIType}");
        }
    }

    public void InitializeSpawnPoints(List<Transform> pointsFromGenerator)
    {
        // If EnemyManager.Start() already set up for a fixed level, don't let LevelGenerator override.
        if (isFixedLevelSetup)
        {
            Debug.LogWarning("EnemyManager: InitializeSpawnPoints called by LevelGenerator, but EnemyManager already initialized for a fixed level. Points from LevelGenerator will be ignored.");
            return;
        }

        // Clear any potentially pre-assigned (Inspector) points if LevelGenerator is providing them.
        this.spawnPoints.Clear();
        this.spawnPoints.AddRange(pointsFromGenerator);
        
        if (this.spawnPoints.Count > 0)
        {
            spawnPointsReady = true;
            Debug.Log($"EnemyManager: Received {this.spawnPoints.Count} spawn points from LevelGenerator.");
            SpawnEnemies(); // Trigger enemy spawning now that points are received
        }
        else
        {
            spawnPointsReady = false; // No valid points were actually provided
            Debug.LogWarning("EnemyManager: InitializeSpawnPoints called by LevelGenerator, but the provided list was empty or resulted in no valid spawn points.");
        }
    }

    private void SpawnEnemies()
    {
        if (!spawnPointsReady)
        {
            Debug.LogError("EnemyManager: Spawn points are not ready. Cannot spawn enemies!");
            return;
        }

        if (selectedAIType != EnemyAIType.Combined && enemyPrefab == null)
        {
            Debug.LogError($"EnemyManager: No prefab assigned for the selected AI type: {selectedAIType}");
            return;
        }

        if (this.spawnPoints.Count == 0)
        {
            Debug.LogError("EnemyManager: No spawn points available (either predefined or from LevelGenerator)!");
            return;
        }

        // Shuffle the spawn points to randomize placement
        List<Transform> shuffledSpawnPoints = new List<Transform>(this.spawnPoints);
        shuffledSpawnPoints.Sort((a, b) => Random.Range(-1, 2)); // Using your existing shuffle method

        // Handle combined mode differently
        if (selectedAIType == EnemyAIType.Combined)
        {
            SpawnCombinedEnemies(shuffledSpawnPoints);
        }
        else
        {
            // Standard spawning for a single AI type
            SpawnSingleTypeEnemies(shuffledSpawnPoints);
        }
    }

    private void SpawnSingleTypeEnemies(List<Transform> shuffledSpawnPoints)
    {
        int enemiesSpawned = 0;
        Debug.Log($"EnemyManager: Attempting to spawn up to {maxEnemies} {selectedAIType} enemies from {shuffledSpawnPoints.Count} available spawn points.");

        // Spawn enemies at random spawn points
        foreach (Transform spawnPoint in shuffledSpawnPoints)
        {
            if (enemiesSpawned >= maxEnemies)
            {
                break;
            }

            if (spawnPoint == null)
            {
                Debug.LogWarning("EnemyManager: A spawn point in the list is null. Skipping...");
                continue;
            }

            // Instantiate the enemy prefab at the spawn point
            GameObject enemyInstance = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            if (enemyInstance != null)
            {
                // Make sure each enemy has the EnemyStatistic component
                if (enemyInstance.GetComponent<EnemyStatistic>() == null)
                {
                    enemyInstance.AddComponent<EnemyStatistic>();
                }
                
                Debug.Log($"EnemyManager: {selectedAIType} enemy spawned at position: {spawnPoint.position}");
                enemiesSpawned++;
            }
            else
            {
                Debug.LogError($"EnemyManager: Failed to instantiate {selectedAIType} enemy prefab!");
            }
        }

        Debug.Log($"EnemyManager: Actually spawned {enemiesSpawned} {selectedAIType} enemies.");
    }

    private void SpawnCombinedEnemies(List<Transform> shuffledSpawnPoints)
    {
        int enemiesSpawned = 0;
        int spawnCount = Mathf.Min(shuffledSpawnPoints.Count, 3); // Need at least 3 spawn points for one of each type
        
        Debug.Log($"EnemyManager: Combined mode - attempting to spawn one of each AI type (total: 3) from {shuffledSpawnPoints.Count} available spawn points.");

        // Check if we have enough spawn points for one of each type
        if (shuffledSpawnPoints.Count < 3)
        {
            Debug.LogWarning($"EnemyManager: Combined mode requires at least 3 spawn points, but only {shuffledSpawnPoints.Count} available. Some AI types won't be spawned.");
        }

        // Spawn FSM Enemy
        if (finiteStateMachinePrefab != null && enemiesSpawned < spawnCount && enemiesSpawned < shuffledSpawnPoints.Count)
        {
            Transform spawnPoint = shuffledSpawnPoints[enemiesSpawned];
            GameObject enemyInstance = Instantiate(finiteStateMachinePrefab, spawnPoint.position, spawnPoint.rotation);
            if (enemyInstance != null)
            {
                if (enemyInstance.GetComponent<EnemyStatistic>() == null)
                {
                    enemyInstance.AddComponent<EnemyStatistic>();
                }
                Debug.Log($"EnemyManager: FSM enemy spawned at position: {spawnPoint.position}");
                enemiesSpawned++;
            }
        }

        // Spawn NavMesh Enemy
        if (navMeshAgentPrefab != null && enemiesSpawned < spawnCount && enemiesSpawned < shuffledSpawnPoints.Count)
        {
            Transform spawnPoint = shuffledSpawnPoints[enemiesSpawned];
            GameObject enemyInstance = Instantiate(navMeshAgentPrefab, spawnPoint.position, spawnPoint.rotation);
            if (enemyInstance != null)
            {
                if (enemyInstance.GetComponent<EnemyStatistic>() == null)
                {
                    enemyInstance.AddComponent<EnemyStatistic>();
                }
                Debug.Log($"EnemyManager: NavMesh enemy spawned at position: {spawnPoint.position}");
                enemiesSpawned++;
            }
        }

        // Spawn ML Agent Enemy
        if (mlAgentPrefab != null && enemiesSpawned < spawnCount && enemiesSpawned < shuffledSpawnPoints.Count)
        {
            Transform spawnPoint = shuffledSpawnPoints[enemiesSpawned];
            GameObject enemyInstance = Instantiate(mlAgentPrefab, spawnPoint.position, spawnPoint.rotation);
            if (enemyInstance != null)
            {
                if (enemyInstance.GetComponent<EnemyStatistic>() == null)
                {
                    enemyInstance.AddComponent<EnemyStatistic>();
                }
                Debug.Log($"EnemyManager: ML-Agent enemy spawned at position: {spawnPoint.position}");
                enemiesSpawned++;
            }
        }

        // Spawn additional enemies of random types if we still have maxEnemies > 3
        int additionalEnemies = maxEnemies - enemiesSpawned;
        if (additionalEnemies > 0 && enemiesSpawned < shuffledSpawnPoints.Count)
        {
            Debug.Log($"EnemyManager: Combined mode - spawning {additionalEnemies} additional random enemies.");
            
            for (int i = enemiesSpawned; i < shuffledSpawnPoints.Count && (i - enemiesSpawned) < additionalEnemies; i++)
            {
                Transform spawnPoint = shuffledSpawnPoints[i];
                
                // Randomly select which enemy type to spawn
                int randomType = Random.Range(0, 3);
                GameObject prefabToSpawn = null;
                string enemyType = "";
                
                switch (randomType)
                {
                    case 0:
                        prefabToSpawn = finiteStateMachinePrefab;
                        enemyType = "FSM";
                        break;
                    case 1:
                        prefabToSpawn = navMeshAgentPrefab;
                        enemyType = "NavMesh";
                        break;
                    case 2:
                        prefabToSpawn = mlAgentPrefab;
                        enemyType = "ML-Agent";
                        break;
                }
                
                if (prefabToSpawn != null)
                {
                    GameObject enemyInstance = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
                    if (enemyInstance != null)
                    {
                        if (enemyInstance.GetComponent<EnemyStatistic>() == null)
                        {
                            enemyInstance.AddComponent<EnemyStatistic>();
                        }
                        Debug.Log($"EnemyManager: Additional {enemyType} enemy spawned at position: {spawnPoint.position}");
                    }
                }
            }
        }

        Debug.Log($"EnemyManager: Combined mode - spawned a total of {enemiesSpawned} enemies ({Mathf.Min(enemiesSpawned, 3)} different types).");
    }

    // Method to change AI type at runtime (useful for debugging or game mechanics)
    public void ChangeAIType(EnemyAIType newType)
    {
        selectedAIType = newType;
        Debug.Log($"EnemyManager: AI type changed to {selectedAIType}. Note: This will only affect newly spawned enemies.");
    }

    public void OnEnemyKilled(GameObject enemy)
    {
        // Remove the enemy from the active enemies list
        if (spawnPoints.Contains(enemy.transform))
        {
            spawnPoints.Remove(enemy.transform);
        }

        // Destroy the enemy GameObject
        Destroy(enemy);

        Debug.Log("Enemy killed and removed from spawn points.");
    }
}

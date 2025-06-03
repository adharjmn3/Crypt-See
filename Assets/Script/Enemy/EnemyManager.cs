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
    [Tooltip("Fixed number of each enemy type to spawn in Combined mode (0 = one of each type)")]
    [SerializeField] private int fixedEnemiesPerType = 0; // Fixed number of each type to spawn in Combined mode
    public List<Transform> spawnPoints = new List<Transform>(); // List of spawn points for enemies
    
    // Dictionary to track which spawn points are currently in use
    private Dictionary<Transform, bool> spawnPointUsage = new Dictionary<Transform, bool>();
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    
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

        // Initialize the spawn point usage tracking
        InitializeSpawnPointTracking();

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

    private void InitializeSpawnPointTracking()
    {
        // Clear any previous data
        spawnPointUsage.Clear();
        spawnedEnemies.Clear();
        
        // Initialize all spawn points as unused
        foreach (Transform sp in spawnPoints)
        {
            if (sp != null)
            {
                spawnPointUsage[sp] = false;
            }
        }
    }

    private Transform GetAvailableSpawnPoint(List<Transform> shuffledSpawnPoints)
    {
        // First try to find an unused spawn point
        foreach (Transform sp in shuffledSpawnPoints)
        {
            if (sp != null && !spawnPointUsage[sp])
            {
                return sp;
            }
        }
        
        // If all spawn points are used, reuse one (round-robin)
        if (shuffledSpawnPoints.Count > 0)
        {
            return shuffledSpawnPoints[Random.Range(0, shuffledSpawnPoints.Count)];
        }
        
        return null;
    }

    private void MarkSpawnPointAsUsed(Transform spawnPoint)
    {
        if (spawnPoint != null && spawnPointUsage.ContainsKey(spawnPoint))
        {
            spawnPointUsage[spawnPoint] = true;
        }
    }

    private void SpawnSingleTypeEnemies(List<Transform> shuffledSpawnPoints)
    {
        int enemiesSpawned = 0;
        Debug.Log($"EnemyManager: Attempting to spawn up to {maxEnemies} {selectedAIType} enemies from {shuffledSpawnPoints.Count} available spawn points.");

        // If we need more enemies than spawn points, we'll need to reuse spawn points
        bool needToReuseSpawnPoints = maxEnemies > shuffledSpawnPoints.Count;
        if (needToReuseSpawnPoints)
        {
            Debug.Log($"EnemyManager: Need to spawn {maxEnemies} enemies but only have {shuffledSpawnPoints.Count} spawn points. Some spawn points will be reused.");
        }

        // Spawn enemies at random spawn points
        while (enemiesSpawned < maxEnemies)
        {
            Transform spawnPoint = GetAvailableSpawnPoint(shuffledSpawnPoints);
            
            if (spawnPoint == null)
            {
                Debug.LogWarning("EnemyManager: No valid spawn points available.");
                break;
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
                
                // Mark this spawn point as used and track the enemy
                MarkSpawnPointAsUsed(spawnPoint);
                spawnedEnemies.Add(enemyInstance);
                
                Debug.Log($"EnemyManager: {selectedAIType} enemy spawned at position: {spawnPoint.position}");
                enemiesSpawned++;
            }
            else
            {
                Debug.LogError($"EnemyManager: Failed to instantiate {selectedAIType} enemy prefab!");
            }
            
            // If we've used all spawn points but need more enemies, reset the usage tracking to allow reuse
            if (enemiesSpawned < maxEnemies && AllSpawnPointsUsed())
            {
                ResetSpawnPointUsage();
            }
        }

        Debug.Log($"EnemyManager: Actually spawned {enemiesSpawned} {selectedAIType} enemies.");
    }

    private bool AllSpawnPointsUsed()
    {
        foreach (var kvp in spawnPointUsage)
        {
            if (!kvp.Value) // If any spawn point is unused
                return false;
        }
        return true;
    }

    private void ResetSpawnPointUsage()
    {
        foreach (Transform sp in spawnPoints)
        {
            if (sp != null)
            {
                spawnPointUsage[sp] = false;
            }
        }
        Debug.Log("EnemyManager: All spawn points have been used. Resetting usage tracking to allow spawn point reuse.");
    }

    private void SpawnCombinedEnemies(List<Transform> shuffledSpawnPoints)
    {
        int enemiesSpawned = 0;
        
        if (fixedEnemiesPerType > 0)
        {
            // Fixed number mode
            int totalNeededEnemies = fixedEnemiesPerType * 3; // 3 types
            int targetEnemies = Mathf.Min(totalNeededEnemies, maxEnemies);
            
            Debug.Log($"EnemyManager: Combined mode - attempting to spawn {fixedEnemiesPerType} of each AI type " +
                      $"(target: {targetEnemies}, max: {maxEnemies}) from {shuffledSpawnPoints.Count} available spawn points.");
            
            // Check if we need to reuse spawn points
            bool needToReuseSpawnPoints = targetEnemies > shuffledSpawnPoints.Count;
            if (needToReuseSpawnPoints)
            {
                Debug.Log($"EnemyManager: Need to spawn {targetEnemies} enemies but only have {shuffledSpawnPoints.Count} spawn points. " +
                          $"Some spawn points will be reused.");
            }
            
            // Calculate how many of each type to spawn based on available maxEnemies
            int fsm = Mathf.Min(fixedEnemiesPerType, maxEnemies / 3 + (maxEnemies % 3 > 0 ? 1 : 0));
            int nav = Mathf.Min(fixedEnemiesPerType, maxEnemies / 3 + (maxEnemies % 3 > 1 ? 1 : 0));
            int ml = Mathf.Min(fixedEnemiesPerType, maxEnemies / 3);
            
            // Spawn FSM Enemies
            enemiesSpawned += SpawnFixedNumberOfEnemies(finiteStateMachinePrefab, "FSM", fsm, shuffledSpawnPoints, enemiesSpawned);
            
            // Reset spawn point usage if needed before spawning next type
            if (AllSpawnPointsUsed() && enemiesSpawned < targetEnemies)
            {
                ResetSpawnPointUsage();
            }
            
            // Spawn NavMesh Enemies
            enemiesSpawned += SpawnFixedNumberOfEnemies(navMeshAgentPrefab, "NavMesh", nav, shuffledSpawnPoints, enemiesSpawned);
            
            // Reset spawn point usage if needed before spawning next type
            if (AllSpawnPointsUsed() && enemiesSpawned < targetEnemies)
            {
                ResetSpawnPointUsage();
            }
            
            // Spawn ML-Agent Enemies
            enemiesSpawned += SpawnFixedNumberOfEnemies(mlAgentPrefab, "ML-Agent", ml, shuffledSpawnPoints, enemiesSpawned);
        }
        else
        {
            // Original behavior - one of each type
            int spawnCount = Mathf.Min(3, maxEnemies); // Up to 3 enemies (one of each type), but respect maxEnemies
            
            Debug.Log($"EnemyManager: Combined mode - attempting to spawn one of each AI type (max: {spawnCount}) from {shuffledSpawnPoints.Count} available spawn points.");

            // Check if we need to reuse spawn points
            if (spawnCount > shuffledSpawnPoints.Count)
            {
                Debug.Log($"EnemyManager: Need to spawn {spawnCount} enemies but only have {shuffledSpawnPoints.Count} spawn points. " +
                          $"Some spawn points will be reused.");
            }

            // Spawn FSM Enemy
            if (finiteStateMachinePrefab != null && enemiesSpawned < spawnCount)
            {
                Transform spawnPoint = GetAvailableSpawnPoint(shuffledSpawnPoints);
                if (spawnPoint != null)
                {
                    GameObject enemyInstance = Instantiate(finiteStateMachinePrefab, spawnPoint.position, spawnPoint.rotation);
                    if (enemyInstance != null)
                    {
                        if (enemyInstance.GetComponent<EnemyStatistic>() == null)
                        {
                            enemyInstance.AddComponent<EnemyStatistic>();
                        }
                        // Mark this spawn point as used and track the enemy
                        MarkSpawnPointAsUsed(spawnPoint);
                        spawnedEnemies.Add(enemyInstance);
                        
                        Debug.Log($"EnemyManager: FSM enemy spawned at position: {spawnPoint.position}");
                        enemiesSpawned++;
                    }
                }
            }

            // Reset spawn point usage if needed before spawning next type
            if (AllSpawnPointsUsed() && enemiesSpawned < spawnCount)
            {
                ResetSpawnPointUsage();
            }

            // Spawn NavMesh Enemy
            if (navMeshAgentPrefab != null && enemiesSpawned < spawnCount)
            {
                Transform spawnPoint = GetAvailableSpawnPoint(shuffledSpawnPoints);
                if (spawnPoint != null)
                {
                    GameObject enemyInstance = Instantiate(navMeshAgentPrefab, spawnPoint.position, spawnPoint.rotation);
                    if (enemyInstance != null)
                    {
                        if (enemyInstance.GetComponent<EnemyStatistic>() == null)
                        {
                            enemyInstance.AddComponent<EnemyStatistic>();
                        }
                        // Mark this spawn point as used and track the enemy
                        MarkSpawnPointAsUsed(spawnPoint);
                        spawnedEnemies.Add(enemyInstance);
                        
                        Debug.Log($"EnemyManager: NavMesh enemy spawned at position: {spawnPoint.position}");
                        enemiesSpawned++;
                    }
                }
            }

            // Reset spawn point usage if needed before spawning next type
            if (AllSpawnPointsUsed() && enemiesSpawned < spawnCount)
            {
                ResetSpawnPointUsage();
            }

            // Spawn ML Agent Enemy
            if (mlAgentPrefab != null && enemiesSpawned < spawnCount)
            {
                Transform spawnPoint = GetAvailableSpawnPoint(shuffledSpawnPoints);
                if (spawnPoint != null)
                {
                    GameObject enemyInstance = Instantiate(mlAgentPrefab, spawnPoint.position, spawnPoint.rotation);
                    if (enemyInstance != null)
                    {
                        if (enemyInstance.GetComponent<EnemyStatistic>() == null)
                        {
                            enemyInstance.AddComponent<EnemyStatistic>();
                        }
                        // Mark this spawn point as used and track the enemy
                        MarkSpawnPointAsUsed(spawnPoint);
                        spawnedEnemies.Add(enemyInstance);
                        
                        Debug.Log($"EnemyManager: ML-Agent enemy spawned at position: {spawnPoint.position}");
                        enemiesSpawned++;
                    }
                }
            }
        }

        // Spawn additional enemies of random types if we still have maxEnemies > enemies spawned
        int additionalEnemies = maxEnemies - enemiesSpawned;
        if (additionalEnemies > 0)
        {
            Debug.Log($"EnemyManager: Combined mode - spawning {additionalEnemies} additional random enemies.");
            
            // Reset spawn point usage if all points are used
            if (AllSpawnPointsUsed())
            {
                ResetSpawnPointUsage();
            }
            
            for (int i = 0; i < additionalEnemies; i++)
            {
                Transform spawnPoint = GetAvailableSpawnPoint(shuffledSpawnPoints);
                if (spawnPoint == null)
                {
                    Debug.LogWarning("EnemyManager: No valid spawn points available for additional enemies.");
                    break;
                }
                
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
                        // Mark this spawn point as used and track the enemy
                        MarkSpawnPointAsUsed(spawnPoint);
                        spawnedEnemies.Add(enemyInstance);
                        
                        Debug.Log($"EnemyManager: Additional {enemyType} enemy spawned at position: {spawnPoint.position}");
                        enemiesSpawned++;
                    }
                }
                
                // Reset spawn point usage if all points are used and we still need more enemies
                if (AllSpawnPointsUsed() && i < additionalEnemies - 1)
                {
                    ResetSpawnPointUsage();
                }
            }
        }

        if (fixedEnemiesPerType > 0)
        {
            Debug.Log($"EnemyManager: Combined mode - spawned a total of {enemiesSpawned} enemies (fixed {fixedEnemiesPerType} per type).");
        }
        else
        {
            Debug.Log($"EnemyManager: Combined mode - spawned a total of {enemiesSpawned} enemies ({Mathf.Min(enemiesSpawned, 3)} different types).");
        }
    }

    // Helper method to spawn a fixed number of a specific enemy type
    private int SpawnFixedNumberOfEnemies(GameObject prefab, string enemyTypeName, int count, 
                                         List<Transform> spawnPoints, int currentTotalSpawned)
    {
        int spawned = 0;
        
        if (prefab == null)
        {
            Debug.LogError($"EnemyManager: {enemyTypeName} prefab is null. Cannot spawn {count} enemies of this type.");
            return 0;
        }
        
        for (int i = 0; i < count; i++)
        {
            // Check if we've hit the max enemies limit
            if (currentTotalSpawned + spawned >= maxEnemies)
            {
                Debug.LogWarning($"EnemyManager: Reached max enemies limit ({maxEnemies}). Stopping spawn of {enemyTypeName}.");
                break;
            }
            
            // Get an available spawn point
            Transform spawnPoint = GetAvailableSpawnPoint(spawnPoints);
            if (spawnPoint == null)
            {
                Debug.LogWarning($"EnemyManager: No valid spawn points available for {enemyTypeName} enemy.");
                break;
            }
            
            GameObject enemyInstance = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            
            if (enemyInstance != null)
            {
                if (enemyInstance.GetComponent<EnemyStatistic>() == null)
                {
                    enemyInstance.AddComponent<EnemyStatistic>();
                }
                // Mark this spawn point as used and track the enemy
                MarkSpawnPointAsUsed(spawnPoint);
                spawnedEnemies.Add(enemyInstance);
                
                Debug.Log($"EnemyManager: {enemyTypeName} enemy {i+1}/{count} spawned at position: {spawnPoint.position}");
                spawned++;
                
                // If all spawn points are used but we need more enemies, reset usage
                if (i < count - 1 && AllSpawnPointsUsed())
                {
                    ResetSpawnPointUsage();
                }
            }
            else
            {
                Debug.LogError($"EnemyManager: Failed to instantiate {enemyTypeName} enemy!");
            }
        }
        
        return spawned;
    }

    // Method to change AI type at runtime (useful for debugging or game mechanics)
    public void ChangeAIType(EnemyAIType newType)
    {
        selectedAIType = newType;
        Debug.Log($"EnemyManager: AI type changed to {selectedAIType}. Note: This will only affect newly spawned enemies.");
    }

    public void OnEnemyKilled(GameObject enemy)
    {
        // Remove the enemy from our tracking
        if (spawnedEnemies.Contains(enemy))
        {
            spawnedEnemies.Remove(enemy);
        }

        // Destroy the enemy GameObject
        Destroy(enemy);

        Debug.Log("Enemy killed and removed from spawned enemies.");
    }
    
    // Method to get the count of currently spawned enemies
    public int GetSpawnedEnemyCount()
    {
        return spawnedEnemies.Count;
    }
    
    // Method to get the count of enemies by specific type
    public int GetSpawnedEnemyCountByType(EnemyAIType type)
    {
        int count = 0;
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy == null) continue;
            
            switch (type)
            {
                case EnemyAIType.FiniteStateMachine:
                    if (enemy.GetComponent<EnemyAIFSM>() != null)
                        count++;
                    break;
                case EnemyAIType.NavMeshAgent:
                    if (enemy.GetComponent<EnemyNavM>() != null)
                        count++;
                    break;
                case EnemyAIType.MLAgent:
                    if (enemy.GetComponent<EnemyNPC>() != null)
                        count++;
                    break;
            }
        }
        return count;
    }

    // New methods to add to your existing EnemyManager class

    // Get a copy of all spawned enemies
    public List<GameObject> GetSpawnedEnemies()
    {
        return new List<GameObject>(spawnedEnemies);
    }

    // Despawn a specific enemy
    public void DespawnEnemy(GameObject enemy)
    {
        if (enemy == null) return;
        
        // Remove from tracking
        if (spawnedEnemies.Contains(enemy))
        {
            spawnedEnemies.Remove(enemy);
        }
        
        // Destroy the enemy GameObject
        Destroy(enemy);
    }

    // Set enemy spawn parameters
    public void SetEnemySpawnParameters(EnemyAIType aiType, int newMaxEnemies, int newFixedEnemiesPerType)
    {
        // Update AI type
        selectedAIType = aiType;
        
        // Update max enemies
        maxEnemies = newMaxEnemies;
        
        // Update fixed enemies per type for combined mode
        fixedEnemiesPerType = newFixedEnemiesPerType;
        
        Debug.Log($"EnemyManager: Parameters updated - AI Type: {selectedAIType}, Max Enemies: {maxEnemies}, Fixed per type: {fixedEnemiesPerType}");
    }

    // Respawn enemies with current settings
    public void RespawnEnemies()
    {
        // Clear any existing tracking
        InitializeSpawnPointTracking();
        
        // Spawn enemies using current settings
        SpawnEnemies();
    }
}
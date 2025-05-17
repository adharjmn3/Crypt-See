using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject enemyPrefab; // The enemy prefab to spawn
    public List<Transform> spawnPoints = new List<Transform>(); // List of spawn points for enemies
    public int maxEnemies = 5; // Maximum number of enemies to spawn

    private bool spawnPointsReady = false; // Flag to indicate if spawn points are ready
    private bool isFixedLevelSetup = false; // Flag to indicate if we've initialized based on Inspector values

    private IEnumerator Start()
    {
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

        if (enemyPrefab == null)
        {
            Debug.LogError("EnemyManager: Enemy prefab is not assigned in EnemyManager!");
            return;
        }

        if (this.spawnPoints.Count == 0) // Check the actual list being used
        {
            Debug.LogError("EnemyManager: No spawn points available (either predefined or from LevelGenerator)!");
            return;
        }

        // Shuffle the spawn points to randomize placement
        List<Transform> shuffledSpawnPoints = new List<Transform>(this.spawnPoints);
        shuffledSpawnPoints.Sort((a, b) => Random.Range(-1, 2)); // Using your existing shuffle method

        int enemiesSpawned = 0;
        Debug.Log($"EnemyManager: Attempting to spawn up to {maxEnemies} enemies from {shuffledSpawnPoints.Count} available spawn points.");

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
                Debug.Log($"EnemyManager: Enemy spawned at position: {spawnPoint.position}");
                enemiesSpawned++;
            }
            else
            {
                Debug.LogError("EnemyManager: Failed to instantiate enemy prefab!");
            }
        }

        Debug.Log($"EnemyManager: Actually spawned {enemiesSpawned} enemies.");
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

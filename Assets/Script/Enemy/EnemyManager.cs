using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject enemyPrefab; // The enemy prefab to spawn
    public List<Transform> spawnPoints = new List<Transform>(); // List of spawn points for enemies
    public int maxEnemies = 5; // Maximum number of enemies to spawn

    private bool spawnPointsReady = false; // Flag to indicate if spawn points are ready

    public void InitializeSpawnPoints(List<Transform> points)
    {
        spawnPoints.AddRange(points);
        spawnPointsReady = true;
        Debug.Log($"EnemyManager received {points.Count} spawn points.");
        SpawnEnemies(); // Trigger enemy spawning after spawn points are ready
    }

    private void SpawnEnemies()
    {
        if (!spawnPointsReady)
        {
            Debug.LogError("Spawn points are not ready. Cannot spawn enemies!");
            return;
        }

        if (enemyPrefab == null)
        {
            Debug.LogError("Enemy prefab is not assigned in EnemyManager!");
            return;
        }

        if (spawnPoints.Count == 0)
        {
            Debug.LogError("No spawn points assigned in EnemyManager!");
            return;
        }

        // Shuffle the spawn points to randomize placement
        List<Transform> shuffledSpawnPoints = new List<Transform>(spawnPoints);
        shuffledSpawnPoints.Sort((a, b) => Random.Range(-1, 2));

        int enemiesSpawned = 0;

        // Spawn enemies at random spawn points
        foreach (Transform spawnPoint in shuffledSpawnPoints)
        {
            if (enemiesSpawned >= maxEnemies)
            {
                break;
            }

            if (spawnPoint == null)
            {
                Debug.LogWarning("Spawn point is null. Skipping...");
                continue;
            }

            // Instantiate the enemy prefab at the spawn point
            GameObject enemyInstance = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            if (enemyInstance != null)
            {
                Debug.Log($"Enemy spawned at position: {spawnPoint.position}");
                enemiesSpawned++;
            }
            else
            {
                Debug.LogError("Failed to instantiate enemy prefab!");
            }
        }

        Debug.Log($"Spawned {enemiesSpawned} enemies.");
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

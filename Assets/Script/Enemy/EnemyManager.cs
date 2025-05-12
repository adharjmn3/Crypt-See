using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject enemyPrefab; // The enemy prefab to spawn
    public List<Transform> spawnPoints = new List<Transform>(); // List of spawn points for enemies
    public int maxEnemies = 5; // Maximum number of enemies to spawn

    public LevelGenerator levelGenerator; // Reference to the LevelGenerator

    private void Start()
    {
        if (levelGenerator != null)
        {
            // Collect spawn points from the LevelGenerator
            spawnPoints.AddRange(levelGenerator.allSpawnPoints);
        }

        SpawnEnemies();
    }

    public void RegisterSpawnPoint(Transform spawnPoint)
    {
        if (!spawnPoints.Contains(spawnPoint))
        {
            spawnPoints.Add(spawnPoint);
            Debug.Log($"Spawn point registered: {spawnPoint.position}");
        }
    }

    private void SpawnEnemies()
    {
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

            GameObject enemyInstance = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            enemiesSpawned++;
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

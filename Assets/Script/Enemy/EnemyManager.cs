using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject enemyPrefab; // The enemy prefab to spawn
    public List<Transform> spawnPoints; // List of spawn points for enemies
    public int maxEnemies = 5; // Maximum number of enemies to spawn

    private List<GameObject> activeEnemies = new List<GameObject>(); // List of active enemies

    private void Start()
    {
        SpawnEnemies();
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

            // Check if the spawn point is already occupied
            bool isOccupied = false;
            foreach (GameObject enemy in activeEnemies)
            {
                if (enemy != null && Vector3.Distance(enemy.transform.position, spawnPoint.position) < 0.1f)
                {
                    isOccupied = true;
                    break;
                }
            }

            if (!isOccupied)
            {
                GameObject enemyInstance = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
                activeEnemies.Add(enemyInstance);
                enemiesSpawned++;
            }
        }

        Debug.Log($"Spawned {enemiesSpawned} enemies.");
    }

    public void RemoveEnemy(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
            Destroy(enemy);
            Debug.Log("Enemy removed.");
        }
    }

    public void RemoveAllEnemies()
    {
        foreach (GameObject enemy in activeEnemies)
        {
            Destroy(enemy);
        }
        activeEnemies.Clear();
        Debug.Log("All enemies removed.");
    }
}

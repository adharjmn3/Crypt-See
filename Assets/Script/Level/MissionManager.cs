using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MissionManager : MonoBehaviour
{
    public List<GameObject> objectivePrefabs; // List of objective prefabs
    public List<Transform> spawnPoints; // Predetermined spawn points
    public UIManager uiManager; // Reference to the UIManager
    public GameObject finishTrigger; // Finish trigger GameObject
    public int maxObjectives = 3; // Maximum number of mandatory objectives to spawn
    public LevelGenerator levelGenerator; // Reference to the LevelGenerator

    private List<GameObject> activeMandatoryObjectives = new List<GameObject>(); // Active mandatory objectives
    private int completedMandatoryObjectives = 0; // Track completed mandatory objectives
    private bool allObjectivesCompleted = false; // Flag to track if all objectives are completed

    private IEnumerator Start()
    {
        // Wait for the LevelGenerator to finish generating the level
        yield return new WaitUntil(() => levelGenerator.GetObjectiveSpawnPoints().Count > 0);

        if (levelGenerator != null)
        {
            Debug.Log("LevelGenerator is referenced. Collecting spawn points...");
            CollectSpawnPointsFromLevelGenerator();
        }
        else
        {
            Debug.Log("No LevelGenerator referenced. Using predefined spawn points.");
        }

        GenerateObjectives();
    }

    private void CollectSpawnPointsFromLevelGenerator()
    {
        if (levelGenerator == null)
        {
            Debug.LogError("LevelGenerator is not assigned in MissionManager!");
            return;
        }

        // Collect spawn points from the LevelGenerator
        spawnPoints = new List<Transform>(levelGenerator.GetObjectiveSpawnPoints());
        Debug.Log($"Collected {spawnPoints.Count} spawn points from LevelGenerator (Objective).");
    }

    private void GenerateObjectives()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogError("No objective spawn points available to generate objectives!");
            return;
        }

        // Shuffle the objective prefabs list to randomize selection
        List<GameObject> shuffledObjectives = new List<GameObject>(objectivePrefabs);
        shuffledObjectives.Sort((a, b) => Random.Range(-1, 2));

        // Shuffle the spawn points to ensure randomness
        List<Transform> shuffledSpawnPoints = new List<Transform>(spawnPoints);
        ShuffleList(shuffledSpawnPoints);

        // Spawn objectives at unique spawn points
        for (int i = 0; i < Mathf.Min(maxObjectives, shuffledSpawnPoints.Count); i++)
        {
            GameObject objectivePrefab = shuffledObjectives[i];
            Transform spawnPoint = shuffledSpawnPoints[i];
            GameObject objectiveInstance = Instantiate(objectivePrefab, spawnPoint.position, spawnPoint.rotation);

            ObjectiveBehavior behavior = objectiveInstance.GetComponent<ObjectiveBehavior>();
            if (behavior != null)
            {
                behavior.Initialize(this);
                activeMandatoryObjectives.Add(objectiveInstance);
            }
            else
            {
                Debug.LogError("ObjectiveBehavior script is missing on the objective prefab!");
            }
        }

        if (levelGenerator != null && finishTrigger != null && shuffledSpawnPoints.Count > 0)
        {
            Transform finishPosition = shuffledSpawnPoints[shuffledSpawnPoints.Count - 1];
            finishTrigger.transform.position = finishPosition.position;
            Debug.Log($"Finish trigger moved to position: {finishPosition.position}");
        }

        if (finishTrigger != null)
        {
            FinishTriggerBehavior finishBehavior = finishTrigger.GetComponent<FinishTriggerBehavior>();
            if (finishBehavior != null)
            {
                finishBehavior.Initialize(this);
            }
            else
            {
                Debug.LogError("FinishTriggerBehavior script is missing on the finish trigger!");
            }
        }

        if (uiManager != null && levelGenerator == null)
        {
            uiManager.UpdateObjectiveCounter(maxObjectives);
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(0, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public void CompleteObjective(GameObject completedObjective, ObjectiveData objectiveData)
    {
        Debug.Log($"Completed Objective: {objectiveData.objectiveName}");

        // Show dialog for the completed objective only if LevelGenerator is not referenced
        if (uiManager != null && levelGenerator == null)
        {
            uiManager.UpdateDialog(
                objectiveData.dialogSpeakerName,
                objectiveData.dialogContent,
                true,
                objectiveData.typingSpeed
            );
        }

        // Handle mandatory objectives
        if (objectiveData.isMandatory)
        {
            if (activeMandatoryObjectives.Contains(completedObjective))
            {
                activeMandatoryObjectives.Remove(completedObjective);
                completedMandatoryObjectives++;

                // Update the objective counter in the UI only if LevelGenerator is not referenced
                if (uiManager != null && levelGenerator == null)
                {
                    int remainingObjectives = Mathf.Max(0, maxObjectives - completedMandatoryObjectives); // Ensure no negative values
                    uiManager.UpdateObjectiveCounter(remainingObjectives);
                }
            }
            else
            {
                Debug.LogWarning("Attempted to complete an objective that is not active or already completed.");
            }
        }

        // Check if all mandatory objectives are completed
        if (completedMandatoryObjectives >= maxObjectives || activeMandatoryObjectives.Count == 0)
        {
            Debug.Log("All mandatory objectives completed!");
            allObjectivesCompleted = true;

            // Enable the finish trigger only when all objectives are completed
            if (finishTrigger != null)
            {
                finishTrigger.SetActive(true);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && allObjectivesCompleted)
        {
            Debug.Log("Player reached the finish point. Showing End Story UI...");

            if (uiManager != null)
            {
                // Show the End Story UI
                uiManager.ShowEndStoryUI(true);

                // Set up button actions
                uiManager.SetupEndStoryButtons(
                    onRestart: ReloadScene,
                    onExit: ExitGame
                );
            }
        }
    }

    public bool AreAllObjectivesCompleted()
    {
        // Ensure all mandatory objectives are completed before returning true
        return allObjectivesCompleted && activeMandatoryObjectives.Count == 0;
    }

    public void FinishGame()
    {
        if (uiManager != null)
        {
            // Show the End Story UI
            uiManager.ShowEndStoryUI(true);

            // Set up button actions
            uiManager.SetupEndStoryButtons(
                onRestart: ReloadScene,
                onExit: ExitGame
            );
        }
        else
        {
            Debug.LogError("UIManager is not assigned in MissionManager.");
        }
    }

    // Method to exit the game
    private void ExitGame()
    {
        Debug.Log("Exiting the game...");
        Application.Quit();
    }

    private void ReloadScene()
    {
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public List<Transform> GetObjectiveSpawnPoints()
    {
        Debug.Log($"Returning {spawnPoints.Count} objective spawn points.");
        return spawnPoints;
    }
}

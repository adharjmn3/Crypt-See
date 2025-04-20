using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MissionManager : MonoBehaviour
{
    public List<GameObject> objectivePrefabs; // List of objective prefabs
    public List<Transform> spawnPoints; // Predetermined spawn points
    public UIManager uiManager; // Reference to the UIManager
    public GameObject finishTrigger; // Finish trigger GameObject
    public int maxObjectives = 3; // Maximum number of mandatory objectives to spawn

    private List<GameObject> activeMandatoryObjectives = new List<GameObject>(); // Active mandatory objectives
    private int completedMandatoryObjectives = 0; // Track completed mandatory objectives
    private bool allObjectivesCompleted = false; // Flag to track if all objectives are completed

    private void Start()
    {
        GenerateObjectives();
        finishTrigger.SetActive(false); // Disable the finish trigger initially
    }

    private void GenerateObjectives()
    {
        // Shuffle the objective prefabs list to randomize selection
        List<GameObject> shuffledObjectives = new List<GameObject>(objectivePrefabs);
        shuffledObjectives.Sort((a, b) => Random.Range(-1, 2));

        // Shuffle spawn points
        List<Transform> shuffledSpawnPoints = new List<Transform>(spawnPoints);
        shuffledSpawnPoints.Sort((a, b) => Random.Range(-1, 2));

        // Spawn objectives at random spawn points
        for (int i = 0; i < Mathf.Min(maxObjectives, shuffledSpawnPoints.Count); i++)
        {
            GameObject objectivePrefab = shuffledObjectives[i];
            Transform spawnPoint = shuffledSpawnPoints[i];
            GameObject objectiveInstance = Instantiate(objectivePrefab, spawnPoint.position, spawnPoint.rotation);

            // Ensure the ObjectiveBehavior script is attached
            ObjectiveBehavior behavior = objectiveInstance.GetComponent<ObjectiveBehavior>();
            if (behavior != null)
            {
                behavior.Initialize(this); // Initialize with the MissionManager reference
                activeMandatoryObjectives.Add(objectiveInstance);
            }
            else
            {
                Debug.LogError("ObjectiveBehavior script is missing on the objective prefab!");
            }
        }

        // Spawn the finish objective at a fixed position (not randomized)
        if (finishTrigger != null)
        {
            ObjectiveBehavior finishBehavior = finishTrigger.GetComponent<ObjectiveBehavior>();
            if (finishBehavior != null)
            {
                finishBehavior.Initialize(this); // Assign the MissionManager to the finish objective
            }
            else
            {
                Debug.LogError("ObjectiveBehavior script is missing on the finish trigger!");
            }
        }
    }

    public void CompleteObjective(GameObject completedObjective, ObjectiveData objectiveData)
    {
        Debug.Log($"Completed Objective: {objectiveData.objectiveName}");

        // Show dialog for the completed objective
        if (uiManager != null)
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
            activeMandatoryObjectives.Remove(completedObjective);
            completedMandatoryObjectives++;
        }

        Destroy(completedObjective);

        // Check if all mandatory objectives are completed
        if (completedMandatoryObjectives >= maxObjectives || activeMandatoryObjectives.Count == 0)
        {
            Debug.Log("All mandatory objectives completed!");
            allObjectivesCompleted = true;
            finishTrigger.SetActive(true); // Enable the finish trigger
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && allObjectivesCompleted)
        {
            Debug.Log("Player reached the finish point. Reloading scene...");
            ReloadScene();
        }
    }

    private void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Restart the current level
    }
}

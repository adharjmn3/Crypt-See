using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // For restarting the level

public class MissionManager : MonoBehaviour
{
    public List<GameObject> objectivePrefabs; // List of objective prefabs
    public List<Transform> spawnPoints; // Predetermined spawn points
    public UIManager uiManager; // Reference to the UIManager
    public GameObject finishTrigger; // Finish trigger GameObject
    public int maxObjectives = 3; // Maximum number of mandatory objectives to spawn

    private List<GameObject> activeMandatoryObjectives = new List<GameObject>(); // Active mandatory objectives
    private List<GameObject> activeSideObjectives = new List<GameObject>(); // Active side objectives
    private int completedMandatoryObjectives = 0; // Track completed mandatory objectives

    private void Start()
    {
        GenerateObjectives();
        PlaceFinishTrigger();
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
        for (int i = 0; i < Mathf.Min(shuffledObjectives.Count, shuffledSpawnPoints.Count); i++)
        {
            GameObject objectivePrefab = shuffledObjectives[i];
            Transform spawnPoint = shuffledSpawnPoints[i];

            GameObject objectiveInstance = Instantiate(objectivePrefab, spawnPoint.position, spawnPoint.rotation);

            // Ensure the ObjectiveBehavior script is attached
            ObjectiveBehavior behavior = objectiveInstance.GetComponent<ObjectiveBehavior>();
            if (behavior != null)
            {
                behavior.Initialize(this); // Initialize with the MissionManager reference

                // Check if the objective is mandatory or a side objective
                if (behavior.objectiveData.isMandatory)
                {
                    activeMandatoryObjectives.Add(objectiveInstance);
                }
                else
                {
                    activeSideObjectives.Add(objectiveInstance);
                }
            }
            else
            {
                Debug.LogError("ObjectiveBehavior script is missing on the objective prefab!");
            }
        }
    }

    private void PlaceFinishTrigger()
    {
        if (finishTrigger != null && spawnPoints.Count > 0)
        {
            // Place the finish trigger at the last spawn point
            Transform finishPoint = spawnPoints[spawnPoints.Count - 1];
            Instantiate(finishTrigger, finishPoint.position, finishPoint.rotation);
        }
        else
        {
            Debug.LogError("Finish trigger or spawn points are not set!");
        }
    }

    public void CompleteObjective(GameObject completedObjective, ObjectiveData objectiveData)
    {
        Debug.Log($"Completed Objective: {objectiveData.objectiveName}");

        // Show dialog for the completed objective
        if (uiManager != null)
        {
            uiManager.UpdateDialog(
                objectiveData.dialogSpeakerName, // Use the speaker name from ObjectiveData
                objectiveData.dialogContent, // Use the dialog content from ObjectiveData
                true,
                objectiveData.typingSpeed // Use the typing speed from ObjectiveData
            );
        }
        else
        {
            Debug.LogError("UIManager reference is not set in MissionManager!");
        }

        // Handle mandatory and side objectives separately
        if (objectiveData.isMandatory)
        {
            activeMandatoryObjectives.Remove(completedObjective);
            completedMandatoryObjectives++;
        }
        else
        {
            activeSideObjectives.Remove(completedObjective);
        }

        Destroy(completedObjective);

        // Check if all mandatory objectives are completed
        if (completedMandatoryObjectives >= maxObjectives || activeMandatoryObjectives.Count == 0)
        {
            Debug.Log("All mandatory objectives completed!");
            CheckAllObjectivesCompleted();
        }
    }

    private void CheckAllObjectivesCompleted()
    {
        if (activeMandatoryObjectives.Count == 0)
        {
            Debug.Log("All mandatory objectives completed! Restarting level...");
            RestartLevel();
        }
    }

    private void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Restart the current level
    }
}

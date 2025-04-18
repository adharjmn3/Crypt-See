using System.Collections.Generic;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    public List<ObjectiveData> objectives; // List of all possible objectives
    public List<Transform> spawnPoints; // Predetermined spawn points
    public UIManager uiManager; // Reference to the UIManager
    public int maxObjectives = 3; // Maximum number of objectives to spawn

    private List<GameObject> activeObjectives = new List<GameObject>(); // Active objectives in the scene
    private int currentObjectiveIndex = 0; // Track the current objective

    private void Start()
    {
        GenerateObjectives();
    }

    private void GenerateObjectives()
    {
        // Shuffle the objectives list to randomize selection
        List<ObjectiveData> shuffledObjectives = new List<ObjectiveData>(objectives);
        shuffledObjectives.Sort((a, b) => Random.Range(-1, 2));

        // Shuffle spawn points
        List<Transform> shuffledSpawnPoints = new List<Transform>(spawnPoints);
        shuffledSpawnPoints.Sort((a, b) => Random.Range(-1, 2));

        // Spawn objectives at random spawn points
        for (int i = 0; i < Mathf.Min(maxObjectives, shuffledObjectives.Count, shuffledSpawnPoints.Count); i++)
        {
            ObjectiveData objective = shuffledObjectives[i];
            Transform spawnPoint = shuffledSpawnPoints[i];

            GameObject objectiveInstance = Instantiate(objective.objectivePrefab, spawnPoint.position, spawnPoint.rotation);
            activeObjectives.Add(objectiveInstance);

            // Attach the ObjectiveBehavior script to handle interactions
            ObjectiveBehavior behavior = objectiveInstance.AddComponent<ObjectiveBehavior>();
            behavior.Initialize(objective, this);
        }
    }

    public void CompleteObjective(ObjectiveData completedObjective)
    {
        Debug.Log($"Completed Objective: {completedObjective.objectiveName}");

        // Show dialog for the completed objective
        if (uiManager != null)
        {
            uiManager.UpdateDialog(
                completedObjective.dialogSpeakerName,
                completedObjective.dialogContent,
                true,
                completedObjective.typingSpeed
            );
        }

        // Handle rewards (optional)
        if (completedObjective.hasReward)
        {
            if (completedObjective.ammoReward > 0)
            {
                Debug.Log($"Player received {completedObjective.ammoReward} ammo.");
                // Add ammo to the player (implement player inventory logic here)
            }

            if (completedObjective.healthReward > 0)
            {
                Debug.Log($"Player received {completedObjective.healthReward} health.");
                // Add health to the player (implement player health logic here)
            }
        }

        // Remove the completed objective from the active list
        currentObjectiveIndex++;
        if (currentObjectiveIndex >= activeObjectives.Count)
        {
            Debug.Log("All objectives completed!");
            // Trigger mission completion logic here
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MissionManager : MonoBehaviour
{
    public List<GameObject> objectivePrefabs; // List of objective prefabs
    public List<Transform> spawnPoints; // Predetermined spawn points for fixed levels, or populated by LevelGenerator
    public UIManager uiManager; // Reference to the UIManager
    public GameObject finishTrigger; // Finish trigger GameObject
    public int maxObjectives = 3; // Maximum number of mandatory objectives to spawn
    public LevelGenerator levelGenerator; // Reference to the LevelGenerator

    private List<GameObject> activeMandatoryObjectives = new List<GameObject>(); // Active mandatory objectives
    private int completedMandatoryObjectives = 0; // Track completed mandatory objectives
    private bool allObjectivesCompleted = false; // Flag to track if all objectives are completed

    private IEnumerator Start()
    {
        if (levelGenerator != null) // Scenario: Random generated level
        {
            Debug.Log("LevelGenerator is referenced. Waiting for it to initialize spawn points...");
            // Wait for the LevelGenerator to have collected its spawn points.
            yield return new WaitUntil(() => levelGenerator.GetObjectiveSpawnPoints() != null && 
                                           levelGenerator.GetObjectiveSpawnPoints().Count > 0);
            
            Debug.Log("LevelGenerator has spawn points. Collecting spawn points...");
            CollectSpawnPointsFromLevelGenerator();
            
            // NEW: Make sure corners are ready before placing the finish trigger
            yield return new WaitForSeconds(0.2f); // Small delay to ensure all corners are created
        }
        else // Scenario: Fixed level (no LevelGenerator)
        {
            Debug.Log("No LevelGenerator referenced. Using predefined spawn points for a fixed level.");
            // For fixed levels, 'spawnPoints' (the public List<Transform>)
            // should already be populated via the Inspector.
            if (this.spawnPoints == null || this.spawnPoints.Count == 0)
            {
                Debug.LogError("MissionManager: LevelGenerator is NOT assigned, AND no predefined spawnPoints are set in the Inspector for the fixed level! Objectives cannot be spawned.");
                yield break;
            }
            Debug.Log($"Using {this.spawnPoints.Count} predefined spawn points for fixed level.");
        }

        // Generate objectives and place the finish trigger
        GenerateObjectives();
        
        finishTrigger.SetActive(false); // Hide the finish trigger initially

    }

    private void CollectSpawnPointsFromLevelGenerator()
    {
        // This method is only called if levelGenerator is not null.
        if (levelGenerator == null) 
        {
            // This case should ideally not be reached due to the check in Start().
            Debug.LogError("LevelGenerator is not assigned in MissionManager when trying to collect spawn points!");
            return;
        }

        // Collect spawn points from the LevelGenerator, overwriting any inspector-assigned ones.
        this.spawnPoints = new List<Transform>(levelGenerator.GetObjectiveSpawnPoints());
        Debug.Log($"Collected {this.spawnPoints.Count} spawn points from LevelGenerator (Objective).");
    }

    private void GenerateObjectives()
    {
        if (objectivePrefabs == null || objectivePrefabs.Count == 0)
        {
            Debug.LogError("No objective prefabs assigned in MissionManager!");
            return;
        }

        if (this.spawnPoints == null || this.spawnPoints.Count == 0)
        {
            Debug.LogError("No objective spawn points available (either predefined or from LevelGenerator) to generate objectives!");
            return;
        }

        // Shuffle the objective prefabs list to randomize selection
        List<GameObject> shuffledObjectives = new List<GameObject>(objectivePrefabs);
        shuffledObjectives.Sort((a, b) => Random.Range(-1, 2));

        // Shuffle the spawn points to ensure randomness
        List<Transform> shuffledSpawnPoints = new List<Transform>(this.spawnPoints);
        ShuffleList(shuffledSpawnPoints);

        // Spawn objectives at unique spawn points
        for (int i = 0; i < Mathf.Min(maxObjectives, shuffledSpawnPoints.Count); i++)
        {
            GameObject objectivePrefab = shuffledObjectives[i % shuffledObjectives.Count]; // Cycle through prefabs if needed
            Transform spawnPoint = shuffledSpawnPoints[i]; // Use a unique spawn point
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

        // Enhanced finish trigger placement
        if (finishTrigger == null)
        {
            Debug.LogError("Finish trigger GameObject is not assigned in the inspector!");
            return;
        }

        // Place the finish trigger at a corner if using LevelGenerator
        if (levelGenerator != null)
        {
            List<Transform> corners = null;
            
            try
            {
                corners = levelGenerator.GetAllCorners();
                Debug.Log($"Retrieved {corners?.Count ?? 0} corners from LevelGenerator");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error getting corners from LevelGenerator: {e.Message}");
            }
            
            if (corners != null && corners.Count > 0)
            {
                // Choose a random corner for the finish trigger
                Transform finishPosition = corners[Random.Range(0, corners.Count)];
                
                // Get player's corner
                Transform playerCorner = null;
                PlayerManager playerManager = FindObjectOfType<PlayerManager>();
                if (playerManager != null)
                {
                    try
                    {
                        playerCorner = playerManager.GetSelectedCorner();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error getting player's corner: {e.Message}");
                    }
                }
                
                // Make sure the chosen corner is different from player's position
                if (playerCorner != null && finishPosition == playerCorner && corners.Count > 1)
                {
                    List<Transform> availableCorners = new List<Transform>(corners);
                    availableCorners.Remove(playerCorner);
                    finishPosition = availableCorners[Random.Range(0, availableCorners.Count)];
                }
                
                // Position and activate the finish trigger
                finishTrigger.transform.position = finishPosition.position;
                Debug.Log($"Finish trigger placed at corner: {finishPosition.name}, Position: {finishPosition.position}");
                
                // Make sure it's visually distinctive
                SpriteRenderer renderer = finishTrigger.GetComponent<SpriteRenderer>();
                if (renderer == null)
                {
                    renderer = finishTrigger.AddComponent<SpriteRenderer>();
                    renderer.sprite = Resources.FindObjectsOfTypeAll<Sprite>().Length > 0 ? 
                        Resources.FindObjectsOfTypeAll<Sprite>()[0] : null;
                    renderer.color = Color.green; // Make it green to stand out
                }
                
                // Initially hide the finish trigger until objectives are completed
                finishTrigger.SetActive(false);
            }
            else
            {
                Debug.LogWarning("No corners available from LevelGenerator. Using fallback position.");
                
                // Fallback to using the last spawn point if no corners are available
                if (this.spawnPoints != null && this.spawnPoints.Count > 0)
                {
                    Transform fallbackPosition = this.spawnPoints[this.spawnPoints.Count - 1];
                    finishTrigger.transform.position = fallbackPosition.position;
                    Debug.Log($"Finish trigger placed at fallback position: {fallbackPosition.position}");
                }
                else
                {
                    Debug.LogError("No valid position found for finish trigger! Placing at origin.");
                    finishTrigger.transform.position = Vector3.zero;
                }
            }
        }
        else
        {
            // For fixed levels, the finish trigger should already be placed in the scene
            Debug.Log($"Using predefined finish trigger position at {finishTrigger.transform.position}");
        }

        // The finish trigger GameObject should have the NextLevelTrigger script attached to handle changing levels.
        
        // Hide the trigger initially until all objectives are completed
        finishTrigger.SetActive(false);

        // Update the objective counter in the UI
        if (uiManager != null)
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
                Debug.Log($"Finish trigger activated at position: {finishTrigger.transform.position}");
                
                // Add a visual indicator to make it more noticeable
                StartCoroutine(PulseFinishTrigger());
            }
            else
            {
                Debug.LogError("Tried to activate finish trigger but it's null!");
    }
        }
    }

    private IEnumerator PulseFinishTrigger()
    {
        SpriteRenderer renderer = finishTrigger.GetComponent<SpriteRenderer>();
        if (renderer == null) yield break;
        
        float duration = 3.0f;
        float elapsed = 0f;
        Color baseColor = renderer.color;
        Color brightColor = new Color(1f, 1f, 0.5f, 1f); // Bright yellow-ish
        
        while (elapsed < duration)
        {
            float t = Mathf.PingPong(elapsed * 4f, 1.0f);
            renderer.color = Color.Lerp(baseColor, brightColor, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        renderer.color = baseColor;
    }

    public bool AreAllObjectivesCompleted()
    {
        // Ensure all mandatory objectives are completed before returning true
        return allObjectivesCompleted && activeMandatoryObjectives.Count == 0;
    }

    private void ReloadScene()
    {
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // NEW: Add a method to verify the finish trigger setup
   
}

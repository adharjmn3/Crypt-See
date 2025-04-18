using UnityEngine;

public class ObjectiveBehavior : MonoBehaviour
{
    private ObjectiveData objectiveData;
    private MissionManager missionManager;

    public void Initialize(ObjectiveData data, MissionManager manager)
    {
        objectiveData = data;
        missionManager = manager;
        
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (objectiveData == null || missionManager == null)
        {
            Debug.LogError("ObjectiveBehavior is not initialized properly! Ensure Initialize() is called.");
            return;
        }

        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player interacted with objective: {objectiveData.objectiveName}");
            missionManager.CompleteObjective(objectiveData);
            Destroy(gameObject); // Remove the objective from the scene
        }
    }

    public static void SpawnObjective(ObjectiveData objective, Transform spawnPoint, MissionManager manager)
    {
        GameObject objectiveInstance = Instantiate(objective.objectivePrefab, spawnPoint.position, spawnPoint.rotation);
        ObjectiveBehavior behavior = objectiveInstance.GetComponent<ObjectiveBehavior>();
        if (behavior != null)
        {
            behavior.Initialize(objective, manager);
        }
        else
        {
            Debug.LogError("ObjectiveBehavior script is missing on the objective prefab!");
        }
    }
}
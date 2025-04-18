using UnityEngine;

public class ObjectiveBehavior : MonoBehaviour
{
    private MissionManager missionManager;

    [Header("Objective Data")]
    public ObjectiveData objectiveData; // Reference to the ScriptableObject containing objective data

    public void Initialize(MissionManager manager)
    {
        missionManager = manager;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Trigger detected with {other.name} for objective: {gameObject.name}");

        if (missionManager == null)
        {
            Debug.LogError("ObjectiveBehavior is not initialized properly! Ensure Initialize() is called.");
            return;
        }

        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player interacted with objective: {gameObject.name}");

            // Pass the objective data to the MissionManager
            missionManager.CompleteObjective(gameObject, objectiveData);
        }
    }
}
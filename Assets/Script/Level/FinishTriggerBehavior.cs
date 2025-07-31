using UnityEngine;

public class FinishTriggerBehavior : MonoBehaviour
{
    private MissionManager missionManager;

    private void Start()
    {
        // Find the MissionManager in the scene
        missionManager = FindObjectOfType<MissionManager>();
        if (missionManager == null)
        {
            Debug.LogError("MissionManager not found in the scene! Make sure a GameObject with the MissionManager script is active.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (missionManager != null)
            {
                if (missionManager.AreAllObjectivesCompleted())
                {
                    Debug.Log("Player has reached the finish trigger with all objectives completed. Loading next level...");
                    // Assuming GameManager is a singleton or easily accessible
                }
                else
                {
                    Debug.LogWarning("Player touched the finish trigger, but not all objectives are completed!");
                }
            }
            else
            {
                Debug.LogError("MissionManager is not assigned to FinishTriggerBehavior!");
            }
        }
    }
}



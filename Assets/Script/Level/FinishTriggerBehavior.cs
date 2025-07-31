using UnityEngine;

public class FinishTriggerBehavior : MonoBehaviour
{
    private MissionManager missionManager;

    public void Initialize(MissionManager manager)
    {
        missionManager = manager;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (missionManager != null)

                Debug.LogWarning("Player touched the finish trigger, but not all objectives are completed!");

        
    }
        else
        {
            Debug.LogError("MissionManager is not assigned to FinishTriggerBehavior!");
        }
    }
}

    

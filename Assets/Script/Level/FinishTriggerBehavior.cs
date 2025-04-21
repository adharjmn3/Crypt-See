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
            {
                if (missionManager.AreAllObjectivesCompleted())
                {
                    Debug.Log("Player touched the finish trigger. Ending the game...");
                    missionManager.FinishGame();
                }
                else
                {
                    Debug.LogWarning("Player touched the finish trigger, but not all objectives are completed!");

                    // Show dialog to indicate objectives are not completed
                    if (missionManager.uiManager != null)
                    {
                        missionManager.uiManager.UpdateDialog(
                            "comms", 
                            "I think we forgot something", 
                            true, 
                            0.05f // Typing speed
                        );
                    }
                }
            }
            else
            {
                Debug.LogError("MissionManager is not assigned to FinishTriggerBehavior!");
            }
        }
    }
}
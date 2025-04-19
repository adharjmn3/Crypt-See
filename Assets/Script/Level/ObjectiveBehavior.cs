using UnityEngine;

public class ObjectiveBehavior : MonoBehaviour
{
    private MissionManager missionManager;
    private Animator playerAnimator; // Reference to the player's Animator

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

            // Play the player's "punch" animation
            if (playerAnimator == null)
            {
                playerAnimator = other.GetComponent<Animator>(); // Get the Animator component from the player
            }

            if (playerAnimator != null)
            {
                playerAnimator.SetTrigger("punch"); // Trigger the "punch" animation
            }
            else
            {
                Debug.LogError("Player Animator not found! Ensure the player has an Animator component.");
            }

            // Pass the objective data to the MissionManager
            missionManager.CompleteObjective(gameObject, objectiveData);
        }
    }
}
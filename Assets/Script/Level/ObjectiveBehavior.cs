using UnityEngine;

public class ObjectiveBehavior : MonoBehaviour
{
    private MissionManager missionManager;
    private Animator playerAnimator; // Reference to the player's Animator

    [Header("Objective Data")]
    public ObjectiveData objectiveData; // Reference to the ScriptableObject containing objective data

    [Header("Audio Settings")]
    public AudioClip collectSound; // Sound to play when the objective is collected
    private AudioSource audioSource; // Reference to the AudioSource component

    public void Initialize(MissionManager manager)
    {
        missionManager = manager;
    }

    private void Start()
    {
        // Add an AudioSource component if it doesn't exist
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure the AudioSource
        audioSource.playOnAwake = false;
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

            // Play the collect sound
            if (collectSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(collectSound);
            }
            else
            {
                Debug.LogWarning("Collect sound or AudioSource is missing!");
            }

            // Pass the objective data to the MissionManager
            missionManager.CompleteObjective(gameObject, objectiveData);
        }
    }
}
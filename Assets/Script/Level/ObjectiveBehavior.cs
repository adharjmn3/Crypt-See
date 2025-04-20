using UnityEngine;

public class ObjectiveBehavior : MonoBehaviour
{
    private MissionManager missionManager;

    [Header("Objective Data")]
    public ObjectiveData objectiveData; // Reference to the ScriptableObject containing objective data

    [Header("Audio Settings")]
    public AudioClip collectSound; // Sound to play when the objective is collected
    private AudioSource audioSource; // Reference to the AudioSource component

    [Header("Animation Settings")]
    public Animator animator; // Animator for the punching animation
    public string punchAnimationTrigger = "Punch"; // Trigger name for the punch animation

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

        // Ensure the Animator is assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player interacted with objective: {gameObject.name}");

            // Play the collect sound
            if (collectSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(collectSound);
            }

            // Play the punching animation
            if (animator != null)
            {
                animator.SetTrigger(punchAnimationTrigger);
            }

            // Pass the objective data to the MissionManager
            if (missionManager != null)
            {
                missionManager.CompleteObjective(gameObject, objectiveData);
            }
            else
            {
                Debug.LogError("MissionManager is not assigned to ObjectiveBehavior!");
            }

            // Destroy the objective after interaction
            Destroy(gameObject);
        }
    }
}
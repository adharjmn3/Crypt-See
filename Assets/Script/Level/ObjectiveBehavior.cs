using UnityEngine;
using System.Collections;

public class ObjectiveBehavior : MonoBehaviour
{
    private MissionManager missionManager;
    private UIManager uiManager;

    [Header("Objective Data")]
    public ObjectiveData objectiveData; // Reference to the ScriptableObject containing objective data

    [Header("Audio Settings")]
    public AudioClip collectSound; // Sound to play when the objective is collected
    private AudioSource audioSource; // Reference to the AudioSource component

    [Header("Animation Settings")]
    public Animator animator; // Animator for the punching animation
    public string punchAnimationTrigger = "Punch"; // Trigger name for the punch animation

    private SpriteRenderer spriteRenderer; // Reference to the SpriteRenderer component
    public void Initialize(MissionManager manager)
    {
        missionManager = manager;
        spriteRenderer = GetComponent<SpriteRenderer>();
        uiManager = FindObjectOfType<UIManager>(); // Find the UIManager in the scene
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

            // Check the type of the objective
            if (objectiveData.type == ObjectiveData.ObjectiveType.Finish)
            {
                Debug.Log("Objective is of type 'Finish'. Showing Finish Story UI.");
                if (uiManager != null)
                {
                    uiManager.ShowFinishStoryUI(true);
                    uiManager.SetupFinishStoryButton(uiManager.LoadNextScene, uiManager.ExitGame);
                }
                else
                {
                    Debug.LogError("UIManager not found in the scene.");
                }
                // Do not destroy the finish objective or call CompleteObjective, as it's a trigger for the level end.
            }
            else
            {
                // Pass the objective data to the MissionManager for other objective types
                if (missionManager != null)
                {
                    missionManager.CompleteObjective(gameObject, objectiveData);
                }
                else
                {
                    Debug.LogError("MissionManager is not assigned to ObjectiveBehavior!");
                }

                spriteRenderer.enabled = false; // Hide the sprite
                StartCoroutine(DestroyAfterDelay(0.1f)); // Add a delay before destroying
            }
        }
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}
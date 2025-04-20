using System.Collections;
using UnityEngine;
using Player.Stats; // Reference to the Health class namespace

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float healthPoint = 10f;
    [SerializeField] private float damageToPlayer = 5f; // Damage dealt to the player
    [SerializeField] private float damageInterval = 1f; // Interval between damage ticks
    [Header("Audio Settings")]
    [SerializeField] private AudioClip hitSound; // Sound to play when the enemy is hit
    private AudioSource audioSource; // Reference to the AudioSource component
    [Header("Death Settings")]
    [SerializeField] private AudioClip deathSound; // Sound to play when the enemy dies
    private Animator animator; // Reference to the Animator component

    private Coroutine damageCoroutine;

    public float HealthPoint { get; }

    private void Start()
    {
        // Get or add an AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Get the Animator component
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("Animator component is missing on the enemy!");
        }
    }

    public void SetHealthPoint(float value)
    {
        healthPoint = value;
    }

    public void AddHealthPoint(float value)
    {
        healthPoint += value;
    }

    public void TakeDamage(float damagePoint)
    {
        healthPoint -= damagePoint;

        // Play the hit sound
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        CheckHP();
    }

    private void CheckHP()
    {
        if (healthPoint <= 0)
        {
            // Play the death sound
            if (audioSource != null && deathSound != null)
            {
                audioSource.PlayOneShot(deathSound);
            }

            // Trigger the death animation
            if (animator != null)
            {
                animator.SetTrigger("Die"); // Ensure the Animator has a "Die" trigger
            }

            // Delay the destruction of the enemy to allow the animation and sound to play
            Destroy(gameObject, 1.5f); // Adjust the delay to match the length of the animation/sound
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player entered enemy trigger: {other.name}");
            Health playerHealth = other.GetComponent<Health>();
            if (playerHealth != null)
            {
                // Start damaging the player
                if (damageCoroutine == null)
                {
                    damageCoroutine = StartCoroutine(DamagePlayer(playerHealth));
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player exited enemy trigger: {other.name}");
            // Stop damaging the player
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }
        }
    }

    private IEnumerator DamagePlayer(Health playerHealth)
    {
        while (true)
        {
            playerHealth.TakeDamage((int)damageToPlayer); // Apply damage to the player
            Debug.Log($"Player took {damageToPlayer} damage from enemy.");
            yield return new WaitForSeconds(damageInterval); // Wait for the next damage tick
        }
    }
}

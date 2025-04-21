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
    [Header("Explosion Settings")]
    [SerializeField] private Sprite explosionSprite; // Sprite for the explosion
    [SerializeField] private float explosionDuration = 1.5f; // Duration to display the explosion sprite
    [SerializeField] private GameObject explosionPrefab; // Prefab for the explosion effect
    [Header("Damage Effect Settings")]
    [SerializeField] private GameObject damageEffectPrefab; // Prefab for the damage effect

    private SpriteRenderer spriteRenderer; // Reference to the SpriteRenderer component
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

        // Get the SpriteRenderer component
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer component is missing on the enemy!");
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

        // Only spawn the damage effect if health is 20 or greater
        if (healthPoint >= 20)
        {
            if (damageEffectPrefab != null)
            {
                GameObject damageEffect = Instantiate(damageEffectPrefab, transform.position, Quaternion.identity);
                StartCoroutine(DestroyDamageEffectAfterDelay(damageEffect, 2f)); // Destroy after 2 seconds
            }
            else
            {
                Debug.LogWarning("Damage effect prefab is not assigned.");
            }
        }

        // Set tension meter to 5 and start chasing the player
        EnemyNPC enemyNPC = GetComponent<EnemyNPC>();
        if (enemyNPC != null)
        {
            enemyNPC.tensionMeter = 5f; // Set tension meter to 5
            Debug.Log("Enemy tension meter set to 5. Starting chase.");
        }

        CheckHP();
    }

    private IEnumerator DestroyDamageEffectAfterDelay(GameObject damageEffect, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (damageEffect != null)
        {
            Destroy(damageEffect); // Destroy the damage effect prefab
        }
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

            // Instantiate the explosion prefab at the enemy's position
            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            }
            else
            {
                Debug.LogError("Explosion prefab is not assigned!");
            }

            // Disable further interactions
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            // Destroy the enemy GameObject
            Destroy(gameObject);
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

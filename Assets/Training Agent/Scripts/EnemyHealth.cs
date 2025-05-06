using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Required for the Slider component
using Player.Stats; // Reference to the Health class namespace

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float healthPoint = 10f;
    [SerializeField] private float baseDamageToPlayer = 5f; // Base damage dealt to the player
    [SerializeField] private float damageInterval = 1f; // Interval between damage ticks
    [SerializeField] private float damageIncreaseRate = 1f; // Rate at which damage increases over time
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
    [SerializeField] private Slider healthSlider; // Reference to the HUD Slider for enemy health
    [SerializeField] private Vector3 sliderOffset = new Vector3(0, 1.5f, 0); // Offset for the slider position

    private SpriteRenderer spriteRenderer; // Reference to the SpriteRenderer component
    private Coroutine damageCoroutine;
    private EnemyManager enemyManager; // Reference to the EnemyManager script
    private float currentDamageToPlayer; // Tracks the current damage being dealt to the player
    private Camera mainCamera; // Reference to the main camera

    public float HealthPoint { get; }

    private void Start()
    {
        // Initialize the current damage to the base damage
        currentDamageToPlayer = baseDamageToPlayer;

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

        // Find the EnemyManager in the scene
        enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager == null)
        {
            Debug.LogError("EnemyManager is missing in the scene!");
        }

        // Initialize the slider if assigned
        if (healthSlider != null)
        {
            healthSlider.maxValue = healthPoint; // Set the slider's max value to the enemy's health
            healthSlider.value = healthPoint;   // Set the slider's current value to the enemy's health
        }

        // Cache the main camera reference
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        // Make the slider follow the enemy's position
        if (healthSlider != null)
        {
            healthSlider.transform.position = transform.position + sliderOffset; // Update the slider's position with an offset
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
        if (healthPoint <= 0)
        {
            Debug.LogWarning("TakeDamage called on an already dead enemy!");
            return;
        }

        healthPoint -= damagePoint;

        // Play the hit sound
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        // Update the slider value
        if (healthSlider != null)
        {
            healthSlider.value = healthPoint;
        }

        // Only spawn the damage effect if health is 20 or greater
        if (healthPoint >= 20)
        {
            if (damageEffectPrefab != null)
            {
                GameObject damageEffect = Instantiate(damageEffectPrefab, transform.position, Quaternion.identity);
                StartCoroutine(DestroyDamageEffectAfterDelay(damageEffect, 1f)); // Destroy after 2 seconds
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

            // Notify EnemyManager about the enemy's death
            if (enemyManager != null)
            {
                enemyManager.OnEnemyKilled(this.gameObject);
            }

            // Hide or destroy the slider when the enemy dies
            if (healthSlider != null)
            {
                healthSlider.gameObject.SetActive(false); // Optionally, you can destroy it with Destroy(healthSlider.gameObject);
            }

            // Destroy the enemy GameObject
            Destroy(gameObject);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player entered enemy trigger: {other.name}");
            Health playerHealth = other.GetComponent<Health>();
            if (playerHealth != null)
            {
                // Check if the EnemyNPC's tensionMeter is 5
                // EnemyNPC enemyNPC = GetComponent<EnemyNPC>();
                // if (enemyNPC != null && enemyNPC.tensionMeter == 5f)
                // {
                //     // Start damaging the player
                //     if (damageCoroutine == null)
                //     {
                //         damageCoroutine = StartCoroutine(DamagePlayer(playerHealth));
                //     }
                // }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player exited enemy trigger: {other.name}");
            // Stop damaging the player and reset the damage to the base value
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }
            currentDamageToPlayer = baseDamageToPlayer; // Reset the damage
        }
    }

    private IEnumerator DamagePlayer(Health playerHealth)
    {
        while (true)
        {
            playerHealth.TakeDamage((int)currentDamageToPlayer); // Apply the current damage to the player
            Debug.Log($"Player took {currentDamageToPlayer} damage from enemy.");
            yield return new WaitForSeconds(damageInterval); // Wait for the next damage tick
        }
    }
}

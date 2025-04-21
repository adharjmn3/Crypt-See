using System.Collections;
using UnityEngine;
using TopDown.Movement;
// Reference to the PlayerMovement namespace

namespace Player.Stats
{
    public class Health : MonoBehaviour
    {
        public int maxHealth = 100;
        public int currentHealth;

        public bool isTakingDamage = false; // Whether the player is currently taking damage
        private float damageMultiplier = 1.0f; // Multiplier for incremental damage
        private Coroutine damageCoroutine; // Coroutine to handle incremental damage

        [Header("UI Manager")]
        public UIManager uiManager; // Reference to the UIManager

        [Header("Audio")]
        public AudioSource audioSource; // Reference to the AudioSource component
        public AudioClip damageSound; // Sound clip to play when taking damage

        [Header("Explosion Settings")]
        public GameObject explosionPrefab; // Prefab for the explosion effect

        [Header("Damage Effect Settings")]
        public GameObject damageEffectPrefab; // Prefab for the damage effect

        private void Start()
        {
            currentHealth = maxHealth;
            UpdateHealthUI(); // Initialize the health UI

            // Dynamically find the PlayerMovement component if not assigned
        }

        private void Update()
        {
            // Test damage logic when pressing the '0' key
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                if (!isTakingDamage)
                {
                    Debug.Log("Starting damage test...");
                    StartTakingDamage();
                }
                else
                {
                    Debug.Log("Stopping damage test...");
                    StopTakingDamage();
                }
            }
        }

        public void TakeDamage(int damage)
        {
            currentHealth -= damage;
            if (currentHealth < 0)
            {
                currentHealth = 0; // Prevent health from going negative
            }

            PlayDamageSound(); // Play the damage sound
            UpdateHealthUI(); // Update the health UI

            // Instantiate the damage effect prefab
            if (damageEffectPrefab != null)
            {
                GameObject damageEffect = Instantiate(damageEffectPrefab, transform.position, Quaternion.identity);
                StartCoroutine(DestroyDamageEffectAfterDelay(damageEffect, 2f)); // Destroy after 2 seconds
            }
            else
            {
                Debug.LogWarning("Damage effect prefab is not assigned.");
            }

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private IEnumerator DestroyDamageEffectAfterDelay(GameObject damageEffect, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (damageEffect != null)
            {
                Destroy(damageEffect); // Destroy the damage effect prefab
            }
        }

        private void Die()
        {
            Debug.Log("Character has died.");

            // Disable the player's sprite
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }

            // Disable player movement
            PlayerMovement playerMovement = GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.CanMove = false; // Disable movement
            }

            // Disable all sounds in the scene
            AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
            foreach (AudioSource source in allAudioSources)
            {
                source.Stop();
            }

            // Instantiate the explosion prefab at the player's position
            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            }
            else
            {
                Debug.LogError("Explosion prefab is not assigned!");
            }

            // Start the coroutine to delay showing the end story UI
            StartCoroutine(ShowEndStoryAfterDelay(1f));
        }

        private IEnumerator ShowEndStoryAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay); // Wait for the specified delay

            // Show the end story UI
            if (uiManager != null)
            {
                uiManager.ShowEndStoryUI(true);

                // Disable the player after showing the end story UI
                gameObject.SetActive(false);
            }
            else
            {
                Debug.LogError("UIManager is not assigned. Cannot show the end story UI.");
            }
        }

        public void Heal(int amount)
        {
            currentHealth += amount;
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }

            UpdateHealthUI(); // Update the health UI
        }

        public int GetCurrentHealth()
        {
            return currentHealth;
        }

        public void StartTakingDamage()
        {
            if (!isTakingDamage)
            {
                isTakingDamage = true;
                damageMultiplier = 1.0f; // Reset the damage multiplier
                damageCoroutine = StartCoroutine(IncrementalDamage());
            }
        }

        public void StopTakingDamage()
        {
            if (isTakingDamage)
            {
                isTakingDamage = false;
                if (damageCoroutine != null)
                {
                    StopCoroutine(damageCoroutine);
                }
            }
        }

        private IEnumerator IncrementalDamage()
        {
            while (isTakingDamage)
            {
                int damage = Mathf.CeilToInt(5 * damageMultiplier); // Base damage of 5, scaled by multiplier
                TakeDamage(damage);
                Debug.Log($"Player took {damage} damage. Current health: {currentHealth}");

                damageMultiplier += 0.5f; // Increment the damage multiplier
                yield return new WaitForSeconds(1.0f); // Damage applied every second
            }
        }

        private void UpdateHealthUI()
        {
            if (uiManager != null)
            {
                uiManager.UpdateHealth(currentHealth, maxHealth);
            }
            else
            {
                Debug.LogWarning("UIManager is not assigned. Health UI will not update.");
            }
        }

        private void PlayDamageSound()
        {
            if (audioSource != null && damageSound != null)
            {
                audioSource.PlayOneShot(damageSound); // Play the damage sound
            }
            else
            {
                Debug.LogWarning("AudioSource or DamageSound is not assigned.");
            }
        }
    }
}


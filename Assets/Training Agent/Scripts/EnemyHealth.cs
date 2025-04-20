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
            Destroy(gameObject); // Destroy the enemy when health reaches 0
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

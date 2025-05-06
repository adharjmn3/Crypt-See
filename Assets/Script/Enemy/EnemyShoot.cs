using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyShoot : MonoBehaviour
{
    [Header("Shooting Settings")]
    public GameObject bulletPrefab; // Prefab for the bullet
    public Transform bulletSpawnPoint; // Transform for bullet spawn
    public float bulletSpeed = 10f; // Speed of the bullet
    public float fireRate = 1f; // Time between shots
    public float shootingRange = 10f; // Maximum range to shoot the player
    public int bulletDamage = 10; // Damage dealt by the bullet

    [Header("Effects")]
    public ParticleSystem muzzleFlash; // Muzzle flash effect
    public AudioSource audioSource; // Audio source for shooting sound
    public AudioClip shootSound; // Sound effect for shooting

    private Transform playerTransform; // Reference to the player's transform
    private float nextFireTime = 0f; // Time until the next shot can be fired
    private EnemyNPC enemyNPC; // Reference to the EnemyNPC script

    void Start()
    {
        // Find the player in the scene
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("Player not found! Make sure the player has the 'Player' tag.");
        }

        // Get the EnemyNPC component
        enemyNPC = GetComponent<EnemyNPC>();
        if (enemyNPC == null)
        {
            Debug.LogError("EnemyNPC component is missing on this GameObject!");
        }
    }

    void Update()
    {
        if (playerTransform == null || enemyNPC == null) return;

        // Check if the tension meter is full
        if (enemyNPC.IsTensionMeterFull())
        {
            // Check if the player is within shooting range
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= shootingRange && Time.time >= nextFireTime)
            {
                // Shoot at the player
                ShootAtPlayer();
                nextFireTime = Time.time + 1f / fireRate; // Set the next fire time
            }
        }
    }

    private void ShootAtPlayer()
    {
        // Play muzzle flash effect
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        // Play shooting sound
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        // Spawn the bullet
        if (bulletPrefab != null && bulletSpawnPoint != null)
        {
            // Calculate the direction to the player
            Vector2 direction = (playerTransform.position - bulletSpawnPoint.position).normalized;

            // Offset the bullet's spawn position slightly forward to avoid collision with the enemy
            Vector3 spawnPosition = bulletSpawnPoint.position + (Vector3)(direction * 0.5f); // Adjust the offset as needed

            GameObject bullet = Instantiate(bulletPrefab, spawnPosition, bulletSpawnPoint.rotation);

            // Get the Bullet script and initialize it
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.Initialize(shootingRange, bulletDamage, Weapon.AmmoType.Kinetic, gameObject); // Pass the enemy as the shooter
            }

            // Set the bullet's velocity
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = direction * bulletSpeed;
            }
            else
            {
                Debug.LogWarning("Rigidbody2D is missing on the bullet prefab.");
            }
        }
    }
}
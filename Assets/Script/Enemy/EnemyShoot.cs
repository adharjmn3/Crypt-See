using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyShoot : MonoBehaviour
{
    [Header("Shooting Settings")]
    public GameObject bulletPrefab;
    public Transform bulletSpawnPoint;
    public float bulletSpeed = 10f;
    public float shootingRange = 15f;
    public int bulletDamage = 10;

    [Header("Effects")]
    public ParticleSystem muzzleFlash;
    public AudioSource audioSource;
    public AudioClip shootSound;

    private Transform playerTransform;
    private float nextFireTime = 0f;
    private EnemyStats enemyStats;

    void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        enemyStats = GetComponent<EnemyStats>();
    }

    /// <summary>
    /// Returns true if the agent is allowed to fire a shot right now.
    /// </summary>
    public bool CanShoot()
    {
        return Time.time >= nextFireTime;
    }

    /// <summary>
    /// Called by the ML-Agent to attempt a shot. Returns true if successful.
    /// </summary>
    public bool TryShoot()
    {
        if (playerTransform == null || enemyStats == null) return false;

        // Check cooldown, range, and accuracy
        if (Time.time < nextFireTime) return false;
        if (Vector3.Distance(transform.position, playerTransform.position) > shootingRange) return false;
        if (Random.value > enemyStats.accuracy) return false; // Missed shot based on accuracy

        // If all checks pass, fire the shot
        nextFireTime = Time.time + 1f / enemyStats.rateOfFire;
        FireBullet();
        return true;
    }

    private void FireBullet()
    {
        // Play effects
        if (muzzleFlash != null) muzzleFlash.Play();
        if (audioSource != null && shootSound != null) audioSource.PlayOneShot(shootSound);

        // Spawn the bullet
        if (bulletPrefab != null && bulletSpawnPoint != null)
        {
            Vector3 direction = (playerTransform.position - bulletSpawnPoint.position).normalized;
            GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, Quaternion.LookRotation(direction));

            // Assuming a Rigidbody on the bullet
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = direction * bulletSpeed;
            }

            // Initialize bullet damage etc. if your bullet has a script for it
        }
    }

}

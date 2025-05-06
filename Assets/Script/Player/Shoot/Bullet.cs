using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour
{
    private float range;
    private int damage;
    private Vector3 startPosition;
    public Weapon.AmmoType ammoType; // Variable to store the weapon type
    private GameObject shooter; // Reference to the shooter

    public void Initialize(float range, int damage, Weapon.AmmoType ammoType, GameObject shooter)
    {
        this.range = range;
        this.damage = damage;
        this.ammoType = ammoType; // Assign the weapon type
        this.shooter = shooter; // Assign the shooter
        startPosition = transform.position;
    }

    void Update()
    {
        // Check if the bullet has traveled beyond its range
        if (Vector3.Distance(startPosition, transform.position) >= range)
        {
            Destroy(gameObject); // Destroy the bullet
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Ignore collision with the shooter
        if (collision.gameObject == shooter) return;

        if (collision.CompareTag("Enemy") && shooter.CompareTag("Player"))
        {
            Debug.Log($"Bullet hit enemy: {collision.gameObject.name}, Ammo Type: {ammoType}");
            EnemyHealth enemyHealth = collision.GetComponent<EnemyHealth>();
            EnemyMovement enemyMovement = collision.GetComponent<EnemyMovement>(); // Assuming the enemy has a movement script

            if (enemyHealth != null)
            {
                if (ammoType == Weapon.AmmoType.Kinetic)
                {
                    // Apply normal damage
                    enemyHealth.TakeDamage(damage);
                }
                else if (ammoType == Weapon.AmmoType.EMP && enemyMovement != null)
                {
                    // Pause enemy movement for 'damage' seconds
                    StartCoroutine(PauseEnemyMovement(enemyMovement, damage));
                }
            }

            Destroy(gameObject); // Destroy the bullet
        }
        else if (collision.CompareTag("Player") && shooter.CompareTag("Enemy"))
        {
            Debug.Log($"Bullet hit player: {collision.gameObject.name}, Ammo Type: {ammoType}");
            Player.Stats.Health playerHealth = collision.GetComponent<Player.Stats.Health>();

            if (playerHealth != null)
            {
                // Apply damage to the player
                playerHealth.TakeDamage(damage);
            }

            Destroy(gameObject); // Destroy the bullet
        }
        else
        {
            Debug.Log($"Bullet hit: {collision.gameObject.name}, Ammo Type: {ammoType}");
            Destroy(gameObject); // Destroy the bullet
        }
    }

    private IEnumerator PauseEnemyMovement(EnemyMovement enemyMovement, float duration)
    {
        Debug.Log($"Pausing enemy movement for {duration} seconds.");
        enemyMovement.enabled = false; // Disable the enemy's movement
        yield return new WaitForSeconds(duration);
        enemyMovement.enabled = true; // Re-enable the enemy's movement
        Debug.Log("Enemy movement resumed.");
    }
}
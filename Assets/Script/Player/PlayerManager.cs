using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Player.Stats;
using TopDown.CameraController;
using TopDown.Movement;

public class PlayerManager : MonoBehaviour
{
    [Header("Player Components")]
    public Health health;
    public Stats stats;
    public Inventory inventory;
    public Visible visibility;
    public PlayerMovement playerMovement;
    public CameraController cameraController;
    public Shoot shoot;

    [Header("UI and Managers")]
    public UIManager uiManager;

    public PlayerInput playerInput;

    [Header("Trigger Collider")]
    public Collider2D triggerCollider; // Reference to the player's trigger collider

    // Public readonly properties for lightLevel and soundLevel
    public float LightLevel => visibility != null ? visibility.LightLevel : 0.0f;
    public float SoundLevel => visibility != null ? visibility.soundLevel : 0.0f;

    private bool isShooting = false;
    private bool isReloading = false; // Prevent shooting during reload

    private bool isTakingDamage = false; // Whether the player is currently taking damage
    private float damageMultiplier = 1.0f; // Multiplier for incremental damage
    private Coroutine damageCoroutine; // Coroutine to handle incremental damage

    void Start()
    {
        // Initialize health
        if (health != null)
        {
            health.currentHealth = health.maxHealth;
        }

        // Initialize inventory and UI
        inventory.RestartAmmo();
        UpdateAmmoUI();
        UpdateWeaponUI();

        // Ensure the trigger collider is set up
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider2D>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true; // Ensure the collider is set as a trigger
            }
            else
            {
                Debug.LogError("No Collider2D found on the player! Please assign a trigger collider.");
            }
        }
    }

    void Update()
    {
        // Handle weapon switching
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SwitchWeapon();
        }

        // Handle firing
        if (Input.GetButtonDown("Fire1") && !isReloading)
        {
            StartShooting();
        }

        // Handle reloading
        if (Input.GetKeyDown(KeyCode.R) && !isReloading)
        {
            StartCoroutine(Reload());
        }
    }

    private void SwitchWeapon()
    {
        // Toggle between primary and secondary weapons
        if (inventory.CurrentWeapon == inventory.GetPrimaryWeapon())
        {
            inventory.ChangeWeapon(1); // Switch to secondary weapon
        }
        else
        {
            inventory.ChangeWeapon(0); // Switch to primary weapon
        }

        // Update the UI to reflect the new weapon
        UpdateWeaponUI();
        UpdateAmmoUI();
    }

    private void StartShooting()
    {
        WeaponInstance currentWeapon = inventory.CurrentWeapon; // Use WeaponInstance
        if (currentWeapon != null)
        {
            // Update the UI after firing
            UpdateAmmoUI();
            UpdateWeaponUI(); // Update weapon name and sound
            UpdateMagUI();

            isShooting = true;
            playerMovement.CanMove = false; // Disable movement while shooting
            shoot.isShooting = true; // Notify Shoot script
            Debug.Log($"Fired {currentWeapon.weaponName}");

            // Reset shooting state after a short delay
            StartCoroutine(ResetShooting());
        }
        else
        {
            Debug.Log("Cannot shoot: No weapon or out of ammo.");
        }
    }

    private IEnumerator ResetShooting()
    {
        yield return new WaitForSeconds(0.1f); // Adjust delay as needed for animation timing
        isShooting = false;
        playerMovement.CanMove = true; // Re-enable movement
        shoot.isShooting = false; // Notify Shoot script
    }

    private IEnumerator Reload()
    {
        WeaponInstance currentWeapon = inventory.CurrentWeapon; // Use WeaponInstance
        if (currentWeapon != null)
        {
            isReloading = true; // Prevent shooting during reload

            // Temporarily update the weapon UI to show "Reloading"
            if (uiManager != null)
            {
                uiManager.UpdateWeaponUI("Reloading", currentWeapon.sound, currentWeapon.ammoType, Color.white);
            }

            if (currentWeapon.bulletsInMagazine > 0)
            {
                // Normal reload
                Debug.Log("Performing normal reload...");
                yield return new WaitForSeconds(1.5f); // Shorter reload time
                int bulletsToReload = currentWeapon.magazineSize - currentWeapon.bulletsInMagazine;
                int totalReload = Mathf.Min(bulletsToReload, currentWeapon.totalAmmo);
                currentWeapon.bulletsInMagazine += totalReload; // Add bullets to the magazine
                currentWeapon.totalAmmo -= totalReload; // Deduct bullets from reserve
            }
            else
            {
                // Empty reload
                Debug.Log("Performing empty reload...");
                yield return new WaitForSeconds(1.5f); // Time for changing the magazine
                yield return new WaitForSeconds(1.0f); // Time for racking the gun

                int bulletsToReload = Mathf.Min(currentWeapon.magazineSize, currentWeapon.totalAmmo);
                currentWeapon.bulletsInMagazine = bulletsToReload; // Reload the magazine
                currentWeapon.totalAmmo -= bulletsToReload; // Deduct bullets from reserve
            }

            Debug.Log($"Reloaded {currentWeapon.weaponName}. Bullets in magazine: {currentWeapon.bulletsInMagazine}, Total ammo: {currentWeapon.totalAmmo}");

            // Update the UI after reload
            UpdateAmmoUI();
            // UpdateWeaponUI(); // Revert to the original weapon name
            isReloading = false; // Allow shooting again
        }
    }

    private void UpdateAmmoUI()
    {
        WeaponInstance currentWeapon = inventory.CurrentWeapon;
        if (uiManager != null && currentWeapon != null)
        {
            // Pass the actual bullets left in the magazine and the total reserved ammo
            uiManager.UpdateAmmo(currentWeapon.bulletsInMagazine, currentWeapon.totalAmmo, currentWeapon.ammoType);
        }
    }

    private void UpdateMagUI()
    {
        WeaponInstance currentWeapon = inventory.CurrentWeapon;
        Debug.Log($"Mags Current Weapon: {currentWeapon.weaponName}, Bullets in Magazine: {currentWeapon.bulletsInMagazine}");
        // Update the UI without modifying the actual bullet count
        if (uiManager != null && currentWeapon != null)
        {
            uiManager.UpdateMag(currentWeapon.bulletsInMagazine);
        }
    }

    private void UpdateWeaponUI()
    {
        WeaponInstance currentWeapon = inventory.CurrentWeapon;
        if (uiManager != null && currentWeapon != null)
        {
            // Get the weapon color based on the ammo type
            Color weaponColor = uiManager.GetWeaponColor(currentWeapon.ammoType);

            // Update the UI with the weapon name and color
            uiManager.UpdateWeaponUI(currentWeapon.weaponName, currentWeapon.sound, currentWeapon.ammoType, weaponColor);
        }
    }

    public void TakeDamage(int damage)
    {
        if (health != null)
        {
            health.TakeDamage(damage);
            uiManager?.UpdateHealth(health.currentHealth, health.maxHealth);

            if (health.currentHealth <= 0)
            {
                Die();
            }
        }
    }

    private void Die()
    {
        Debug.Log("Player has died.");
        // Additional death logic (e.g., respawn, game over screen)
    }

    public void AddAmmo(int amount)
    {
        WeaponInstance currentWeapon = inventory.CurrentWeapon; // Use WeaponInstance
        if (currentWeapon != null)
        {
            currentWeapon.totalAmmo += amount;
            currentWeapon.totalAmmo = Mathf.Clamp(currentWeapon.totalAmmo, 0, currentWeapon.magazineSize * 4); // Example: maxAmmo = 4x magazineSize
            Debug.Log($"Added {amount} ammo to {currentWeapon.weaponName}. Current ammo: {currentWeapon.totalAmmo}/{currentWeapon.magazineSize * 4}");
            UpdateAmmoUI();
        }
    }

    public void AddKill()
    {
        if (stats != null)
        {
            stats.AddKill();
            Debug.Log($"Player kills: {stats.GetKills()}");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Debug.Log($"Enemy entered trigger: {other.name}");
            StartTakingDamage();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Debug.Log($"Enemy exited trigger: {other.name}");
            StopTakingDamage();
        }
    }

    private void StartTakingDamage()
    {
        if (!isTakingDamage)
        {
            isTakingDamage = true;
            damageMultiplier = 1.0f; // Reset the damage multiplier
            damageCoroutine = StartCoroutine(IncrementalDamage());
        }
    }

    private void StopTakingDamage()
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
            if (health != null)
            {
                int damage = Mathf.CeilToInt(5 * damageMultiplier); // Base damage of 5, scaled by multiplier
                health.TakeDamage(damage);
                uiManager?.UpdateHealth(health.currentHealth, health.maxHealth);
                Debug.Log($"Player took {damage} damage. Current health: {health.currentHealth}");
            }

            damageMultiplier += 0.5f; // Increment the damage multiplier
            yield return new WaitForSeconds(1.0f); // Damage applied every second
        }
    }
}

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

    // Public readonly properties for lightLevel and soundLevel
    public float LightLevel => visibility != null ? visibility.LightLevel : 0.0f;
    public float SoundLevel => visibility != null ? visibility.soundLevel : 0.0f;

    private bool isShooting = false;

    void Start()
    {
        // Initialize health
        if (health != null)
        {
            health.currentHealth = health.maxHealth;
        }

        inventory.RestartAmmo(); // Initialize ammo for all weapons in the inventory
        UpdateAmmoUI();
        UpdateWeaponUI();
    }

    void Update()
    {
        // Update the sound slider in the UI based on SoundLevel
        if (uiManager != null)
        {
            uiManager.UpdateSoundSlider(SoundLevel);
        }
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (context.performed && !isShooting && playerMovement.CanMove)
        {
            StartShooting();
        }
    }

    private void StartShooting()
    {
        WeaponInstance currentWeapon = inventory.CurrentWeapon; // Use WeaponInstance
        if (currentWeapon != null && currentWeapon.Fire())
        {
            isShooting = true;
            playerMovement.CanMove = false; // Disable movement while shooting
            shoot.isShooting = true; // Notify Shoot script
            // shoot.TriggerShootAnimation(true); // Trigger shooting animation
            Debug.Log($"Fired {currentWeapon.weaponName}");
            UpdateAmmoUI();

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
        //shoot.TriggerShootAnimation(false); // Reset shooting animation
    }

    public void OnReload(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Reload();
        }
    }

    private void Reload()
    {
        WeaponInstance currentWeapon = inventory.CurrentWeapon; // Use WeaponInstance
        if (currentWeapon != null)
        {
            currentWeapon.Reload();
            Debug.Log($"Reloaded {currentWeapon.weaponName}");
            UpdateAmmoUI();
        }
    }

    public void OnChangeWeapon(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            int weaponSlot = Mathf.RoundToInt(context.ReadValue<float>()) - 1; // Convert 1-based input to 0-based index

            if (weaponSlot >= 0 && weaponSlot < inventory.weaponReferences.Count)
            {
                inventory.ChangeWeapon(weaponSlot); // Change to the selected weapon slot
                UpdateWeaponUI();
                UpdateAmmoUI();
            }
            else
            {
                Debug.Log("Invalid weapon slot selected.");
            }
        }
    }

    private void UpdateAmmoUI()
    {
        WeaponInstance currentWeapon = inventory.CurrentWeapon; // Use WeaponInstance
        if (uiManager != null && currentWeapon != null)
        {
            uiManager.UpdateAmmo(currentWeapon.bulletsInMagazine, currentWeapon.totalAmmo, currentWeapon.ammoType);
        }
    }

    private void UpdateWeaponUI()
    {
        WeaponInstance currentWeapon = inventory.CurrentWeapon; // Use WeaponInstance
        if (uiManager != null && currentWeapon != null)
        {
            uiManager.UpdateWeaponUI(currentWeapon.weaponName, currentWeapon.sound, currentWeapon.ammoType);
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
}

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
    public Health health; // Reference to the Health script
    public Stats stats; // Reference to the Stats script
    public Inventory inventory; // Reference to the Inventory script
    public Visible visibility; // Reference to the Visible script
    public PlayerMovement playerMovement; // Reference to the PlayerMovement script
    public CameraController cameraController; // Reference to the CameraController script

    [Header("UI and Managers")]
    public UIManager uiManager; // Reference to the UI Manager

    private PlayerInput playerInput;

    // Public readonly properties for lightLevel and soundLevel
    public float LightLevel => visibility != null ? visibility.LightLevel : 0.0f;
    public float SoundLevel => visibility != null ? visibility.soundLevel : 0.0f; // Initialize sound level from Visible script;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    void Start()
    {
        // Initialize health
        if (health != null)
        {
            health.currentHealth = health.maxHealth;
        }

        UpdateAmmoUI();
    }

    void Update()
    {
        // Update the sound slider in the UI based on SoundLevel
        if (uiManager != null)
        {   
            
            uiManager.UpdateSoundSlider(SoundLevel);
        }

        // Optionally log light and sound levels for debugging
        Debug.Log($"Light Level: {LightLevel}, Sound Level: {SoundLevel}");
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        Weapon currentWeapon = inventory.CurrentWeapon;
        if (currentWeapon != null && currentWeapon.Fire())
        {
            Debug.Log($"Fired {currentWeapon.weaponName}");
            UpdateAmmoUI();
        }
        else
        {
            Debug.Log("Cannot shoot: No weapon or out of ammo.");
        }
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
        Weapon currentWeapon = inventory.CurrentWeapon;
        if (currentWeapon != null)
        {
            currentWeapon.Reload();
            Debug.Log($"Reloaded {currentWeapon.weaponName}");
            UpdateAmmoUI();
        }
    }

    private void UpdateAmmoUI()
    {
        Weapon currentWeapon = inventory.CurrentWeapon;
        if (uiManager != null && currentWeapon != null)
        {
            // Pass the ammoType to the UpdateAmmo method
            uiManager.UpdateAmmo(currentWeapon.bulletsInMagazine, currentWeapon.totalAmmo, currentWeapon.ammoType);
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
        Weapon currentWeapon = inventory.CurrentWeapon;
        if (currentWeapon != null)
        {
            // Add ammo to totalAmmo but ensure it does not exceed maxAmmo
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

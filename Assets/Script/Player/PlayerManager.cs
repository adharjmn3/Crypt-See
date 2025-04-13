using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Player.Stats;
using TopDown.CameraController;

public class PlayerManager : MonoBehaviour
{
    [Header("Player Components")]
    public Health health; // Reference to the Health script
    public Stats stats; // Reference to the Stats script
    public Inventory inventory; // Reference to the Inventory script
    public Visible visibility; // Reference to the Visible script
    public CameraController cameraController; // Reference to the CameraController script

    [Header("UI and Managers")]
    public UIManager uiManager; // Reference to the UI Manager

    private PlayerInput playerInput;

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
        // Dynamically update the sound slider in the UI
        if (visibility != null && uiManager != null)
        {
            uiManager.UpdateSoundSlider(visibility.SoundLevel);
        }
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

    public void UpdateVisibility()
    {
        if (visibility != null)
        {
            float lightLevel = visibility.LightLevel;
            float soundLevel = visibility.SoundLevel;
            Debug.Log($"Light Level: {lightLevel}, Sound Level: {soundLevel}");
            // Additional logic for visibility (e.g., alert AI)
        }
    }

    // Expose Visible stats for other elements
    public float GetLightLevel()
    {
        return visibility != null ? visibility.LightLevel : 0.0f;
    }

    public float GetSoundLevel()
    {
        return visibility != null ? visibility.SoundLevel : 0.0f;
    }

    public Visible GetVisibility()
    {
        return visibility;
    }
}

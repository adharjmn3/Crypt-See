using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Shoot : MonoBehaviour
{
    private PlayerInput playerInput;
    private Inventory inventory;
    private UIManager uiManager;
    private PlayerManager playerManager; // Reference to PlayerManager

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        inventory = GetComponent<Inventory>();
        uiManager = FindObjectOfType<UIManager>();
        playerManager = GetComponent<PlayerManager>(); // Initialize PlayerManager
    }

    void OnEnable()
    {
        // Updated to use "Fire" instead of "Shoot"
        playerInput.actions["Fire"].performed += OnFire;
        playerInput.actions["Reload"].performed += OnReload;
    }

    void OnDisable()
    {
        // Updated to use "Fire" instead of "Shoot"
        playerInput.actions["Fire"].performed -= OnFire;
        playerInput.actions["Reload"].performed -= OnReload;
    }

    public void OnFire(InputAction.CallbackContext context) // Renamed from OnShoot to OnFire
    {
        Weapon currentWeapon = inventory.CurrentWeapon;
        if (currentWeapon != null && currentWeapon.Fire())
        {
            Debug.Log($"Fired {currentWeapon.weaponName}");

            // Add weapon sound to the Visible component via PlayerManager
            // Visible visibility = playerManager?.GetVisibility();
            // if (visibility != null)
            // {
            //     visibility.AddWeaponSound(currentWeapon.sound * 0.2f); // Scale weapon sound as needed
            // }

            UpdateWeaponUI();
        }
        else
        {
            Debug.Log("Cannot fire: No weapon or out of ammo.");
        }
    }

    public void OnReload(InputAction.CallbackContext context)
    {
        Weapon currentWeapon = inventory.CurrentWeapon;
        if (currentWeapon != null)
        {
            currentWeapon.Reload();
            Debug.Log($"Reloaded {currentWeapon.weaponName}");
            UpdateWeaponUI();
        }
    }

    private void UpdateWeaponUI()
    {
        Weapon currentWeapon = inventory.CurrentWeapon;
        if (uiManager != null && currentWeapon != null)
        {
            // Update ammo and weapon name in the UI
            uiManager.UpdateAmmo(currentWeapon.bulletsInMagazine, currentWeapon.totalAmmo, currentWeapon.ammoType);
            uiManager.UpdateWeaponUI(currentWeapon.weaponName, currentWeapon.sound, currentWeapon.ammoType);
        }
    }
}

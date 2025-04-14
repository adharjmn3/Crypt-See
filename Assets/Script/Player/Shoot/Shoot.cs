using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TopDown.Movement; // Add this line to fix the namespace issue

public class Shoot : MonoBehaviour
{
    public bool isShooting = false;
    private PlayerInput playerInput;
    private Inventory inventory;
    private UIManager uiManager;
    private PlayerMovement playerMovement;
    private Animator animator;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        inventory = GetComponent<Inventory>();
        uiManager = FindObjectOfType<UIManager>();
        playerMovement = GetComponent<PlayerMovement>();
        animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        playerInput.actions["Fire"].performed += OnFire;
        playerInput.actions["Fire"].canceled += OnFireCanceled;
        playerInput.actions["Reload"].performed += OnReload;
    }

    void OnDisable()
    {
        playerInput.actions["Fire"].performed -= OnFire;
        playerInput.actions["Fire"].canceled -= OnFireCanceled;
        playerInput.actions["Reload"].performed -= OnReload;
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        if (!isShooting && playerMovement.CanMove) // Only shoot if not already shooting and player can move
        {
            WeaponInstance currentWeapon = inventory.CurrentWeapon;
                UpdateWeaponUI();
            if (currentWeapon != null && currentWeapon.Fire())
            {
                isShooting = true;
                playerMovement.CanMove = false; // Disable movement while shooting
                TriggerShootAnimation(true); // Set the shooting animation
                Debug.Log($"Fired {currentWeapon.weaponName}");
            }
            else
            {
                Debug.Log("Cannot fire: No weapon or out of ammo.");
            }
        }
    }

    public void OnFireCanceled(InputAction.CallbackContext context)
    {
        if (isShooting)
        {
            isShooting = false;
            playerMovement.CanMove = true; // Re-enable movement
            TriggerShootAnimation(false); // Reset the shooting animation
        }
    }

    public void OnReload(InputAction.CallbackContext context)
    {
        WeaponInstance currentWeapon = inventory.CurrentWeapon;
        if (currentWeapon != null)
        {
            currentWeapon.Reload(); // Reload the weapon
            Debug.Log($"Reloaded {currentWeapon.weaponName}");
            UpdateWeaponUI(); // Update the UI immediately after reloading
        }
    }

    private void UpdateWeaponUI()
    {
        WeaponInstance currentWeapon = inventory.CurrentWeapon;
        if (uiManager != null && currentWeapon != null)
        {
            Debug.Log($"Updating UI: Bullets in magazine: {currentWeapon.bulletsInMagazine}, Total ammo: {currentWeapon.totalAmmo}");
            uiManager.UpdateAmmo(currentWeapon.bulletsInMagazine, currentWeapon.totalAmmo, currentWeapon.ammoType);
            uiManager.UpdateWeaponUI(currentWeapon.weaponName, currentWeapon.sound, currentWeapon.ammoType);
        }
    }

    public void TriggerShootAnimation(bool isShooting)
    {
        animator.SetBool("isShoot", isShooting);
    }
}

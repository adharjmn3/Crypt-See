using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Shoot : MonoBehaviour
{
    private PlayerInput playerInput;
    private Inventory inventory;
    private UIManager uiManager;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        inventory = GetComponent<Inventory>();
        uiManager = FindObjectOfType<UIManager>();
    }

    void OnEnable()
    {
        playerInput.actions["Fire"].performed += OnFire;
        playerInput.actions["Reload"].performed += OnReload;
    }

    void OnDisable()
    {
        playerInput.actions["Fire"].performed -= OnFire;
        playerInput.actions["Reload"].performed -= OnReload;
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        WeaponInstance currentWeapon = inventory.CurrentWeapon;
        if (currentWeapon != null && currentWeapon.Fire())
        {
            Debug.Log($"Fired {currentWeapon.weaponName}");
            UpdateWeaponUI();
        }
        else
        {
            Debug.Log("Cannot fire: No weapon or out of ammo.");
        }
    }

    public void OnReload(InputAction.CallbackContext context)
    {
        WeaponInstance currentWeapon = inventory.CurrentWeapon;
        if (currentWeapon != null)
        {
            currentWeapon.Reload();
            Debug.Log($"Reloaded {currentWeapon.weaponName}");
            UpdateWeaponUI();
        }
    }

    private void UpdateWeaponUI()
    {
        WeaponInstance currentWeapon = inventory.CurrentWeapon;
        if (uiManager != null && currentWeapon != null)
        {
            uiManager.UpdateAmmo(currentWeapon.bulletsInMagazine, currentWeapon.totalAmmo, currentWeapon.ammoType);
            uiManager.UpdateWeaponUI(currentWeapon.weaponName, currentWeapon.sound, currentWeapon.ammoType);
        }
    }
}

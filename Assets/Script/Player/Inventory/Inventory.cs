using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Header("Weapon Slots")]
    public Weapon primaryWeaponData; // Assignable in the Unity Editor
    public Weapon secondaryWeaponData; // Assignable in the Unity Editor

    private WeaponInstance primaryWeapon; // Primary weapon instance
    private WeaponInstance secondaryWeapon; // Secondary weapon instance

    [SerializeField, Tooltip("Currently equipped weapon (read-only)")]
    private WeaponInstance currentWeapon; // Currently equipped weapon

    public WeaponInstance CurrentWeapon => currentWeapon;

    void Start()
    {
        // Initialize weapon instances from the assigned Weapon ScriptableObjects
        if (primaryWeaponData != null)
        {
            primaryWeapon = new WeaponInstance(primaryWeaponData);
        }

        if (secondaryWeaponData != null)
        {
            secondaryWeapon = new WeaponInstance(secondaryWeaponData);
        }

        // Set the current weapon to the primary weapon by default
        currentWeapon = primaryWeapon;
    }

    public void ChangeWeapon(int weaponSlot)
    {
        if (weaponSlot == 0 && primaryWeapon != null)
        {
            currentWeapon = primaryWeapon;
            Debug.Log($"Switched to Primary Weapon: {primaryWeapon.weaponName}");
        }
        else if (weaponSlot == 1 && secondaryWeapon != null)
        {
            currentWeapon = secondaryWeapon;
            Debug.Log($"Switched to Secondary Weapon: {secondaryWeapon.weaponName}");
        }
        else
        {
            Debug.Log("Invalid weapon slot or weapon not equipped.");
        }

        // Update the UI after switching weapons
        UpdateWeaponUI();
        UpdateAmmoUI();
    }

    public WeaponInstance GetPrimaryWeapon()
    {
        return primaryWeapon;
    }

    public WeaponInstance GetSecondaryWeapon()
    {
        return secondaryWeapon;
    }

    public void RestartAmmo()
    {
        // Restart ammo for both weapons
        if (primaryWeapon != null)
        {
            primaryWeapon.bulletsInMagazine = primaryWeapon.magazineSize;
            primaryWeapon.totalAmmo = primaryWeapon.magazineSize * 4; // Example: max ammo = 4x magazine size
        }

        if (secondaryWeapon != null)
        {
            secondaryWeapon.bulletsInMagazine = secondaryWeapon.magazineSize;
            secondaryWeapon.totalAmmo = secondaryWeapon.magazineSize * 4; // Example: max ammo = 4x magazine size
        }

        Debug.Log("Ammo has been restarted for both weapons.");
    }

    private void UpdateWeaponUI()
    {
        if (currentWeapon != null)
        {
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateWeaponUI(currentWeapon.weaponName, currentWeapon.sound, currentWeapon.ammoType);
            }
        }
    }

    private void UpdateAmmoUI()
    {
        if (currentWeapon != null)
        {
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateAmmo(currentWeapon.bulletsInMagazine, currentWeapon.totalAmmo, currentWeapon.ammoType);
            }
        }
    }
}
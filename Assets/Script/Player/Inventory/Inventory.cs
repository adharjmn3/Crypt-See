using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Header("Weapon Slots")]
    public Weapon primaryWeaponData; // Assignable in the Unity Editor
    public Weapon secondaryWeaponData; // Assignable in the Unity Editor

    private WeaponInstance primaryWeapon; // Primary weapon instance
    private WeaponInstance secondaryWeapon; // Secondary weapon instance
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
        primaryWeapon?.Reload();
        secondaryWeapon?.Reload();
        Debug.Log("Ammo has been restarted for both weapons.");
    }
}
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public List<Weapon> weapons = new List<Weapon>(); // List to store weapons
    private int currentWeaponIndex = 0; // Index of the currently equipped weapon
    private const int weaponLimit = 2; // Limit for the number of weapons

    public Weapon CurrentWeapon => weapons.Count > 0 ? weapons[currentWeaponIndex] : null;

    // Method to add a weapon to the inventory
    public void AddWeapon(Weapon weapon)
    {
        if (weapons.Count < weaponLimit)
        {
            weapons.Add(weapon);
            Debug.Log($"Added weapon: {weapon.weaponName} to inventory.");
        }
        else
        {
            Debug.Log("Weapon inventory is full. Cannot add more weapons.");
        }
    }

    // Method to switch to the next weapon
    public void SwitchWeapon()
    {
        if (weapons.Count > 1)
        {
            currentWeaponIndex = (currentWeaponIndex + 1) % weapons.Count;
            Debug.Log($"Switched to weapon: {weapons[currentWeaponIndex].weaponName}");
        }
        else
        {
            Debug.Log("Not enough weapons to switch.");
        }
    }

    // Method to remove a weapon from the inventory
    public void RemoveWeapon(Weapon weapon)
    {
        if (weapons.Contains(weapon))
        {
            weapons.Remove(weapon);
            Debug.Log($"Removed weapon: {weapon.weaponName} from inventory.");

            // Adjust the current weapon index if necessary
            if (currentWeaponIndex >= weapons.Count)
            {
                currentWeaponIndex = Mathf.Max(0, weapons.Count - 1);
            }
        }
        else
        {
            Debug.Log("Weapon not found in inventory.");
        }
    }

    // Method to reload the current weapon
    public void ReloadCurrentWeapon()
    {
        Weapon currentWeapon = CurrentWeapon;
        if (currentWeapon != null)
        {
            currentWeapon.Reload();
        }
        else
        {
            Debug.Log("No weapon equipped to reload.");
        }
    }

    // Method to add ammo to the current weapon
    public void AddAmmoToCurrentWeapon(int amount)
    {
        Weapon currentWeapon = CurrentWeapon;
        if (currentWeapon != null)
        {
            currentWeapon.AddAmmo(amount);
        }
        else
        {
            Debug.Log("No weapon equipped to add ammo.");
        }
    }

    // Method to change to a specific weapon by index
    public void ChangeWeapon(int weaponIndex)
    {
        if (weaponIndex >= 0 && weaponIndex < weapons.Count)
        {
            currentWeaponIndex = weaponIndex;
            Debug.Log($"Changed to weapon: {weapons[currentWeaponIndex].weaponName}");
        }
        else
        {
            Debug.Log("Invalid weapon index. Cannot change weapon.");
        }
    }

    public void RestartAmmo()
    {
        foreach (var weapon in weapons)
        {
            weapon.Initialize(); // Reset the weapon to its initial state
        }
    }
}
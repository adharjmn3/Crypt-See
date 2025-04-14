using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public List<Weapon> weaponReferences = new List<Weapon>(); // Reference to ScriptableObjects
    private List<WeaponInstance> weaponInstances = new List<WeaponInstance>(); // Runtime weapon instances
    private int currentWeaponIndex = 0; // Index of the currently equipped weapon
    private const int weaponLimit = 2; // Limit for the number of weapons

    public WeaponInstance CurrentWeapon => weaponInstances.Count > 0 ? weaponInstances[currentWeaponIndex] : null;

    void Start()
    {
        // Initialize weapon instances from references
        foreach (var weapon in weaponReferences)
        {
            weaponInstances.Add(new WeaponInstance(weapon));
        }
    }

    public void AddWeapon(Weapon weapon)
    {
        if (weaponInstances.Count < weaponLimit)
        {
            weaponInstances.Add(new WeaponInstance(weapon));
            Debug.Log($"Added weapon: {weapon.weaponName} to inventory.");
        }
        else
        {
            Debug.Log("Weapon inventory is full. Cannot add more weapons.");
        }
    }

    public void SwitchWeapon()
    {
        if (weaponInstances.Count > 1)
        {
            currentWeaponIndex = (currentWeaponIndex + 1) % weaponInstances.Count;
            Debug.Log($"Switched to weapon: {weaponInstances[currentWeaponIndex].weaponName}");
        }
        else
        {
            Debug.Log("Not enough weapons to switch.");
        }
    }

    public void ChangeWeapon(int weaponIndex)
    {
        if (weaponIndex >= 0 && weaponIndex < weaponInstances.Count)
        {
            currentWeaponIndex = weaponIndex;
            Debug.Log($"Changed to weapon: {weaponInstances[currentWeaponIndex].weaponName}");
        }
        else
        {
            Debug.Log("Invalid weapon index. Cannot change weapon.");
        }
    }

    public void RestartAmmo()
    {
        foreach (var weaponInstance in weaponInstances)
        {
            weaponInstance.bulletsInMagazine = weaponInstance.magazineSize; // Reset to full magazine
            weaponInstance.totalAmmo = weaponInstance.magazineSize * 4; // Example: max ammo = 4x magazine size
        }
        Debug.Log("Ammo has been restarted for all weapons.");
    }
}
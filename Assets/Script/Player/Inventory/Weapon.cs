using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapon")]
public class Weapon : ScriptableObject
{
    public enum AmmoType { Kinetic, EMP, Unarmed }

    public string weaponName; // Name of the weapon
    public AmmoType ammoType; // Type of ammo
    public int magazineSize;  // Maximum bullets in the magazine
    public int bulletsInMagazine; // Current bullets in the magazine
    public int totalAmmo;     // Total ammo the player has (including reserve)
    public int damage;        // Damage dealt by the weapon
    public float fireRate;    // Rate of fire (shots per second)
    public float range;       // Range of the weapon
    [Range(0, 5)] public int sound; // Sound level (0 = silent, 5 = very loud)

    private float lastFireTime; // Tracks the last time the weapon was fired

    private int originalBulletsInMagazine; // Store the original bullets in magazine for reloading
    private int originalTotalAmmo; // Store the original total ammo for reloading
    // Method to reload the weapon
    public void Reload()
    {
        if (totalAmmo > 0)
        {
            int bulletsNeeded = magazineSize - bulletsInMagazine;
            int bulletsToReload = Mathf.Min(bulletsNeeded, totalAmmo);

            bulletsInMagazine += bulletsToReload;
            totalAmmo -= bulletsToReload;

            Debug.Log($"{weaponName} reloaded. Bullets in magazine: {bulletsInMagazine}, Total ammo: {totalAmmo}");
        }
        else
        {
            Debug.Log($"{weaponName} has no ammo left to reload.");
        }
    }

    // Method to fire the weapon
    public bool Fire()
    {
        if (Time.time - lastFireTime < 1f / fireRate)
        {
            Debug.Log("Weapon is on cooldown.");
            return false; // Cooldown not finished
        }

        if (bulletsInMagazine > 0)
        {
            bulletsInMagazine--;
            lastFireTime = Time.time;
            Debug.Log($"Fired a {ammoType} bullet from {weaponName}! Bullets left in magazine: {bulletsInMagazine}");
            return true; // Successfully fired
        }
        else
        {
            Debug.Log($"{weaponName} is out of bullets in the magazine! Reload required.");
            return false; // Failed to fire
        }
    }

    // Method to add ammo to the total pool
    public void AddAmmo(int amount)
    {
        totalAmmo += amount;
        Debug.Log($"Added {amount} ammo to {weaponName}. Total ammo: {totalAmmo}");
    }

    public void Initialize()
    {
        originalBulletsInMagazine = magazineSize; // Set to max size initially
        originalTotalAmmo = totalAmmo; // Store the initial total ammo
        bulletsInMagazine = magazineSize; // Start with a full magazine
    }


}

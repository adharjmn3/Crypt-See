using UnityEngine;

public class WeaponInstance
{
    public string weaponName;
    public Weapon.AmmoType ammoType;
    public Weapon.AmmoType weaponType; // Add this property
    public int magazineSize;
    public int bulletsInMagazine;
    public int totalAmmo;
    public int damage;
    public float fireRate;
    public float range;
    public float bulletSpeed; // New property for bullet speed
    public int sound;

    private float lastFireTime;

    public WeaponInstance(Weapon weapon)
    {
        weaponName = weapon.weaponName;
        ammoType = weapon.ammoType;
        weaponType = weapon.ammoType; // Initialize weaponType
        magazineSize = weapon.magazineSize;
        bulletsInMagazine = weapon.bulletsInMagazine;
        totalAmmo = weapon.totalAmmo;
        damage = weapon.damage;
        fireRate = weapon.fireRate;
        range = weapon.range;
        bulletSpeed = weapon.bulletSpeed; // Initialize bullet speed
        sound = weapon.sound;
        lastFireTime = 0f;
    }

    public bool Fire()
    {
        if (Time.time - lastFireTime < 1f / fireRate)
        {
            Debug.Log("Weapon is on cooldown.");
            return false; // Cooldown not finished
        }

        if (bulletsInMagazine > 0)
        {
            bulletsInMagazine--; // Decrease bullets in the magazine
            lastFireTime = Time.time;
            Debug.Log($"Fired a bullet! Bullets left in magazine: {bulletsInMagazine}");
            return true; // Successfully fired
        }
        else
        {
            Debug.Log("Out of bullets in the magazine! Reload required.");
            return false; // Failed to fire
        }
    }

    public void Reload(bool isEmptyReload)
    {
        if (totalAmmo > 0)
        {
            if (isEmptyReload)
            {
                // Empty reload: Reload only up to the magazine size
                int bulletsToReload = Mathf.Min(magazineSize, totalAmmo);
                bulletsInMagazine = bulletsToReload; // Fill the magazine
                totalAmmo -= bulletsToReload; // Decrease total ammo
                Debug.Log($"Empty reload completed. Bullets in magazine: {bulletsInMagazine}, Total ammo: {totalAmmo}");
            }
            else
            {
                // Non-empty reload: Reload with +1 bullet
                int bulletsNeeded = magazineSize - bulletsInMagazine;
                int bulletsToReload = Mathf.Min(bulletsNeeded, totalAmmo);
                bulletsInMagazine += bulletsToReload + 1; // Add bullets and +1
                totalAmmo -= bulletsToReload; // Decrease total ammo
                Debug.Log($"Normal reload completed (+1). Bullets in magazine: {bulletsInMagazine}, Total ammo: {totalAmmo}");
            }
        }
        else
        {
            Debug.Log("No ammo left to reload.");
        }
    }
}
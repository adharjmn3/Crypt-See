using UnityEngine;

public class WeaponInstance
{
    public string weaponName;
    public Weapon.AmmoType ammoType;
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

    public void Reload()
    {
        if (totalAmmo > 0)
        {
            int bulletsNeeded = magazineSize - bulletsInMagazine;
            int bulletsToReload = Mathf.Min(bulletsNeeded, totalAmmo);

            bulletsInMagazine += bulletsToReload; // Add bullets to the magazine
            totalAmmo -= bulletsToReload; // Decrease total ammo

            Debug.Log($"Reloaded weapon. Bullets in magazine: {bulletsInMagazine}, Total ammo: {totalAmmo}");
        }
        else
        {
            Debug.Log("No ammo left to reload.");
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TopDown.Movement;

public class Shoot : MonoBehaviour
{
    [Header("Effects")]
    public ParticleSystem muzzleFlash; // Particle effect for muzzle flash
    public Transform casingEjectPoint; // Transform for bullet casing ejection
    public GameObject casingPrefab; // Prefab for bullet casing
    public GameObject magazinePrefab; // Prefab for magazine eject object
    public GameObject casingContainer; // Empty GameObject to hold casings
    public AudioSource audioSource; // AudioSource for weapon sounds

    [Header("Casing Ejection Settings")]
    public float ejectionForceMin = 1.5f; // Minimum force applied to the casing
    public float ejectionForceMax = 2.5f; // Maximum force applied to the casing

    [Header("Weapon Sounds")]
    public AudioClip kineticSound; // Sound for kinetic weapons
    public AudioClip empSound; // Sound for EMP weapons
    public AudioClip punchSound; // Sound for punching
    public AudioClip reloadSound; // Sound for normal reload
    public AudioClip emptyReloadSound; // Sound for empty reload
    public AudioClip changeMagazineSound; // Sound for changing the magazine
    public AudioClip gunRackingSound; // Sound for racking the gun

    [Header("Bullet Settings")]
    public GameObject bulletPrefab; // Prefab for the bullet
    public Transform bulletSpawnPoint; // Transform for bullet spawn

    private Inventory inventory;
    private PlayerMovement playerMovement;
    private Animator animator;
    public bool isShooting = false;
    private bool isReloading = false; // Prevent shooting during reload

    private Queue<GameObject> casingQueue = new Queue<GameObject>(); // Queue to manage casings
    private const int maxCasings = 25; // Maximum number of casings allowed

    void Awake()
    {
        inventory = GetComponent<Inventory>();
        playerMovement = GetComponent<PlayerMovement>();
        animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        PlayerInput playerInput = GetComponent<PlayerInput>();
        playerInput.actions["Fire"].performed += OnFire;
        playerInput.actions["Fire"].canceled += OnFireCanceled;
        playerInput.actions["Reload"].performed += OnReload;
    }

    void OnDisable()
    {
        PlayerInput playerInput = GetComponent<PlayerInput>();
        playerInput.actions["Fire"].performed -= OnFire;
        playerInput.actions["Fire"].canceled -= OnFireCanceled;
        playerInput.actions["Reload"].performed -= OnReload;
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        if (context.performed && !isShooting && !isReloading && playerMovement.CanMove)
        {
            WeaponInstance currentWeapon = inventory.CurrentWeapon;
            if (currentWeapon != null)
            {
                if (currentWeapon.Fire())
                {
                    StartShooting(currentWeapon);
                }
                else
                {
                    Debug.Log("Cannot fire: Out of ammo.");
                    PlayEmptySound(); // Play empty weapon sound
                }
            }
            else
            {
                Debug.Log("Cannot fire: No weapon equipped.");
            }
        }
    }

    private void OnFireCanceled(InputAction.CallbackContext context)
    {
        // Reset shooting state when the fire button is released
        isShooting = false;
        playerMovement.CanMove = true; // Re-enable movement
        animator.SetBool("isShoot", false); // Reset shooting animation
        Debug.Log("Fire action canceled.");
    }

    private void StartShooting(WeaponInstance currentWeapon)
    {
        Debug.Log("StartShooting called");
        isShooting = true;
        playerMovement.CanMove = false; // Disable movement while shooting
        animator.SetBool("isShoot", true); // Trigger shooting animation

        // Play muzzle flash
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
        else
        {
            Debug.LogWarning("Muzzle flash is null");
        }

        // Eject casing
        if (currentWeapon.ammoType == Weapon.AmmoType.Kinetic || currentWeapon.ammoType == Weapon.AmmoType.EMP)
        {
            EjectCasing();
        }

        // Spawn bullet
        SpawnBullet(currentWeapon);

        // Play weapon sound
        PlayWeaponSound(currentWeapon);

        // Reset shooting state after a short delay
        StartCoroutine(ResetShooting());
    }

    private IEnumerator ResetShooting()
    {
        yield return new WaitForSeconds(0.1f); // Adjust delay as needed for animation timing
        isShooting = false;
        playerMovement.CanMove = true; // Re-enable movement
        animator.SetBool("isShoot", false); // Reset shooting animation
    }

    public void OnReload(InputAction.CallbackContext context)
    {
        if (context.performed && !isReloading)
        {
            StartCoroutine(ReloadWeapon());
        }
    }

    private IEnumerator ReloadWeapon()
    {
        WeaponInstance currentWeapon = inventory.CurrentWeapon;
        if (currentWeapon != null)
        {
            isReloading = true; // Prevent shooting during reload

            if (currentWeapon.bulletsInMagazine > 0)
            {
                // Normal reload with +1 bullet
                Debug.Log("Reloading with bullets in magazine...");
                PlayReloadSound(reloadSound);
                yield return new WaitForSeconds(1.5f); // Shorter reload time
                int bulletsToReload = currentWeapon.magazineSize - currentWeapon.bulletsInMagazine;
                int totalReload = Mathf.Min(bulletsToReload, currentWeapon.totalAmmo);
                currentWeapon.bulletsInMagazine += totalReload + 1; // Add +1 bullet
                currentWeapon.totalAmmo -= totalReload;
            }
            else
            {
                // Empty reload
                Debug.Log("Reloading from empty...");
                PlayReloadSound(changeMagazineSound); // Play change magazine sound
                yield return new WaitForSeconds(1.5f); // Time for changing the magazine

                if (casingEjectPoint != null)
                {
                    GameObject magazine = Instantiate(magazinePrefab, casingEjectPoint.position, casingEjectPoint.rotation);
                    Rigidbody rb = magazine.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.AddForce(casingEjectPoint.right * Random.Range(ejectionForceMin, ejectionForceMax), ForceMode.Impulse);
                    }
                }
                PlayReloadSound(gunRackingSound); // Play gun racking sound
                yield return new WaitForSeconds(1.0f); // Time for racking the gun

                int bulletsToReload = Mathf.Min(currentWeapon.magazineSize, currentWeapon.totalAmmo);
                currentWeapon.bulletsInMagazine = bulletsToReload;
                currentWeapon.totalAmmo -= bulletsToReload;

                // Spawn magazine eject object (commented for now)
            }

            Debug.Log($"Reloaded {currentWeapon.weaponName}. Bullets in magazine: {currentWeapon.bulletsInMagazine}, Total ammo: {currentWeapon.totalAmmo}");
            UpdateWeaponUI(); // Update the UI after reload
            isReloading = false; // Allow shooting again
        }
    }

    private void PlayReloadSound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    private void PlayEmptySound()
    {
        if (audioSource != null && emptyReloadSound != null)
        {
            audioSource.PlayOneShot(emptyReloadSound);
        }
    }

    private void EjectCasing()
    {
        if (casingPrefab != null && casingEjectPoint != null)
        {
            // Instantiate the casing
            GameObject casing = Instantiate(casingPrefab, casingEjectPoint.position, casingEjectPoint.rotation);

            // Assign the ejection direction
            Rigidbody rb = casing.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(casingEjectPoint.right * Random.Range(ejectionForceMin, ejectionForceMax), ForceMode.Impulse);
            }
        }
    }

    private void SpawnBullet(WeaponInstance currentWeapon)
    {
        if (bulletPrefab != null && bulletSpawnPoint != null)
        {
            // Instantiate the bullet
            GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);

            // Get the Bullet script and initialize it
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.Initialize(currentWeapon.range, currentWeapon.damage, currentWeapon.ammoType);
            }

            // Apply velocity to the bullet
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = (bulletSpawnPoint.right * currentWeapon.bulletSpeed); // Set bullet velocity
            }
        }
    }

    private void PlayWeaponSound(WeaponInstance currentWeapon)
    {
        if (audioSource != null)
        {
            AudioClip clipToPlay = null;

            // Determine the sound based on the weapon type
            switch (currentWeapon.ammoType)
            {
                case Weapon.AmmoType.Kinetic:
                    clipToPlay = kineticSound;
                    break;
                case Weapon.AmmoType.EMP:
                    clipToPlay = empSound;
                    break;
                case Weapon.AmmoType.Unarmed:
                    clipToPlay = punchSound;
                    break;
            }

            if (clipToPlay != null)
            {
                audioSource.clip = clipToPlay;
                audioSource.volume = Mathf.Clamp01(currentWeapon.sound / 5f); // Adjust volume based on weapon sound level (0-5)
                audioSource.Play();
            }
        }
    }

    private void UpdateWeaponUI()
    {
        WeaponInstance currentWeapon = inventory.CurrentWeapon;
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null && currentWeapon != null)
        {
            // Get the weapon color based on the weapon type
            Color weaponColor = uiManager.GetWeaponColor(currentWeapon.weaponType);

            // Update the UI with the weapon name and color
            uiManager.UpdateWeaponUI(currentWeapon.weaponName, currentWeapon.sound, currentWeapon.ammoType, weaponColor);
        }
    }
}



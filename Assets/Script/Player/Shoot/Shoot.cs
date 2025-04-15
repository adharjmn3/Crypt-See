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
    public GameObject casingContainer; // Empty GameObject to hold casings
    public AudioSource audioSource; // AudioSource for weapon sounds

    [Header("Casing Ejection Settings")]
    public float ejectionForceMin = 1.5f; // Minimum force applied to the casing
    public float ejectionForceMax = 2.5f; // Maximum force applied to the casing

    [Header("Weapon Sounds")]
    public AudioClip kineticSound; // Sound for kinetic weapons
    public AudioClip empSound; // Sound for EMP weapons
    public AudioClip punchSound; // Sound for punching

    private Inventory inventory;
    private PlayerMovement playerMovement;
    private Animator animator;
    public bool isShooting = false;

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
        if (context.performed && !isShooting && playerMovement.CanMove)
        {
            WeaponInstance currentWeapon = inventory.CurrentWeapon;
            if (currentWeapon != null && currentWeapon.Fire())
            {
                StartShooting(currentWeapon);
            }
            else
            {
                Debug.Log("Cannot fire: No weapon equipped or out of ammo.");
            }
        }
    }

    private void StartShooting(WeaponInstance currentWeapon)
    {
        isShooting = true;
        playerMovement.CanMove = false; // Disable movement while shooting
        animator.SetBool("isShoot", true); // Trigger shooting animation

        // Play muzzle flash
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        // Eject casing (only for kinetic and EMP weapons)
        if (currentWeapon.ammoType == Weapon.AmmoType.Kinetic || currentWeapon.ammoType == Weapon.AmmoType.EMP)
        {
            EjectCasing();
        }

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

    public void OnFireCanceled(InputAction.CallbackContext context)
    {
        if (isShooting)
        {
            isShooting = false;
            playerMovement.CanMove = true; // Re-enable movement
            animator.SetBool("isShoot", false); // Reset shooting animation
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

    private void EjectCasing()
    {
        if (casingPrefab != null && casingEjectPoint != null)
        {
            // Instantiate the casing
            GameObject casing = Instantiate(casingPrefab, casingEjectPoint.position, casingEjectPoint.rotation);

            // Parent the casing to the container
            if (casingContainer != null)
            {
                casing.transform.SetParent(casingContainer.transform);
            }

            // Add the casing to the queue
            casingQueue.Enqueue(casing);
            if (casingQueue.Count > maxCasings)
            {
                GameObject oldestCasing = casingQueue.Dequeue();
                Destroy(oldestCasing); // Destroy the oldest casing
            }
            
        }
    }

    // private IEnumerator ApplyCasingForce(GameObject casing)
    // {
    //     yield return null; // Wait for one frame to ensure the casing is fully initialized

    //     Rigidbody2D rb = casing.GetComponent<Rigidbody2D>();
    //     if (rb != null)
    //     {
    //         // Apply force to simulate ejection
    //         Vector2 ejectionForce = (Vector2)casingEjectPoint.right * Random.Range(ejectionForceMin, ejectionForceMax)
    //                                 + Vector2.up * Random.Range(0.5f, 1.0f);
    //         rb.AddForce(ejectionForce, ForceMode2D.Impulse);
    //     }
    // }

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
            Debug.Log($"Updating UI: Bullets in magazine: {currentWeapon.bulletsInMagazine}, Total ammo: {currentWeapon.totalAmmo}");
            uiManager.UpdateAmmo(currentWeapon.bulletsInMagazine, currentWeapon.totalAmmo, currentWeapon.ammoType);
            uiManager.UpdateWeaponUI(currentWeapon.weaponName, currentWeapon.sound, currentWeapon.ammoType);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TopDown.Movement;

public class Shoot : MonoBehaviour
{
    public bool isShooting = false;

    [Header("Effects")]
    public ParticleSystem muzzleFlash; // Particle effect for muzzle flash
    public Transform casingEjectPoint; // Transform for bullet casing ejection
    public GameObject casingPrefab; // Prefab for bullet casing
    public AudioSource audioSource; // AudioSource for weapon sounds

    [Header("Weapon Sounds")]
    public AudioClip kineticSound; // Sound for kinetic weapons
    public AudioClip empSound; // Sound for EMP weapons
    public AudioClip punchSound; // Sound for punching

    private PlayerInput playerInput;
    private Inventory inventory;
    private UIManager uiManager;
    private PlayerMovement playerMovement;
    private Animator animator;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        inventory = GetComponent<Inventory>();
        uiManager = FindObjectOfType<UIManager>();
        playerMovement = GetComponent<PlayerMovement>();
        animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        playerInput.actions["Fire"].performed += OnFire;
        playerInput.actions["Fire"].canceled += OnFireCanceled;
        playerInput.actions["Reload"].performed += OnReload;
    }

    void OnDisable()
    {
        playerInput.actions["Fire"].performed -= OnFire;
        playerInput.actions["Fire"].canceled -= OnFireCanceled;
        playerInput.actions["Reload"].performed -= OnReload;
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        if (!isShooting && playerMovement.CanMove) // Only shoot if not already shooting and player can move
        {
            WeaponInstance currentWeapon = inventory.CurrentWeapon;
            if (currentWeapon != null && currentWeapon.Fire())
            {
                isShooting = true;
                playerMovement.CanMove = false; // Disable movement while shooting
                TriggerShootAnimation(true); // Set the shooting animation
                PlayMuzzleFlash(); // Play particle effect
                EjectCasing(); // Eject bullet casing
                PlayWeaponSound(currentWeapon); // Play weapon sound
                Debug.Log($"Fired {currentWeapon.weaponName}");
                UpdateWeaponUI();
            }
            else
            {
                Debug.Log("Cannot fire: No weapon or out of ammo.");
            }
        }
    }

    public void OnFireCanceled(InputAction.CallbackContext context)
    {
        if (isShooting)
        {
            isShooting = false;
            playerMovement.CanMove = true; // Re-enable movement
            TriggerShootAnimation(false); // Reset the shooting animation
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

    private void PlayMuzzleFlash()
    {
        if (muzzleFlash != null)
        {
            muzzleFlash.Play(); // Play the muzzle flash particle effect
        }
    }

    private void EjectCasing()
    {
        if (casingPrefab != null && casingEjectPoint != null)
        {
            Instantiate(casingPrefab, casingEjectPoint.position, casingEjectPoint.rotation); // Instantiate bullet casing
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
                audioSource.Play(); // Play the sound
            }
        }
    }

    private void UpdateWeaponUI()
    {
        WeaponInstance currentWeapon = inventory.CurrentWeapon;
        if (uiManager != null && currentWeapon != null)
        {
            Debug.Log($"Updating UI: Bullets in magazine: {currentWeapon.bulletsInMagazine}, Total ammo: {currentWeapon.totalAmmo}");
            uiManager.UpdateAmmo(currentWeapon.bulletsInMagazine, currentWeapon.totalAmmo, currentWeapon.ammoType);
            uiManager.UpdateWeaponUI(currentWeapon.weaponName, currentWeapon.sound, currentWeapon.ammoType);
        }
    }

    public void TriggerShootAnimation(bool isShooting)
    {
        animator.SetBool("isShoot", isShooting);
    }
}

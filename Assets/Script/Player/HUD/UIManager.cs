using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // For the health slider
using TMPro; // For TextMeshPro

public class UIManager : MonoBehaviour
{
    [Header("Health UI")]
    public Slider healthSlider; // Slider for displaying health

    [Header("Ammo UI")]
    public TMP_Text currentAmmoText; // Text for current ammo in the magazine
    public TMP_Text reservedAmmoText; // Text for reserved ammo

    [Header("Weapon UI")]
    public TMP_Text weaponNameText; // Text for displaying the weapon name
    public Slider soundSlider; // Slider for weapon sound level

    // Method to determine the weapon color based on the weapon type
    public Color GetWeaponColor(Weapon.AmmoType ammoType)
    {
        switch (ammoType)
        {
            case Weapon.AmmoType.Kinetic:
                return Color.yellow; // Yellow for kinetic weapons
            case Weapon.AmmoType.EMP:
                return new Color32(21, 193, 250, 255); // Blue hex color #15C1FA
            default:
                return Color.white; // Default color
        }
    }

    // Method to update the health slider
    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    public void UpdateMag(int currentAmmo){
        currentAmmoText.text = currentAmmo.ToString(); // Display the actual bullets left in the magazine
    }

    // Method to update the ammo text
    public void UpdateAmmo(int currentAmmo, int reservedAmmo, Weapon.AmmoType ammoType)
    {
        // Update the current ammo text (bullets left in the magazine)
        if (currentAmmoText != null)
        {
            currentAmmoText.text = currentAmmo.ToString(); // Display the actual bullets left in the magazine
            Debug.Log($"Updated Current Ammo: {currentAmmo}");
        }
        else
        {
            Debug.LogError("Current Ammo Text is null.");
        }

        // Update the reserved ammo text (total ammo outside the magazine)
        if (reservedAmmoText != null)
        {
            reservedAmmoText.text = reservedAmmo.ToString(); // Display the total reserved ammo
            Debug.Log($"Updated Reserved Ammo: {reservedAmmo}");
        }
        else
        {
            Debug.LogError("Reserved Ammo Text is null.");
        }
    }

    // Method to update the weapon name, sound, and ammo type
    public void UpdateWeaponUI(string weaponName, int soundLevel, Weapon.AmmoType ammoType, Color weaponColor)
    {
        if (weaponNameText != null)
        {
            weaponNameText.text = weaponName;

            // Apply the weapon color to the weapon name text
            weaponNameText.color = weaponColor;

            // Change weapon name outline color based on sound level
            Color outlineColor = soundLevel <= 2 ? Color.white : Color.black; // White for low sound, Black for high sound
            var outline = weaponNameText.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = outlineColor;
            }

            Debug.Log($"Updated Weapon Name: {weaponName}, Sound Level: {soundLevel}, Weapon Color: {weaponColor}");
        }

        if (soundSlider != null)
        {
            soundSlider.maxValue = 5;
            soundSlider.value = soundLevel;
        }
    }

    // Method to update the sound slider dynamically
    public void UpdateSoundSlider(float soundLevel)
    {
        if (soundSlider != null)
        {
            soundSlider.maxValue = 5.0f; // Set the max value to 10
        float roundedSoundLevel = Mathf.Round(soundLevel * 10f) / 10f;

        // Update the slider value only if the rounded value is valid
        soundSlider.value = Mathf.Clamp(roundedSoundLevel * 5.0f, 0.0f, 5.0f);
        }
        else
        {
            Debug.LogError("Sound slider is not assigned in the UIManager.");
        }
    }
}

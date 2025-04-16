using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal; // For Light2D
using Player.Stats; // Import the namespace for Visible

public class LightBehaviour : MonoBehaviour
{
    [Header("Light Settings")]
    public Light2D light2D; // Reference to the 2D light component
    public SpriteRenderer spriteRenderer; // Reference to the sprite renderer
    public Sprite brokenLightSprite; // Sprite to use when the light is destroyed
    public float flickerDuration = 10f; // Duration of the flicker effect for EMP bullets

    [Header("Sound Settings")]
    public AudioClip brokenSound; // Sound to play when the light is destroyed
    public AudioClip empSound; // Sound to play when the light flickers
    public AudioSource audioSource; // AudioSource to play the sounds

    private bool isFlickering = false; // To track if the light is currently flickering
    private bool isBroken = false; // Flag to track if the light is already broken
    private Visible visible; // Reference to the Visible script for managing light detection

    // Variables to store the initial light properties
    private float initialInnerRadius;
    private float initialOuterRadius;
    private float initialFalloffIntensity;
    private float initialInnerSpotAngle;
    private float initialOuterSpotAngle;

    void Start()
    {
        // Get the Visible script from the player
        visible = FindObjectOfType<Visible>();

        if (light2D == null)
        {
            light2D = GetComponent<Light2D>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Store the initial light properties
        if (light2D != null)
        {
            initialInnerRadius = light2D.pointLightInnerRadius;
            initialOuterRadius = light2D.pointLightOuterRadius;
            initialFalloffIntensity = light2D.falloffIntensity;
            initialInnerSpotAngle = light2D.pointLightInnerAngle;
            initialOuterSpotAngle = light2D.pointLightOuterAngle;
        }

        // Start the subtle flicker effect
        StartCoroutine(SubtleFlicker());
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the object that hit the light is a bullet
        Bullet bullet = collision.GetComponent<Bullet>();
        if (bullet != null)
        {
            if (bullet.ammoType == Weapon.AmmoType.Kinetic)
            {
                if (!isBroken) // Only play the sound and handle breaking once
                {
                    DestroyLight(); // Handle kinetic bullet behavior
                    isBroken = true; // Mark the light as broken
                }
            }
            else if (bullet.ammoType == Weapon.AmmoType.EMP && !isBroken && !isFlickering)
            {
                StartCoroutine(FlickerLight()); // Handle EMP bullet behavior
                InitialLightProperties(); // Restore initial light properties
            }
        }
    }

    private void DestroyLight()
    {
        if (light2D != null)
        {
            light2D.enabled = false;
        }

        if (spriteRenderer != null && brokenLightSprite != null)
        {
            spriteRenderer.sprite = brokenLightSprite;
        }
        // disable the collider to prevent further interactions
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        if (audioSource != null && brokenSound != null)
        {
            audioSource.PlayOneShot(brokenSound); // Play the sound only once
        }

        if (visible != null)
        {
            visible.ExcludeLight(light2D, isFunctional: true); // Use functional exclusion
        }
    }

    private IEnumerator FlickerLight()
    {
        if (isFlickering) yield break; // Prevent multiple flicker effects from stacking
        isFlickering = true;

        // Notify the Visible script to exclude this light
        if (visible != null && light2D != null)
        {
            visible.ExcludeLight(light2D, isFunctional: true); // Use functional exclusion
        }

        float elapsedTime = 0f;
        float flickerInterval = 0.2f; // Initial flicker interval

        // Play the EMP sound
        if (audioSource != null && empSound != null)
        {
            audioSource.clip = empSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        while (elapsedTime < flickerDuration)
        {
            elapsedTime += flickerInterval;

            if (light2D != null)
            {
                // Toggle the light on and off
                light2D.enabled = !light2D.enabled;

                if (light2D.enabled)
                {
                    // Randomize the intensity for a chaotic flicker effect
                    light2D.intensity = Random.Range(0.5f, 2.0f); // Flicker between dim and bright
                }
            }

            // Randomize the volume for each flicker
            if (audioSource != null)
            {
                audioSource.volume = Random.Range(0.3f, 1.0f); // Random volume between 30% and 100%
            }

            // Gradually decrease the flicker interval to make it flicker faster
            flickerInterval = Mathf.Lerp(0.2f, 0.05f, elapsedTime / flickerDuration);

            // Gradually increase the pitch to make the sound faster as the effect progresses
            if (audioSource != null)
            {
                audioSource.pitch = Mathf.Lerp(1.0f, 1.5f, elapsedTime / flickerDuration); // Pitch increases from 1.0 to 1.5
            }

            // Fade in and fade out effect
            if (audioSource != null)
            {
                float fadeFactor = Mathf.PingPong(elapsedTime * 2, 1); // Oscillates between 0 and 1
                audioSource.volume *= fadeFactor; // Apply fade effect
            }

            yield return new WaitForSeconds(flickerInterval);
        }

        // Ensure the light is turned back on after flickering
        if (light2D != null)
        {
            light2D.enabled = true;

            // Restore the light's original properties
            light2D.intensity = 10f; // Restore intensity to its original value
            light2D.pointLightInnerRadius = initialInnerRadius;
            light2D.pointLightOuterRadius = initialOuterRadius;
            light2D.falloffIntensity = initialFalloffIntensity;
            light2D.pointLightInnerAngle = initialInnerSpotAngle;
            light2D.pointLightOuterAngle = initialOuterSpotAngle;
        }

        // Stop the EMP sound and reset the loop property
        if (audioSource != null)
        {
            audioSource.loop = false;
            audioSource.Stop();
            audioSource.pitch = 1.0f; // Reset pitch to default
            audioSource.volume = 1.0f; // Reset volume to default
        }

        // Notify the Visible script to include this light again
        if (visible != null && light2D != null)
        {
            visible.IncludeLight(light2D, isFunctional: true); // Use functional inclusion
        }

        isFlickering = false;

        // Restart the subtle flicker effect
        StartCoroutine(SubtleFlicker());
    }

    private void InitialLightProperties()
    {
        if (light2D != null)
        {
            // Force the intensity to 10

            light2D.pointLightInnerRadius = initialInnerRadius;
            light2D.pointLightOuterRadius = initialOuterRadius;
            light2D.falloffIntensity = initialFalloffIntensity;
            light2D.pointLightInnerAngle = initialInnerSpotAngle;
            light2D.pointLightOuterAngle = initialOuterSpotAngle;
        }
    }

    private IEnumerator SubtleFlicker()
    {
        while (!isBroken && !isFlickering) // Only flicker if the light is not broken or flickering
        {
            if (light2D != null)
            {
                // Randomly adjust the intensity slightly for a subtle flicker effect
                light2D.intensity = Random.Range(9.1f, 10.3f); // Flicker between 90% and 110% of the original intensity

                // Randomly toggle the light off briefly for a very subtle effect
                if (Random.value > 0.95f) // 5% chance to briefly turn off
                {
                    light2D.enabled = false;
                    yield return new WaitForSeconds(0.005f); // Briefly turn off for 50ms
                    light2D.enabled = true;
                }
            }

            yield return new WaitForSeconds(Random.Range(0.1f, 0.3f)); // Wait for a random interval before the next flicker
        }
    }
}

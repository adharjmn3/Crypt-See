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
        // Cache references and initialize properties
        visible = FindObjectOfType<Visible>();
        light2D ??= GetComponent<Light2D>();
        spriteRenderer ??= GetComponent<SpriteRenderer>();
        audioSource ??= GetComponent<AudioSource>();

        if (light2D != null)
        {
            // Store the initial light properties
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
        if (collision.TryGetComponent(out Bullet bullet))
        {
            if (bullet.ammoType == Weapon.AmmoType.Kinetic && !isBroken)
            {
                DestroyLight(); // Handle kinetic bullet behavior
            }
            else if (bullet.ammoType == Weapon.AmmoType.EMP && !isBroken && !isFlickering)
            {
                StartCoroutine(FlickerLight()); // Handle EMP bullet behavior
            }
        }
    }

    private void DestroyLight()
    {
        if (isBroken) return; // Prevent multiple calls
        isBroken = true;

        if (light2D != null) light2D.enabled = false;

        if (spriteRenderer != null && brokenLightSprite != null)
        {
            spriteRenderer.sprite = brokenLightSprite;
        }

        // Disable the collider to prevent further interactions
        if (TryGetComponent(out Collider2D collider))
        {
            collider.enabled = false;
        }

        if (audioSource != null && brokenSound != null)
        {
            audioSource.PlayOneShot(brokenSound);
        }

        if (visible != null && light2D != null)
        {
            visible.ExcludeLight(light2D, isFunctional: true);
        }
    }

    private IEnumerator FlickerLight()
    {
        if (isFlickering) yield break; // Prevent multiple flicker effects
        isFlickering = true;

        // Notify the Visible script to exclude this light
        if (visible != null && light2D != null)
        {
            visible.ExcludeLight(light2D, isFunctional: true);
        }

        float elapsedTime = 0f;
        float flickerInterval = 0.2f;

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
                    light2D.intensity = Random.Range(0.5f, 2.0f); // Randomize intensity
                }
            }

            // Gradually decrease the flicker interval
            flickerInterval = Mathf.Lerp(0.2f, 0.05f, elapsedTime / flickerDuration);

            yield return new WaitForSeconds(flickerInterval);
        }

        // Restore the light's original properties
        RestoreLightProperties();

        // Stop the EMP sound
        if (audioSource != null)
        {
            audioSource.loop = false;
            audioSource.Stop();
        }

        // Notify the Visible script to include this light again
        if (visible != null && light2D != null)
        {
            visible.IncludeLight(light2D, isFunctional: true);
        }

        isFlickering = false;

        // Restart the subtle flicker effect
        StartCoroutine(SubtleFlicker());
    }

    private void RestoreLightProperties()
    {
        if (light2D != null)
        {
            light2D.enabled = true;
            light2D.intensity = 10f; // Restore intensity
            light2D.pointLightInnerRadius = initialInnerRadius;
            light2D.pointLightOuterRadius = initialOuterRadius;
            light2D.falloffIntensity = initialFalloffIntensity;
            light2D.pointLightInnerAngle = initialInnerSpotAngle;
            light2D.pointLightOuterAngle = initialOuterSpotAngle;
        }
    }

    private IEnumerator SubtleFlicker()
    {
        while (!isBroken && !isFlickering)
        {
            if (light2D != null)
            {
                // Randomly adjust the intensity slightly for a subtle flicker effect
                light2D.intensity = Random.Range(9.1f, 10.3f);

            }

            yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
        }
    }

    // Method to reset the light to its initial state
    public void ResetLight()
    {
        isBroken = false;
        isFlickering = false;

        if (light2D != null)
        {
            light2D.enabled = true;
            light2D.intensity = 10f; // Restore intensity
            light2D.pointLightInnerRadius = initialInnerRadius;
            light2D.pointLightOuterRadius = initialOuterRadius;
            light2D.falloffIntensity = initialFalloffIntensity;
            light2D.pointLightInnerAngle = initialInnerSpotAngle;
            light2D.pointLightOuterAngle = initialOuterSpotAngle;
        }

        if (spriteRenderer != null && brokenLightSprite != null)
        {
            spriteRenderer.sprite = null; // Restore the original sprite
        }

        if (TryGetComponent(out Collider2D collider))
        {
            collider.enabled = true; // Re-enable the collider
        }

        Debug.Log($"Light {gameObject.name} has been reset.");
    }

    // Method to disable the light
    public void DisableLight()
    {
        if (light2D != null)
        {
            light2D.enabled = false;
        }
        Debug.Log($"Light {gameObject.name} has been disabled.");
    }

    // Method to enable the light
    public void EnableLight()
    {
        if (light2D != null)
        {
            light2D.enabled = true;
        }
        Debug.Log($"Light {gameObject.name} has been enabled.");
    }
}

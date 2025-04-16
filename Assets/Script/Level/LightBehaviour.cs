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
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the object that hit the light is a bullet
        Bullet bullet = collision.GetComponent<Bullet>();
        if (bullet != null)
        {
            if (bullet.ammoType == Weapon.AmmoType.Kinetic)
            {
                DestroyLight(); // Handle kinetic bullet behavior
            }
            else if (bullet.ammoType == Weapon.AmmoType.EMP)
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

        if (audioSource != null && brokenSound != null)
        {
            audioSource.PlayOneShot(brokenSound);
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
            audioSource.PlayOneShot(empSound);
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

            // Gradually decrease the flicker interval to make it flicker faster
            flickerInterval = Mathf.Lerp(0.2f, 0.05f, elapsedTime / flickerDuration);

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

        // Notify the Visible script to include this light again
        if (visible != null && light2D != null)
        {
            visible.IncludeLight(light2D, isFunctional: true); // Use functional inclusion
        }

        isFlickering = false;
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
}

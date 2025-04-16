using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal; // For Light2D

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
    private PlayerManager playerManager; // Reference to the PlayerManager for light level adjustments

    // Variables to store the initial light properties
    private float initialIntensity;
    private float initialInnerRadius;
    private float initialOuterRadius;
    private float initialFalloffIntensity;
    private float initialInnerSpotAngle;
    private float initialOuterSpotAngle;

    void Start()
    {
        // Get the PlayerManager from the scene
        playerManager = FindObjectOfType<PlayerManager>();

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
            initialIntensity = light2D.intensity;
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
            }
        }
    }

    private void DestroyLight()
    {
        if (light2D != null)
        {
            light2D.enabled = false; // Disable the light
        }

        if (spriteRenderer != null && brokenLightSprite != null)
        {
            spriteRenderer.sprite = brokenLightSprite; // Change the sprite to the broken light sprite
        }

        // Play the broken sound
        if (audioSource != null && brokenSound != null)
        {
            audioSource.PlayOneShot(brokenSound);
        }

        // Reduce the light level in the PlayerManager
        if (playerManager != null)
        {
            playerManager.visibility.LightLevel -= 0.2f; // Adjust the light level (example value)
        }
    }

    private IEnumerator FlickerLight()
    {
        if (isFlickering) yield break; // Prevent multiple flicker effects from stacking
        isFlickering = true;

        float elapsedTime = 0f;
        float flickerInterval = 0.5f; // Initial flicker interval

        // Play the EMP sound
        if (audioSource != null && empSound != null)
        {
            audioSource.PlayOneShot(empSound);
        }

        while (elapsedTime < flickerDuration)
        {
            elapsedTime += flickerInterval;

            // Toggle the light on and off
            if (light2D != null)
            {
                light2D.enabled = !light2D.enabled;

                if (light2D.enabled)
                {
                    // Randomize light properties when the light is on
                    light2D.intensity = Random.Range(initialIntensity * 0.5f, initialIntensity); // Reduce intensity to half with randomness
                    light2D.pointLightInnerRadius = Random.Range(initialInnerRadius * 0.5f, initialInnerRadius); // Randomize inner radius
                    light2D.pointLightOuterRadius = Random.Range(initialOuterRadius * 0.5f, initialOuterRadius); // Randomize outer radius
                    light2D.falloffIntensity = 1f; // Set falloff strength to 1
                    light2D.pointLightInnerAngle = Random.Range(initialInnerSpotAngle * 0.5f, initialInnerSpotAngle); // Randomize inner spot angle
                    light2D.pointLightOuterAngle = Random.Range(initialOuterSpotAngle * 0.5f, initialOuterSpotAngle); // Randomize outer spot angle
                }
            }

            // Gradually decrease the flicker interval to make it flicker faster
            flickerInterval = Mathf.Lerp(0.5f, 0.05f, elapsedTime / flickerDuration);

            // Reduce the light level in the PlayerManager while flickering
            if (playerManager != null)
            {
                playerManager.visibility.LightLevel = Mathf.Clamp(playerManager.visibility.LightLevel - 0.5f, 0f, 1f);
            }

            yield return new WaitForSeconds(flickerInterval);
        }

        // Ensure the light is turned back on after flickering
        if (light2D != null)
        {
            light2D.enabled = true;
            light2D.intensity = initialIntensity; // Restore intensity
            light2D.pointLightInnerRadius = initialInnerRadius; // Restore inner radius
            light2D.pointLightOuterRadius = initialOuterRadius; // Restore outer radius
            light2D.falloffIntensity = initialFalloffIntensity; // Restore falloff strength
            light2D.pointLightInnerAngle = initialInnerSpotAngle; // Restore inner spot angle
            light2D.pointLightOuterAngle = initialOuterSpotAngle; // Restore outer spot angle
        }

        // Restore the light level in the PlayerManager
        if (playerManager != null)
        {
            playerManager.visibility.LightLevel = Mathf.Clamp(playerManager.visibility.LightLevel + 0.5f, 0f, 1f);
        }

        isFlickering = false;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal; // For Light2D
using TopDown.Movement; // Import the namespace for PlayerMovement

namespace Player.Stats
{
    public class Visible : MonoBehaviour
    {
        [SerializeField] public float lightLevel = 0.0f;
        [SerializeField] public float soundLevel = 0.0f; // Placeholder for sound level
        [SerializeField] private List<Light2D> excludedLights; // List of lights to exclude from detection
        [SerializeField, Range(0.1f, 2.0f)] private float lightSensitivity = 1.0f; // Slider for light sensitivity
        [SerializeField, Range(0.1f, 1.0f)] private float soundDecayRate = 0.2f; // Rate at which sound level decreases over time
        [SerializeField] private AudioClip lightOffSound; // Audio clip to play when lights are turned off
        [SerializeField] private AudioClip lightOnSound; // Audio clip to play when lights are turned on
        private AudioSource audioSource; // Reference to the AudioSource component

        private PlayerMovement playerMovement; // Reference to PlayerMovement

        public float LightLevel
        {
            get { return lightLevel; }
            set { lightLevel = value; }
        }

        public float SoundLevel
        {
            get { return soundLevel; }
            set { soundLevel = Mathf.Clamp(value, 0.0f, 5.0f); } // Clamp sound level between 0 and 5
        }

        private void Awake()
        {
            // Get the PlayerMovement component
            playerMovement = GetComponent<PlayerMovement>();

            // Add or get the AudioSource component
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void Update()
        {
            DetectLightSources();

            // Check if the player is pressing the move button
            if (playerMovement != null && playerMovement.CurrentSpeed > 0.0f && IsMoving())
            {
                // Dynamically scale sound level based on player movement
                float movementSound = playerMovement.CalculateVolume(playerMovement.CurrentSpeed);
                soundLevel = movementSound;
                soundLevel = Mathf.Clamp(soundLevel, 0.0f, 5.0f); // Ensure it doesn't exceed max
            }
            else
            {
                // Gradually decrease sound level over time when not moving
                if (soundLevel > 0.0f)
                {
                    soundLevel -= soundDecayRate * Time.deltaTime; // Decrease sound level
                    soundLevel = Mathf.Clamp(soundLevel, 0.0f, 5.0f); // Ensure it doesn't go below 0
                }
            }
        }

        // Helper method to check if the player is moving
        private bool IsMoving()
        {
            return Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;
        }

        // Method to add weapon sound to the current sound level
        public void AddWeaponSound(float weaponSound)
        {
            soundLevel += weaponSound;
            soundLevel = Mathf.Clamp(soundLevel, 0.0f, 5.0f); // Ensure it doesn't exceed max
        }

        private void DetectLightSources()
        {
            // Find all Light2D objects in the scene
            Light2D[] lights = FindObjectsOfType<Light2D>();
            float totalIntensity = 0.0f;

            foreach (Light2D light in lights)
            {
                // Skip excluded lights
                if (excludedLights.Contains(light)) continue;

                // Calculate the distance to the light source
                float distanceToLight = Vector2.Distance(transform.position, light.transform.position);

                // Check if the player is within the light's range
                if (distanceToLight <= light.pointLightOuterRadius)
                {
                    // Calculate the intensity based on distance and light properties
                    float distanceFactor = 1.0f - (distanceToLight / light.pointLightOuterRadius); // Linear falloff
                    float intensity = light.intensity * distanceFactor;

                    // Apply the light sensitivity multiplier
                    intensity *= lightSensitivity;

                    totalIntensity += intensity;
                }
            }

            // Update the light level (clamped between 0 and 1)
            LightLevel = Mathf.Clamp(totalIntensity, 0.0f, 1.0f);

            // Disable or enable excluded lights based on LightLevel
            if (LightLevel == 1.0f)
            {
                DisableExcludedLights();
                Debug.Log("All lights are on, disabling excluded lights.");
            }
            else
            {
                EnableExcludedLights();
                Debug.Log("Not all lights are on, enabling excluded lights.");
            }
        }

        private void DisableExcludedLights()
        {
            foreach (Light2D excludedLight in excludedLights)
            {
                if (excludedLight != null && excludedLight.enabled)
                {
                    excludedLight.enabled = false; // Disable the excluded light

                    // Play the light-off sound
                    if (lightOffSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(lightOffSound);
                    }
                }
            }
        }

        private void EnableExcludedLights()
        {
            foreach (Light2D excludedLight in excludedLights)
            {
                if (excludedLight != null && !excludedLight.enabled)
                {
                    excludedLight.enabled = true; // Enable the light
                    StartCoroutine(GraduallyTurnOnLight(excludedLight)); // Gradually increase intensity

                    // Play the light-on sound
                    if (lightOnSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(lightOnSound);
                    }
                }
            }
        }

        // Coroutine to gradually increase the intensity of a light
        private IEnumerator GraduallyTurnOnLight(Light2D light)
        {
            float targetIntensity = light.intensity; // Store the target intensity
            light.intensity = 0.0f; // Start from 0 intensity

            float duration = 1.0f; // Duration of the effect in seconds
            float elapsedTime = 0.0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                light.intensity = Mathf.Lerp(0.0f, targetIntensity, elapsedTime / duration); // Gradually increase intensity
                yield return null; // Wait for the next frame
            }

            light.intensity = targetIntensity; // Ensure the final intensity is set
        }
    }
}
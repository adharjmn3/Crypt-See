using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal; // For Light2D
using TopDown.Movement; // Import the namespace for PlayerMovement

namespace Player.Stats
{
    public class Visible : MonoBehaviour
    {
        [SerializeField] private float lightLevel = 0.0f;
        [SerializeField] private float soundLevel = 0.0f; // Placeholder for sound level
        [SerializeField] private List<Light2D> excludedLights; // List of lights to exclude from detection
        [SerializeField, Range(0.1f, 2.0f)] private float lightSensitivity = 1.0f; // Slider for light sensitivity
        [SerializeField, Range(0.1f, 1.0f)] private float soundDecayRate = 0.2f; // Rate at which sound level decreases over time

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
        }

        private void Update()
        {
            DetectLightSources();

            // Gradually decrease sound level over time
            if (soundLevel > 0.0f)
            {
                soundLevel -= soundDecayRate * Time.deltaTime; // Decrease sound level
                soundLevel = Mathf.Clamp(soundLevel, 0.0f, 5.0f); // Ensure it doesn't go below 0
            }

            // Dynamically scale sound level based on player movement
            if (playerMovement != null)
            {
                float movementSound = playerMovement.CalculateVolume(playerMovement.CurrentSpeed);
                soundLevel += movementSound;
                soundLevel = Mathf.Clamp(soundLevel, 0.0f, 5.0f); // Ensure it doesn't exceed max
            }
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
        }

        private void DisableExcludedLights()
        {
            foreach (Light2D excludedLight in excludedLights)
            {
                if (excludedLight != null && excludedLight.enabled)
                {
                    excludedLight.enabled = false; // Disable the excluded light
                }
            }
        }

        private void EnableExcludedLights()
        {
            foreach (Light2D excludedLight in excludedLights)
            {
                if (excludedLight != null && !excludedLight.enabled)
                {
                    excludedLight.enabled = true; // Re-enable the excluded light
                }
            }
        }
    }
}
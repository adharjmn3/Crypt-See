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
        [SerializeField] private List<Light2D> excludedLights; // Cosmetic excluded lights (for player visuals)
        [SerializeField] private List<Light2D> functionalExcludedLights; // Functional excluded lights (for LightBehaviour logic)
        [SerializeField, Range(0.1f, 2.0f)] private float lightSensitivity = 1.0f; // Slider for light sensitivity
        [SerializeField, Range(0.1f, 1.0f)] private float soundDecayRate = 0.2f; // Rate at which sound level decreases over time
        [SerializeField] private AudioClip lightOffSound; // Audio clip to play when lights are turned off
        [SerializeField] private AudioClip lightOnSound; // Audio clip to play when lights are turned on
        private AudioSource audioSource; // Reference to the AudioSource component

        private PlayerMovement playerMovement; // Reference to PlayerMovement

        private Dictionary<Light2D, float> lightCooldowns = new Dictionary<Light2D, float>(); // Cooldown tracker for lights
        [SerializeField] private float lightToggleCooldown = 0.5f; // Cooldown duration in seconds

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

            // Check if the player is moving or shooting
            if ((playerMovement != null && playerMovement.CurrentSpeed > 0.0f && IsMoving()) || IsShooting())
            {
                // Dynamically scale sound level based on player movement or shooting
                float movementSound = playerMovement.CalculateVolume(playerMovement.CurrentSpeed);
                soundLevel = Mathf.Max(soundLevel, movementSound); // Use the higher value
                soundLevel = Mathf.Clamp(soundLevel, 0.0f, 5.0f); // Ensure it doesn't exceed max
            }
            else
            {
                // Gradually decrease sound level over time when not moving or shooting
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

        // Helper method to check if the player is shooting
        private bool IsShooting()
        {
            Shoot shoot = GetComponent<Shoot>();
            return shoot != null && shoot.isShooting;
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
                // Skip lights in either excluded list
                if (excludedLights.Contains(light) || functionalExcludedLights.Contains(light)) continue;

                // Calculate the distance to the light source
                Vector2 lightPosition = light.transform.position;
                Vector2 playerPosition = transform.position;
                float distanceToLight = Vector2.Distance(playerPosition, lightPosition);

                // Check if the player is within the light's outer radius
                if (distanceToLight <= light.pointLightOuterRadius)
                {
                    // Calculate the angle between the player and the light's forward direction
                    Vector2 directionToPlayer = (playerPosition - lightPosition).normalized;
                    float angleToPlayer = Vector2.Angle(light.transform.up, directionToPlayer);

                    // Check if the player is within the light's outer angle
                    if (angleToPlayer <= light.pointLightOuterAngle / 2)
                    {
                        // Perform multiple raycasts to check for obstacles
                        int rayCount = 5; // Number of rays to cast
                        float angleStep = light.pointLightOuterAngle / (rayCount - 1);
                        bool hasObstacle = false;

                        for (int i = 0; i < rayCount; i++)
                        {
                            // Calculate the direction for each ray
                            float rayAngle = -light.pointLightOuterAngle / 2 + angleStep * i;
                            Vector2 rayDirection = Quaternion.Euler(0, 0, rayAngle) * directionToPlayer;

                            // Perform the raycast
                            RaycastHit2D hit = Physics2D.Raycast(playerPosition, rayDirection, distanceToLight);
                            if (hit.collider != null && hit.collider.gameObject != light.gameObject)
                            {
                                hasObstacle = true;
                                break; // Stop checking further rays if an obstacle is found
                            }
                        }

                        // Determine if the player is within the inner radius and angle
                        bool isInInnerRadius = distanceToLight <= light.pointLightInnerRadius;
                        bool isInInnerAngle = angleToPlayer <= light.pointLightInnerAngle / 2;

                        // Calculate the intensity based on distance and angle
                        float distanceFactor = isInInnerRadius
                            ? 1.0f
                            : 1.0f - ((distanceToLight - light.pointLightInnerRadius) /
                                      (light.pointLightOuterRadius - light.pointLightInnerRadius));

                        float angleFactor = isInInnerAngle
                            ? 1.0f
                            : 1.0f - ((angleToPlayer - light.pointLightInnerAngle / 2) /
                                      (light.pointLightOuterAngle / 2 - light.pointLightInnerAngle / 2));

                        float intensity = light.intensity * distanceFactor * angleFactor;

                        // Apply the light sensitivity multiplier
                        intensity *= lightSensitivity;

                        // If there is an obstacle, reduce the intensity
                        if (hasObstacle)
                        {
                            intensity *= 0.5f; // Reduce intensity by 50% if an obstacle is present
                        }

                        totalIntensity += intensity;
                    }
                }
            }

            // Update the light level (clamped between 0 and 1)
            LightLevel = Mathf.Clamp(totalIntensity, 0.0f, 1.0f);

            // Disable or enable excluded lights based on LightLevel
            if (LightLevel == 1.0f)
            {
                DisableExcludedLights();
            }
            else
            {
                EnableExcludedLights();
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
            if (!excludedLights.Contains(light)) yield break;

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

        // Exclude a light for cosmetic purposes
        public void ExcludeLight(Light2D light, bool isFunctional = false)
        {
            if (isFunctional)
            {
                if (!functionalExcludedLights.Contains(light))
                {
                    functionalExcludedLights.Add(light);
                }
            }
            else
            {
                // Check if the light is already excluded or on cooldown
                if (!excludedLights.Contains(light))
                {
                    if (lightCooldowns.ContainsKey(light) && Time.time < lightCooldowns[light])
                    {
                        return; // Skip if the light is still on cooldown
                    }

                    // Perform a raycast to ensure the light is actually blocked before excluding it
                    Vector2 lightPosition = light.transform.position;
                    Vector2 playerPosition = transform.position;
                    float distanceToLight = Vector2.Distance(playerPosition, lightPosition);
                    Vector2 directionToLight = (lightPosition - playerPosition).normalized;

                    RaycastHit2D hit = Physics2D.Raycast(playerPosition, directionToLight, distanceToLight);

                    // Only exclude the light if there is a collider blocking the light source
                    if (hit.collider != null && hit.collider.gameObject != light.gameObject)
                    {
                        excludedLights.Add(light);
                        lightCooldowns[light] = Time.time + lightToggleCooldown; // Set cooldown for this light
                    }
                }
            }
        }

        // Include a light back for cosmetic purposes
        public void IncludeLight(Light2D light, bool isFunctional = false)
        {
            if (isFunctional)
            {
                if (functionalExcludedLights.Contains(light))
                {
                    functionalExcludedLights.Remove(light);
                }
            }
            else
            {
                if (excludedLights.Contains(light))
                {
                    excludedLights.Remove(light);
                }
            }
        }
    }
}
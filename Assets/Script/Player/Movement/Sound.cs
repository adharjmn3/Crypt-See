using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDown.Movement
{
    [RequireComponent(typeof(AudioSource))]
    public class Sound : MonoBehaviour
    {
        [Header("Sound Effects")]
        [SerializeField] private AudioClip[] walkingSFX; // Walking sound effects
        [SerializeField] private AudioClip[] runningSFX; // Running sound effects
        [SerializeField] private AudioClip[] jumpSFX; // Jumping sound effects
        
        [Header("Sound Settings")]
        [SerializeField] private float walkingIntensity = 0.3f; // How loud walking is to enemies
        [SerializeField] private float runningIntensity = 0.7f; // How loud running is to enemies
        [SerializeField] private float jumpIntensity = 0.5f; // How loud jumping is to enemies
        [SerializeField] private float soundCooldown = 0.1f; // Minimum time between sound broadcasts
        
        private AudioSource audioSource;
        private PlayerMovement playerMovement;
        private float lastSoundTime;
        private bool wasMovingLastFrame;
        
        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            playerMovement = GetComponent<PlayerMovement>();
        }

        private void Update()
        {
            HandleMovementSounds();
        }
        
        private void HandleMovementSounds()
        {
            bool isMoving = playerMovement.CurrentInput.magnitude > 0;
            float currentSpeed = playerMovement.CurrentSpeed;
            
            // Check if we should play/broadcast a sound
            if (isMoving && Time.time - lastSoundTime > soundCooldown)
            {
                // Determine if walking or running based on speed
                bool isRunning = currentSpeed > (playerMovement.MinSpeed + playerMovement.MaxSpeed) * 0.6f;
                
                // Play audio
                if (!audioSource.isPlaying)
                {
                    PlayMovementSound(isRunning);
                }
                
                // Broadcast sound to enemies
                BroadcastMovementSound(isRunning, currentSpeed);
                
                lastSoundTime = Time.time;
            }
            
            // Stop audio if not moving
            if (!isMoving && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            
            wasMovingLastFrame = isMoving;
        }

        private void PlayMovementSound(bool isRunning)
        {
            AudioClip[] soundArray = isRunning ? runningSFX : walkingSFX;
            
            if (soundArray.Length == 0) return;

            // Select a random sound effect
            AudioClip randomClip = soundArray[Random.Range(0, soundArray.Length)];
            audioSource.clip = randomClip;

            // Set the volume based on the player's speed
            float volume = playerMovement.CalculateVolume(playerMovement.CurrentSpeed);
            audioSource.volume = volume;

            // Play the sound
            audioSource.Play();
        }
        
        private void BroadcastMovementSound(bool isRunning, float speed)
        {
            // Calculate sound intensity based on movement type and speed
            float baseIntensity = isRunning ? runningIntensity : walkingIntensity;
            
            // Normalize speed and apply to intensity
            float normalizedSpeed = (speed - playerMovement.MinSpeed) / (playerMovement.MaxSpeed - playerMovement.MinSpeed);
            float finalIntensity = baseIntensity * (0.5f + normalizedSpeed * 0.5f); // Scale between 50-100% of base intensity
            
            // Determine sound type
            SoundType soundType = isRunning ? SoundType.Running : SoundType.Footstep;
            
            // Broadcast to enemies via SoundManager
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.BroadcastSound(transform.position, finalIntensity, soundType, gameObject);
            }
        }
        
        /// <summary>
        /// Public method for other scripts to trigger specific sounds
        /// </summary>
        public void PlayJumpSound()
        {
            if (jumpSFX.Length > 0)
            {
                AudioClip randomJumpClip = jumpSFX[Random.Range(0, jumpSFX.Length)];
                audioSource.PlayOneShot(randomJumpClip);
                
                // Broadcast jump sound to enemies
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.BroadcastSound(transform.position, jumpIntensity, SoundType.Generic, gameObject);
                }
            }
        }
        
        /// <summary>
        /// Public method for weapon sounds
        /// </summary>
        public void PlayWeaponSound(SoundType weaponSoundType, float intensity = 1f)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.BroadcastSound(transform.position, intensity, weaponSoundType, gameObject);
            }
        }
        
        /// <summary>
        /// Public method for interaction sounds
        /// </summary>
        public void PlayInteractionSound(SoundType interactionType, float intensity = 0.4f)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.BroadcastSound(transform.position, intensity, interactionType, gameObject);
            }
        }
    }
}

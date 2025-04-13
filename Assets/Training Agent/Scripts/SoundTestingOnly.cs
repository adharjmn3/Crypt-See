using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDown.Movement
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundTestingOnly : MonoBehaviour
    {
        [SerializeField] private AudioClip[] movementSFX; // Array of movement sound effects
        private AudioSource audioSource;
        private PlayerMovement playerMovement;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            playerMovement = GetComponent<PlayerMovement>();
        }

        private void Update()
        {
            if (!audioSource.isPlaying)
            {
                PlayRandomMovementSound();
            }
        }

        private void PlayRandomMovementSound()
        {
            if (movementSFX.Length == 0) return;

            // Select a random sound effect
            AudioClip randomClip = movementSFX[Random.Range(0, movementSFX.Length)];
            audioSource.clip = randomClip;

            // Set the volume based on the player's speed
            float volume = playerMovement.CalculateVolume(playerMovement.CurrentSpeed);
            audioSource.volume = volume;

            // Play the sound
            audioSource.Play();
        }
    }
}

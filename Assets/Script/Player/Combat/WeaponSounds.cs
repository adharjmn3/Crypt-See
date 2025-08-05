using UnityEngine;

namespace TopDown.Combat
{
    /// <summary>
    /// Add this script to weapons or shooting systems to integrate with the sound system
    /// </summary>
    public class WeaponSounds : MonoBehaviour
    {
        [Header("Weapon Sound Settings")]
        [SerializeField] private float gunshotIntensity = 1.0f;
        [SerializeField] private float reloadIntensity = 0.4f;
        [SerializeField] private float weaponSwitchIntensity = 0.3f;
        [SerializeField] private float impactIntensity = 0.6f;
        
        [Header("Audio Clips")]
        [SerializeField] private AudioClip[] gunshotClips;
        [SerializeField] private AudioClip[] reloadClips;
        [SerializeField] private AudioClip[] weaponSwitchClips;
        [SerializeField] private AudioClip[] impactClips;
        
        private AudioSource audioSource;
        
        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        /// <summary>
        /// Call this when the weapon fires
        /// </summary>
        public void PlayGunshotSound()
        {
            PlaySoundAndBroadcast(gunshotClips, gunshotIntensity, SoundType.Gunshot);
        }
        
        /// <summary>
        /// Call this when reloading
        /// </summary>
        public void PlayReloadSound()
        {
            PlaySoundAndBroadcast(reloadClips, reloadIntensity, SoundType.Reload);
        }
        
        /// <summary>
        /// Call this when switching weapons
        /// </summary>
        public void PlayWeaponSwitchSound()
        {
            PlaySoundAndBroadcast(weaponSwitchClips, weaponSwitchIntensity, SoundType.WeaponSwitch);
        }
        
        /// <summary>
        /// Call this when bullets hit surfaces
        /// </summary>
        public void PlayImpactSound()
        {
            PlaySoundAndBroadcast(impactClips, impactIntensity, SoundType.Impact);
        }
        
        private void PlaySoundAndBroadcast(AudioClip[] clips, float intensity, SoundType soundType)
        {
            // Play audio locally
            if (clips.Length > 0 && audioSource != null)
            {
                AudioClip randomClip = clips[Random.Range(0, clips.Length)];
                audioSource.PlayOneShot(randomClip);
            }
            
            // Broadcast to enemies
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.BroadcastSound(transform.position, intensity, soundType, gameObject);
            }
        }
        
        /// <summary>
        /// Custom sound with specific intensity
        /// </summary>
        public void PlayCustomSound(SoundType soundType, float customIntensity)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.BroadcastSound(transform.position, customIntensity, soundType, gameObject);
            }
        }
    }
}

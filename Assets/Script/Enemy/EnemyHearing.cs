using UnityEngine;

public class EnemyHearing : MonoBehaviour
{
    [Header("Hearing Settings")]
    [SerializeField] private float hearingRange = 20f;
    [SerializeField] private float soundDetectionThreshold = 0.5f;
    [SerializeField] private float memoryDuration = 5f;
    
    [Header("Sound Categories")]
    [SerializeField] private float footstepSensitivity = 1f;
    [SerializeField] private float gunshotSensitivity = 2f;
    [SerializeField] private float voiceSensitivity = 1.5f;
    
    private Transform playerTransform;
    private float lastSoundTime = -1f;
    private Vector3 lastSoundPosition;
    private float lastSoundIntensity;
    
    // Public properties
    public bool CanHearPlayer { get; private set; }
    public Vector3 LastHeardPosition { get; private set; }
    public float TimeSinceLastSound => Time.time - lastSoundTime;
    public bool HasRecentSoundMemory => TimeSinceLastSound < memoryDuration;
    
    private void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
    }
    
    private void Update()
    {
        UpdateHearingStatus();
    }
    
    private void UpdateHearingStatus()
    {
        // Update hearing status based on memory duration
        CanHearPlayer = HasRecentSoundMemory;
        
        if (CanHearPlayer)
        {
            LastHeardPosition = lastSoundPosition;
        }
    }
    
    /// <summary>
    /// Called when a sound is detected. This would be called by a sound management system.
    /// </summary>
    /// <param name="soundPosition">World position where the sound occurred</param>
    /// <param name="soundIntensity">How loud the sound is (0-1)</param>
    /// <param name="soundType">Type of sound for different sensitivities</param>
    public void OnSoundDetected(Vector3 soundPosition, float soundIntensity, SoundType soundType = SoundType.Generic)
    {
        float distanceToSound = Vector3.Distance(transform.position, soundPosition);
        
        // Check if sound is within hearing range
        if (distanceToSound > hearingRange) return;
        
        // Apply distance falloff
        float distanceFactor = 1f - (distanceToSound / hearingRange);
        
        // Apply sound type sensitivity
        float sensitivity = GetSensitivityForSoundType(soundType);
        float effectiveIntensity = soundIntensity * distanceFactor * sensitivity;
        
        // Check if sound is loud enough to detect
        if (effectiveIntensity >= soundDetectionThreshold)
        {
            lastSoundTime = Time.time;
            lastSoundPosition = soundPosition;
            lastSoundIntensity = effectiveIntensity;
            CanHearPlayer = true;
        }
    }
    
    /// <summary>
    /// Simulates hearing player footsteps based on movement
    /// Updated for improved compilation compatibility
    /// </summary>
    public void CheckForPlayerMovement()
    {
        if (playerTransform == null) return;
        
        // Simple movement detection - you might want to improve this
        // by checking actual player movement speed or other factors
        Vector3 playerVelocity = GetPlayerVelocity();
        float movementIntensity = playerVelocity.magnitude * 0.1f; // Scale factor
        
        if (movementIntensity > 0.1f) // Only if player is moving significantly
        {
            OnSoundDetected(playerTransform.position, movementIntensity, SoundType.Footstep);
        }
    }
    
    private Vector3 lastPlayerPosition = Vector3.zero;
    private bool hasLastPosition = false;
    
    private Vector3 GetPlayerVelocity()
    {
        // This is a simplified version - you'd want to get actual velocity from player controller
        // For now, we'll estimate based on position change
        
        if (!hasLastPosition)
        {
            lastPlayerPosition = playerTransform.position;
            hasLastPosition = true;
            return Vector3.zero;
        }
        
        Vector3 velocity = (playerTransform.position - lastPlayerPosition) / Time.deltaTime;
        lastPlayerPosition = playerTransform.position;
        return velocity;
    }
    
    private float GetSensitivityForSoundType(SoundType soundType)
    {
        switch (soundType)
        {
            case SoundType.Footstep: return footstepSensitivity;
            case SoundType.Gunshot: return gunshotSensitivity;
            case SoundType.Voice: return voiceSensitivity;
            default: return 1f;
        }
    }
    
    /// <summary>
    /// Sets the target transform for the hearing system to track
    /// </summary>
    /// <param name="target">The transform to track (usually the player)</param>
    public void SetTarget(Transform target)
    {
        playerTransform = target;
    }
    
    public float GetDistanceToLastSound()
    {
        if (!HasRecentSoundMemory) return float.MaxValue;
        return Vector3.Distance(transform.position, lastSoundPosition);
    }
}

public enum SoundType
{
    Generic,
    Footstep,
    Gunshot,
    Voice,
    ObjectInteraction
}

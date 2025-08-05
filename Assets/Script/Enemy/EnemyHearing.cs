using UnityEngine;

public class EnemyHearing : MonoBehaviour
{
    [Header("Hearing Settings")]
    [SerializeField] private float hearingRange = 20f;
    [SerializeField] private float soundDetectionThreshold = 0.3f;
    [SerializeField] private float memoryDuration = 5f;
    
    [Header("Sound Categories")]
    [SerializeField] private float footstepSensitivity = 1f;
    [SerializeField] private float runningSensitivity = 1.5f;
    [SerializeField] private float gunshotSensitivity = 3f;
    [SerializeField] private float voiceSensitivity = 1.5f;
    [SerializeField] private float interactionSensitivity = 0.8f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugRays = true;
    
    private Transform playerTransform;
    private float lastSoundTime = -1f;
    private Vector3 lastSoundPosition;
    private float lastSoundIntensity;
    private SoundType lastSoundType;
    private GameObject lastSoundSource;
    
    // Public properties
    public bool CanHearPlayer { get; private set; }
    public Vector3 LastHeardPosition { get; private set; }
    public float TimeSinceLastSound => Time.time - lastSoundTime;
    public bool HasRecentSoundMemory => TimeSinceLastSound < memoryDuration;
    public SoundType LastHeardSoundType => lastSoundType;
    public GameObject LastSoundSource => lastSoundSource;
    
    private void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
    }
    
    private void Start()
    {
        // Register with SoundManager
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.RegisterEnemyHearer(this);
        }
    }
    
    private void OnDestroy()
    {
        // Unregister when destroyed
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.UnregisterEnemyHearer(this);
        }
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
    /// Called when a sound is detected. This is called by the SoundManager.
    /// </summary>
    /// <param name="soundPosition">World position where the sound occurred</param>
    /// <param name="soundIntensity">How loud the sound is (0-1)</param>
    /// <param name="soundType">Type of sound for different sensitivities</param>
    /// <param name="source">GameObject that made the sound</param>
    public void OnSoundDetected(Vector3 soundPosition, float soundIntensity, SoundType soundType, GameObject source = null)
    {
        float distanceToSound = Vector3.Distance(transform.position, soundPosition);
        
        // Check if sound is within hearing range
        if (distanceToSound > hearingRange) return;
        
        // Apply distance falloff (inverse square law with minimum falloff)
        float distanceFactor = Mathf.Max(0.1f, 1f - (distanceToSound / hearingRange));
        distanceFactor = distanceFactor * distanceFactor; // Square falloff for more realistic sound
        
        // Apply sound type sensitivity
        float sensitivity = GetSensitivityForSoundType(soundType);
        float effectiveIntensity = soundIntensity * distanceFactor * sensitivity;
        
        // Check if sound is loud enough to detect
        if (effectiveIntensity >= soundDetectionThreshold)
        {
            lastSoundTime = Time.time;
            lastSoundPosition = soundPosition;
            lastSoundIntensity = effectiveIntensity;
            lastSoundType = soundType;
            lastSoundSource = source;
            CanHearPlayer = true;
            
            // Debug visualization
            if (showDebugRays)
            {
                Debug.DrawLine(transform.position, soundPosition, Color.yellow, 2f);
                Debug.Log($"{gameObject.name} heard {soundType} with intensity {effectiveIntensity:F2} from {distanceToSound:F1}m away");
            }
        }
    }
    
    /// <summary>
    /// Simulates hearing player footsteps based on movement - Updated for improved compatibility
    /// </summary>
    public void CheckForPlayerMovement()
    {
        if (playerTransform == null) return;
        
        // This method is now primarily used as a fallback
        // The main sound detection happens through the SoundManager system
        
        // Simple movement detection for backward compatibility
        Vector3 playerVelocity = GetPlayerVelocity();
        float movementIntensity = playerVelocity.magnitude * 0.05f; // Reduced scale factor
        
        if (movementIntensity > 0.1f) // Only if player is moving significantly
        {
            // Use the new sound system instead of direct detection
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.BroadcastSound(playerTransform.position, movementIntensity, SoundType.Footstep, playerTransform.gameObject);
            }
            else
            {
                // Fallback to old system if SoundManager not available
                OnSoundDetected(playerTransform.position, movementIntensity, SoundType.Footstep);
            }
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
            case SoundType.Running: return runningSensitivity;
            case SoundType.Gunshot: return gunshotSensitivity;
            case SoundType.Voice: return voiceSensitivity;
            case SoundType.ObjectInteraction:
            case SoundType.DoorOpen:
            case SoundType.DoorClose:
            case SoundType.Impact:
            case SoundType.Reload:
            case SoundType.WeaponSwitch:
                return interactionSensitivity;
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
    
    /// <summary>
    /// Get information about the last heard sound for AI decision making
    /// </summary>
    public SoundInfo GetLastSoundInfo()
    {
        return new SoundInfo
        {
            position = lastSoundPosition,
            intensity = lastSoundIntensity,
            soundType = lastSoundType,
            timeHeard = lastSoundTime,
            source = lastSoundSource
        };
    }
}

/// <summary>
/// Data structure for sound information
/// </summary>
[System.Serializable]
public struct SoundInfo
{
    public Vector3 position;
    public float intensity;
    public SoundType soundType;
    public float timeHeard;
    public GameObject source;
}

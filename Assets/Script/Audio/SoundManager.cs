using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    
    [Header("Debug Settings")]
    [SerializeField] private bool showSoundDebug = true;
    [SerializeField] private float debugSoundDuration = 1f;
    
    // List of all enemies that can hear sounds
    private List<EnemyHearing> allEnemyHearers = new List<EnemyHearing>();
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Register an enemy hearing component with the sound manager
    /// </summary>
    public void RegisterEnemyHearer(EnemyHearing enemyHearer)
    {
        if (!allEnemyHearers.Contains(enemyHearer))
        {
            allEnemyHearers.Add(enemyHearer);
        }
    }
    
    /// <summary>
    /// Unregister an enemy hearing component
    /// </summary>
    public void UnregisterEnemyHearer(EnemyHearing enemyHearer)
    {
        allEnemyHearers.Remove(enemyHearer);
    }
    
    /// <summary>
    /// Broadcast a sound to all enemies that can hear
    /// </summary>
    /// <param name="soundPosition">World position where the sound occurred</param>
    /// <param name="intensity">How loud the sound is (0-1)</param>
    /// <param name="soundType">Type of sound for different enemy reactions</param>
    /// <param name="source">What caused the sound (optional)</param>
    public void BroadcastSound(Vector3 soundPosition, float intensity, SoundType soundType, GameObject source = null)
    {
        // Debug visualization
        if (showSoundDebug)
        {
            Debug.DrawLine(soundPosition, soundPosition + Vector3.up * (intensity * 5f), GetSoundTypeColor(soundType), debugSoundDuration);
        }
        
        // Notify all enemy hearers
        foreach (var enemyHearer in allEnemyHearers)
        {
            if (enemyHearer != null)
            {
                // Use reflection to safely call OnSoundDetected
                try
                {
                    var method = enemyHearer.GetType().GetMethod("OnSoundDetected");
                    if (method != null)
                    {
                        method.Invoke(enemyHearer, new object[] { soundPosition, intensity, soundType, source });
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to call OnSoundDetected on {enemyHearer.name}: {e.Message}");
                }
            }
        }
        
        // Log for debugging
        if (showSoundDebug)
        {
            Debug.Log($"Sound broadcasted: {soundType} at {soundPosition} with intensity {intensity:F2}");
        }
    }
    
    /// <summary>
    /// Get color for debug visualization based on sound type
    /// </summary>
    private Color GetSoundTypeColor(SoundType soundType)
    {
        switch (soundType)
        {
            case SoundType.Footstep: return Color.green;
            case SoundType.Running: return Color.cyan;
            case SoundType.Gunshot: return Color.red;
            case SoundType.Voice: return Color.blue;
            case SoundType.ObjectInteraction: return Color.yellow;
            case SoundType.Impact: return new Color(1f, 0.5f, 0f); // Orange color
            case SoundType.Reload: return Color.magenta;
            case SoundType.WeaponSwitch: return new Color(0.5f, 0f, 1f); // Purple
            default: return Color.white;
        }
    }
    
    /// <summary>
    /// Clean up null references
    /// </summary>
    private void Update()
    {
        // Remove null references (destroyed enemies)
        allEnemyHearers.RemoveAll(hearer => hearer == null);
    }
}

/// <summary>
/// Enhanced sound types for better enemy AI reactions
/// </summary>
public enum SoundType
{
    Generic,
    Footstep,
    Running,
    Gunshot,
    Voice,
    ObjectInteraction,
    DoorOpen,
    DoorClose,
    Impact,
    Reload,
    WeaponSwitch
}

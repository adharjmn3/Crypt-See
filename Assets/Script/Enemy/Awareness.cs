using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class Awareness : MonoBehaviour
{
    [Header("Awareness Settings")]
    public EnemyNPC enemyNPC; // Reference to the EnemyNPC script
    public Color unawareColor = Color.green; // Color when unaware (detection level 0-20%)
    public Color suspiciousColor = Color.yellow; // Color when investigating (detection level 20-60%)
    public Color alertColor = new Color(1f, 0.5f, 0f); // Orange when detected (detection level 60-80%)
    public Color combatColor = Color.red; // Color when in combat (detection level 80-100%)
    
    [Header("Visual Effects")]
    public bool enableFlashing = true; // Enable flashing in combat mode
    public float flashSpeed = 3f; // How fast the light flashes in combat
    public AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1f); // Intensity based on detection
    
    private Light2D light2D; // Reference to the 2D light component
    private ImprovedEnemyAI improvedAI; // Reference to the improved AI system
    private float originalIntensity;
    private bool useImprovedAI = false;

    void Start()
    {
        // Get the Light2D component
        light2D = GetComponent<Light2D>();
        if (light2D == null)
        {
            Debug.LogError("Light2D component not found! Please attach a Light2D component to this GameObject.");
        }
        else
        {
            originalIntensity = light2D.intensity;
        }

        // Try to get ImprovedEnemyAI first (preferred)
        improvedAI = GetComponent<ImprovedEnemyAI>();
        if (improvedAI != null)
        {
            useImprovedAI = true;
            Debug.Log($"{gameObject.name}: Using ImprovedEnemyAI detection system");
        }
        else if (enemyNPC == null)
        {
            // Fallback to finding EnemyNPC
            enemyNPC = GetComponent<EnemyNPC>();
            if (enemyNPC == null)
            {
                Debug.LogError("Neither ImprovedEnemyAI nor EnemyNPC reference found! Please assign one of these scripts.");
            }
        }
    }

    void Update()
    {
        if (light2D == null) return;

        if (useImprovedAI && improvedAI != null)
        {
            UpdateWithImprovedAI();
        }
        else if (enemyNPC != null)
        {
            UpdateWithEnemyNPC();
        }
    }

    private void UpdateWithImprovedAI()
    {
        float detectionPercentage = improvedAI.GetDetectionPercentage();
        ImprovedEnemyAI.DetectionState state = improvedAI.GetDetectionState();
        
        // Determine color based on detection state
        Color targetColor = GetColorForDetectionState(state, detectionPercentage);
        
        // Handle flashing in combat
        if (enableFlashing && state == ImprovedEnemyAI.DetectionState.Combat)
        {
            float flash = Mathf.Sin(Time.time * flashSpeed) * 0.5f + 0.5f;
            light2D.color = Color.Lerp(alertColor, combatColor, flash);
        }
        else
        {
            light2D.color = targetColor;
        }
        
        // Update intensity based on detection level
        float targetIntensity = originalIntensity * intensityCurve.Evaluate(detectionPercentage);
        light2D.intensity = targetIntensity;
    }

    private void UpdateWithEnemyNPC()
    {
        // Original behavior for backward compatibility
        float awarenessLevel = enemyNPC.tensionMeter / enemyNPC.maxTensionMeter;
        light2D.color = Color.Lerp(unawareColor, combatColor, awarenessLevel);
    }

    private Color GetColorForDetectionState(ImprovedEnemyAI.DetectionState state, float detectionPercentage)
    {
        switch (state)
        {
            case ImprovedEnemyAI.DetectionState.Unaware:
                return unawareColor;
                
            case ImprovedEnemyAI.DetectionState.Investigating:
                // Lerp between unaware and suspicious based on detection level
                float investigateProgress = Mathf.InverseLerp(0f, 0.3f, detectionPercentage);
                return Color.Lerp(unawareColor, suspiciousColor, investigateProgress);
                
            case ImprovedEnemyAI.DetectionState.Detected:
                // Lerp between suspicious and alert based on detection level
                float detectedProgress = Mathf.InverseLerp(0.3f, 0.8f, detectionPercentage);
                return Color.Lerp(suspiciousColor, alertColor, detectedProgress);
                
            case ImprovedEnemyAI.DetectionState.Combat:
                return combatColor;
                
            default:
                return unawareColor;
        }
    }

    /// <summary>
    /// Force set the awareness color (useful for external systems)
    /// </summary>
    public void SetAwarenessColor(Color color)
    {
        if (light2D != null)
        {
            light2D.color = color;
        }
    }

    /// <summary>
    /// Get current awareness level (0-1)
    /// </summary>
    public float GetAwarenessLevel()
    {
        if (useImprovedAI && improvedAI != null)
        {
            return improvedAI.GetDetectionPercentage();
        }
        else if (enemyNPC != null)
        {
            return enemyNPC.tensionMeter / enemyNPC.maxTensionMeter;
        }
        return 0f;
    }

    /// <summary>
    /// Check if enemy is in combat state
    /// </summary>
    public bool IsInCombat()
    {
        if (useImprovedAI && improvedAI != null)
        {
            return improvedAI.IsInCombat();
        }
        else if (enemyNPC != null)
        {
            return enemyNPC.tensionMeter >= enemyNPC.maxTensionMeter * 0.8f;
        }
        return false;
    }
}

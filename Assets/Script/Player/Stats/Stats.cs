using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Player.Stats {

public class Stats : MonoBehaviour
{
    // Detection percentage, the higher the value, the more likely the player will be detected
    private float detectionValue = 0.0f;

    // The maximum value for the detection percentage
    [SerializeField] private float maxDetectionPercentage = 100.0f;

    [SerializeField] private int kills = 0;

    // Timers for shadow and light
    [SerializeField] private float shadowTime = 0.0f;
    [SerializeField] private float lightTime = 0.0f;

    // Visibility status and level
    [SerializeField] private string visibilityStatus = "hide"; // Default status
    [SerializeField] private float visibilityLevel = 0.0f; // How visible the player is (0 to 1)

    public float GetDetectionValue()
    {
        return detectionValue;
    }

    public void SetDetectionValue(float value)
    {
        detectionValue = Mathf.Clamp(value, 0.0f, maxDetectionPercentage);
    }

    public void AddKill()
    {
        kills++;
        Debug.Log("Kill added. Total kills: " + kills);
    }

    public int GetKills()
    {
        return kills;
    }

    // Methods to update shadow and light timers
    public void UpdateShadowTime(float deltaTime)
    {
        shadowTime += deltaTime;
    }

    public void UpdateLightTime(float deltaTime)
    {
        lightTime += deltaTime;
    }

    public float GetShadowTime()
    {
        return shadowTime;
    }

    public float GetLightTime()
    {
        return lightTime;
    }

    // Methods to handle visibility status and level
    public void SetVisibilityStatus(string status)
    {
        visibilityStatus = status;
    }

    public string GetVisibilityStatus()
    {
        return visibilityStatus;
    }

    public void SetVisibilityLevel(float level)
    {
        visibilityLevel = Mathf.Clamp01(level); // Clamp between 0 and 1
    }

    public float GetVisibilityLevel()
    {
        return visibilityLevel;
    }
}

}

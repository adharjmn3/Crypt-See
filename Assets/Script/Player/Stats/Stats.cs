using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Player.Stats {

public class Stats : MonoBehaviour
{
    // Detection percentage, the higher the value, the more likely the player will be detected
    private float detectionValue = 0.0f;

    // The maximum value for the detection percentage
    private float maxDetectionPercentage = 100.0f;

    private int kills = 0;

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

    
}


    

}

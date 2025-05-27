using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStatistic : MonoBehaviour
{
    // Timer variables
    private float detectionTimer = 0f;
    private bool isTimerRunning = true;
    private bool hasLoggedTime = false;

    // Reference to the EnemyNPC script
    private EnemyNPC enemyNPC;

    // Start is called before the first frame update
    void Start()
    {
        // Initialize timer
        detectionTimer = 0f;
        isTimerRunning = true;
        
        // Get reference to the EnemyNPC component
        enemyNPC = GetComponent<EnemyNPC>();
        if (enemyNPC == null)
        {
            Debug.LogError("EnemyNPC component not found on the same GameObject as EnemyStatistic!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Update timer if it's still running and EnemyNPC reference exists
        if (isTimerRunning && enemyNPC != null)
        {
            detectionTimer += Time.deltaTime;
            
            // Check if target is in sight using the EnemyNPC's variable
            if (enemyNPC.isTargetInSight)
            {
                // Stop the timer
                isTimerRunning = false;
                
                // Log the detection time
                if (!hasLoggedTime)
                {
                    string formattedTime = FormatTime(detectionTimer);
                    Debug.Log($"<color=yellow>Enemy Detection Statistic</color>: Player found after {formattedTime}");
                    hasLoggedTime = true;
                }
            }
        }
    }
    
    // Format time to make it more readable (minutes:seconds.milliseconds)
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int milliseconds = Mathf.FloorToInt((timeInSeconds * 100f) % 100f);
        
        return string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
    }
    
    // Method to manually reset the timer
    public void ResetTimer()
    {
        detectionTimer = 0f;
        isTimerRunning = true;
        hasLoggedTime = false;
    }
}

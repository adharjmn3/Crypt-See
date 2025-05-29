using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStatistic : MonoBehaviour
{
    [Header("Statistics Settings")]
    [SerializeField] private bool enableLogging = true;
    [SerializeField] private bool logToScreen = false;
    [SerializeField] private Color logColor = Color.yellow;
    
    // Timer variables
    private float detectionTimer = 0f;
    private bool isTimerRunning = true;
    private bool hasLoggedTime = false;

    // Enemy controller references
    private EnemyNPC enemyNPC;
    private EnemyNavM enemyNavM;
    private EnemyAIFSM enemyAIFSM;
    
    // Cached UI elements for on-screen display
    private GUIStyle guiStyle;
    private string currentStatDisplay = "";

    // Start is called before the first frame update
    void Start()
    {
        // Initialize timer
        detectionTimer = 0f;
        isTimerRunning = true;
        hasLoggedTime = false;
        
        // Get references to all possible enemy controller types
        enemyNPC = GetComponent<EnemyNPC>();
        enemyNavM = GetComponent<EnemyNavM>();
        enemyAIFSM = GetComponent<EnemyAIFSM>();
        
        // Verify we have at least one valid controller
        if (enemyNPC == null && enemyNavM == null && enemyAIFSM == null)
        {
            Debug.LogWarning("EnemyStatistic: No compatible enemy controller found on " + gameObject.name);
        }
        
        // Initialize GUI style for on-screen display
        if (logToScreen)
        {
            guiStyle = new GUIStyle();
            guiStyle.fontSize = 16;
            guiStyle.fontStyle = FontStyle.Bold;
            guiStyle.normal.textColor = logColor;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Only update if timer is running
        if (isTimerRunning)
        {
            detectionTimer += Time.deltaTime;
            
            // Check if player is detected by any of the controller types
            bool isPlayerDetected = IsPlayerDetectedByAnyController();
            
            if (isPlayerDetected)
            {
                // Stop the timer
                isTimerRunning = false;
                
                // Log the detection time
                if (!hasLoggedTime && enableLogging)
                {
                    string formattedTime = FormatTime(detectionTimer);
                    string enemyType = DetermineEnemyType();
                    string logMessage = $"<color=#{ColorUtility.ToHtmlStringRGB(logColor)}>Enemy Detection Statistic</color>: {enemyType} found player after {formattedTime}";
                    
                    Debug.Log(logMessage);
                    currentStatDisplay = $"Player found after {formattedTime}";
                    hasLoggedTime = true;
                    
                    // Notify other game systems about detection
                    NotifyDetection(detectionTimer);
                }
            }
        }
    }
    
    // Detects if any controller has spotted the player
    private bool IsPlayerDetectedByAnyController()
    {
        if (enemyNPC != null && enemyNPC.isTargetInSight)
            return true;
            
        if (enemyNavM != null && enemyNavM.isTargetInSight)
            return true;
            
        if (enemyAIFSM != null && enemyAIFSM.currentState == EnemyState.Combat)
            return true;
            
        return false;
    }
    
    // Determines the type of enemy for more detailed logging
    private string DetermineEnemyType()
    {
        if (enemyNPC != null)
            return "ML-Agent";
        if (enemyNavM != null)
            return "NavMesh Agent";
        if (enemyAIFSM != null)
            return "FSM Agent";
        
        return "Enemy";
    }
    
    // Notifies other game systems about the detection event
    private void NotifyDetection(float detectionTime)
    {
        // You can add events or calls to game manager here
        // For example:
        // GameManager.Instance.OnPlayerDetected(detectionTime);
        
        // Send message to any listeners
        SendMessage("OnEnemyDetectedPlayer", detectionTime, SendMessageOptions.DontRequireReceiver);
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
        currentStatDisplay = "";
    }
    
    // Display stats on screen if enabled
    void OnGUI()
    {
        if (logToScreen && !string.IsNullOrEmpty(currentStatDisplay))
        {
            GUI.Label(new Rect(10, 10, 300, 30), currentStatDisplay, guiStyle);
        }
    }
    
    // Public getter for the detection time
    public float GetDetectionTime()
    {
        return detectionTimer;
    }
    
    // Public getter for formatted detection time
    public string GetFormattedDetectionTime()
    {
        return FormatTime(detectionTimer);
    }
}

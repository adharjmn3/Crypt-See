using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // For the slider UI

public class DetectMeter : MonoBehaviour
{
    [Header("Detection UI")]
    public Slider detectionSlider; // Reference to the detection slider (world-space canvas)
    public float detectionFillSpeed = 1.0f; // Speed at which the detection meter fills
    public float detectionDecaySpeed = 0.5f; // Speed at which the detection meter decays

    private float currentDetectionLevel = 0.0f; // Current detection level (0 to 1)
    private bool isBeingDetected = false; // Whether the player is currently being detected
    private Transform detectingEnemy; // Reference to the enemy detecting the player

    void Start()
    {
        if (detectionSlider != null)
        {
            detectionSlider.value = 0.0f; // Initialize the slider to 0
        }
    }

    void Update()
    {
        // If the player is being detected, fill the detection meter
        if (isBeingDetected)
        {
            // Uncomment this when AI detection is implemented
            // currentDetectionLevel += detectionFillSpeed * Time.deltaTime;

            // Rotate the detection slider (and arrow) to face the enemy
            if (detectingEnemy != null && detectionSlider != null)
            {
                Vector3 directionToEnemy = detectingEnemy.position - transform.position;
                float angle = Mathf.Atan2(directionToEnemy.y, directionToEnemy.x) * Mathf.Rad2Deg;
                detectionSlider.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
        else
        {
            // Decay the detection meter when not being detected
            currentDetectionLevel -= detectionDecaySpeed * Time.deltaTime;
        }

        // Clamp the detection level between 0 and 1
        currentDetectionLevel = Mathf.Clamp01(currentDetectionLevel);

        // Update the slider UI
        if (detectionSlider != null)
        {
            detectionSlider.value = currentDetectionLevel;
        }

        // Uncomment this when AI detection is implemented
        // if (currentDetectionLevel >= 1.0f)
        // {
        //     Debug.Log("Player fully detected! Trigger enemy alert.");
        //     // Add logic to handle full detection (e.g., alert enemies)
        // }
    }

    // Call this method to simulate detection from an enemy
    public void SetDetectionState(bool detected, Transform enemy = null)
    {
        isBeingDetected = detected;

        if (detected)
        {
            detectingEnemy = enemy; // Set the enemy that is detecting the player
        }
        else
        {
            detectingEnemy = null; // Clear the enemy reference when not being detected
        }
    }
}

// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Rendering.Universal;

// [RequireComponent(typeof(Light2D))]
// public class Awareness : MonoBehaviour
// {
//     [Header("Awareness Settings")]
//     public EnemyNPC enemyNPC; // Reference to the EnemyNPC script
//     public Color lowAwarenessColor = Color.green; // Color when tension is low
//     public Color highAwarenessColor = Color.red; // Color when tension is high
//     private Light2D light2D; // Reference to the 2D light component

//     void Start()
//     {
//         // Get the Light2D component
//         light2D = GetComponent<Light2D>();
//         if (light2D == null)
//         {
//             Debug.LogError("Light2D component not found! Please attach a Light2D component to this GameObject.");
//         }

//         if (enemyNPC == null)
//         {
//             Debug.LogError("EnemyNPC reference is missing! Please assign the EnemyNPC script.");
//         }
//     }

//     void Update()
//     {
//         if (enemyNPC != null && light2D != null)
//         {
//             // Calculate the awareness level as a percentage
//             float awarenessLevel = enemyNPC.tensionMeter / enemyNPC.maxTensionMeter;

//             // Lerp the light color between lowAwarenessColor and highAwarenessColor
//             light2D.color = Color.Lerp(lowAwarenessColor, highAwarenessColor, awarenessLevel);
//         }
//     }
// }

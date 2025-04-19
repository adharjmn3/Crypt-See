using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public List<GameObject> tutorialGameObjects; // List of tutorial GameObjects already placed in the scene
    public UIManager uiManager; // Reference to the UIManager

    private List<TutorialData> activeTutorials = new List<TutorialData>(); // Active tutorials

    private void Start()
    {
        InitializeTutorials();
    }

    private void InitializeTutorials()
    {
        foreach (GameObject tutorialGameObject in tutorialGameObjects)
        {
            // Ensure the TutorialBehavior script is attached
            TutorialBehavior behavior = tutorialGameObject.GetComponent<TutorialBehavior>();
            if (behavior != null)
            {
                activeTutorials.Add(behavior.tutorialData); // Add the tutorial data to the active list
                behavior.Initialize(uiManager); // Initialize the tutorial with the UIManager
            }
            else
            {
                Debug.LogError($"TutorialBehavior script is missing on {tutorialGameObject.name}!");
            }
        }
    }
}
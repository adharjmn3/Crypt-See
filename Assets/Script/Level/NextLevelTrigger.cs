using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextLevelTrigger : MonoBehaviour
{
    private BoxCollider2D boxCollider; // Reference to the BoxCollider2D component
    private void Start(){
        // Get the BoxCollider2D component attached to this GameObject
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            Debug.LogError("NextLevelTrigger: BoxCollider2D component not found!");
        }
        else
        {
            boxCollider.isTrigger = true; // Ensure the collider is set as a trigger
        }
    }
// When the player enters the trigger, load the next level
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("NextLevelTrigger: OnTriggerEnter2D called");
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player has reached the next level trigger.");
            LoadNextLevel();
        }
    }

    // Method to load the next level
    private void LoadNextLevel()
    {
        Debug.Log("Loading next level...");
        // Assuming you have a method in your GameManager to handle level loading
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            Debug.Log($"Loading scene at index {nextSceneIndex}");
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.Log("No more levels to load.");
        }
        
    }
}

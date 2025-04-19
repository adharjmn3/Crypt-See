using UnityEngine;

public class TutorialBehavior : MonoBehaviour
{
    public TutorialData tutorialData; // Reference to the TutorialData ScriptableObject
    private UIManager uiManager;

    public void Initialize(UIManager manager)
    {
        uiManager = manager;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Show the tutorial dialog
            if (uiManager != null && tutorialData != null)
            {
                uiManager.UpdateDialog(
                    tutorialData.dialogSpeakerName,
                    tutorialData.dialogContent,
                    true,
                    tutorialData.typingSpeed
                );
            }

            // Destroy the tutorial GameObject after showing the dialog
            Destroy(gameObject);
        }
    }
}
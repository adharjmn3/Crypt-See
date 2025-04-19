using UnityEngine;

public class TriggerDialog : MonoBehaviour
{
    public DialogData dialogData; // Reference to the DialogData ScriptableObject
    private UIManager uiManager;

    private void Start()
    {
        // Find the UIManager in the scene
        uiManager = FindObjectOfType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogError("UIManager not found in the scene!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && uiManager != null && dialogData != null)
        {
            // Show the dialog
            uiManager.UpdateDialog(
                dialogData.dialogSpeakerName,
                dialogData.dialogContent,
                true,
                dialogData.typingSpeed
            );

            // Destroy the trigger after showing the dialog
            Destroy(gameObject);
        }
    }
}
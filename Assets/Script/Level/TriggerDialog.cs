using UnityEngine;
using System.Collections;
using TopDown.CameraController;

public class TriggerDialog : MonoBehaviour
{
    public DialogData dialogData; // Reference to the DialogData ScriptableObject
    private UIManager uiManager;
    private CameraController cameraController;

    private void Start()
    {
        // Find the UIManager in the scene
        uiManager = FindObjectOfType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogError("UIManager not found in the scene!");
        }

        // Find the CameraController in the scene
        cameraController = FindObjectOfType<CameraController>();
        if (cameraController == null)
        {
            Debug.LogError("CameraController not found in the scene!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && uiManager != null && dialogData != null)
        {
            // If the dialog requires a camera change
            if (dialogData.changeCameraFocus && cameraController != null)
            {
                StartCoroutine(HandleCameraFocus());
            }
            else
            {
                // Show the dialog without changing the camera
                ShowDialog();
            }
        }
    }

    private IEnumerator HandleCameraFocus()
    {
        // Move the camera to the manually specified position
        cameraController.SetTargetPosition(dialogData.cameraTargetPosition);

        // Show the dialog
        ShowDialog();

        // Wait until the dialog is finished
        while (uiManager.IsDialogActive())
        {
            yield return null;
        }

        // Return the camera to the player
        cameraController.ResetToPlayer();
    }

    private void ShowDialog()
    {
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
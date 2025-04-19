using UnityEngine;

[CreateAssetMenu(fileName = "NewDialog", menuName = "Game/Dialog")]
public class DialogData : ScriptableObject
{
    public string dialogName; // Name of the dialog
    public string dialogSpeakerName; // Speaker name for the dialog
    [TextArea] public string dialogContent; // Dialog content
    public float typingSpeed = 0.05f; // Typing speed for the dialog

    [Header("Camera Settings")]
    public bool changeCameraFocus = false; // Whether to change the camera focus
    public Vector3 cameraTargetPosition; // Manual input for the camera position
}
using UnityEngine;

[CreateAssetMenu(fileName = "NewTutorial", menuName = "Game/Tutorial")]
public class TutorialData : ScriptableObject
{
    public string tutorialName; // Name of the tutorial
    public string dialogSpeakerName; // Speaker name for the tutorial
    [TextArea] public string dialogContent; // Tutorial dialog content
    public float typingSpeed = 0.05f; // Typing speed for the dialog
}
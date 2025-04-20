using UnityEngine;

[CreateAssetMenu(fileName = "NewObjective", menuName = "Game/Objective")]
public class ObjectiveData : ScriptableObject
{
    public enum ObjectiveType { Collect, Finish }

    public string objectiveName; // Name of the objective
    public ObjectiveType type; // Type of the objective
    public GameObject objectivePrefab; // Prefab for the in-game objective
    public string dialogSpeakerName; // Speaker name for dialog
    [TextArea] public string dialogContent; // Dialog content
    public float typingSpeed = 0.05f; // Typing speed for dialog

    [Header("Objective Settings")]
    public bool isMandatory = true; // Whether this objective is mandatory to finish the level

    [Header("Reward Settings")]
    public bool hasReward; // Whether this objective gives a reward
    public int ammoReward; // Ammo reward (if applicable)
    public int healthReward; // Health reward (if applicable)
}
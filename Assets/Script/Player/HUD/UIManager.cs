using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // For the health slider
using TMPro; // For TextMeshPro

public class UIManager : MonoBehaviour
{
    [Header("Health UI")]
    public Slider healthSlider; // Slider for displaying health

    [Header("Ammo UI")]
    public TMP_Text currentAmmoText; // Text for current ammo in the magazine
    public TMP_Text reservedAmmoText; // Text for reserved ammo

    [Header("Weapon UI")]
    public TMP_Text weaponNameText; // Text for displaying the weapon name
    public Slider soundSlider; // Slider for weapon sound level

    [Header("Dialog UI")]
    public TMP_Text dialogNameText; // Text for the name of the dialog speaker
    public TMP_Text dialogText; // Text for the dialog content
    public GameObject dialogBackground; // Background for the dialog

    [Header("End Story UI")]
    public GameObject endStoryPanel; // Panel for the end story UI
    public Button restartButton; // Button to restart the level
    public Button exitButton; // Button to exit the game

    [Header("Minimap UI")]
    public GameObject minimap; // Reference to the minimap GameObject

    [Header("Objective Counter UI")]
    public TMP_Text objectivesLeftText; // Text for objectives left
    public TMP_Text objectivesLeftCopyText; // Copy of the objectives left text

    private Coroutine typingCoroutine; // To manage the typing effect coroutine

    private void Start()
    {
        // Ensure the dialog is disabled at the start of the game
        if (dialogBackground != null)
        {
            dialogBackground.SetActive(false);
            dialogNameText.text = ""; // Clear the dialog name text
            dialogText.text = ""; // Clear the dialog text
        }
        UpdateWeaponUI("", 0, Weapon.AmmoType.Kinetic, Color.white); // Initialize weapon UI with default values
        
    }

    // Method to determine the weapon color based on the weapon type
    public Color GetWeaponColor(Weapon.AmmoType ammoType)
    {
        switch (ammoType)
        {
            case Weapon.AmmoType.Kinetic:
                return Color.yellow; // Yellow for kinetic weapons
            case Weapon.AmmoType.EMP:
                return new Color32(21, 193, 250, 255); // Blue hex color #15C1FA
            default:
                return Color.white; // Default color
        }
    }

    // Method to update the health slider
    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;

            // Debug logs to verify the values
            Debug.Log($"Health UI Updated: Current Health = {currentHealth}, Max Health = {maxHealth}");
        }
        else
        {
            Debug.LogWarning("Health Slider is not assigned in the UIManager.");
        }
    }

    public void UpdateMag(int currentAmmo)
    {
        if (currentAmmoText != null)
        {
            currentAmmoText.text = currentAmmo.ToString(); // Display the actual bullets left in the magazine
        }
    }

    // Method to update the ammo text
    public void UpdateAmmo(int currentAmmo, int reservedAmmo, Weapon.AmmoType ammoType)
    {
        if (currentAmmoText != null)
        {
            currentAmmoText.text = currentAmmo.ToString(); // Display the actual bullets left in the magazine
        }

        if (reservedAmmoText != null)
        {
            reservedAmmoText.text = reservedAmmo.ToString(); // Display the total reserved ammo
        }
    }

    // Method to update the weapon name, sound, and ammo type
    public void UpdateWeaponUI(string weaponName, int soundLevel, Weapon.AmmoType ammoType, Color weaponColor)
    {
        if (weaponNameText != null)
        {
            weaponNameText.text = weaponName;
            weaponNameText.color = weaponColor;

            Color outlineColor = soundLevel <= 2 ? Color.white : Color.black;
            var outline = weaponNameText.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = outlineColor;
            }
        }

        if (soundSlider != null)
        {
            soundSlider.maxValue = 5;
            soundSlider.value = soundLevel;
        }
    }
        public bool IsDialogActive()
    {
        return dialogBackground != null && dialogBackground.activeSelf;
    }

    // Method to update the sound slider dynamically
    public void UpdateSoundSlider(float soundLevel)
    {
        if (soundSlider != null)
        {
            soundSlider.maxValue = 5.0f;
            float roundedSoundLevel = Mathf.Round(soundLevel * 10f) / 10f;
            soundSlider.value = Mathf.Clamp(roundedSoundLevel * 5.0f, 0.0f, 5.0f);
        }
    }

    // Method to update the dialog UI with typing effect
    public void UpdateDialog(string speakerName, string dialogContent, bool showDialog, float typingSpeed = 0.05f)
    {
        if (dialogBackground != null)
        {
            dialogBackground.SetActive(showDialog); // Show or hide the dialog background
        }

        if (dialogNameText != null)
        {
            dialogNameText.text = speakerName; // Update the speaker's name
        }

        if (dialogText != null)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine); // Stop any ongoing typing effect
            }

            if (showDialog)
            {
                typingCoroutine = StartCoroutine(TypeDialog(dialogContent, typingSpeed));
            }
        }
    }

    // Coroutine for the typing effect
    private IEnumerator TypeDialog(string dialogContent, float typingSpeed)
    {
        dialogText.text = ""; // Clear the dialog text
        foreach (char letter in dialogContent.ToCharArray())
        {
            dialogText.text += letter; // Add one letter at a time
            yield return new WaitForSeconds(typingSpeed); // Wait for the typing speed
        }

        // Automatically hide the dialog after a duration based on the content length
        float dialogDuration = Mathf.Clamp(dialogContent.Length * typingSpeed + 2.0f, 3.0f, 10.0f); // Minimum 3s, max 10s
        yield return new WaitForSeconds(dialogDuration);

        if (dialogBackground != null)
        {
            dialogBackground.SetActive(false); // Hide the dialog background
            // clear the dialog text after the duration
            dialogText.text = ""; // Clear the dialog text
            dialogNameText.text = ""; // Clear the dialog name text

        }
    }

    // Method to show the End Story UI
    public void ShowEndStoryUI(bool show)
    {
        if (endStoryPanel != null)
        {
            Debug.Log($"End Story UI visibility set to: {show}");
            endStoryPanel.SetActive(show);

            // Disable the minimap when showing the end screen
            ToggleMinimap(!show);
        }
        else
        {
            Debug.LogError("End Story Panel is not assigned in the UIManager.");
        }
    }

    // Method to set up button actions
    public void SetupEndStoryButtons(System.Action onRestart, System.Action onExit)
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners(); // Good practice
            if (onRestart != null)
            {
                restartButton.onClick.AddListener(() => {
                    Debug.Log("UIManager: Restart button listener triggered. Invoking onRestart action.");
                    onRestart.Invoke();
                });
                Debug.Log("UIManager: Restart button listener configured.");
            }
            else
            {
                Debug.LogWarning("UIManager: onRestart action provided to SetupEndStoryButtons is null. Restart button will do nothing.");
            }
        }
        else
        {
            Debug.LogError("UIManager: restartButton is not assigned in the Inspector.");
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners(); // Good practice
            if (onExit != null)
            {
                exitButton.onClick.AddListener(() => {
                    Debug.Log("UIManager: Exit button listener triggered. Invoking onExit action.");
                    onExit.Invoke();
                });
                Debug.Log("UIManager: Exit button listener configured.");
            }
            else
            {
                Debug.LogWarning("UIManager: onExit action provided to SetupEndStoryButtons is null. Exit button will do nothing.");
            }
        }
        else
        {
            Debug.LogError("UIManager: exitButton is not assigned in the Inspector.");
        }
    }

    public void RestartGame()
    {
        Debug.Log("UIManager: RestartGame() method called. Attempting to reload scene.");
        // If you pause the game by setting Time.timeScale to 0, reset it here.
        // Time.timeScale = 1f; 

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"UIManager: Reloading scene: {sceneName}");
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    public void ExitGame()
    {
        Debug.Log("Exiting the game...");
        Application.Quit();
    }

    // Method to enable or disable the minimap
    public void ToggleMinimap(bool enable)
    {
        if (minimap != null)
        {
            minimap.SetActive(enable);
            Debug.Log($"Minimap visibility set to: {enable}");
        }
        else
        {
            Debug.LogError("Minimap is not assigned in the UIManager.");
        }
    }

    // Method to update the objective counter with a delay
    public void UpdateObjectiveCounter(int objectivesLeft)
    {
        StartCoroutine(UpdateObjectiveCounterWithDelay(objectivesLeft));
    }

    // Coroutine to handle the delay
    private IEnumerator UpdateObjectiveCounterWithDelay(int objectivesLeft)
    {
        yield return new WaitForSeconds(1f); // Add a 1-second delay

        if (objectivesLeftText != null)
        {
            objectivesLeftText.text = Mathf.Max(0, objectivesLeft).ToString(); // Ensure no negative values
            objectivesLeftCopyText.text = Mathf.Max(0, objectivesLeft).ToString();
        }
    }
}

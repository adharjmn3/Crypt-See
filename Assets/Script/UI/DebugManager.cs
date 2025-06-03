using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown aiTypeDropdown;
    [SerializeField] private TMP_InputField enemyCountInputField;
    [SerializeField] private TMP_InputField fixedCountPerTypeInputField;
    [SerializeField] private Button applyButton;
    [SerializeField] private Toggle useFixedCountToggle;
    
    [Header("References")]
    [SerializeField] private EnemyManager enemyManager;
    
    private void Start()
    {
        // Find EnemyManager if not assigned
        if (enemyManager == null)
        {
            enemyManager = FindObjectOfType<EnemyManager>();
            if (enemyManager == null)
            {
                Debug.LogError("DebugManager: No EnemyManager found in scene. Debug controls will not work.");
                enabled = false;
                return;
            }
        }
        
        // Initialize dropdown options
        InitializeDropdown();
        
        // Set default values
        SetDefaultValues();
        
        // Add listeners to UI components
        AddUIListeners();
    }
    
    private void InitializeDropdown()
    {
        if (aiTypeDropdown != null)
        {
            aiTypeDropdown.ClearOptions();
            
            // Add all enemy AI types from enum
            List<string> options = new List<string>();
            foreach (EnemyAIType type in System.Enum.GetValues(typeof(EnemyAIType)))
            {
                options.Add(type.ToString());
            }
            
            aiTypeDropdown.AddOptions(options);
        }
        else
        {
            Debug.LogWarning("DebugManager: AI Type Dropdown is not assigned.");
        }
    }
    
    private void SetDefaultValues()
    {
        if (enemyCountInputField != null)
        {
            enemyCountInputField.text = enemyManager.maxEnemies.ToString();
        }
        
        if (fixedCountPerTypeInputField != null)
        {
            fixedCountPerTypeInputField.text = "0";
            fixedCountPerTypeInputField.interactable = false;
        }
        
        if (useFixedCountToggle != null)
        {
            useFixedCountToggle.isOn = false;
        }
    }
    
    private void AddUIListeners()
    {
        // Add listener to apply button
        if (applyButton != null)
        {
            applyButton.onClick.AddListener(ApplySettings);
        }
        else
        {
            Debug.LogWarning("DebugManager: Apply Button is not assigned.");
        }
        
        // Add listener to fixed count toggle
        if (useFixedCountToggle != null)
        {
            useFixedCountToggle.onValueChanged.AddListener(OnFixedCountToggleChanged);
        }
    }
    
    private void OnFixedCountToggleChanged(bool isOn)
    {
        if (fixedCountPerTypeInputField != null)
        {
            fixedCountPerTypeInputField.interactable = isOn;
        }
    }
    
    public void ApplySettings()
    {
        if (enemyManager == null) return;
        
        // Parse max enemies count
        int maxEnemies = enemyManager.maxEnemies;
        if (enemyCountInputField != null && !string.IsNullOrEmpty(enemyCountInputField.text))
        {
            if (int.TryParse(enemyCountInputField.text, out int count) && count > 0)
            {
                maxEnemies = count;
            }
            else
            {
                Debug.LogWarning("DebugManager: Invalid max enemies count. Using current value: " + maxEnemies);
                enemyCountInputField.text = maxEnemies.ToString();
            }
        }
        
        // Parse fixed count per type
        int fixedCountPerType = 0;
        if (useFixedCountToggle != null && useFixedCountToggle.isOn && 
            fixedCountPerTypeInputField != null && !string.IsNullOrEmpty(fixedCountPerTypeInputField.text))
        {
            if (int.TryParse(fixedCountPerTypeInputField.text, out int count) && count >= 0)
            {
                fixedCountPerType = count;
            }
            else
            {
                Debug.LogWarning("DebugManager: Invalid fixed count per type. Using 0.");
                fixedCountPerTypeInputField.text = "0";
            }
        }
        
        // Parse AI type
        EnemyAIType selectedType = EnemyAIType.FiniteStateMachine;
        if (aiTypeDropdown != null)
        {
            selectedType = (EnemyAIType)aiTypeDropdown.value;
        }
        
        // Despawn current enemies and spawn new ones with the new settings
        StartCoroutine(RespawnEnemies(selectedType, maxEnemies, fixedCountPerType));
    }
    
    private IEnumerator RespawnEnemies(EnemyAIType aiType, int maxEnemies, int fixedCountPerType)
    {
        Debug.Log($"DebugManager: Respawning enemies with AI Type: {aiType}, Max Count: {maxEnemies}, Fixed Count Per Type: {fixedCountPerType}");
        
        // Disable the apply button while processing
        if (applyButton != null)
        {
            applyButton.interactable = false;
        }
        
        // Despawn all existing enemies
        DespawnAllEnemies();
        
        // Wait a frame to ensure all enemies are properly despawned
        yield return null;
        
        // Update enemy manager settings
        UpdateEnemyManagerSettings(aiType, maxEnemies, fixedCountPerType);
        
        // Trigger enemy respawning
        enemyManager.RespawnEnemies();
        
        // Re-enable the apply button
        if (applyButton != null)
        {
            applyButton.interactable = true;
        }
    }
    
    private void DespawnAllEnemies()
    {
        // Get a copy of the spawned enemies
        List<GameObject> enemiesToDespawn = new List<GameObject>(enemyManager.GetSpawnedEnemies());
        
        // Despawn each enemy
        foreach (GameObject enemy in enemiesToDespawn)
        {
            if (enemy != null)
            {
                enemyManager.DespawnEnemy(enemy);
            }
        }
        
        Debug.Log($"DebugManager: Despawned {enemiesToDespawn.Count} enemies.");
    }
    
    private void UpdateEnemyManagerSettings(EnemyAIType aiType, int maxEnemies, int fixedCountPerType)
    {
        // Update enemy manager settings
        enemyManager.SetEnemySpawnParameters(aiType, maxEnemies, fixedCountPerType);
    }
}

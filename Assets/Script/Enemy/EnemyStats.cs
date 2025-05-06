using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [Header("Core Stats")]
    public float health = 100f; // Current health
    public float maxHealth = 100f; // Maximum health
    public float armor = 0f; // Reduces incoming damage
    public float speed = 3f; // Movement speed

    [Header("Combat Stats")]
    public float attackPower = 10f; // Damage dealt to the player
    public float attackRange = 2f; // Range within which the enemy can attack
    public float attackCooldown = 1.5f; // Time between attacks
    public float accuracy = 0.9f; // Accuracy of attacks (1.0 = perfect accuracy)
    public float rateOfFire = 1f; // Number of attacks per second

    [Header("Awareness Stats")]
    public float detectionRange = 10f; // How far the enemy can detect the player
    public float fieldOfView = 60f; // Vision cone angle
    public float hearingSensitivity = 5f; // Sensitivity to sound

    [Header("Behavioral States")]
    public bool isAlert = false; // Whether the enemy is in an alert state
    public bool isCallingForHelp = false; // Whether the enemy is calling for help
    public Vector3 lastKnownPlayerPosition; // Last position where the player was seen

    [Header("Special Abilities")]
    public float specialAbilityCooldown = 10f; // Cooldown for special abilities
    public int specialAbilityCharges = 1; // Number of times the ability can be used

    [Header("Miscellaneous")]
    public float stamina = 100f; // Energy for actions like running or attacking
    public float fearLevel = 0f; // Fear level (optional mechanic)

    // Start is called before the first frame update
    void Start()
    {
        // Initialize stats if needed
        health = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        // Example: Regenerate stamina over time
        if (stamina < 100f)
        {
            stamina += Time.deltaTime * 5f; // Regenerate 5 stamina per second
        }
    }

    // Method to take damage
    public void TakeDamage(float damage)
    {
        float effectiveDamage = Mathf.Max(0, damage - armor); // Reduce damage by armor
        health -= effectiveDamage;

        if (health <= 0)
        {
            Die();
        }
    }

    // Method to handle death
    private void Die()
    {
        Debug.Log($"{gameObject.name} has died.");
        // Add death logic here (e.g., notify EnemyManager, play animation, etc.)
        Destroy(gameObject);
    }

    // Method to call for help
    public void CallForHelp()
    {
        if (!isCallingForHelp)
        {
            isCallingForHelp = true;
            Debug.Log($"{gameObject.name} is calling for help!");
            // Add logic to notify nearby enemies
        }
    }
}

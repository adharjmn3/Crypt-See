using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class EnemyNPC : Agent
{
    [Header("Agent Settings")]
    [SerializeField] public float tensionMeter;
    [SerializeField] public float maxTensionMeter;
    [SerializeField] private float fillSpeed = 0.5f;
    [SerializeField] private float drainSpeed = 0.2f;
    private EnemyStats enemyStats;

    private float lastTensionMeter = 0f;

    private EnemyVision enemyVision;
    private EnemyHearing enemyHearing;
    private EnemyMovement enemyMovement;

    private Transform playerTransform;

    private bool isGettingShot = false;

    bool isTargetInSight = false;
    bool isSoundDetected = false;

    Vector3 agentPos;
    Vector3 targetPos;

    private float fallbackTimer = 0f;

    public override void Initialize()
    {
        enemyMovement = GetComponent<EnemyMovement>();
        enemyHearing = GetComponent<EnemyHearing>();
        enemyVision = GetComponent<EnemyVision>();
        enemyStats = GetComponent<EnemyStats>();

        GameObject bulletObj = GameObject.FindGameObjectWithTag("Bullet");
        if (bulletObj != null)
        {
            // enemyVision.SetBullet(bulletObj);
            
        }

        // Otomatis cari Player berdasarkan tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Debug.Log(playerObj);
            playerTransform = playerObj.transform;
            enemyVision.SetTarget(playerObj);
        }
    }

    void Update()
    {
        agentPos = transform.position;
        targetPos = playerTransform.transform.position;

        isTargetInSight = enemyVision.CanSeeTarget(agentPos, targetPos);
        isSoundDetected = enemyHearing.CanHearPlayer(agentPos, targetPos);

        Debug.Log(isTargetInSight);

        HandleTensionMeter();

        // Fallback logic
        if (isGettingShot)
        {
            fallbackTimer -= Time.deltaTime;
            FallbackFromPlayer();
            if (fallbackTimer <= 0f)
            {
                isGettingShot = false;
            }
        }
    }

    // Call this method when the enemy is hit by a bullet
    public void OnBulletHit()
    {
        isGettingShot = true;
        if (enemyStats != null)
        {
            // Instead of directly setting fallbackTimer with fearLevel here,
            // we now let EnemyStats handle its own fear level.
            // The TakeDamage method in EnemyStats will call IncreaseFear.
            // If you also want to trigger TakeDamage from here:
            // enemyStats.TakeDamage(0); // Assuming a hit means some form of "damage" event even if no health lost

            // Fallback duration can still be influenced by the current fear level
            fallbackTimer = enemyStats.fearLevel; // Or some scaled version: enemyStats.fearLevel * 0.5f;
        }
        else
        {
            fallbackTimer = 1.5f; // Default fallback duration
        }
    }

    // Move away from the player
    private void FallbackFromPlayer()
    {
        Vector3 directionAway = (agentPos - targetPos).normalized;
        float fallbackSpeed = enemyStats != null ? enemyStats.speed : 3f;
        transform.position += directionAway * fallbackSpeed * Time.deltaTime;
    }

    private void HandleTensionMeter()
    {
        float distance = Vector3.Distance(agentPos, targetPos);
        float distanceFactor = Mathf.Clamp01(1f - (distance / 5f));

        if (isTargetInSight)
        {
            if (distance < 2f)
                tensionMeter = maxTensionMeter;
            else
                tensionMeter += Time.deltaTime * fillSpeed * distanceFactor;
        }
        else
        {
            tensionMeter -= Time.deltaTime * drainSpeed;
        }

        tensionMeter = Mathf.Clamp(tensionMeter, 0f, maxTensionMeter);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float playerVisible = isTargetInSight ? 1f : 0f;
        float canHear = isSoundDetected ? 1f : 0f;
        float tensionFull = IsTensionMeterFull() ? 1f : 0f;
        float tensionChange = tensionMeter - lastTensionMeter;

        //Position & Rotation Observations
        sensor.AddObservation(agentPos);
        sensor.AddObservation(targetPos);
        sensor.AddObservation(transform.up.normalized);

        //Distance Observation
        sensor.AddObservation((targetPos - agentPos).normalized);

        //Status Observations
        sensor.AddObservation(tensionChange);
        sensor.AddObservation(tensionFull);
        sensor.AddObservation(playerVisible);
        sensor.AddObservation(canHear);
        if (enemyStats != null)
        {
            sensor.AddObservation(enemyStats.fearLevel / enemyStats.maxFearLevel); // Normalize fear level
        }
        else
        {
            sensor.AddObservation(0f); // Default if no stats
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveAction = actions.DiscreteActions[0];
        float lookAction = actions.DiscreteActions[1];

        enemyMovement.Move(moveAction, lookAction);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var cont = actionsOut.ContinuousActions;
        cont[0] = Input.GetKey(KeyCode.W) ? 1f : 0f;
        cont[1] = Input.GetKey(KeyCode.A) ? 1f : Input.GetKey(KeyCode.D) ? -1f : 0f;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            // Assuming the bullet script itself calls TakeDamage on the EnemyStats component.
            // If not, you might need to get the damage from the bullet and call:
            // enemyStats.TakeDamage(bulletDamage); 
            // For now, OnBulletHit will be called, which can then use the current fear level for fallback.
            OnBulletHit(); 
        }

        if (collision.gameObject.CompareTag("Wall") ||
            (collision.gameObject.CompareTag("Player") && enemyVision.CanSeeTarget(agentPos, targetPos) && IsTensionMeterFull()))
        {
            EndEpisode();
        }
    }

    public bool IsTensionMeterFull()
    {
        return tensionMeter >= maxTensionMeter;
    }
}

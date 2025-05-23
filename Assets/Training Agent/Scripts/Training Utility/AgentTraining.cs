using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class AgentTraining : Agent
{
    [Header("Training Manager Gameobject")]
    [SerializeField] SpawnerTraining spawnerTraining;

    [Header("Target Reference")]
    [SerializeField] private GameObject targetObj;

    [Header("Agent Settings")]
    [SerializeField] public float tensionMeter;
    [SerializeField] public float maxTensionMeter;
    [SerializeField] private float fillSpeed = 0.5f;
    [SerializeField] private float drainSpeed = 0.2f;
    [SerializeField] private float memoryDuration = 10f;
    private float currentMemoryTimer = 0f;
    private bool hasPlayerMemory = false;

    [Header("Training Settings")]
    [SerializeField] float timePast = 0;

    private float lastTensionMeter = 0f;

    bool isTargetInSight = false;
    bool isSoundDetected = false;

    Vector3 agentPos;
    Vector3 targetPos;

    private EnemyVision enemyVision;
    private EnemyHearing enemyHearing;
    private EnemyMovement enemyMovement;
    private EnemyStats enemyStats;
    float previousDistanceToTarget = 0f;
    float normalizedHealth = 0f;

    public override void Initialize()
    {
        enemyMovement = GetComponent<EnemyMovement>();
        enemyHearing = GetComponent<EnemyHearing>();
        enemyVision = GetComponent<EnemyVision>();
        enemyStats = GetComponent<EnemyStats>();
        enemyVision.SetTarget(targetObj);
    }

    void Update()
    {
        agentPos = transform.position;
        targetPos = targetObj.transform.position;

        isTargetInSight = enemyVision.CanSeeTarget(agentPos, targetPos);
        isSoundDetected = enemyHearing.CanHearPlayer(agentPos, targetPos);

        Debug.Log(isTargetInSight);

        if (isSoundDetected)
        {
            currentMemoryTimer = memoryDuration;
            hasPlayerMemory = true;
        }

        if (hasPlayerMemory)
        {
            if (previousDistanceToTarget == 0f)
            {
                previousDistanceToTarget = Vector2.Distance(agentPos, targetPos);
            }

            currentMemoryTimer -= Time.deltaTime;
            if (currentMemoryTimer <= 0)
            {
                hasPlayerMemory = false;
            }
        }
        else
        {
            previousDistanceToTarget = 0f;
        }
    }

    public override void OnEpisodeBegin()
    {
        tensionMeter = 0f;
        previousDistanceToTarget = 0f;
        spawnerTraining.ResetStartPosition();
        enemyStats.health = Random.Range(10, enemyStats.maxHealth);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float playerVisible = isTargetInSight ? 1f : 0f;
        float canHear = isSoundDetected ? 1f : 0f;
        float tensionFull = IsTensionMeterFull() ? 1f : 0f;
        float tensionChange = tensionMeter - lastTensionMeter;
        normalizedHealth = enemyStats.health / enemyStats.maxHealth;

        Debug.Log($"Current health {normalizedHealth}");

        //Position & Rotation Observations
        sensor.AddObservation(agentPos);
        sensor.AddObservation(transform.up.normalized);

        if (isTargetInSight || hasPlayerMemory)
        {
            sensor.AddObservation(targetPos);
            Vector3 targetRelativePosition = targetPos - agentPos;
            sensor.AddObservation(targetRelativePosition.normalized);
            sensor.AddObservation(targetRelativePosition.magnitude/5f);
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
        }

        //Status Observations
        sensor.AddObservation(tensionChange);
        sensor.AddObservation(tensionFull);
        sensor.AddObservation(playerVisible);
        sensor.AddObservation(canHear);
        sensor.AddObservation(normalizedHealth);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        Vector3 move;
        float rotation;

        var discreteActions = actions.DiscreteActions;
        move = discreteActions[0] == 1 ? transform.up : Vector3.zero;
        rotation = discreteActions[1] == 1 ? 1f : discreteActions[1] == 2 ? -1f : 0f;

        enemyMovement.Move(move, rotation);
        HandleTensionMeter();

        if (normalizedHealth < 0.3)
        {
            float distToTarget = Vector2.Distance(agentPos, targetPos);
            if (distToTarget < previousDistanceToTarget)
            {
                AddReward(-0.5f); // penalti karena masih mendekat padahal darah rendah
            }
            else
            {
                AddReward(0.05f); // reward karena menjauh
            }
        }

        if (hasPlayerMemory && normalizedHealth > 0.3)
        {
            float distToTarget = Vector2.Distance(agentPos, targetPos);
            if (distToTarget < previousDistanceToTarget)
            {
                previousDistanceToTarget = distToTarget;
                AddReward(0.0002f);
            }

            if (distToTarget > previousDistanceToTarget)
            {
                AddReward(-0.002f);
            }
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var cont = actionsOut.DiscreteActions;
        cont[0] = Input.GetKey(KeyCode.W) ? 1 : 0;
        cont[1] = Input.GetKey(KeyCode.A) ? 1 : Input.GetKey(KeyCode.D) ? 2 : 0;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && IsTensionMeterFull())
        {
            if (normalizedHealth > 0.3)
            {
                AddReward(2f);
            }
            else
            {
                AddReward(0.01f);
            }
            EndEpisode();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.2f);
            EndEpisode();
        }
    }

    private bool IsTensionMeterFull()
    {
        return tensionMeter >= maxTensionMeter;
    }

    private void HandleTensionMeter()
    {
        float distance = Vector3.Distance(agentPos, targetPos);
        float distanceFactor = Mathf.Clamp01(1f - (distance / 5f));

        if (isSoundDetected || isTargetInSight)
        {
            if (distance < 3f)
                tensionMeter = maxTensionMeter;
            else
                tensionMeter += Time.deltaTime * fillSpeed * distanceFactor;
        }
        else if (tensionMeter != 0)
        {
            tensionMeter -= Time.deltaTime * drainSpeed;
        }

        tensionMeter = Mathf.Clamp(tensionMeter, 0f, maxTensionMeter);
    }

}

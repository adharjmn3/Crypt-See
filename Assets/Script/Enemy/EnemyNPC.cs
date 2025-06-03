using System;
using Player.Stats;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class EnemyNPC : Agent
{
    [Header("Target Reference")]
    [SerializeField] private GameObject targetObj;
    [SerializeField] GameObject[] starPosition;
    [SerializeField] GameObject[] targetPosition;

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

    public bool isTargetInSight = false;
    bool isSoundDetected = false;

    Vector3 agentPos;
    Vector3 targetPos;

    private EnemyVision enemyVision;
    private EnemyHearing enemyHearing;
    private EnemyMovement enemyMovement;
    private EnemyStats enemyStats;
    private EnemyShoot enemyShoot;
    float previousDistanceToTarget = 0f;
    
    // Track if chase mode is active
    private bool isChasing = false;

    private float startTime;
    private float episodeStartDuration;

    public override void Initialize()
    {
        targetObj = GameObject.FindGameObjectWithTag("Player");
        enemyMovement = GetComponent<EnemyMovement>();
        enemyHearing = GetComponent<EnemyHearing>();
        enemyVision = GetComponent<EnemyVision>();
        enemyStats = GetComponent<EnemyStats>();
        enemyVision.SetTarget(targetObj);

        // Get the shooting component
        enemyShoot = GetComponent<EnemyShoot>();

        // Disable shooting at the start
        if (enemyShoot != null)
        {
            enemyShoot.enabled = false;
        }

        episodeStartDuration = Time.time;
    }

    public override void OnEpisodeBegin()
    {
        tensionMeter = 0f;
        previousDistanceToTarget = 0f;

        // Disable shooting when episode restarts
        if (enemyShoot != null)
        {
            enemyShoot.enabled = false;
        }

        isChasing = false;

        targetObj.GetComponent<Health>().currentHealth = 100;
        episodeStartDuration = Time.time;


        int index = UnityEngine.Random.Range(0, starPosition.Length);
        transform.position = starPosition[index].transform.position;

        index = UnityEngine.Random.Range(0, targetPosition.Length);
        targetObj.transform.position = targetPosition[index].transform.position;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float playerVisible = isTargetInSight ? 1f : 0f;
        float canHear = isSoundDetected ? 1f : 0f;
        float tensionFull = IsTensionMeterFull() ? 1f : 0f;
        float tensionChange = tensionMeter - lastTensionMeter;

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
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        StatusUpdate();
        Vector3 move;
        float rotation;

        var discreteActions = actions.DiscreteActions;
        move = discreteActions[0] == 1 ? transform.up : Vector3.zero;
        rotation = discreteActions[1] == 1 ? 1f : discreteActions[1] == 2 ? -1f : 0f;

        enemyMovement.Move(move, rotation);
        HandleTensionMeter();
        UpdateChaseState();

        if (targetObj.GetComponent<Health>().currentHealth <= 0)
        {
            float episodeDuration = Time.time - episodeStartDuration;
            float captureTime = Time.time - startTime;
            FindObjectOfType<StatisticLogger>().LogData("Yes", captureTime, episodeDuration);
            EndEpisode();
        }

        if (Time.time - episodeStartDuration >= 60f)
        {
            float episodeDuration = Time.time - episodeStartDuration;
            float captureTime = -1;
            FindObjectOfType<StatisticLogger>().LogData("No", captureTime, episodeDuration);
            EndEpisode();
        }

    }

    private void StatusUpdate()
    {
        agentPos = transform.position;
        targetPos = targetObj.transform.position;

        isTargetInSight = enemyVision.CanSeeTarget(agentPos, targetPos);
        isSoundDetected = enemyHearing.CanHearPlayer(agentPos, targetPos);

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

    // Update chase state and toggle shooting accordingly
    private void UpdateChaseState()
    {
        bool shouldChase = IsTensionMeterFull();
        
        // Only update if there's a state change
        if (shouldChase != isChasing)
        {
            isChasing = shouldChase;
            
            // Toggle shooting component
            if (enemyShoot != null)
            {
                enemyShoot.enabled = isChasing;

                // Log for debugging
                if (isChasing)
                {
                    startTime = Time.time;
                    Debug.Log($"{gameObject.name}: <color=red>Started chasing - enabling shooting</color>");
                }
                else
                {
                    Debug.Log($"{gameObject.name}: <color=blue>Stopped chasing - disabling shooting</color>");
                }
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
            
        }
    }

    public bool IsTensionMeterFull()
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

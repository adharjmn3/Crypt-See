using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class AgentTraining : Agent
{
    [Header("Target Reference")]
    [SerializeField] private GameObject targetObj;
    [SerializeField] GameObject[] agentSpawnPoint;
    [SerializeField] GameObject[] targetSpawnPoints;

    [Header("Agent Settings")]
    [SerializeField] public float tensionMeter;
    [SerializeField] public float maxTensionMeter;
    [SerializeField] private float fillSpeed = 0.5f;
    [SerializeField] private float drainSpeed = 0.2f;

    [Header("Training Settings")]
    [SerializeField] float seeTimeThreshold = 2f;

    private float lastTensionMeter = 0f;

    bool isTargetInSight = false;
    bool isSoundDetected = false;

    Vector3 agentPos;
    Vector3 targetPos;

    private EnemyVision enemyVision;
    private EnemyHearing enemyHearing;
    private EnemyMovement enemyMovement;

    HashSet<Vector2Int> visitedTiles = new HashSet<Vector2Int>();

    private float lastRotation = 0f;
    private const float spinPenaltyThreshold = 45f; // Degrees
    private const float spinPenaltyAmount = -0.1f;

    public override void Initialize()
    {
        enemyMovement = GetComponent<EnemyMovement>();
        enemyHearing = GetComponent<EnemyHearing>();
        enemyVision = GetComponent<EnemyVision>();
        enemyVision.SetTarget(targetObj);
    }

    void Update()
    {
        agentPos = transform.position;
        targetPos = targetObj.transform.position;

        isTargetInSight = enemyVision.CanSeeTarget(agentPos, targetPos);
        isSoundDetected = enemyHearing.CanHearPlayer(agentPos, targetPos);

        HandleTensionMeter();
        ExplorationReward();
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

            if (IsTensionMeterFull())
            {
                // Reward semakin dekat ke player
                float approachReward = (1f - distance / 5f) * 0.01f;
                AddReward(approachReward);
            }
            else
            {
                AddReward(0.001f);
            }
        }
        else
        {
            tensionMeter -= Time.deltaTime * drainSpeed;
        }

        tensionMeter = Mathf.Clamp(tensionMeter, 0f, maxTensionMeter);
    }

    private void ExplorationReward()
    {
        Vector2Int tilePos = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));

        if (!visitedTiles.Contains(tilePos))
        {
            visitedTiles.Add(tilePos);
            AddReward(0.002f);
        }
    }

    public override void OnEpisodeBegin()
    {
        tensionMeter = 0f;
        transform.localPosition = agentSpawnPoint[Random.Range(0, agentSpawnPoint.Length)].transform.localPosition;
        transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-180, 180));

        Vector3 newTargetPos = targetSpawnPoints[Random.Range(0, targetSpawnPoints.Length)].transform.localPosition;
        targetObj.transform.localPosition = newTargetPos;
        visitedTiles.Clear();
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
        sensor.AddObservation(transform.localRotation.z);

        //Distance Observation
        sensor.AddObservation((targetPos - agentPos).normalized);

        //Status Observations
        sensor.AddObservation(tensionChange);
        sensor.AddObservation(tensionFull);
        sensor.AddObservation(playerVisible);
        sensor.AddObservation(canHear);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveAction = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f);
        float lookAction = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        enemyMovement.Move(moveAction, lookAction);

        float currentRotation = transform.localRotation.eulerAngles.z;
        float rotationDelta = Mathf.DeltaAngle(lastRotation, currentRotation); // gunakan DeltaAngle agar akurat

        if (Mathf.Abs(rotationDelta) > spinPenaltyThreshold && moveAction < 0.1f)
        {
            AddReward(spinPenaltyAmount);  // Penalti hanya jika muter di tempat
        }
        lastRotation = currentRotation;

        if (!isTargetInSight && !isSoundDetected && moveAction < 0.1f)
        {
            AddReward(-0.001f); // Penalti jika diam saja saat tidak tahu arah
        }

        bool tensionFull = IsTensionMeterFull();

        if(moveAction >= 0.1f){
            AddReward(0.0001f);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var cont = actionsOut.ContinuousActions;
        cont[0] = Input.GetKey(KeyCode.W) ? 1f : 0f;
        cont[1] = Input.GetKey(KeyCode.A) ? 1f : Input.GetKey(KeyCode.D) ? -1f : 0f;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && isTargetInSight && IsTensionMeterFull())
        {
            AddReward(1f);
            EndEpisode();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.5f);
            // EndEpisode();
        }
    }

    private bool IsTensionMeterFull(){
        return tensionMeter >= maxTensionMeter;
    }
}

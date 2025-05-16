using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class AgentTraining : Agent
{
    [Header("Target Reference")]
    [SerializeField] private GameObject targetObj;
    [SerializeField] Transform[] spawnPoints;

    [Header("Agent Settings")]
    [SerializeField] public float tensionMeter;
    [SerializeField] public float maxTensionMeter;
    [SerializeField] private float fillSpeed = 0.5f;
    [SerializeField] private float drainSpeed = 0.2f;
    [SerializeField] private float memoryDuration = 5f;

    [Header("Training Settings")]
    [SerializeField] float seeTimeThreshold = 2f;

    private float lastTensionMeter = 0f;

    bool isTargetInSight = false;
    bool isSoundDetected = false;

    Vector3 agentPos;
    Vector3 targetPos;
    Vector3 targetLastPosition = Vector3.zero;

    private EnemyVision enemyVision;
    private EnemyHearing enemyHearing;
    private EnemyMovement enemyMovement;
    List<Transform> spawnPointsList = new List<Transform>();
    private float lastHeardTime = -Mathf.Infinity;
    private float reachedSoundRadius = 3f;
    float previousDistanceToTarget;

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

        if (isSoundDetected)
        {
            targetLastPosition = targetPos;
            lastHeardTime = Time.time;
        }

        HandleTensionMeter();

        // if (!isTargetInSight && Time.time - lastHeardTime < memoryDuration && IsTensionMeterFull())
        // {
        //     float distanceToLastHeard = Vector3.Distance(agentPos, targetLastPosition);
        //     float approachReward = (1f - distanceToLastHeard / 5f) * 0.01f;
        //     AddReward(approachReward);
        //     if (distanceToLastHeard < reachedSoundRadius)
        //     {
        //         AddReward(0.1f); // Reward saat mencapai posisi suara terakhir
        //         lastHeardTime = -Mathf.Infinity;
        //     }
        // }
    }

    private void HandleTensionMeter()
    {
        float distance = Vector3.Distance(agentPos, targetPos);
        float distanceFactor = Mathf.Clamp01(1f - (distance / 5f));

        if (isTargetInSight || isSoundDetected)
        {
            if (isTargetInSight)
            {
                if (distance < 4f)
                    tensionMeter = maxTensionMeter;
                else
                    tensionMeter += Time.deltaTime * fillSpeed * distanceFactor;
            }
            else if (isSoundDetected)
            {
                if (distance < 6f)
                    tensionMeter = maxTensionMeter;
                else
                    tensionMeter += Time.deltaTime * fillSpeed * distanceFactor;
            }

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
        else if (tensionMeter != 0)
        {
            tensionMeter -= Time.deltaTime * drainSpeed;
            AddReward(-0.02f);
        }

        tensionMeter = Mathf.Clamp(tensionMeter, 0f, maxTensionMeter);
    }

    public override void OnEpisodeBegin()
    {
        tensionMeter = 0f;

        foreach (var point in spawnPoints)
        {
            spawnPointsList.Add(point);
        }

        int index = Random.Range(0, spawnPointsList.Count);
        transform.localPosition = spawnPointsList[index].localPosition;
        spawnPointsList.RemoveAt(index);

        index = Random.Range(0, spawnPointsList.Count);
        targetObj.transform.localPosition = spawnPointsList[index].localPosition;

        spawnPointsList.Clear();
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

        if (canHear == 1f || playerVisible == 1f)
        {
            sensor.AddObservation(targetPos);
            Vector3 targetRelativePosition = targetPos - agentPos;
            sensor.AddObservation(targetRelativePosition.normalized);
            sensor.AddObservation(targetRelativePosition.magnitude);
        }
        else if (Time.time - lastHeardTime < memoryDuration)
        {
            sensor.AddObservation(targetLastPosition);
            Vector3 targetRelativePosition = targetLastPosition - agentPos;
            sensor.AddObservation(targetRelativePosition.normalized);
            sensor.AddObservation(targetRelativePosition.magnitude);
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
        float moveAction = actions.DiscreteActions[0];
        float lookAction = actions.DiscreteActions[1];

        enemyMovement.Move(moveAction, lookAction);
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
            AddReward(1f);
            EndEpisode();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.2f);
            //EndEpisode();
        }
    }

    private bool IsTensionMeterFull(){
        return tensionMeter >= maxTensionMeter;
    }
}

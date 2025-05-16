using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Tilemaps;

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
    List<Transform> spawnPointsList = new List<Transform>();

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
        sensor.AddObservation(targetPos);
        sensor.AddObservation(transform.up.normalized);

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
            AddReward(-0.2f);
            //EndEpisode();
        }
    }

    private bool IsTensionMeterFull(){
        return tensionMeter >= maxTensionMeter;
    }
}

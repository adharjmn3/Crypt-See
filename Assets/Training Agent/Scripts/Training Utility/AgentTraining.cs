using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class AgentTraining : Agent
{
    [Header("Target Reference")]
    [SerializeField] private GameObject targetObj;
    [SerializeField] GameObject agentSpawnPoint;
    [SerializeField] GameObject[] targetSpawnPoints;

    [Header("Agent Settings")]
    [SerializeField] public float tensionMeter;
    [SerializeField] public float maxTensionMeter;
    [SerializeField] private float fillSpeed = 0.5f;
    [SerializeField] private float drainSpeed = 0.2f;

    [Header("Training Settings")]
    [SerializeField] float seeTimeThreshold = 2f;
    private float seeTimer = 0f;

    private float lastTensionMeter = 0f;

    bool isTargetInSight = false;
    bool isSoundDetected = false;

    Vector3 agentPos;
    Vector3 targetPos;

    private EnemyVision enemyVision;
    private EnemyHearing enemyHearing;
    private EnemyMovement enemyMovement;

    public override void Initialize()
    {
        enemyMovement = GetComponent<EnemyMovement>();
        enemyHearing = GetComponent<EnemyHearing>();
        enemyVision = GetComponent<EnemyVision>();

        enemyVision.SetTarget(targetObj);
    }

    void Update()
    {
        agentPos = transform.localPosition;
        targetPos = targetObj.transform.localPosition;

        isTargetInSight = enemyVision.CanSeeTarget(agentPos, targetPos);
        isSoundDetected = enemyHearing.CanHearPlayer(agentPos, targetPos);

        if(isTargetInSight){
            seeTimer += Time.deltaTime;
            AddReward(0.0001f);
            if(seeTimer >= seeTimeThreshold){
                AddReward(1f);
                EndEpisode();
            }
        }
        else{
            seeTimer = 0f;
        }
    }

    public override void OnEpisodeBegin()
    {
        tensionMeter = 0f;
        seeTimer = 0f;
        transform.localPosition = agentSpawnPoint.transform.localPosition;
        transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-180, 180));

        Vector3 newTargetPos = targetSpawnPoints[Random.Range(0, targetSpawnPoints.Length)].transform.localPosition;
        targetObj.transform.localPosition = newTargetPos;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(agentPos);
        sensor.AddObservation(targetPos);
        sensor.AddObservation(transform.localRotation.z);

        sensor.AddObservation(Vector2.Distance(agentPos, targetPos));
        sensor.AddObservation((targetPos - agentPos).normalized);

        float playerVisible = isTargetInSight ? 1f : 0f;
        float canHear = isSoundDetected ? 1f : 0f;
        float tensionFull = IsTensionMeterFull() ? 1f : 0f;
        float tensionChange = tensionMeter - lastTensionMeter;

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
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.5f);
            EndEpisode();
        }

        if (collision.gameObject.CompareTag("Player") && isTargetInSight && IsTensionMeterFull())
        {
            AddReward(1f);
            EndEpisode();
        }
    }

    private bool IsTensionMeterFull(){
        return tensionMeter >= maxTensionMeter;
    }
}

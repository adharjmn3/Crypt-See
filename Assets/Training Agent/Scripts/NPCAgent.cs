using System;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class NPCAgent : Agent
{
    [Header("Target Reference")]
    [SerializeField] private GameObject targetObj;

    [Header("Agent Settings")]
    [SerializeField] public float tensionMeter;
    [SerializeField] public float maxTensionMeter;
    [SerializeField] private float fillSpeed = 0.5f;
    [SerializeField] private float drainSpeed = 0.2f;

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
        agentPos = transform.position;
        targetPos = targetObj.transform.position;

        isTargetInSight = enemyVision.CanSeeTarget(agentPos, targetPos);
        isSoundDetected = enemyHearing.CanHearPlayer(agentPos, targetPos);
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
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var cont = actionsOut.ContinuousActions;
        cont[0] = Input.GetKey(KeyCode.W) ? 1f : 0f;
        cont[1] = Input.GetKey(KeyCode.A) ? 1f : Input.GetKey(KeyCode.D) ? -1f : 0f;
    }

    private bool IsTensionMeterFull(){
        return tensionMeter >= maxTensionMeter;
    }
}

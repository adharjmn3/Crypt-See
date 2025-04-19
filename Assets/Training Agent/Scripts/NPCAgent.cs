using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class NPCAgent : Agent
{
    [Header("Gameobject Reference")]
    [SerializeField] private TrainingManager trainingManager;

    [Header("Agent Settings")]
    [SerializeField] private float tensionMeter;
    [SerializeField] private float maxTensionMeter;
    [SerializeField] private float fillSpeed = 0.5f;
    [SerializeField] private float drainSpeed = 0.2f;
    private float lastTensionMeter = 0f;

    private EnemyVision enemyVision;
    private EnemyHearing enemyHearing;
    private EnemyMovement enemyMovement;

    public override void Initialize()
    {
        enemyMovement = GetComponent<EnemyMovement>();
        enemyHearing = GetComponent<EnemyHearing>();
        enemyVision = GetComponent<EnemyVision>();

        enemyVision.SetTarget(trainingManager.playerGameobject, trainingManager);
    }

    void Update()
    {
        bool canSee = enemyVision.CanSeeTarget();
        bool canHear = enemyHearing.CanHearPlayer(trainingManager.GetAgentPosition(), trainingManager.GetPlayerPosition());

        if(canSee || canHear){
            tensionMeter = MathF.Min(maxTensionMeter, tensionMeter + fillSpeed * Time.deltaTime);
        }
        else {
            tensionMeter = MathF.Max(0f, tensionMeter - drainSpeed * Time.deltaTime);
        }

        lastTensionMeter = tensionMeter;
    }

    public override void OnEpisodeBegin()
    {
        trainingManager.ResetTrainingPosition();
        tensionMeter = 0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(trainingManager.GetAgentPosition());
        sensor.AddObservation(trainingManager.GetPlayerPosition());

        float playerVisible = enemyVision.CanSeeTarget() ? 1f : 0f;
        float canHear = enemyHearing.CanHearPlayer(trainingManager.GetAgentPosition(), trainingManager.GetPlayerPosition()) ? 1f : 0f;
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

        bool canSee = enemyVision.CanSeeTarget();
        bool canHear = enemyHearing.CanHearPlayer(trainingManager.GetAgentPosition(), trainingManager.GetPlayerPosition());
        bool tensionFull = IsTensionMeterFull();

        Debug.Log(tensionMeter);

        if(tensionFull){
            Vector2 toPlayer = trainingManager.GetPlayerPosition() - trainingManager.GetAgentPosition();
            float distance = toPlayer.magnitude;

            // Semakin dekat dengan player, semakin besar reward
            float approachReward = Mathf.Clamp01(1f - distance / 10f); // anggap 10 sebagai jarak max
            AddReward(approachReward * 0.001f);

            // Tambahan bonus kecil jika agen bergerak (dorong untuk tidak diam)
            if (moveAction > 0.1f)
            {
                AddReward(0.0005f);
            }

            if (distance < 2.5f && moveAction < 0.1f)
            {
                // Agen terlalu dekat tapi tidak melakukan apa-apa
                AddReward(-0.001f);
            }
        }

        if(canSee || canHear){
            if(!tensionFull){
                AddReward(0.0001f);
            }
        }

        bool isFilling = tensionMeter > lastTensionMeter;

        if (isFilling && moveAction > 0.1f)
        {
            // Penalti jika agen bergerak saat tension mengisi
            AddReward(-0.001f);
        }

        if (isFilling && Mathf.Abs(lookAction) < 0.01f && !canSee)
        {
            // Bonus kecil jika tetap melihat untuk menemukan player saat tension naik
            AddReward(0.0005f);
        }

        AddReward(0.0001f);
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

        if (collision.gameObject.CompareTag("Player") && enemyVision.CanSeeTarget() && IsTensionMeterFull())
        {
            AddReward(1f);
            EndEpisode();
        }
        else if(collision.gameObject.CompareTag("Player") && enemyVision.CanSeeTarget() && !IsTensionMeterFull()){
            AddReward(-0.5f);
            EndEpisode();
        }
    }

    private bool IsTensionMeterFull(){
        return tensionMeter >= maxTensionMeter;
    }
}

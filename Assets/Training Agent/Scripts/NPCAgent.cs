using System;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Tilemaps;

public class NPCAgent : Agent
{
    [Header("Gameobject Reference")]
    [SerializeField] private TrainingManager trainingManager;

    [Header("Agent Settings")]
    [SerializeField] public float tensionMeter;
    [SerializeField] public float maxTensionMeter;
    [SerializeField] private float fillSpeed = 0.5f;
    [SerializeField] private float drainSpeed = 0.2f;
    private float lastTensionMeter = 0f;

    private EnemyVision enemyVision;
    private EnemyHearing enemyHearing;
    private EnemyMovement enemyMovement;

    private Vector2 lastExplorationPosition;
    private float explorationThreshold = 3f; // jarak minimum untuk dianggap menjelajah
    private float explorationReward = 0.001f;

    private HashSet<Vector2Int> visitedTiles = new HashSet<Vector2Int>();
    [SerializeField] private float subgoalReward = 0.001f;
    [SerializeField] private Tilemap tilemap; // drag dari inspector

    public override void Initialize()
    {
        enemyMovement = GetComponent<EnemyMovement>();
        enemyHearing = GetComponent<EnemyHearing>();
        enemyVision = GetComponent<EnemyVision>();

        //enemyVision.SetTarget(trainingManager.playerGameobject, trainingManager);
        lastExplorationPosition = trainingManager.GetAgentPosition();
    }

    // void Update()
    // {
    //     //bool canSee = enemyVision.CanSeeTarget();
    //     bool canHear = enemyHearing.CanHearPlayer(trainingManager.GetAgentPosition(), trainingManager.GetPlayerPosition());

    //     if(canSee || canHear){
    //         float distance = Vector3.Distance(trainingManager.GetAgentPosition(), trainingManager.GetPlayerPosition());
    //         float proximityFactor = Mathf.Clamp01(1f - distance / 10f); // semakin dekat, semakin mendekati 1

    //         float adjustedFillSpeed = fillSpeed * (0.5f + proximityFactor); // dasar 0.5x, naik hingga 1.5x saat dekat
    //         tensionMeter = MathF.Min(maxTensionMeter, tensionMeter + adjustedFillSpeed * Time.deltaTime);
    //         if(distance < 2f){
    //             tensionMeter = maxTensionMeter;
    //         }
    //     }
    //     else {
    //         tensionMeter = MathF.Max(0f, tensionMeter - drainSpeed * Time.deltaTime);
    //     }

    //     lastTensionMeter = tensionMeter;
    // }

    // public override void OnEpisodeBegin()
    // {
    //     trainingManager.ResetTrainingPosition();
    //     tensionMeter = 0f;
    //     visitedTiles.Clear();
    // }

    // public override void CollectObservations(VectorSensor sensor)
    // {
    //     sensor.AddObservation(trainingManager.GetAgentPosition());
    //     sensor.AddObservation(trainingManager.GetPlayerPosition());
    //     sensor.AddObservation(transform.localRotation.z);

    //     sensor.AddObservation(Vector2.Distance(trainingManager.GetPlayerPosition(), trainingManager.GetAgentPosition()));
    //     sensor.AddObservation((trainingManager.GetPlayerPosition() - trainingManager.GetAgentPosition()).normalized);

    //     //float playerVisible = enemyVision.CanSeeTarget() ? 1f : 0f;
    //     float canHear = enemyHearing.CanHearPlayer(trainingManager.GetAgentPosition(), trainingManager.GetPlayerPosition()) ? 1f : 0f;
    //     float tensionFull = IsTensionMeterFull() ? 1f : 0f;
    //     float tensionChange = tensionMeter - lastTensionMeter;

    //     sensor.AddObservation(tensionChange);
    //     sensor.AddObservation(tensionFull);
    //     sensor.AddObservation(playerVisible);
    //     sensor.AddObservation(canHear);
    // }

    // public override void OnActionReceived(ActionBuffers actions)
    // {
    //     float moveAction = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f);
    //     float lookAction = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

    //     enemyMovement.Move(moveAction, lookAction);

    //     //bool canSee = enemyVision.CanSeeTarget();
    //     bool canHear = enemyHearing.CanHearPlayer(trainingManager.GetAgentPosition(), trainingManager.GetPlayerPosition());
    //     bool tensionFull = IsTensionMeterFull();

    //     Debug.Log(tensionMeter);

    //     if(tensionFull){
    //         Vector2 toPlayer = trainingManager.GetPlayerPosition() - trainingManager.GetAgentPosition();
    //         float distance = toPlayer.magnitude;

    //         // Semakin dekat dengan player, semakin besar reward
    //         float approachReward = Mathf.Clamp01(1f - distance / 10f); // anggap 10 sebagai jarak max
    //         float chaseBonus = Mathf.Pow(approachReward, 2f); // lebih tajam reward saat dekat
    //         AddReward(chaseBonus * 0.01f);

    //         if(moveAction > 0.8f){
    //             AddReward(0.001f);
    //         }
    //         else if(moveAction < 0.6f){
    //             AddReward(-0.0005f);
    //         }
    //     }
    //     else if(canHear || canSee){
    //         AddReward(0.001f);
    //     }
    //     else{
    //         if (moveAction > 0.8f && Mathf.Abs(lookAction) < 0.1f)
    //         {
    //             AddReward(0.0005f); // reward kecil untuk maju lurus
    //         }

    //         if (Mathf.Abs(lookAction) > 0.5f && moveAction < 0.1f)
    //         {
    //             AddReward(-0.0002f); // penalti kecil karena hanya muter tanpa gerak
    //         }

    //         Vector3 worldPos = trainingManager.GetAgentPosition();
    //         Vector3Int cellPos = tilemap.WorldToCell(worldPos);
    //         Vector2Int tileCoord = new Vector2Int(cellPos.x, cellPos.y);

    //         if (!visitedTiles.Contains(tileCoord)) {
    //             visitedTiles.Add(tileCoord);
    //             AddReward(subgoalReward);
    //         }
    //     }

    //     bool isFilling = tensionMeter > lastTensionMeter;
    //     bool isDraining = tensionMeter < lastTensionMeter;

    //     if(isDraining){
    //         AddReward(-0.005f);
    //     }

    //     if (isFilling && Mathf.Abs(lookAction) < 0.01f && !canSee)
    //     {
    //         // Bonus kecil jika tetap melihat untuk menemukan player saat tension naik
    //         AddReward(0.0005f);
    //     }
    // }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var cont = actionsOut.ContinuousActions;
        cont[0] = Input.GetKey(KeyCode.W) ? 1f : 0f;
        cont[1] = Input.GetKey(KeyCode.A) ? 1f : Input.GetKey(KeyCode.D) ? -1f : 0f;
    }

    // void OnTriggerEnter2D(Collider2D collision)
    // {
    //     if (collision.gameObject.CompareTag("Wall"))
    //     {
    //         AddReward(-0.5f);
    //         EndEpisode();
    //     }

    //     if (collision.gameObject.CompareTag("Player") && enemyVision.CanSeeTarget() && IsTensionMeterFull())
    //     {
    //         AddReward(2f);
    //         EndEpisode();
    //     }
    // }

    private bool IsTensionMeterFull(){
        return tensionMeter >= maxTensionMeter;
    }
}

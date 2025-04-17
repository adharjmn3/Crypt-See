using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class NPCAgent : Agent
{
    [Header("Gameobject Reference")]
    [SerializeField] private TrainingManager trainingManager;

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

    public override void OnEpisodeBegin()
    {
        trainingManager.ResetTrainingPosition();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(trainingManager.GetAgentPosition());
        sensor.AddObservation(trainingManager.GetPlayerPosition());

        float playerVisible = enemyVision.CanSeeTarget() ? 1f : 0f;
        float canHear = enemyHearing.CanHearPlayer(trainingManager.GetAgentPosition(), trainingManager.GetPlayerPosition()) ? 1f : 0f;

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

        Debug.Log(canSee);

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

        if (collision.gameObject.CompareTag("Player") && enemyVision.CanSeeTarget())
        {
            AddReward(1f);
            EndEpisode();
        }
    }
}

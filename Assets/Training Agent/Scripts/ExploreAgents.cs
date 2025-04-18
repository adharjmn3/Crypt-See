using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.Rendering.Universal;
using Player.Stats;
using UnityEditor;

public class ExploreAgents : Agent
{
    [Header("Player & Environment")]
    [SerializeField] private GameObject player;
    [SerializeField] private Light2D[] lights;
    [SerializeField] private Transform[] points;
    [SerializeField] private Transform[] pointsWithoutLight;

    [Header("Agent Properties")]
    [SerializeField] private float hearingRadius = 1.5f;
    [SerializeField] private float chaseDelay = 2f;

    private EnemyMovement movement;
    private PositionHandler agentPositionHandler;
    private PositionHandler playerPositionHandler;
    private EnemyVision enemyVision;
    private EnemyHearing enemyHearing;
    private float lastDistance = 0, currentDistance = 0;
    private Vector3 startPos;
    private Vector3 playerPos;
    private float difficulty;
    private int steps;
    private Vector3 lastSeenPosition;
    private float tensionMeter = 0f;

    public override void Initialize()
    {
        movement = GetComponent<EnemyMovement>();
        agentPositionHandler = GetComponent<PositionHandler>();
        playerPositionHandler = player.GetComponent<PositionHandler>();
        enemyVision = GetComponent<EnemyVision>();
        enemyHearing = GetComponent<EnemyHearing>();

        //enemyVision.SetTarget(player);
        startPos = points[0].localPosition;
    }

    public override void OnEpisodeBegin()
    {
        if (steps >= MaxStep && difficulty >= 2)
        {
            AddReward(-1f);
        }

        movement.Stop();

        steps = 0;
        lastSeenPosition = Vector3.zero;
        tensionMeter = 0;
        difficulty = Academy.Instance.EnvironmentParameters.GetWithDefault("difficulty", 0f);

        PositionManagement();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(agentPositionHandler.GetPosition());
        sensor.AddObservation(player.transform.localPosition);
        sensor.AddObservation(Vector3.Distance(agentPositionHandler.GetPosition(), playerPositionHandler.GetPosition()));

        float playerInLight = IsPlayerInLight() ? 1f : 0f;
        float playerVisible = enemyVision.CanSeeTarget() ? 1f : 0f;
        float canHear = enemyHearing.CanHearPlayer(agentPositionHandler.GetPosition(), playerPositionHandler.GetPosition()) ? 1f : 0f;

        sensor.AddObservation(canHear);
        sensor.AddObservation(playerInLight);
        sensor.AddObservation(playerVisible);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveAction = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f);
        float rotateAction = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        movement.Move(moveAction, rotateAction);

        float dist = Vector3.Distance(agentPositionHandler.GetPosition(), playerPositionHandler.GetPosition());
        bool canSee = enemyVision.CanSeeTarget();
        bool isInLight = IsPlayerInLight();
        bool canHear = enemyHearing.CanHearPlayer(agentPositionHandler.GetPosition(), playerPositionHandler.GetPosition());

        HandleBehavior(dist, canSee, isInLight, canHear, moveAction, rotateAction);

        steps++;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var cont = actionsOut.ContinuousActions;
        cont[0] = Input.GetKey(KeyCode.W) ? 1f : 0f;
        cont[1] = Input.GetKey(KeyCode.A) ? 1f : Input.GetKey(KeyCode.D) ? -1f : 0f;
    }

    private void PositionManagement()
    {
        if (difficulty == 1.0f)
        {
            playerPos = lights[0].transform.localPosition;
            startPos = points[0].localPosition;
        }
        else if (difficulty == 2.0f)
        {
            playerPos = lights[Random.Range(0, lights.Length)].transform.localPosition;
            startPos = points[Random.Range(0, 2)].localPosition;
        }
        else
        {
            playerPos = pointsWithoutLight[Random.Range(0, pointsWithoutLight.Length)].transform.localPosition;
            startPos = points[Random.Range(0, points.Length)].localPosition;
        }

        agentPositionHandler.SetPosition(startPos + new Vector3(Random.Range(-3, 3), Random.Range(0, 2), 0));
        playerPositionHandler.SetPosition(playerPos + new Vector3(Random.Range(1, 1), Random.Range(0, 1), 0));
    }

    private bool IsPlayerInLight()
    {
        if (player.TryGetComponent(out Visible visible))
        {
            return visible.LightLevel == 1;
        }
        return false;
    }

    private void HandleBehavior(float dist, bool canSee, bool isInLight, bool canHear, float moveAction, float lookAction)
    {
        if (canSee && isInLight)
        {
            Debug.Log("Agen melihat player di cahaya");
            lastSeenPosition = playerPositionHandler.GetPosition();
            Debug.Log($"Posisi player {lastSeenPosition}");
            if (tensionMeter < chaseDelay)
            {
                tensionMeter += 1;
                if(moveAction == 0f){
                    AddReward(0.0001f);
                }
            }
            else
            {
                lastDistance = currentDistance;
                currentDistance = Vector3.Distance(agentPositionHandler.GetPosition(), lastSeenPosition);
                float delta = lastDistance - currentDistance;
                AddReward(delta * 0.01f /dist);
                Debug.Log($"Jarak agen dan player {delta}");
            }
        }
        else if (canSee && dist < 3f)
        {
            Debug.Log("Agen melihat player dari dekat");
            lastSeenPosition = playerPositionHandler.GetPosition();
            Debug.Log($"Posisi player {lastSeenPosition}");
            if (tensionMeter < chaseDelay)
            {
                tensionMeter += 1;
                if(moveAction == 0f){
                    AddReward(0.0001f);
                }
            }
            else
            {
                lastDistance = currentDistance;
                currentDistance = Vector3.Distance(agentPositionHandler.GetPosition(), lastSeenPosition);
                float delta = lastDistance - currentDistance;
                AddReward(delta * 0.01f / dist);
                Debug.Log($"Jarak agen dan player {delta}");
            }
        }
        else if (canHear)
        {
            Debug.Log("Agen mendengar sesuatu");
            lastSeenPosition = playerPositionHandler.GetPosition();
            Debug.Log($"Posisi suara {lastSeenPosition}");
            if (tensionMeter < chaseDelay)
            {
                tensionMeter += 1;
                if(moveAction == 0f){
                    AddReward(0.0001f);
                }
            }
            else
            {
                lastDistance = currentDistance;
                currentDistance = Vector3.Distance(agentPositionHandler.GetPosition(), lastSeenPosition);
                float delta = lastDistance - currentDistance;
                AddReward(delta * 0.01f / dist);
                Debug.Log($"Jarak agen dan suara {delta}");
            }
        }
        else
        {
            tensionMeter = 0f;
            if(lookAction > 0 || lookAction < 0){
                AddReward(-0.0001f);
            }
            AddReward(0.0005f);
        }
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

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.25f);
            Gizmos.DrawSphere(agentPositionHandler.GetPosition(), hearingRadius);
        }
    }
}

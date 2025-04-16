using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.Rendering.Universal;
using Player.Stats;
using UnityEditor;

public class ExploreAgents : Agent
{
    [SerializeField] GameObject player;
    [SerializeField] Light2D[] lights;
    [SerializeField] Transform[] points;
    [SerializeField] Transform[] pointsWithoutLight;
    [SerializeField] float hearingRadius = 1.5f;

    private EnemyMovement movement;
    private PositionHandler agentPositionHandler;
    private PositionHandler playerPositionHandler;
    private EnemyVision enemyVision;

    private Vector3 startPos;
    private Vector3 playerPos;
    private float difficulty;
    private int steps;
    private float previousDistance;
    private AudioSource playerAudioSource;

    public override void Initialize()
    {
        movement = GetComponent<EnemyMovement>();
        agentPositionHandler = GetComponent<PositionHandler>();
        playerPositionHandler = player.GetComponent<PositionHandler>();

        enemyVision = GetComponent<EnemyVision>();
        enemyVision.SetTarget(player);

        startPos = points[0].localPosition;
        playerAudioSource = player.GetComponent<AudioSource>();
    }

    public override void OnEpisodeBegin()
    {
        if (steps >= MaxStep && difficulty >= 2)
        {
            AddReward(-1f);
        }

        steps = 0;
        difficulty = Academy.Instance.EnvironmentParameters.GetWithDefault("difficulty", 0f);

        PositionManagement();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(agentPositionHandler.GetPosition());
        sensor.AddObservation(player.transform.localPosition);
        sensor.AddObservation(Vector3.Distance(transform.localPosition, player.transform.localPosition));

        float playerInLight = IsPlayerInLight() ? 1f : 0f;
        float playerVisible = enemyVision.CanSeeTarget() ? 1f : 0f;
        float canHear = CanHearPlayer() ? 1f : 0f;

        sensor.AddObservation(canHear);
        sensor.AddObservation(playerInLight);
        sensor.AddObservation(playerVisible);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveAction = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f);
        float lookAction = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        movement.Move(moveAction, lookAction);

        float dist = Vector3.Distance(transform.localPosition, player.transform.localPosition);
        bool canSee = enemyVision.CanSeeTarget();
        bool isInLight = IsPlayerInLight();
        bool canHear = CanHearPlayer();

        // Fokus hanya saat player terlihat & dalam cahaya
        if (canSee && isInLight)
        {
            Debug.Log("Agent See Player");
            if (dist < 1.5f)
            {
                AddReward(1.0f);
                EndEpisode();
            }
            else
            {
                if (dist < previousDistance)
                {
                    AddReward(0.005f); // reward kecil saat makin dekat
                }
                else
                {
                    AddReward(-0.005f); // penalti saat menjauh
                }
            }
        }
        else if(canHear){
            Debug.Log("Agent Hear Player");
            if (dist < previousDistance)
                AddReward(0.0025f);
            else
                AddReward(-0.001f);
        }
        else
        {
            // Eksplorasi → reward netral (bisa ubah jadi 0.0001f jika perlu)
            AddReward(0.0001f);
        }

        previousDistance = dist;
        steps++;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var cont = actionsOut.ContinuousActions;
        cont[0] = Input.GetKey(KeyCode.W) ? 1f : 0f;
        cont[1] = Input.GetKey(KeyCode.A) ? 1f : Input.GetKey(KeyCode.D) ? -1f : 0f;
    }

    private void PositionManagement(){
        if (difficulty == 1.0f)
        {
            playerPos = lights[0].transform.localPosition;
            startPos = points[0].localPosition;
        }
        else if (difficulty == 2.0f)
        {
            playerPos = lights[Random.Range(0, lights.Length)].transform.localPosition;
            startPos = points[Random.Range(0,1)].localPosition;
        }
        else
        {
            playerPos = pointsWithoutLight[Random.Range(0, pointsWithoutLight.Length)].transform.localPosition;
            startPos = points[Random.Range(0,points.Length)].localPosition;
        }

        agentPositionHandler.SetPosition(startPos + new Vector3(Random.Range(-3, 3), Random.Range(0, 2), 0));

        playerPositionHandler.SetPosition(playerPos + new Vector3(Random.Range(1, 1), Random.Range(0, 1), 0));

        previousDistance = Vector3.Distance(agentPositionHandler.GetPosition(), playerPositionHandler.GetPosition());
    }

    private bool IsPlayerInLight()
    {
        if (player.TryGetComponent(out Visible visible))
        {
            return visible.LightLevel == 1;
        }
        return false;
    }

    private bool CanHearPlayer(){
        float distance = Vector3.Distance(transform.localPosition, player.transform.localPosition);

        if (playerAudioSource == null)
            return false;

        return playerAudioSource.isPlaying && distance <= hearingRadius && playerAudioSource.volume > 0.01f;
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
            // Warna radius pendengaran
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.25f); // Biru transparan
            Gizmos.DrawSphere(transform.localPosition, hearingRadius);
        }
    }

}

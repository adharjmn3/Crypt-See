using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.Rendering.Universal;
using Player.Stats;

public class ExploreAgents : Agent
{
    [SerializeField] GameObject player;
    [SerializeField] float moveSpeed;
    [SerializeField] float rotateSpeed;
    [SerializeField] Light2D[] lights;
    private Rigidbody2D rb;
    private Vector3 startPos;
    private float difficulty;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        startPos = transform.localPosition;
        difficulty = Academy.Instance.EnvironmentParameters.GetWithDefault("difficulty", 0f);
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = startPos + new Vector3(Random.Range(-3, 3), Random.Range(0, 2), 0);
        rb.velocity = Vector3.zero;
        
        Vector3 playerPos = lights[Random.Range(0, lights.Length)].transform.position;
        float maxOffset = Mathf.Lerp(1f, 6f, difficulty); // player moves further away at higher difficulty
        player.transform.localPosition = playerPos + new Vector3(Random.Range(-maxOffset, maxOffset), Random.Range(0, maxOffset), 0);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(player.transform.localPosition);
        sensor.AddObservation(Vector3.Distance(transform.localPosition, player.transform.localPosition));

        float playerNearLight = IsPlayerNearLight() ? 1f : 0f;
        sensor.AddObservation(playerNearLight);
    }

    private bool IsPlayerNearLight()
    {
        float lightLevel = player.GetComponent<Visible>().LightLevel;
        return lightLevel == 1;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveAction = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f);
        float lookAction = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        Vector2 move = transform.up * moveAction * moveSpeed * Time.deltaTime;
        rb.MovePosition(rb.position + move);

        rb.MoveRotation(rb.rotation + lookAction * rotateSpeed * Time.deltaTime);

        float dist = Vector3.Distance(transform.localPosition, player.transform.localPosition);

        // Reward if caught
        if (dist < 1.5f && IsPlayerNearLight())
        {
            SetReward(1.0f);
            EndEpisode();
        }

        // Small reward for exploring when player is not near light
        if (!IsPlayerNearLight())
        {
            AddReward(0.001f);
        }

        // Small penalty to encourage efficiency
        AddReward(-0.001f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var cont = actionsOut.ContinuousActions;
        cont[0] = Input.GetKey(KeyCode.W) ? 1f : 0f;
        cont[1] = Input.GetKey(KeyCode.A) ? 1f : Input.GetKey(KeyCode.D) ? -1f : 0f;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Wall")){
            SetReward(-0.5f);
            EndEpisode();
        }

        if(collision.gameObject.CompareTag("Player")){
            SetReward(1f);
            EndEpisode();
        }
    }
}

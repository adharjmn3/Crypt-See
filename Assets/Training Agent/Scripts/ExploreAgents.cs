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
    [SerializeField] LayerMask obstacleMask;
    [SerializeField] Transform[] points;

    private Rigidbody2D rb;
    private Vector3 startPos;
    private Vector3 playerPos;
    private float difficulty;
    private int steps;
    private float previousDistance;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        startPos =points[0].localPosition;
    }

    public override void OnEpisodeBegin()
    {
        if (steps >= MaxStep && difficulty >= 2)
        {
            AddReward(-1f);
        }

        steps = 0;
        difficulty = Academy.Instance.EnvironmentParameters.GetWithDefault("difficulty", 0f);

        if (difficulty == 1.0f)
        {
            playerPos = lights[0].transform.localPosition;
            startPos = points[0].localPosition;
        }
        else if (difficulty == 2.0f)
        {
            playerPos = lights[Random.Range(0, 1)].transform.localPosition;
            startPos = points[Random.Range(0,1)].localPosition;
        }
        else
        {
            playerPos = lights[Random.Range(0, lights.Length)].transform.localPosition;
            startPos = points[Random.Range(0,points.Length)].localPosition;
        }

        transform.localPosition = startPos + new Vector3(Random.Range(-3, 3), Random.Range(0, 2), 0);
        rb.velocity = Vector3.zero;

        player.transform.localPosition = playerPos + new Vector3(Random.Range(1, 1), Random.Range(0, 1), 0);
        previousDistance = Vector3.Distance(transform.localPosition, player.transform.localPosition);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(player.transform.localPosition);
        sensor.AddObservation(Vector3.Distance(transform.localPosition, player.transform.localPosition));

        float playerInLight = IsPlayerInLight() ? 1f : 0f;
        float playerVisible = CanSeePlayer() ? 1f : 0f;

        sensor.AddObservation(playerInLight);
        sensor.AddObservation(playerVisible);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveAction = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f);
        float lookAction = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        Vector2 move = transform.up * moveAction * moveSpeed * Time.deltaTime;
        rb.MovePosition(rb.position + move);
        rb.MoveRotation(rb.rotation + lookAction * rotateSpeed * Time.deltaTime);

        float dist = Vector3.Distance(transform.localPosition, player.transform.localPosition);
        bool canSee = CanSeePlayer();
        bool isInLight = IsPlayerInLight();

        // Fokus hanya saat player terlihat & dalam cahaya
        if (canSee && isInLight)
        {
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

    private bool IsPlayerInLight()
    {
        if (player.TryGetComponent(out Visible visible))
        {
            return visible.LightLevel == 1;
        }
        return false;
    }

    private bool CanSeePlayer()
    {
        Vector2 dir = player.transform.position - transform.position;
        float dist = dir.magnitude;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir.normalized, dist, obstacleMask);
        return hit.collider == null;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.5f);
            EndEpisode();
        }

        if (collision.gameObject.CompareTag("Player") && CanSeePlayer())
        {
            AddReward(1f);
            EndEpisode();
        }
    }
}

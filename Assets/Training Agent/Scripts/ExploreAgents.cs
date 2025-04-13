using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.Tilemaps;

public class ExploreAgents : Agent
{
    [SerializeField] Transform playerTransform;
    [SerializeField] AudioSource playerAudioSource;
    [SerializeField] float maxHearingDistance = 15f;
    [SerializeField] float moveSpeed;
    [SerializeField] float rotateSpeed;

    [SerializeField] Transform[] spawnPoints;
    private Rigidbody2D rb;
    private float previousDistance;

    public override void OnEpisodeBegin()
    {
        int index = Random.Range(0, spawnPoints.Length - 1);

        transform.localPosition = spawnPoints[index].localPosition;
        transform.Rotate(0, 0, Random.Range(1, 180));

        index = Random.Range(0, spawnPoints.Length - 1);
        playerTransform.localPosition = spawnPoints[index].localPosition;

        previousDistance = Vector3.Distance(playerTransform.localPosition, transform.localPosition);
    }

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(transform.rotation);
        sensor.AddObservation(playerTransform.localPosition);

        float dist = Vector3.Distance(transform.localPosition, playerTransform.localPosition);
        float normalizedDist = Mathf.Clamp01(dist / maxHearingDistance);
        bool canHear = CanHearFootStep();

        sensor.AddObservation(normalizedDist);
        sensor.AddObservation(canHear ? 1f : 0f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveAction = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f);
        float lookAction = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        Vector2 move = transform.up * moveAction * moveSpeed;
        rb.velocity = move;

        rb.MoveRotation(rb.rotation + lookAction * rotateSpeed * Time.deltaTime);

        float currentDistance = Vector3.Distance(transform.localPosition, playerTransform.localPosition);

        if(CanHearFootStep()){
            float reward = (previousDistance - currentDistance) * 0.2f;
            AddReward(reward);
            previousDistance = currentDistance;
        }

        if (Mathf.Abs(lookAction) < 0.1f && Mathf.Abs(moveAction) > 0.5f)
        {
            AddReward(0.01f);
        }

        float absRotation = Mathf.Abs(lookAction);
        AddReward(-absRotation * 0.0005f);
    }

    private bool CanHearFootStep()
    {
        float dist = Vector3.Distance(transform.localPosition, playerTransform.localPosition);
        return playerAudioSource.isPlaying && dist < maxHearingDistance;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var cont = actionsOut.ContinuousActions;
        cont[0] = Input.GetKey(KeyCode.S) ? -1f : Input.GetKey(KeyCode.W) ? 1f : 0f;
        cont[1] = Input.GetKey(KeyCode.A) ? 1f : Input.GetKey(KeyCode.D) ? -1f : 0f;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Wall")){
            AddReward(-1f);
            EndEpisode();
        }

        if(collision.gameObject.CompareTag("Player")){
            AddReward(2f);
            EndEpisode();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class WallDetectionAgent : Agent
{
    [Header("Agent Properties")]
    [SerializeField] float moveSpeed;
    [SerializeField] float rotateSpeed;
    [SerializeField] Transform playerPosition;

    [Header("Exploration Settings")]
    [SerializeField] Transform[] spawnPoints;

    public override void OnEpisodeBegin()
    {
        int randomIndex = Random.Range(0, spawnPoints.Length);
        transform.localPosition = spawnPoints[randomIndex].localPosition;
        transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));

        int randomPlayerIndex = Random.Range(0, spawnPoints.Length);
        playerPosition.localPosition = spawnPoints[randomPlayerIndex].localPosition;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(transform.rotation.eulerAngles.z);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int rotateAction = actions.DiscreteActions[0];
        int moveAction = actions.DiscreteActions[1];

        Rotate(rotateAction);
        Move(moveAction);

        AddReward(0.01f);
    }

    private void Rotate(int action)
    {
        if (action == 1) transform.Rotate(Vector3.forward * rotateSpeed * Time.deltaTime);
        else if (action == 2) transform.Rotate(Vector3.back * rotateSpeed * Time.deltaTime);
    }

    private void Move(int action)
    {
        if (action == 1) transform.position += transform.up * moveSpeed * Time.deltaTime;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.2f);
            EndEpisode();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player")){
            AddReward(5f);
            EndEpisode();
        }
    }
}

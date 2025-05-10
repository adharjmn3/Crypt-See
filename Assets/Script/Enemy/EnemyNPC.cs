using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class EnemyNPC : Agent
{
    [Header("Agent Settings")]
    [SerializeField] public float tensionMeter;
    [SerializeField] public float maxTensionMeter;
    [SerializeField] private float fillSpeed = 0.5f;
    [SerializeField] private float drainSpeed = 0.2f;

    private float lastTensionMeter = 0f;

    private EnemyVision enemyVision;
    private EnemyHearing enemyHearing;
    private EnemyMovement enemyMovement;

    private Transform playerTransform;

    Vector3 agentPos;
    Vector3 targetPos;

    public override void Initialize()
    {
        enemyMovement = GetComponent<EnemyMovement>();
        enemyHearing = GetComponent<EnemyHearing>();
        enemyVision = GetComponent<EnemyVision>();

        // Otomatis cari Player berdasarkan tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            enemyVision.SetTarget(playerObj);
        }
        else
        {
            Debug.LogWarning("Player dengan tag 'Player' tidak ditemukan!");
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        agentPos = transform.position;
        targetPos = playerTransform.position;

        bool canSee = enemyVision.CanSeeTarget(agentPos, targetPos);
        bool canHear = enemyHearing.CanHearPlayer(agentPos, targetPos);

        if (canSee || canHear)
        {
            float distance = Vector3.Distance(agentPos, targetPos);
            float proximityFactor = Mathf.Clamp01(1f - distance / 10f);
            float adjustedFillSpeed = fillSpeed * (0.5f + proximityFactor);
            tensionMeter = MathF.Min(maxTensionMeter, tensionMeter + adjustedFillSpeed * Time.deltaTime);

            if (distance < 3f)
            {
                tensionMeter = maxTensionMeter;
            }
        }
        else
        {
            tensionMeter = MathF.Max(0f, tensionMeter - drainSpeed * Time.deltaTime);
        }

        if (IsTensionMeterFull() && playerTransform != null)
        {
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        lastTensionMeter = tensionMeter;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (playerTransform == null)
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            return;
        }

        Vector3 agentPos = transform.position;
        Vector3 playerPos = playerTransform.position;

        sensor.AddObservation(agentPos);
        sensor.AddObservation(playerPos);
        sensor.AddObservation(transform.rotation.z);
        sensor.AddObservation(Vector2.Distance(playerPos, agentPos));
        sensor.AddObservation((playerPos - agentPos).normalized);

        float tensionFull = IsTensionMeterFull() ? 1f : 0f;
        float tensionChange = tensionMeter - lastTensionMeter;

        sensor.AddObservation(tensionChange);
        sensor.AddObservation(tensionFull);
        sensor.AddObservation(enemyVision.CanSeeTarget(agentPos,targetPos) ? 1f : 0f);
        sensor.AddObservation(enemyHearing.CanHearPlayer(agentPos, targetPos) ? 1f : 0f);

        Debug.Log(enemyVision.CanSeeTarget(agentPos, targetPos));
        Debug.Log(enemyHearing.CanHearPlayer(agentPos, playerPos));
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveAction = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f);
        float lookAction = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        if(IsTensionMeterFull()){
            if(moveAction < 0.2f && enemyVision.CanSeeTarget(agentPos, targetPos)){
                moveAction = 1f;
            }
            else if(!enemyVision.CanSeeTarget(agentPos, targetPos)){
                moveAction = 0f;
            }
        }

        if (moveAction < 0.2f && IsTensionMeterFull() && enemyVision.CanSeeTarget(agentPos, targetPos))
        {
            moveAction = 1f; // Paksa maju saat tension penuh
        }

        enemyMovement.Move(moveAction, lookAction);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var cont = actionsOut.ContinuousActions;
        cont[0] = Input.GetKey(KeyCode.W) ? 1f : 0f;
        cont[1] = Input.GetKey(KeyCode.A) ? 1f : Input.GetKey(KeyCode.D) ? -1f : 0f;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Wall") ||
            (collision.gameObject.CompareTag("Player") && enemyVision.CanSeeTarget(agentPos, targetPos) && IsTensionMeterFull()))
        {
            EndEpisode();
        }
    }

    public bool IsTensionMeterFull()
    {
        return tensionMeter >= maxTensionMeter;
    }
}

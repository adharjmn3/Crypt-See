using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAIFSM : MonoBehaviour
{
    public enum AIState
    {
        Idle,
        Moving,
        Turning
    }

    public AIState currentState = AIState.Moving; // Start by moving
    public float moveSpeed = 2.0f;
    public float rotationSpeed = 100.0f;
    public float wallCheckDistance = 1.0f;
    public LayerMask obstacleLayer; // Set this in the Inspector to define what is a wall

    private bool isTurning = false;
    private float turnAngle = 0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        switch (currentState)
        {
            case AIState.Idle:
                IdleState();
                break;
            case AIState.Moving:
                MovingState();
                break;
            case AIState.Turning:
                TurningState();
                break;
        }
    }

    void IdleState()
    {
        // For now, just transition to moving
        // You can add more complex logic here later
        currentState = AIState.Moving;
    }

    void MovingState()
    {
        // Check for obstacles in front
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, wallCheckDistance, obstacleLayer))
        {
            Debug.Log("Wall detected!");
            // Obstacle detected, switch to Turning state
            currentState = AIState.Turning;
            isTurning = false; // Reset turning flag
            return;
        }

        // Move forward
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }

    void TurningState()
    {
        if (!isTurning)
        {
            // Decide a random turn angle (e.g., 90 degrees left or right)
            // Or implement more sophisticated logic to find an empty space
            turnAngle = Random.Range(0, 2) == 0 ? 90f : -90f; 
            isTurning = true;
            Debug.Log($"Starting to turn by {turnAngle} degrees.");
        }

        // Rotate until the turn is complete
        float angleToRotate = rotationSpeed * Time.deltaTime;
        if (Mathf.Abs(turnAngle) < angleToRotate)
        {
            transform.Rotate(0, turnAngle, 0);
            turnAngle = 0;
        }
        else
        {
            float rotationDirection = Mathf.Sign(turnAngle);
            transform.Rotate(0, angleToRotate * rotationDirection, 0);
            turnAngle -= angleToRotate * rotationDirection;
        }


        if (turnAngle == 0)
        {
            Debug.Log("Finished turning.");
            isTurning = false;
            currentState = AIState.Moving; // Go back to moving
        }
    }

    // Visualize the raycast in the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * wallCheckDistance);
    }
}

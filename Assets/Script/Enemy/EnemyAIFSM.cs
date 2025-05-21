using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAIFSM : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2.0f;
    public float rotationSpeed = 3.0f;
    public float steeringStrength = 1.0f;
    public float raycastDistance = 2.0f;
    public float raycastSpread = 30f;
    public LayerMask obstacleLayer;

    [Header("Debug")]
    public bool showDebugRays = true;
    
    private Rigidbody2D rb;
    private Vector2 desiredDirection;
    private Vector2 currentVelocity = Vector2.zero;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("No Rigidbody2D found on this GameObject!");
            return;
        }
        
        // Configure rigidbody for 2D navigation
        rb.gravityScale = 0;  // No gravity for top-down movement
        rb.freezeRotation = true;  // We'll handle rotation ourselves
        
        // Initial desired direction is right (x-axis in 2D)
        desiredDirection = transform.right;
    }

    void FixedUpdate()
    {
        // Detect obstacles and adjust direction
        Vector2 steeringForce = CalculateSteeringForce();
        desiredDirection = (desiredDirection + steeringForce * steeringStrength).normalized;
        
        // Gradually rotate towards desired direction (2D rotation around Z-axis)
        float angle = Mathf.Atan2(desiredDirection.y, desiredDirection.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        
        // Move in the forward direction (which is right in 2D)
        Vector2 moveVector = (Vector2)transform.right * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + moveVector);
        
        // Store current velocity for steering calculations
        currentVelocity = moveVector / Time.fixedDeltaTime;
    }
    
    Vector2 CalculateSteeringForce()
    {
        Vector2 steeringForce = Vector2.zero;
        bool obstacleDetected = false;
        
        // Cast rays in an arc to detect obstacles
        for (int i = -2; i <= 2; i++)
        {
            float angle = i * raycastSpread / 2;
            Vector2 rayDirection = RotateVector2(transform.right, angle);
            
            // Draw debug rays if enabled
            if (showDebugRays)
            {
                Debug.DrawRay(transform.position, rayDirection * raycastDistance, 
                    Color.yellow, Time.fixedDeltaTime);
            }
            
            // Check for obstacles with 2D raycast
            RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection, raycastDistance, obstacleLayer);
            if (hit.collider != null)
            {
                // Calculate avoidance force - stronger when directly ahead and close
                float weight = 1.0f - Mathf.Abs(i) / 3.0f; // Center ray has highest weight
                
                // Create perpendicular avoidance direction
                Vector2 normal = hit.normal;
                Vector2 avoidanceDirection = new Vector2(normal.y, -normal.x);
                
                // Choose the direction that's closer to our current heading
                if (Vector2.Dot(avoidanceDirection, (Vector2)transform.right) < 0)
                    avoidanceDirection = -avoidanceDirection;
                
                // Weighted avoidance force based on distance and angle
                float distanceFactor = 1.0f - (hit.distance / raycastDistance);
                steeringForce += avoidanceDirection * weight * distanceFactor * 2.0f;
                
                obstacleDetected = true;
                
                if (showDebugRays)
                {
                    Debug.DrawRay(hit.point, hit.normal, Color.red, Time.fixedDeltaTime);
                }
            }
        }
        
        // If no obstacle detected, occasionally choose a new random direction
        if (!obstacleDetected && Random.value < 0.005f)
        {
            float randomAngle = Random.Range(-45f, 45f);
            steeringForce = RotateVector2(transform.right, randomAngle);
        }
        
        return steeringForce.normalized;
    }
    
    // Helper method to rotate a 2D vector by specified degrees
    Vector2 RotateVector2(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        
        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }
    
    // Visualize the steering in the editor
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, (Vector3)desiredDirection * 2);
        
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.right * 1.5f);
    }
}

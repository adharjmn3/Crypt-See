using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyState
{
    Roaming,    // Patrolling, no player detection
    Aware,      // Suspicious, investigating potential player sighting
    Combat      // Player fully detected, actively engaging
}

public class EnemyAIFSM : MonoBehaviour
{
    [Header("State Management")]
    public EnemyState currentState = EnemyState.Roaming;
    
    [Header("Detection Settings")]
    public float sightRange = 10f;           // How far enemy can see
    public float sightAngle = 60f;           // Vision cone angle (degrees)
    public float hearingRange = 5f;          // How far enemy can hear player
    public float timeToForgetPlayer = 5f;    // Seconds until enemy forgets player
    public LayerMask playerLayer;            // Layer for player detection
    public LayerMask obstacleLayer;          // Obstacles that block vision
    
    [Header("Investigation Settings")]
    public float investigationTime = 3f;     // How long to search in Aware state
    public float investigationSpeed = 1.5f;  // Move speed while investigating
    
    [Header("Movement Settings")]
    public float roamingSpeed = 2.0f;
    public float combatSpeed = 3.0f;
    public float rotationSpeed = 3.0f;
    public float raycastDistance = 2.0f;
    public float raycastSpread = 30f;
    public float obstacleAvoidanceThreshold = 0.5f;
    
    [Header("Random Movement")]
    public float randomDirectionChangeInterval = 3.0f;
    public float randomDirectionChangeChance = 0.3f;
    public float randomTurnAngleMin = 20f;
    public float randomTurnAngleMax = 90f;
    
    [Header("Combat Settings")]
    public float chaseSpeedMultiplier = 1.5f;    // How much faster the enemy moves when chasing
    public float shootingCooldown = 0.5f;        // Time between enabling/disabling shooting while in combat
    
    [Header("Debug")]
    public bool showDebugRays = true;
    public Color roamingColor = Color.green;
    public Color awareColor = Color.yellow;
    public Color combatColor = Color.red;
    
    // Private state fields
    private Transform playerTransform;
    private Vector3 lastKnownPlayerPosition;
    private float currentStateTime = 0f;
    private float lastSeenPlayerTime = 0f;
    private bool hasPlayerBeenSeen = false;
    
    // Roaming state fields
    private Rigidbody2D rb;
    private bool isTurningAround = false;
    private bool isChangingDirection = false;
    private float turnAroundTargetAngle = 0f;
    private float lastDirectionChangeTime = 0f;
    
    // Investigation state fields
    private Vector3 investigationPoint;
    private bool isInvestigating = false;
    
    // Combat state fields
    private EnemyShoot shootComponent;
    private EnemyStats statsComponent;
    private bool isPlayerInSight = false;
    private float lastShootToggleTime = 0f;      // Track when we last toggled shooting
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        shootComponent = GetComponent<EnemyShoot>();
        statsComponent = GetComponent<EnemyStats>();
        
        if (rb == null)
        {
            Debug.LogError("No Rigidbody2D found on this GameObject!");
            return;
        }
        
        if (shootComponent == null) 
        {
            Debug.LogWarning("No EnemyShoot component found. Enemy won't be able to attack!");
        }
        
        if (statsComponent == null)
        {
            Debug.LogWarning("No EnemyStats component found. Using default stats!");
        }
        
        // Configure rigidbody for 2D navigation
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        
        // Initialize last direction change time with an offset
        lastDirectionChangeTime = Time.time;
        
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }
    
    void Update()
    {
        // Update state timer
        currentStateTime += Time.deltaTime;
        
        // Check for player visibility
        CheckForPlayer();
        
        // State transitions based on player detection
        UpdateState();
        
        // Debug visualization
        if (showDebugRays)
        {
            DrawStateDebug();
        }
    }
    
    void FixedUpdate()
    {
        // Execute current state behavior
        switch (currentState)
        {
            case EnemyState.Roaming:
                ExecuteRoamingState();
                break;
            case EnemyState.Aware:
                ExecuteAwareState();
                break;
            case EnemyState.Combat:
                ExecuteCombatState();
                break;
        }
    }
    
    #region State Management
    
    void UpdateState()
    {
        // Check for state transitions
        switch (currentState)
        {
            case EnemyState.Roaming:
                // Transition to Aware if player is detected but not visible
                if (hasPlayerBeenSeen && !isPlayerInSight && Time.time - lastSeenPlayerTime < timeToForgetPlayer)
                {
                    TransitionToState(EnemyState.Aware);
                }
                // Transition to Combat if player is currently visible
                else if (isPlayerInSight)
                {
                    TransitionToState(EnemyState.Combat);
                }
                break;
                
            case EnemyState.Aware:
                // Transition to Roaming if player has been forgotten
                if (Time.time - lastSeenPlayerTime > timeToForgetPlayer)
                {
                    TransitionToState(EnemyState.Roaming);
                }
                // Transition to Combat if player becomes visible again
                else if (isPlayerInSight)
                {
                    TransitionToState(EnemyState.Combat);
                }
                break;
                
            case EnemyState.Combat:
                // Transition to Aware if player is no longer visible
                if (!isPlayerInSight && Time.time - lastSeenPlayerTime > 0.5f)
                {
                    TransitionToState(EnemyState.Aware);
                }
                break;
        }
    }
    
    void TransitionToState(EnemyState newState)
    {
        // Don't transition to the same state
        if (newState == currentState) return;
        
        // Exit current state
        switch (currentState)
        {
            case EnemyState.Roaming:
                // Cancel any turns in progress
                isTurningAround = false;
                isChangingDirection = false;
                break;
                
            case EnemyState.Aware:
                // Reset investigation
                isInvestigating = false;
                break;
                
            case EnemyState.Combat:
                // Disable shooting when leaving combat state
                if (shootComponent != null)
                {
                    shootComponent.enabled = false;
                }
                break;
        }
        
        // Enter new state
        switch (newState)
        {
            case EnemyState.Roaming:
                // Reset player memory
                hasPlayerBeenSeen = false;
                break;
                
            case EnemyState.Aware:
                // Set investigation point to last known player position
                investigationPoint = lastKnownPlayerPosition;
                isInvestigating = true;
                currentStateTime = 0f;
                break;
                
            case EnemyState.Combat:
                // Initialize combat variables
                lastShootToggleTime = 0f; // Reset shooting toggle timer
                break;
        }
        
        // Set new state
        currentState = newState;
        Debug.Log($"{gameObject.name} transitioned to {newState} state");
    }
    
    #endregion
    
    #region Player Detection
    
    void CheckForPlayer()
    {
        if (playerTransform == null) return;
        
        // Check direct line of sight to player
        isPlayerInSight = false;
        
        // Calculate distance to player
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        
        // Only check if within maximum detection range
        if (distanceToPlayer <= sightRange)
        {
            // Get direction to player
            Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
            
            // Check if player is within sight angle
            float angleToPlayer = Vector2.Angle(transform.up, directionToPlayer);
            
            if (angleToPlayer <= sightAngle / 2f)
            {
                // Cast a ray to check for obstacles between enemy and player
                RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleLayer | playerLayer);
                
                // Draw debug ray
                if (showDebugRays)
                {
                    Debug.DrawRay(transform.position, directionToPlayer * distanceToPlayer, Color.magenta);
                }
                
                // If we hit something and it's the player, we have line of sight
                if (hit.collider != null && hit.collider.gameObject.layer == Mathf.Log(playerLayer.value, 2))
                {
                    isPlayerInSight = true;
                    lastKnownPlayerPosition = playerTransform.position;
                    lastSeenPlayerTime = Time.time;
                    hasPlayerBeenSeen = true;
                }
            }
        }
        
        // Check for hearing range (simplified - just checks distance)
        if (!isPlayerInSight && distanceToPlayer <= hearingRange)
        {
            // Player is close enough to be heard
            if (!hasPlayerBeenSeen)
            {
                // First time hearing the player
                lastKnownPlayerPosition = playerTransform.position;
                hasPlayerBeenSeen = true;
                lastSeenPlayerTime = Time.time;
            }
        }
    }
    
    #endregion
    
    #region State Execution
    
    void ExecuteRoamingState()
    {
        // Use the existing roaming logic
        
        // Priority 1: Turn around if obstacle ahead (highest priority)
        if (isTurningAround)
        {
            HandleTurningAround();
            return;
        }
        
        // Priority 2: Check if there's a clear path forward
        if (!HasClearPath())
        {
            // No clear path - initiate turn around
            StartTurnAround();
            return;
        }
        
        // Priority 3: Random direction changes (lowest priority)
        if (!isChangingDirection)
        {
            CheckForRandomDirectionChange();
        }
        
        // Handle random direction change if active
        if (isChangingDirection)
        {
            HandleDirectionChange();
            // Still move while doing small direction changes
        }
        
        // Set appropriate speed for roaming
        float currentSpeed = roamingSpeed;
        
        // Always move forward in the direction we're facing (up in 2D)
        Vector2 moveVector = (Vector2)transform.up * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + moveVector);
    }
    
    void ExecuteAwareState()
    {
        // If investigating a location
        if (isInvestigating)
        {
            // Calculate direction to investigation point
            Vector2 directionToPoint = (investigationPoint - transform.position).normalized;
            
            // Calculate angle to turn towards investigation point
            float targetAngle = Mathf.Atan2(directionToPoint.y, directionToPoint.x) * Mathf.Rad2Deg - 90f; // -90 to convert from right = 0 to up = 0
            
            // Smoothly rotate towards the investigation point
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.Euler(0, 0, targetAngle),
                rotationSpeed * Time.fixedDeltaTime * 100f
            );
            
            // Move toward investigation point
            Vector2 moveVector = (Vector2)transform.up * investigationSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + moveVector);
            
            // Check if we've reached the investigation point
            float distanceToPoint = Vector2.Distance(transform.position, investigationPoint);
            if (distanceToPoint < 0.5f || currentStateTime > investigationTime)
            {
                // Look around at the investigation point (create a random direction)
                isInvestigating = false;
                StartRandomDirectionChange();
            }
        }
        else
        {
            // After reaching investigation point, just look around
            if (isChangingDirection)
            {
                HandleDirectionChange();
            }
            else
            {
                StartRandomDirectionChange();
            }
            
            // Transition back to roaming if we've been in aware state too long
            if (currentStateTime > investigationTime * 2f)
            {
                TransitionToState(EnemyState.Roaming);
            }
        }
    }
    
    void ExecuteCombatState()
    {
        // Target is the player
        if (playerTransform != null)
        {
            // Calculate direction to player
            Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
            
            // Calculate angle to face player
            float targetAngle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg - 90f;
            
            // Rotate towards player - increased rotation speed to turn faster when chasing
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.Euler(0, 0, targetAngle),
                rotationSpeed * Time.fixedDeltaTime * 150f // Increased from 100f for faster turning
            );
            
            // Check if we have a clear path to player
            RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, 
                Vector2.Distance(transform.position, playerTransform.position), obstacleLayer);
            
            // Calculate distance to player
            float currentDistance = Vector2.Distance(transform.position, playerTransform.position);
            
            // Get optimal distance for shooting
            float optimalDistance = shootComponent != null ? shootComponent.shootingRange * 0.7f : 5f;
            
            // SHOOTING LOGIC
            if (shootComponent != null)
            {
                // Only enable shooting if we have direct line of sight
                if (Time.time > lastShootToggleTime + shootingCooldown)
                {
                    // Toggle shooting state
                    if (hit.collider == null && currentDistance < shootComponent.shootingRange)
                    {
                        // Enable shooting when in range with clear line of sight
                        shootComponent.enabled = true;
                    }
                    else
                    {
                        // Disable shooting when path is blocked or out of range
                        shootComponent.enabled = false;
                    }
                    
                    // Update toggle time
                    lastShootToggleTime = Time.time;
                }
            }
            
            // MOVEMENT LOGIC - PRIORITIZE GETTING TO PLAYER
            
            // Calculate effective chase speed - always use max multiplier when actively chasing
            float effectiveSpeed = combatSpeed * chaseSpeedMultiplier;
            
            // Direct path to player is clear
            if (hit.collider == null)
            {
                // Move directly to player, only stop at very close range
                if (currentDistance > 0.5f) // Reduced from 1.0f to get closer
                {
                    // Always move at full speed when chasing
                    Vector2 moveVector = (Vector2)transform.up * effectiveSpeed * Time.fixedDeltaTime;
                    rb.MovePosition(rb.position + moveVector);
                    
                    if (showDebugRays)
                    {
                        Debug.DrawRay(transform.position, transform.up * 2f, Color.red);
                    }
                }
            }
            else
            {
                // Path to player is blocked - try to find a way around
                
                // Cast more rays to find a path (increased from 45° to 60° for wider search)
                RaycastHit2D leftHit = Physics2D.Raycast(transform.position, 
                    RotateVector2(directionToPlayer, -60f), raycastDistance * 1.5f, obstacleLayer);
                RaycastHit2D rightHit = Physics2D.Raycast(transform.position, 
                    RotateVector2(directionToPlayer, 60f), raycastDistance * 1.5f, obstacleLayer);
                
                // Also try more extreme angles if needed
                RaycastHit2D farLeftHit = Physics2D.Raycast(transform.position, 
                    RotateVector2(directionToPlayer, -90f), raycastDistance, obstacleLayer);
                RaycastHit2D farRightHit = Physics2D.Raycast(transform.position, 
                    RotateVector2(directionToPlayer, 90f), raycastDistance, obstacleLayer);
                
                // Choose best direction to move:
                // 1. First preference: standard 60° rays
                // 2. Second preference: extreme 90° rays
                // 3. Last resort: back up
                
                bool foundPath = false;
                Vector2 moveDir = Vector2.zero;
                float moveSpeed = effectiveSpeed * 0.8f; // Slightly reduced speed for navigation
                
                // Try primary directions first (60° rays)
                if (leftHit.collider == null || (rightHit.collider != null && leftHit.distance > rightHit.distance))
                {
                    // Move left
                    moveDir = RotateVector2(transform.up, -30f); // Less extreme turning for smoother path
                    foundPath = true;
                }
                else if (rightHit.collider == null || (leftHit.collider != null && rightHit.distance > leftHit.distance))
                {
                    // Move right
                    moveDir = RotateVector2(transform.up, 30f); // Less extreme turning for smoother path
                    foundPath = true;
                }
                
                // If no path found, try more extreme angles
                if (!foundPath)
                {
                    if (farLeftHit.collider == null)
                    {
                        moveDir = RotateVector2(transform.up, -45f);
                        foundPath = true;
                    }
                    else if (farRightHit.collider == null)
                    {
                        moveDir = RotateVector2(transform.up, 45f);
                        foundPath = true;
                    }
                    else
                    {
                        // All directions blocked, back up to reposition
                        moveDir = -transform.up;
                        moveSpeed = effectiveSpeed * 0.5f; // Half speed when backing up
                    }
                }
                
                // Apply the movement
                rb.MovePosition(rb.position + moveDir * moveSpeed * Time.fixedDeltaTime);
                
                // Visual debugging
                if (showDebugRays)
                {
                    Debug.DrawRay(transform.position, RotateVector2(directionToPlayer, -60f) * raycastDistance * 1.5f, Color.cyan);
                    Debug.DrawRay(transform.position, RotateVector2(directionToPlayer, 60f) * raycastDistance * 1.5f, Color.cyan);
                    Debug.DrawRay(transform.position, RotateVector2(directionToPlayer, -90f) * raycastDistance, Color.blue);
                    Debug.DrawRay(transform.position, RotateVector2(directionToPlayer, 90f) * raycastDistance, Color.blue);
                    Debug.DrawRay(transform.position, moveDir * 1.5f, Color.green);
                }
            }
        }
        else
        {
            // No player found, revert to aware state
            TransitionToState(EnemyState.Aware);
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    void HandleTurningAround()
    {
        // Continue turning until we reach the target angle
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.Euler(0, 0, turnAroundTargetAngle),
            rotationSpeed * Time.fixedDeltaTime * 100f
        );
        
        // Check if we've completed the turn
        if (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.z, turnAroundTargetAngle)) < 5f)
        {
            isTurningAround = false;
            lastDirectionChangeTime = Time.time + randomDirectionChangeInterval; // Delay next random change
        }
    }
    
    void HandleDirectionChange()
    {
        // Continue turning until we reach the target angle
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.Euler(0, 0, turnAroundTargetAngle),
            rotationSpeed * Time.fixedDeltaTime * 50f  // Slower rotation for random changes
        );
        
        // Check if we've completed the turn
        if (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.z, turnAroundTargetAngle)) < 2f)
        {
            isChangingDirection = false;
            lastDirectionChangeTime = Time.time + randomDirectionChangeInterval;
        }
    }
    
    void CheckForRandomDirectionChange()
    {
        // Check if it's time to consider a direction change
        if (Time.time > lastDirectionChangeTime + randomDirectionChangeInterval)
        {
            if (Random.value < randomDirectionChangeChance)
            {
                StartRandomDirectionChange();
            }
            else
            {
                // Reset timer even if we didn't change direction
                lastDirectionChangeTime = Time.time;
            }
        }
    }
    
    void StartRandomDirectionChange()
    {
        isChangingDirection = true;
        
        // Pick a random direction (left or right)
        float randomAngle = Random.Range(randomTurnAngleMin, randomTurnAngleMax);
        if (Random.value < 0.5f) randomAngle = -randomAngle;
        
        // Calculate new target angle
        turnAroundTargetAngle = transform.eulerAngles.z + randomAngle;
        
        if (showDebugRays)
        {
            Debug.Log("Random direction change to angle: " + turnAroundTargetAngle);
        }
    }
    
    bool HasClearPath()
    {
        bool hasClearDirection = false;
        
        // Cast rays in an arc to detect obstacles
        for (int i = -1; i <= 1; i++)
        {
            float angle = i * raycastSpread;
            Vector2 rayDirection = RotateVector2(transform.up, angle);
            
            // Draw debug rays
            if (showDebugRays)
            {
                Debug.DrawRay(transform.position, rayDirection * raycastDistance, 
                    Color.yellow, Time.fixedDeltaTime);
            }
            
            // Check for obstacles with 2D raycast
            RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection, raycastDistance, obstacleLayer);
            
            // If center ray is clear enough, we have a path
            if (i == 0 && (hit.collider == null || hit.distance > obstacleAvoidanceThreshold))
            {
                hasClearDirection = true;
            }
            
            // Draw hit points if we hit something
            if (hit.collider != null && showDebugRays)
            {
                Debug.DrawRay(hit.point, hit.normal, Color.red, Time.fixedDeltaTime);
            }
        }
        
        return hasClearDirection;
    }
    
    void StartTurnAround()
    {
        isTurningAround = true;
        isChangingDirection = false; // Cancel any random direction change
        
        // Calculate a 180 degree turn from current rotation
        turnAroundTargetAngle = transform.eulerAngles.z + 180f;
        
        if (showDebugRays)
        {
            Debug.Log("No clear path - turning around to angle: " + turnAroundTargetAngle);
        }
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
    
    void DrawStateDebug()
    {
        // Draw vision cone
        int segments = 20;
        float angleStep = sightAngle / segments;
        
        Color stateColor;
        switch (currentState)
        {
            case EnemyState.Aware: stateColor = awareColor; break;
            case EnemyState.Combat: stateColor = combatColor; break;
            default: stateColor = roamingColor; break;
        }
        
        Vector3 previousPoint = transform.position;
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = -sightAngle / 2 + angleStep * i;
            Vector2 direction = RotateVector2(transform.up, angle).normalized;
            
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, sightRange, obstacleLayer);
            Vector3 rayEnd;
            
            if (hit.collider != null)
                rayEnd = hit.point;
            else
                rayEnd = transform.position + (Vector3)(direction * sightRange);
                
            Debug.DrawLine(previousPoint, rayEnd, stateColor);
            previousPoint = rayEnd;
        }
        
        // Draw line back to origin
        Debug.DrawLine(previousPoint, transform.position, stateColor);
        
        // Draw hearing range
        if (currentState == EnemyState.Roaming)
        {
            DebugExtension.DrawCircle(transform.position, Vector3.forward, Color.blue, hearingRange);
        }
        
        // Draw target position or investigation point
        if (currentState == EnemyState.Aware && isInvestigating)
        {
            Debug.DrawLine(transform.position, investigationPoint, Color.yellow);
            DebugExtension.DrawPoint(investigationPoint, Color.yellow, 0.5f);
        }
        else if (currentState == EnemyState.Combat && playerTransform != null)
        {
            Debug.DrawLine(transform.position, playerTransform.position, Color.red);
        }
    }
    
    #endregion
    
    // Visualize the movement direction in the editor
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.up * 1.5f);
        
        if (isTurningAround || isChangingDirection)
        {
            Gizmos.color = isChangingDirection ? Color.yellow : Color.red;
            Vector2 targetDir = RotateVector2(Vector2.up, turnAroundTargetAngle);
            Gizmos.DrawRay(transform.position, targetDir * 1.5f);
        }
    }
}

// Simple extension class for debug drawing
public static class DebugExtension
{
    public static void DrawCircle(Vector3 position, Vector3 up, Color color, float radius, float duration = 0)
    {
        Vector3 _up = up.normalized * radius;
        Vector3 _forward = Vector3.Slerp(_up, -_up, 0.5f);
        Vector3 _right = Vector3.Cross(_up, _forward).normalized * radius;
        
        Matrix4x4 matrix = new Matrix4x4();
        matrix[0] = _right.x;
        matrix[1] = _right.y;
        matrix[2] = _right.z;
        
        matrix[4] = _up.x;
        matrix[5] = _up.y;
        matrix[6] = _up.z;
        
        matrix[8] = _forward.x;
        matrix[9] = _forward.y;
        matrix[10] = _forward.z;
        
        Vector3 _lastPoint = position + matrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(0), 0, Mathf.Sin(0)));
        Vector3 _nextPoint = Vector3.zero;
        
        color = (color == default(Color)) ? Color.white : color;
        
        for (var i = 0; i < 91; i++)
        {
            _nextPoint.x = Mathf.Cos((i * 4) * Mathf.Deg2Rad);
            _nextPoint.z = Mathf.Sin((i * 4) * Mathf.Deg2Rad);
            _nextPoint.y = 0;
            
            _nextPoint = position + matrix.MultiplyPoint3x4(_nextPoint);
            
            Debug.DrawLine(_lastPoint, _nextPoint, color, duration);
            _lastPoint = _nextPoint;
        }
    }
    
    public static void DrawPoint(Vector3 position, Color color, float scale = 1.0f)
    {
        Debug.DrawLine(position + Vector3.up * scale, position - Vector3.up * scale, color);
        Debug.DrawLine(position + Vector3.right * scale, position - Vector3.right * scale, color);
        Debug.DrawLine(position + Vector3.forward * scale, position - Vector3.forward * scale, color);
    }
}

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
    public float sightRange = 10f;
    public float sightAngle = 60f;
    public float hearingRange = 5f;
    public float timeToForgetPlayer = 5f;
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;

    [Header("Investigation Settings")]
    public float investigationTime = 3f;
    public float investigationSpeed = 1.5f;

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
    public float chaseSpeedMultiplier = 1.5f;
    public float shootingCooldown = 0.5f;

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
    private float lastShootToggleTime = 0f;

    // Stuck detection
    [Header("Stuck Detection")]
    public float stuckDetectionTime = 2.0f;
    public float stuckDetectionThreshold = 0.1f;
    public float unstuckTurnAngle = 135f;

    private Vector3 lastPositionCheck;
    private float timeSinceLastMovement = 0f;
    private bool isStuck = false;
    private bool isAttemptingUnstuck = false;
    private int unstuckAttempts = 0;
    private const int MAX_UNSTUCK_ATTEMPTS = 3;

    // Cached player layer index for fast comparison
    private int playerLayerIndex;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        shootComponent = GetComponent<EnemyShoot>();
        statsComponent = GetComponent<EnemyStats>();

        lastPositionCheck = transform.position;
        timeSinceLastMovement = 0f;

        if (rb == null)
        {
            Debug.LogError("No Rigidbody2D found on this GameObject!");
            enabled = false;
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

        rb.gravityScale = 0;
        rb.freezeRotation = true;
        lastDirectionChangeTime = Time.time;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // Cache player layer index for fast comparison
        playerLayerIndex = playerLayer.value != 0 ? Mathf.RoundToInt(Mathf.Log(playerLayer.value, 2)) : 0;
    }

    void Update()
    {
        currentStateTime += Time.deltaTime;
        CheckForPlayer();
        UpdateState();
        CheckIfStuck();

        if (showDebugRays)
        {
            DrawStateDebug();
        }
    }

    void FixedUpdate()
    {
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
        switch (currentState)
        {
            case EnemyState.Roaming:
                if (hasPlayerBeenSeen && !isPlayerInSight && Time.time - lastSeenPlayerTime < timeToForgetPlayer)
                {
                    TransitionToState(EnemyState.Aware);
                }
                else if (isPlayerInSight)
                {
                    TransitionToState(EnemyState.Combat);
                }
                break;

            case EnemyState.Aware:
                if (Time.time - lastSeenPlayerTime > timeToForgetPlayer)
                {
                    TransitionToState(EnemyState.Roaming);
                }
                else if (isPlayerInSight)
                {
                    TransitionToState(EnemyState.Combat);
                }
                break;

            case EnemyState.Combat:
                if (!isPlayerInSight && Time.time - lastSeenPlayerTime > 0.5f)
                {
                    TransitionToState(EnemyState.Aware);
                }
                break;
        }
    }

    void TransitionToState(EnemyState newState)
    {
        if (newState == currentState) return;

        switch (currentState)
        {
            case EnemyState.Roaming:
                isTurningAround = false;
                isChangingDirection = false;
                break;
            case EnemyState.Aware:
                isInvestigating = false;
                break;
            case EnemyState.Combat:
                if (shootComponent != null)
                {
                    shootComponent.enabled = false;
                }
                break;
        }

        switch (newState)
        {
            case EnemyState.Roaming:
                hasPlayerBeenSeen = false;
                break;
            case EnemyState.Aware:
                investigationPoint = lastKnownPlayerPosition;
                isInvestigating = true;
                currentStateTime = 0f;
                break;
            case EnemyState.Combat:
                lastShootToggleTime = 0f;
                break;
        }

        currentState = newState;
        Debug.Log($"{gameObject.name} transitioned to {newState} state");
    }

    #endregion

    #region Player Detection

    void CheckForPlayer()
    {
        if (playerTransform == null) return;

        isPlayerInSight = false;
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= sightRange)
        {
            Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
            float angleToPlayer = Vector2.Angle(transform.up, directionToPlayer);

            if (angleToPlayer <= sightAngle / 2f)
            {
                bool hasLineOfSight = CheckLineOfSightToPlayer(directionToPlayer, distanceToPlayer);

                if (hasLineOfSight)
                {
                    isPlayerInSight = true;
                    lastKnownPlayerPosition = playerTransform.position;
                    lastSeenPlayerTime = Time.time;
                    hasPlayerBeenSeen = true;
                }
            }
        }

        if (!isPlayerInSight && distanceToPlayer <= hearingRange)
        {
            if (!hasPlayerBeenSeen)
            {
                lastKnownPlayerPosition = playerTransform.position;
                hasPlayerBeenSeen = true;
                lastSeenPlayerTime = Time.time;
            }
        }
    }

    bool CheckLineOfSightToPlayer(Vector2 directionToPlayer, float distance)
    {
        // Center raycast
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, distance, obstacleLayer | playerLayer);
        if (hit.collider != null && hit.collider.gameObject.layer == playerLayerIndex)
        {
            if (showDebugRays)
                Debug.DrawRay(transform.position, directionToPlayer * distance, Color.green);
            return true;
        }

        // Only do extra raycasts if center ray didn't hit player
        Vector2 perpendicularDir = new Vector2(-directionToPlayer.y, directionToPlayer.x).normalized;
        float rayOffset = 0.3f;

        // Left offset
        Vector2 leftStart = (Vector2)transform.position + perpendicularDir * rayOffset;
        hit = Physics2D.Raycast(leftStart, directionToPlayer, distance, obstacleLayer | playerLayer);
        if (hit.collider != null && hit.collider.gameObject.layer == playerLayerIndex)
        {
            if (showDebugRays)
                Debug.DrawRay(leftStart, directionToPlayer * distance, Color.green);
            return true;
        }

        // Right offset
        Vector2 rightStart = (Vector2)transform.position - perpendicularDir * rayOffset;
        hit = Physics2D.Raycast(rightStart, directionToPlayer, distance, obstacleLayer | playerLayer);
        if (hit.collider != null && hit.collider.gameObject.layer == playerLayerIndex)
        {
            if (showDebugRays)
                Debug.DrawRay(rightStart, directionToPlayer * distance, Color.green);
            return true;
        }

        if (showDebugRays)
            Debug.DrawRay(transform.position, directionToPlayer * distance, Color.red);
        return false;
    }

    #endregion

    #region State Execution

    void ExecuteRoamingState()
    {
        if (isTurningAround)
        {
            HandleTurningAround();
            return;
        }

        if (!HasClearPath())
        {
            StartTurnAround();
            return;
        }

        if (!isChangingDirection)
        {
            CheckForRandomDirectionChange();
        }

        if (isChangingDirection)
        {
            HandleDirectionChange();
        }

        float currentSpeed = roamingSpeed;
        Vector2 moveVector = (Vector2)transform.up * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + moveVector);
    }

    void ExecuteAwareState()
    {
        if (isInvestigating)
        {
            Vector2 directionToPoint = (investigationPoint - transform.position).normalized;
            float targetAngle = Mathf.Atan2(directionToPoint.y, directionToPoint.x) * Mathf.Rad2Deg - 90f;

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.Euler(0, 0, targetAngle),
                rotationSpeed * Time.fixedDeltaTime * 100f
            );

            Vector2 moveVector = (Vector2)transform.up * investigationSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + moveVector);

            float distanceToPoint = Vector2.Distance(transform.position, investigationPoint);
            if (distanceToPoint < 0.5f || currentStateTime > investigationTime)
            {
                isInvestigating = false;
                StartRandomDirectionChange();
            }
        }
        else
        {
            if (isChangingDirection)
            {
                HandleDirectionChange();
            }
            else
            {
                StartRandomDirectionChange();
            }

            if (currentStateTime > investigationTime * 2f)
            {
                TransitionToState(EnemyState.Roaming);
            }
        }
    }

    void ExecuteCombatState()
    {
        if (playerTransform != null)
        {
            Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
            float targetAngle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg - 90f;

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.Euler(0, 0, targetAngle),
                rotationSpeed * Time.fixedDeltaTime * 150f
            );

            RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer,
                Vector2.Distance(transform.position, playerTransform.position), obstacleLayer);

            float currentDistance = Vector2.Distance(transform.position, playerTransform.position);
            float optimalDistance = shootComponent != null ? shootComponent.shootingRange * 0.7f : 5f;

            if (shootComponent != null)
            {
                if (Time.time > lastShootToggleTime + shootingCooldown)
                {
                    if (hit.collider == null && currentDistance < shootComponent.shootingRange)
                    {
                        shootComponent.enabled = true;
                    }
                    else
                    {
                        shootComponent.enabled = false;
                    }
                    lastShootToggleTime = Time.time;
                }
            }

            float effectiveSpeed = combatSpeed * chaseSpeedMultiplier;

            if (hit.collider == null)
            {
                if (currentDistance > 0.5f)
                {
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
                RaycastHit2D leftHit = Physics2D.Raycast(transform.position,
                    RotateVector2(directionToPlayer, -60f), raycastDistance * 1.5f, obstacleLayer);
                RaycastHit2D rightHit = Physics2D.Raycast(transform.position,
                    RotateVector2(directionToPlayer, 60f), raycastDistance * 1.5f, obstacleLayer);
                RaycastHit2D farLeftHit = Physics2D.Raycast(transform.position,
                    RotateVector2(directionToPlayer, -90f), raycastDistance, obstacleLayer);
                RaycastHit2D farRightHit = Physics2D.Raycast(transform.position,
                    RotateVector2(directionToPlayer, 90f), raycastDistance, obstacleLayer);

                bool foundPath = false;
                Vector2 moveDir = Vector2.zero;
                float moveSpeed = effectiveSpeed * 0.8f;

                if (leftHit.collider == null || (rightHit.collider != null && leftHit.distance > rightHit.distance))
                {
                    moveDir = RotateVector2(transform.up, -30f);
                    foundPath = true;
                }
                else if (rightHit.collider == null || (leftHit.collider != null && rightHit.distance > leftHit.distance))
                {
                    moveDir = RotateVector2(transform.up, 30f);
                    foundPath = true;
                }

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
                        moveDir = -transform.up;
                        moveSpeed = effectiveSpeed * 0.5f;
                    }
                }

                rb.MovePosition(rb.position + moveDir * moveSpeed * Time.fixedDeltaTime);

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
            TransitionToState(EnemyState.Aware);
        }
    }

    #endregion

    #region Helper Methods

    void HandleTurningAround()
    {
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.Euler(0, 0, turnAroundTargetAngle),
            rotationSpeed * Time.fixedDeltaTime * 100f
        );

        if (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.z, turnAroundTargetAngle)) < 5f)
        {
            isTurningAround = false;
            lastDirectionChangeTime = Time.time + randomDirectionChangeInterval;

            if (isAttemptingUnstuck)
            {
                rb.AddForce(transform.up * 2f, ForceMode2D.Impulse);
            }
        }
    }

    void HandleDirectionChange()
    {
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.Euler(0, 0, turnAroundTargetAngle),
            rotationSpeed * Time.fixedDeltaTime * 50f
        );

        if (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.z, turnAroundTargetAngle)) < 2f)
        {
            isChangingDirection = false;
            lastDirectionChangeTime = Time.time + randomDirectionChangeInterval;
        }
    }

    void CheckForRandomDirectionChange()
    {
        if (Time.time > lastDirectionChangeTime + randomDirectionChangeInterval)
        {
            if (Random.value < randomDirectionChangeChance)
            {
                StartRandomDirectionChange();
            }
            else
            {
                lastDirectionChangeTime = Time.time;
            }
        }
    }

    void StartRandomDirectionChange()
    {
        isChangingDirection = true;
        float randomAngle = Random.Range(randomTurnAngleMin, randomTurnAngleMax);
        if (Random.value < 0.5f) randomAngle = -randomAngle;
        turnAroundTargetAngle = transform.eulerAngles.z + randomAngle;

        if (showDebugRays)
        {
            Debug.Log("Random direction change to angle: " + turnAroundTargetAngle);
        }
    }

    bool HasClearPath()
    {
        bool hasClearDirection = false;

        for (int i = -1; i <= 1; i++)
        {
            float angle = i * raycastSpread;
            Vector2 rayDirection = RotateVector2(transform.up, angle);

            if (showDebugRays)
            {
                Debug.DrawRay(transform.position, rayDirection * raycastDistance,
                    Color.yellow, Time.fixedDeltaTime);
            }

            RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection, raycastDistance, obstacleLayer);

            if (i == 0 && (hit.collider == null || hit.distance > obstacleAvoidanceThreshold))
            {
                hasClearDirection = true;
            }

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
        isChangingDirection = false;
        turnAroundTargetAngle = transform.eulerAngles.z + 180f;

        if (showDebugRays)
        {
            Debug.Log("No clear path - turning around to angle: " + turnAroundTargetAngle);
        }
    }

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

        Debug.DrawLine(previousPoint, transform.position, stateColor);

        if (currentState == EnemyState.Roaming)
        {
            DebugExtension.DrawCircle(transform.position, Vector3.forward, Color.blue, hearingRange);
        }

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

    public bool IsPlayerVisible()
    {
        return isPlayerInSight;
    }

    public float GetTimeToDetection()
    {
        if (currentState == EnemyState.Combat && hasPlayerBeenSeen)
        {
            return lastSeenPlayerTime - Time.time;
        }
        return -1f;
    }

    public bool IsInPlayerSight()
    {
        return isPlayerInSight && currentState == EnemyState.Combat;
    }

    void CheckIfStuck()
    {
        if (isChangingDirection || isTurningAround || isAttemptingUnstuck)
            return;

        float distanceMoved = Vector3.Distance(transform.position, lastPositionCheck);

        if (distanceMoved < stuckDetectionThreshold)
        {
            timeSinceLastMovement += Time.deltaTime;

            if (timeSinceLastMovement > stuckDetectionTime && !isStuck)
            {
                isStuck = true;
                StartCoroutine(AttemptToUnstuckCoroutine());
            }
        }
        else
        {
            timeSinceLastMovement = 0f;
            lastPositionCheck = transform.position;
            isStuck = false;
            unstuckAttempts = 0;
        }
    }

    System.Collections.IEnumerator AttemptToUnstuckCoroutine()
    {
        if (unstuckAttempts >= MAX_UNSTUCK_ATTEMPTS)
        {
            Debug.Log($"{gameObject.name} tried {unstuckAttempts} times to unstuck. Reversing direction.");
            isAttemptingUnstuck = true;
            isTurningAround = true;
            turnAroundTargetAngle = transform.eulerAngles.z + 180f;
            unstuckAttempts = 0;
            yield break;
        }

        isAttemptingUnstuck = true;
        float turnAngle = unstuckTurnAngle * (unstuckAttempts % 2 == 0 ? -1 : 1);
        turnAngle *= (1f + (unstuckAttempts * 0.2f));
        Debug.Log($"{gameObject.name} is stuck! Attempting to unstuck (attempt {unstuckAttempts + 1})");
        isTurningAround = true;
        turnAroundTargetAngle = transform.eulerAngles.z + turnAngle;
        unstuckAttempts++;
        yield return new WaitForSeconds(1.0f);
        isAttemptingUnstuck = false;
        lastPositionCheck = transform.position;
        timeSinceLastMovement = 0f;
    }
}

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

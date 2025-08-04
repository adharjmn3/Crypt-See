using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    [Header("Vision Settings")]
    [SerializeField] private float visionRange = 15f;
    [SerializeField] private float fieldOfViewAngle = 60f;
    [SerializeField] private LayerMask obstacleMask = -1;
    [SerializeField] private LayerMask playerMask = -1;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugRays = true;
    
    private Transform playerTransform;
    private Transform eyeTransform;
    
    // Public properties
    public bool CanSeePlayer { get; private set; }
    public Vector3 LastSeenPlayerPosition { get; private set; }
    public float TimeLastSeen { get; private set; }
    
    private void Awake()
    {
        eyeTransform = transform;
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
    }
    
    private void Update()
    {
        CheckForPlayer();
    }
    
    private void CheckForPlayer()
    {
        if (playerTransform == null) return;
        
        Vector3 directionToPlayer = (playerTransform.position - eyeTransform.position).normalized;
        float distanceToPlayer = Vector3.Distance(eyeTransform.position, playerTransform.position);
        
        // Check if player is within range
        if (distanceToPlayer > visionRange)
        {
            CanSeePlayer = false;
            return;
        }
        
        // Check if player is within field of view
        float angleToPlayer = Vector3.Angle(eyeTransform.forward, directionToPlayer);
        if (angleToPlayer > fieldOfViewAngle / 2)
        {
            CanSeePlayer = false;
            return;
        }
        
        // Raycast to check for obstacles
        if (Physics.Raycast(eyeTransform.position, directionToPlayer, out RaycastHit hit, distanceToPlayer, obstacleMask | playerMask))
        {
            if ((playerMask.value & (1 << hit.transform.gameObject.layer)) != 0)
            {
                CanSeePlayer = true;
                LastSeenPlayerPosition = playerTransform.position;
                TimeLastSeen = Time.time;
            }
            else
            {
                CanSeePlayer = false;
            }
        }
        else
        {
            CanSeePlayer = false;
        }
        
        // Debug visualization
        if (showDebugRays)
        {
            Debug.DrawRay(eyeTransform.position, directionToPlayer * distanceToPlayer, CanSeePlayer ? Color.green : Color.red);
        }
    }
    
    public void SetTarget(Transform target)
    {
        playerTransform = target;
    }
    
    public float GetDistanceToPlayer()
    {
        if (playerTransform == null) return float.MaxValue;
        return Vector3.Distance(transform.position, playerTransform.position);
    }
}

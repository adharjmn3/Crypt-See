using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    [SerializeField] private LayerMask obstacleMask;
    private PositionHandler agentPosition;
    private PositionHandler targetPosition;

    void Awake()
    {
        agentPosition = GetComponent<PositionHandler>();
    }

    public bool CanSeeTarget()
    {
        if (targetPosition == null)
            return false;

        Vector3 origin = agentPosition.GetPosition();
        Vector3 direction = targetPosition.GetPosition() - origin;
        float distance = direction.magnitude;

        Debug.DrawRay(origin, direction.normalized * distance, Color.red, 0.1f);
        RaycastHit2D hit = Physics2D.Raycast(origin, direction.normalized, distance, obstacleMask);
        
        return hit.collider == null;
    }

    public void SetTarget(GameObject newTarget){
        targetPosition = newTarget.GetComponent<PositionHandler>();
    }
}

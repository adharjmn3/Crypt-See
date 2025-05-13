using System.Collections;
using System.Collections.Generic;
using Player.Stats;
using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private Transform viewDirectionSource;

    Vector3 agentPosition;
    Vector3 targetPosition;
    private GameObject target;

    public bool CanSeeTarget(Vector3 agentPos, Vector3 playerPos)
    {
        if (target == null){
            return false;
        }

        agentPosition = agentPos;
        targetPosition = playerPos;

        Vector3 directionToTarget = targetPosition - agentPosition;
        float distanceToTarget = directionToTarget.magnitude;

        // Cek sudut pandang
        Vector3 forward = viewDirectionSource != null ? viewDirectionSource.up : transform.up;
        float angleToTarget = Vector3.Angle(forward, directionToTarget);

        if (angleToTarget > viewAngle / 2f)
            return false;

        // Raycast untuk deteksi halangan
        RaycastHit2D[] hits = Physics2D.RaycastAll(agentPosition, directionToTarget.normalized, distanceToTarget);

        foreach (var hit in hits)
        {
            if (((1 << hit.collider.gameObject.layer) & obstacleMask) != 0)
                return false;

            if (hit.collider.gameObject == target)
            {
                Visible visible = hit.collider.gameObject.GetComponent<Visible>();
                if (visible == null) return true;

                if (distanceToTarget < 5f && visible.LightLevel <= 1)
                    return true;
                else if (visible.LightLevel >= 1)
                    return true;

                return false;
            }
        }

        return false;
    }

    public void SetTarget(GameObject newTarget)
    {
        target = newTarget;
    }

    void OnDrawGizmosSelected()
    {
        if (viewDirectionSource == null)
        viewDirectionSource = transform;

        Gizmos.color = Color.yellow;

        Vector3 origin = viewDirectionSource.position;
        Vector3 forward = viewDirectionSource.up;

        float halfFOV = viewAngle / 2f;
        float range = 5f;

        Vector3 dirLeft = Quaternion.Euler(0, 0, -halfFOV) * forward;
        Vector3 dirRight = Quaternion.Euler(0, 0, halfFOV) * forward;

        Gizmos.DrawRay(origin, dirLeft * range);
        Gizmos.DrawRay(origin, dirRight * range);
    }
}

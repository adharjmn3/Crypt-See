using System.Collections;
using System.Collections.Generic;
using Player.Stats;
using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private Transform viewDirectionSource;
    private TrainingManager trainingManager;
    private GameObject target;

    public bool CanSeeTarget()
    {
        if (target == null)
            return false;

        Vector3 origin = trainingManager.GetAgentPosition();
        Vector3 directionToTarget = trainingManager.GetPosition(target.transform) - origin;
        float distanceToTarget = directionToTarget.magnitude;

        // CEK ANGLE TERLEBIH DAHULU
        Vector3 forward = viewDirectionSource.up;
        float angleToTarget = Vector3.Angle(forward, directionToTarget);

        if (angleToTarget > viewAngle / 2f)
        {
            return false;
        }

        // CEK RAYCAST SESUDAH LULUS ANGLE
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, directionToTarget.normalized, distanceToTarget);

        foreach (var hit in hits)
        {
            if (((1 << hit.collider.gameObject.layer) & obstacleMask) != 0)
            {
                return false;
            }

            if (hit.collider.gameObject == target)
            {
                Visible visible = hit.collider.gameObject.GetComponent<Visible>();
                if(distanceToTarget < 5f && visible.LightLevel <= 1){
                    return true;
                }
                else if(visible.LightLevel >= 1){
                    return true;
                }
                return false;
            }
        }

        return false;
    }

    public void SetTarget(GameObject newTarget, TrainingManager trainingManager){
        target = newTarget;
        this.trainingManager = trainingManager;
    }

    void OnDrawGizmosSelected()
    {
        if (viewDirectionSource == null) return;

        Gizmos.color = Color.yellow;
        Vector3 origin = trainingManager != null ? trainingManager.GetAgentPosition() : transform.position;
        Vector3 forward = viewDirectionSource.up;

        float halfFOV = viewAngle / 2f;
        float range = 5f;

        Vector3 dirLeft = Quaternion.Euler(0, 0, -halfFOV) * forward;
        Vector3 dirRight = Quaternion.Euler(0, 0, halfFOV) * forward;

        Gizmos.DrawRay(origin, dirLeft * range);
        Gizmos.DrawRay(origin, dirRight * range);
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float viewAngle = 90f; // Field of View dalam derajat
    [SerializeField] private Transform viewDirectionSource; // misalnya kepala atau badan agent
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
        Vector3 forward = viewDirectionSource.up; // asumsi right sebagai arah pandang
        float angleToTarget = Vector3.Angle(forward, directionToTarget);

        if (angleToTarget > viewAngle / 2f)
        {
            return false; // target di luar jangkauan sudut pandang
        }

        // CEK RAYCAST SESUDAH LULUS ANGLE
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, directionToTarget.normalized, distanceToTarget);

        foreach (var hit in hits)
        {
            if (((1 << hit.collider.gameObject.layer) & obstacleMask) != 0)
            {
                return false; // Ada penghalang
            }

            if (hit.collider.gameObject == target)
            {
                return true; // Player terlihat
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
        float range = 5f; // untuk visual saja

        Vector3 dirLeft = Quaternion.Euler(0, 0, -halfFOV) * forward;
        Vector3 dirRight = Quaternion.Euler(0, 0, halfFOV) * forward;

        Gizmos.DrawRay(origin, dirLeft * range);
        Gizmos.DrawRay(origin, dirRight * range);
    }

}

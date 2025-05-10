using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomPlayerMovementTraining : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float waitTime = 2f;
    [SerializeField] private float minDistance = 0.1f;

    [Header("Target Area Settings")]
    [SerializeField] private Transform[] points;

    private Vector3 targetPosition;
    private bool isMoving = false;

    void Start()
    {
        SetNewTarget();
    }

    void Update()
    {
        if (isMoving)
        {
            MoveTowardsTarget();
        }
    }

    private void MoveTowardsTarget()
    {
        Vector3 direction = (targetPosition - transform.localPosition).normalized;
        transform.localPosition += direction * moveSpeed * Time.deltaTime;

        if (Vector3.Distance(transform.localPosition, targetPosition) < minDistance)
        {
            StartCoroutine(WaitBeforeNextMove());
        }
    }

    private IEnumerator WaitBeforeNextMove()
    {
        isMoving = false;
        yield return new WaitForSecondsRealtime(waitTime);
        SetNewTarget();
    }

    private void SetNewTarget()
    {
        int index = Random.Range(0, points.Length);

        targetPosition = points[index].transform.localPosition;
        isMoving = true;
    }
}

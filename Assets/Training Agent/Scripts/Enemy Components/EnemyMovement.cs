using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float lookSpeed = 180f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Move(Vector2 newPosition, float newRotation)
    {
        Vector3 move = rb.position + newPosition * moveSpeed * Time.deltaTime;
        float rotation = rb.rotation + newRotation * lookSpeed * Time.deltaTime;

        rb.MovePosition(move);
        rb.MoveRotation(rotation);
    }
}

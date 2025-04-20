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
    
    public void Move(float moveAction, float lookAction)
    {
        Vector2 move = moveAction * moveSpeed * Time.deltaTime * transform.up;
        rb.MovePosition(rb.position + move);

        rb.MoveRotation(rb.rotation + lookAction * lookSpeed * Time.deltaTime);
    }

    public void Stop(){
        rb.velocity = Vector2.zero;
    }
}

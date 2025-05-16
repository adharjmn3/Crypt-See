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

        float rotationAmount = 0f;

        if (lookAction == 1)
        {
            rotationAmount = lookSpeed * Time.deltaTime; // Rotasi ke kiri
        }
        else if (lookAction == 2)
        {
            rotationAmount = -lookSpeed * Time.deltaTime; // Rotasi ke kanan
        }
        // Jika lookAction == 0, rotationAmount tetap 0 (tidak ada rotasi)

        rb.MoveRotation(rb.rotation + rotationAmount);
    }
}

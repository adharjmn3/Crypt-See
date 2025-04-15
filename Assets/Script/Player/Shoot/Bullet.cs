using UnityEngine;

public class Bullet : MonoBehaviour
{
    private float range;
    private int damage;
    private Vector3 startPosition;

    public void Initialize(float range, int damage)
    {
        this.range = range;
        this.damage = damage;
        startPosition = transform.position;
    }

    void Update()
    {
        // Check if the bullet has traveled beyond its range
        if (Vector3.Distance(startPosition, transform.position) >= range)
        {
            Destroy(gameObject); // Destroy the bullet
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) // Use OnTriggerEnter for 3D
    {
        // Log the object the bullet hit
        Debug.Log($"Bullet hit: {collision.gameObject.name}");

        // Destroy the bullet upon collision
        Destroy(gameObject);
    }
}
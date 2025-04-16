using UnityEngine;

public class Bullet : MonoBehaviour
{
    private float range;
    private int damage;
    private Vector3 startPosition;
    public Weapon.AmmoType ammoType; // Variable to store the weapon type

    public void Initialize(float range, int damage, Weapon.AmmoType ammoType)
    {
        this.range = range;
        this.damage = damage;
        this.ammoType = ammoType; // Assign the weapon type
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
        // Destroy the bullet upon collision
        Debug.Log($"Bullet hit: {collision.gameObject.name}, Ammo Type: {ammoType}");
        Destroy(gameObject);
    }
}
using UnityEngine;

public class CasingBehavior : MonoBehaviour
{
    [Header("Casing Settings")]
    public Transform ejectionDirection; // Transform to define the direction of the ejection
    public float ejectionForceMin = 0.5f; // Reduced minimum force for realistic ejection
    public float ejectionForceMax = 1.0f; // Reduced maximum force for realistic ejection
    public float lifetime = 10f; // Increased lifetime for better visibility

    private Rigidbody2D rb;

    void Start()
    {
        // Get the Rigidbody2D component
        rb = GetComponent<Rigidbody2D>();

        // Apply force to simulate ejection
        if (rb != null && ejectionDirection != null)
        {
            // Calculate the ejection force based on the ejectionDirection's rotation
            Vector2 ejectionForce = ejectionDirection.right * Random.Range(ejectionForceMin, ejectionForceMax);
            rb.AddForce(ejectionForce, ForceMode2D.Impulse);
        }

        // Schedule destruction of the casing after its lifetime
        Destroy(gameObject, lifetime);
    }
}
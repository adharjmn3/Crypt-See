using System.Collections.Generic;
using UnityEngine;

namespace TopDown.Movement
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Move : MonoBehaviour
    {
        private Rigidbody2D rb;
        protected Vector3 currInput; // Keep this protected
        protected float scrollInput;

        public GameObject linePrefab; // Prefab for the line (with LineRenderer)
        public float lineDuration = 0.1f; // Duration the line stays visible
        public int maxLines = 100; // Maximum number of lines allowed on the screen

        [SerializeField] private float speed;

        private Queue<GameObject> lineQueue = new Queue<GameObject>(); // Queue to manage line instances

        // Add a public property to expose currInput as read-only
        public Vector3 CurrentInput => currInput;
        public float CurrentSpeed => speed; // Expose speed as a read-only property

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            rb.velocity = currInput * speed * Time.fixedDeltaTime;

            // Spawn a line at the player's position every frame
            if (currInput != Vector3.zero) // Only spawn lines if the player is moving
            {
                DrawFootstepLine(transform.position);
            }
        }

        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
        }

        public void DrawFootstepLine(Vector3 position)
        {
            if (linePrefab != null)
            {
                // Instantiate the line prefab at the player's position
                GameObject lineInstance = Instantiate(linePrefab, position, Quaternion.identity);

                // Add the new line to the queue
                lineQueue.Enqueue(lineInstance);

                // If the number of lines exceeds the maximum, remove the oldest line
                if (lineQueue.Count > maxLines)
                {
                    GameObject oldestLine = lineQueue.Dequeue();
                    Destroy(oldestLine);
                }

                // Set the start and end points of the line
                LineRenderer lineRenderer = lineInstance.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    lineRenderer.SetPosition(0, position); // Start point
                    lineRenderer.SetPosition(1, position + Vector3.down * 0.1f); // End point slightly below
                }

                // Destroy the line after the specified duration
                Destroy(lineInstance, lineDuration);
            }
        }
    }
}

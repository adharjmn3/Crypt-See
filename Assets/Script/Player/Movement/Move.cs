using UnityEngine;

namespace TopDown.Movement
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Move : MonoBehaviour
    {
        private Rigidbody2D rb;
        protected Vector3 currInput; // Keep this protected
        protected float scrollInput;
        [SerializeField] private float speed;

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
        }

        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
        }
    }
}

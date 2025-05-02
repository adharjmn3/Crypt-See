using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDown.Movement
{
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerMovement : Move
    {
        private float baseSpeed = 400f;
        [SerializeField] private float speedIncrement = 1f;
        [SerializeField] private float maxSpeed = 1000f; // Maximum speed limit
        [SerializeField] private float minSpeed = 100f; // Minimum speed limit

        [SerializeField] private float minVolume = 0.1f; // Minimum volume
        [SerializeField] private float maxVolume = 1.0f; // Maximum volume 
        public bool CanMove { get; set; } = true; // New property to control movement

        public float CurrentSpeed
        {
            get { return baseSpeed; }
        }

        // Expose minSpeed and maxSpeed as public properties
        public float MinSpeed => minSpeed;
        public float MaxSpeed => maxSpeed;

        private void OnMove(InputValue value)
        {
            if (CanMove) // Only process movement input if CanMove is true
            {
                Vector3 playerInput = new Vector3(value.Get<Vector2>().x, value.Get<Vector2>().y, 0);
                currInput = playerInput;

                // Draw a line at the player's position
            }
            else
            {
                currInput = Vector3.zero; // Stop movement if CanMove is false
            }
        }

        private void OnScroll(InputValue value)
        {
            float scrollDelta = value.Get<Vector2>().y;
            baseSpeed += scrollDelta * speedIncrement;
            baseSpeed = Mathf.Clamp(baseSpeed, minSpeed, maxSpeed); // Limit the speed to a maximum value

            SetSpeed(baseSpeed);

            // Update volume based on speed
            float volume = CalculateVolume(baseSpeed);
            // Debug.Log($"Current Volume: {volume}");
        }

        public float CalculateVolume(float speed)
        {
            // Normalize speed to a 0-1 range based on minSpeed and maxSpeed
            float normalizedSpeed = (speed - minSpeed) / (maxSpeed - minSpeed);

            // Generate a random volume within the range based on normalized speed
            float volume = Random.Range(
                Mathf.Lerp(minVolume, maxVolume, normalizedSpeed * 0.5f), // Lower bound
                Mathf.Lerp(minVolume, maxVolume, normalizedSpeed)         // Upper bound
            );

            return volume;
        }

       
    }
}

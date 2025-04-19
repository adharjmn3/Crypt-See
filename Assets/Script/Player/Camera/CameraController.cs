using UnityEngine;

namespace TopDown.CameraController
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float displacementMult = 0.15f;
        private float zOffset = -10f;

        private Transform originalTarget; // Store the original target

        private void Start()
        {
            originalTarget = target; // Save the initial target (player)
        }

        private void Update()
        {
            if (target == null) return;

            // Calculate mouse position
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 cameraDisplacement = (mousePos - target.position) * displacementMult;

            Vector3 finalPos = target.position + cameraDisplacement;
            finalPos.z = zOffset;

            transform.position = finalPos;
        }

        public void SetTargetPosition(Vector3 position)
        {
            // Move the camera to the specified position
            transform.position = new Vector3(position.x, position.y, zOffset);
        }

        public void ResetToPlayer()
        {
            // Reset the camera to the original target (player)
            if (originalTarget != null)
            {
                target = originalTarget;
            }
        }
    }
}

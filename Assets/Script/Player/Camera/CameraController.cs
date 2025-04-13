
using UnityEngine;

namespace TopDown.CameraController{

    public class CameraController : MonoBehaviour
    {
      [SerializeField] private Transform target;
      [SerializeField] private float displacementMult = 0.15f;
      private float zOffset = -10f;

      private void Update()
      {
        // calculate mouse position
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 cameraDisplacement = (mousePos - target.position) * displacementMult;

        Vector3 finalPos = target.position + cameraDisplacement;
        finalPos.z = zOffset;

        transform.position = finalPos;


      }
}

}

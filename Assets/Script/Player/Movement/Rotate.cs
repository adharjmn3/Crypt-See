
using UnityEngine;

namespace TopDown.Movement
{

    public class Rotate : MonoBehaviour
    {
        protected void LookAt(Vector3 target)
        {
            float angle = AngleBetweenTwoPoints(transform.position, target) + 90;
            transform.eulerAngles = new Vector3(0, 0, angle);
        }

        private float AngleBetweenTwoPoints(Vector3 a, Vector3 b)
        {
            return Mathf.Atan2(a.y - b.y, a.x - b.x) * Mathf.Rad2Deg;
        }

    }
}

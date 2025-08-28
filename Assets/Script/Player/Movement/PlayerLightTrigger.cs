using UnityEngine;

public class PlayerLightTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        LightBehaviour light = other.GetComponent<LightBehaviour>();
        if (light != null)
        {
            light.EnableLight();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        LightBehaviour light = other.GetComponent<LightBehaviour>();
        if (light != null)
        {
            light.DisableLight();
        }
    }
}
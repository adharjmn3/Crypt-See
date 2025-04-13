
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDown.Movement
{
    // 
    public class PlayerRotation : Rotate
    {
     private void OnLook(InputValue value)
     {
         Vector2 mousePos = Camera.main.ScreenToWorldPoint(value.Get<Vector2>());
         LookAt(mousePos);
     }
    }
}

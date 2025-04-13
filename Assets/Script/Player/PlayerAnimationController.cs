using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TopDown.Movement
{
    public class PlayerAnimationController : MonoBehaviour
    {
        public Animator animator; // Reference to the Animator component attached to the player character.
        private PlayerMovement playerMovement; // Reference to the PlayerMovement script for input detection.

        // Start is called before the first frame update
        void Start()
        {
            // Get the PlayerMovement component attached to the player.
            playerMovement = GetComponent<PlayerMovement>();
        }

        // Update is called once per frame
        void Update()
        {
            // Check if the player is moving by checking the input vector from PlayerMovement.
            bool isMoving = playerMovement.CurrentInput.magnitude > 0.1f;

            // Update the Animator's "isMove" parameter.
            animator.SetBool("isMove", isMoving);
            Debug.Log("isMove: " + isMoving); // Log the value of isMoving for debugging.
        }
    }
}

